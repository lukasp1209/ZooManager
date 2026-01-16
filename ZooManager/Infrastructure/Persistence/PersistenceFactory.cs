using ZooManager.Core.Interfaces;
using ZooManager.Infrastructure.Configuration;

namespace ZooManager.Infrastructure.Persistence;

public static class PersistenceFactory
{
    public static MySqlPersistenceService CreateRepository() 
    {
        return new MySqlPersistenceService(DatabaseConfig.GetConnectionString());
    }
}