#include "stdafx.h"
#include "GlobalHook.h"
#include <Windows.h>
#include <msclr\marshal.h>

using namespace IvyLock::Native::ARCH;
using namespace System::Runtime::InteropServices;
using namespace msclr::interop;

std::map<int, HHOOK> IvyLock::Native::ARCH::GlobalHookImpl::Hooks;

bool queueThreadRunning = false;

HMODULE WINAPI ModuleFromAddress(PVOID pv)
{
	MEMORY_BASIC_INFORMATION mbi;
	if (::VirtualQuery(pv, &mbi, sizeof(mbi)) != 0)
	{
		return (HMODULE)mbi.AllocationBase;
	}
	else
	{
		return NULL;
	}
}

HMODULE GetCurrentModule()
{ // NB: XP+ solution!
	HMODULE hModule = NULL;
	GetModuleHandleEx(
		GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS,
		(LPCTSTR)GetCurrentModule,
		&hModule);

	return hModule;
}

void HookThread() {
	queueThreadRunning = true;
	try {
		NamedPipeClientStream^ npcs = gcnew NamedPipeClientStream(".", GlobalHook::Pipe, PipeDirection::InOut);

		npcs->Connect(250);

		System::Diagnostics::Process^ currentProcess = System::Diagnostics::Process::GetCurrentProcess();
		StreamWriter^ sw = gcnew StreamWriter(npcs);
		IFormatter^ serialiser = gcnew BinaryFormatter;
		CancellationTokenSource^ cts = gcnew CancellationTokenSource(1000);

		while (true)
		{
			HookCallbackInfo^ hci = GlobalHook::queue->Take(cts->Token);

			HookType hookType = hci->Type;
			int nCode = hci->nCode;
			WPARAM wParam = hci->wParam;
			LPARAM lParam = hci->lParam;
			Object^ extra = hci->Extra;
			String^ base64 = "";

			if (extra != nullptr) {

				MemoryStream^ ms = gcnew MemoryStream;
				serialiser->Serialize(ms, extra);
				base64 = Convert::ToBase64String(ms->ToArray());
				ms->Close();
			}

			sw->WriteLine(String::Format(
				"{4}\t{3}\t{0}\t{1}\t{2}",
				nCode,
				wParam,
				lParam,
				(int)hookType,
				currentProcess->Id
				)); 
			sw->WriteLine(base64);
			sw->Flush();
		}
	}
	catch (TimeoutException^) {}
	catch (OperationCanceledException^) {}
	catch (Exception^ ex) {

		while (ex->InnerException != nullptr)
			ex = ex->InnerException;

		marshal_context^ mc = gcnew marshal_context;
		MessageBox(NULL,
			mc->marshal_as<LPCTSTR>(ex->Message + "\n" + ex->StackTrace),
			mc->marshal_as<LPCTSTR>(ex->GetType()->FullName), MB_OK);

		queueThreadRunning = false;
	}
}

LRESULT HookProc(HookType hookType, int nCode, WPARAM wParam, LPARAM lParam) {
	try {
		if (!queueThreadRunning) {
			Thread^ thread = gcnew Thread(gcnew ThreadStart(HookThread));
			thread->Start();
		}

		HookCallbackInfo^ info = gcnew HookCallbackInfo();
		info->nCode = nCode;
		info->wParam = wParam;
		info->lParam = lParam;
		info->Type = hookType;

		Object^ extra = nullptr;
		if (hookType == HookType::GetMessage) {
			MSG* msg = (MSG*)lParam;

			if (msg->message != WM_CREATE)
				return CallNextHookEx(NULL, nCode, wParam, lParam);

			extra = gcnew Message(*msg);
		}

		if (hookType == HookType::CallWndProc) {
			CWPSTRUCT *cwp = (CWPSTRUCT*)lParam;

			if (cwp->message != WM_CREATE)
				return CallNextHookEx(NULL, nCode, wParam, lParam);

			extra = gcnew CallWndProc(*cwp);
		}

		if (hookType == HookType::CBT) {
			switch (nCode)
			{
			case HCBT_ACTIVATE:
				extra = gcnew CBTActivate(*(CBTACTIVATESTRUCT*)lParam);
				break;
			case HCBT_CREATEWND:
				// CBT_CREATEWND* cbtcw = (CBT_CREATEWND*)lParam;
				extra = gcnew CBTCreateWnd(*(CBT_CREATEWND*)lParam);
				break;
			case HCBT_MOVESIZE:
				// RECT* r = (RECT*)lParam;
				extra = gcnew Rect(*(RECT*)lParam);
				break;
			default:
				break;
			}
		}

		info->Extra = extra;

		GlobalHook::queue->Add(info);
	}
	finally {
	}

	return CallNextHookEx(NULL, nCode, wParam, lParam);
}

