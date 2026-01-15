using System.Windows;

namespace ZooManager.UI.Views
{
    public partial class ZooMessageBox : Window
    {
        public ZooMessageBox(string message, string title)
        {
            InitializeComponent();
            MessageText.Text = message;
            TitleText.Text = title;
        }

        public static void Show(string message, string title = "Zoo Manager Info")
        {
            var msg = new ZooMessageBox(message, title);
            msg.Owner = Application.Current.MainWindow;
            msg.ShowDialog();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}