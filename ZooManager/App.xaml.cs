using System.Windows;
using ZooManager.Infrastructure.Persistence;
using ZooManager.Core.Interfaces;
using ZooManager.UI.Views;

namespace ZooManager
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            IPersistenceService persistenceService = new SqlitePersistenceService("zoo.db");
            
            var mainWindow = new MainWindow(persistenceService);
            mainWindow.Show();
        }
    }
}