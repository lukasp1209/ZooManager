using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using ZooManager.Core.Interfaces;
using ZooManager.Core.Models;
using ZooManager.Infrastructure.Persistence;
using ZooManager.Infrastructure.Configuration;

namespace ZooManager.UI.Views
{
    public partial class ReportsView : UserControl
    {
        private SqlitePersistenceService _db;

        public ReportsView(IPersistenceService persistenceService = null)
        {
            InitializeComponent();
            _db = persistenceService as SqlitePersistenceService ?? 
                  new SqlitePersistenceService(DatabaseConfig.GetConnectionString());
        }

        private void ReportTypeList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ReportTypeList.SelectedItem is ListBoxItem selectedItem)
            {
                // Da wir jetzt ein StackPanel mit TextBlocks haben, müssen wir den ersten TextBlock finden
                var stackPanel = selectedItem.Content as StackPanel;
                var firstTextBlock = stackPanel?.Children.OfType<TextBlock>().FirstOrDefault();
                string reportName = firstTextBlock?.Text;
                    
                if (string.IsNullOrEmpty(reportName)) return;

                ReportTitle.Text = reportName;

                switch (reportName)
                {
                    case "Bestand nach Tierart":
                        ShowSpeciesReport();
                        break;
                    case "Gehege-Auslastung":
                        ShowEnclosureReport();
                        break;
                    case "Personal-Qualifikationen":
                        ShowEmployeeReport();
                        break;
                }
            }
        }

        private void ShowSpeciesReport()
        {
            var animals = _db.LoadAnimals().ToList();
            var species = _db.LoadSpecies().ToList();

            var data = species.Select(s => new {
                Tierart = s.Name,
                Anzahl = animals.Count(a => a.SpeciesId == s.Id)
            }).OrderByDescending(x => x.Anzahl).ToList();

            ReportDataGrid.ItemsSource = data;

            // Diagramm-Logik: Top 5 Arten visualisieren
            double max = data.Any() ? data.Max(d => d.Anzahl) : 1;
            var chartData = data.Take(5).Select(d => new ChartDataPoint {
                Label = d.Tierart,
                Value = d.Anzahl,
                BarHeight = (d.Anzahl / max) * 120 // Max Höhe 120px
            }).ToList();
            
            ChartItemsControl.ItemsSource = chartData;
        }

        private void ShowEnclosureReport()
        {
            var animals = _db.LoadAnimals().ToList();
            var enclosures = _db.LoadEnclosures().ToList();

            var data = enclosures.Select(e => new {
                Gehege = e.Name,
                Auslastung = (double)animals.Count(a => a.EnclosureId == e.Id) / e.MaxCapacity
            }).ToList();

            ReportDataGrid.ItemsSource = data.Select(d => new { 
                d.Gehege, 
                Status = $"{(d.Auslastung*100):N0}% voll" 
            }).ToList();

            // Diagramm: Auslastung visualisieren (0.0 bis 1.0)
            var chartData = data.Select(d => new ChartDataPoint {
                Label = d.Gehege,
                Value = Math.Round(d.Auslastung * 100, 1),
                BarHeight = d.Auslastung * 120
            }).ToList();

            ChartItemsControl.ItemsSource = chartData;
        }

        private void ShowEmployeeReport()
        {
            var employees = _db.LoadEmployees().ToList();
            var species = _db.LoadSpecies().ToList();

            var data = species.Select(s => new {
                Art = s.Name,
                Experten = employees.Count(e => e.QualifiedSpeciesIds.Contains(s.Id))
            }).ToList();

            ReportDataGrid.ItemsSource = data;

            // Analyse: Wo fehlen Experten am dringendsten?
            double max = data.Any() ? data.Max(d => d.Experten) : 1;
            ChartItemsControl.ItemsSource = data.Select(d => new ChartDataPoint {
                Label = d.Art,
                Value = d.Experten,
                BarHeight = (d.Experten / (max == 0 ? 1 : max)) * 120
            }).ToList();
        }
    }
}