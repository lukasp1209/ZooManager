using System.Windows;
using ZooManager.Core.Interfaces;
using ZooManager.Core.Services;
using ZooManager.Infrastructure.Persistence.Connection;
using ZooManager.UI.Views;

namespace ZooManager
{
    public partial class App
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            IPersistenceService persistenceService = new SqlitePersistenceService("zoo.db");
            IAuthenticationService authService = new AuthenticationService(persistenceService);
            
            var loginWindow = new LoginWindow(authService, persistenceService);
            loginWindow.Show();
        }
    }
}