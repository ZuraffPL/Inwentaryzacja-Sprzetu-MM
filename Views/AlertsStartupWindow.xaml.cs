using System;
using System.Collections.Generic;
using System.Windows;
using InwentaryzacjaSprzetu.Helpers;
using InwentaryzacjaSprzetu.Models;

namespace InwentaryzacjaSprzetu.Views
{
    public partial class AlertsStartupWindow : Window
    {
        private readonly AppPreferences _prefs;

        /// <summary>True gdy użytkownik kliknął „Otwórz Powiadomienia" — wywołujący powinien nawigować do widoku alertów.</summary>
        public bool NavigateToAlerts { get; private set; }

        public AlertsStartupWindow(List<Alert> triggeredAlerts, AppPreferences prefs)
        {
            _prefs = prefs;

            InitializeComponent();

            // Nagłówek
            int count = triggeredAlerts.Count;
            CounterText.Text  = count.ToString();
            SubtitleText.Text = count == 1
                ? "Masz 1 powiadomienie wymagające uwagi."
                : $"Masz {count} powiadomienia wymagające uwagi.";

            // Wypełnij listę
            AlertsList.ItemsSource = triggeredAlerts;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            SaveDismissIfChecked();
            Close();
        }

        private void NavigateButton_Click(object sender, RoutedEventArgs e)
        {
            SaveDismissIfChecked();
            NavigateToAlerts = true;
            Close();
        }

        private void SaveDismissIfChecked()
        {
            if (DontShowTodayCheckbox.IsChecked == true)
            {
                _prefs.AlertStartupDismissedDate = DateTime.Today.ToString("yyyy-MM-dd");
                _prefs.Save();
            }
        }
    }
}
