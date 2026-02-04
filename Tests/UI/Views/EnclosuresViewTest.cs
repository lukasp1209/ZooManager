using System;
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
public class EnclosuresViewTest : UiTestBase
{
    [SetUp]
    public void Setup()
    {
        var exe = FindZooManagerExe();
        App = Application.Launch(exe);
        Automation = new UIA3Automation();

        Login("manager", "password");
        EnsureDashboardIsShown();
        OpenEnclosuresView();
    }

    [TearDown]
    public void TearDown()
    {
        try { Automation.Dispose(); } catch { /* ignore */ }
        try { App.Close(); } catch { /* ignore */ }
        try { App.Dispose(); } catch { /* ignore */ }
    }

    [Test]
    public void EnclosuresView_ShouldRender_CoreControls()
    {
        var main = WaitForWindowByTitleContains("ZooManager");
        Assert.That(main, Is.Not.Null, "MainWindow nicht gefunden.");

        var viewRoot = Retry.WhileNull(
            () => main!.FindFirstDescendant(cf => cf.ByAutomationId("View.Enclosures")),
            timeout: TimeSpan.FromSeconds(8)
        ).Result;
        Assert.That(viewRoot, Is.Not.Null, "EnclosuresView Root (View.Enclosures) nicht gefunden.");

        Assert.That(main!.FindFirstDescendant(cf => cf.ByAutomationId("Enclosures.EnclosureList"))?.AsListBox(), Is.Not.Null);
        Assert.That(main.FindFirstDescendant(cf => cf.ByAutomationId("Enclosures.AddEnclosureBtn"))?.AsButton(), Is.Not.Null);
        Assert.That(main.FindFirstDescendant(cf => cf.ByAutomationId("Enclosures.DeleteEnclosureBtn"))?.AsButton(), Is.Not.Null);

        Assert.That(main.FindFirstDescendant(cf => cf.ByAutomationId("Enclosures.DetailsArea")), Is.Not.Null);
        Assert.That(main.FindFirstDescendant(cf => cf.ByAutomationId("Enclosures.AllAnimalsSelector"))?.AsComboBox(), Is.Not.Null);
        Assert.That(main.FindFirstDescendant(cf => cf.ByAutomationId("Enclosures.AssignAnimalBtn"))?.AsButton(), Is.Not.Null);
    }

    [Test]
    public void AddEnclosure_Click_ShouldOpenEditor_And_Cancel_ShouldClose()
    {
        var main = WaitForWindowByTitleContains("ZooManager");
        Assert.That(main, Is.Not.Null, "MainWindow nicht gefunden.");

        var addBtn = main!.FindFirstDescendant(cf => cf.ByAutomationId("Enclosures.AddEnclosureBtn"))?.AsButton();
        Assert.That(addBtn, Is.Not.Null, "Enclosures.AddEnclosureBtn nicht gefunden.");
        addBtn!.Invoke();

        var nameBox = Retry.WhileNull(
            () => main.FindFirstDescendant(cf => cf.ByAutomationId("Enclosures.Editor.NewEnclosureName"))?.AsTextBox(),
            timeout: TimeSpan.FromSeconds(6)
        ).Result;

        Assert.That(nameBox, Is.Not.Null, "Editor wurde nicht geöffnet (NewEnclosureName nicht gefunden).");
        Assert.That(nameBox!.Properties.IsOffscreen.Value, Is.False);

        var cancelBtn = main.FindFirstDescendant(cf => cf.ByAutomationId("Enclosures.Editor.CancelBtn"))?.AsButton();
        Assert.That(cancelBtn, Is.Not.Null, "Enclosures.Editor.CancelBtn nicht gefunden.");
        cancelBtn!.Invoke();

        var details = Retry.WhileNull(
            () => main.FindFirstDescendant(cf => cf.ByAutomationId("Enclosures.DetailsArea")),
            timeout: TimeSpan.FromSeconds(6)
        ).Result;

        Assert.That(details, Is.Not.Null, "DetailsArea nicht gefunden nach Abbrechen.");
        Assert.That(details!.Properties.IsOffscreen.Value, Is.False);
    }

    private void OpenEnclosuresView()
    {
        var main = WaitForWindowByTitleContains("ZooManager");
        Assert.That(main, Is.Not.Null, "MainWindow nicht gefunden.");

        var button = main!.FindFirstDescendant(cf => cf.ByAutomationId("EnclosuresButton"))?.AsButton();
        Assert.That(button, Is.Not.Null, "Nav-Button EnclosuresButton nicht gefunden.");

        button!.Invoke();

        var viewRoot = Retry.WhileNull(
            () => main.FindFirstDescendant(cf => cf.ByAutomationId("View.Enclosures")),
            timeout: TimeSpan.FromSeconds(8)
        ).Result;

        Assert.That(viewRoot, Is.Not.Null, "EnclosuresView wurde nicht geladen (View.Enclosures).");
    }
    
