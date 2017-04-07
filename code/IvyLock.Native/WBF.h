// IvyLock.Native.h

#pragma once
#include <Windows.h>
#include <WinBio.h>

using namespace System;
using namespace System::Collections::Generic;
using namespace System::Linq;
using namespace System::ComponentModel;
using namespace System::Runtime::InteropServices;

#define uint UInt32
#define string String^

namespace IvyLock {
	namespace Native {
		WCHAR* StringToNative(String^ str) {
			pin_ptr<wchar_t> ptr = &str->ToCharArray()[0];
			return ptr;
		}

		Guid GuidFromNative(const GUID &g)
		{
			return Guid(g.Data1, g.Data2, g.Data3, g.Data4[0], g.Data4[1], g.Data4[2],
				g.Data4[3], g.Data4[4], g.Data4[5], g.Data4[6], g.Data4[7]);
		}

		GUID GuidToNative(Guid guid) {
			array<Byte>^ guidData = guid.ToByteArray();
			pin_ptr<Byte> data = &(guidData[0]);
			return *(_GUID*)data;
		}

		public enum class BiometricType : uint
		{
			Fingerprint = WINBIO_TYPE_FINGERPRINT,
			Multiple = WINBIO_TYPE_MULTIPLE,
			FacialFeatures = WINBIO_TYPE_FACIAL_FEATURES,
			Voice = WINBIO_TYPE_VOICE,
			Iris = WINBIO_TYPE_IRIS,
			Retina = WINBIO_TYPE_RETINA,
			HandGeometry = WINBIO_TYPE_HAND_GEOMETRY,
			SignatureDynamics = WINBIO_TYPE_SIGNATURE_DYNAMICS,
			KeystrokeDynamics = WINBIO_TYPE_KEYSTROKE_DYNAMICS,
			LipMovement = WINBIO_TYPE_LIP_MOVEMENT,
			ThermalFaceImage = WINBIO_TYPE_THERMAL_FACE_IMAGE,
			THERMAL_HAND_IMAGE = WINBIO_TYPE_THERMAL_HAND_IMAGE,
			Gait = WINBIO_TYPE_GAIT,
			Scent = WINBIO_TYPE_SCENT,
			DNA = WINBIO_TYPE_DNA,
			EarShape = WINBIO_TYPE_EAR_SHAPE,
			FingerGeometry = WINBIO_TYPE_FINGER_GEOMETRY,
			PalmPrint = WINBIO_TYPE_PALM_PRINT,
			VeinPattern = WINBIO_TYPE_VEIN_PATTERN,
			FootPrint = WINBIO_TYPE_FOOT_PRINT
		};

		public enum class BiometricRejectDetail : uint
		{
			TooHigh = WINBIO_FP_TOO_HIGH,
			TooLow = WINBIO_FP_TOO_LOW,
			TooLeft = WINBIO_FP_TOO_LEFT,
			TooRight = WINBIO_FP_TOO_RIGHT,
			TooFast = WINBIO_FP_TOO_FAST,
			TooSlow = WINBIO_FP_TOO_SLOW,
			PoorQuality = WINBIO_FP_POOR_QUALITY,
			TooSkewed = WINBIO_FP_TOO_SKEWED,
			TooShort = WINBIO_FP_TOO_SHORT,
			MergeFailure = WINBIO_FP_MERGE_FAILURE
		};

		public enum class BiometricSensorSubtype : uint
		{
			Unknown = WINBIO_SENSOR_SUBTYPE_UNKNOWN,
			Swipe = WINBIO_FP_SENSOR_SUBTYPE_SWIPE,
			Touch = WINBIO_FP_SENSOR_SUBTYPE_TOUCH
		};

