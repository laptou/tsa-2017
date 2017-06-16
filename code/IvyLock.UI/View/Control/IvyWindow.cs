using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace IvyLock.View.Control
{
    public static class ControlDoubleClickBehavior
    {
        #region Fields

        public static readonly DependencyProperty ExecuteCommand = DependencyProperty.RegisterAttached("ExecuteCommand",
            typeof(ICommand), typeof(ControlDoubleClickBehavior),
            new UIPropertyMetadata(null, OnExecuteCommandChanged));

        public static readonly DependencyProperty ExecuteCommandParameter = DependencyProperty.RegisterAttached("ExecuteCommandParameter",
            typeof(Window), typeof(ControlDoubleClickBehavior));

        #endregion Fields

        #region Methods

        public static ICommand GetExecuteCommand(DependencyObject obj)
        {
            return (ICommand)obj.GetValue(ExecuteCommand);
        }

        public static Window GetExecuteCommandParameter(DependencyObject obj)
        {
            return (Window)obj.GetValue(ExecuteCommandParameter);
        }

        public static void SetExecuteCommand(DependencyObject obj, ICommand command)
        {
            obj.SetValue(ExecuteCommand, command);
        }

        public static void SetExecuteCommandParameter(DependencyObject obj, ICommand command)
        {
            obj.SetValue(ExecuteCommandParameter, command);
        }

#pragma warning disable IDE1006 // Naming Styles

        private static void control_MouseDoubleClick(object sender, MouseButtonEventArgs e)
#pragma warning restore IDE1006 // Naming Styles
        {
            if (sender is System.Windows.Controls.Control control)
            {
                var command = control.GetValue(ExecuteCommand) as ICommand;
                var commandParameter = control.GetValue(ExecuteCommandParameter);

                if (command.CanExecute(e))
                {
                    command.Execute(commandParameter);
                }
            }
        }

        private static void OnExecuteCommandChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is System.Windows.Controls.Control control)
            {
                control.MouseDoubleClick += control_MouseDoubleClick;
            }
        }

        #endregion Methods
    }

    public static class ShowSystemMenuBehavior
    {
        #region Fields

        public static readonly DependencyProperty LeftButtonShowAt = DependencyProperty.RegisterAttached("LeftButtonShowAt",
            typeof(UIElement), typeof(ShowSystemMenuBehavior),
            new UIPropertyMetadata(null, LeftButtonShowAtChanged));

        public static readonly DependencyProperty RightButtonShow = DependencyProperty.RegisterAttached("RightButtonShow",
            typeof(bool), typeof(ShowSystemMenuBehavior),
            new UIPropertyMetadata(false, RightButtonShowChanged));

        public static readonly DependencyProperty TargetWindow = DependencyProperty.RegisterAttached("TargetWindow", typeof(Window), typeof(ShowSystemMenuBehavior));

        private static bool leftButtonToggle = true;

        #endregion Fields

        #region Methods

        public static UIElement GetLeftButtonShowAt(DependencyObject obj)
        {
            return (UIElement)obj.GetValue(LeftButtonShowAt);
        }

        public static bool GetRightButtonShow(DependencyObject obj)
        {
            return (bool)obj.GetValue(RightButtonShow);
        }

        public static Window GetTargetWindow(DependencyObject obj)
        {
            return (Window)obj.GetValue(TargetWindow);
        }

        public static void SetLeftButtonShowAt(DependencyObject obj, UIElement element)
        {
            obj.SetValue(LeftButtonShowAt, element);
        }

        public static void SetRightButtonShow(DependencyObject obj, bool arg)
        {
            obj.SetValue(RightButtonShow, arg);
        }

        public static void SetTargetWindow(DependencyObject obj, Window window)
        {
            obj.SetValue(TargetWindow, window);
        }

        private static void LeftButtonDownShow(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (leftButtonToggle)
            {
                var element = ((UIElement)sender).GetValue(LeftButtonShowAt);

                var showMenuAt = ((Visual)element).PointToScreen(new Point(0, 0));

                var targetWindow = ((UIElement)sender).GetValue(TargetWindow) as Window;

                SystemMenuManager.ShowMenu(targetWindow, showMenuAt);

                leftButtonToggle = !leftButtonToggle;
            }
            else
            {
                leftButtonToggle = !leftButtonToggle;
            }
        }

        private static void LeftButtonShowAtChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is UIElement element && element != null)
            {
                element.MouseLeftButtonDown += LeftButtonDownShow;
            }
        }

        private static void RightButtonDownShow(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var element = (UIElement)sender;

            var targetWindow = element.GetValue(TargetWindow) as Window;

            var showMenuAt = targetWindow.PointToScreen(Mouse.GetPosition((targetWindow)));

            SystemMenuManager.ShowMenu(targetWindow, showMenuAt);
        }

        private static void RightButtonShowChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is UIElement element)
            {
                element.MouseRightButtonDown += RightButtonDownShow;
            }
        }

        #endregion Methods
    }

    public static class SystemMenuManager
    {
        #region Methods

        public static void ShowMenu(Window targetWindow, Point menuLocation)
        {
            if (targetWindow == null)
                throw new ArgumentNullException("TargetWindow is null.");

            int x, y;

            try
            {
                x = Convert.ToInt32(menuLocation.X);
                y = Convert.ToInt32(menuLocation.Y);
            }
            catch (OverflowException)
            {
                x = 0;
                y = 0;
            }

            uint WM_SYSCOMMAND = 0x112, TPM_LEFTALIGN = 0x0000, TPM_RETURNCMD = 0x0100;

            IntPtr window = new WindowInteropHelper(targetWindow).Handle;

            IntPtr wMenu = NativeMethods.GetSystemMenu(window, false);

            int command = NativeMethods.TrackPopupMenuEx(wMenu, TPM_LEFTALIGN | TPM_RETURNCMD, x, y, window, IntPtr.Zero);

            if (command == 0)
                return;

            NativeMethods.PostMessage(window, WM_SYSCOMMAND, new IntPtr(command), IntPtr.Zero);
        }

        #endregion Methods
    }

    public static class WindowDragBehavior
    {
        #region Fields

        public static readonly DependencyProperty LeftMouseButtonDrag = DependencyProperty.RegisterAttached("LeftMouseButtonDrag",
            typeof(Window), typeof(WindowDragBehavior),
            new UIPropertyMetadata(null, OnLeftMouseButtonDragChanged));

        #endregion Fields

        #region Methods

        public static Window GetLeftMouseButtonDrag(DependencyObject obj)
        {
            return (Window)obj.GetValue(LeftMouseButtonDrag);
        }

        public static void SetLeftMouseButtonDrag(DependencyObject obj, Window window)
        {
            obj.SetValue(LeftMouseButtonDrag, window);
        }

#pragma warning disable IDE1006 // Naming Styles

        private static void buttonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
#pragma warning restore IDE1006 // Naming Styles
        {
            var element = sender as UIElement;

            if (element.GetValue(LeftMouseButtonDrag) is Window targetWindow)
            {
                targetWindow.DragMove();
            }
        }

        private static void OnLeftMouseButtonDragChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is UIElement element)
            {
                element.MouseLeftButtonDown += buttonDown;
            }
        }

        #endregion Methods
    }

    public static class WindowResizeBehavior
    {
        #region Fields

        public static readonly DependencyProperty BottomLeftResize = DependencyProperty.RegisterAttached("BottomLeftResize",
            typeof(Window), typeof(WindowResizeBehavior),
            new UIPropertyMetadata(null, OnBottomLeftResizeChanged));

        public static readonly DependencyProperty BottomResize = DependencyProperty.RegisterAttached("BottomResize",
            typeof(Window), typeof(WindowResizeBehavior),
            new UIPropertyMetadata(null, OnBottomResizeChanged));

        public static readonly DependencyProperty BottomRightResize = DependencyProperty.RegisterAttached("BottomRightResize",
            typeof(Window), typeof(WindowResizeBehavior),
            new UIPropertyMetadata(null, OnBottomRightResizeChanged));

        public static readonly DependencyProperty LeftResize = DependencyProperty.RegisterAttached("LeftResize",
            typeof(Window), typeof(WindowResizeBehavior),
            new UIPropertyMetadata(null, OnLeftResizeChanged));

        public static readonly DependencyProperty RightResize = DependencyProperty.RegisterAttached("RightResize",
            typeof(Window), typeof(WindowResizeBehavior),
            new UIPropertyMetadata(null, OnRightResizeChanged));

        public static readonly DependencyProperty TopLeftResize = DependencyProperty.RegisterAttached("TopLeftResize",
            typeof(Window), typeof(WindowResizeBehavior),
            new UIPropertyMetadata(null, OnTopLeftResizeChanged));

        public static readonly DependencyProperty TopResize = DependencyProperty.RegisterAttached("TopResize",
            typeof(Window), typeof(WindowResizeBehavior),
            new UIPropertyMetadata(null, OnTopResizeChanged));

        public static readonly DependencyProperty TopRightResize = DependencyProperty.RegisterAttached("TopRightResize",
            typeof(Window), typeof(WindowResizeBehavior),
            new UIPropertyMetadata(null, OnTopRightResizeChanged));

        #endregion Fields

        #region Methods

        public static Window GetBottomLeftResize(DependencyObject obj)
        {
            return (Window)obj.GetValue(BottomLeftResize);
        }

        public static Window GetBottomResize(DependencyObject obj)
        {
            return (Window)obj.GetValue(BottomResize);
        }

        public static Window GetBottomRightResize(DependencyObject obj)
        {
            return (Window)obj.GetValue(BottomRightResize);
        }

        public static Window GetLeftResize(DependencyObject obj)
        {
            return (Window)obj.GetValue(LeftResize);
        }

        public static Window GetRightResize(DependencyObject obj)
        {
            return (Window)obj.GetValue(RightResize);
        }

        public static Window GetTopLeftResize(DependencyObject obj)
        {
            return (Window)obj.GetValue(TopLeftResize);
        }

        public static Window GetTopResize(DependencyObject obj)
        {
            return (Window)obj.GetValue(TopResize);
        }

        public static Window GetTopRightResize(DependencyObject obj)
        {
            return (Window)obj.GetValue(TopRightResize);
        }

        public static void SetBottomLeftResize(DependencyObject obj, Window window)
        {
            obj.SetValue(BottomLeftResize, window);
        }

        public static void SetBottomResize(DependencyObject obj, Window window)
        {
            obj.SetValue(BottomResize, window);
        }

        public static void SetBottomRightResize(DependencyObject obj, Window window)
        {
            obj.SetValue(BottomRightResize, window);
        }

        public static void SetLeftResize(DependencyObject obj, Window window)
        {
            obj.SetValue(LeftResize, window);
        }

        public static void SetRightResize(DependencyObject obj, Window window)
        {
            obj.SetValue(RightResize, window);
        }

        public static void SetTopLeftResize(DependencyObject obj, Window window)
        {
            obj.SetValue(TopLeftResize, window);
        }

        public static void SetTopResize(DependencyObject obj, Window window)
        {
            obj.SetValue(TopResize, window);
        }

        public static void SetTopRightResize(DependencyObject obj, Window window)
        {
            obj.SetValue(TopRightResize, window);
        }

        private static void DragBottom(object sender, DragDeltaEventArgs e)
        {
            var thumb = sender as Thumb;

            if (thumb.GetValue(BottomResize) is Window window)
            {
                var verticalChange = window.SafeHeightChange(e.VerticalChange);
                window.Height += verticalChange;
            }
        }

        private static void DragBottomLeft(object sender, DragDeltaEventArgs e)
        {
            var thumb = sender as Thumb;

            if (thumb.GetValue(BottomLeftResize) is Window window)
            {
                var verticalChange = window.SafeHeightChange(e.VerticalChange);
                var horizontalChange = window.SafeWidthChange(e.HorizontalChange, false);

                window.Width -= horizontalChange;
                window.Left += horizontalChange;
                window.Height += verticalChange;
            }
        }

        private static void DragBottomRight(object sender, DragDeltaEventArgs e)
        {
            var thumb = sender as Thumb;

            if (thumb.GetValue(BottomRightResize) is Window window)
            {
                var verticalChange = window.SafeHeightChange(e.VerticalChange);
                var horizontalChange = window.SafeWidthChange(e.HorizontalChange);

                window.Width += horizontalChange;
                window.Height += verticalChange;
            }
        }

        private static void DragLeft(object sender, DragDeltaEventArgs e)
        {
            var thumb = sender as Thumb;

            if (thumb.GetValue(LeftResize) is Window window)
            {
                var horizontalChange = window.SafeWidthChange(e.HorizontalChange, false);
                window.Width -= horizontalChange;
                window.Left += horizontalChange;
            }
        }

        private static void DragRight(object sender, DragDeltaEventArgs e)
        {
            var thumb = sender as Thumb;

            if (thumb.GetValue(RightResize) is Window window)
            {
                var horizontalChange = window.SafeWidthChange(e.HorizontalChange);
                window.Width += horizontalChange;
            }
        }

        private static void DragTop(object sender, DragDeltaEventArgs e)
        {
            var thumb = sender as Thumb;

            if (thumb.GetValue(TopResize) is Window window)
            {
                var verticalChange = window.SafeHeightChange(e.VerticalChange, false);
                window.Height -= verticalChange;
                window.Top += verticalChange;
            }
        }

        private static void DragTopLeft(object sender, DragDeltaEventArgs e)
        {
            var thumb = sender as Thumb;

            if (thumb.GetValue(TopLeftResize) is Window window)
            {
                var verticalChange = window.SafeHeightChange(e.VerticalChange, false);
                var horizontalChange = window.SafeWidthChange(e.HorizontalChange, false);

                window.Width -= horizontalChange;
                window.Left += horizontalChange;
                window.Height -= verticalChange;
                window.Top += verticalChange;
            }
        }

        private static void DragTopRight(object sender, DragDeltaEventArgs e)
        {
            var thumb = sender as Thumb;

            if (thumb.GetValue(TopRightResize) is Window window)
            {
                var verticalChange = window.SafeHeightChange(e.VerticalChange, false);
                var horizontalChange = window.SafeWidthChange(e.HorizontalChange);

                window.Width += horizontalChange;
                window.Height -= verticalChange;
                window.Top += verticalChange;
            }
        }

        private static void OnBottomLeftResizeChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is Thumb thumb)
            {
                thumb.DragDelta += DragBottomLeft;
            }
        }

        private static void OnBottomResizeChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is Thumb thumb)
            {
                thumb.DragDelta += DragBottom;
            }
        }

        private static void OnBottomRightResizeChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is Thumb thumb)
            {
                thumb.DragDelta += DragBottomRight;
            }
        }

        private static void OnLeftResizeChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is Thumb thumb)
            {
                thumb.DragDelta += DragLeft;
            }
        }

        private static void OnRightResizeChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is Thumb thumb)
            {
                thumb.DragDelta += DragRight;
            }
        }

        private static void OnTopLeftResizeChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is Thumb thumb)
            {
                thumb.DragDelta += DragTopLeft;
            }
        }

        private static void OnTopResizeChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is Thumb thumb)
            {
                thumb.DragDelta += DragTop;
            }
        }

        private static void OnTopRightResizeChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is Thumb thumb)
            {
                thumb.DragDelta += DragTopRight;
            }
        }

        private static double SafeHeightChange(this Window window, double change, bool positive = true)
        {
            var result = positive ? window.Height + change : window.Height - change;

            if (result <= window.MinHeight)
            {
                return 0;
            }
            else if (result >= window.MaxHeight)
            {
                return 0;
            }
            else if (result < 0)
            {
                return 0;
            }
            else
            {
                return change;
            }
        }

        private static double SafeWidthChange(this Window window, double change, bool positive = true)
        {
            var result = positive ? window.Width + change : window.Width - change;

            if (result <= window.MinWidth)
            {
                return 0;
            }
            else if (result >= window.MaxWidth)
            {
                return 0;
            }
            else if (result < 0)
            {
                return 0;
            }
            else
            {
                return change;
            }
        }

        #endregion Methods
    }

    public class WindowCloseCommand : ICommand
    {
        #region Events

        public event EventHandler CanExecuteChanged;

        #endregion Events

        #region Methods

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            if (parameter is Window window)
            {
                window.Close();
            }
        }

        #endregion Methods
    }

    public class WindowMaximizeCommand : ICommand
    {
        #region Events

        public event EventHandler CanExecuteChanged;

        #endregion Events

        #region Methods

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            if (parameter is Window window)
            {
                if (window.WindowState == WindowState.Maximized)
                {
                    window.WindowState = WindowState.Normal;
                }
                else
                {
                    window.WindowState = WindowState.Maximized;
                }
            }
        }

        #endregion Methods
    }

    public class WindowMinimizeCommand : ICommand
    {
        #region Events

        public event EventHandler CanExecuteChanged;

        #endregion Events

        #region Methods

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            if (parameter is Window window)
            {
                window.WindowState = WindowState.Minimized;
            }
        }

        #endregion Methods
    }

    internal static class NativeMethods
    {
        #region Methods

        [DllImport("user32.dll")]
        internal static extern bool EnableMenuItem(IntPtr hMenu, uint uIDEnableItem, uint uEnable);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("user32.dll")]
        internal static extern IntPtr PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        internal static extern int TrackPopupMenuEx(IntPtr hmenu, uint fuFlags, int x, int y, IntPtr hwnd, IntPtr lptpm);

        #endregion Methods
    }

    public class IvyWindow
    {
        public static string GetIcon(DependencyObject obj)
        {
            return (string)obj.GetValue(IconProperty);
        }

        public static void SetIcon(DependencyObject obj, string value)
        {
            obj.SetValue(IconProperty, value);
        }

        public static readonly DependencyProperty IconProperty =
            DependencyProperty.RegisterAttached("Icon", typeof(string), typeof(IvyWindow), new PropertyMetadata(null));
    }
}