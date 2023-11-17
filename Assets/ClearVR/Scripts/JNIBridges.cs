#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine;
using System;
using System.Reflection;
using UnityEngine.Scripting;
using com.tiledmedia.clearvr.protobuf;
using com.tiledmedia.clearvr.cvrinterface;
namespace com.tiledmedia.clearvr{
	/// <summary>
	/// ClearVR AndroidJNI bridge
	/// </summary>
	class JNIBridges {
		internal static jvalue[] emptyJvalue = new jvalue[0];
		// Explicitly construct all our bridges in a very specific order.
		// Enums
#pragma warning disable 0414 // silence declared but not used warning.
		private JNIBridgeClearVRMessageTypes jniBridgeClearVRMessageType = new JNIBridgeClearVRMessageTypes();
		private JNIBridgeClearVRRequestTypes jniBridgeClearVRRequestTypes = new JNIBridgeClearVRRequestTypes();
		private JNIBridgeTimingTypes jniBridgeTimingTypes = new JNIBridgeTimingTypes();
		private JNIBridgeClearVRCoreWrapperConstructorParameters jniBridgeClearVRCoreWrapperConstructorParameters = new JNIBridgeClearVRCoreWrapperConstructorParameters();
		private JNIBridgeStopClearVRCoreParameters jniBridgeStopClearVRCoreParameters = new JNIBridgeStopClearVRCoreParameters();
		private JNIBridgeClearVRMessage jniBridgeClearVRMessage = new JNIBridgeClearVRMessage();
		private JNIBridgeClearVRAsyncRequest jniBridgeClearVRAsyncRequest = new JNIBridgeClearVRAsyncRequest();
		private JNIBridgeClearVRAsyncRequestResponse jniBridgeClearVRAsyncRequestResponse = new JNIBridgeClearVRAsyncRequestResponse();
		private JNIBridgeClearVRCoreWrapper jniBridgeClearVRCoreWrapper = new JNIBridgeClearVRCoreWrapper();
#pragma warning restore 0414

		internal JNIBridges() {
			JNIBridgeClearVRMessageTypes.Initialize();
			JNIBridgeClearVRRequestTypes.Initialize();
			JNIBridgeClearVRMessage.Initialize();
			JNIBridgeClearVRAsyncRequest.Initialize();
			JNIBridgeClearVRAsyncRequestResponse.Initialize();
			JNIBridgeTimingTypes.Initialize();
			JNIBridgeClearVRCoreWrapperConstructorParameters.Initialize();
			JNIBridgeStopClearVRCoreParameters.Initialize();
			JNIBridgeClearVRCoreWrapper.Initialize();
		}

		internal static jvalue[] ConvertGlobalRefToJValues(IntPtr argGlobalRef) {
			jvalue[] jValues = new jvalue[1];
			jValues[0].l = argGlobalRef;
			return jValues;
		}

		internal static void DeleteGlobalRefFromJValueArray(jvalue[] argJValues) {
			if(argJValues.Length != 1) {
				throw new Exception(String.Format("[ClearVR] Attempted to call DeleteGlobalRef on a jvalue object that contains {0} items. Only 1 item allowed. Please report this bug to Tiledmedia.", argJValues.Length));
			}
			AndroidJNI.DeleteGlobalRef(argJValues[0].l);
		}

		internal static IntPtr ByteArrayToLocalRef(byte[] argByteArray) {
#if UNITY_2019_1_OR_NEWER
			return AndroidJNI.ToSByteArray(ConvertByteArrayToSByteArray(argByteArray));
#else
			return AndroidJNI.ToByteArray(argByteArray);
#endif
		}

		/// <summary>
		/// Converts a JavaObject IntPtr to a ByteArray.
		/// </summary>
		/// <param name="argRawObject">The object to convert</param>
		/// <returns>Returns the byte[] equivalent of the argument, or null if the argument is null.</returns>
		internal static byte[] GetByteArrayFromRawObject(IntPtr argRawObject) {
			if(argRawObject == IntPtr.Zero) {
				return null;
			}
#if UNITY_2019_1_OR_NEWER
			return ConvertSByteArrayToByteArray(AndroidJNI.FromSByteArray(argRawObject));
#else
			return AndroidJNI.FromByteArray(argRawObject);
#endif
		}

#if UNITY_2019_1_OR_NEWER
		// Only used on UNITY 2019_1 and up to convert byte[] to sbyte[]
		internal static sbyte[] ConvertByteArrayToSByteArray(byte[] argValue) {
			if(argValue == null) {
				return null;
			}
			return Array.ConvertAll(argValue, b => unchecked((sbyte)b));
		}

		internal static byte[] ConvertSByteArrayToByteArray(sbyte[] argValue) {
			if(argValue == null) {
				return null;
			}
			return Array.ConvertAll(argValue, b => unchecked((byte)b));
		}
#endif
		internal abstract class JNIBridgeBase<T> where T : class {
			private static bool _isInitialized = false;
			protected static IntPtr pClassLocalRef = IntPtr.Zero;
			public static IntPtr pClassGlobalRef = IntPtr.Zero;
			private static String constructorArguments = "";
			public static IntPtr pConstructorMethodID = IntPtr.Zero;

			public static String className { get; private set; }
			public static String jniClassName { get; private set; }

			private static Type _GetType() {
				return typeof(T);
			}

			protected JNIBridgeBase(String argClassName, String argConstructorArguments = "") {
				className = argClassName;
				jniClassName = String.Format("L{0};", className);
				constructorArguments = argConstructorArguments;
				Initialize();
			}

			internal static void Initialize() {
				if(!_isInitialized) {
					pClassLocalRef = AndroidJNI.FindClass(className);
					pClassGlobalRef = AndroidJNI.NewGlobalRef(pClassLocalRef);
					AndroidJNI.DeleteLocalRef(pClassLocalRef);
					if(constructorArguments != "") {
						pConstructorMethodID = AndroidJNI.GetMethodID(pClassGlobalRef, "<init>", constructorArguments);
					}
					// Each JNIBridge MUST implement an internal static Setup() method
					MethodInfo method = _GetType().GetMethod("Setup", BindingFlags.NonPublic /* it's internal */ | BindingFlags.Static | BindingFlags.FlattenHierarchy);
					if(method == null) {
						throw new Exception(String.Format("[ClearVR] {0} not properly implemented. Setup() method not found.", _GetType()));
					}
					method.Invoke(null, null);
					_isInitialized = true;
				}
			}