		public enum class BiometricSubtype : uint
		{
			Unknown = WINBIO_SUBTYPE_NO_INFORMATION,
			RightThumb = WINBIO_ANSI_381_POS_RH_THUMB,
			RightIndexFinger = WINBIO_ANSI_381_POS_RH_INDEX_FINGER,
			RightMiddleFinger = WINBIO_ANSI_381_POS_RH_MIDDLE_FINGER,
			RightRingFinger = WINBIO_ANSI_381_POS_RH_RING_FINGER,
			RightLittleFinger = WINBIO_ANSI_381_POS_RH_LITTLE_FINGER,
			LeftThumb = WINBIO_ANSI_381_POS_LH_THUMB,
			LeftIndexFinger = WINBIO_ANSI_381_POS_LH_INDEX_FINGER,
			LeftMiddleFinger = WINBIO_ANSI_381_POS_LH_MIDDLE_FINGER,
			LeftRingFinger = WINBIO_ANSI_381_POS_LH_RING_FINGER,
			LeftLittleFinger = WINBIO_ANSI_381_POS_LH_LITTLE_FINGER,
			RightFourFingers = WINBIO_ANSI_381_POS_RH_FOUR_FINGERS,
			LeftFourFingers = WINBIO_ANSI_381_POS_LH_FOUR_FINGERS,
			TwoThumbs = WINBIO_ANSI_381_POS_TWO_THUMBS,
			Any = WINBIO_SUBTYPE_ANY
		};

		public enum class BiometricPoolType : uint
		{
			Unknown = WINBIO_POOL_UNKNOWN,
			System = WINBIO_POOL_SYSTEM,
			Private = WINBIO_POOL_PRIVATE
		};

		[Flags]
		public enum class BiometricSessionFlags : uint
		{
			Default = WINBIO_FLAG_DEFAULT,
			Raw = WINBIO_FLAG_RAW,
			Maintenance = WINBIO_FLAG_MAINTENANCE,
			Basic = WINBIO_FLAG_BASIC,
			Advanced = WINBIO_FLAG_ADVANCED
		};

		[Flags]
		public enum class BiometricCapabilities : uint
		{
			Sensor = WINBIO_CAPABILITY_SENSOR,
			Matching = WINBIO_CAPABILITY_MATCHING,
			Database = WINBIO_CAPABILITY_DATABASE,
			Processing = WINBIO_CAPABILITY_PROCESSING,
			Encryption = WINBIO_CAPABILITY_ENCRYPTION,
			Navigation = WINBIO_CAPABILITY_NAVIGATION,
			Indicator = WINBIO_CAPABILITY_INDICATOR,
			VirtualSensor = WINBIO_CAPABILITY_VIRTUAL_SENSOR
		};

		public value class BiometricVersion
		{
		public:
			int MajorVersion;
			int MinorVersion;
		};

		public value class BiometricUnitSchema
		{
		public:
			int UnitId;
			BiometricPoolType PoolType;
			BiometricType BiometricFactor;
			BiometricSubtype SensorSubType;
			BiometricCapabilities Capabilities;
			String^ DeviceInstanceId;
			String^ Description;
			String^ Manufacturer;
			String^ Model;
			String^ SerialNumber;
			BiometricVersion FirmwareVersion;
		internal:
			BiometricUnitSchema(WINBIO_UNIT_SCHEMA native) {
				this->UnitId = native.UnitId;
				this->PoolType = (BiometricPoolType)native.PoolType;
				this->SensorSubType = (BiometricSubtype)native.SensorSubType;
				this->Capabilities = (BiometricCapabilities)native.Capabilities;
				this->DeviceInstanceId = gcnew String(native.DeviceInstanceId, 0, 256);
				this->Description = gcnew String(native.Description, 0, 256);
				this->Manufacturer = gcnew String(native.Manufacturer, 0, 256);
				this->Model = gcnew String(native.Model, 0, 256);
				this->SerialNumber = gcnew String(native.SerialNumber, 0, 256);
			}

			WINBIO_UNIT_SCHEMA ToNative() {
				WINBIO_UNIT_SCHEMA native = WINBIO_UNIT_SCHEMA();
				native.UnitId = this->UnitId;
				native.PoolType = (WINBIO_POOL_TYPE)this->PoolType;
				native.SensorSubType = (WINBIO_BIOMETRIC_SENSOR_SUBTYPE)this->SensorSubType;
				native.Capabilities = (WINBIO_CAPABILITIES)this->Capabilities;
				wcscpy(native.DeviceInstanceId, StringToNative(this->DeviceInstanceId));
				wcscpy(native.Description, StringToNative(this->Description));
				wcscpy(native.Manufacturer, StringToNative(this->Manufacturer));
				wcscpy(native.Model, StringToNative(this->Model));
				wcscpy(native.SerialNumber, StringToNative(this->SerialNumber));
				return native;
			}
		};

