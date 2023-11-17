#if UNITY_ANDROID && !UNITY_EDITOR
/*
Tiledmedia Sample Player for Unity3D
(c) Tiledmedia, 2017 - 2018

Author: Arjen Veenhuizen
Contact: arjen@tiledmedia.com

*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System.Reflection;
using System;
using UnityEngine.XR;
using System.Diagnostics;
using System.Threading;
using AOT;
using System.Runtime.CompilerServices;
using com.tiledmedia.clearvr.protobuf;
using com.tiledmedia.clearvr.cvrinterface;

namespace com.tiledmedia.clearvr {
	internal class MediaPlayerAndroid : MediaPlayerBase {

		private void SetJvaluesWithoutVelocity(ref _ClearVRViewportAndObjectPose vop_c, ref jvalue[] sendSensorDataParams) {
			int index = 0;
			sendSensorDataParams[index++].d = vop_c.viewportPose.posX;
			sendSensorDataParams[index++].d = vop_c.viewportPose.posY;
			sendSensorDataParams[index++].d = vop_c.viewportPose.posZ;
			sendSensorDataParams[index++].d = vop_c.viewportPose.w;
			sendSensorDataParams[index++].d = vop_c.viewportPose.x;
			sendSensorDataParams[index++].d = vop_c.viewportPose.y;
			sendSensorDataParams[index++].d = vop_c.viewportPose.z;

			sendSensorDataParams[index++].d = vop_c.displayObject.pose.posX;
			sendSensorDataParams[index++].d = vop_c.displayObject.pose.posY;
			sendSensorDataParams[index++].d = vop_c.displayObject.pose.posZ;
			sendSensorDataParams[index++].d = vop_c.displayObject.pose.w;
			sendSensorDataParams[index++].d = vop_c.displayObject.pose.x;
			sendSensorDataParams[index++].d = vop_c.displayObject.pose.y;
			sendSensorDataParams[index++].d = vop_c.displayObject.pose.z;
			sendSensorDataParams[index++].d = vop_c.displayObject.scale.x;
			sendSensorDataParams[index++].d = vop_c.displayObject.scale.y;
			sendSensorDataParams[index++].d = vop_c.displayObject.scale.z;
		}


		/* Global raw object reference. Only AndroidJNI.DeleteGlobalRef() this pointer when you are no longer calling any native library method */
		private IntPtr clearVRCoreWrapperGlobalRef = IntPtr.Zero;
		/* Global raw object reference of the current activity. */
		private static IntPtr activityRawObjectGlobalRef {
			get {
				if(_activityRawObjectGlobalRef == IntPtr.Zero) {
					AndroidJavaClass jc = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
					_activityRawObjectGlobalRef = AndroidJNI.NewGlobalRef(jc.GetStatic<AndroidJavaObject>("currentActivity").GetRawObject());
				}
				return _activityRawObjectGlobalRef;
			}
		}

		private static IntPtr _activityRawObjectGlobalRef = IntPtr.Zero;

		// These values are only set when running in GearVR or Oculus Go
		private Vector3 ovrCameraAngularAcceleration;
		private Vector3 ovrCameraAngularVelocity;

		/* class private pointers to the various methods that are exposed in the ClearVRCoreWrapper class */

#pragma warning disable 0414 // silence declared but not used warning.
		private JNIBridges _androidJNIBridge = new JNIBridges(); // unused, but this forcefully intializes the JNIBridg
