using IvyLock.Model;
using IvyLock.Service;
using IvyLock.View;
using IvyLock.ViewModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using WF = System.Windows.Forms;

namespace IvyLock
{
    public partial class App : Application
    {
        #region Fields

        private static string logFile =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "IvyLock",
                DateTime.Now.ToString("M-dd-yyyy_HH-mm-ss") + ".log"
            );

        private static FileStream logStream = File.Create(logFile);

        private static Mutex mutex = new Mutex(true, "{D680C778-1943-460C-9907-25DEC5C912A9}");
        private IProcessService ips;

        private ISettingsService iss;

        private WF.NotifyIcon ni;

        private Dictionary<string, ProcessAuthenticationView> views = new Dictionary<string, ProcessAuthenticationView>();

        #endregion Fields

        #region Constructors

        public App()
        {
            InitializeComponent();
        }

        #endregion Constructors

        #region Properties

        public static bool IsDesigner { get { return LicenseManager.UsageMode == LicenseUsageMode.Designtime; } }

        #endregion Properties

        #region Methods

        public static async Task Log(Exception ex, bool inner = false)
        {
            if (!inner) Monitor.Enter(logStream);

            var (reader, writer) = logStream.OpenStream();

            if (inner)
            {
                await writer.WriteLineAsync($"\t[InnerException] {ex.GetType().FullName}: {ex?.Message}");
                await writer.WriteLineAsync("\t" + ex.StackTrace.Replace("\n", "\n\t"));
            }
            else
            {
                await writer.WriteLineAsync($"[Error, {DateTime.Now}] {ex.GetType().FullName}: {ex.Message}");
                await writer.WriteLineAsync(ex.StackTrace);

                if (ex.InnerException != null) await Log(ex.InnerException, true);
            }

            Monitor.Exit(logStream);
        }

        public void Activate() => MainWindow?.Activate();

        protected override void OnExit(ExitEventArgs e)
        {
            ips?.Dispose();
            iss?.Dispose();
            ni?.Dispose();

            base.OnExit(e);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (e.Args.Length > 0)
            {
                IEnumerable<string> args = e.Args;

                if (e.Args[0].Equals("-encrypt", StringComparison.InvariantCultureIgnoreCase))
                {
                    args = args.Skip(1);

                    foreach (string arg in args)
                    {
                        FileAuthenticationView fav = new FileAuthenticationView();
                        FileAuthenticationViewModel favm = (FileAuthenticationViewModel)fav.DataContext;
                        favm.Path = arg;
                        fav.ShowDialog();
                    }

                    Shutdown();
                }
                else
                {
                    if (e.Args[0].Equals("-decrypt", StringComparison.InvariantCultureIgnoreCase))
                        args = args.Skip(1);

                    foreach (string arg in args)
                    {
                        if (!File.Exists(arg))
                            continue;

                        FileAuthenticationView fav = new FileAuthenticationView();
                        FileAuthenticationViewModel favm = (FileAuthenticationViewModel)fav.DataContext;
                        favm.Path = arg;
                        fav.ShowDialog();
                    }

                    Shutdown();
                }

                return;
            }

            if (!IsDesigner)
            {
                ips = ManagedProcessService.Default;
                iss = XmlSettingsService.Default;

                IvyLockSettings ils = iss.OfType<IvyLockSettings>().First();
                switch (ils.Theme)
                {
                    case Theme.Light:
                        Resources.MergedDictionaries.Clear();
                        Resources.MergedDictionaries.Add(new ResourceDictionary()
                        {
                            Source = new Uri("pack://application:,,,/IvyLock;component/Content/Theme.Light.xaml")
                        });
                        break;
                }

                ips.ProcessChanged += ProcessChanged;

                MainWindow = new SettingsView();
                MainWindow.Show();
                MainWindow.Activate();

                ni = new WF.NotifyIcon();
                ni.Click += (s, e1) =>
                {
                    MainWindow?.Show();
                    MainWindow?.Activate();
                };

                ni.Icon = System.Drawing.Icon.ExtractAssociatedIcon(Assembly.GetEntryAssembly().Location);
                ni.ContextMenu = new WF.ContextMenu();
                ni.ContextMenu.MenuItems.Add("Exit", (s, e2) =>
                {
                    SettingsViewModel svm = MainWindow.DataContext as SettingsViewModel;

                    svm.PropertyChanged += (s2, e3) =>
                    {
                        if (e3.PropertyName == "CurrentScreen" && svm.CurrentScreen == ViewModel.Screen.Main)
                            Shutdown();
                    };

                    svm.CurrentScreen = ViewModel.Screen.EnterPassword;

                    MainWindow.Show();
                    MainWindow.Activate();
                });
                ni.Visible = true;
            }
        }

        [STAThread]
        private static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += async (s, e) => await Log(e.ExceptionObject as Exception);
            AppDomain.CurrentDomain.ProcessExit += (s, e) =>
            {
                if (logStream.Length == 0)
                {
                    logStream.Dispose();
                    File.Delete(logFile);
                }
            };

            try
            {
                App app = new App();
                app.Run();
            }
            catch (IOException ex) when (ex.HResult != unchecked((int)0x80070020)) // file in use
            {
            }
            catch (AggregateException ex) when (ex.InnerException.HResult != unchecked((int)0x80070020)) // file in use
            {
            }
            catch (Exception ex)
            {
                Log(ex).Wait();
            }
        }

        private void ProcessChanged(int pid, string path, ProcessOperation po)
        {
            if (po == ProcessOperation.Modified)
                return;

            ProcessSettings ps = iss.FindByPath(path);

            if (ps == null || !ps.UsePassword)
                return;

            Task.Run(() =>
            {
                switch (po)
                {
                    case ProcessOperation.Started:
                        if (path == null) return;

                        Dispatcher?.Invoke(async () =>
                        {
                            ProcessAuthenticationView av = views.ContainsKey(path) ? views[path] : new ProcessAuthenticationView();
                            ProcessAuthenticationViewModel avm = av.DataContext as ProcessAuthenticationViewModel;

                            avm.ProcessPath = path;
                            avm.Processes.Add(Process.GetProcessById(pid));

                            await avm.Lock();

                            if (!avm.Locked)
                            {
                                avm.Dispose();
                                return;
                            }

                            if (!views.ContainsKey(path))
                                av.Closed += (s, e) => views.Remove(path);

                            views[path] = av;
                            views[path].Show();
                            views[path].Activate();
                        });
                        break;

                    case ProcessOperation.Modified:
                        break;

                    case ProcessOperation.Deleted:
                        if (path == null || !views.ContainsKey(path)) return;

                        if (!Process.GetProcesses().Any(process =>
                         {
                             try
                             {
                                 return process.MainModule.FileName.Equals(path, StringComparison.InvariantCultureIgnoreCase);
                             }
                             catch (Win32Exception) { return false; }
                         }))
                        {
                            views[path].Close();
                            views.Remove(path);
                        }
                        break;

                    default:
                        break;
                }
            });
        }

        #endregion Methods
    }
}