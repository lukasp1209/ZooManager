using System;
using System.IO;
using System.Linq;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Tools;
using FlaUI.UIA3;
using NUnit.Framework;
using Tests.UI;

namespace Tests.UI.Views;

[TestFixture]
[NonParallelizable]
[Apartment(System.Threading.ApartmentState.STA)]
public class DashboardViewTest : UiTestBase
{
    private Application _app = null!;
    private UIA3Automation _automation = null!;

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
    public void TearDown()
    {
        try { Automation.Dispose(); } catch { /* ignore */ }
        try { App.Close(); } catch { /* ignore */ }
        try { App.Dispose(); } catch { /* ignore */ }
    }

    [Test]
    public void Dashboard_ShouldOpen_And_NavigateToFeedingAndEvents()
    {
        var main = WaitForWindowByTitleContains("ZooManager");
        Assert.That(main, Is.Not.Null, "MainWindow nicht gefunden.");

        var dashboardBtn = main!.FindFirstDescendant(cf => cf.ByAutomationId("DashboardButton"))?.AsButton();
        Assert.That(dashboardBtn, Is.Not.Null, "DashboardButton nicht gefunden.");
        dashboardBtn!.Invoke();

        var dashboardRoot = Retry.WhileNull(
            () => main.FindFirstDescendant(cf => cf.ByAutomationId("View.Dashboard")),
            timeout: TimeSpan.FromSeconds(8)
        ).Result;
        Assert.That(dashboardRoot, Is.Not.Null, "Dashboard View Root nicht gefunden (View.Dashboard).");

        Assert.That(main.FindFirstDescendant(cf => cf.ByAutomationId("TotalAnimalsText")), Is.Not.Null);
        Assert.That(main.FindFirstDescendant(cf => cf.ByAutomationId("TotalEnclosuresText")), Is.Not.Null);
        Assert.That(main.FindFirstDescendant(cf => cf.ByAutomationId("TotalEmployeesText")), Is.Not.Null);

        var openFeeding = main.FindFirstDescendant(cf => cf.ByAutomationId("Dashboard.OpenFeedingPlanBtn"))?.AsButton();
        Assert.That(openFeeding, Is.Not.Null, "Dashboard.OpenFeedingPlanBtn nicht gefunden.");
        openFeeding!.Invoke();

        var feedingRoot = Retry.WhileNull(
            () => main.FindFirstDescendant(cf => cf.ByAutomationId("View.Feeding")),
            timeout: TimeSpan.FromSeconds(8)
        ).Result;
        Assert.That(feedingRoot, Is.Not.Null, "FeedingView wurde nicht geladen (View.Feeding).");

        dashboardBtn.Invoke();

        dashboardRoot = Retry.WhileNull(
            () => main.FindFirstDescendant(cf => cf.ByAutomationId("View.Dashboard")),
            timeout: TimeSpan.FromSeconds(8)
        ).Result;
        Assert.That(dashboardRoot, Is.Not.Null);

        var openEvents = main.FindFirstDescendant(cf => cf.ByAutomationId("Dashboard.OpenEventsBtn"))?.AsButton();
        Assert.That(openEvents, Is.Not.Null, "Dashboard.OpenEventsBtn nicht gefunden.");
        openEvents!.Invoke();

        var eventsRoot = Retry.WhileNull(
            () => main.FindFirstDescendant(cf => cf.ByAutomationId("View.Events")),
            timeout: TimeSpan.FromSeconds(8)
        ).Result;
        Assert.That(eventsRoot, Is.Not.Null, "EventsView wurde nicht geladen (View.Events).");
    }
}