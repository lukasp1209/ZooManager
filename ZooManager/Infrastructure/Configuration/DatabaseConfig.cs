using System.Configuration;

namespace ZooManager.Infrastructure.Configuration
{
    public class DatabaseConfig
    {
        /// <summary>
        /// Gibt den ConnectionString aus der App.config zurück. 
        /// Falls keiner konfiguriert ist, wird ein Standard-Fallback genutzt.
        /// </summary>
        public static string GetConnectionString()
        {
            var config = ConfigurationManager.ConnectionStrings["ZooManagerDB"];
            
            if (config != null)
            {
                return config.ConnectionString;
            }

            // Fallback für die lokale Entwicklung
            return "Server=localhost;Database=ZooManagerDB;Uid=root;Pwd=password;";
        }
    }
}