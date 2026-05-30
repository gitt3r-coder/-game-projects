using System.Windows;

namespace BlackBookAppWPF
{
    public partial class InputDialog : Window
    {
        public string Answer { get; private set; }

        public InputDialog(string prompt, string title, string defaultValue = "")
        {
            InitializeComponent();
            this.Title = title;
            lblPrompt.Text = prompt;
            txtInput.Text = defaultValue;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            Answer = txtInput.Text;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}