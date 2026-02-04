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
}