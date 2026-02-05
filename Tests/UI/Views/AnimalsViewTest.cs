using System;
using System.IO;
using System.Linq;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Tools;
using FlaUI.UIA3;
using NUnit.Framework;
using Tests.UI;

namespace Tests.UI.Views;

[TestFixture]
[NonParallelizable]
[Apartment(System.Threading.ApartmentState.STA)]
public class AnimalsViewTest : UiTestBase
{
    [SetUp]
    public void Setup()
    {
        var exe = FindZooManagerExe();
        App = Application.Launch(exe);
        Automation = new UIA3Automation();

        Login("manager", "password");
        EnsureDashboardIsShown();
        OpenAnimalsView();
    }

    [TearDown]
    public void TearDown()
    {
        try { Automation.Dispose(); } catch { /* ignore */ }
        try { App.Close(); } catch { /* ignore */ }
        try { App.Dispose(); } catch { /* ignore */ }
    }

    [Test]
    public void AnimalsView_ShouldRender_CoreControls()
    {
        var main = WaitForWindowByTitleContains("ZooManager");
        Assert.That(main, Is.Not.Null, "MainWindow nicht gefunden.");

        var viewRoot = Retry.WhileNull(
            () => main!.FindFirstDescendant(cf => cf.ByAutomationId("View.Animals")),
            timeout: TimeSpan.FromSeconds(8)
        ).Result;
        Assert.That(viewRoot, Is.Not.Null, "AnimalsView Root (View.Animals) nicht gefunden.");

        Assert.That(main!.FindFirstDescendant(cf => cf.ByAutomationId("Animals.Title")), Is.Not.Null, "Animals.Title nicht gefunden.");
        Assert.That(main.FindFirstDescendant(cf => cf.ByAutomationId("Animals.AddAnimalBtn"))?.AsButton(), Is.Not.Null);
        Assert.That(main.FindFirstDescendant(cf => cf.ByAutomationId("Animals.DeleteAnimalBtn"))?.AsButton(), Is.Not.Null);
        Assert.That(main.FindFirstDescendant(cf => cf.ByAutomationId("Animals.AnimalsList"))?.AsListBox(), Is.Not.Null);
        
        Assert.That(main.FindFirstDescendant(cf => cf.ByAutomationId("Animals.DetailsArea")), Is.Not.Null, "Animals.DetailsArea nicht gefunden.");
        Assert.That(main.FindFirstDescendant(cf => cf.ByAutomationId("Animals.EventsList")), Is.Not.Null, "Animals.EventsList nicht gefunden.");
        Assert.That(main.FindFirstDescendant(cf => cf.ByAutomationId("Animals.NewEvent.SaveBtn"))?.AsButton(), Is.Not.Null, "Animals.NewEvent.SaveBtn nicht gefunden.");
    }

    [Test]
    public void AddAnimal_Click_ShouldOpen_Editor_And_Cancel_ShouldClose()
    {
        var main = WaitForWindowByTitleContains("ZooManager");
        Assert.That(main, Is.Not.Null, "MainWindow nicht gefunden.");

        var addBtn = main!.FindFirstDescendant(cf => cf.ByAutomationId("Animals.AddAnimalBtn"))?.AsButton();
        Assert.That(addBtn, Is.Not.Null, "Animals.AddAnimalBtn nicht gefunden.");

        addBtn!.Invoke();

        var newAnimalName = Retry.WhileNull(
            () => main.FindFirstDescendant(cf => cf.ByAutomationId("Animals.Editor.NewAnimalName"))?.AsTextBox(),
            timeout: TimeSpan.FromSeconds(6)
        ).Result;

        Assert.That(newAnimalName, Is.Not.Null, "Editor wurde nicht geöffnet (Animals.Editor.NewAnimalName nicht gefunden).");
        Assert.That(newAnimalName!.Properties.IsOffscreen.Value, Is.False, "Editor-Feld ist offscreen/unsichtbar.");

        Assert.That(main.FindFirstDescendant(cf => cf.ByAutomationId("Animals.Editor.SpeciesSelector"))?.AsComboBox(), Is.Not.Null);
        Assert.That(main.FindFirstDescendant(cf => cf.ByAutomationId("Animals.Editor.EnclosureSelector"))?.AsComboBox(), Is.Not.Null);

        var cancelBtn = main.FindFirstDescendant(cf => cf.ByAutomationId("Animals.Editor.CancelBtn"))?.AsButton();
        Assert.That(cancelBtn, Is.Not.Null, "Animals.Editor.CancelBtn nicht gefunden.");

        cancelBtn!.Invoke();

        var detailsArea = Retry.WhileNull(
            () => main.FindFirstDescendant(cf => cf.ByAutomationId("Animals.DetailsArea")),
            timeout: TimeSpan.FromSeconds(6)
        ).Result;

        Assert.That(detailsArea, Is.Not.Null, "Animals.DetailsArea nicht gefunden.");
        Assert.That(detailsArea!.Properties.IsOffscreen.Value, Is.False, "DetailsArea ist offscreen/unsichtbar nach Abbrechen.");
    }

