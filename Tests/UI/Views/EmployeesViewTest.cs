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
public class EmployeesViewTest : UiTestBase
{
    [SetUp]
    public void Setup()
    {
        var exe = FindZooManagerExe();
        App = Application.Launch(exe);
        Automation = new UIA3Automation();

        Login("manager", "password");
        EnsureDashboardIsShown();
        OpenEmployeesView();
    }

    [TearDown]
    public void TearDown()
    {
        try
        {
            var main = TryGetMainWindow();
            if (main != null)
            {
                CloseAllNonMainWindows(main, timeoutSeconds: 2);
            }
        }
        catch
        {
            /* ignore */
        }

        try
        {
            Automation.Dispose();
        }
        catch
        {
            /* ignore */
        }

        try
        {
            App.Close();

            Retry.WhileFalse(() => App.HasExited, timeout: TimeSpan.FromSeconds(2));
        }
        catch
        {
            try
            {
                App.Kill();
            }
            catch
            {
                /* ignore */
            }
        }

        try
        {
            App.Dispose();
        }
        catch
        {
            /* ignore */
        }
    }

    [Test]
    public void CreateEmployee_WithLogin_ShouldAppearInEmployeesList()
    {
        var main = WaitForWindowByTitleContains("ZooManager");
        Assert.That(main, Is.Not.Null, "MainWindow nicht gefunden.");

        var addBtn = main!.FindFirstDescendant(cf => cf.ByAutomationId("Employees.AddEmployeeBtn"))?.AsButton();
        Assert.That(addBtn, Is.Not.Null, "Employees.AddEmployeeBtn nicht gefunden.");
        addBtn!.Invoke();

        var firstNameBox = Retry.WhileNull(
            () => main.FindFirstDescendant(cf => cf.ByAutomationId("Employees.Editor.NewEmpFirstName"))?.AsTextBox(),
            timeout: TimeSpan.FromSeconds(6)
        ).Result;
        Assert.That(firstNameBox, Is.Not.Null, "Editor nicht geöffnet (NewEmpFirstName fehlt).");

        var lastNameBox = main.FindFirstDescendant(cf => cf.ByAutomationId("Employees.Editor.NewEmpLastName"))
            ?.AsTextBox();
        var usernameBox = main.FindFirstDescendant(cf => cf.ByAutomationId("Employees.Editor.NewEmpUsername"))
            ?.AsTextBox();
        var passwordEl = main.FindFirstDescendant(cf => cf.ByAutomationId("Employees.Editor.NewEmpPassword"));
        var saveBtn = main.FindFirstDescendant(cf => cf.ByAutomationId("Employees.Editor.SaveBtn"))?.AsButton();

        Assert.That(lastNameBox, Is.Not.Null, "Employees.Editor.NewEmpLastName nicht gefunden.");
        Assert.That(usernameBox, Is.Not.Null, "Employees.Editor.NewEmpUsername nicht gefunden.");
        Assert.That(passwordEl, Is.Not.Null, "Employees.Editor.NewEmpPassword nicht gefunden.");
        Assert.That(saveBtn, Is.Not.Null, "Employees.Editor.SaveBtn nicht gefunden.");

        var unique = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff");
        var firstName = "Ui";
        var lastName = $"Testemp{unique}";
        var username = $"ui_emp_{unique}".ToLowerInvariant();
        var password = "pw_test_123!";

        firstNameBox!.Text = firstName;
        lastNameBox!.Text = lastName;
        usernameBox!.Text = username;

        Assert.That(passwordEl!.Patterns.Value.IsSupported, Is.True, "PasswordBox unterstützt kein ValuePattern.");
        passwordEl.Patterns.Value.Pattern.SetValue(password);

        saveBtn!.Invoke();

        // 1) Erfolg-Overlay/MessageBox im MainWindow schließen
        DismissInAppMessageBox(main!, timeoutSeconds: 8);

        // 2) Warten bis Editor wirklich weg ist (statt DetailsArea per AutomationId zu erwarten)
        var editorGone = Retry.WhileFalse(
            () =>
            {
                var firstName = main!.FindFirstDescendant(cf => cf.ByAutomationId("Employees.Editor.NewEmpFirstName"))?.AsTextBox();
                if (firstName == null) return true; // nicht mehr im Tree -> gut
                return firstName.Properties.IsOffscreen.Value; // offscreen -> gut
            },
            timeout: TimeSpan.FromSeconds(8)
        ).Result;

        Assert.That(editorGone, Is.True, "Editor ist nach Speichern weiterhin sichtbar (wahrscheinlich blockiert noch ein Dialog).");

        // 3) Mitarbeiter in Liste finden (über sichtbare Textknoten)
        var found = Retry.WhileFalse(
            () =>
            {
                var list = main!.FindFirstDescendant(cf => cf.ByAutomationId("Employees.EmployeesList"))?.AsListBox();
                if (list == null) return false;

                foreach (var item in list.Items)
                {
                    var textNodes = item.FindAllDescendants(cf => cf.ByControlType(ControlType.Text));
                    var combined = string.Join(" ", textNodes.Select(t => t.Name).Where(n => !string.IsNullOrWhiteSpace(n)));

                    if (combined.Contains(lastName, StringComparison.OrdinalIgnoreCase))
                        return true;
                }

                return false;
            },
            timeout: TimeSpan.FromSeconds(12)
        ).Result;

        Assert.That(found, Is.True, $"Neuer Mitarbeiter wurde nicht in der Liste gefunden (Suche nach: {lastName}).");
    }

