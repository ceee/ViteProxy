using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ViteProxy;

public static class ViteProxyServiceCollectionExtensions
{
  public static IServiceCollection AddViteProxy(this IServiceCollection services, string configSectionPath = "Vite")
  {
    services.AddLogging();
    services.AddOptions();
    services.AddHostedService<ViteProxyService>();
    services.AddOptions<ViteProxyOptions>().BindConfiguration(configSectionPath);

    return services;
  }


  public static IServiceCollection AddViteProxy(this IServiceCollection services, Action<ViteProxyOptions> configure, string configSectionPath = "Vite")
  {
    services.AddLogging();
    services.AddOptions();
    services.AddHostedService<ViteProxyService>();
    services.AddOptions<ViteProxyOptions>().BindConfiguration(configSectionPath);
    services.PostConfigure(configure);

    return services;
  }


  public static IServiceCollection AddViteProxy(this IServiceCollection services, string configName, IConfiguration namedConfigurationSection)
  {
    services.AddLogging();
    services.AddOptions();
    services.AddHostedService((svc) => new ViteProxyService(
      env: svc.GetService<IWebHostEnvironment>(), 
      logger: svc.GetService<ILogger<ViteProxyService>>(),
      options: svc.GetService<IOptionsSnapshot<ViteProxyOptions>>().Get(configName)
    ));
    services.AddOptions<ViteProxyOptions>(configName).Bind(namedConfigurationSection);

    return services;
  }
}
