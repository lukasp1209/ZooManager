using System.Windows;
using System.Windows.Controls;
using ZooManager.Core.Models;
using ZooManager.Infrastructure.Persistence;
using ZooManager.Infrastructure.Configuration;

namespace ZooManager.UI.Views
{
    public partial class EventsView : UserControl
    {
        private MySqlPersistenceService _db;

        public EventsView()
        {
            InitializeComponent();
            _db = new MySqlPersistenceService(DatabaseConfig.GetConnectionString());
            
            // Zeit-Selektoren füllen
            for (int i = 0; i < 24; i++) HourSelector.Items.Add(i.ToString("D2"));
            for (int i = 0; i < 60; i += 5) MinuteSelector.Items.Add(i.ToString("D2"));
            
            LoadData();
        }

        private void LoadData()
        {
            EventsList.ItemsSource = _db.LoadEvents().OrderBy(e => e.Start).ToList();
        }

        private void EventsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (EventsList.SelectedItem is ZooEvent selected)
            {
                SelectedEventTitle.Text = selected.Title;
                SelectedEventTime.Text = selected.Start.ToString("dddd, dd. MMMM yyyy - HH:mm") + " Uhr";
                SelectedEventDesc.Text = selected.Description;
                
                EventDetailsArea.Visibility = Visibility.Visible;
                EventEditorArea.Visibility = Visibility.Collapsed;
            }
        }

        private void AddEventUI_Click(object sender, RoutedEventArgs e)
        {
            EventDetailsArea.Visibility = Visibility.Collapsed;
            EventEditorArea.Visibility = Visibility.Visible;
            NewEventTitle.Text = "";
            NewEventDescription.Text = "";
            NewEventDate.SelectedDate = DateTime.Now;
            HourSelector.SelectedIndex = 12;
            MinuteSelector.SelectedIndex = 0;
        }

        private void CancelEventEditor_Click(object? sender, RoutedEventArgs? e)
        {
            EventEditorArea.Visibility = Visibility.Collapsed;
            EventDetailsArea.Visibility = Visibility.Visible;
        }

        private void SaveNewEvent_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NewEventTitle.Text) || NewEventDate.SelectedDate == null) return;

            DateTime date = NewEventDate.SelectedDate.Value;
            int hour = int.Parse(HourSelector.SelectedItem?.ToString() ?? "0");
            int minute = int.Parse(MinuteSelector.SelectedItem?.ToString() ?? "0");
            
            var newEvent = new ZooEvent
            {
                Title = NewEventTitle.Text,
                Description = NewEventDescription.Text,
                Start = new DateTime(date.Year, date.Month, date.Day, hour, minute, 0)
            };

            _db.SaveEvents(new List<ZooEvent> { newEvent });
            ZooMessageBox.Show("Das Event wurde erfolgreich geplant.", "Erfolg");
            CancelEventEditor_Click(null, null);
            LoadData();
        }

        private void DeleteEvent_Click(object sender, RoutedEventArgs e)
        {
            if (EventsList.SelectedItem is ZooEvent selected)
            {
                using (var conn = new MySql.Data.MySqlClient.MySqlConnection(DatabaseConfig.GetConnectionString()))
                {
                    conn.Open();
                    var cmd = new MySql.Data.MySqlClient.MySqlCommand("DELETE FROM ZooEvents WHERE Title = @t AND Start = @s", conn);
                    cmd.Parameters.AddWithValue("@t", selected.Title);
                    cmd.Parameters.AddWithValue("@s", selected.Start);
                    cmd.ExecuteNonQuery();
                }
                ZooMessageBox.Show("Event wurde abgesagt und gelöscht.", "Info");
                LoadData();
            }
        }
    }
}