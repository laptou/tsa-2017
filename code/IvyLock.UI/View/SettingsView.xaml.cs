using IvyLock.Model;
using IvyLock.Service;
using System;
using System.Windows;
using Xceed.Wpf.Toolkit;
using System.ComponentModel;

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
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			Hide();
			e.Cancel = true;
			base.OnClosing(e);
		}
	}
}