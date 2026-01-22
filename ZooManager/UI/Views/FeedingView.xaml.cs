using System.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ZooManager.Core.Models;
using ZooManager.Infrastructure.Persistence;
using ZooManager.Infrastructure.Configuration;

namespace ZooManager.UI.Views
{
    public partial class FeedingView : UserControl
    {
        private SqlitePersistenceService _db;

        public FeedingView()
        {
            InitializeComponent();
            _db = new SqlitePersistenceService(DatabaseConfig.GetConnectionString());
            
            for (int i = 0; i < 24; i++) EditFeedingHour.Items.Add(i.ToString("D2"));
            for (int i = 0; i < 60; i += 5) EditFeedingMinute.Items.Add(i.ToString("D2"));
            
            LoadPlan();
        }

        private void LoadPlan()
        {
            FeedingList.ItemsSource = _db.LoadAnimals().OrderBy(a => a.NextFeedingTime).ToList();
        }

        private void FeedingList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FeedingList.SelectedItem is Animal animal)
            {
                PlaceholderText.Visibility = Visibility.Collapsed;
                FeedingEditorArea.Visibility = Visibility.Visible;
                
                SelectedAnimalTitle.Text = $"Fütterung: {animal.Name} ({animal.SpeciesName})";
                EditFeedingDate.SelectedDate = animal.NextFeedingTime;
                EditFeedingHour.SelectedItem = animal.NextFeedingTime.Hour.ToString("D2");
                EditFeedingMinute.SelectedItem = (animal.NextFeedingTime.Minute / 5 * 5).ToString("D2");
            }
        }

        private void SaveFeedingTime_Click(object sender, RoutedEventArgs e)
        {
            if (FeedingList.SelectedItem is Animal animal)
            {
                DateTime date = EditFeedingDate.SelectedDate ?? DateTime.Now;
                int h = int.Parse(EditFeedingHour.SelectedItem.ToString());
                int m = int.Parse(EditFeedingMinute.SelectedItem.ToString());
                
                animal.NextFeedingTime = new DateTime(date.Year, date.Month, date.Day, h, m, 0);
                _db.SaveAnimals(new List<Animal> { animal });
                
                ZooMessageBox.Show("Fütterungszeit wurde manuell angepasst.", "Planer");
                LoadPlan();
            }
        }

        private void FeedingDone_Click(object sender, RoutedEventArgs e)
        {
            if (FeedingList.SelectedItem is Animal animal)
            {
                animal.NextFeedingTime = animal.NextFeedingTime.AddDays(1);
                
                _db.SaveAnimals(new List<Animal> { animal });
                ZooMessageBox.Show($"{animal.Name} gefüttert. Nächster Termin morgen um {animal.NextFeedingTime:HH:mm} Uhr.", "Erfolg");
                LoadPlan();
            }
        }
    }
}