using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;

namespace IvyLock.View
{
    /// <summary>
    /// Interaction logic for EnrollView.xaml
    /// </summary>
    public partial class EnrollView : Window
    {
        public EnrollView()
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