using System.Windows;

namespace ZooManager.UI.Views
{
    public partial class LogoutDialog : Window
    {
        public bool Confirmed { get; private set; }

        public LogoutDialog()
        {
            InitializeComponent();
            Confirmed = false;
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            Confirmed = true;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Confirmed = false;
            this.Close();
        }
    }
}