using System.Configuration;

namespace ZooManager.Infrastructure.Configuration
{
    public class DatabaseConfig
    {
        public static string GetConnectionString()
        {
            return "Server=localhost;Database=ZooManagerDB;Uid=root;Pwd=charlix37;";
        }

        // OPTION 2: Aus App.config/Web.config lesen (empfohlen)
        public static string GetConnectionStringFromConfig()
        {
            return ConfigurationManager.ConnectionStrings["ZooManagerDB"].ConnectionString;
        }
    }
}