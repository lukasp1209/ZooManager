using System.Linq;
using System.Windows.Controls;
using ZooManager.Core.Interfaces;
using ZooManager.Infrastructure.Persistence;
using ZooManager.Infrastructure.Configuration;

namespace ZooManager.UI.Views
{
    public partial class DashboardView : UserControl
    {
        private readonly IPersistenceService _db;
        
        public DashboardView(IPersistenceService persistenceService = null)
        {
            InitializeComponent(); 
            
            _db = persistenceService ?? new SqlitePersistenceService(DatabaseConfig.GetConnectionString());
            
            LoadDashboardStats();
        }

        private void LoadDashboardStats()
        {
            var allAnimals = _db.LoadAnimals().ToList();
            var allEnclosures = _db.LoadEnclosures().ToList();
            var allEmployees = _db.LoadEmployees().ToList();
            var allEvents = _db.LoadEvents().ToList();
            
            TotalAnimalsText.Text = allAnimals.Count.ToString();
            TotalEnclosuresText.Text = allEnclosures.Count.ToString();
            TotalEmployeesText.Text = allEmployees.Count.ToString(); 
            
            FeedingPreviewList.ItemsSource = allAnimals
                .OrderBy(a => a.NextFeedingTime)
                .Take(3).ToList();
            
            EventsPreviewList.ItemsSource = allEvents
                .Where(e => e.Start >= System.DateTime.Now)
                .OrderBy(e => e.Start)
                .Take(3)
                .ToList();
        }

        // Methode zum manuellen Aktualisieren der Daten
        public void RefreshData()
        {
            LoadDashboardStats();
        }

        private void OpenFeedingPlan_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var mainWindow = System.Windows.Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.MainContentPresenter.Content = new FeedingView();
            }
        }

        private void OpenEvents_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var mainWindow = System.Windows.Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.MainContentPresenter.Content = new EventsView();
            }
        }
    }
}