			internal static void VerifyProperlyInitialized() {
				if(!_isInitialized) {
					Initialize();
				}
			}

			internal static IntPtr ConstructGlobalRef(jvalue[] argConstructorParams) {
				VerifyProperlyInitialized();
				return AndroidJNI.NewGlobalRef(AndroidJNI.NewObject(pClassGlobalRef, pConstructorMethodID, argConstructorParams));
			}

			internal static IntPtr ConstructGlobalRef(IntPtr argConstructorParams) {
				VerifyProperlyInitialized();
				jvalue[] jValues = new jvalue[1];
				jValues[0].l = argConstructorParams;
				return AndroidJNI.NewGlobalRef(AndroidJNI.NewObject(pClassGlobalRef, pConstructorMethodID, jValues));
			}

			internal static jvalue[] ConstructGlobalRefAsJValues(IntPtr argConstructorParams) {
				VerifyProperlyInitialized();
				jvalue[] jValues = new jvalue[1];
				jvalue[] jValuesOut = new jvalue[1];
				jValues[0].l = argConstructorParams;
				jValuesOut[0].l = AndroidJNI.NewGlobalRef(AndroidJNI.NewObject(pClassGlobalRef, pConstructorMethodID, jValues));
				return jValuesOut;
			}

			internal static jvalue[] ConstructGlobalRefAsJValues(jvalue[] argConstructorParams) {
				VerifyProperlyInitialized();
				IntPtr objectGlobalRef = AndroidJNI.NewGlobalRef(AndroidJNI.NewObject(pClassGlobalRef, pConstructorMethodID, argConstructorParams));
				jvalue[] jValuesOut = new jvalue[1];
				jValuesOut[0].l = objectGlobalRef;
				return jValuesOut;
			}

			internal static jvalue ConstructGlobalRefAsJValue(jvalue[] argConstructorParams) {
				VerifyProperlyInitialized();
				IntPtr objectGlobalRef = AndroidJNI.NewGlobalRef(AndroidJNI.NewObject(pClassGlobalRef, pConstructorMethodID, argConstructorParams));
				jvalue jValueOut = new jvalue();
				jValueOut.l = objectGlobalRef;
				return jValueOut;
			}

			internal static void Release() {
				if(pClassGlobalRef != IntPtr.Zero) {
					AndroidJNI.DeleteGlobalRef(pClassGlobalRef);
					pClassGlobalRef = IntPtr.Zero;
				}
				_isInitialized = false;
			}
		}

		internal class JNIBridgeClearVRMessage : JNIBridgeBase<JNIBridgeClearVRMessage> {
			/* ClearVRMessage fields */
			private static IntPtr pMessageTypeFieldID = IntPtr.Zero;
			private static IntPtr pCodeFieldID = IntPtr.Zero;
			private static IntPtr pMessageFieldID = IntPtr.Zero;
			private static IntPtr pIsSuccessFieldID = IntPtr.Zero;

			internal JNIBridgeClearVRMessage() : base("com/tiledmedia/clearvrcorewrapper/ClearVRMessage") {
				// Intentionally left empty
			}

			[Preserve]
			internal static void Setup() {
				pMessageTypeFieldID = AndroidJNI.GetFieldID(pClassGlobalRef, "messageType", JNIBridgeClearVRMessageTypes.jniClassName);
				pCodeFieldID = AndroidJNI.GetFieldID(pClassGlobalRef, "code", "I"); // Code is just an integer, not an enum
				pMessageFieldID = AndroidJNI.GetFieldID(pClassGlobalRef, "message", "Ljava/lang/String;");
				pIsSuccessFieldID = AndroidJNI.GetFieldID(pClassGlobalRef, "isSuccess", "Z");
			}

			public static ClearVRMessage GetClearVRMessageFromRawJavaObject(IntPtr argClearVRMessageRawObject) {
				IntPtr clearVRMessageTypeRawObject = AndroidJNI.GetObjectField(argClearVRMessageRawObject, pMessageTypeFieldID);
				int messageType = JNIBridgeClearVRMessageTypes.GetValue(clearVRMessageTypeRawObject);
				int messageCode = AndroidJNI.GetIntField(argClearVRMessageRawObject, pCodeFieldID);
				String message = AndroidJNI.GetStringField(argClearVRMessageRawObject, pMessageFieldID);
				bool isSuccess = AndroidJNI.GetBooleanField(argClearVRMessageRawObject, pIsSuccessFieldID);
				return new ClearVRMessage(messageType,
						messageCode,
						message,
						ClearVRMessage.ConvertBooleanToClearVRResult(isSuccess));
			}
		}

		internal class JNIBridgeClearVRRequestTypes : JNIBridgeBase<JNIBridgeClearVRRequestTypes> {
			private static IntPtr pGetValueMethodID = IntPtr.Zero;

			internal JNIBridgeClearVRRequestTypes() : base("com/tiledmedia/clearvrenums/ClearVRAsyncRequestTypes")  {
			}

			[Preserve]
			internal static void Setup() {
				pGetValueMethodID = AndroidJNI.GetMethodID(pClassGlobalRef, "getValue", "()I");
			}
			internal static int GetValue(IntPtr argClearVRRequestTypesObject) {
				return AndroidJNI.CallIntMethod(
					argClearVRRequestTypesObject,
					pGetValueMethodID,
					emptyJvalue);
			}

			internal static RequestTypes GetValueAsRequestType(IntPtr argClearVRRequestTypesObject) {
				return (RequestTypes) GetValue(argClearVRRequestTypesObject);
			}
		}

