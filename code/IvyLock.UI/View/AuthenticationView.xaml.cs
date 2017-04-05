using IvyLock.ViewModel;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;

namespace IvyLock.View
{
    /// <summary>
    /// Interaction logic for AuthenticationView.xaml
    /// </summary>
    public partial class AuthenticationView : Window
    {
        private AuthenticationViewModel avm;

        public AuthenticationView()
        {
            InitializeComponent();
            avm = DataContext as AuthenticationViewModel;
        }

        private void AuthenticationViewModel_CloseRequested()
        {
            if (IsLoaded)
                Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            Task.Run(() =>
            {
                if (avm.Locked)
                    foreach (Process process in avm.Processes)
                        try { process.Kill(); } catch { }
            });

            base.OnClosed(e);
        }

        private void AuthenticationViewModel_ShowRequested()
        {
            Show();
        }
    }
}