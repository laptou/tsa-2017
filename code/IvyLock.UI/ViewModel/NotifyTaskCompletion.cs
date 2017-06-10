using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace IvyLock.ViewModel
{
    public sealed class NotifyTaskCompletion<TResult> : INotifyPropertyChanged
    {
        public NotifyTaskCompletion(Task<TResult> task)
        {
            Task = task;
            if (!task.IsCompleted)
            {
                var _ = WatchTaskAsync(task);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string ErrorMessage
        {
            get
            { return InnerException?.Message; }
        }

        public AggregateException Exception { get { return Task.Exception; } }

        public Exception InnerException
        {
            get
            {
                return Exception?.InnerException;
            }
        }

        public bool IsCanceled { get { return Task.IsCanceled; } }

        public bool IsCompleted { get { return Task.IsCompleted; } }

        public bool IsFaulted { get { return Task.IsFaulted; } }

        public bool IsNotCompleted { get { return !Task.IsCompleted; } }

        public bool IsSuccessfullyCompleted
        {
            get
            {
                return Task.Status == TaskStatus.RanToCompletion;
            }
        }

        public TResult Result
        {
            get
            {
                return (Task.Status == TaskStatus.RanToCompletion) ? Task.Result : default(TResult);
            }
        }

        public TaskStatus Status { get { return Task.Status; } }

        public Task<TResult> Task { get; private set; }

        private async Task WatchTaskAsync(Task task)
        {
            try
            {
                await task;
            }
            catch
            {
            }

            var propertyChanged = PropertyChanged;
            if (propertyChanged == null) return;
            propertyChanged(this, new PropertyChangedEventArgs("Status"));
            propertyChanged(this, new PropertyChangedEventArgs("IsCompleted"));
            propertyChanged(this, new PropertyChangedEventArgs("IsNotCompleted"));
            if (task.IsCanceled)
                propertyChanged(this, new PropertyChangedEventArgs("IsCanceled"));
            else if (task.IsFaulted)
            {
                propertyChanged(this, new PropertyChangedEventArgs("IsFaulted"));
                propertyChanged(this, new PropertyChangedEventArgs("Exception"));
                propertyChanged(this,
                  new PropertyChangedEventArgs("InnerException"));
                propertyChanged(this, new PropertyChangedEventArgs("ErrorMessage"));
            }
            else
            {
                propertyChanged(this,
                  new PropertyChangedEventArgs("IsSuccessfullyCompleted"));
                propertyChanged(this, new PropertyChangedEventArgs("Result"));
            }
        }
    }

    public sealed class NotifyTaskCompletion : Model.Model, IProgress<double>
    {
        public NotifyTaskCompletion(Task task)
        {
            Task = task;
            if (!task.IsCompleted)
            {
                var _ = WatchTaskAsync(task);
            }
        }

        public NotifyTaskCompletion(Func<IProgress<double>, Task> func)
        {
            Task = func(this);
            if (!Task.IsCompleted)
            {
                var _ = WatchTaskAsync(Task);
            }
        }

        public NotifyTaskCompletion(Func<Task> func)
        {
            Task = func();
            if (!Task.IsCompleted)
            {
                var _ = WatchTaskAsync(Task);
            }
        }

        public string ErrorMessage
        {
            get
            { return InnerException?.Message; }
        }

        public AggregateException Exception { get { return Task.Exception; } }

        public Exception InnerException
        {
            get
            {
                return Exception?.InnerException;
            }
        }

        public bool IsCanceled { get { return Task.IsCanceled; } }

        public bool IsCompleted { get { return Task.IsCompleted; } }

        public bool IsFaulted { get { return Task.IsFaulted; } }

        public bool IsNotCompleted { get { return !Task.IsCompleted; } }

        public bool IsSuccessfullyCompleted
        {
            get
            {
                return Task.Status == TaskStatus.RanToCompletion;
            }
        }

        public bool IsRunning
        {
            get
            {
                return Task.Status == TaskStatus.Running;
            }
        }

        public double Progress { get; private set; }

        public TaskStatus Status { get { return Task.Status; } }

        public Task Task { get; private set; }

        public void Report(double value)
        {
            Progress = value;
            RaisePropertyChanged("Progress");
        }

        private async Task WatchTaskAsync(Task task)
        {
            try
            {
                await task;
            }
            catch
            {
            }
            
            RaisePropertyChanged("Status");
            RaisePropertyChanged("IsCompleted");
            RaisePropertyChanged("IsNotCompleted");
            if (task.IsCanceled)
                RaisePropertyChanged("IsCanceled");
            else if (task.IsFaulted)
            {
                RaisePropertyChanged("IsFaulted");
                RaisePropertyChanged("Exception");
                RaisePropertyChanged("InnerException");
                RaisePropertyChanged("ErrorMessage");
            }
            else
            {
                RaisePropertyChanged("IsSuccessfullyCompleted");
                RaisePropertyChanged("Result");
            }
        }
    }
}