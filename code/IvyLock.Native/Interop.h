#pragma once

#include <msclr\marshal.h>
#include <string>

using namespace System;
using namespace System::Diagnostics;
using namespace System::ComponentModel;
using namespace std;
using namespace msclr::interop;

namespace IvyLock {
	namespace Native {
		public class NativeInterop abstract sealed {
		public:
			static Point PointFromNative(POINT native) {
				Point point = Point();
				point.x = native.x;
				point.y = native.y;
				return point;
			}

			static Message MessageFromNative(MSG native) {
				Message msg = Message();
				msg.hWnd = IntPtr(native.hwnd);
				msg.message = native.message;
				msg.wParam = native.wParam;
				msg.lParam = native.lParam;
				msg.time = native.time;
				msg.pt = PointFromNative(native.pt);
				return msg;
			}

			static CBTActivate CBTActivateFromNative(CBTACTIVATESTRUCT native) {
				CBTActivate cbta = CBTActivate();
				cbta.fMouse = native.fMouse;
				cbta.hWndActive = IntPtr(native.hWndActive);
				return cbta;
			}

			static CreateStruct CreateStructFromNative(CREATESTRUCT native) {
				CreateStruct cs = CreateStruct();
				cs.hInstance = IntPtr(native.hInstance);
				cs.hMenu = IntPtr(native.hMenu);
				cs.hwndParent = IntPtr(native.hwndParent);
				cs.cx = native.cx;
				cs.cy = native.cy;
				cs.x = native.x;
				cs.y = native.y;
				cs.style = native.style;
				/*cs.lpszName = Interop::from_wchar(native.lpszName);
				cs.lpszClass = Interop::from_wchar(native.lpszClass);*/
				cs.dwExStyle = native.dwExStyle;
				return cs;
			}

			static CBTCreateWnd CBTCreateWndFromNative(CBT_CREATEWND native) {
				CBTCreateWnd cbtcw = CBTCreateWnd();
				cbtcw.hwndInsertAfter = IntPtr(native.hwndInsertAfter);
				cbtcw.cs = CreateStructFromNative(*native.lpcs);
				return cbtcw;
			}

			static IvyLock::Native::Rect RectFromNative(RECT native) {
				Rect rect = Rect();
				rect.left = native.left;
				rect.top = native.top;
				rect.right = native.right;
				rect.bottom = native.bottom;
				return rect;
			}

			static CallWndProc CallWndProcFromNative(CWPSTRUCT native) {
				CallWndProc cwp = CallWndProc();
				cwp.lParam = native.lParam;
				cwp.wParam = native.wParam;
				cwp.message = native.message;
				cwp.hwnd = IntPtr(native.hwnd);
				return cwp;
			}
		};

		public ref class Interop abstract sealed
		{
		public:
			static String^ from_wchar(LPCWSTR wstr) {
				std::wstring ws(wstr);
				std::string str(ws.begin(), ws.end());
				return gcnew String(str.c_str());
			}

			static Rect^ GetWindowRectM(IntPtr hWnd) {
				RECT* rect;
				GetWindowRect(HWND(hWnd.ToPointer()), rect);
				Rect^ rectM = NativeInterop::RectFromNative(*rect);
				return rectM;
			}

			static Process^ GetProcessForHwnd(IntPtr hWnd) {
				HWND nativeHWND = (HWND)hWnd.ToPointer();
				LPDWORD pid;
				GetWindowThreadProcessId(nativeHWND, pid);
				DWORD dPid = *pid;
				return Process::GetProcessById(dPid);
			}
		};
	}
}