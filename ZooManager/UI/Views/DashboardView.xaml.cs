using System.Linq;
using System.Windows.Controls;
using ZooManager.Infrastructure.Persistence;
using ZooManager.Infrastructure.Configuration;

namespace ZooManager.UI.Views
{
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            InitializeComponent();
            LoadDashboardStats();
        }

        private void LoadDashboardStats()
        {
            try
            {
                var db = new MySqlPersistenceService(DatabaseConfig.GetConnectionString());

                // Daten laden und Zählen
                int animalCount = db.LoadAnimals().Count();
                int enclosureCount = db.LoadEnclosures().Count();
                int employeeCount = db.LoadEmployees().Count();

                // UI aktualisieren
                TotalAnimalsText.Text = animalCount.ToString();
                TotalEnclosuresText.Text = enclosureCount.ToString();
                TotalEmployeesText.Text = employeeCount.ToString();
            }
            catch (System.Exception ex)
            {
                // Falls die DB nicht erreichbar ist, zeigen wir eine Fehlermeldung
                ZooMessageBox.Show("Fehler beim Laden der Dashboard-Statistiken: " + ex.Message);
            }
        }
    }
}
