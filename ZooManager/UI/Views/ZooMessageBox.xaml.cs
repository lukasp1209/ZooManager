using System.Linq;
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
            
            var owner =
                Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive && w != msg)
                ?? (Application.Current?.MainWindow != msg ? Application.Current?.MainWindow : null);
            
            if (owner != null && owner != msg)
                msg.Owner = owner;

            msg.ShowDialog();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}