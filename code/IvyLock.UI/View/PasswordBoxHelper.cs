using System;
using System.Security;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace IvyLock.UI.View
{
	public class PasswordBoxHelper
	{
		public static string GetPlaceholder(DependencyObject obj)
		{
			return (string)obj.GetValue(PlaceholderProperty);
		}

		public static void SetPlaceholder(DependencyObject obj, string value)
		{
			obj.SetValue(PlaceholderProperty, value);
		}

		// Using a DependencyProperty as the backing store for Placeholder.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty PlaceholderProperty =
			DependencyProperty.RegisterAttached("Placeholder", typeof(string), typeof(PasswordBoxHelper), new PropertyMetadata(null));
		
		public static SecureString GetEncryptedPassword(DependencyObject obj)
		{
			return (SecureString)obj.GetValue(EncryptedPasswordProperty);
		}

		public static void SetEncryptedPassword(DependencyObject obj, SecureString value)
		{
			obj.SetValue(EncryptedPasswordProperty, value);
		}

		// Using a DependencyProperty as the backing store for
		// EncryptedPassword. This enables animation, styling, binding, etc...
		public static readonly DependencyProperty EncryptedPasswordProperty =
			DependencyProperty.RegisterAttached("EncryptedPassword", typeof(SecureString), typeof(PasswordBoxHelper));


		public static bool GetWatch(DependencyObject obj)
		{
			return (bool)obj.GetValue(EncryptedPasswordProperty);
		}

		public static void SetWatch(DependencyObject obj, bool value)
		{
			obj.SetValue(WatchProperty, value);
		}

		// Using a DependencyProperty as the backing store for Watch.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty WatchProperty =
			DependencyProperty.RegisterAttached("Watch", typeof(bool), typeof(PasswordBoxHelper), new PropertyMetadata(false, WatchChanged));

		private static void WatchChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			PasswordBox passwordBox = d as PasswordBox;
			if (passwordBox == null)
				return;
			if ((bool)e.OldValue)
			{
				passwordBox.PasswordChanged -= PasswordChanged;
			}
			if ((bool)e.NewValue)
			{
				passwordBox.PasswordChanged += PasswordChanged;
			}
		}

		private static void PasswordChanged(object sender, RoutedEventArgs e)
		{
			PasswordBox pb = (sender as PasswordBox);
			SecureString ss = pb.SecurePassword;

			SetEncryptedPassword(sender as DependencyObject, ss.Length > 0 ? ss : null);
		}

	}
}