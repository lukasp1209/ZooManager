using System.Windows;
using ZooManager.UI.ViewModels;
using ZooManager.UI.Views;

namespace ZooManager
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var loginView = new LoginView
            {
                DataContext = new LoginViewModel()
            };

            loginView.Show();
        }
    }
}