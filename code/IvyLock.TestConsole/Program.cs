// using IveLock.Service;
using IvyLock.Native;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace IvyLock.TestConsole
{
	internal class Program
	{
		private static void Main(string[] args)
		{
			Console.Write("What are you trying to test?: ");
			string testMode = Console.ReadLine().ToLower();

			switch (testMode)
			{
				case "hooker":
					try
					{
						GlobalHook.Initialize();

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
											"CBT\t{3}\t{0}\t{1}\t{2}", type, info.wParam, cbtas, info.Process
											));
										break;

									case CBTType.CreateWnd:
										CBTCreateWnd cbtcw = (CBTCreateWnd)info.Extra;
										string cbtcws = String.Format("{{ x: {0} y: {1} cx: {2} cy: {3} style: {4} }}",
											cbtcw.cs.x, cbtcw.cs.y, cbtcw.cs.cx, cbtcw.cs.cy, (WindowStyle)(cbtcw.cs.style));

										Console.WriteLine(string.Format(
											"CBT\t{3}\t{0}\t{1}\t{2}", type, info.wParam, cbtcws, info.Process
											));
										break;

									case CBTType.MoveSize:
										Rect r = (Rect)info.Extra;
										string rs = String.Format("{{ left: {0} top: {1} right: {2} bottom: {3} }}",
											r.left, r.top, r.right, r.bottom);
										Console.WriteLine(string.Format(
											"CBT\t{3}\t{0}\t{1}\t{2}", type, info.wParam, rs, info.Process
											));
										break;

									case CBTType.KeySkipped:
										Keystroke.Info kinf = Keystroke.DecodeLParam(info.lParam);
										string kinfs = string.Format(
											"{{ RepeatCount: {0} Alt: {1} Extended: {2} PreviousState: {3} ScanCode: {4} }}",
											kinf.RepeatCount, kinf.Alt, kinf.Extended,
											kinf.PreviousState, kinf.ScanCode);
										Console.WriteLine(string.Format(
											"CBT\t{3}\t{0}\t{1}\t{2}", type, (VirtualKey)info.wParam, kinfs, info.Process
											));
										break;

									case CBTType.SysCommand:
										Console.WriteLine(string.Format(
											"CBT\t{3}\t{0}\t{1}\t{2}", type, (SystemCommand)info.wParam, info.lParam, info.Process
											));
										break;

									default:
										Console.WriteLine(string.Format(
											"CBT\t{3}\t{0}\t{1}\t{2}", type, info.wParam, info.lParam, info.Process
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

						IntPtr msg = new IntPtr(1);
						//msg = GlobalHook.SetHook(HookType.KeyboardLowLevel, info =>
						//{
						//	Console.WriteLine(string.Format(
						//				"KeyboardLL\t{3}\t{0}\t{1}\t{2}", info.Extra, info.wParam, info.lParam, info.Process
						//				));
						//	// info.wParam = (uint)"PENIS"[(counter++ / 2) % 5];
						//	return info;
						//});

						if (cbt == IntPtr.Zero || msg == IntPtr.Zero)
							throw new Win32Exception(Marshal.GetLastWin32Error());

						Console.WriteLine("CBT Hooked: {0}", cbt);
						// Console.WriteLine("Keyboard Hooked: {0}", msg);

						Console.ReadLine();

						GlobalHook.Stop();
					}
					catch (Win32Exception w32ex)
					{
						Console.WriteLine("Win32 Error: {0} ({1})", w32ex.Message, w32ex.NativeErrorCode);
					}
					catch (Exception ex)
					{
						Console.WriteLine("Error: {0} ({1})", ex.Message, ex.GetType().FullName);
						Console.WriteLine(ex.StackTrace);
					}
					break;
			}
		}
	}
}