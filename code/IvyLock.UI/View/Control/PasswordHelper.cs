using System;
using System.Security;
using System.Windows;
using System.Windows.Controls;

namespace IvyLock.View.Control
{
    public class PasswordHelper
    {
        public static SecureString GetEncryptedPassword(DependencyObject obj)
        {
            return (SecureString)obj.GetValue(EncryptedPasswordProperty);
        }

        public static void SetEncryptedPassword(DependencyObject obj, SecureString value)
        {
            obj.SetValue(EncryptedPasswordProperty, value);
        }

        public static readonly DependencyProperty EncryptedPasswordProperty =
            DependencyProperty.RegisterAttached("EncryptedPassword", typeof(SecureString), typeof(PasswordHelper),
                new PropertyMetadata());

        public static bool GetWatch(DependencyObject obj)
        {
            return (bool)obj.GetValue(EncryptedPasswordProperty);
        }

        public static void SetWatch(DependencyObject obj, bool value)
        {
            obj.SetValue(WatchProperty, value);
        }

        // Using a DependencyProperty as the backing store for Watch.
        // This enables animation, styling, binding, etc...
        public static readonly DependencyProperty WatchProperty =
            DependencyProperty.RegisterAttached("Watch", typeof(bool), typeof(PasswordHelper), new PropertyMetadata(false, WatchChanged));

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
            if (sender is PasswordBox pb)
            {
                SecureString ss = pb.SecurePassword;
                SetEncryptedPassword(pb, ss.Length > 0 ? ss : null);
            }
        }
    }
}