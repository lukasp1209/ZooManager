using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Tools;
using FlaUI.UIA3;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;

namespace Tests
{
    [TestFixture]
    [NonParallelizable]
    [Apartment(System.Threading.ApartmentState.STA)]
    public class MainWindowTests
    {
        private Application _app = null!;
        private UIA3Automation _automation = null!;

        [SetUp]
        public void Setup()
        {
            var appPath = FindZooManagerExe();
            Assert.That(File.Exists(appPath), Is.True, $"App-Datei nicht gefunden: {appPath}");

            _app = Application.Launch(appPath);
            _automation = new UIA3Automation();
            
            Login("manager", "password");
        }

        [TearDown]
        public void Teardown()
        {
            try { _automation.Dispose(); } catch { /* ignore */ }
            try { _app.Close(); } catch { /* ignore */ }
            try { _app.Dispose(); } catch { /* ignore */ }
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

            // In MainWindow.xaml heißt der Button: x:Name="EventsButton"
            var eventsButton = mainWindow!.FindFirstDescendant(cf => cf.ByAutomationId("EventsButton"))?.AsButton();
            Assert.That(eventsButton, Is.Not.Null, "Button 'EventsButton' wurde nicht gefunden.");

            eventsButton!.Invoke();

            // Minimaler Stabilitäts-Check: UI-Thread hat Zeit zu reagieren
            Retry.WhileFalse(
                () => mainWindow.IsEnabled,
                timeout: TimeSpan.FromSeconds(2)
            );
        }

        private void Login(string username, string password)
        {
            var loginWindow = WaitForWindowByTitleContains("Anmeldung");
            Assert.That(loginWindow, Is.Not.Null, "LoginWindow konnte nicht gefunden werden.");

            var userBox = loginWindow!.FindFirstDescendant(cf => cf.ByAutomationId("UsernameTextBox"))?.AsTextBox();
            Assert.That(userBox, Is.Not.Null, "UsernameTextBox nicht gefunden.");

            var passwordElement = loginWindow.FindFirstDescendant(cf => cf.ByAutomationId("PasswordBox"));
            Assert.That(passwordElement, Is.Not.Null, "PasswordBox nicht gefunden.");

            var loginButton = loginWindow.FindFirstDescendant(cf => cf.ByAutomationId("LoginButton"))?.AsButton();
            Assert.That(loginButton, Is.Not.Null, "LoginButton nicht gefunden.");

            userBox!.Text = username;

            Assert.That(passwordElement!.Patterns.Value.IsSupported, Is.True,
                "PasswordBox unterstützt kein ValuePattern (UIA).");
            passwordElement.Patterns.Value.Pattern.SetValue(password);

            loginButton!.Invoke();

            // warten bis MainWindow da ist (und LoginWindow weg ist)
            var mainWindow = Retry.WhileNull(
                () => _app.GetAllTopLevelWindows(_automation)
                    .FirstOrDefault(w =>
                        w.Title.Contains("ZooManager", StringComparison.OrdinalIgnoreCase) &&
                        !w.Title.Contains("Anmeldung", StringComparison.OrdinalIgnoreCase)),
                timeout: TimeSpan.FromSeconds(10)
            ).Result;

            Assert.That(mainWindow, Is.Not.Null, "MainWindow wurde nach Login nicht geöffnet.");
        }

        private Window? WaitForWindowByTitleContains(string titlePart)
        {
            return Retry.WhileNull(
                () => _app.GetAllTopLevelWindows(_automation)
                    .FirstOrDefault(w => w.Title.Contains(titlePart, StringComparison.OrdinalIgnoreCase)),
                timeout: TimeSpan.FromSeconds(10)
            ).Result;
        }

        private static string FindZooManagerExe()
        {
            var baseDir = TestContext.CurrentContext.TestDirectory;

            var dir = new DirectoryInfo(baseDir);
            for (var i = 0; i < 8 && dir != null; i++, dir = dir.Parent)
            {
                var root = dir.FullName;

                var candidates = new[]
                {
                    Path.Combine(root, "ZooManager", "bin", "Debug", "net8.0-windows", "ZooManager.exe"),
                    Path.Combine(root, "ZooManager", "bin", "Release", "net8.0-windows", "ZooManager.exe"),
                };

                foreach (var c in candidates)
                {
                    if (File.Exists(c))
                        return c;
                }
            }

            throw new FileNotFoundException($"ZooManager.exe nicht gefunden. Startpfad: {baseDir}");
        }
    }
}