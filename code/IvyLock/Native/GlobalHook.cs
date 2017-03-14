using System;
using System.Linq;

namespace IvyLock.Native
{
	public delegate HookCallbackInfo HookCallback(HookCallbackInfo info);

	[Serializable]
	public struct CallWndProc
	{
		#region Fields

		public IntPtr hwnd;
		public long lParam;
		public uint message;
		public ulong wParam;

		#endregion Fields
	}

	[Serializable]
	public struct CBTActivate
	{
		#region Fields

		public bool fMouse;
		public IntPtr hWndActive;

		#endregion Fields
	}

	[Serializable]
	public struct CBTCreateWnd
	{
		#region Fields

		public CreateStruct cs;
		public IntPtr hwndInsertAfter;

		#endregion Fields
	}

	[Serializable]
	public struct CreateStruct
	{
		#region Fields

		public int cx;
		public int cy;
		public uint dwExStyle;
		public IntPtr hInstance;
		public IntPtr hMenu;
		public IntPtr hwndParent;
		public string lpszClass;
		public string lpszName;
		public int style;
		public int x;
		public int y;

		#endregion Fields
	}

	[Serializable]
	public struct Message
	{
		#region Fields

		public IntPtr hWnd;
		public long lParam;
		public uint message;
		public Point pt;
		public uint time;
		public ulong wParam;

		#endregion Fields
	}

	[Serializable]
	public struct Point
	{
		#region Fields

		public int x;
		public int y;

		#endregion Fields
	}

	[Serializable]
	public struct Rect
	{
		#region Fields

		public int bottom;
		public int left;
		public int right;
		public int top;

		#endregion Fields
	}

	public static class Keystroke
	{
		#region Methods

		public static Info DecodeLParam(long lParam)
		{
			Info info = new Info();
			info.RepeatCount = (short)(lParam & 0xFFFF);
			info.ScanCode = (byte)((lParam >> 15) & 0xFF);
			info.Extended = ((lParam >> 23) & 0x1) == 1;
			info.Alt = ((lParam >> 28) & 0x1) == 1;
			info.PreviousState = ((lParam >> 29) & 0x1) == 1;
			info.TransitionState = ((lParam >> 30) & 0x1) == 1;
			return info;
		}

		public static long EncodeLParam(Info info)
		{
			long lParam = 0;
			lParam += info.RepeatCount;
			lParam += info.ScanCode << 16;
			lParam += (info.Extended ? 1 : 0) << 24;
			lParam += (info.Alt ? 1 : 0) << 29;
			lParam += (info.PreviousState ? 1 : 0) << 30;
			lParam += (info.TransitionState ? 1 : 0) << 31;
			return lParam;
		}

		#endregion Methods

		#region Classes

		public class Info
		{
			#region Fields

			public bool Alt;
			public bool Extended;
			public bool PreviousState;
			public short RepeatCount;
			public byte ScanCode;
			public bool TransitionState;

			#endregion Fields
		}

		#endregion Classes
	}

	public class HookCallbackInfo
	{
		#region Constructors

		public HookCallbackInfo()
		{
			CallNext = true;
			ReturnValue = new IntPtr(1);
		}

		#endregion Constructors

		#region Properties

		public bool CallNext { get; set; }
		public object Extra { get; set; }
		public long lParam { get; set; }
		public int nCode { get; set; }
		public int Process { get; set; }
		public IntPtr ReturnValue { get; set; }
		public HookType Type { get; set; }
		public ulong wParam { get; set; }

		#endregion Properties
	}
}