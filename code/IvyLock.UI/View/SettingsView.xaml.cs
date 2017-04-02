using IvyLock.Model;
using IvyLock.Service;
using System;
using System.Windows;
using Xceed.Wpf.Toolkit;
using System.ComponentModel;
using IvyLock.UI.ViewModel;

namespace IvyLock.UI.View
{
	/// <summary>
	/// Interaction logic for SettingsView.xaml
	/// </summary>
	public partial class SettingsView : Window
	{
		public SettingsView()
		{
			InitializeComponent();
            (DataContext as SettingsViewModel).View = this;
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			if (IsLoaded)
			{
				SettingsViewModel svm = ((SettingsViewModel)DataContext);
				svm.CurrentScreen = SettingsViewModel.Screen.EnterPassword;
				svm.Locked = true;
				pwdBox.Clear();
			}

			// window can be hidden but not closed
			Hide();

			e.Cancel = true;
			base.OnClosing(e);
		}
	}
}