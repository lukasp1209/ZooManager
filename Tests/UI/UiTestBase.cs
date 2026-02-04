using System;
using System.IO;
using System.Linq;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Tools;
using FlaUI.UIA3;
using NUnit.Framework;

namespace Tests.UI;

public abstract class UiTestBase
{
    protected Application App = null!;
    protected UIA3Automation Automation = null!;

    protected Window? WaitForWindowByTitleContains(string titlePart, int timeoutSeconds = 10)
    {
        return Retry.WhileNull(
            () => App.GetAllTopLevelWindows(Automation)
                .FirstOrDefault(w => w.Title.Contains(titlePart, StringComparison.OrdinalIgnoreCase)),
            timeout: TimeSpan.FromSeconds(timeoutSeconds)
        ).Result;
    }

    protected void EnsureDashboardIsShown()
    {
        var main = WaitForWindowByTitleContains("ZooManager");
        Assert.That(main, Is.Not.Null, "MainWindow nicht gefunden.");

        var dashboardBtn = Retry.WhileNull(
            () => main!.FindFirstDescendant(cf => cf.ByAutomationId("DashboardButton"))?.AsButton(),
            timeout: TimeSpan.FromSeconds(6)
        ).Result;
        Assert.That(dashboardBtn, Is.Not.Null, "DashboardButton nicht gefunden.");

        dashboardBtn!.Invoke();

        var dashboardRoot = Retry.WhileNull(
            () => main!.FindFirstDescendant(cf => cf.ByAutomationId("View.Dashboard")),
            timeout: TimeSpan.FromSeconds(8)
        ).Result;

        Assert.That(dashboardRoot, Is.Not.Null, "Dashboard View Root nicht gefunden (View.Dashboard).");
        Assert.That(dashboardRoot!.Properties.IsOffscreen.Value, Is.False, "Dashboard ist offscreen/unsichtbar.");
    }

    protected static string FindZooManagerExe()
    {
        var baseDir = TestContext.CurrentContext.TestDirectory;
        var dir = new DirectoryInfo(baseDir);

        for (var i = 0; i < 8 && dir != null; i++, dir = dir.Parent)
        {
            var root = dir.FullName;

            var debug = Path.Combine(root, "ZooManager", "bin", "Debug", "net8.0-windows", "ZooManager.exe");
            if (File.Exists(debug)) return debug;

            var release = Path.Combine(root, "ZooManager", "bin", "Release", "net8.0-windows", "ZooManager.exe");
            if (File.Exists(release)) return release;
        }

        throw new FileNotFoundException($"ZooManager.exe nicht gefunden. Startpfad: {baseDir}");
    }

    protected void Login(string username, string password)
    {
        var login = WaitForWindowByTitleContains("Anmeldung");
        Assert.That(login, Is.Not.Null, "LoginWindow nicht gefunden.");

        var userBox = Retry.WhileNull(
            () => login!.FindFirstDescendant(cf => cf.ByAutomationId("UsernameTextBox"))?.AsTextBox(),
            timeout: TimeSpan.FromSeconds(8)
        ).Result;

        var passBox = Retry.WhileNull(
            () => login!.FindFirstDescendant(cf => cf.ByAutomationId("PasswordBox")),
            timeout: TimeSpan.FromSeconds(8)
        ).Result;

        var loginBtn = Retry.WhileNull(
            () => login!.FindFirstDescendant(cf => cf.ByAutomationId("LoginButton"))?.AsButton(),
            timeout: TimeSpan.FromSeconds(8)
        ).Result;

        Assert.That(userBox, Is.Not.Null, "UsernameTextBox nicht gefunden.");
        Assert.That(passBox, Is.Not.Null, "PasswordBox nicht gefunden.");
        Assert.That(loginBtn, Is.Not.Null, "LoginButton nicht gefunden.");

        userBox!.Text = username;

        Assert.That(passBox!.Patterns.Value.IsSupported, Is.True, "PasswordBox unterstützt kein ValuePattern.");
        passBox.Patterns.Value.Pattern.SetValue(password);

        loginBtn!.Invoke();

        var main = Retry.WhileNull(
            () => App.GetAllTopLevelWindows(Automation)
                .FirstOrDefault(w => w.Title.Contains("ZooManager", StringComparison.OrdinalIgnoreCase)
                                     && !w.Title.Contains("Anmeldung", StringComparison.OrdinalIgnoreCase)),
            timeout: TimeSpan.FromSeconds(10)
        ).Result;

        Assert.That(main, Is.Not.Null, "MainWindow wurde nach Login nicht geöffnet.");
    }
}