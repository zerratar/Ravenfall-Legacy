
public class IoCContainerRegistry
{
    private readonly IIoC ioc;

    public IoCContainerRegistry(IIoC ioc)
    {
        this.ioc = ioc;
        SetupIoCContainer();
    }

    private void SetupIoCContainer()
    {
        ioc.RegisterShared<RavenNest.SDK.ILogger, RavenNest.SDK.UnityLogger>();
        ioc.RegisterShared<IItemResolver, ItemResolver>();
    }
}