		internal class JNIBridgeTimingTypes : JNIBridgeBase<JNIBridgeTimingTypes> {
			private static IntPtr pGetValueMethodID = IntPtr.Zero;
			private static IntPtr pGetTimingTypeMethodID = IntPtr.Zero;
			internal JNIBridgeTimingTypes() : base("com/tiledmedia/clearvrenums/TimingTypes")  {
			}

			[Preserve]
			internal static void Setup() {
				pGetValueMethodID = AndroidJNI.GetMethodID(pClassGlobalRef, "getValue", "()I");
				pGetTimingTypeMethodID = AndroidJNI.GetStaticMethodID(pClassGlobalRef, "getTimingType", String.Format("(I){0}", jniClassName));
			}

			internal static int GetValue(IntPtr argTimingTypeRawObject) {
				return AndroidJNI.CallIntMethod(
					argTimingTypeRawObject,
					pGetValueMethodID,
					emptyJvalue);
			}
			internal static TimingTypes GetValueAsTimingType(IntPtr argTimingTypeRawObject) {
				return (TimingTypes) GetValue(argTimingTypeRawObject);
			}

			internal static IntPtr GetTimingTypeRawObjectLocalRef(TimingTypes argTimingType) {
				jvalue[] jValues = new jvalue[1];
				jValues[0].i = (int) argTimingType;
				IntPtr rawObjectLocalRef = AndroidJNI.CallStaticObjectMethod(pClassGlobalRef,
					pGetTimingTypeMethodID,
					jValues);
				return rawObjectLocalRef;
			}
		}

		internal class JNIBridgeClearVRMessageTypes : JNIBridgeBase<JNIBridgeClearVRMessageTypes> {
			private static IntPtr pGetValueMethodID = IntPtr.Zero;

			internal JNIBridgeClearVRMessageTypes() : base("com/tiledmedia/clearvrenums/ClearVRMessageTypes")  {
			}

			[Preserve]
			internal static void Setup() {
				pGetValueMethodID = AndroidJNI.GetMethodID(pClassGlobalRef, "getValue", "()I");
			}

			internal static int GetValue(IntPtr argClearVRMessageType) {
				return AndroidJNI.CallIntMethod(
					argClearVRMessageType,
					pGetValueMethodID,
					emptyJvalue);
			}
		}

		internal class JNIBridgeClearVRCoreWrapperConstructorParameters : JNIBridgeBase<JNIBridgeClearVRCoreWrapperConstructorParameters> {
			internal JNIBridgeClearVRCoreWrapperConstructorParameters() : base("com/tiledmedia/clearvrparameters/ClearVRCoreWrapperConstructorParameters", String.Format("({0}Landroid/app/Activity;Ljava/lang/String;)V", ClearVRCoreWrapperExternalInterface.JNI_CLASS_NAME)) {
			}

			[Preserve]
			internal static void Setup() {
				// Intentionally left empty.
			}

			internal static jvalue[] GetGlobalRefAsJValues(IntPtr argClearVRCoreWrapperExternalInterfaceJavaProxyGlobalRef, IntPtr argActivityRawObjectGlobalRef, String argDeviceAppId) {
				jvalue[] parameters = new jvalue[3];
				parameters[0] = new jvalue();
				parameters[0].l = argClearVRCoreWrapperExternalInterfaceJavaProxyGlobalRef; // the callback interface
				parameters[1] = new jvalue();
				parameters[1].l = argActivityRawObjectGlobalRef; // activity
				parameters[2] = new jvalue();
				parameters[2].l = AndroidJNI.NewStringUTF(argDeviceAppId); // device app id (empty string)
				jvalue[] rawObjectGlobalRef = ConstructGlobalRefAsJValues(parameters);
				// We DO NOT unref the Activity and ExternalInterface GlobalRefs as they are used throughout the lifetime of the ClearVRPlayer object
				AndroidJNI.DeleteLocalRef(parameters[2].l);
				return rawObjectGlobalRef;
			}
		}

		internal class JNIBridgeStopClearVRCoreParameters : JNIBridgeBase<JNIBridgeStopClearVRCoreParameters> {
			internal JNIBridgeStopClearVRCoreParameters() : base("com/tiledmedia/clearvrparameters/StopClearVRCoreParameters", "(ZZZ)V") {
			}

			[Preserve]
			internal static void Setup() {
				// Intentionally left empty
			}

			/// <summary>
			/// Construct object, one MUST keep all arguments set to false. Never set any of these to true!
			/// </summary>
			/// <param name="argIsErrorReported">MUST be false</param>
			/// <param name="argIsClearVRCoreAlreadyStopped">MUST be false</param>
			/// <param name="argIsClearVRCoreCrashed">MUST be false</param>
			/// <returns></returns>
			internal static jvalue[] GetGlobalRefAsJValues(bool argIsErrorReported = false, bool argIsClearVRCoreAlreadyStopped = false, bool argIsClearVRCoreCrashed = false) {
				if(argIsErrorReported || argIsClearVRCoreAlreadyStopped || argIsClearVRCoreCrashed) {
					throw new Exception("[ClearVR] You cannot set any of the arguments of this method to true.");
				}
				jvalue[] parameters = new jvalue[3];
				parameters[0].z = argIsErrorReported;
				parameters[1].z = argIsClearVRCoreAlreadyStopped;
				parameters[2].z = argIsClearVRCoreCrashed;
				jvalue[] rawObjectGlobalRef = ConstructGlobalRefAsJValues(parameters);
				return rawObjectGlobalRef;
			}
		}

		internal class JNIBridgeClearVRAsyncRequest : JNIBridgeBase<JNIBridgeClearVRAsyncRequest> {
			/* ClearVRAsyncRequest Fields */
			private static IntPtr pRequestTypeFieldID = IntPtr.Zero;
			private static IntPtr pRequestIdFieldID = IntPtr.Zero;
			internal JNIBridgeClearVRAsyncRequest() : base("com/tiledmedia/clearvrcorewrapper/ClearVRAsyncRequest")  {
			}

