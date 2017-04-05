using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace IvyLock.ViewModel
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
            get
            {
                return action;
            }
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

    public class AsyncDelegateCommand : ICommand, INotifyPropertyChanged
    {
        private Func<Task> task;

        public event EventHandler CanExecuteChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        public AsyncDelegateCommand(Func<Task> task)
        {
            this.task = task;
        }

        public AsyncDelegateCommand(Action task) : this(() => System.Threading.Tasks.Task.Run(task))
        {
        }

        public Func<Task> Task
        {
            get
            {
                return task;
            }
            set
            {
                if (task != value && (task == null || value == null))
                    CanExecuteChanged?.Invoke(this, null);

                task = value;
            }
        }

        public NotifyTaskCompletion Execution { get; private set; }

        public bool CanExecute(object parameter)
        {
            return task != null;
        }

        public void Execute(object parameter)
        {
            Execution = new NotifyTaskCompletion(task?.Invoke());
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Execution"));
        }
    }
}