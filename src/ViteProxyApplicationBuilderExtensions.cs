using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace ViteProxy;

public static class ViteProxyApplicationBuilderExtensions
{
  /// <summary>
  /// Provides static files from the vite working directory or a custom directory or from the named vite settings
  /// </summary>
  /// <param name="app">The app builder</param>
  /// <param name="path">Provide a file path for static file delivery</param>
  /// <param name="configName">When vite proxy is added via named options these can be used here for correct retrieval of the vite working directory</param>
  /// <returns>The app builde</returns>
  public static IApplicationBuilder UseViteStaticFiles(this IApplicationBuilder app, string path = null, string configName = null)
  {
    IWebHostEnvironment env = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>();

    if (path == null && configName == null)
    {
      IOptions<ViteProxyOptions> options = app.ApplicationServices.GetRequiredService<IOptions<ViteProxyOptions>>();
      path = options.Value.WorkingDirectory;
    }

    if (configName != null)
    {
      using IServiceScope scope = app.ApplicationServices.CreateScope();
      IOptionsSnapshot<ViteProxyOptions> optionsSnapschat = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<ViteProxyOptions>>();
      ViteProxyOptions options = optionsSnapschat.Get(configName);
      path = options.WorkingDirectory;
    }

    if (path == null)
    {
      return app;
    }

    // TODO build full path
    ViteWorkingDirectory workingDirectory = new(env, path);

    app.UseStaticFiles();
    app.UseStaticFiles(new StaticFileOptions
    {
      FileProvider = new PhysicalFileProvider(workingDirectory.Path)
    });

    return app;
  }
}
