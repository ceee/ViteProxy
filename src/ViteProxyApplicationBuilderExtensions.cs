using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace ViteProxy;

public static class ViteProxyApplicationBuilderExtensions
{
  public static IApplicationBuilder UseViteStaticFiles(this IApplicationBuilder app)
  {
    IWebHostEnvironment env = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>();
    IOptions<ViteProxyOptions> options = app.ApplicationServices.GetRequiredService<IOptions<ViteProxyOptions>>();
    string workingDirectory = (options.Value.WorkingDirectory ?? String.Empty).Trim('/');

    if (workingDirectory == null)
    {
      return app;
    }

    app.UseStaticFiles();
    app.UseStaticFiles(new StaticFileOptions
    {
      FileProvider = new PhysicalFileProvider(Path.Combine(env.ContentRootPath, workingDirectory))
    });

    return app;
  }


  public static IApplicationBuilder UseViteStaticFiles(this IApplicationBuilder app, string path)
  {
    IWebHostEnvironment env = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>();

    if (path == null)
    {
      return app;
    }

    app.UseStaticFiles();
    app.UseStaticFiles(new StaticFileOptions
    {
      FileProvider = new PhysicalFileProvider(Path.Combine(env.ContentRootPath, path))
    });

    return app;
  }
}