HHOOK IvyLock::Native::ARCH::GlobalHookImpl::GetHook(int hookType)
{
	return GlobalHookImpl::Hooks.at(hookType);
}

HHOOK IvyLock::Native::ARCH::GlobalHookImpl::SetHook(int hookType)
{
	HOOKPROC proc = HOOKPROC();

	switch ((HookType)hookType)
	{
	case HookType::CallWndProc:
		proc = [](int nCode, WPARAM wParam, LPARAM lParam) -> LRESULT { return HookProc(HookType::CallWndProc, nCode, wParam, lParam); };
		break;
	case HookType::CallWndProcRet:
		proc = [](int nCode, WPARAM wParam, LPARAM lParam) -> LRESULT { return HookProc(HookType::CallWndProcRet, nCode, wParam, lParam); };
		break;
	case HookType::CBT:
		proc = [](int nCode, WPARAM wParam, LPARAM lParam) -> LRESULT { return HookProc(HookType::CBT, nCode, wParam, lParam); };
		break;
	case HookType::Debug:
		proc = [](int nCode, WPARAM wParam, LPARAM lParam) -> LRESULT { return HookProc(HookType::Debug, nCode, wParam, lParam); };
		break;
	case HookType::ForegroundIdle:
		proc = [](int nCode, WPARAM wParam, LPARAM lParam) -> LRESULT { return HookProc(HookType::ForegroundIdle, nCode, wParam, lParam); };
		break;
	case HookType::GetMessage:
		proc = [](int nCode, WPARAM wParam, LPARAM lParam) -> LRESULT { return HookProc(HookType::GetMessage, nCode, wParam, lParam); };
		break;
	case HookType::Hardware:
		proc = [](int nCode, WPARAM wParam, LPARAM lParam) -> LRESULT { return HookProc(HookType::Hardware, nCode, wParam, lParam); };
		break;
	case HookType::JournalPlayback:
		proc = [](int nCode, WPARAM wParam, LPARAM lParam) -> LRESULT { return HookProc(HookType::JournalPlayback, nCode, wParam, lParam); };
		break;
	case HookType::JournalRecord:
		proc = [](int nCode, WPARAM wParam, LPARAM lParam) -> LRESULT { return HookProc(HookType::JournalRecord, nCode, wParam, lParam); };
		break;
	case HookType::Keyboard:
		proc = [](int nCode, WPARAM wParam, LPARAM lParam) -> LRESULT { return HookProc(HookType::Keyboard, nCode, wParam, lParam); };
		break;
	case HookType::KeyboardLowLevel:
		proc = [](int nCode, WPARAM wParam, LPARAM lParam) -> LRESULT { return HookProc(HookType::KeyboardLowLevel, nCode, wParam, lParam); };
		break;
	case HookType::Mouse:
		proc = [](int nCode, WPARAM wParam, LPARAM lParam) -> LRESULT { return HookProc(HookType::Mouse, nCode, wParam, lParam); };
		break;
	case HookType::MouseLowLevel:
		proc = [](int nCode, WPARAM wParam, LPARAM lParam) -> LRESULT { return HookProc(HookType::MouseLowLevel, nCode, wParam, lParam); };
		break;
	case HookType::Shell:
		proc = [](int nCode, WPARAM wParam, LPARAM lParam) -> LRESULT { return HookProc(HookType::Shell, nCode, wParam, lParam); };
		break;
	case HookType::SysMsgFilter:
		proc = [](int nCode, WPARAM wParam, LPARAM lParam) -> LRESULT { return HookProc(HookType::SysMsgFilter, nCode, wParam, lParam); };
		break;
	}

	HHOOK hook = SetWindowsHookEx(hookType, proc, GetCurrentModule(), 0);
	GlobalHookImpl::Hooks.insert(std::make_pair(hookType, hook));
	return hook;
}

void IvyLock::Native::ARCH::GlobalHookImpl::ReleaseHook(int hookType)
{
	UnhookWindowsHookEx(GlobalHookImpl::GetHook(hookType));
	GlobalHookImpl::Hooks.erase(hookType);
}

IntPtr IvyLock::Native::ARCH::GlobalHook::SetHook(HookType hookType, HookCallback ^ callback)
{
	if (!GlobalHook::Hooks->ContainsKey(hookType))
		GlobalHook::Hooks->Add(hookType, gcnew List<HookCallback^>);

	GlobalHook::Hooks[hookType]->Add(callback);
	return IntPtr(GlobalHookImpl::SetHook((int)hookType));
}

void IvyLock::Native::ARCH::GlobalHook::ReleaseHook(HookType hookType)
{
	if (!GlobalHook::Hooks->ContainsKey(hookType))
		return;

	GlobalHook::Hooks->Remove(hookType);
	GlobalHookImpl::ReleaseHook((int)hookType);
}