using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;

namespace IvyLock.View
{
    /// <summary>
    /// Interaction logic for FileAuthenticationView.xaml
    /// </summary>
    public partial class FileAuthenticationView : Window
    {
        public FileAuthenticationView()
        {
            InitializeComponent();
        }

        private void AuthenticationViewModel_CloseRequested()
        {
            Close();
        }

        private void AuthenticationViewModel_ShowRequested()
        {
            Show();
        }
    }
}