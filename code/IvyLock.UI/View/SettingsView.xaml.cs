using System;
using IvyLock.ViewModel;
using System.ComponentModel;
using System.Windows;

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
    }
}