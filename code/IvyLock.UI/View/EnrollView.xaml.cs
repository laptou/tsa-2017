using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

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
