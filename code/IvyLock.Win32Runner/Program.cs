using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IvyLock.Native.x86;
using System.Threading;

namespace IvyLock.Win32Runner
{
	class Program
	{
		static void Main(string[] args)
		{
			GlobalHook.Start();

			foreach (string arg in args)
			{
				HookType ht;
				if (!Enum.TryParse(arg, out ht)) continue;

				GlobalHook.SetHook(ht, info => info);
			}

			Thread.Sleep(-1);
		}
	}
}
