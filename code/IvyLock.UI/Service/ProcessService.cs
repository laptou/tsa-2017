using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;

namespace IvyLock.Service
{
    public delegate void ProcessChangedHandler(int pid, string path, ProcessOperation po);

    public enum ProcessOperation
    {
        Started, Modified, Deleted
    }

    public interface IProcessService : IDisposable
    {
        #region Events

        event ProcessChangedHandler ProcessChanged;

        #endregion Events

        #region Methods

        Process GetProcessById(int pid);

        IList<Process> GetProcesses();

        IList<Process> GetProcessesByName(string execName);

        #endregion Methods
    }

    public class ManagedProcessService : IProcessService
    {
        #region Fields

        private ManagementEventWatcher mew;
        private List<string> processesToMonitor = new List<string>();

        #endregion Fields

        #region Constructors

        public ManagedProcessService()
        {
            mew = new ManagementEventWatcher(
                new ManagementScope(@"\\.\root\CIMV2"),
                new WqlEventQuery(
                    "__InstanceOperationEvent",
                    new TimeSpan(0, 0, 0, 1),
                    "TargetInstance ISA \"Win32_Process\""));
            mew.Start();
            mew.EventArrived += EventArrived;
        }

        ~ManagedProcessService()
        {
            mew.Stop();
            mew.Dispose();
        }

        #endregion Constructors

        #region Events

        public event ProcessChangedHandler ProcessChanged;

        #endregion Events

        #region Properties

        public static ManagedProcessService Default { get; set; } = new ManagedProcessService();

        #endregion Properties

        #region Methods

        public Process GetProcessById(int pid)
        {
            return Process.GetProcessById(pid);
        }

        public IList<Process> GetProcesses()
        {
            return Process.GetProcesses();
        }

        public IList<Process> GetProcessesByName(string execName)
        {
            return Process.GetProcessesByName(execName);
        }

        private void EventArrived(object sender, EventArrivedEventArgs e)
        {
            ManagementBaseObject mbo = e.NewEvent["TargetInstance"] as ManagementBaseObject;

            ProcessOperation po = ProcessOperation.Started;
            switch (e.NewEvent.ClassPath.ClassName)
            {
                case "__InstanceModificationEvent":
                    po = ProcessOperation.Modified;
                    break;

                case "__InstanceDeletionEvent":
                    po = ProcessOperation.Deleted;
                    break;
            }

            ProcessChanged?.Invoke((int)(uint)mbo["ProcessId"], (string)mbo["ExecutablePath"], po);
        }

        public void Dispose()
        {
            mew.Dispose();
        }

        #endregion Methods
    }
}