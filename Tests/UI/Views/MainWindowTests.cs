using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Tools;
using FlaUI.UIA3;
using NUnit.Framework;

namespace Tests.UI.Views
{
    [TestFixture]
    [NonParallelizable]
    [Apartment(System.Threading.ApartmentState.STA)]
    public class MainWindowTests : UiTestBase
    {
        [SetUp]
        public void Setup()
        {
            var exe = FindZooManagerExe();
            App = Application.Launch(exe);
            Automation = new UIA3Automation();

            Login("manager", "password");
            EnsureDashboardIsShown();
        }

        [TearDown]
        public void Teardown()
        {
            try { Automation.Dispose(); } catch { /* ignore */ }

            try { App.Close(); } catch { /* ignore */ }
            try { App.Dispose(); } catch { /* ignore */ }
        }

        [Test]
        public void MainWindow_ShouldOpen_AndDisplayTitle()
        {
            var mainWindow = WaitForWindowByTitleContains("ZooManager");
            Assert.That(mainWindow, Is.Not.Null, "MainWindow konnte nicht gefunden werden.");

            Assert.That(mainWindow!.Title, Does.Contain("ZooManager"));
        }

        [Test]
        public void ClickEventsButton_ShouldNotFail()
        {
            var mainWindow = WaitForWindowByTitleContains("ZooManager");
            Assert.That(mainWindow, Is.Not.Null, "MainWindow konnte nicht gefunden werden.");

            var eventsButton = mainWindow!.FindFirstDescendant(cf => cf.ByAutomationId("EventsButton"))?.AsButton();
            Assert.That(eventsButton, Is.Not.Null, "Button 'EventsButton' wurde nicht gefunden.");

            eventsButton!.Invoke();

            Retry.WhileFalse(
                () => mainWindow.IsEnabled,
                timeout: TimeSpan.FromSeconds(2)
            );
        }
    }
}