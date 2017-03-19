using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace IvyLock.UI.View
{
	/// <summary>
	/// Interaction logic for NumericUpDown.xaml
	/// </summary>
	public partial class NumericUpDown : UserControl
	{
		private int _value;
		private bool _handleValue = true;

		public int Value
		{
			get { return (int)GetValue(ValueProperty); }
			set
			{
				SetValue(ValueProperty, Math.Min(Math.Max(value, MinValue), MaxValue));
			}
		}

		// Using a DependencyProperty as the backing store for Value.
		// This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ValueProperty =
			DependencyProperty.Register("Value", typeof(int), typeof(NumericUpDown), new PropertyMetadata(0, ValueChanged));

		private static void ValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{

			NumericUpDown nud = d as NumericUpDown;
			nud._handleValue = false;
			nud.NUDTextBox.Text = e.NewValue.ToString();
			nud._handleValue = true;
		}

		public int MaxValue
		{
			get { return (int)GetValue(MaxValueProperty); }
			set { SetValue(MaxValueProperty, value); }
		}

		// Using a DependencyProperty as the backing store for
		// MaxValue. This enables animation, styling, binding, etc...
		public static readonly DependencyProperty MaxValueProperty =
			DependencyProperty.Register("MaxValue", typeof(int), typeof(NumericUpDown), new PropertyMetadata(100));

		public int MinValue
		{
			get { return (int)GetValue(MinValueProperty); }
			set { SetValue(MinValueProperty, value); }
		}

		// Using a DependencyProperty as the backing store for
		// MinValue. This enables animation, styling, binding, etc...
		public static readonly DependencyProperty MinValueProperty =
			DependencyProperty.Register("MinValue", typeof(int), typeof(NumericUpDown), new PropertyMetadata(0));

		public event PropertyChangedEventHandler PropertyChanged;

		public NumericUpDown()
		{
			InitializeComponent();
			NUDTextBox.TextChanged += (s, e) =>
			{

			};
			
		}

		private void NUDButtonUP_Click(object sender, RoutedEventArgs e)
		{
			if (Value < MaxValue)
				Value++;
		}

		private void NUDButtonDown_Click(object sender, RoutedEventArgs e)
		{
			if (Value > MinValue)
				Value--;
		}

		private void NUDTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Up)
			{
				NUDButtonUP.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
				typeof(Button).GetMethod("set_IsPressed", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(NUDButtonUP, new object[] { true });
			}

			if (e.Key == Key.Down)
			{
				NUDButtonDown.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
				typeof(Button).GetMethod("set_IsPressed", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(NUDButtonDown, new object[] { true });
			}
		}

		private void NUDTextBox_PreviewKeyUp(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Up)
				typeof(Button).GetMethod("set_IsPressed", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(NUDButtonUP, new object[] { false });

			if (e.Key == Key.Down)
				typeof(Button).GetMethod("set_IsPressed", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(NUDButtonDown, new object[] { false });
		}

		private void NUDTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (!_handleValue) return;

			int.TryParse(NUDTextBox.Text, out _value);
			NUDTextBox.Text = _value.ToString();
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Value"));
		}
	}
}