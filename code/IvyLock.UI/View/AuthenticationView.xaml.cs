using System;
using System.Windows;

namespace IvyLock.UI.View
{
	/// <summary>
	/// Interaction logic for AuthenticationView.xaml
	/// </summary>
	public partial class AuthenticationView : Window
	{
		public AuthenticationView()
		{
			InitializeComponent();
		}

		private void AuthenticationViewModel_CloseRequested()
		{
			Close();
		}
	}
}