			[Preserve]
			internal static void Setup() {
				pRequestTypeFieldID = AndroidJNI.GetFieldID(pClassGlobalRef, "requestType", JNIBridgeClearVRRequestTypes.jniClassName);
				pRequestIdFieldID = AndroidJNI.GetFieldID(pClassGlobalRef, "requestId", "I");
			}

			internal static ClearVRAsyncRequest ConvertClearVRAsyncRequestAsRawObjectToClearVRAsyncRequest(IntPtr argClearVRAsyncRequestRawObject) {
				IntPtr clearVRRequestTypesObject = AndroidJNI.GetObjectField(argClearVRAsyncRequestRawObject, pRequestTypeFieldID);
				return new ClearVRAsyncRequest(
					JNIBridgeClearVRRequestTypes.GetValue(clearVRRequestTypesObject),
					AndroidJNI.GetIntField(argClearVRAsyncRequestRawObject, pRequestIdFieldID)
				);
			}
		}

		internal class JNIBridgeClearVRAsyncRequestResponse : JNIBridgeBase<JNIBridgeClearVRAsyncRequestResponse> {
			private static IntPtr pMessageFieldID = IntPtr.Zero;
			private static IntPtr pRequestTypeFieldID = IntPtr.Zero;
			private static IntPtr pRequestIdFieldID = IntPtr.Zero;
			internal JNIBridgeClearVRAsyncRequestResponse() : base("com/tiledmedia/clearvrcorewrapper/ClearVRAsyncRequestResponse") {
			}

			[Preserve]
			internal static void Setup() {
				// The requestType field is an enum so we need to access it as if it were a class
				pRequestTypeFieldID = AndroidJNI.GetFieldID(pClassGlobalRef, "requestType", JNIBridgeClearVRRequestTypes.jniClassName);
				// requestId
				pRequestIdFieldID = AndroidJNI.GetFieldID(pClassGlobalRef, "requestId", "I");
				// clearVRMessage
				pMessageFieldID = AndroidJNI.GetFieldID(pClassGlobalRef, "clearVRMessage", JNIBridgeClearVRMessage.jniClassName);
			}

			internal static ClearVRAsyncRequestResponse GetClearVRAsyncRequestResponseFromClearVRAsyncRequestResponseRawObject(IntPtr argClearVRAsyncRequestResponseRawObject) {
				IntPtr clearVRRequestTypesObject = AndroidJNI.GetObjectField(argClearVRAsyncRequestResponseRawObject, pRequestTypeFieldID);
				RequestTypes requestType = JNIBridgeClearVRRequestTypes.GetValueAsRequestType(clearVRRequestTypesObject);
				int requestId = AndroidJNI.GetIntField(argClearVRAsyncRequestResponseRawObject, pRequestIdFieldID);
				return new ClearVRAsyncRequestResponse(requestType, requestId);
			}

			internal static ClearVRMessage GetClearVRMessageFromClearVRAsyncRequestResponseRawObject(IntPtr argClearVRAsyncRequestResponseRawObject) {
				IntPtr clearVRMessageObject = AndroidJNI.GetObjectField(argClearVRAsyncRequestResponseRawObject, pMessageFieldID);
				return JNIBridgeClearVRMessage.GetClearVRMessageFromRawJavaObject(clearVRMessageObject);
			}
		}

		/*
		This class implements com.tiledmedia.clearvrcorewrapper.ClearVRCoreWrapperExternalInterface java interface as an AndroidJavaProxy
		*/
		internal class ClearVRCoreWrapperExternalInterface : AndroidJavaProxy {
			/* The underlying SDK makes sure that this callback is always invoked from the main thread. Otherwise, random SEGFAULTS are to be expected! */
			private MediaPlayerAndroid mediaPlayer;
			internal const String CLASS_NAME = "com.tiledmedia.clearvrcorewrapper.ClearVRCoreWrapperExternalInterface";
			internal const String JNI_CLASS_NAME = "Lcom/tiledmedia/clearvrcorewrapper/ClearVRCoreWrapperExternalInterface;";
			public ClearVRCoreWrapperExternalInterface(MediaPlayerAndroid argMediaPlayer) : base(CLASS_NAME) {
				mediaPlayer = argMediaPlayer;
			}

			public void cbClearVRCoreWrapperRequestCompleted(AndroidJavaObject argClearVRAsyncRequestResponseJavaObject) {
				IntPtr clearVRAsyncRequestResponseRawObject = argClearVRAsyncRequestResponseJavaObject.GetRawObject();
				mediaPlayer.CbClearVRCoreWrapperRequestCompleted(JNIBridgeClearVRAsyncRequestResponse.GetClearVRAsyncRequestResponseFromClearVRAsyncRequestResponseRawObject(clearVRAsyncRequestResponseRawObject),
					JNIBridgeClearVRAsyncRequestResponse.GetClearVRMessageFromClearVRAsyncRequestResponseRawObject(clearVRAsyncRequestResponseRawObject));
			}

			public void cbClearVRCoreWrapperMessage(AndroidJavaObject argClearVRMessageJavaObject) {
				ClearVRMessage clearVRMessage = JNIBridgeClearVRMessage.GetClearVRMessageFromRawJavaObject(argClearVRMessageJavaObject.GetRawObject());
				mediaPlayer.CbClearVRCoreWrapperMessage(clearVRMessage);
			}
		}

		/*
		This class implements com.tiledmedia.clearvrcorewrapper.StaticAsyncResponseInterface java interface as an AndroidJavaProxy
		*/
		internal class StaticAsyncResponseInterface : AndroidJavaProxy {
			/* The underlying SDK makes sure that this callback is always invoked from the main thread. Otherwise, random SEGFAULTS are to be expected! */
			private Action<ClearVRMessage> cbClearVRMessageActionCallback;
			internal const String CLASS_NAME = "com.tiledmedia.clearvrcorewrapper.StaticAsyncResponseInterface";
			internal const String JNI_CLASS_NAME = "Lcom/tiledmedia/clearvrcorewrapper/StaticAsyncResponseInterface;";

