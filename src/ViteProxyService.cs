using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ViteProxy;

public class ViteProxyService : IHostedService
{
  readonly ViteProxyOptions _options;
  readonly ViteWorkingDirectory _workingDirectory;
  readonly ILogger<ViteProxyService> _logger;
  readonly IWebHostEnvironment _env;
  bool _isRunning = false;


  public ViteProxyService(IWebHostEnvironment env, ILogger<ViteProxyService> logger, IOptions<ViteProxyOptions> options) : this(env, logger, options.Value ?? new()) { }

  public ViteProxyService(IWebHostEnvironment env, ILogger<ViteProxyService> logger, ViteProxyOptions options)
  {
    _options = options ?? new();
    _workingDirectory = new ViteWorkingDirectory(env, _options.WorkingDirectory);
    _env = env;
    _logger = logger;
  }


  /// <inheritdoc />
  public async Task StartAsync(CancellationToken cancellationToken)
  {
    if (!_options.Enabled || !_env.IsDevelopment())
    {
      return;
    }

    // locate npm version and throw if it is not installed
    Version npmVersion = await FindNpmVersion() ?? throw new Exception("Please install node+npm to use the vite dev service (https://www.npmjs.com/)");

    // start vite server
    ProcessProxy viteProcess = await StartDevServer(_options.Port);
    _logger.LogInformation("vite listening on: http://localhost:{port} (path: {workingDirectory})", _options.Port, _workingDirectory.Path);
  }

  public Task StopAsync(CancellationToken cancellationToken)
  {
    return Task.CompletedTask;
  }


  /// <summary>
  /// Get the node version from the system
  /// </summary>
  async Task<Version> FindNpmVersion()
  {
    Version version = null;

    ProcessProxy process = new ProcessProxy(_workingDirectory.Path, "npm").Argument("-v").Capture((value, err) =>
    {
      if (version == null && !value.Contains("not recognized") && Version.TryParse(value, out Version _version))
      {
        version = _version;
      }
    });

    await process.ExecuteAsync();

    return version;
  }


  /// <summary>
  /// Starts the vite dev server which also support HMR
  /// </summary>
  async Task<ProcessProxy> StartDevServer(int port)
  {
    // if the port we want to use is occupied, terminate the process utilizing that port.
    // this occurs when "stop" is used from the debugger and the middleware does not have the opportunity to kill the process
    PidUtils.KillPort((ushort)port, true);

    // create and run the vite script
    ProcessProxy process = new ProcessProxy(_workingDirectory.Path, "npm", _options.ForwardLog)
      .Argument("run " + _options.ScriptName)
      .EnvVar("PORT", port.ToString())
      //.EnvVar("URL_PREFIX", "/@viteproxy/")
      .Capture(CaptureLog);

    await process.RunAsync(_options.StartupCondition, TimeSpan.FromSeconds(_options.TimeoutInSeconds));

    _isRunning = true;

    return process;
  }


  void CaptureLog(string line, bool isError)
  {
    if (!_isRunning)
    {
      return;
    }

    if (isError)
    {
      _logger.LogWarning(line);
    }
    else
    {
      _logger.LogInformation(line);
    }
  }
}
