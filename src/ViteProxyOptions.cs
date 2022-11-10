namespace ViteProxy;

public class ViteProxyOptions
{
  /// <summary>
  /// Port which is used for the vite dev server
  /// </summary>
  public int Port { get; set; } = 5123;

  /// <summary>
  /// Whether to display forward npm console logs to dotnet logger
  /// </summary>
  public bool ForwardLog { get; set; } = false;

  /// <summary>
  /// Directory where the frontend files are located
  /// </summary>
  public string WorkingDirectory { get; set; }

  /// <summary>
  /// Whether to enable the proxy or not
  /// </summary>
  public bool Enabled { get; set; } = true;

  /// <summary>
  /// Startup timeout for the node service
  /// </summary>
  public int TimeoutInSeconds { get; set; } = 60;

  /// <summary>
  /// Name of the npm script to run
  /// </summary>
  public string ScriptName { get; set; } = "dev";

  /// <summary>
  /// The service waits for console input from the npm script
  /// And continues if it could find the supplied text
  /// </summary>
  public string StartupCondition { get; set; } = "ready in";
}
