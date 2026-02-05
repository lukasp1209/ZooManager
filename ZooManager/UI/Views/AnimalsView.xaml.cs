using System.Windows.Controls;
using ZooManager.Core.Interfaces;
using ZooManager.Infrastructure.Configuration;
using ZooManager.Infrastructure.Persistence.Connection;
using ZooManager.UI.ViewModels;

namespace ZooManager.UI.Views
{
    public partial class AnimalsView : UserControl
    {
        public AnimalsView(IPersistenceService persistenceService = null, IAuthenticationService authService = null)
        {
            InitializeComponent();

            var db = persistenceService ?? new SqlitePersistenceService(DatabaseConfig.GetConnectionString());
            DataContext = new AnimalsViewModel(db, authService);
        }
    }
}