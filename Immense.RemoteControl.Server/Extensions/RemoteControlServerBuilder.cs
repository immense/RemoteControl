using Immense.RemoteControl.Server.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Immense.RemoteControl.Server.Extensions;

public interface IRemoteControlServerBuilder
{
    void AddHubEventHandler<T>()
        where T : class, IHubEventHandler;

    void AddViewerAuthorizer<T>()
        where T : class, IViewerAuthorizer;

    void AddViewerPageDataProvider<T>()
        where T : class, IViewerPageDataProvider;
}

internal class RemoteControlServerBuilder : IRemoteControlServerBuilder
{
    private readonly IServiceCollection _services;

    public RemoteControlServerBuilder(IServiceCollection services)
    {
        _services = services;
    }

    public void AddHubEventHandler<T>() 
        where T : class, IHubEventHandler
    {
        _services.AddSingleton<IHubEventHandler, T>();
    }

    public void AddViewerAuthorizer<T>() 
        where T : class, IViewerAuthorizer
    {
        _services.AddScoped<IViewerAuthorizer, T>();
    }

    public void AddViewerPageDataProvider<T>() 
        where T : class, IViewerPageDataProvider
    {
        _services.AddScoped<IViewerPageDataProvider, T>();
    }

    internal void Validate()
    {
        var serviceTypes = new[]
        {
            typeof(IHubEventHandler),
            typeof(IViewerAuthorizer),
            typeof(IViewerPageDataProvider)
        };

        foreach (var type in serviceTypes)
        {
            if (!_services.Any(x => x.ServiceType == type))
            {
                throw new Exception($"Missing service registration for type {type.Name}.");
            }
        }
    }
}
