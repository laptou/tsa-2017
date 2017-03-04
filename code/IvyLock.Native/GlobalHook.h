#pragma once

#include <map>
#include <allocators>
#include "Enums.h"
#include "Interop.h"

using namespace System;
using namespace System::Diagnostics;
using namespace System::Threading;
using namespace System::Threading::Tasks;
using namespace System::IO;
using namespace System::IO::Pipes;
using namespace System::Runtime::Serialization;
using namespace System::Runtime::Serialization::Formatters::Binary;
using namespace System::Linq;
using namespace System::Collections::Generic;

namespace IvyLock {
	namespace Native {
		static public ref class Keystroke {
		public:
			ref struct Info {
				short RepeatCount;
				byte ScanCode;
				bool Extended;
				bool Alt;
				bool PreviousState;
				bool TransitionState;
			};

			static Info^ DecodeLParam(long lParam) {
				Info^ info = gcnew Info;
				info->RepeatCount = lParam && 0xFFFF;
				info->ScanCode = (lParam >> 16) && 0xFF;
				info->Extended = (lParam >> 24) && 0x1;
				info->Alt = (lParam >> 29) && 0x1;
				info->PreviousState = (lParam >> 30) && 0x1;
				info->TransitionState = (lParam >> 31) && 0x1;
				return info;
			}

			static LPARAM EncodeLParam(Info^ info) {
				LPARAM lParam = 0;
				lParam += info->RepeatCount;
				lParam += info->ScanCode << 16;
				lParam += info->Extended << 24;
				lParam += info->Alt << 29;
				lParam += info->PreviousState << 30;
				lParam += info->TransitionState << 31;
				return lParam;
			}
		};

		public ref class HookCallbackInfo {
		public:
			property int Process;
			property HookType Type;
			property int nCode;
			property UInt32 wParam;
			property long lParam;
			property bool CallNext;
			property IntPtr ReturnValue;
			property Object^ Extra;

			HookCallbackInfo() {
				CallNext = true;
				ReturnValue = IntPtr(1);
			}
		};

		public delegate HookCallbackInfo^ HookCallback(HookCallbackInfo^ info);

		static public ref class GlobalHook
		{
		private:
			static List<Thread^>^ threads;
		public:
			static IntPtr SetHook(HookType hookType, HookCallback^ callback);
			static void ReleaseHook(HookType hookType);
			static Dictionary<HookType, List<HookCallback^>^>^ Hooks;
			static GlobalHook() {
				Hooks = gcnew Dictionary<HookType, List<HookCallback^>^>;
			}

			static void Initialize() {
				threads = gcnew List<Thread^>;

				Thread^ thread = gcnew Thread(gcnew ThreadStart(PollThread));
				thread->Priority = ThreadPriority::BelowNormal;
				thread->Start();
			}

			static void Stop() {
				for each (HookType type in Enumerable::ToList(Hooks->Keys))
				{
					ReleaseHook(type);
				}

				for each (Thread^ thread in threads)
				{
					if (thread->IsAlive)
						thread->Abort();
				}
			}

			static void PollThread() {
				try {
					threads->Add(Thread::CurrentThread);

					NamedPipeServerStream^ npss = gcnew NamedPipeServerStream(
						GlobalHook::Pipe,
						PipeDirection::InOut,
						NamedPipeServerStream::MaxAllowedServerInstances,
						PipeTransmissionMode::Message,
						PipeOptions::Asynchronous);

					npss->BeginWaitForConnection(gcnew AsyncCallback(PollThreadAsync), npss);
				}
				catch (Exception^ ex) {
					Console::WriteLine("NPSS Error: {0} ({1})", ex->Message, ex->GetType()->FullName);
					Console::WriteLine(ex->StackTrace);
				}
			}

			static void PollThreadAsync(IAsyncResult^ iar) {
				NamedPipeServerStream^ npss = (NamedPipeServerStream^)iar->AsyncState;

				npss->EndWaitForConnection(iar);

				npss->ReadMode = PipeTransmissionMode::Message;

				Thread^ thread = gcnew Thread(gcnew ThreadStart(PollThread));
				thread->Priority = ThreadPriority::BelowNormal;
				thread->Start();

				MemoryStream^ npms = gcnew MemoryStream;
				array<byte>^ buffer = gcnew array<byte>(1000);

				StreamReader^ sr = gcnew StreamReader(npss);
				StreamWriter^ sw = gcnew StreamWriter(npss);

				String^ line = sr->ReadLine();

				if (line == nullptr || String::IsNullOrWhiteSpace(line))
					return;

				array<String^>^ data = line->Split();
				HookCallbackInfo^ info = gcnew HookCallbackInfo;

				info->Process = Int32::Parse(data[0]);
				info->Type = (HookType)Int32::Parse(data[1]);
				info->nCode = Int32::Parse(data[2]);
				info->wParam = UInt32::Parse(data[3]);
				info->lParam = Int32::Parse(data[4]);

				String^ extraLine = sr->ReadLine();

				if (!String::IsNullOrWhiteSpace(extraLine)) {
					IFormatter^ formatter = gcnew BinaryFormatter;
					MemoryStream^ ms = gcnew MemoryStream(Convert::FromBase64String(extraLine));
					info->Extra = formatter->Deserialize(ms);
				}

				if (Hooks->ContainsKey(info->Type)) {
					List<HookCallback^>^ callbacks = Hooks[info->Type];
					for each (HookCallback^ callback in callbacks)
					{
						try {
							info = callback(info);
						}
						catch (Exception^) {
							break;
						}

						if (!info->CallNext) break;
					}
				}
				else {
					Console::WriteLine("Hook with no callback: " + info->Type.ToString());
				}

				sw->WriteLine(String::Format("{0}\t{1}\t{2}\t{3}\t{4}",
					info->nCode,
					info->wParam,
					info->lParam,
					info->CallNext,
					info->ReturnValue));
				sw->Flush();
				npss->WaitForPipeDrain();
				npss->Close();

				threads->Remove(Thread::CurrentThread);
			}

			property static String^ Pipe { String^ get() { return "IVYLOCK-NATIVE"; }; };
		};

		static private class GlobalHookImpl {
		private:
			static std::map<int, HHOOK> Hooks;
		public:
			static HHOOK GetHook(int hookType);
			static HHOOK SetHook(int hookType);
			static void ReleaseHook(int hookType);
		};
	}
}
