﻿using IvyLock.Native.x64;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace IvyLock.TestConsole
{
	internal class Program
	{
		[DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool IsWow64Process([In] IntPtr process, [Out] out bool wow64Process);
		
		private static void Main(string[] args)
		{
			if (Environment.Is64BitProcess)
				Console.WriteLine("Running in 64-bit mode");
			else
				Console.WriteLine("Running in 32-bit mode");

			try
			{
				GlobalHook.Start();

				Func<int, string> formatProcess = pid =>
				{
					Process process = Process.GetProcessById(pid);
					bool x64;
					IsWow64Process(process.Handle, out x64);
					return string.Format("{0} [{1}] ({2})",
						Path.GetFileName(process.MainModule.FileVersionInfo.FileName),
						process.MainModule.FileVersionInfo.FileDescription,
						x64 ? "x64" : "x86");
				};

				Process runner = null;
				CancellationTokenSource cts = new CancellationTokenSource();

				if (Environment.Is64BitProcess)
				{
					ProcessStartInfo psi = new ProcessStartInfo("IvyLock.Win32Runner.exe", "CBT");
					psi.UseShellExecute = false;
					psi.RedirectStandardError = true;
					psi.RedirectStandardOutput = true;
					runner = Process.Start(psi);
					
					Task.Factory.StartNew(() => 
					{
						while (true)
						{
							string line = runner.StandardError.ReadLine();
							if(!string.IsNullOrWhiteSpace(line))
								Console.WriteLine(line);
						}
					}, cts.Token);
				}

				IntPtr cbt = GlobalHook.SetHook(HookType.CBT, info =>
				{
					try
					{
						CBTType type = (CBTType)info.nCode;
						switch (type)
						{
							case CBTType.Activate:
								CBTActivate cbta = (CBTActivate)info.Extra;
								string cbtas = String.Format("{{ Mouse: {0} }}", cbta.fMouse);
								Console.WriteLine(string.Format(
									"CBT\t{3}\t{0}\t{1}\t{2}", type, info.wParam, cbtas, formatProcess(info.Process)
									));
								break;

							case CBTType.CreateWnd:
								CBTCreateWnd cbtcw = (CBTCreateWnd)info.Extra;
								string cbtcws = String.Format("{{ x: {0} y: {1} cx: {2} cy: {3} style: {4} }}",
									cbtcw.cs.x, cbtcw.cs.y, cbtcw.cs.cx, cbtcw.cs.cy, (WindowStyle)(cbtcw.cs.style));

								Console.WriteLine(string.Format(
									"CBT\t{3}\t{0}\t{1}\t{2}", type, info.wParam, cbtcws, formatProcess(info.Process)
									));
								break;

							case CBTType.MoveSize:
								Rect r = (Rect)info.Extra;
								string rs = String.Format("{{ left: {0} top: {1} right: {2} bottom: {3} }}",
									r.left, r.top, r.right, r.bottom);
								Console.WriteLine(string.Format(
									"CBT\t{3}\t{0}\t{1}\t{2}", type, info.wParam, rs, formatProcess(info.Process)
									));
								break;

							case CBTType.KeySkipped:
								Keystroke.Info kinf = Keystroke.DecodeLParam(info.lParam);
								string kinfs = string.Format(
									"{{ RepeatCount: {0} Alt: {1} Extended: {2} PreviousState: {3} ScanCode: {4} }}",
									kinf.RepeatCount, kinf.Alt, kinf.Extended,
									kinf.PreviousState, kinf.ScanCode);
								Console.WriteLine(string.Format(
									"CBT\t{3}\t{0}\t{1}\t{2}", type, (VirtualKey)info.wParam, kinfs, formatProcess(info.Process)
									));
								break;

							case CBTType.SysCommand:
								Console.WriteLine(string.Format(
									"CBT\t{3}\t{0}\t{1}\t{2}", type, (SystemCommand)info.wParam, info.lParam, formatProcess(info.Process)
									));
								break;

							default:
								Console.WriteLine(string.Format(
									"CBT\t{3}\t{0}\t{1}\t{2}", type, info.wParam, info.lParam, formatProcess(info.Process)
									));
								break;
						}
					}
					catch (Exception ex)
					{
						Console.Write(ex);
					}
					return info;
				});

				if (cbt == IntPtr.Zero)
					throw new Win32Exception(Marshal.GetLastWin32Error());

				Console.WriteLine("CBT Hooked: {0}", cbt);

				Console.ReadLine();

				GlobalHook.Stop();

				if (runner != null)
				{
					runner.Kill();
					cts.Cancel();
				}
			}
			catch (Win32Exception w32ex)
			{
				Console.WriteLine("Win32 Error: {0} ({1})", w32ex.Message, w32ex.NativeErrorCode);

				Console.ReadLine();
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error: {0} ({1})", ex.Message, ex.GetType().FullName);
				Console.WriteLine(ex.StackTrace);

				Console.ReadLine();
			}
		}
	}
}