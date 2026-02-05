using System;
using System.Windows;
using System.Windows.Controls;
using ZooManager.Core.Interfaces;
using ZooManager.UI.ViewModels;

namespace ZooManager.UI.Views
{
    public partial class DashboardView : UserControl
    {
        public DashboardView(IPersistenceService persistenceService, IAuthenticationService authService)
        {
            InitializeComponent();

            Action openFeedingPlan = () =>
            {
                if (Window.GetWindow(this) is not MainWindow mainWindow)
                    return;

                if (mainWindow.DataContext is not MainWindowViewModel shellVm)
                    return;

                shellVm.ShowFeedingPlanCommand.Execute(null);
            };

            Action openEvents = () =>
            {
                if (Window.GetWindow(this) is not MainWindow mainWindow)
                    return;

                if (mainWindow.DataContext is not MainWindowViewModel shellVm)
                    return;

                shellVm.ShowEventsCommand.Execute(null);
            };

            DataContext = new DashboardViewModel(
                persistenceService,
                authService,
                openFeedingPlan,
                openEvents);
        }
    }
}