			public StaticAsyncResponseInterface(Action<ClearVRMessage> argCbClearVRMessage) : base(CLASS_NAME) {
				cbClearVRMessageActionCallback = argCbClearVRMessage;
			}

			public void cbResponse(AndroidJavaObject argClearVRMessageJavaObject) {
				ClearVRMessage clearVRMessage = JNIBridgeClearVRMessage.GetClearVRMessageFromRawJavaObject(argClearVRMessageJavaObject.GetRawObject());
				cbClearVRMessageActionCallback(clearVRMessage);
				Release();
			}

			public IntPtr CreateJavaProxy() {
				// We do NOT need a GlobalRef for the callback interface.
				return AndroidJNIHelper.CreateJavaProxy(this);
			}

			public void Release() {
				// Intentionally left empty
			}
		}

		internal class JNIBridgeClearVRCoreWrapper : JNIBridgeBase<JNIBridgeClearVRCoreWrapper> {
			internal static IntPtr initializeMethodId = IntPtr.Zero;
			internal static IntPtr prepareContentForPlayoutMethodId = IntPtr.Zero;
			internal static IntPtr getParameterSafelyMethodId = IntPtr.Zero;
			internal static IntPtr getContentParameterSafelyMethodId = IntPtr.Zero;
			internal static IntPtr getContentArrayParameterSafelyMethodId = IntPtr.Zero;
			internal static IntPtr getArrayParameterSafelyMethodId = IntPtr.Zero;
			internal static IntPtr setParameterMethodId = IntPtr.Zero;
			internal static IntPtr startPlayoutMethodId = IntPtr.Zero;
			internal static IntPtr pauseMethodId = IntPtr.Zero;
			internal static IntPtr unpauseMethodId = IntPtr.Zero;
			internal static IntPtr callCoreMethodId = IntPtr.Zero;
			internal static IntPtr callCoreStaticMethodId = IntPtr.Zero;
			internal static IntPtr callCoreSyncMethodId = IntPtr.Zero;
			internal static IntPtr callCoreStaticSyncMethodId = IntPtr.Zero;
			internal static IntPtr loadStateMethodId = IntPtr.Zero;
			internal static IntPtr populateMediaInfoMethodId = IntPtr.Zero;
			internal static IntPtr stopClearVRCoreMethodId = IntPtr.Zero;
			internal static IntPtr sendSensorDataMethodId = IntPtr.Zero;
			internal static IntPtr getAverageBitrateInKbpsMethodId = IntPtr.Zero;
			internal static IntPtr muteAudioMethodId = IntPtr.Zero;
			internal static IntPtr unmuteAudioMethodId = IntPtr.Zero;
			internal static IntPtr setAudioGainMethodId = IntPtr.Zero;
			internal static IntPtr getAudioGainMethodId = IntPtr.Zero;
			internal static IntPtr getIsAudioMutedMethodId = IntPtr.Zero;
			internal static IntPtr seekMethodId = IntPtr.Zero;
			internal static IntPtr switchContentMethodId = IntPtr.Zero;
			internal static IntPtr setStereoscopicModeMethodId = IntPtr.Zero;
			internal static IntPtr getDeviceAppIdMethodId = IntPtr.Zero;
			internal static IntPtr getVersionMethodId = IntPtr.Zero;
			internal static IntPtr getProxyParametersMethodId = IntPtr.Zero;
			internal static IntPtr testIsContentSupportedMethodId = IntPtr.Zero;
			internal static IntPtr getTimingReportMethodId = IntPtr.Zero;
			internal static IntPtr getMuteStateMethodId = IntPtr.Zero;
			internal static IntPtr clearVRCoreLogMethodId = IntPtr.Zero;
			internal static IntPtr getIsHardwareHEVCDecoderAvailableMethodId = IntPtr.Zero;
			internal static String CLASS_NAME = "com/tiledmedia/clearvrcorewrapper/ClearVRCoreWrapper";

			internal JNIBridgeClearVRCoreWrapper() : base(CLASS_NAME, String.Format("({0})V", JNIBridgeClearVRCoreWrapperConstructorParameters.jniClassName)) {
			}

