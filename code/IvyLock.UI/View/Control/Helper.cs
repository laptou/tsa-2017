using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace IvyLock.View.Control
{
    public class Helper
    {
        public static bool GetUpdateOnEnter(DependencyObject obj)
        {
            return (bool)obj.GetValue(UpdateOnEnterProperty);
        }

        public static void SetUpdateOnEnter(DependencyObject obj, bool value)
        {
            obj.SetValue(UpdateOnEnterProperty, value);
        }

        public static readonly DependencyProperty UpdateOnEnterProperty =
            DependencyProperty.RegisterAttached("UpdateOnEnter", typeof(bool), typeof(Helper), new PropertyMetadata(false, UpdateOnEnterChanged));

        private static void UpdateOnEnterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox tb && e.NewValue as bool? == true)
            {
                tb.PreviewKeyDown += (s, f) =>
                {
                    if (f.Key == Key.Enter)
                    {
                        BindingExpression exp = tb.GetBindingExpression(TextBox.TextProperty);
                        exp.UpdateSource();
                    }
                };
            }
            else if (d is PasswordBox pb && e.NewValue as bool? == true)
            {
                pb.PreviewKeyDown += (s, f) =>
                {
                    if (f.Key == Key.Enter)
                    {
                        BindingExpression exp = pb.GetBindingExpression(PasswordHelper.EncryptedPasswordProperty);
                        exp.UpdateSource();
                    }
                };
            }
        }

        public static readonly DependencyProperty UpdateOnDeleteProperty =
            DependencyProperty.RegisterAttached("UpdateOnDelete", typeof(bool), typeof(Helper), new PropertyMetadata(false, UpdateOnDeleteChanged));

        private static void UpdateOnDeleteChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox tb && e.NewValue as bool? == true)
            {
                tb.PreviewKeyDown += (s, f) =>
                {
                    if (f.Key == Key.Delete)
                    {
                        BindingExpression exp = tb.GetBindingExpression(TextBox.TextProperty);
                        exp.UpdateSource();
                    }
                };
            }
            else if (d is PasswordBox pb && e.NewValue as bool? == true)
            {
                pb.PreviewKeyDown += (s, f) =>
                {
                    if (f.Key == Key.Delete)
                    {
                        BindingExpression exp = pb.GetBindingExpression(PasswordHelper.EncryptedPasswordProperty);
                        exp.UpdateSource();
                    }
                };
            }
        }

        public static string GetPlaceholder(DependencyObject obj)
        {
            return (string)obj.GetValue(PlaceholderProperty);
        }

        public static void SetPlaceholder(DependencyObject obj, string value)
        {
            obj.SetValue(PlaceholderProperty, value);
        }

        public static readonly DependencyProperty PlaceholderProperty =
            DependencyProperty.RegisterAttached("Placeholder", typeof(string), typeof(Helper), new PropertyMetadata(null));
    }
}