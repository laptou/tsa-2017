using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;

namespace IvyLock.UI.ViewModel
{
	public abstract class ViewModel : INotifyPropertyChanged,
		INotifyPropertyChanging
	{
		public event PropertyChangingEventHandler PropertyChanging;

		public event PropertyChangedEventHandler PropertyChanged;

		private PropertyInfo GetPropertyInfo<TProperty>(
			Expression<Func<TProperty>> propertyLambda)
		{
			MemberExpression member = propertyLambda.Body as MemberExpression;
			if (member == null)
				throw new ArgumentException(string.Format(
					"Expression '{0}' refers to a method, not a property.",
					propertyLambda.ToString()));

			PropertyInfo propInfo = member.Member as PropertyInfo;
			if (propInfo == null)
				throw new ArgumentException(string.Format(
					"Expression '{0}' refers to a field, not a property.",
					propertyLambda.ToString()));

			return propInfo;
		}

		public void SetProperty<T>(T value, ref T variable, [CallerMemberName] string propertyName = "")
		{
			RaisePropertyChanging(propertyName);

			variable = value;

			RaisePropertyChanged(propertyName);
		}

		public void SetProperty<T>(T value, ref T variable, Expression<Func<T>> propertyLambda)
		{
			string propertyName = GetPropertyInfo(propertyLambda).Name;

			RaisePropertyChanging(propertyName);

			variable = value;

			RaisePropertyChanged(propertyName);
		}

		protected void DesignTime(Action action)
		{
			if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
			{
				// in design mode
				action();
			}
		}

		protected void RunTime(Action action)
		{
			if (!DesignerProperties.GetIsInDesignMode(new DependencyObject()))
			{
				// not in design mode
				action();
			}
		}

		protected void UI(Action action)
		{
			Application.Current.Dispatcher.Invoke(action);
		}

		protected async Task UIAsync(Action action)
		{
			await Application.Current.Dispatcher.InvokeAsync(action);
		}

		protected async Task DesignTimeAsync(Func<Task> task)
		{
			if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
			{
				// in design mode
				await task();
			}
		}

		protected async Task RunTimeAsync(Func<Task> task)
		{
			if (!DesignerProperties.GetIsInDesignMode(new DependencyObject()))
			{
				// not in design mode
				await task();
			}
		}

		protected async Task<T> DesignOrRunTimeAsync<T>(Func<Task<T>> designTime, Func<Task<T>> runTime)
		{
			if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
			{
				// in design mode
				return await designTime();
			}
			else
			{
				return await runTime();
			}
		}

		public void RaisePropertyChanging(string propertyName)
		{
			Application.Current.Dispatcher.Invoke(() =>
			{
				PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));
			});
		}

		public void RaisePropertyChanging<T>(Expression<Func<T>> propertyLambda)
		{
			string propertyName = GetPropertyInfo(propertyLambda).Name;

			Application.Current.Dispatcher.Invoke(() =>
			{
				PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));
			});
		}

		public void RaisePropertyChanged(string propertyName)
		{
			Application.Current.Dispatcher.Invoke(() =>
			{
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
			});
		}

		public void RaisePropertyChanged<T>(Expression<Func<T>> propertyLambda)
		{
			string propertyName = GetPropertyInfo(propertyLambda).Name;
			Application.Current.Dispatcher.Invoke(() =>
			{
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
			});
		}

		public void CloseView()
		{
			CloseRequested?.Invoke();
		}

		public void HideView()
		{
			HideRequested?.Invoke();
		}

		public void ShowView()
		{
			ShowRequested?.Invoke();
		}

		public event Action CloseRequested;

		public event Action HideRequested;

		public event Action ShowRequested;
	}
}