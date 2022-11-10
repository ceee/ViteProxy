using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace ViteProxy;

public class ProcessProxy
{
  string workingDirectory;
  string script;
  Action<ProcessStartInfo> onProcessConfigure = null;
  Action<string, bool> captureLog = null;
  bool isWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
  Dictionary<string, string> envVars = new();
  HashSet<string> arguments = new();
  bool forwardLog = false;

  Process process = null;
  EventedStreamReader stdOut;
  EventedStreamReader stdErr;




  public ProcessProxy(string workingDirectory, string script, bool forwardLog = false)
  {
    this.workingDirectory = workingDirectory;
    this.script = script;
    this.forwardLog = forwardLog;
  }


  /// <summary>
  /// Adds an environment variable
  /// </summary>
  public ProcessProxy EnvVar(string key, string value)
  {
    envVars.Add(key, value);
    return this;
  }


  /// <summary>
  /// Adds an argument
  /// </summary>
  public ProcessProxy Argument(string argument)
  {
    arguments.Add(argument);
    return this;
  }


  /// <summary>
  /// Configures the process start info
  /// </summary>
  public ProcessProxy Configure(Action<ProcessStartInfo> onProcessConfigure)
  {
    this.onProcessConfigure = onProcessConfigure;
    return this;
  }


  /// <summary>
  /// Capture the log instead of outputting it to the console
  /// </summary>
  public ProcessProxy Capture(Action<string, bool> action)
  {
    this.captureLog = action;
    return this;
  }


  /// <summary>
  /// Run the script and wait for completion.
  /// This is only recommended for scripts which finish automatically and have no user interaction.
  /// </summary>
  public async Task ExecuteAsync(TimeSpan timeout = default)
  {
    StartProcess();

    using var stdErrReader = new EventedStreamStringReader(stdErr);
    try
    {
      // TODO implement timeout
      // https://stackoverflow.com/questions/18760252/timeout-an-async-method-implemented-with-taskcompletionsource
      await stdOut.WaitForFinish(); 
    }
    catch (EndOfStreamException ex)
    {
      throw new InvalidOperationException($"The script '{script}' exited without indicating that the server was listening for requests.\nThe error output was: " + $"{stdErrReader.ReadAsString()}", ex);
    }
  }


  /// <summary>
  /// Run the script and return as soon as a condition is met.
  /// The script will not cancel and will continue to run as a sub-process as long as it does not auto-close.
  /// </summary>
  public async Task RunAsync(string startupCondition, TimeSpan startupTimeout = default)
  {
    StartProcess();

    using var stdErrReader = new EventedStreamStringReader(stdErr);
    try
    {
      await stdOut.WaitForMatch(new Regex(startupCondition, RegexOptions.IgnoreCase, startupTimeout == default ? TimeSpan.FromMinutes(5) : startupTimeout));
    }
    catch (EndOfStreamException ex)
    {
      throw new InvalidOperationException($"The script '{script}' exited without indicating that the server was listening for requests.\nThe error output was: " + $"{stdErrReader.ReadAsString()}", ex);
    }
  }


  public void Exit()
  {
    try { process?.Kill(); } catch { }
    try { process?.WaitForExit(); } catch { }

    AppDomain.CurrentDomain.DomainUnload -= UnloadHandler;
    AppDomain.CurrentDomain.ProcessExit -= UnloadHandler;
    AppDomain.CurrentDomain.UnhandledException -= UnloadHandler;
  }


  /// <summary>
  /// Run
  /// </summary>
  Process StartProcess()
  {
    string executable = script;

    StringBuilder command = new();
    command.Append(script);
    command.Append(' ');
    foreach (string arg in arguments)
    {
      command.Append(arg);
    }
    string argumentList = command.ToString();

    if (isWindows)
    {
      argumentList = $"/c {argumentList}";
      executable = "cmd";
    }

    ProcessStartInfo startInfo = new(executable)
    {
      Arguments = argumentList,
      UseShellExecute = false,
      RedirectStandardInput = true,
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      WorkingDirectory = workingDirectory
    };

    foreach (var envVar in envVars)
    {
      startInfo.Environment[envVar.Key] = envVar.Value;
    }

    onProcessConfigure?.Invoke(startInfo);

    try
    {
      process = Process.Start(startInfo);
      process.EnableRaisingEvents = true;
    }
    catch (Exception ex)
    {
      var message = $"Failed to start '{startInfo.FileName}'. To resolve this:.\n\n"
                  + $"[1] Ensure that '{startInfo.FileName}' is installed and can be found in one of the PATH directories.\n"
                  + $"    Current PATH enviroment variable is: { Environment.GetEnvironmentVariable("PATH") }\n"
                  + "    Make sure the executable is in one of those directories, or update your PATH.\n\n"
                  + "[2] See the InnerException for further details of the cause.";
      throw new InvalidOperationException(message, ex);
    }

    AttachLogger();

    AppDomain.CurrentDomain.DomainUnload += UnloadHandler;
    AppDomain.CurrentDomain.ProcessExit += UnloadHandler;
    AppDomain.CurrentDomain.UnhandledException += UnloadHandler;

    return process;
  }


  void UnloadHandler(object sender, EventArgs e)
  {
    Exit();
  }


  void AttachLogger(bool isStream = false)
  {
    stdOut = new EventedStreamReader(process.StandardOutput);
    stdErr = new EventedStreamReader(process.StandardError);

    stdOut.OnReceivedLine += line => WriteToLog(line);
    stdOut.OnReceivedChunk += chunk => WriteToLog(chunk);
    stdErr.OnReceivedLine += line => WriteToLog(line, true);
    stdErr.OnReceivedChunk += chunk => WriteToLog(chunk, true);
  }


  void WriteToLog(string line, bool isError = false)
  {
    if (String.IsNullOrWhiteSpace(line))
    {
      return;
    }

    line = line.StartsWith("<s>") ? line.Substring(3) : line;

    if (captureLog != null)
    {
      captureLog(line, isError);
    }
    if (forwardLog)
    {
      (isError ? Console.Error : Console.Out).WriteLine(line);
    }
  }


  void WriteToLog(ArraySegment<char> chunk, bool isError = false)
  {
    bool containsNewline = Array.IndexOf(chunk.Array, '\n', chunk.Offset, chunk.Count) >= 0;

    if (containsNewline)
    {
      return;
    }

    if (captureLog != null)
    {
      captureLog(new String(chunk.Array, chunk.Offset, chunk.Count), isError);
    }
    if (forwardLog)
    {
      (isError ? Console.Error : Console.Out).Write(chunk.Array, chunk.Offset, chunk.Count);
    }
  }
}
