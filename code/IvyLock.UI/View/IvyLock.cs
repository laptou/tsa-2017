using System;
using System.Windows;
using System.Windows.Controls;

namespace IvyLock.UI.View
{
	public class IvyLock : DependencyObject
	{
		public static readonly DependencyProperty PlaceholderProperty = DependencyProperty.RegisterAttached(
			"Placeholder",
			  typeof(String),
			  typeof(IvyLock),
			  new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsRender)
			);

		public static void SetPlaceholder(UIElement element, String value)
		{
			element.SetValue(PlaceholderProperty, value);
		}

		public static String GetPlaceholder(UIElement element)
		{
			return (String)element.GetValue(PlaceholderProperty);
		}

		public static bool GetIsMonitoring(DependencyObject obj)
		{
			return (bool)obj.GetValue(IsMonitoringProperty);
		}

		public static void SetIsMonitoring(DependencyObject obj, bool value)
		{
			obj.SetValue(IsMonitoringProperty, value);
		}

		public static readonly DependencyProperty IsMonitoringProperty =
			DependencyProperty.RegisterAttached("IsMonitoring", typeof(bool), typeof(IvyLock), new UIPropertyMetadata(false, OnIsMonitoringChanged));

		public static int GetPasswordLength(DependencyObject obj)
		{
			return (int)obj.GetValue(PasswordLengthProperty);
		}

		public static void SetPasswordLength(DependencyObject obj, int value)
		{
			obj.SetValue(PasswordLengthProperty, value);
		}

		public static readonly DependencyProperty PasswordLengthProperty =
			DependencyProperty.RegisterAttached("PasswordLength", typeof(int), typeof(IvyLock), new UIPropertyMetadata(0));

		private static void OnIsMonitoringChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var pb = d as PasswordBox;
			if (pb == null)
			{
				return;
			}
			if ((bool)e.NewValue)
			{
				pb.PasswordChanged += PasswordChanged;
			}
			else
			{
				pb.PasswordChanged -= PasswordChanged;
			}
		}

		private static void PasswordChanged(object sender, RoutedEventArgs e)
		{
			var pb = sender as PasswordBox;
			if (pb == null)
			{
				return;
			}
			SetPasswordLength(pb, pb.Password.Length);
		}
	}
}