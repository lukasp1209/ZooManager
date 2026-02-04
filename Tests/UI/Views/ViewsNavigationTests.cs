using System;
using System.Linq;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Tools;
using FlaUI.UIA3;
using NUnit.Framework;

namespace Tests.UI.Views;

[TestFixture]
[NonParallelizable]
[Apartment(System.Threading.ApartmentState.STA)]
public class ViewsNavigationTests : UiTestBase
{
    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        var exe = FindZooManagerExe();
        App = Application.Launch(exe);
        Automation = new UIA3Automation();

        Login("manager", "password");
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        try { Automation.Dispose(); } catch { /* ignore */ }
        try { App.Close(); } catch { /* ignore */ }
        try { App.Dispose(); } catch { /* ignore */ }
    }

    [SetUp]
    public void BeforeEach()
    {
        EnsureDashboardIsShown();
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
}