			[Preserve]
			internal static void Setup() {
 				/* static methods */
				getVersionMethodId = AndroidJNI.GetStaticMethodID(pClassGlobalRef, "getClearVRCoreVersion", "()Ljava/lang/String;");
				getIsHardwareHEVCDecoderAvailableMethodId = AndroidJNI.GetStaticMethodID(pClassGlobalRef, "getIsHardwareHEVCDecoderAvailable", "()Z");
				testIsContentSupportedMethodId = AndroidJNI.GetStaticMethodID(pClassGlobalRef, "testIsContentSupported", String.Format("(Landroid/app/Activity;[B{0})V", StaticAsyncResponseInterface.JNI_CLASS_NAME));
				callCoreStaticMethodId = AndroidJNI.GetStaticMethodID(pClassGlobalRef, "callCoreStatic", String.Format("(Ljava/lang/String;{0})V", StaticAsyncResponseInterface.JNI_CLASS_NAME));
				callCoreStaticSyncMethodId = AndroidJNI.GetStaticMethodID(pClassGlobalRef, "callCoreStaticSync", "(Ljava/lang/String;)Ljava/lang/String;");
				loadStateMethodId = AndroidJNI.GetStaticMethodID(pClassGlobalRef, "loadState", "(Ljava/lang/String;Landroid/app/Activity;)Ljava/lang/String;");
				getProxyParametersMethodId = AndroidJNI.GetStaticMethodID(pClassGlobalRef, "getProxyParameters", "(Ljava/lang/String;)Ljava/lang/String;");
				/* instance methods */
				initializeMethodId = AndroidJNI.GetMethodID(pClassGlobalRef,"initialize", String.Format("(Landroid/view/Surface;[B){0}", JNIBridgeClearVRAsyncRequest.jniClassName));
				prepareContentForPlayoutMethodId = AndroidJNI.GetMethodID(pClassGlobalRef,"prepareContentForPlayout", String.Format("([B){0}", JNIBridgeClearVRAsyncRequest.jniClassName));
				sendSensorDataMethodId = AndroidJNI.GetMethodID(pClassGlobalRef, "sendSensorData", "(DDDDDDDDDDDDDDDDD)V");
				getParameterSafelyMethodId = AndroidJNI.GetMethodID(pClassGlobalRef,"getParameterSafely", "(Ljava/lang/String;)Ljava/lang/String;");
				getContentParameterSafelyMethodId = AndroidJNI.GetMethodID(pClassGlobalRef,"getContentParameterSafely", "(ILjava/lang/String;)Ljava/lang/String;");
				getContentArrayParameterSafelyMethodId = AndroidJNI.GetMethodID(pClassGlobalRef,"getContentArrayParameterSafely", "(ILjava/lang/String;I)Ljava/lang/String;");
				
				getArrayParameterSafelyMethodId = AndroidJNI.GetMethodID(pClassGlobalRef,"getArrayParameterSafely", "(Ljava/lang/String;I)Ljava/lang/String;");
				setParameterMethodId = AndroidJNI.GetMethodID(pClassGlobalRef,"setParameterSafely", "(Ljava/lang/String;Ljava/lang/String;)Z");
				startPlayoutMethodId = AndroidJNI.GetMethodID(pClassGlobalRef,"startPlayout", String.Format("(){0}", JNIBridgeClearVRAsyncRequest.jniClassName));

				pauseMethodId = AndroidJNI.GetMethodID(pClassGlobalRef, "pause", String.Format("(){0}", JNIBridgeClearVRAsyncRequest.jniClassName));
				unpauseMethodId = AndroidJNI.GetMethodID(pClassGlobalRef, "unpause", String.Format("([B){0}", JNIBridgeClearVRAsyncRequest.jniClassName));
				callCoreMethodId = AndroidJNI.GetMethodID(pClassGlobalRef, "callCore", String.Format("(Ljava/lang/String;){0}", JNIBridgeClearVRAsyncRequest.jniClassName));
				callCoreSyncMethodId = AndroidJNI.GetMethodID(pClassGlobalRef, "callCoreSync", "(Ljava/lang/String;)Ljava/lang/String;");

				muteAudioMethodId = AndroidJNI.GetMethodID(pClassGlobalRef, "muteAudio", "()Z");
				unmuteAudioMethodId = AndroidJNI.GetMethodID(pClassGlobalRef, "unmuteAudio", "()Z");
				setAudioGainMethodId = AndroidJNI.GetMethodID(pClassGlobalRef, "setAudioGain", "(F)V");
				getAudioGainMethodId = AndroidJNI.GetMethodID(pClassGlobalRef, "getAudioGain", "()F");
				getIsAudioMutedMethodId = AndroidJNI.GetMethodID(pClassGlobalRef, "getIsAudioMuted", "()Z");

				stopClearVRCoreMethodId = AndroidJNI.GetMethodID(pClassGlobalRef, "stopClearVRCore", String.Format("({0}){1}", JNIBridgeStopClearVRCoreParameters.jniClassName, JNIBridgeClearVRAsyncRequest.jniClassName));
				populateMediaInfoMethodId = AndroidJNI.GetMethodID(pClassGlobalRef, "populateMediaInfo", String.Format("([B){0}", JNIBridgeClearVRAsyncRequest.jniClassName));
				getAverageBitrateInKbpsMethodId = AndroidJNI.GetMethodID(pClassGlobalRef, "getAverageBitrateInKbps", "()I");
				seekMethodId = AndroidJNI.GetMethodID(pClassGlobalRef, "seek", String.Format("([B){0}", JNIBridgeClearVRAsyncRequest.jniClassName));
				getDeviceAppIdMethodId = AndroidJNI.GetMethodID(pClassGlobalRef,"getDeviceAppId", "()Ljava/lang/String;");
				switchContentMethodId = AndroidJNI.GetMethodID(pClassGlobalRef, "switchContent", String.Format("([B){0}", JNIBridgeClearVRAsyncRequest.jniClassName));
				setStereoscopicModeMethodId = AndroidJNI.GetMethodID(pClassGlobalRef, "setStereoscopicMode", String.Format("(Z){0}", JNIBridgeClearVRAsyncRequest.jniClassName));
				getTimingReportMethodId = AndroidJNI.GetMethodID(pClassGlobalRef, "getTimingReport", String.Format("({0})[B", JNIBridgeTimingTypes.jniClassName));
				getMuteStateMethodId = AndroidJNI.GetMethodID(pClassGlobalRef, "getMuteState", "()F");
			}

			/// <summary>
			/// Small convenience method for getting the ClearVRCore version string.
			/// </summary>
			/// <returns>The version of the ClearVRCore as a string</returns>
			public static ClearVRAsyncRequest CallCore(IntPtr argClearVRCoreWrapperGlobalRef, String argBase64Message) {
				if(callCoreMethodId == IntPtr.Zero || argClearVRCoreWrapperGlobalRef == IntPtr.Zero) {
					throw new Exception("[ClearVR] Cannot handle CallCore() API. Not properly initialized. Fix your code.");
				}
				jvalue[] parms = new jvalue[1];
				parms[0].l = AndroidJNI.NewStringUTF(argBase64Message);
				IntPtr clearVRAsyncRequestRawObject = AndroidJNI.CallObjectMethod(argClearVRCoreWrapperGlobalRef, callCoreMethodId, parms);
				AndroidJNI.DeleteLocalRef(parms[0].l);
				return JNIBridges.JNIBridgeClearVRAsyncRequest.ConvertClearVRAsyncRequestAsRawObjectToClearVRAsyncRequest(clearVRAsyncRequestRawObject);
			}

