using System.Windows;
using ZooManager.Infrastructure.Persistence;
using ZooManager.Core.Interfaces;
using ZooManager.Infrastructure.Authentication;
using ZooManager.UI.Views;

namespace ZooManager
{
    public partial class App : Application
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