    [Test]
    public void NewEventCard_ShouldContain_Inputs_And_SaveButton()
    {
        var main = WaitForWindowByTitleContains("ZooManager");
        Assert.That(main, Is.Not.Null, "MainWindow nicht gefunden.");

        var date = Retry.WhileNull(
            () => main!.FindFirstDescendant(cf => cf.ByAutomationId("Animals.NewEvent.Date")),
            timeout: TimeSpan.FromSeconds(6)
        ).Result;
        Assert.That(date, Is.Not.Null, "Animals.NewEvent.Date nicht gefunden.");

        var type = main!.FindFirstDescendant(cf => cf.ByAutomationId("Animals.NewEvent.Type"))?.AsTextBox();
        Assert.That(type, Is.Not.Null, "Animals.NewEvent.Type nicht gefunden.");

        var desc = main.FindFirstDescendant(cf => cf.ByAutomationId("Animals.NewEvent.Description"))?.AsTextBox();
        Assert.That(desc, Is.Not.Null, "Animals.NewEvent.Description nicht gefunden.");

        var save = main.FindFirstDescendant(cf => cf.ByAutomationId("Animals.NewEvent.SaveBtn"))?.AsButton();
        Assert.That(save, Is.Not.Null, "Animals.NewEvent.SaveBtn nicht gefunden.");
    }

    private void OpenAnimalsView()
    {
        var main = WaitForWindowByTitleContains("ZooManager");
        Assert.That(main, Is.Not.Null, "MainWindow nicht gefunden.");

        var button = main!.FindFirstDescendant(cf => cf.ByAutomationId("AnimalsButton"))?.AsButton();
        Assert.That(button, Is.Not.Null, "Nav-Button AnimalsButton nicht gefunden.");

        button!.Invoke();

        var viewRoot = Retry.WhileNull(
            () => main.FindFirstDescendant(cf => cf.ByAutomationId("View.Animals")),
            timeout: TimeSpan.FromSeconds(8)
        ).Result;

        Assert.That(viewRoot, Is.Not.Null, "AnimalsView wurde nicht geladen (View.Animals).");
    }
    
