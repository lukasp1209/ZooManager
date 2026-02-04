using System;
using System.IO;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Tools;
using FlaUI.UIA3;
using NUnit.Framework;

namespace Tests.UI.Views;

[TestFixture]
[NonParallelizable]
[Apartment(System.Threading.ApartmentState.STA)]
public class LoginWindowTest : UiTestBase
{
    [SetUp]
    public void Setup()
    {
        var appPath = FindZooManagerExe();
        Assert.That(File.Exists(appPath), Is.True, $"App-Datei nicht gefunden: {appPath}");

        App = Application.Launch(appPath);
        Automation = new UIA3Automation();
    }

    [TearDown]
    public void Teardown()
    {
        try { Automation.Dispose(); } catch { /* ignore */ }
        try { App.Close(); } catch { /* ignore */ }
        try { App.Dispose(); } catch { /* ignore */ }
    }

    [Test]
    public void Login_WithEmptyFields_ShowsErrorMessage()
    {
        var loginWindow = WaitForWindowByTitleContains("Anmeldung");
        Assert.That(loginWindow, Is.Not.Null);

        var loginButton = loginWindow!.FindFirstDescendant(cf => cf.ByAutomationId("LoginButton"))?.AsButton();
        Assert.That(loginButton, Is.Not.Null, "LoginButton nicht gefunden.");

        loginButton!.Click();

        var error = Retry.WhileNull(
            () => loginWindow.FindFirstDescendant(cf => cf.ByAutomationId("ErrorMessage")),
            timeout: TimeSpan.FromSeconds(3)
        ).Result;

        Assert.That(error, Is.Not.Null, "ErrorMessage nicht gefunden.");

        var errorText = error!.AsLabel()?.Text ?? error.Name;
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
            () => App.GetAllTopLevelWindows(Automation)
                .FirstOrDefault(w => w.Title.Contains("Zoo", StringComparison.OrdinalIgnoreCase)
                                     && !w.Title.Contains("Anmeldung", StringComparison.OrdinalIgnoreCase)),
            timeout: TimeSpan.FromSeconds(8)
        ).Result;

        Assert.That(mainWindow, Is.Not.Null, "MainWindow wurde nach Login nicht geöffnet.");
    }
}