using Microsoft.AspNetCore.Hosting;

namespace ViteProxy;

internal class ViteWorkingDirectory
{
  public string Path { get; protected set; }

  public string InputPath { get; protected set; }

  public ViteWorkingDirectory(IWebHostEnvironment env, string inputPath)
  {
    InputPath = inputPath;
    string path = inputPath.Replace('\\', '/').Trim().TrimEnd('/');

    if (System.IO.Path.IsPathFullyQualified(path))
    {
      Path = path;
    }
    else 
    {
      Path = System.IO.Path.Combine(env.ContentRootPath, path);
    }
  }
}
