using System.Collections.Generic;
using System.Diagnostics;

namespace IvyLock.Service
{
	public interface IProcessService
	{
		IList<Process> GetProcesses();

		Process GetProcessById(int pid);

		IList<Process> GetProcessesByName(string execName);
	}

	public class ManagedProcessService : IProcessService
	{
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
	}
}