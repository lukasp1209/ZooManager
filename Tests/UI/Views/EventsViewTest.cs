using System;
using System.Linq;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Tools;
using FlaUI.UIA3;
using NUnit.Framework;

namespace Tests.UI.Views;

[TestFixture]
[NonParallelizable]
[Apartment(System.Threading.ApartmentState.STA)]
public class EventsViewTest : UiTestBase
{
    [SetUp]
    public void Setup()
    {
        var exe = FindZooManagerExe();
        App = Application.Launch(exe);
        Automation = new UIA3Automation();

        Login("manager", "password");
        EnsureDashboardIsShown();
        OpenEventsView();
    }

    [TearDown]
    public void TearDown()
    {
        try { Automation.Dispose(); } catch { /* ignore */ }
        try { App.Close(); } catch { /* ignore */ }
        try { App.Dispose(); } catch { /* ignore */ }
    }

    [Test]
    public void CreateEvent_ShouldAppearInEventsList()
    {
        var main = WaitForWindowByTitleContains("ZooManager");
        Assert.That(main, Is.Not.Null, "MainWindow nicht gefunden.");

        var addBtn = main!.FindFirstDescendant(cf => cf.ByAutomationId("Events.AddEventBtn"))?.AsButton();
        Assert.That(addBtn, Is.Not.Null, "Events.AddEventBtn nicht gefunden.");
        addBtn!.Invoke();

        var titleBox = Retry.WhileNull(
            () => main.FindFirstDescendant(cf => cf.ByAutomationId("Events.Editor.NewEventTitle"))?.AsTextBox(),
            timeout: TimeSpan.FromSeconds(6)
        ).Result;
        Assert.That(titleBox, Is.Not.Null, "Event-Editor nicht geöffnet (Events.Editor.NewEventTitle nicht gefunden).");

        var datePicker = main.FindFirstDescendant(cf => cf.ByAutomationId("Events.Editor.NewEventDate"));
        var hourCombo = main.FindFirstDescendant(cf => cf.ByAutomationId("Events.Editor.HourSelector"))?.AsComboBox();
        var minuteCombo = main.FindFirstDescendant(cf => cf.ByAutomationId("Events.Editor.MinuteSelector"))?.AsComboBox();
        var descBox = main.FindFirstDescendant(cf => cf.ByAutomationId("Events.Editor.NewEventDescription"))?.AsTextBox();
        var saveBtn = main.FindFirstDescendant(cf => cf.ByAutomationId("Events.Editor.SaveBtn"))?.AsButton();

        Assert.That(datePicker, Is.Not.Null, "Events.Editor.NewEventDate nicht gefunden.");
        Assert.That(hourCombo, Is.Not.Null, "Events.Editor.HourSelector nicht gefunden.");
        Assert.That(minuteCombo, Is.Not.Null, "Events.Editor.MinuteSelector nicht gefunden.");
        Assert.That(descBox, Is.Not.Null, "Events.Editor.NewEventDescription nicht gefunden.");
        Assert.That(saveBtn, Is.Not.Null, "Events.Editor.SaveBtn nicht gefunden.");

        var unique = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff");
        var eventTitle = $"UiEvent_{unique}";
        titleBox!.Text = eventTitle;
        descBox!.Text = "UI Test Event Description";

        EnsureComboHasSelection(hourCombo!);
        EnsureComboHasSelection(minuteCombo!);

        saveBtn!.Invoke();

        DismissInAppMessageBox(main, timeoutSeconds: 8);

        var editorGone = Retry.WhileFalse(
            () =>
            {
                var editorRoot = main.FindFirstDescendant(cf => cf.ByAutomationId("Events.EditorArea"));
                if (editorRoot == null) return true;
                return editorRoot.Properties.IsOffscreen.Value;
            },
            timeout: TimeSpan.FromSeconds(8)
        ).Result;
        Assert.That(editorGone, Is.True, "Event-Editor ist nach Speichern weiterhin sichtbar (Dialog blockiert evtl.).");

        var found = Retry.WhileFalse(
            () =>
            {
                var list = main.FindFirstDescendant(cf => cf.ByAutomationId("Events.EventsList"))?.AsListBox();
                if (list == null) return false;

                foreach (var item in list.Items)
                {
                    var texts = item.FindAllDescendants(cf => cf.ByControlType(ControlType.Text))
                        .Select(t => t.Name)
                        .Where(n => !string.IsNullOrWhiteSpace(n));
                    var combined = string.Join(" ", texts);

                    if (combined.Contains(eventTitle, StringComparison.OrdinalIgnoreCase))
                        return true;
                }

                return false;
            },
            timeout: TimeSpan.FromSeconds(12)
        ).Result;

        Assert.That(found, Is.True, $"Neues Event wurde nicht in der Liste gefunden (Suche nach: {eventTitle}).");
    }

    private void OpenEventsView()
    {
        var main = WaitForWindowByTitleContains("ZooManager");
        Assert.That(main, Is.Not.Null, "MainWindow nicht gefunden.");

        var button = main!.FindFirstDescendant(cf => cf.ByAutomationId("EventsButton"))?.AsButton();
        Assert.That(button, Is.Not.Null, "Nav-Button EventsButton nicht gefunden.");

        button!.Invoke();

        var viewRoot = Retry.WhileNull(
            () => main.FindFirstDescendant(cf => cf.ByAutomationId("View.Events")),
            timeout: TimeSpan.FromSeconds(8)
        ).Result;

        Assert.That(viewRoot, Is.Not.Null, "EventsView wurde nicht geladen (View.Events).");
    }

    private static void EnsureComboHasSelection(ComboBox combo)
    {
        combo.Expand();
        if (combo.SelectedItem == null && combo.Items.Length > 0)
        {
            combo.Items[0].Select();
        }
        combo.Collapse();
    }

    private static void DismissInAppMessageBox(Window mainWindow, int timeoutSeconds)
    {
        Retry.WhileFalse(
            () =>
            {
                var btn =
                    mainWindow.FindFirstDescendant(cf => cf.ByControlType(ControlType.Button).And(cf.ByName("Verstanden")))?.AsButton()
                    ?? mainWindow.FindFirstDescendant(cf => cf.ByControlType(ControlType.Button).And(cf.ByName("OK")))?.AsButton()
                    ?? mainWindow.FindFirstDescendant(cf => cf.ByControlType(ControlType.Button).And(cf.ByName("Ok")))?.AsButton();

                if (btn == null) return false;

                try { btn.Invoke(); } catch { return false; }
                return true;
            },
            timeout: TimeSpan.FromSeconds(timeoutSeconds)
        );
    }
}