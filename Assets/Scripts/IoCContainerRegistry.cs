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
        ioc.RegisterShared<ILogger, UnityLogger>();
        ioc.RegisterShared<IItemResolver, ItemResolver>();
        //this.ioc.RegisterShared<IItemRepository, RavenNestItemRepository>();
        //this.ioc.RegisterShared<IPlayerRepository, PlayerRepository>();
    }
}