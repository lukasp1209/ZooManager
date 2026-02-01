using System;
using System.IO;
using System.Linq;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Tools;
using FlaUI.UIA3;
using NUnit.Framework;

namespace Tests;

[TestFixture]
[NonParallelizable] 
[Apartment(System.Threading.ApartmentState.STA)]
public class LoginWIndowTest
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
    }

    [TearDown]
    public void Teardown()
    {
        try { _automation.Dispose(); } catch { /* ignore */ }
        try { _app.Close(); } catch { /* ignore */ }
        try { _app.Dispose(); } catch { /* ignore */ }
    }

    [Test]
    public void Login_WithEmptyFields_ShowsErrorMessage()
    {
        var loginWindow = WaitForWindowByTitleContains("Anmeldung");
        Assert.That(loginWindow, Is.Not.Null);

        var loginButton = loginWindow.FindFirstDescendant(cf => cf.ByAutomationId("LoginButton"))?.AsButton();
        Assert.That(loginButton, Is.Not.Null, "LoginButton nicht gefunden.");

        loginButton.Click();

        var error = Retry.WhileNull(
            () => loginWindow.FindFirstDescendant(cf => cf.ByAutomationId("ErrorMessage")),
            timeout: TimeSpan.FromSeconds(3)
        ).Result;

        Assert.That(error, Is.Not.Null, "ErrorMessage nicht gefunden.");
        
        var errorText = error.AsLabel()?.Text ?? error.Name;
        Assert.That(errorText, Does.Contain("Bitte geben Sie Benutzername und Passwort ein."));
        
        Assert.That(error.Properties.IsOffscreen.Value, Is.False);
    }

    [Test]
    public void Login_WithValidCredentials_OpensMainWindow()
    {
        var loginWindow = WaitForWindowByTitleContains("Anmeldung");
        Assert.That(loginWindow, Is.Not.Null);

        var userBox = loginWindow.FindFirstDescendant(cf => cf.ByAutomationId("UsernameTextBox"))?.AsTextBox();
        Assert.That(userBox, Is.Not.Null, "UsernameTextBox nicht gefunden.");

        var passwordElement = loginWindow.FindFirstDescendant(cf => cf.ByAutomationId("PasswordBox"));
        Assert.That(passwordElement, Is.Not.Null, "PasswordBox nicht gefunden.");

        var loginButton = loginWindow.FindFirstDescendant(cf => cf.ByAutomationId("LoginButton"))?.AsButton();
        Assert.That(loginButton, Is.Not.Null, "LoginButton nicht gefunden.");

        userBox.Text = "manager";
        
        Assert.That(passwordElement.Patterns.Value.IsSupported, Is.True, "PasswordBox unterstützt kein ValuePattern.");
        passwordElement.Patterns.Value.Pattern.SetValue("password");

        loginButton.Click();
        
        var mainWindow = Retry.WhileNull(
            () => _app.GetAllTopLevelWindows(_automation)
                .FirstOrDefault(w => w.Title.Contains("Zoo", StringComparison.OrdinalIgnoreCase)
                                     && !w.Title.Contains("Anmeldung", StringComparison.OrdinalIgnoreCase)),
            timeout: TimeSpan.FromSeconds(8)
        ).Result;

        Assert.That(mainWindow, Is.Not.Null, "MainWindow wurde nach Login nicht geöffnet.");
    }

    private Window? WaitForWindowByTitleContains(string titlePart)
    {
        return Retry.WhileNull(
            () => _app.GetAllTopLevelWindows(_automation)
                .FirstOrDefault(w => w.Title.Contains(titlePart, StringComparison.OrdinalIgnoreCase)),
            timeout: TimeSpan.FromSeconds(8)
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