			public static void CallCoreStatic(String base64Message, Action<ClearVRMessage> cbClearVRMessage) {
				IntPtr _callCoreStaticMethodId = callCoreStaticMethodId;
				IntPtr _pClass = pClassGlobalRef;
				StaticAsyncResponseInterface externalInterface = new StaticAsyncResponseInterface(cbClearVRMessage);
				jvalue[] jvalues = new jvalue[2];
				jvalues[0].l = AndroidJNI.NewStringUTF(base64Message);
				jvalues[1].l = externalInterface.CreateJavaProxy();
				if(_pClass == IntPtr.Zero) {
					_pClass = AndroidJNI.FindClass(JNIBridgeClearVRCoreWrapper.CLASS_NAME); // Must be released as it returns a LocalRef
					_callCoreStaticMethodId = AndroidJNI.GetStaticMethodID(_pClass, "callCoreStatic", String.Format("(Ljava/lang/String;){0}", JNIBridgeClearVRAsyncRequest.jniClassName));
				}
				AndroidJNI.CallStaticVoidMethod(_pClass, _callCoreStaticMethodId, jvalues);
				if(pClassGlobalRef == IntPtr.Zero) {
					// We created a local ref, so we free it.
					AndroidJNI.DeleteLocalRef(_pClass);
				}
				AndroidJNI.DeleteLocalRef(jvalues[0].l);
				// Of course, we cannot delete our GlobalRef of the callback interface here (jvalues[0].l). That would invalidate it before it gets triggered.
			}

			public static String CallCoreStaticSync(String base64Message) {
				IntPtr _callCoreStaticSyncMethodId = callCoreStaticSyncMethodId;
				IntPtr _pClass = pClassGlobalRef;
				jvalue[] jvalues = new jvalue[1];
				jvalues[0].l = AndroidJNI.NewStringUTF(base64Message);
				if(_pClass == IntPtr.Zero) {
					_pClass = AndroidJNI.FindClass(JNIBridgeClearVRCoreWrapper.CLASS_NAME); // Must be released as it returns a LocalRef
					_callCoreStaticSyncMethodId = AndroidJNI.GetStaticMethodID(pClassGlobalRef, "callCoreStaticSync", "(Ljava/lang/String;)Ljava/lang/String;");
				}
				String result = AndroidJNI.CallStaticStringMethod(_pClass, _callCoreStaticSyncMethodId, jvalues);
				if( pClassGlobalRef == IntPtr.Zero) {
					// We created a local ref, so we free it.
					AndroidJNI.DeleteLocalRef(_pClass);
				}
				AndroidJNI.DeleteLocalRef(jvalues[0].l);
				return result;
			}

			public static String LoadState(String base64Message, IntPtr activityGlobalRef) {
				IntPtr _loadStateMethodId = loadStateMethodId;
				IntPtr _pClass = pClassGlobalRef;
				jvalue[] jvalues = new jvalue[2];
				jvalues[0].l = AndroidJNI.NewStringUTF(base64Message);
				jvalues[1].l = activityGlobalRef;
				if (_pClass == IntPtr.Zero) {
					_pClass = AndroidJNI.FindClass(JNIBridgeClearVRCoreWrapper.CLASS_NAME); // Must be released as it returns a LocalRef
					_loadStateMethodId = AndroidJNI.GetStaticMethodID(pClassGlobalRef, "loadState", "(Ljava/lang/String;Landroid/app/Activity;)Ljava/lang/String;");
				}
				String result = AndroidJNI.CallStaticStringMethod(_pClass, _loadStateMethodId, jvalues);
				if (pClassGlobalRef == IntPtr.Zero) {
					// We created a local ref, so we free it.
					AndroidJNI.DeleteLocalRef(_pClass);
				}
				AndroidJNI.DeleteLocalRef(jvalues[0].l);
				return result;
			}

			/// <summary>
			/// Small convenience method for getting the ClearVRCore version string.
			/// </summary>
			/// <returns>The version of the ClearVRCore as a string</returns>
			public static String GetClearVRCoreVersion() {
				String version = "";
				if(getVersionMethodId == IntPtr.Zero) {
					// We have to grab a hold of our
					IntPtr clearVRCoreWrapperClass = AndroidJNI.FindClass(JNIBridgeClearVRCoreWrapper.CLASS_NAME); // Must be released as it returns a LocalRef
					IntPtr getVersionMethodId = AndroidJNI.GetStaticMethodID(clearVRCoreWrapperClass, "getClearVRCoreVersion", "()Ljava/lang/String;");
					version = AndroidJNI.CallStaticStringMethod(clearVRCoreWrapperClass, getVersionMethodId, new jvalue[0]);
					AndroidJNI.DeleteLocalRef(clearVRCoreWrapperClass);
				} else {
					version = AndroidJNI.CallStaticStringMethod(pClassGlobalRef, getVersionMethodId, new jvalue[0]);
				}
				return version;
			}

			/// <summary>
			/// Convenience method for determining whether an HEVC video decoder is available or not.
			/// </summary>
			/// <returns>True in case a hardware HEVC video decoder is present, false otherwise.</returns>
			public static bool GetIsHardwareHEVCDecoderAvailable() {
				bool isAvailable = false;
				if(getIsHardwareHEVCDecoderAvailableMethodId == IntPtr.Zero) {
					// We have to grab a hold of our
					IntPtr clearVRCoreWrapperClass = AndroidJNI.FindClass(JNIBridgeClearVRCoreWrapper.CLASS_NAME); // Must be released as it returns a LocalRef
					IntPtr getIsHardwareHEVCDecoderAvailableMethodId = AndroidJNI.GetStaticMethodID(clearVRCoreWrapperClass, "getIsHardwareHEVCDecoderAvailable", "()Z");
					isAvailable = AndroidJNI.CallStaticBooleanMethod(clearVRCoreWrapperClass, getIsHardwareHEVCDecoderAvailableMethodId, new jvalue[0]);
					AndroidJNI.DeleteLocalRef(clearVRCoreWrapperClass);
				} else {
					isAvailable = AndroidJNI.CallStaticBooleanMethod(pClassGlobalRef, getIsHardwareHEVCDecoderAvailableMethodId, new jvalue[0]);
				}
				return isAvailable;
			}

