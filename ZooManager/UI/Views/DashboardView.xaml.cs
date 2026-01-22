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
        public DashboardView(IPersistenceService persistenceService)
        {
            _db = persistenceService;
            InitializeComponent();
            LoadDashboardStats(new SqlitePersistenceService("zoo.db"));
        }

        private void LoadDashboardStats(SqlitePersistenceService db)
        {
            var allAnimals = db.LoadAnimals().ToList();
            var allEnclosures = db.LoadEnclosures().ToList();
            var allEmployees = db.LoadEmployees().ToList();
            var allEvents = db.LoadEvents().ToList();
            
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

        private void OpenFeedingPlan_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            // Navigation zum Fütterungsplan UserControl
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
