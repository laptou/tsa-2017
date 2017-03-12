#pragma once

#include "Enums.h"
#include <msclr\marshal.h>
#include <string>

using namespace System;
using namespace System::Diagnostics;
using namespace System::ComponentModel;
using namespace std;
using namespace msclr::interop;

namespace IvyLock {
	namespace Native {
		namespace ARCH {
			[Serializable]
			public ref struct Point {
				LONG  x;
				LONG  y;

				Point() {
				}

				Point(POINT native) {
					this->x = native.x;
					this->y = native.y;
				}
			};

			[Serializable]
			public ref struct Message {
				IntPtr hWnd;
				UINT   message;
				WPARAM wParam;
				LPARAM lParam;
				DWORD  time;
				Point^  pt;

				Message() {
				}

				Message(MSG native) {
					this->hWnd = IntPtr(native.hwnd);
					this->message = native.message;
					this->wParam = native.wParam;
					this->lParam = native.lParam;
					this->time = native.time;
					this->pt = gcnew Point(native.pt);
				}
			};

			[Serializable]
			public ref struct CBTActivate {
				bool fMouse;
				IntPtr hWndActive;

				CBTActivate() {
				}

				CBTActivate(CBTACTIVATESTRUCT native) {
					this->fMouse = native.fMouse;
					this->hWndActive = IntPtr(native.hWndActive);
				}

				/*String^ ToString() override {
					return String::Format("{ Mouse: {0} }", this->fMouse);
				}*/
			};

			[Serializable]
			public ref struct CreateStruct {
				IntPtr hInstance;
				IntPtr hMenu;
				IntPtr hwndParent;
				int       cy;
				int       cx;
				int       y;
				int       x;
				LONG      style;
				String^   lpszName;
				String^   lpszClass;
				DWORD     dwExStyle;

				CreateStruct() {}

				CreateStruct(CREATESTRUCT native) {
					this->hInstance = IntPtr(native.hInstance);
					this->hMenu = IntPtr(native.hMenu);
					this->hwndParent = IntPtr(native.hwndParent);
					this->cx = native.cx;
					this->cy = native.cy;
					this->x = native.x;
					this->y = native.y;
					this->style = native.style;
					/*this->lpszName = Interop::from_wchar(native.lpszName);
					this->lpszClass = Interop::from_wchar(native.lpszClass);*/
					this->dwExStyle = native.dwExStyle;
				}

				/*String^ ToString() override {
					return String::Format("{ x: {0} y: {1} cx: {2} cy: {3} style: {4} }",
						this->x, this->y, this->cx, this->cy, (WindowStyle)(this->style));
				}*/
			};

			[Serializable]
			public ref struct CBTCreateWnd {
				IntPtr hwndInsertAfter;
				CreateStruct^ cs;

				CBTCreateWnd() {}

				CBTCreateWnd(CBT_CREATEWND native) {
					this->hwndInsertAfter = IntPtr(native.hwndInsertAfter);
					this->cs = gcnew CreateStruct(*native.lpcs);
				}

				/*String^ ToString() override {
					return String::Format("{ cs: {0} }", this->cs->ToString());
				}*/
			};

			[Serializable]
			public ref struct Rect {
				LONG left;
				LONG top;
				LONG right;
				LONG bottom;

				Rect() {}

				Rect(RECT native) {
					this->left = native.left;
					this->top = native.top;
					this->right = native.right;
					this->bottom = native.bottom;
				}

				/*String^ ToString() override {
					return String::Format("{ left: {0} top: {1} right: {2} bottom: {3} }",
						this->left, this->top, this->right, this->bottom);
				}*/
			};

			[Serializable]
			public ref struct CallWndProc {
				LPARAM lParam;
				WPARAM wParam;
				UINT   message;
				IntPtr   hwnd;

				CallWndProc() {}

				CallWndProc(CWPSTRUCT native) {
					this->lParam = native.lParam;
					this->wParam = native.wParam;
					this->message = native.message;
					this->hwnd = IntPtr(native.hwnd);
				}
			};

			static public ref class Interop
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
					Rect^ rectM = gcnew Rect(*rect);
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
}
