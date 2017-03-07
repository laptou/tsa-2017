using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace IvyLock.UI.ViewModel
{
	public class DelegateCommand : ICommand
	{
		private Action<object> action;

		public event EventHandler CanExecuteChanged;

		public DelegateCommand(Action<object> action)
		{
			this.action = action;
		}

		public DelegateCommand(Task action)
		{
			this.action = (o) => action.Start();
		}

		public Action<object> Action
		{
			get { return action; }
			set
			{
				if (action != value && (action == null || value == null))
					CanExecuteChanged?.Invoke(this, null);

				action = value;
			}
		}

		public bool CanExecute(object parameter)
		{
			return action != null;
		}

		public void Execute(object parameter)
		{
			action?.Invoke(parameter);
		}
	}

	public class AsyncDelegateCommand : ICommand
	{
		private Func<Task> task;

		public event EventHandler CanExecuteChanged;

		public AsyncDelegateCommand(Func<Task> task)
		{
			this.task = task;
		}

		public Func<Task> Task
		{
			get { return task; }
			set
			{
				if (task != value && (task == null || value == null))
					CanExecuteChanged?.Invoke(this, null);

				task = value;
			}
		}

		public NotifyTaskCompletion Execution { get; set; }

		public bool CanExecute(object parameter)
		{
			return task != null;
		}

		public void Execute(object parameter)
		{
			Execution = new NotifyTaskCompletion(task?.Invoke());
		}
	}
}
