using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using NUnit.Framework;

namespace ZooManager.Tests
{
    [TestFixture]
    public class MainWindowTests
    {
        private Application _app;
        private UIA3Automation _automation;

        [SetUp]
        public void Setup()
        {
            // Pfad zur deiner ZooManager.exe (im bin-Ordner)
            // Passe den Pfad ggf. an deine Struktur an
            var appPath = @"..\..\..\ZooManager\bin\Debug\net8.0-windows\ZooManager.exe";
            _app = Application.Launch(appPath);
            _automation = new UIA3Automation();
        }

        [TearDown]
        public void Teardown()
        {
            _automation?.Dispose();
            _app?.Close();
        }

        [Test]
        public void MainWindow_ShouldOpen_AndDisplayTitle()
        {
            // Das Hauptfenster abrufen
            var window = _app.GetMainWindow(_automation);
            
            Assert.That(window, Is.Not.Null);
            // Prüfen, ob der Titel korrekt ist (Beispiel)
            Assert.That(window.Title, Contains.Substring("Zoo Manager"));
        }

        [Test]
        public void ClickButton_ShouldNavigateToEvents()
        {
             var window = _app.GetMainWindow(_automation);
            
            // Suche ein Element per AutomationId (in XAML: AutomationProperties.AutomationId="BtnEvents")
            var eventButton = window.FindFirstDescendant(cf => cf.ByAutomationId("BtnEvents"))?.AsButton();
            
            Assert.That(eventButton, Is.Not.Null, "Button 'BtnEvents' wurde nicht gefunden.");
            eventButton.Click();

            // Prüfen, ob eine View geladen wurde
            var eventsView = window.FindFirstDescendant(cf => cf.ByClassName("EventsView"));
            Assert.That(eventsView, Is.Not.Null);
        }
    }
}