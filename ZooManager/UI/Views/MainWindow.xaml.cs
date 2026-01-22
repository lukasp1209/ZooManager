using System;
using System.Windows;
using System.Windows.Controls;
using ZooManager.Core.Interfaces;
using ZooManager.Core.Models;
using ZooManager.Infrastructure.Persistence;

namespace ZooManager.UI.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow(IPersistenceService persistenceService)
        {
            InitializeComponent();
            _persistenceService = persistenceService;
            
            MainContentPresenter.Content = new DashboardView(_persistenceService);
        }
        
        private readonly IPersistenceService _persistenceService;

        private void NavButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag != null)
            {
                string target = button.Tag.ToString();
                
                switch (target)
                {
                    case "Dashboard":
                        MainContentPresenter.Content = new DashboardView(persistenceService: new SqlitePersistenceService());
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