		public enum class BiometricIdentityType : uint
		{
			Null = WINBIO_ID_TYPE_NULL,
			Wildcard = WINBIO_ID_TYPE_WILDCARD,
			Guid = WINBIO_ID_TYPE_GUID,
			SID = WINBIO_ID_TYPE_SID
		};

		public value class BiometricIdentity
		{
		public:
			value struct AccountSid
			{
				uint Size;

				array<unsigned char>^ Data;
			};

			BiometricIdentityType Type;
			Guid TemplateGuid;
			AccountSid SID;

		internal:
			BiometricIdentity(WINBIO_IDENTITY native) {
				this->Type = (BiometricIdentityType)native.Type;
				switch (this->Type)
				{
				case BiometricIdentityType::Guid:
					this->TemplateGuid = GuidFromNative(native.Value.TemplateGuid);
					break;
				case BiometricIdentityType::SID:
					unsigned char* data = reinterpret_cast<unsigned char*> (native.Value.AccountSid.Data);
					array<unsigned char>^ cdata = gcnew array<unsigned char>(native.Value.AccountSid.Size);
					System::Runtime::InteropServices::Marshal::Copy(IntPtr((void *)data), cdata, 0, native.Value.AccountSid.Size);

					this->SID.Data = cdata;
					this->SID.Size = native.Value.AccountSid.Size;
					break;
				}
			}

			WINBIO_IDENTITY ToNative() {
				WINBIO_IDENTITY native = WINBIO_IDENTITY();

				native.Type = (WINBIO_IDENTITY_TYPE)this->Type;

				switch (this->Type)
				{
				case BiometricIdentityType::Null:
					native.Value.Null = 1;
					break;
				case BiometricIdentityType::Wildcard:
					native.Value.Wildcard = 1;
					break;
				case BiometricIdentityType::Guid:
					native.Value.TemplateGuid = GuidToNative(this->TemplateGuid);
					break;
				case BiometricIdentityType::SID:
					native.Value.AccountSid.Size = this->SID.Size;
					pin_ptr<unsigned char> ptr = &this->SID.Data[0];
					memcpy(native.Value.AccountSid.Data, ptr, this->SID.Size);
					break;
				}

				return native;
			}
		};

		public enum class BiometricDatabaseType {
			None = 0,
			Default = 1,
			Bootstrap = 2,
			OnChip = 3
		};

		public enum class BiometricError {
			None = S_OK,
			BadCapture = WINBIO_E_BAD_CAPTURE,
			EnrollmentInProgress = WINBIO_E_ENROLLMENT_IN_PROGRESS,
			NoMatch = WINBIO_E_NO_MATCH
		};

		HRESULT GetCurrentUserIdentity(__inout PWINBIO_IDENTITY Identity)
		{
			// Declare variables.
			HRESULT hr = S_OK;
			HANDLE tokenHandle = NULL;
			DWORD bytesReturned = 0;
			struct {
				TOKEN_USER tokenUser;
				BYTE buffer[SECURITY_MAX_SID_SIZE];
			} tokenInfoBuffer;

			// Zero the input identity and specify the type.
			ZeroMemory(Identity, sizeof(WINBIO_IDENTITY));
			Identity->Type = WINBIO_ID_TYPE_NULL;

			// Open the access token associated with the
			// current process
			if (!OpenProcessToken(
				GetCurrentProcess(),            // Process handle
				TOKEN_READ,                     // Read access only
				&tokenHandle))                  // Access token handle
			{
				DWORD win32Status = GetLastError();
				hr = HRESULT_FROM_WIN32(win32Status);
				goto e_Exit;
			}

			// Zero the tokenInfoBuffer structure.
			ZeroMemory(&tokenInfoBuffer, sizeof(tokenInfoBuffer));

			// Retrieve information about the access token. In this case,
			// retrieve a SID.
			if (!GetTokenInformation(
				tokenHandle,                    // Access token handle
				TokenUser,                      // User for the token
				&tokenInfoBuffer.tokenUser,     // Buffer to fill
				sizeof(tokenInfoBuffer),        // Size of the buffer
				&bytesReturned))                // Size needed
			{
				DWORD win32Status = GetLastError();
				hr = HRESULT_FROM_WIN32(win32Status);
				goto e_Exit;
			}

			// Copy the SID from the tokenInfoBuffer structure to the
			// WINBIO_IDENTITY structure.
			CopySid(
				SECURITY_MAX_SID_SIZE,
				Identity->Value.AccountSid.Data,
				tokenInfoBuffer.tokenUser.User.Sid
			);

			// Specify the size of the SID and assign WINBIO_ID_TYPE_SID
			// to the type member of the WINBIO_IDENTITY structure.
			Identity->Value.AccountSid.Size = GetLengthSid(tokenInfoBuffer.tokenUser.User.Sid);
			Identity->Type = WINBIO_ID_TYPE_SID;

		e_Exit:

			if (tokenHandle != NULL)
			{
				CloseHandle(tokenHandle);
			}

			return hr;
		}

