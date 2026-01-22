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
        private readonly IPersistenceService _persistenceService;

        public MainWindow(IPersistenceService persistenceService)
        {
            InitializeComponent();
            _persistenceService = persistenceService;
            
            MainContentPresenter.Content = new DashboardView(_persistenceService);
        }

        private void NavButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag != null)
            {
                string target = button.Tag.ToString();
                
                switch (target)
                {
                    case "Dashboard":
                        MainContentPresenter.Content = new DashboardView(_persistenceService);
                        break;
                    case "FeedingPlan":
                        MainContentPresenter.Content = new FeedingView(_persistenceService);
                        break;
                    case "Animals":
                        MainContentPresenter.Content = new AnimalsView(_persistenceService);
                        break;
                    case "Species":
                        MainContentPresenter.Content = new SpeciesView(_persistenceService);
                        break;
                    case "Enclosures":
                        MainContentPresenter.Content = new EnclosuresView(_persistenceService);
                        break;
                    case "Employees":
                        MainContentPresenter.Content = new EmployeesView(_persistenceService);
                        break;
                    case "Events":
                        MainContentPresenter.Content = new EventsView(_persistenceService);
                        break;
                    case "Reports":
                        MainContentPresenter.Content = new ReportsView(_persistenceService);
                        break;
                }
            }
        }
    }
}