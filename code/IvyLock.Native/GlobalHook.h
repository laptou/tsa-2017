#pragma once

#include <map>
#include <allocators>
#include "Interop.h"

using namespace System;
using namespace System::Diagnostics;
using namespace System::Threading;
using namespace System::Threading::Tasks;
using namespace System::Reflection;
using namespace System::IO;
using namespace System::IO::Pipes;
using namespace System::Runtime::Serialization;
using namespace System::Runtime::Serialization::Formatters::Binary;
using namespace System::Linq;
using namespace System::Collections::Generic;
using namespace System::Collections::Concurrent;
using namespace System::Collections::ObjectModel;
using namespace IvyLock::Native;

namespace IvyLock {
	namespace Native {
		namespace ARCH {
			public ref class Binder : SerializationBinder
			{
			public:
				Type^ BindToType(String^ assemblyName, String^ typeName) override
				{
					try 
					{
						Type^ tyType = nullptr;
						List<Assembly^>^ assemblies = gcnew List<Assembly^>(AppDomain::CurrentDomain->GetAssemblies());
						Assembly^ assembly = Enumerable::FirstOrDefault<Assembly^>(assemblies, gcnew Func<Assembly^, bool>(Binder::predicate));

						if(typeName->Contains("x86") && Environment::Is64BitProcess)
							tyType = assembly->GetType(typeName->Replace("x86", "x64"));

						return tyType;
					}
					catch (Exception^)
					{
						return nullptr;
					}
				}
			private:
				static bool predicate(Assembly^ arg) {
					return arg->FullName->StartsWith("IvyLock.Native");
				}
			};

			public ref class GlobalHook abstract sealed
			{
			private:
				static ObservableCollection<Thread^>^ threads;

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

					Thread^ thread = gcnew Thread(gcnew ThreadStart(PollThread));
					thread->Name = "Server Thread";
					thread->Priority = ThreadPriority::BelowNormal;
					thread->Start();

					try {
						npss->EndWaitForConnection(iar);

						npss->ReadMode = PipeTransmissionMode::Message;

						StreamReader^ sr = gcnew StreamReader(npss);
						StreamWriter^ sw = gcnew StreamWriter(npss);

						while (npss->IsConnected) {
							String^ line = sr->ReadLine();
							// Console::WriteLine(line);

							if (line == nullptr || String::IsNullOrWhiteSpace(line))
								return;

							cli::array<String^>^ data = line->Split();
							HookCallbackInfo^ info = gcnew HookCallbackInfo;

							info->Process = int::Parse(data[0]);
							info->Type = (HookType)int::Parse(data[1]);
							info->nCode = int::Parse(data[2]);
							info->wParam = UInt32::Parse(data[3]);
							info->lParam = Int64::Parse(data[4]);

							String^ extraLine = sr->ReadLine();

							if (!String::IsNullOrWhiteSpace(extraLine)) {
								IFormatter^ formatter = gcnew BinaryFormatter;
								// formatter->Binder = gcnew Binder;
								MemoryStream^ ms = gcnew MemoryStream(Convert::FromBase64String(extraLine));
								info->Extra = formatter->Deserialize(ms);
								ms->Close();
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

							if (false && npss->IsConnected) {
								sw->WriteLine(String::Format("{0}\t{1}\t{2}\t{3}\t{4}",
									info->nCode,
									info->wParam,
									info->lParam,
									info->CallNext,
									info->ReturnValue));
								sw->Flush();
								npss->WaitForPipeDrain();
								npss->Close();
							}
						}
					}
					catch (Exception^ ex) {
						Console::WriteLine("NPSS Worker Error: {0} ({1})", ex->Message, ex->GetType()->FullName);
						Console::WriteLine(ex->StackTrace);
					}

					threads->Remove(Thread::CurrentThread);
				}
			internal:
				static BlockingCollection<HookCallbackInfo^>^ queue =
					gcnew BlockingCollection<HookCallbackInfo^>(gcnew ConcurrentQueue<HookCallbackInfo^>);
			public:
				static IntPtr SetHook(HookType hookType, HookCallback^ callback);
				static void ReleaseHook(HookType hookType);
				static Dictionary<HookType, List<HookCallback^>^>^ Hooks;
				static List<UINT>^ MessageFilter;

				static GlobalHook() {
					Hooks = gcnew Dictionary<HookType, List<HookCallback^>^>;
					MessageFilter = gcnew List<UINT>;
				}

				static void Start() {
					threads = gcnew ObservableCollection<Thread^>;
					threads->CollectionChanged += gcnew System::Collections::Specialized::NotifyCollectionChangedEventHandler(&IvyLock::Native::ARCH::GlobalHook::OnCollectionChanged);

					Thread^ thread = gcnew Thread(gcnew ThreadStart(PollThread));
					thread->Name = "Server Thread";
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

				property static String^ Pipe { String^ get() { return PIPE_NAME; }; };

				static void OnCollectionChanged(System::Object ^sender,
					System::Collections::Specialized::NotifyCollectionChangedEventArgs ^e) {
					Console::WriteLine("Num worker threads: {0}", threads->Count);
				}
			};

			private class GlobalHookImpl abstract sealed {
			private:
#pragma data_seg (".MY_HOOK_DATA")
				static std::map<int, HHOOK> Hooks;
#pragma data_seg()

			public:
				static HHOOK GetHook(int hookType);
				static HHOOK SetHook(int hookType);
				static void ReleaseHook(int hookType);
			};

			public struct HookCallbackInfoNative {
				UINT32 hookType; int nCode; WPARAM wParam; LPARAM lParam;
			};
		}
	}
}
