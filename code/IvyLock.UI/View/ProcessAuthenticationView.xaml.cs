using IvyLock.ViewModel;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace IvyLock.View
{
    /// <summary>
    /// Interaction logic for AuthenticationView.xaml
    /// </summary>
    public partial class ProcessAuthenticationView : Window
    {
        private ProcessAuthenticationViewModel avm;

        public ProcessAuthenticationView()
        {
            InitializeComponent();
            avm = DataContext as ProcessAuthenticationViewModel;
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
            Activate();
        }

        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                avm.VerifyPassword();
            }
        }

        private void CompletedEventHandler(object sender, EventArgs e)
        {
            Close();
        }
    }
}