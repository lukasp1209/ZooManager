using System.Windows;
using System.Windows.Controls;
using ZooManager.Core.Models;

namespace ZooManager.UI.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            MainContentPresenter.Content = new DashboardView();
        }
        

        private void TestData()
        {
            try 
            {
                var db = new ZooManager.Infrastructure.Persistence.MySqlPersistenceService(
                    ZooManager.Infrastructure.Configuration.DatabaseConfig.GetConnectionString());
                
                var animals = db.LoadAnimals();
                foreach (var a in animals)
                {
                    System.Diagnostics.Debug.WriteLine($"Gefundenes Tier: {a.Name}");
                    if (a.Attributes.ContainsKey("Mähnenfarbe"))
                        System.Diagnostics.Debug.WriteLine($" - Attribut: {a.Attributes["Mähnenfarbe"]}");
                }
            }
                catch (Exception ex)
                {
                    ZooMessageBox.Show("Datenbankfehler: " + ex.Message, "Fehler beim Laden");
                }

        }

        private void NavButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag != null)
            {
                string target = button.Tag.ToString();
                
                switch (target)
                {
                    case "Dashboard":
                        MainContentPresenter.Content = new DashboardView();
                        break;
                    case "FeedingPlan":
                        MainContentPresenter.Content = new FeedingView();
                        break;
                    case "Animals":
                        MainContentPresenter.Content = new AnimalsView();
                        break;
                    case "Species":
                        MainContentPresenter.Content = new SpeciesView();
                        break;
                    case "Enclosures":
                        MainContentPresenter.Content = new EnclosuresView();
                        break;
                    case "Employees":
                        MainContentPresenter.Content = new EmployeesView();
                        break;
                    case "Events":
                        MainContentPresenter.Content = new EventsView();
                        break;
                    case "Reports":
                        MainContentPresenter.Content = new ReportsView();
                        break;
                }
            }
        }
    }
}