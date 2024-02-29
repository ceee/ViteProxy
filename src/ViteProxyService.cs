using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ViteProxy;

public class ViteProxyService : IHostedService
{
  ViteProxyOptions options;
  ViteWorkingDirectory workingDirectory;
  ILogger<ViteProxyService> logger;
  bool isRunning = false;


  public ViteProxyService(IWebHostEnvironment env, ILogger<ViteProxyService> logger, IOptions<ViteProxyOptions> options) : this(env, logger, options.Value ?? new()) { }

  public ViteProxyService(IWebHostEnvironment env, ILogger<ViteProxyService> logger, ViteProxyOptions options)
  {
    this.options = options ?? new();   
    this.workingDirectory = new ViteWorkingDirectory(env, options.WorkingDirectory);
    this.logger = logger;
  }


  /// <inheritdoc />
  public async Task StartAsync(CancellationToken cancellationToken)
  {
    if (!this.options.Enabled)
    {
      return;
    }

    // locate npm version and throw if it is not installed
    Version npmVersion = await FindNpmVersion() ?? throw new Exception("Please install node+npm to use the vite dev service (https://www.npmjs.com/)");

    // start vite server
    ProcessProxy viteProcess = await StartDevServer(options.Port);
    logger.LogInformation("vite listening on: http://localhost:{port} (path: {workingDirectory})", options.Port, this.workingDirectory.Path);
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

    ProcessProxy process = new ProcessProxy(workingDirectory.Path, "npm").Argument("-v").Capture((value, err) =>
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
    ProcessProxy process = new ProcessProxy(workingDirectory.Path, "npm", options.ForwardLog)
      .Argument("run " + options.ScriptName)
      .EnvVar("PORT", port.ToString())
      .Capture(CaptureLog);

    await process.RunAsync(options.StartupCondition, TimeSpan.FromSeconds(options.TimeoutInSeconds));

    isRunning = true;

    return process;
  }


  void CaptureLog(string line, bool isError)
  {
    if (!isRunning)
    {
      return;
    }

    if (isError)
    {
      logger.LogWarning(line);
    }
    else
    {
      logger.LogInformation(line);
    }
  }
}