    [Test]
    public void CreateEnclosure_ShouldAppearInEnclosuresList()
    {
        var main = WaitForWindowByTitleContains("ZooManager");
        Assert.That(main, Is.Not.Null, "MainWindow nicht gefunden.");

        // Editor öffnen
        var addBtn = main!.FindFirstDescendant(cf => cf.ByAutomationId("Enclosures.AddEnclosureBtn"))?.AsButton();
        Assert.That(addBtn, Is.Not.Null, "Enclosures.AddEnclosureBtn nicht gefunden.");
        addBtn!.Invoke();

        var nameBox = Retry.WhileNull(
            () => main.FindFirstDescendant(cf => cf.ByAutomationId("Enclosures.Editor.NewEnclosureName"))?.AsTextBox(),
            timeout: TimeSpan.FromSeconds(6)
        ).Result;
        Assert.That(nameBox, Is.Not.Null, "Editor wurde nicht geöffnet (Enclosures.Editor.NewEnclosureName nicht gefunden).");

        var climateCombo = main.FindFirstDescendant(cf => cf.ByAutomationId("Enclosures.Editor.NewEnclosureClimate"))?.AsComboBox();
        var hasWater = main.FindFirstDescendant(cf => cf.ByAutomationId("Enclosures.Editor.NewEnclosureHasWater"))?.AsCheckBox();
        var areaBox = main.FindFirstDescendant(cf => cf.ByAutomationId("Enclosures.Editor.NewEnclosureArea"))?.AsTextBox();
        var capBox = main.FindFirstDescendant(cf => cf.ByAutomationId("Enclosures.Editor.NewEnclosureCapacity"))?.AsTextBox();
        var saveBtn = main.FindFirstDescendant(cf => cf.ByAutomationId("Enclosures.Editor.SaveBtn"))?.AsButton();

        Assert.That(climateCombo, Is.Not.Null, "Enclosures.Editor.NewEnclosureClimate nicht gefunden.");
        Assert.That(hasWater, Is.Not.Null, "Enclosures.Editor.NewEnclosureHasWater nicht gefunden.");
        Assert.That(areaBox, Is.Not.Null, "Enclosures.Editor.NewEnclosureArea nicht gefunden.");
        Assert.That(capBox, Is.Not.Null, "Enclosures.Editor.NewEnclosureCapacity nicht gefunden.");
        Assert.That(saveBtn, Is.Not.Null, "Enclosures.Editor.SaveBtn nicht gefunden.");

        // Daten füllen (eindeutig)
        var unique = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff");
        var encName = $"UiEnclosure_{unique}";

        nameBox!.Text = encName;

        // Klima auswählen (erstes Item)
        SelectFirstComboItem(climateCombo!);

        // Wasser an (optional)
        if (hasWater!.IsChecked != true)
            hasWater.Click();

        // Zahlenfelder
        areaBox!.Text = "123";
        capBox!.Text = "7";

        // Speichern
        saveBtn!.Invoke();

        // ZooMessageBox schließen (in-app overlay, daher im MainWindow suchen)
        DismissInAppMessageBox(main, timeoutSeconds: 8);

        // Warten bis Editor zu ist (oder zumindest Name-Feld offscreen/weg)
        var editorGone = Retry.WhileFalse(
            () =>
            {
                var editorRoot = main.FindFirstDescendant(cf => cf.ByAutomationId("Enclosures.EditorArea"));
                if (editorRoot == null) return true;
                return editorRoot.Properties.IsOffscreen.Value;
            },
            timeout: TimeSpan.FromSeconds(8)
        ).Result;
        Assert.That(editorGone, Is.True, "Enclosure-Editor ist nach Speichern weiterhin sichtbar (Dialog blockiert evtl.).");

        // Verifizieren: neuer Name taucht in EnclosureList auf (robust über Text)
        var found = Retry.WhileFalse(
            () =>
            {
                var list = main.FindFirstDescendant(cf => cf.ByAutomationId("Enclosures.EnclosureList"))?.AsListBox();
                if (list == null) return false;

                foreach (var item in list.Items)
                {
                    var texts = item.FindAllDescendants(cf => cf.ByControlType(ControlType.Text))
                        .Select(t => t.Name)
                        .Where(n => !string.IsNullOrWhiteSpace(n));
                    var combined = string.Join(" ", texts);

                    if (combined.Contains(encName, StringComparison.OrdinalIgnoreCase))
                        return true;
                }

                return false;
            },
            timeout: TimeSpan.FromSeconds(12)
        ).Result;

        Assert.That(found, Is.True, $"Neues Gehege wurde nicht in der Liste gefunden (Suche nach: {encName}).");
    }

    private static void SelectFirstComboItem(ComboBox combo)
    {
        combo.Expand();
        Retry.WhileFalse(() => combo.Items.Length > 0, timeout: TimeSpan.FromSeconds(3));
        combo.Items[0].Select();
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