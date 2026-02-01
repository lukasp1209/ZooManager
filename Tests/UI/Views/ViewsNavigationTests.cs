using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Tools;
using FlaUI.UIA3;
using NUnit.Framework;

namespace Tests.UI.Views;

[TestFixture]
[NonParallelizable]
[Apartment(System.Threading.ApartmentState.STA)]
public class ViewsNavigationTests
{
    private Application _app = null!;
    private UIA3Automation _automation = null!;

    [SetUp]
    public void Setup()
    {
        var exe = FindZooManagerExe();
        _app = Application.Launch(exe);
        _automation = new UIA3Automation();

        Login("manager", "password");
    }

    [TearDown]
    public void TearDown()
    {
        try { _automation.Dispose(); } catch { /* ignore */ }
        try { _app.Close(); } catch { /* ignore */ }
        try { _app.Dispose(); } catch { /* ignore */ }
    }

    [TestCase("DashboardButton", "View.Dashboard")]
    [TestCase("AnimalsButton", "View.Animals")]
    [TestCase("FeedingPlanButton", "View.Feeding")]
    [TestCase("SpeciesButton", "View.Species")]
    [TestCase("EnclosuresButton", "View.Enclosures")]
    [TestCase("EmployeesButton", "View.Employees")]
    [TestCase("EventsButton", "View.Events")]
    [TestCase("ReportsButton", "View.Reports")]
    public void Navigation_ShouldOpen_View(string navButtonAutomationId, string viewRootAutomationId)
    {
        var main = WaitForWindowByTitleContains("ZooManager");
        Assert.That(main, Is.Not.Null, "MainWindow nicht gefunden.");

        var button = main!.FindFirstDescendant(cf => cf.ByAutomationId(navButtonAutomationId))?.AsButton();
        Assert.That(button, Is.Not.Null, $"Nav-Button nicht gefunden: {navButtonAutomationId}");

        button!.Invoke();

        var viewRoot = Retry.WhileNull(
            () => main.FindFirstDescendant(cf => cf.ByAutomationId(viewRootAutomationId)),
            timeout: TimeSpan.FromSeconds(8)
        ).Result;

        Assert.That(viewRoot, Is.Not.Null, $"View wurde nicht geladen: {viewRootAutomationId}");
        Assert.That(viewRoot!.Properties.IsOffscreen.Value, Is.False);
    }

    private void Login(string username, string password)
    {
        var login = WaitForWindowByTitleContains("Anmeldung");
        Assert.That(login, Is.Not.Null, "LoginWindow nicht gefunden.");

        var userBox = login!.FindFirstDescendant(cf => cf.ByAutomationId("UsernameTextBox"))?.AsTextBox();
        var passBox = login.FindFirstDescendant(cf => cf.ByAutomationId("PasswordBox"));
        var loginBtn = login.FindFirstDescendant(cf => cf.ByAutomationId("LoginButton"))?.AsButton();

        Assert.That(userBox, Is.Not.Null, "UsernameTextBox nicht gefunden.");
        Assert.That(passBox, Is.Not.Null, "PasswordBox nicht gefunden.");
        Assert.That(loginBtn, Is.Not.Null, "LoginButton nicht gefunden.");

        userBox!.Text = username;

        Assert.That(passBox!.Patterns.Value.IsSupported, Is.True, "PasswordBox unterstützt kein ValuePattern.");
        passBox.Patterns.Value.Pattern.SetValue(password);

        loginBtn!.Invoke();

        var main = Retry.WhileNull(
            () => _app.GetAllTopLevelWindows(_automation)
                .FirstOrDefault(w => w.Title.Contains("ZooManager", StringComparison.OrdinalIgnoreCase)
                                     && !w.Title.Contains("Anmeldung", StringComparison.OrdinalIgnoreCase)),
            timeout: TimeSpan.FromSeconds(10)
        ).Result;

        Assert.That(main, Is.Not.Null, "MainWindow wurde nach Login nicht geöffnet.");
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

            var debug = Path.Combine(root, "ZooManager", "bin", "Debug", "net8.0-windows", "ZooManager.exe");
            if (File.Exists(debug)) return debug;

            var release = Path.Combine(root, "ZooManager", "bin", "Release", "net8.0-windows", "ZooManager.exe");
            if (File.Exists(release)) return release;
        }

        throw new FileNotFoundException($"ZooManager.exe nicht gefunden. Startpfad: {baseDir}");
    }
}