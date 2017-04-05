using IvyLock.ViewModel;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace IvyLock.View.Control
{
    /// <summary>
    /// Interaction logic for TaskButton.xaml
    /// </summary>
    public partial class TaskButton : Button
    {
        public TaskButton()
        {
            InitializeComponent();
        }

        public AsyncDelegateCommand AsyncCommand
        {
            get { return (AsyncDelegateCommand)GetValue(AsyncCommandProperty); }
            set { SetValue(AsyncCommandProperty, value); SetValue(ExecutionProperty, value.Execution); }
        }

        public static readonly DependencyProperty AsyncCommandProperty =
            DependencyProperty.Register("AsyncCommand", typeof(AsyncDelegateCommand), typeof(TaskButton), new PropertyMetadata(null));

        public NotifyTaskCompletion Execution
        {
            get { return (NotifyTaskCompletion)GetValue(ExecutionProperty); }
            set { SetValue(ExecutionProperty, value); SetValue(TaskProperty, value.Task); }
        }

        public static readonly DependencyProperty ExecutionProperty =
            DependencyProperty.Register("Execution", typeof(NotifyTaskCompletion), typeof(TaskButton), new PropertyMetadata(null));

        public Task Task
        {
            get { return (Task)GetValue(TaskProperty); }
            set { SetValue(TaskProperty, value); }
        }

        public static readonly DependencyProperty TaskProperty =
            DependencyProperty.Register("Task", typeof(Task), typeof(TaskButton), new PropertyMetadata(null));
    }
}