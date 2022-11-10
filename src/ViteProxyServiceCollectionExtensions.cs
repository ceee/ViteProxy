using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ViteProxy;

public static class ViteProxyServiceCollectionExtensions
{
  public static IServiceCollection AddViteProxy(this IServiceCollection services)
  {
    services.AddLogging();
    services.AddOptions();
    services.AddHostedService<ViteProxyService>();
    services.AddOptions<ViteProxyOptions>().BindConfiguration("Vite");

    return services;
  }


  public static IServiceCollection AddViteProxy(this IServiceCollection services, Action<ViteProxyOptions> configure)
  {
    services.AddLogging();
    services.AddOptions();
    services.AddHostedService<ViteProxyService>();
    services.AddOptions<ViteProxyOptions>().BindConfiguration("Vite");
    services.PostConfigure(configure);

    return services;
  }


  public static IServiceCollection AddViteProxy(this IServiceCollection services, IConfiguration namedConfigurationSection)
  {
    services.AddLogging();
    services.AddOptions();
    services.AddHostedService<ViteProxyService>();
    services.AddOptions<ViteProxyOptions>().Bind(namedConfigurationSection);

    return services;
  }
}