#pragma warning restore 0414
		private IntPtr clearVRCoreWrapperExternalInterfaceJavaProxyGlobalRef = IntPtr.Zero;
		// private jvalue[] sendSensorDataParams = new jvalue[1];//new jvalue[17]; // ViewportAndDisplayObjectPose
		private jvalue[] sendSensorDataParams = new jvalue[17]; // ViewportAndDisplayObjectPose
		/* We keep track of angular velocity and acceleration when running on the GearVR and Oculus GO */
		protected object ovrDisplay = null;
	    protected delegate Vector3 AngularVelocityDelegate(); // takes the signature of the OVRDisplay.angularVelocity getter
	    protected delegate Vector3 AngularAccelerationDelegate(); // takes the signature of the OVRDisplay.angularAcceleration getter
	    protected AngularVelocityDelegate angularVelocity;
	    protected AngularAccelerationDelegate angularAcceleration;
		/* Dummy load a simple function from the OVRPlugin. This is used lateron to verify whether the OVRPlugin is at all available or not. */
		[DllImport("OVRPlugin", CallingConvention = CallingConvention.Cdecl)]
		public static extern bool ovrp_GetInitialized();

		/// <summary>
		/// Class constructor
		/// </summary>
		/// <param name="argPlatformOptions">The options to use when constructing the class</param>
		/// <param name="argCbClearVREvent">The app-level ClearVREvent callback to trigger.</param>
		public MediaPlayerAndroid(PlatformOptionsBase argPlatformOptions, ClearVRLayoutManager argClearVRLayoutManager, UnityEngine.Events.UnityAction<MediaPlayerBase,ClearVREvent> argCbClearVREvent) : base(argPlatformOptions, argClearVRLayoutManager, argCbClearVREvent) {
			DoOVRPluginReflection();
		}
		
		/// <summary>
		/// Uses reflection to get the angularVelocity and angularAcceleration fields from the OVRDisplay class if available.
		/// Note: this is dead code (angularVelocity and angularAcceleration are not used anymore), but kept for future reference.
		/// </summary>
		protected void DoOVRPluginReflection() {
#pragma warning disable 0618
			if(!_platformOptions.deviceParameters.deviceType.GetIsAndroidOculusDevice()) {
				return;
			}
#pragma warning restore 0618
			Type ovrDisplayType = Utils.GetType("OVRDisplay");
			if(ovrDisplayType != null) {
				bool isOVRPluginLibraryFound = false;
				try {
					ovrp_GetInitialized();
					isOVRPluginLibraryFound = true;
				} catch (DllNotFoundException) {
					// OVR source code is there, but the OVR plugin cannot be loaded.
				}
				if(isOVRPluginLibraryFound) {
					try {
						ovrDisplay = Activator.CreateInstance(ovrDisplayType);
						PropertyInfo angularVelocityPropertyInfo = ovrDisplayType.GetProperty("angularVelocity");
						PropertyInfo angularAccelerationPropertyInfo = ovrDisplayType.GetProperty("angularAcceleration");
						if(angularVelocityPropertyInfo == null || angularAccelerationPropertyInfo == null){
							ovrDisplay = null; // Cannot track acceleration and/or velocity. Old OVR plugin?
						} else {
							MethodInfo angularVelocityMethodInfo = angularVelocityPropertyInfo.GetGetMethod();
							MethodInfo angularAccelerationMethodInfo = angularAccelerationPropertyInfo.GetGetMethod();
							if(angularVelocityMethodInfo == null || angularAccelerationMethodInfo == null){
								ovrDisplay = null; // Cannot track acceleration and/or velocity. Old OVR plugin?
							} else {
								// We use delegates so that we do not incure the penalty of introspection every time we call this method.
								angularVelocity = (AngularVelocityDelegate) Delegate.CreateDelegate(typeof(AngularVelocityDelegate), ovrDisplay, angularVelocityMethodInfo);
								angularAcceleration = (AngularAccelerationDelegate) Delegate.CreateDelegate(typeof(AngularAccelerationDelegate), ovrDisplay, angularAccelerationMethodInfo);
							}
						}
					} catch { // explicit fallthrough as we do not care for the exception in this case
						ovrDisplay = null;
					}
				} else {
					// no OVRPlugin.so present in application
				}
			}
		}
		
		public override bool _InitializePlatformBindings() {
			AndroidJNIHelper.debug = false;
			if(!base._InitializePlatformBindings()) {
				return false;
			}
            // Initialize the native plugin.
            // Note that this asynchronous call can take one or two frames to complete.
			GetClearVRCoreVersion();
			return true;
		}

		public static string GetClearVRCoreVersion() {
			new JNIBridges(); // Make sure our JNIBridges are initialized.
			if(String.IsNullOrEmpty(_clearVRCoreVersionString) || _clearVRCoreVersionString.Equals(MediaPlayerBase.DEFAULT_CLEAR_VR_CORE_VERSION_STRING)) {
				_clearVRCoreVersionString = JNIBridges.JNIBridgeClearVRCoreWrapper.GetClearVRCoreVersion();
			}
			return _clearVRCoreVersionString;
		}

		internal static bool GetIsHardwareHEVCDecoderAvailable() {
			new JNIBridges(); // Make sure our JNIBridges are initialized.
            return JNIBridges.JNIBridgeClearVRCoreWrapper.GetIsHardwareHEVCDecoderAvailable();
        }


		public static string GetProxyParameters(String base64Message) {
			// Make sure our JNIBridges are initialized.
			new JNIBridges();
			return JNIBridges.JNIBridgeClearVRCoreWrapper.GetProxyParameters(base64Message);
		}

		public static void TestIsContentSupported(ContentSupportedTesterParameters contentSupportedTesterParameters, Action<ClearVRMessage> cbClearVRMessage) {
			// Make sure our JNIBridges are inited
			new JNIBridges();
			JNIBridges.JNIBridgeClearVRCoreWrapper.TestIsContentSupported(activityRawObjectGlobalRef, contentSupportedTesterParameters, cbClearVRMessage);
		}

		public static void CallCoreStatic(String base64Message, Action<ClearVRMessage> cbClearVRMessage) {
			// Make sure our JNIBridges are initialized.
			new JNIBridges();
			JNIBridges.JNIBridgeClearVRCoreWrapper.CallCoreStatic(base64Message, cbClearVRMessage);
		}

		public static string CallCoreStaticSync(String base64Message) {
			// Make sure our JNIBridges are initialized.
			new JNIBridges();
			return JNIBridges.JNIBridgeClearVRCoreWrapper.CallCoreStaticSync(base64Message);
		}

		public static string LoadState(String base64Message) {
			// Seperate method for load state because we need the activity.
			// Make sure our JNIBridges are initialized.
			new JNIBridges();
			return JNIBridges.JNIBridgeClearVRCoreWrapper.LoadState(base64Message, activityRawObjectGlobalRef);
		}

		/// <summary>
		/// Called to prepare the underlying libraries for playout.
		/// This will yield a single frame that is used to construct the ClearVRDisplayObjectController.
		/// </summary>
		public override ClearVRAsyncRequest _PrepareCore() {
			if(!_state.Equals(States.Initialized))
				return InvokeClearVRCoreWrapperInvalidStateEvent(String.Format("Cannot prepare core when in {0} state.", GetCurrentStateAsString()), RequestTypes.Initialize);
			SetState(States.PreparingCore, ClearVRMessage.GetGenericOKMessage());
			/* Get Android Surface object pointer from native plugin */
			IntPtr androidSurface = NativeRendererPluginAndroid.CVR_NRP_GetSurfaceObject();
			/* Create ClearVRCoreWrapper External Interface Java Proxy */
			JNIBridges.ClearVRCoreWrapperExternalInterface clearVRCoreWrapperExternalInterface = new JNIBridges.ClearVRCoreWrapperExternalInterface(this);
			clearVRCoreWrapperExternalInterfaceJavaProxyGlobalRef = AndroidJNI.NewGlobalRef(AndroidJNIHelper.CreateJavaProxy(clearVRCoreWrapperExternalInterface));

			jvalue[] constructorParametersGlobalRefAsJValue = JNIBridges.JNIBridgeClearVRCoreWrapperConstructorParameters.GetGlobalRefAsJValues(clearVRCoreWrapperExternalInterfaceJavaProxyGlobalRef, activityRawObjectGlobalRef, "");
			ClearVRAsyncRequest clearVRAsyncRequest;
			try {
				/* Initialize the ClearVRCoreWrapper class which extends the Thread class */
				clearVRCoreWrapperGlobalRef = JNIBridges.JNIBridgeClearVRCoreWrapper.ConstructGlobalRef(constructorParametersGlobalRefAsJValue);
				JNIBridges.DeleteGlobalRefFromJValueArray(constructorParametersGlobalRefAsJValue);
				/* Start the ClearVRCoreWrapper thread */
				jvalue[] jValues = new jvalue[2];
				jValues[0].l = androidSurface;
				jValues[1].l = JNIBridges.ByteArrayToLocalRef(_platformOptions.ToCoreProtobuf().ToByteArray());
				IntPtr clearVRAsyncRequestRawObject = AndroidJNI.CallObjectMethod(clearVRCoreWrapperGlobalRef, JNIBridges.JNIBridgeClearVRCoreWrapper.initializeMethodId, jValues);
				// We do not DeleteGlobalRef the AndroidSurface object
				AndroidJNI.DeleteLocalRef(jValues[1].l);
				clearVRAsyncRequest = JNIBridges.JNIBridgeClearVRAsyncRequest.ConvertClearVRAsyncRequestAsRawObjectToClearVRAsyncRequest(clearVRAsyncRequestRawObject);
				NativeRendererPluginAndroid.CVR_NRP_SetClearVRCoreWrapperObject(clearVRCoreWrapperGlobalRef);

			} catch (Exception argException) {
				throw new Exception(String.Format("[ClearVR] Unable to initialize ClearVR! Error: {0}", argException));
			}
			// Get a reference to methods exposed by our library

			/* Initialize statistics */
			_clearVRCoreWrapperStatistics = new StatisticsAndroid(JNIBridges.JNIBridgeClearVRCoreWrapper.pClassGlobalRef, clearVRCoreWrapperGlobalRef);
			return clearVRAsyncRequest;
		}

		public override void Update() {
			//System.Console.Write("[Unity] Update()");
   		    base.UpdateClearVREventsToInvokeQueue();
		}

		private _ClearVRViewportAndObjectPose _vopC = new _ClearVRViewportAndObjectPose();
		public override void SendSensorInfo() {
			UpdatePoseForSendSensorInfo(ref _vopC);
			SetJvaluesWithoutVelocity(ref _vopC, ref sendSensorDataParams);
			AndroidJNI.CallVoidMethod(clearVRCoreWrapperGlobalRef, JNIBridges.JNIBridgeClearVRCoreWrapper.sendSensorDataMethodId, sendSensorDataParams);
		}

		public override ClearVRAsyncRequest _PrepareContentForPlayout(PrepareContentParameters argPrepareContentParameters) {
			jvalue[] prepareContentParametersLocalRef = new jvalue[1];
			prepareContentParametersLocalRef[0].l = JNIBridges.ByteArrayToLocalRef(argPrepareContentParameters.ToCoreProtobuf(_platformOptions).ToByteArray());
			IntPtr clearVRAsyncRequestRawObject = AndroidJNI.CallObjectMethod(clearVRCoreWrapperGlobalRef, JNIBridges.JNIBridgeClearVRCoreWrapper.prepareContentForPlayoutMethodId, prepareContentParametersLocalRef);
			AndroidJNI.DeleteLocalRef(prepareContentParametersLocalRef[0].l);
			return JNIBridges.JNIBridgeClearVRAsyncRequest.ConvertClearVRAsyncRequestAsRawObjectToClearVRAsyncRequest(clearVRAsyncRequestRawObject);			
		}

		/// <summary>
		/// This method is directly linked to ClearVRCoreWrapper.getParameter().
		/// See the ClearVRCore docs for details about valid keys and when they can be queried. Note that this will return an empty string in case you try to query a parameter
		/// when in an illegal state.
		/// </summary>
		/// <param name="argKey"></param> The key to query.
		/// <returns>The value of the queried key. </returns>
		public override String GetClearVRCoreParameter(String argKey) {
			if(!GetIsPlayerBusy())
				return "";
			jvalue[] parms = new jvalue[1];
			parms[0] = new jvalue();
			parms[0].l = AndroidJNI.NewStringUTF(argKey);
			string output = "";
			try {
				output = AndroidJNI.CallStringMethod(clearVRCoreWrapperGlobalRef, JNIBridges.JNIBridgeClearVRCoreWrapper.getParameterSafelyMethodId, parms);
			} catch (Exception e) { // catches JNI errors only.
				UnityEngine.Debug.LogWarning(String.Format("[ClearVR] An exception was thrown while accessing key '{0}' from ClearVRCore. Error: {1}", argKey, e.ToString()));
			}
			AndroidJNI.DeleteLocalRef(parms[0].l);
			return output;
		}

		public override String GetClearVRCoreContentParameter(int argContentID, String argKey) {
			if(!GetIsPlayerBusy())
				return "";
			jvalue[] parms = new jvalue[2];
			parms[0].i = argContentID;
			parms[1].l = AndroidJNI.NewStringUTF(argKey);
			string output = "";
			try {
				output = AndroidJNI.CallStringMethod(clearVRCoreWrapperGlobalRef, JNIBridges.JNIBridgeClearVRCoreWrapper.getContentParameterSafelyMethodId, parms);
			} catch (Exception e) { // catches JNI errors only.
				UnityEngine.Debug.LogWarning(String.Format("[ClearVR] An exception was thrown while accessing key '{0}' for ContentID: {1} from ClearVRCore. Error: {2}", argKey, argContentID, e.ToString()));
			}
			AndroidJNI.DeleteLocalRef(parms[1].l);
			return output;
		}

		public override float GetMuteState() {
			if(!hasReachedPreparingContentForPlayoutState) { // Only by this point, the initial settings have been signalled to wrapper.
				return _platformOptions.muteState;
			}
			return AndroidJNI.CallFloatMethod(clearVRCoreWrapperGlobalRef, JNIBridges.JNIBridgeClearVRCoreWrapper.getMuteStateMethodId, JNIBridges.emptyJvalue);
		}

		public override String GetDeviceAppId() {
			if(!GetIsPlayerBusy())
				return "";
			return AndroidJNI.CallStringMethod(clearVRCoreWrapperGlobalRef, JNIBridges.JNIBridgeClearVRCoreWrapper.getDeviceAppIdMethodId, JNIBridges.emptyJvalue);
		}

		/// <summary>
		/// This method is directly linked to ClearVRCoreWrapper.getParameter().
		/// See the ClearVRCore docs for details about valid keys and when they can be queried. Note that this will return an empty string in case you try to query a parameter
		/// when in an illegal state.
		/// </summary>
		/// <param name="argKey"></param> The key to query.
		/// <returns>The value of the queried key. </returns>
		public override String GetClearVRCoreArrayParameter(String argKey, int argIndex) {
			if(!GetIsPlayerBusy())
				return "";
			jvalue[] parms = new jvalue[2];
			parms[0].l = AndroidJNI.NewStringUTF(argKey);
			parms[1].i = argIndex;
			string output = "";
			try {
				output = AndroidJNI.CallStringMethod(clearVRCoreWrapperGlobalRef, JNIBridges.JNIBridgeClearVRCoreWrapper.getArrayParameterSafelyMethodId, parms);
			} catch (Exception e) { // catches JNI errors only.
				UnityEngine.Debug.LogWarning(String.Format("[ClearVR] An exception was thrown while accessing key '{0}'[{1}] from ClearVRCore. Error: {2}", argKey, argIndex, e.ToString()));
			}
			AndroidJNI.DeleteLocalRef(parms[0].l);
			return output;
		}

		public override String GetClearVRCoreContentArrayParameter(int argContentID, String argKey, int argIndex) {
			if(!GetIsPlayerBusy())
				return "";
			jvalue[] parms = new jvalue[3];
			parms[0].i = argContentID;
			parms[1].l = AndroidJNI.NewStringUTF(argKey);
			parms[2].i = argIndex;
			string output = "";
			try {
				output = AndroidJNI.CallStringMethod(clearVRCoreWrapperGlobalRef, JNIBridges.JNIBridgeClearVRCoreWrapper.getContentArrayParameterSafelyMethodId, parms);
			} catch (Exception e) { // catches JNI errors only.
				UnityEngine.Debug.LogWarning(String.Format("[ClearVR] An exception was thrown while accessing key '{0}'[{1}] for ContentID: {2} from ClearVRCore. Error: {3}", argKey, argIndex, argContentID, e.ToString()));
			}
			AndroidJNI.DeleteLocalRef(parms[1].l);
			return output;
		}

		/// <summary>
		///	This method is directly linked to ClearVRCoreWrapper.setParameter(). Return true is successful, false otherwise.
		/// See the ClearVRCore docs for details about valid keys and when they can be queried.
		/// </summary>
		/// <param name="argKey"> The key to set.</param>
		/// <param name="argValue">The value they key should be set to.</param>
		/// <returns>true if success, false otherwise.</returns>
		public override bool SetClearVRCoreParameter(String argKey, String argValue) {
			if(!GetIsPlayerBusy())
				return false;
			jvalue[] parms = new jvalue[2];
			parms[0].l = AndroidJNI.NewStringUTF(argKey);
			parms[1].l = AndroidJNI.NewStringUTF(argValue);
			bool isSuccess = false;
			try {
				isSuccess = AndroidJNI.CallBooleanMethod(clearVRCoreWrapperGlobalRef, JNIBridges.JNIBridgeClearVRCoreWrapper.setParameterMethodId, parms);
			} catch (Exception e) {
				UnityEngine.Debug.LogWarning(String.Format("[ClearVR] An exception was thrown while setting key '{0}' to {1} on ClearVRCore. Error: {2}", argKey, argValue, e.ToString()));
			}
			AndroidJNI.DeleteLocalRef(parms[0].l);
			AndroidJNI.DeleteLocalRef(parms[1].l);
			return isSuccess;
		}

		/// <summary>
		/// Parse media info. This method is handled asynchronously since it involves network traffic and manifest parsing.
		/// You will have to wait for the ClearVREventTypes.MediaInfoParsed Event before it is safe to query
		/// audio/video parameters and select the audio track of you liking.
		/// </summary>
		/// <param name="argUrl"></param>
		public override ClearVRAsyncRequest _PopulateMediaInfo(PopulateMediaInfoParameters argPopulateMediaInfoParameters) {
			if(!GetIsPlayerBusy()) {
				return InvokeClearVRCoreWrapperInvalidStateEvent(String.Format("Cannot parse media info when in {0} state.", GetCurrentStateAsString()), RequestTypes.ParseMediaInfo);
			}
			ClearVREvent clearVREvent = ClearVREvent.GetGenericOKEvent(ClearVREventTypes.ParsingMediaInfo);
			ScheduleClearVREvent(clearVREvent);

			jvalue[] jValues = new jvalue[1];
			jValues[0].l = JNIBridges.ByteArrayToLocalRef(argPopulateMediaInfoParameters.ToCoreProtobuf().ToByteArray());
			IntPtr clearVRAsyncRequestRawObject = AndroidJNI.CallObjectMethod(clearVRCoreWrapperGlobalRef, JNIBridges.JNIBridgeClearVRCoreWrapper.populateMediaInfoMethodId, jValues);
			AndroidJNI.DeleteLocalRef(jValues[0].l);
			return JNIBridges.JNIBridgeClearVRAsyncRequest.ConvertClearVRAsyncRequestAsRawObjectToClearVRAsyncRequest(clearVRAsyncRequestRawObject);
		}

		/*
		Get the current average bitrate in megabit per second. Notice that the lower level method returns this value in kilobit per second.
		*/
		public override float GetAverageBitrateInMbps() {
			if(!GetIsPlayerBusy())
				return 0f; // Cannot query this parameter in these states
			try {
				return (float) Math.Round((float)AndroidJNI.CallIntMethod(clearVRCoreWrapperGlobalRef, JNIBridges.JNIBridgeClearVRCoreWrapper.getAverageBitrateInKbpsMethodId, JNIBridges.emptyJvalue) / 1024.0f, 1);
			} catch (Exception e) {
				UnityEngine.Debug.LogWarning(String.Format("[ClearVR] An error was thrown while getting the average stream bitrate. Details: {0}.", e));
			}
			return 0.0f;
		}

		public override TimingReport _GetTimingReport(TimingTypes argTimingType) {
			jvalue[] jvalues = new jvalue[1];
			jvalues[0].l = JNIBridges.JNIBridgeTimingTypes.GetTimingTypeRawObjectLocalRef(argTimingType);
			// This API is never supposed to return null, see ClearVRCoreWrapper.java::getTimingReport() API
			IntPtr timingReportRawObject = AndroidJNI.CallObjectMethod(clearVRCoreWrapperGlobalRef, JNIBridges.JNIBridgeClearVRCoreWrapper.getTimingReportMethodId, jvalues); // Can return null.
			byte[] timingReportByteArray = JNIBridges.GetByteArrayFromRawObject(timingReportRawObject); // Returns null if argument is null.
			AndroidJNI.DeleteLocalRef(jvalues[0].l); // TimingType enum
			if(timingReportByteArray == null) {
				return TIMING_REPORT_NOT_AVAILABLE;
			}
			cvrinterface.TimingReport cvriTimingReport = cvrinterface.TimingReport.Parser.ParseFrom(timingReportByteArray);
			return new TimingReport(cvriTimingReport);
		}

		public override ClearVRAsyncRequest _Seek(SeekParameters argSeekParameters) {
			if(!GetIsPlayerBusy()) {
				return InvokeClearVRCoreWrapperInvalidStateEvent(String.Format("Cannot seek when in {0} state.", GetCurrentStateAsString()), RequestTypes.Seek);
			}
			jvalue[] jValues = new jvalue[1];
			jValues[0].l = JNIBridges.ByteArrayToLocalRef(argSeekParameters.ToCoreProtobuf().ToByteArray());
			IntPtr clearVRAsyncRequestRawObject = AndroidJNI.CallObjectMethod(clearVRCoreWrapperGlobalRef, JNIBridges.JNIBridgeClearVRCoreWrapper.seekMethodId, jValues);
			AndroidJNI.DeleteLocalRef(jValues[0].l);
			return JNIBridges.JNIBridgeClearVRAsyncRequest.ConvertClearVRAsyncRequestAsRawObjectToClearVRAsyncRequest(clearVRAsyncRequestRawObject);			
		}

        public override bool SetMuteAudio(bool argIsMuted) {
			if(argIsMuted) {
				return AndroidJNI.CallBooleanMethod(clearVRCoreWrapperGlobalRef, JNIBridges.JNIBridgeClearVRCoreWrapper.muteAudioMethodId, JNIBridges.emptyJvalue);
			} else {
				bool returnedValue = AndroidJNI.CallBooleanMethod(clearVRCoreWrapperGlobalRef, JNIBridges.JNIBridgeClearVRCoreWrapper.unmuteAudioMethodId, JNIBridges.emptyJvalue);
				if(returnedValue) {
					// Successfully unmuted, the SetMuteAudio() API returns false in this case
					return false;
				}
				// Unable to unmute, we fall-through to returning false.
			}
			return false;
		}

		public override void SetAudioGain(float argGain) {
			jvalue[] parms = new jvalue[1];
			parms[0].f = argGain;
			AndroidJNI.CallVoidMethod(clearVRCoreWrapperGlobalRef, JNIBridges.JNIBridgeClearVRCoreWrapper.setAudioGainMethodId, parms);
		}

		public override float GetAudioGain() {
			if(!hasReachedPreparingContentForPlayoutState) { // Only by this point, the initial settings have been signalled to wrapper.
				return _platformOptions.initialAudioGain;
			}
			return AndroidJNI.CallFloatMethod(clearVRCoreWrapperGlobalRef, JNIBridges.JNIBridgeClearVRCoreWrapper.getAudioGainMethodId, JNIBridges.emptyJvalue);
		}

		public override bool GetIsAudioMuted() {
			if(!hasReachedPreparingContentForPlayoutState) { // Only by this point, the initial settings have been signalled to wrapper.
				return false;
			}
			return AndroidJNI.CallBooleanMethod(clearVRCoreWrapperGlobalRef, JNIBridges.JNIBridgeClearVRCoreWrapper.getIsAudioMutedMethodId, JNIBridges.emptyJvalue);
		}

		public override ClearVRAsyncRequest _SwitchContent(SwitchContentParameters argSwitchContentParameters) {
			jvalue[] jValues = new jvalue[1];
			jValues[0].l = JNIBridges.ByteArrayToLocalRef(argSwitchContentParameters.ToCoreProtobuf().ToByteArray());
			IntPtr clearVRAsyncRequestRawObject = AndroidJNI.CallObjectMethod(clearVRCoreWrapperGlobalRef, JNIBridges.JNIBridgeClearVRCoreWrapper.switchContentMethodId, jValues);
			AndroidJNI.DeleteLocalRef(jValues[0].l);
			return JNIBridges.JNIBridgeClearVRAsyncRequest.ConvertClearVRAsyncRequestAsRawObjectToClearVRAsyncRequest(clearVRAsyncRequestRawObject);
		}

        public override bool SetLoopContent(bool argIsContentLoopEnabled) {
			return SetClearVRCoreParameter("playback.loop_content", argIsContentLoopEnabled.ToString());
		}

		public override ClearVRAsyncRequest _SetStereoMode(bool argStereo) {
			if(!GetIsPlayerBusy()) {
				return InvokeClearVRCoreWrapperInvalidStateEvent(String.Format("Cannot set stereo mode when in {0} state.", GetCurrentStateAsString()), RequestTypes.ChangeStereoMode);
			}
			jvalue[] parms = new jvalue[1];
			parms[0].z = argStereo;
			IntPtr clearVRAsyncRequestRawObject = AndroidJNI.CallObjectMethod(clearVRCoreWrapperGlobalRef, JNIBridges.JNIBridgeClearVRCoreWrapper.setStereoscopicModeMethodId, parms);
			return JNIBridges.JNIBridgeClearVRAsyncRequest.ConvertClearVRAsyncRequestAsRawObjectToClearVRAsyncRequest(clearVRAsyncRequestRawObject);
		}

		public override ClearVRAsyncRequest _StartPlayout() {
			if(!GetIsPlayerBusy()) {
				return InvokeClearVRCoreWrapperInvalidStateEvent("You can only call Play() once. If you would like to unpause, use the Unpause() API instead.", RequestTypes.Start);
			}
			IntPtr clearVRAsyncRequestRawObject = AndroidJNI.CallObjectMethod(clearVRCoreWrapperGlobalRef, JNIBridges.JNIBridgeClearVRCoreWrapper.startPlayoutMethodId, JNIBridges.emptyJvalue);
			return JNIBridges.JNIBridgeClearVRAsyncRequest.ConvertClearVRAsyncRequestAsRawObjectToClearVRAsyncRequest(clearVRAsyncRequestRawObject);
		}

		public override ClearVRAsyncRequest _Pause() {
			if(!GetIsPlayerBusy()) {
				return InvokeClearVRCoreWrapperInvalidStateEvent(String.Format("Cannot pause when in {0} state.", GetCurrentStateAsString()), RequestTypes.Pause);
			}
			IntPtr clearVRAsyncRequestRawObject = AndroidJNI.CallObjectMethod(clearVRCoreWrapperGlobalRef, JNIBridges.JNIBridgeClearVRCoreWrapper.pauseMethodId, JNIBridges.emptyJvalue);
			return JNIBridges.JNIBridgeClearVRAsyncRequest.ConvertClearVRAsyncRequestAsRawObjectToClearVRAsyncRequest(clearVRAsyncRequestRawObject);
		}

		public override ClearVRAsyncRequest _Unpause(TimingParameters argTimingParameters) {
			if(!GetIsPlayerBusy()) {
				return InvokeClearVRCoreWrapperInvalidStateEvent(String.Format("Cannot unpause when in {0} state.", GetCurrentStateAsString()), RequestTypes.Unpause);
			}
			jvalue[] jValues = new jvalue[1];
			jValues[0].l = (argTimingParameters != null) ? JNIBridges.ByteArrayToLocalRef(argTimingParameters.ToCoreProtobuf().ToByteArray()) : IntPtr.Zero;
			IntPtr clearVRAsyncRequestRawObject = AndroidJNI.CallObjectMethod(clearVRCoreWrapperGlobalRef, JNIBridges.JNIBridgeClearVRCoreWrapper.unpauseMethodId, jValues);
			if(jValues[0].l != null) {
				AndroidJNI.DeleteLocalRef(jValues[0].l);
			}
			return JNIBridges.JNIBridgeClearVRAsyncRequest.ConvertClearVRAsyncRequestAsRawObjectToClearVRAsyncRequest(clearVRAsyncRequestRawObject);
		}

        public override ClearVRAsyncRequest _CallCore(byte[] argRawMessage) {
			String base64Message = System.Convert.ToBase64String(argRawMessage);
			return JNIBridges.JNIBridgeClearVRCoreWrapper.CallCore(clearVRCoreWrapperGlobalRef, base64Message);
        }

		public override String _CallCoreSync(String base64Message) {
			jvalue[] param = new jvalue[1];
			param[0] = new jvalue();
			param[0].l = AndroidJNI.NewStringUTF(base64Message);
			String msg = AndroidJNI.CallStringMethod(clearVRCoreWrapperGlobalRef, JNIBridges.JNIBridgeClearVRCoreWrapper.callCoreSyncMethodId, param);
			AndroidJNI.DeleteLocalRef(param[0].l);
			return msg;
		} 

		/// <summary>
		/// Stop ClearVR, wait for CbClearVRCoreWrapperStopped() to know when this process has completed.
		/// </summary>
		public override ClearVRAsyncRequest _Stop(bool argForceLastMinuteCleanUpAfterPanic) {
			ClearVRAsyncRequest _clearVRAsyncRequest = base._Stop(argForceLastMinuteCleanUpAfterPanic);
			if(_clearVRAsyncRequest == null || _clearVRAsyncRequest.requestType == RequestTypes.Stop) {
				return _clearVRAsyncRequest;
			} // else: succes, _clearVRAsyncRequest can be safely ignored.
			if(clearVRCoreWrapperGlobalRef != IntPtr.Zero && JNIBridges.JNIBridgeClearVRCoreWrapper.stopClearVRCoreMethodId != IntPtr.Zero) { // This is true in case we created a ClearVRCoreWrapper object. If there is such an object, we can call its stop() method.
				// This will stop ClearVR. MUST be called before leaving the scene!
				jvalue[] jValues = JNIBridges.JNIBridgeStopClearVRCoreParameters.GetGlobalRefAsJValues();
				IntPtr clearVRAsyncRequestRawObject = AndroidJNI.CallObjectMethod(clearVRCoreWrapperGlobalRef, JNIBridges.JNIBridgeClearVRCoreWrapper.stopClearVRCoreMethodId, jValues);
				JNIBridges.DeleteGlobalRefFromJValueArray(jValues);
				return JNIBridges.JNIBridgeClearVRAsyncRequest.ConvertClearVRAsyncRequestAsRawObjectToClearVRAsyncRequest(clearVRAsyncRequestRawObject);
			} // else no ClearVRCoreWrapper object is available. 
			// We will: 
			// 1. fake our Stop request and response.
			ClearVRAsyncRequest clearVRAsyncRequest = new ClearVRAsyncRequest(RequestTypes.Stop);
			// 2. fake our response to this request. 
			// Note that we MUST call CbClearVRCoreWrapperRequestCompleted() rather than ScheduleClearVREvent() as we rely on the state-specific logic in the former method
			CbClearVRCoreWrapperRequestCompleted(new ClearVRAsyncRequestResponse(clearVRAsyncRequest.requestType, 
					clearVRAsyncRequest.requestId),
					ClearVRMessage.GetGenericOKMessage());
			// Remember that in MediaPlayerBase.CbClearVRCoreWrapperRequestCompleted() we trigger the state change to Stopped, so there is NO need to that here.
			return clearVRAsyncRequest;
		}

		/// <summary>
		/// Called to destroy the object
		/// </summary>
		public override void Destroy() {
			base.Destroy();
			if(!isPlayerShuttingDown) {
				StopInternal();
			}
		}
		
		public override void CleanUpAfterStopped() {
			if(!clearVRCoreWrapperGlobalRef.Equals(IntPtr.Zero))
				AndroidJNI.DeleteGlobalRef(clearVRCoreWrapperGlobalRef);
			clearVRCoreWrapperGlobalRef = IntPtr.Zero;
			if(!_activityRawObjectGlobalRef.Equals(IntPtr.Zero))
				AndroidJNI.DeleteGlobalRef(_activityRawObjectGlobalRef);
			_activityRawObjectGlobalRef = IntPtr.Zero;
			if(!clearVRCoreWrapperExternalInterfaceJavaProxyGlobalRef.Equals(IntPtr.Zero))
				AndroidJNI.DeleteGlobalRef(clearVRCoreWrapperExternalInterfaceJavaProxyGlobalRef);
			clearVRCoreWrapperExternalInterfaceJavaProxyGlobalRef = IntPtr.Zero;
			JNIBridges.Release();
		}
	}
}
#endif
