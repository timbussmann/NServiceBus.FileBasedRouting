using NServiceBus.Persistence;

namespace FileBasedRouting
{
    public class StaticRoutingPersistence : PersistenceDefinition
    {
        public StaticRoutingPersistence()
        {
            Supports<StorageType.Subscriptions>(s => { });
        }
    }
}