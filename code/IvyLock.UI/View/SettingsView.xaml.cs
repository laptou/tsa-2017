using System;
using IvyLock.ViewModel;
using System.ComponentModel;
using System.Windows;
using System.IO;

namespace IvyLock.View
{
    /// <summary>
    /// Interaction logic for SettingsView.xaml
    /// </summary>
    public partial class SettingsView : Window
    {
        SettingsViewModel svm;

        public SettingsView()
        {
            InitializeComponent();
            svm = (DataContext as SettingsViewModel);
            svm.View = this;

            svm.PasswordVerified += (s, e) =>
            {
                pwdBox.Clear();
            };
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (IsLoaded)
            {
                svm.CurrentScreen = Screen.EnterPassword;
                pwdBox.Clear();
            }

            // window can be hidden but not closed
            Hide();

            e.Cancel = true;
            base.OnClosing(e);
        }

        private void OnDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Note that you can have more than one file.
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                foreach (string file in files)
                {
                    FileAuthenticationView fav = new FileAuthenticationView();
                    FileAuthenticationViewModel favm = (FileAuthenticationViewModel)fav.DataContext;
                    favm.Path = file;
                    favm.Encrypt = !string.Equals(Path.GetExtension(file), ".ivy", StringComparison.InvariantCultureIgnoreCase);
                    fav.Show();
                }
            }
        }
    }
}