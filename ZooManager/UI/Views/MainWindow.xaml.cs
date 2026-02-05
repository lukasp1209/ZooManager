using System.Windows;
using ZooManager.Core.Interfaces;
using ZooManager.UI.ViewModels;

namespace ZooManager.UI.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow(IPersistenceService persistenceService, IAuthenticationService authService)
        {
            InitializeComponent();

            DataContext = new MainWindowViewModel(
                persistenceService,
                authService,
                openLoginWindow: () =>
                {
                    var login = new LoginWindow(authService, persistenceService);
                    login.Show();
                },
                closeMainWindow: Close,
                confirmLogout: () =>
                {
                    var dlg = new LogoutDialog
                    {
                        Owner = this,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    };
                    dlg.ShowDialog();
                    return dlg.Confirmed;
                });
        }
    }
}