    private void DismissInAppMessageBox(Window mainWindow, int timeoutSeconds)
    {
        // Sucht innerhalb des MainWindows nach einem Button "Verstanden"/"OK" und klickt ihn.
        Retry.WhileFalse(
            () =>
            {
                var button =
                    mainWindow.FindFirstDescendant(cf => cf.ByControlType(ControlType.Button).And(cf.ByName("Verstanden")))?.AsButton()
                    ?? mainWindow.FindFirstDescendant(cf => cf.ByControlType(ControlType.Button).And(cf.ByName("OK")))?.AsButton()
                    ?? mainWindow.FindFirstDescendant(cf => cf.ByControlType(ControlType.Button).And(cf.ByName("Ok")))?.AsButton();

                if (button == null) return false;

                try { button.Invoke(); } catch { return false; }
                return true;
            },
            timeout: TimeSpan.FromSeconds(timeoutSeconds)
        );
    }

    private Window? TryGetMainWindow()
    {
        try
        {
            return App.GetAllTopLevelWindows(Automation)
                .FirstOrDefault(w => w != null && w.Title.Contains("Zoo", StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return null;
        }
    }

    private void CloseAllNonMainWindows(Window mainWindow, int timeoutSeconds)
    {
        var until = DateTime.UtcNow.AddSeconds(timeoutSeconds);

        while (DateTime.UtcNow < until)
        {
            var windows = App.GetAllTopLevelWindows(Automation);
            var dialogs = windows
                .Where(w => w != null && !ReferenceEquals(w, mainWindow))
                .ToList();

            if (dialogs.Count == 0)
                return;

            foreach (var dialog in dialogs)
            {
                var btn =
                    dialog.FindFirstDescendant(cf => cf.ByControlType(ControlType.Button).And(cf.ByName("OK")))
                        ?.AsButton()
                    ?? dialog.FindFirstDescendant(cf => cf.ByControlType(ControlType.Button).And(cf.ByName("Ok")))
                        ?.AsButton()
                    ?? dialog.FindFirstDescendant(cf => cf.ByControlType(ControlType.Button).And(cf.ByName("Ja")))
                        ?.AsButton()
                    ?? dialog
                        .FindFirstDescendant(cf => cf.ByControlType(ControlType.Button).And(cf.ByName("Schließen")))
                        ?.AsButton()
                    ?? dialog.FindAllDescendants(cf => cf.ByControlType(ControlType.Button)).FirstOrDefault()
                        ?.AsButton();

                if (btn != null)
                {
                    try
                    {
                        btn.Invoke();
                    }
                    catch
                    {
                        /* ignore */
                    }

                    continue;
                }

                try
                {
                    dialog.Close();
                }
                catch
                {
                    /* ignore */
                }
            }

            System.Threading.Thread.Sleep(150);
        }
    }

    private void OpenEmployeesView()
    {
        var main = WaitForWindowByTitleContains("ZooManager");
        Assert.That(main, Is.Not.Null, "MainWindow nicht gefunden.");

        var button = main!.FindFirstDescendant(cf => cf.ByAutomationId("EmployeesButton"))?.AsButton();
        Assert.That(button, Is.Not.Null, "Nav-Button EmployeesButton nicht gefunden.");

        button!.Invoke();

        var viewRoot = Retry.WhileNull(
            () => main.FindFirstDescendant(cf => cf.ByAutomationId("View.Employees")),
            timeout: TimeSpan.FromSeconds(8)
        ).Result;

        Assert.That(viewRoot, Is.Not.Null, "EmployeesView wurde nicht geladen (View.Employees).");
    }

    private System.Collections.Generic.List<string> CloseAllNonMainWindowsAndCollectText(Window mainWindow,
        int timeoutSeconds)
    {
        var texts = new System.Collections.Generic.List<string>();
        var until = DateTime.UtcNow.AddSeconds(timeoutSeconds);

        while (DateTime.UtcNow < until)
        {
            var windows = App.GetAllTopLevelWindows(Automation);
            
            var dialogs = windows.Where(w => w != null && !ReferenceEquals(w, mainWindow)).ToList();
            if (dialogs.Count == 0)
                return texts;

            foreach (var dialog in dialogs)
            {
                try
                {
                    var textNodes = dialog.FindAllDescendants(cf => cf.ByControlType(ControlType.Text));
                    var combined = string.Join(" ",
                        textNodes.Select(t => t.Name).Where(n => !string.IsNullOrWhiteSpace(n)));
                    if (!string.IsNullOrWhiteSpace(combined))
                        texts.Add(combined);
                }
                catch
                {
                    /* ignore */
                }
                
                var btn =
                    dialog.FindFirstDescendant(cf => cf.ByControlType(ControlType.Button).And(cf.ByName("OK")))
                        ?.AsButton()
                    ?? dialog.FindFirstDescendant(cf => cf.ByControlType(ControlType.Button).And(cf.ByName("Ok")))
                        ?.AsButton()
                    ?? dialog.FindFirstDescendant(cf => cf.ByControlType(ControlType.Button).And(cf.ByName("Ja")))
                        ?.AsButton()
                    ?? dialog
                        .FindFirstDescendant(cf => cf.ByControlType(ControlType.Button).And(cf.ByName("Schließen")))
                        ?.AsButton()
                    ?? dialog.FindAllDescendants(cf => cf.ByControlType(ControlType.Button)).FirstOrDefault()
                        ?.AsButton();

                if (btn != null)
                {
                    try
                    {
                        btn.Invoke();
                    }
                    catch
                    {
                        /* ignore */
                    }
                }
                else
                {
                    try
                    {
                        dialog.Close();
                    }
                    catch
                    {
                        /* ignore */
                    }
                }
            }

            System.Threading.Thread.Sleep(150);
        }

        return texts;
    }

}