		public ref class WBF abstract sealed {
		public:
			static BiometricIdentity GetCurrentIdentity() {
				WINBIO_IDENTITY ident = { 0 };
				GetCurrentUserIdentity(&ident);
				return BiometricIdentity(ident);
			}

			static UInt32 OpenSession(BiometricType type, BiometricPoolType poolType, BiometricSessionFlags flags,
				array<int>^ units, BiometricDatabaseType dbType) {
				pin_ptr<int> unit = units == nullptr ? nullptr : &units[0];
				int unitCount = units == nullptr ? 0 : units->Length;

				WINBIO_SESSION_HANDLE handle = 0;

				HRESULT hr =
					WinBioOpenSession(
					(WINBIO_BIOMETRIC_TYPE)type,
						(WINBIO_POOL_TYPE)poolType,
						(WINBIO_SESSION_FLAGS)flags,
						(WINBIO_UNIT_ID *)unit,
						unitCount,
						(GUID *)dbType,
						&handle);

				if (FAILED(hr))
					throw gcnew Win32Exception(hr);
				else
					return handle;
			}

			static void CloseSession(UInt32 sessionHandle) {
				HRESULT hr = WinBioCloseSession(sessionHandle);

				if (FAILED(hr))
					throw gcnew Win32Exception(hr);
			}

			static bool Verify(UInt32 sessionHandle, BiometricIdentity identity, BiometricSubtype subFactor,
				[Out] UInt32% unitId, [Out] BiometricRejectDetail% rejectDetail, [Out] BiometricError% error) {
				WINBIO_UNIT_ID uid = 0;
				BOOLEAN match = false;
				WINBIO_REJECT_DETAIL rd = NULL;
				WINBIO_IDENTITY nativeIdentity = identity.ToNative();
				WINBIO_IDENTITY nativeIdentity2 = { 0 };
				GetCurrentUserIdentity(&nativeIdentity2);

				HRESULT hr =
					WinBioVerify(
					(WINBIO_SESSION_HANDLE)sessionHandle,
						&nativeIdentity,
						(WINBIO_BIOMETRIC_SUBTYPE)subFactor,
						&uid,
						&match,
						&rd);

				unitId = uid;
				rejectDetail = (BiometricRejectDetail)rd;

				if (Enum::IsDefined(BiometricError::typeid, hr))
					error = (BiometricError)hr;
				else if (FAILED(hr))
					throw gcnew Win32Exception(hr);

				return match;
			}

			static array<BiometricUnitSchema>^ GetBiometricUnits(BiometricType factor) {
				PWINBIO_UNIT_SCHEMA unitSchema = NULL;
				SIZE_T unitCount = 0;

				HRESULT hr = WinBioEnumBiometricUnits((WINBIO_BIOMETRIC_TYPE)factor, &unitSchema, &unitCount);

				if (FAILED(hr))
					throw gcnew Win32Exception(hr);

				List<BiometricUnitSchema>^ list = gcnew List<BiometricUnitSchema>();

				for (int index = 0; index < unitCount; ++index)
				{
					list->Add(BiometricUnitSchema(unitSchema[index]));
				}

				WinBioFree(unitSchema);

				return list->ToArray();
			}
		};
	}
}
