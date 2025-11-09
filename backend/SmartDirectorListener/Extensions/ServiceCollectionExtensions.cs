using Backend.SmartDirectorListener;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.SmartDirectorListener.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSmartDirectorListener(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SmartDirectorOptions>(configuration.GetSection("SmartDirector"));
        services.AddSingleton<SdDecoder>();
        services.AddHostedService<SmartDirectorWorker>();
        return services;
    }
}