			public static String GetProxyParameters(String base64Message) {
				IntPtr _proxyParameterMethodId = getProxyParametersMethodId;
				IntPtr _pClass = pClassGlobalRef;
				jvalue[] jvalues = new jvalue[1];
				jvalues[0] = new jvalue();
				jvalues[0].l = AndroidJNI.NewStringUTF(base64Message);
				if(_pClass == IntPtr.Zero) {
					_pClass = AndroidJNI.FindClass(JNIBridgeClearVRCoreWrapper.CLASS_NAME); // Must be released as it returns a LocalRef
					_proxyParameterMethodId = AndroidJNI.GetStaticMethodID(_pClass, "getProxyParameters", "(Ljava/lang/String;)Ljava/lang/String;");
				}
				String msg = AndroidJNI.CallStaticStringMethod(_pClass, JNIBridges.JNIBridgeClearVRCoreWrapper.getProxyParametersMethodId, jvalues);
				if(pClassGlobalRef == IntPtr.Zero) {
					// We created a local ref, so we free it.
					AndroidJNI.DeleteLocalRef(_pClass);
				}
				AndroidJNI.DeleteLocalRef(jvalues[0].l);
				return msg;
			}

			/// <summary>
			/// Bridge into the ContentSupported tester.
			/// </summary>
			public static void TestIsContentSupported(IntPtr argActivityGlobalRef, ContentSupportedTesterParameters argContentSupportedTesterParameters, Action<ClearVRMessage> argCbClearVRMessage) {
				IntPtr _testIsContentSupportedMethodId = testIsContentSupportedMethodId;
				IntPtr _pClass = pClassGlobalRef;
				StaticAsyncResponseInterface externalInterface = new StaticAsyncResponseInterface(argCbClearVRMessage);
				byte[] raw = argContentSupportedTesterParameters.ToCoreProtobuf().ToByteArray();
				jvalue[] jvalues = new jvalue[3];
				jvalues[0].l = argActivityGlobalRef;
				jvalues[1].l = JNIBridges.ByteArrayToLocalRef(raw);
				jvalues[2].l = externalInterface.CreateJavaProxy();
				if(_pClass == IntPtr.Zero) {
					_pClass = AndroidJNI.FindClass(JNIBridgeClearVRCoreWrapper.CLASS_NAME); // Must be released as it returns a LocalRef
					_testIsContentSupportedMethodId = AndroidJNI.GetStaticMethodID(_pClass, "testIsContentSupported", String.Format("(Landroid/app/Activity;[B{0})V", StaticAsyncResponseInterface.JNI_CLASS_NAME));
				}
				AndroidJNI.CallStaticVoidMethod(_pClass, _testIsContentSupportedMethodId, jvalues);
				if(pClassGlobalRef == IntPtr.Zero) {
					// We created a local ref, so we free it.
					AndroidJNI.DeleteLocalRef(_pClass);
				}
				AndroidJNI.DeleteLocalRef(jvalues[1].l);
				// Of course, we cannot delete our GlobalRef of the callback interface here (jvalues[0].l). That would invalidate it before it gets triggered.
			}
		}

		private static IntPtr wrapperGLobalRefStaticForLogging = IntPtr.Zero; // We never DeleteGlobalRef this one because logging should be available at any time in a static way
		private static IntPtr clearVRCoreLogMethodIdStaticForLogging = IntPtr.Zero;
		internal static void ClearVRCoreLog(String msg, LogLevels logLevel) {
			jvalue[] jvalues = new jvalue[3];
			jvalues[0].l = AndroidJNI.NewStringUTF(msg);
			jvalues[1].i = (int) LogComponents.Sdk; // we hardcode the fact that log message is coming from the SDK
			jvalues[2].i = (int) logLevel;
			if (wrapperGLobalRefStaticForLogging == IntPtr.Zero) {
				IntPtr clearVRCoreWrapperClass = AndroidJNI.FindClass(JNIBridgeClearVRCoreWrapper.CLASS_NAME); 
				wrapperGLobalRefStaticForLogging = AndroidJNI.NewGlobalRef(clearVRCoreWrapperClass);
				clearVRCoreLogMethodIdStaticForLogging = AndroidJNI.GetStaticMethodID(clearVRCoreWrapperClass, "clearVRCoreLog", "(Ljava/lang/String;II)V");
			}
			AndroidJNI.CallStaticVoidMethod(wrapperGLobalRefStaticForLogging, clearVRCoreLogMethodIdStaticForLogging, jvalues);
			AndroidJNI.DeleteLocalRef(jvalues[0].l);
		}

		public static void Release() {
			JNIBridgeClearVRMessageTypes.Release();
			JNIBridgeClearVRRequestTypes.Release();
			JNIBridgeTimingTypes.Release();
			JNIBridgeClearVRCoreWrapperConstructorParameters.Release();
			JNIBridgeStopClearVRCoreParameters.Release();
			JNIBridgeClearVRMessage.Release();
			JNIBridgeClearVRAsyncRequest.Release();
			JNIBridgeClearVRAsyncRequestResponse.Release();
			JNIBridgeClearVRCoreWrapper.Release();

			//jniBridgeClearVRCoreWrapper.Release();
			// Do not release these GlobalRefs for now as that results in a crash.
			// Note that we never freed them in the first place, but this is a TODO
			// if(clearVRInitializeParametersRawClassGlobalRef != IntPtr.Zero) {
			// 	AndroidJNI.DeleteGlobalRef(clearVRInitializeParametersRawClassGlobalRef);
			// 	clearVRInitializeParametersRawClassGlobalRef = IntPtr.Zero;
			// }
			// if(initializeParametersGlobalRef != IntPtr.Zero) {
			// 	AndroidJNI.DeleteGlobalRef(initializeParametersGlobalRef);
			// }
			// initializeParametersGlobalRef = IntPtr.Zero;
			// if(clearVRInitializeParametersClassConstructorMethodId != IntPtr.Zero) {
			// 	AndroidJNI.DeleteGlobalRef(clearVRInitializeParametersClassConstructorMethodId);
			// }
			// clearVRInitializeParametersClassConstructorMethodId = IntPtr.Zero;
		}

	}
}
#endif