     [Test]
    public void CreateAnimal_ShouldAppearInAnimalsList()
    {
        var main = WaitForWindowByTitleContains("ZooManager");
        Assert.That(main, Is.Not.Null, "MainWindow nicht gefunden.");

        // Editor öffnen
        var addBtn = main!.FindFirstDescendant(cf => cf.ByAutomationId("Animals.AddAnimalBtn"))?.AsButton();
        Assert.That(addBtn, Is.Not.Null, "Animals.AddAnimalBtn nicht gefunden.");
        addBtn!.Invoke();

        var nameBox = Retry.WhileNull(
            () => main.FindFirstDescendant(cf => cf.ByAutomationId("Animals.Editor.NewAnimalName"))?.AsTextBox(),
            timeout: TimeSpan.FromSeconds(6)
        ).Result;
        Assert.That(nameBox, Is.Not.Null, "Editor wurde nicht geöffnet (Animals.Editor.NewAnimalName nicht gefunden).");

        var speciesCombo = main.FindFirstDescendant(cf => cf.ByAutomationId("Animals.Editor.SpeciesSelector"))?.AsComboBox();
        var enclosureCombo = main.FindFirstDescendant(cf => cf.ByAutomationId("Animals.Editor.EnclosureSelector"))?.AsComboBox();
        var feedingDate = main.FindFirstDescendant(cf => cf.ByAutomationId("Animals.Editor.FeedingDate"));
        var hourCombo = main.FindFirstDescendant(cf => cf.ByAutomationId("Animals.Editor.FeedingHour"))?.AsComboBox();
        var minuteCombo = main.FindFirstDescendant(cf => cf.ByAutomationId("Animals.Editor.FeedingMinute"))?.AsComboBox();
        var saveBtn = main.FindFirstDescendant(cf => cf.ByAutomationId("Animals.Editor.SaveBtn"))?.AsButton();

        Assert.That(speciesCombo, Is.Not.Null, "Animals.Editor.SpeciesSelector nicht gefunden.");
        Assert.That(enclosureCombo, Is.Not.Null, "Animals.Editor.EnclosureSelector nicht gefunden.");
        Assert.That(feedingDate, Is.Not.Null, "Animals.Editor.FeedingDate nicht gefunden.");
        Assert.That(hourCombo, Is.Not.Null, "Animals.Editor.FeedingHour nicht gefunden.");
        Assert.That(minuteCombo, Is.Not.Null, "Animals.Editor.FeedingMinute nicht gefunden.");
        Assert.That(saveBtn, Is.Not.Null, "Animals.Editor.SaveBtn nicht gefunden.");

        // Eindeutiger Name
        var unique = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff");
        var animalName = $"UiAnimal_{unique}";
        nameBox!.Text = animalName;

        // Species auswählen (erstes Item)
        SelectFirstComboItem(speciesCombo!);

        // Enclosure optional: wenn Items vorhanden, erstes nehmen (sonst null ok)
        TrySelectFirstComboItem(enclosureCombo!);

        // Fütterungszeit: wir lassen Defaults stehen (View setzt Defaultwerte).
        // Optional könnte man Hour/Minute selektieren:
        TrySelectComboItemByText(hourCombo!, DateTime.Now.Hour.ToString("D2"));
        TrySelectComboItemByText(minuteCombo!, "00"); // oder "05"/"10" etc., je nachdem was es gibt

        // Speichern
        saveBtn!.Invoke();

        // In-App ZooMessageBox schließen (meist Button "Verstanden")
        DismissInAppMessageBox(main, timeoutSeconds: 8);

        // Warten bis Editor weg ist
        var editorGone = Retry.WhileFalse(
            () =>
            {
                var editorRoot = main.FindFirstDescendant(cf => cf.ByAutomationId("Animals.EditorArea"));
                if (editorRoot == null) return true;
                return editorRoot.Properties.IsOffscreen.Value;
            },
            timeout: TimeSpan.FromSeconds(8)
        ).Result;
        Assert.That(editorGone, Is.True, "Editor ist nach Speichern weiterhin sichtbar (Dialog blockiert evtl.).");

        // Verifizieren: neues Tier in AnimalsList sichtbar (über Text nodes im Item)
        var found = Retry.WhileFalse(
            () =>
            {
                var list = main.FindFirstDescendant(cf => cf.ByAutomationId("Animals.AnimalsList"))?.AsListBox();
                if (list == null) return false;

                foreach (var item in list.Items)
                {
                    var texts = item.FindAllDescendants(cf => cf.ByControlType(ControlType.Text))
                        .Select(t => t.Name)
                        .Where(n => !string.IsNullOrWhiteSpace(n));
                    var combined = string.Join(" ", texts);

                    if (combined.Contains(animalName, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
                return false;
            },
            timeout: TimeSpan.FromSeconds(12)
        ).Result;

        Assert.That(found, Is.True, $"Neues Tier wurde nicht in der Liste gefunden (Suche nach: {animalName}).");
    }

    private static void SelectFirstComboItem(ComboBox combo)
    {
        combo.Expand();
        Retry.WhileFalse(() => combo.Items.Length > 0, timeout: TimeSpan.FromSeconds(3));
        combo.Items[0].Select();
        combo.Collapse();
    }

    private static void TrySelectFirstComboItem(ComboBox combo)
    {
        combo.Expand();
        if (combo.Items.Length > 0)
        {
            combo.Items[0].Select();
        }
        combo.Collapse();
    }

    private static void TrySelectComboItemByText(ComboBox combo, string text)
    {
        combo.Expand();
        var item = combo.Items.FirstOrDefault(i => string.Equals(i.Name, text, StringComparison.OrdinalIgnoreCase));
        if (item != null)
        {
            item.Select();
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