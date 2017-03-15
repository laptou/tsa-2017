#include "stdafx.h"
#include "GlobalHook.h"
#include "blocking_queue.h"
#include <Windows.h>

using namespace IvyLock::Native;
using namespace IvyLock::Native::ARCH;
using namespace System::Runtime::InteropServices;
using namespace msclr::interop;

std::map<int, HHOOK> IvyLock::Native::ARCH::GlobalHookImpl::Hooks;

bool queueThreadRunning = false;
bool procThreadRunning = false;

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

void HookQueueThread() {
	if (queueThreadRunning) return;

	queueThreadRunning = true;
	try {
		NamedPipeClientStream^ npcs = gcnew NamedPipeClientStream(".", GlobalHook::Pipe, PipeDirection::InOut);

		npcs->Connect(250);

		System::Diagnostics::Process^ currentProcess = System::Diagnostics::Process::GetCurrentProcess();
		StreamWriter^ sw = gcnew StreamWriter(npcs);
		IFormatter^ serialiser = gcnew BinaryFormatter;

		while (true)
		{
			HookCallbackInfo^ hci = nullptr;
			try {
				CancellationTokenSource^ cts = gcnew CancellationTokenSource(1000);
				hci = GlobalHook::queue->Take(cts->Token);
			}
			catch (OperationCanceledException^) {
				continue;
			}

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

			String^ line = String::Format(
				"{4}\t{3}\t{0}\t{1}\t{2}",
				nCode,
				wParam,
				lParam,
				(int)hookType,
				currentProcess->Id
			);
			// Console::WriteLine(line);
			sw->WriteLine(line);
			sw->WriteLine(base64);
			sw->Flush();
		}
	}
	catch (TimeoutException^) {}
	catch (IOException^) {}
	catch (Exception^ ex) {
		if (Debugger::IsAttached)
			Debugger::Break();

		queueThreadRunning = false;
	}
}

DWORD WINAPI HookProcThread(LPVOID lpParam) {
	if (!queueThreadRunning) (gcnew Thread(gcnew ThreadStart(HookQueueThread)))->Start();
	if (procThreadRunning) return 0;

	//HookCallbackInfoNative* hcin = (HookCallbackInfoNative*)lpParam;
	// GlobalHook::queue->Add(hcin->toManaged());

	return 0;
}

LRESULT HookProc(UINT32 hookType, int nCode, WPARAM wParam, LPARAM lParam) {
	// can't use the CLR here or we end up with a problem when the user tries to start a new process
	HookCallbackInfoNative* hcin = new HookCallbackInfoNative();
	hcin->hookType = hookType;
	hcin->nCode = nCode;
	hcin->wParam = wParam;
	hcin->lParam = lParam;

	// so gotta create thread the native way, and then use CLR in our fancy new thread
	if(!procThreadRunning)
		CreateThread(NULL, 0, HookProcThread, NULL, 0, NULL);

	GlobalHook::queue->Add(hcin->toManaged());

	return 0; 
}

HHOOK IvyLock::Native::ARCH::GlobalHookImpl::GetHook(int hookType)
{
	return GlobalHookImpl::Hooks.at(hookType);
}

HHOOK IvyLock::Native::ARCH::GlobalHookImpl::SetHook(int hookType)
{
	HOOKPROC proc = HOOKPROC();

#define HookCase(type) \
case type: \
	proc = [](int nCode, WPARAM wParam, LPARAM lParam) -> LRESULT { \
		return HookProc((UINT32)type, nCode, wParam, lParam); \
	}; \
	break;

	switch ((HookType)hookType)
	{
		HookCase(HookType::CallWndProc)
		HookCase(HookType::CallWndProcRet)
		HookCase(HookType::CBT)
		HookCase(HookType::Debug)
		HookCase(HookType::ForegroundIdle)
		HookCase(HookType::GetMessage)
		HookCase(HookType::Hardware)
		HookCase(HookType::JournalPlayback)
		HookCase(HookType::JournalRecord)
		HookCase(HookType::Keyboard)
		HookCase(HookType::KeyboardLowLevel)
		HookCase(HookType::Mouse)
		HookCase(HookType::MouseLowLevel)
		HookCase(HookType::Shell)
		HookCase(HookType::SysMsgFilter)
	}

	//HWND hwnd = FindWindow(NULL, TEXT("Telegram"));
	//DWORD threadId = GetWindowThreadProcessId(hwnd, NULL);

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