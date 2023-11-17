// #define USE_MF_BYPASS
// For now, we only support Windows.

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
/*
Tiledmedia Sample Player for Unity3D
(c) Tiledmedia, 2017 - 2018

Author: Arjen Veenhuizen
Contact: arjen@tiledmedia.com

*/
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System;
using AOT;

using cvri = com.tiledmedia.clearvr.cvrinterface;
using com.tiledmedia.clearvr.protobuf;

#if USE_MF_BYPASS
using fts;
#endif

namespace com.tiledmedia.clearvr {
    internal class MediaPlayerPC : MediaPlayerBase {
        public static class ClearVRCoreWrapperExternalInterface {
            /* The underlying SDK makes sure that this callback is always invoked from the main thread. Otherwise, random SEGFAULTS are to be expected! */

            public delegate void CbClearVRCoreWrapperRequestCompletedDelegate(IntPtr clearVRAsyncRequestResponseCIntPtr);
            public delegate void CbClearVRCoreWrapperMessageDelegate(IntPtr clearVRMessageCIntPtr);
            [Serializable, StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
            public struct ClearVRAsyncRequestC {
                public Int32 requestType;
                public UInt32 requestId;
            }

            [Serializable, StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
            public struct ClearVRAsyncRequestResponseC {
                public Int32 requestType;
                public UInt32 requestId;
                /// <summary>
                ///  On PC, the ClearVRMessage is a C struct, not a pointer to a struct (as on iOS)
                /// </summary>
                public ClearVRMessageC clearVRMessageC;
            }

            [Serializable, StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
            public struct ClearVRMessageC { // size = 3* 8
                public Int32 messageType;
                public Int32 messageCode;
                public IntPtr messageAsIntPtr;
                public byte isSuccess;
                private byte padding1;
                private Int16 padding2;
                private Int32 padding3;
            }

             public static ClearVRMessage ConvertClearVRMessageCToClearVRMessage(ClearVRMessageC clearVRMessageC) {
                ClearVRMessage clearVRMessage = new ClearVRMessage(clearVRMessageC.messageType,
                                                   clearVRMessageC.messageCode,
                                                   MediaPlayerPC.MarshalString(clearVRMessageC.messageAsIntPtr),
                                                   ClearVRMessage.ConvertBooleanToClearVRResult(clearVRMessageC.isSuccess == 1));
                return clearVRMessage;
            }

            private static ClearVRMessage ConvertClearVRMessageCPtrToClearVRMessageAndFree(IntPtr clearVRMessageCIntPtr) {
                ClearVRCoreWrapperExternalInterface.ClearVRMessageC clearVRMessageC = (ClearVRMessageC)Marshal.PtrToStructure(clearVRMessageCIntPtr, typeof(ClearVRMessageC));
                ClearVRMessage clearVRMessage = ConvertClearVRMessageCToClearVRMessage(clearVRMessageC);
                MediaFlowPC._tm_freeClearVRMessageC(clearVRMessageCIntPtr); // Free the clearVRMessageCIntPtr after usage
                return clearVRMessage;
            }

            public static RequestCompletedStruct ClearVRAsyncRequestResponseCPtrToRequestCompletedStructAndFree(IntPtr clearVRAsyncRequestResponseCIntPtr) {
                ClearVRAsyncRequestResponseC clearVRAsyncRequestResponseC= (ClearVRAsyncRequestResponseC)Marshal.PtrToStructure(clearVRAsyncRequestResponseCIntPtr,typeof(ClearVRAsyncRequestResponseC));
                ClearVRMessage clearVRMessage = ConvertClearVRMessageCToClearVRMessage(clearVRAsyncRequestResponseC.clearVRMessageC);
                ClearVRAsyncRequestResponse clearVRAsyncRequestResponse = new ClearVRAsyncRequestResponse((RequestTypes)clearVRAsyncRequestResponseC.requestType, (int)clearVRAsyncRequestResponseC.requestId);
                MediaFlowPC._tm_freeClearVRAsyncRequestResponseC(clearVRAsyncRequestResponseCIntPtr); // Free clearVRAsyncRequestResponseCIntPtr after usage
                return new RequestCompletedStruct(clearVRAsyncRequestResponse, clearVRMessage);
            }

            [MonoPInvokeCallback(typeof(CbClearVRCoreWrapperRequestCompletedDelegate))]
            /* The underlying SDK makes sure that this callback is always invoked from the main thread. Otherwise, random SEGFAULTS are to be expected! */
            public static void CbClearVRCoreWrapperRequestCompleted(IntPtr argClearVRAsyncRequestResponseStruct) {
                RequestCompletedStruct requestCompletedStruct = ClearVRAsyncRequestResponseCPtrToRequestCompletedStructAndFree(argClearVRAsyncRequestResponseStruct);
                // On PC, we must use an intermediary queue to cache our request responses and handle them on the main Unity thread.
                // Handling the callback synchronously would block the callee (MediaFlow-PC)
                // This in contrast to iOS and Android, where this callback is already tirggered asynchronously on the main (Unity) thread.
                requestCompletedStructs.Enqueue(requestCompletedStruct);
            }

            [MonoPInvokeCallback(typeof(CbClearVRCoreWrapperMessageDelegate))]
            /* The underlying SDK makes sure that this callback is always invoked from the main thread. Otherwise, random SEGFAULTS are to be expected! */
            public static void CbClearVRCoreWrapperMessage(IntPtr clearVRMessageCIntPtr) {
                // On PC, we must use an intermediary queue to cache our request responses and handle them on the main Unity thread.
                // Handling the callback synchronously would block the callee (MediaFlow-PC)
                // This in contrast to iOS and Android, where this callback is already tirggered asynchronously on the main (Unity) thread.
                ClearVRMessage clearVRMessage = ConvertClearVRMessageCPtrToClearVRMessageAndFree(clearVRMessageCIntPtr);
                requestCompletedStructs.Enqueue(new RequestCompletedStruct(null, clearVRMessage));
            }
        }

        static internal ConcurrentQueue<RequestCompletedStruct> requestCompletedStructs = new ConcurrentQueue<RequestCompletedStruct>();

        private _ClearVRViewportAndObjectPose sendSensorDataParams = new _ClearVRViewportAndObjectPose();
        /// <summary>
        /// Class constructor
        /// </summary>
        /// <param name="argPlatformOptions">The options to use when constructing the class</param>
        /// <param name="argCbClearVREvent">The app-level ClearVREvent callback to trigger.</param>
        public MediaPlayerPC(PlatformOptionsBase argPlatformOptions, ClearVRLayoutManager argClearVRLayoutManager, UnityEngine.Events.UnityAction<MediaPlayerBase,ClearVREvent> argCbClearVREvent) : base(argPlatformOptions, argClearVRLayoutManager, argCbClearVREvent) {
            int size = requestCompletedStructs.Count;
            for (int i = 0; i < size; i++) {
                RequestCompletedStruct requestCompletedStruct;
                bool isSuccess =  requestCompletedStructs.TryDequeue(out requestCompletedStruct);
            }
            // requestCompletedStructs.Clear(); // Not available in .NET Standard 2.0 
        }

        public override bool _InitializePlatformBindings() {
            if(!base._InitializePlatformBindings()) {
                return false;
            }
            //Set default sendSensorDataParams
            sendSensorDataParams = new _ClearVRViewportAndObjectPose(1f);
            GetClearVRCoreVersion();
            return true;
        }

        // Allocate on the stack to make sure that it doesn't get GC-ed.
        IntPtr cbRendererFrameAsByteArrayAvailable;

        /// <summary>
        /// Called to prepare the underlying libraries for playout.
        /// This will yield a single frame that is used to construct the ClearVRDisplayObjectController.
        /// </summary>
        public override ClearVRAsyncRequest _PrepareCore() {
            if (!_state.Equals(States.Initialized))
                return InvokeClearVRCoreWrapperInvalidStateEvent(String.Format("Cannot prepare core when in {0} state.", GetCurrentStateAsString()), RequestTypes.Initialize);
            SetState(States.PreparingCore, ClearVRMessage.GetGenericOKMessage());
            // Bootstrap the C --> C++ bridge
            MediaFlowPC._tm_bootstrap(); // synchronous
            // This can only be called after _bootstrap() has completed.
            // Register delegates.
            // Since #5353, there is no need to register the request completion callback.
            MediaFlowPC._tm_setCbClearVRCoreWrapperMessageDelegate(ClearVRCoreWrapperExternalInterface.CbClearVRCoreWrapperMessage);
            cvri.InitializeParametersMediaFlow initializeParameters = _platformOptions.ToCoreProtobuf();
            byte[] rawProto = initializeParameters.ToByteArray();
            ClearVRAsyncRequest clearVRAsyncRequest = ConvertClearVRAsyncRequestCIntPtrToClearVRAsyncRequestAndFree(MediaFlowPC._tm_initializeProto(rawProto, rawProto.Length,ClearVRCoreWrapperExternalInterface.CbClearVRCoreWrapperRequestCompleted));
            cbRendererFrameAsByteArrayAvailable = NativeRendererPluginPC.CVR_NRP_GetCbRendererFrameAsByteArrayAvailable();
            MediaFlowPC._tm_setCbClearVRCoreWrapperRendererFrameAvailableDelegate(cbRendererFrameAsByteArrayAvailable);
            // TODO: Port to PC
            ///* Initialize statistics */
            //_clearVRCoreWrapperStatistics = new StatisticsAndroid(clearVRCoreWrapperRawClassGlobalRef, clearVRCoreWrapperGlobalRef);
            return clearVRAsyncRequest;
        }

        public static string GetClearVRCoreVersion() {
            if (_clearVRCoreVersionString.Equals(MediaPlayerBase.DEFAULT_CLEAR_VR_CORE_VERSION_STRING)) {
                _clearVRCoreVersionString = MarshalStringAndFree(MediaFlowPC._tm_getClearVRCoreVersion());
            }
            return _clearVRCoreVersionString;
        }

		internal static bool GetIsHardwareHEVCDecoderAvailable() {
            return true; // TODO, ref #6348
        }

        public static string GetProxyParameters(string base64Message) {
            return MarshalStringAndFree(MediaFlowPC._tm_getProxyParameters(base64Message));
        }

        [MonoPInvokeCallback(typeof(ClearVRCoreWrapperExternalInterface.CbClearVRCoreWrapperRequestCompletedDelegate))]
        /* The underlying SDK makes sure that this callback is always invoked from the main thread. Otherwise, random SEGFAULTS are to be expected! */
        public static void CbClearVRCoreWrapperRequestCompletedForCTS(IntPtr argClearVRAsyncRequestResponseStruct) {
            RequestCompletedStruct requestCompletedStruct = ClearVRCoreWrapperExternalInterface.ClearVRAsyncRequestResponseCPtrToRequestCompletedStructAndFree(argClearVRAsyncRequestResponseStruct);
            
            for (int i = 0; i < _staticClearVRAsyncRequests.Count; i++) {
                if (_staticClearVRAsyncRequests[i].requestId == requestCompletedStruct.clearVRAsyncRequestResponse.requestId) {
                    if (_staticClearVRAsyncRequests[i].requestType == RequestTypes.ContentSupportedTest || _staticClearVRAsyncRequests[i].requestType == RequestTypes.CallCoreStatic) {
                        // Callback is hidden in the optional arguments of the request
                        Action<ClearVRMessage> callback = (Action<ClearVRMessage>)_staticClearVRAsyncRequests[i].optionalArguments[0];
                        callback(requestCompletedStruct.clearVRMessage);
                        _staticClearVRAsyncRequests.RemoveAt(i);
                        break;

                    }
                }
            }
        }

        public static void _CallCoreStatic(String base64Message, Action<ClearVRMessage> cbClearVRMessage) {
            ClearVRAsyncRequest clearVRAsyncRequest = ConvertClearVRAsyncRequestCIntPtrToClearVRAsyncRequestAndFree(MediaFlowPC._tm_callCoreStatic(base64Message, CbClearVRCoreWrapperRequestCompletedForCTS));
            clearVRAsyncRequest.UpdateActionAndOptionalArguments(null, null, new System.Object[] { cbClearVRMessage } );
            _staticClearVRAsyncRequests.Add(clearVRAsyncRequest);
        }

        public static string _CallCoreStaticSync(string base64Message) {
            return MarshalStringAndFree(MediaFlowPC._tm_callCoreStaticSync(base64Message));
        }

        public override void Update() {
            while(requestCompletedStructs.Count > 0) {
                RequestCompletedStruct requestCompletedStruct;
                bool isSuccess =  requestCompletedStructs.TryDequeue(out requestCompletedStruct);
                if (isSuccess == true) {
                    if (requestCompletedStruct.clearVRAsyncRequestResponse != null) {
                        CbClearVRCoreWrapperRequestCompleted(requestCompletedStruct.clearVRAsyncRequestResponse, requestCompletedStruct.clearVRMessage);
                    } else {
                        CbClearVRCoreWrapperMessage(requestCompletedStruct.clearVRMessage);
                }
                }
                
            }
            base.UpdateClearVREventsToInvokeQueue();
        }

        public override void SendSensorInfo() {
            UpdatePoseForSendSensorInfo(ref sendSensorDataParams);
            MediaFlowPC._tm_sendSensorData(sendSensorDataParams);
        }

        /// <summary>
        /// Actually load and start playout of the content item
        /// </summary>
        /// <param name="argContentItem">The content item to prepare</param>
        public override ClearVRAsyncRequest _PrepareContentForPlayout(PrepareContentParameters argPrepareContentParameters) {
            byte[] rawProto = argPrepareContentParameters.ToCoreProtobuf(_platformOptions).ToByteArray();

            return ConvertClearVRAsyncRequestCIntPtrToClearVRAsyncRequestAndFree(MediaFlowPC._tm_prepareContentForPlayoutProto(rawProto, rawProto.Length,ClearVRCoreWrapperExternalInterface.CbClearVRCoreWrapperRequestCompleted));
        }

        public override String GetDeviceAppId() {
            return MarshalStringAndFree(MediaFlowPC._tm_getDeviceAppId());
        }

        /// <summary>
        /// This method is directly linked to ClearVRCoreWrapper.getParameter().
        /// See the ClearVRCore docs for details about valid keys and when they can be queried. Note that this will return an empty string in case you try to query a parameter
        /// when in an illegal state.
        /// </summary>
        /// <param name="key"></param> The key to query.
        /// <returns>The value of the queried key. </returns>
        public override String GetClearVRCoreParameter(String key) {
            if (!GetIsPlayerBusy())
                return "";
            return MarshalStringAndFree(MediaFlowPC._tm_getParameterSafely(key));
        }

        /// <summary>
        /// This method is directly linked to ClearVRCoreWrapper.getParameter().
        /// See the ClearVRCore docs for details about valid keys and when they can be queried. Note that this will return an empty string in case you try to query a parameter
        /// when in an illegal state.
        /// </summary>
        /// <param name="key"></param> The key to query.
        /// <returns>The value of the queried key. </returns>
        public override String GetClearVRCoreArrayParameter(String key, int argIndex) {
            if (!GetIsPlayerBusy())
                return "";
            return MarshalStringAndFree(MediaFlowPC._tm_getArrayParameterSafely(key, argIndex));
        }


        /// <summary>
        /// This method is directly linked to ClearVRCoreWrapper.setParameter(). Return true is successful, false otherwise.
        /// See the ClearVRCore docs for details about valid keys and when they can be queried.
        /// </summary>
        /// <param name="key"> The key to set.</param>
        /// <param name="value">The value they key should be set to.</param>
        /// <returns>true if success, false otherwise.</returns>
        public override bool SetClearVRCoreParameter(String key, String value) {
            if (!GetIsPlayerBusy())
                return false;
            bool isSuccess = (MediaFlowPC._tm_setParameterSafely(key, value) == 1);

            if (!isSuccess) {
               ClearVRLogger.LOGW("An exception was thrown while setting key '{0}' to {1} on ClearVRCore.", key, value);
            }
            return isSuccess;
        }

        /// <summary>
        /// Parse media info. This method is handled asynchronously since it involves network traffic and manifest parsing.
        /// You will have to wait for the ClearVREventTypes.MediaInfoParsed Event before it is safe to query
        /// audio/video parameters and select the audio track of you liking.
        /// </summary>
        /// <param name="argUrl"></param>
        public override ClearVRAsyncRequest _PopulateMediaInfo(PopulateMediaInfoParameters argPopulateMediaInfoParameters) {
            if (!GetIsPlayerBusy()) {
                return InvokeClearVRCoreWrapperInvalidStateEvent(String.Format("Cannot parse media info when in {0} state.", GetCurrentStateAsString()), RequestTypes.ParseMediaInfo);
            }

            byte[] rawProto = argPopulateMediaInfoParameters.ToCoreProtobuf().ToByteArray();
            return ConvertClearVRAsyncRequestCIntPtrToClearVRAsyncRequestAndFree(MediaFlowPC._tm_populateMediaInfo(rawProto, rawProto.Length,ClearVRCoreWrapperExternalInterface.CbClearVRCoreWrapperRequestCompleted));
        }

        /*
        Get the current average bitrate in megabit per second. Notice that the lower level method returns this value in kilobit per second.
        */
        public override float GetAverageBitrateInMbps() {
            if (!GetIsPlayerBusy())
                return 0f; // Cannot query this parameter in these states
            return (float)Math.Round((float)MediaFlowPC._tm_getAverageBitrateInKbps() / 1024f, 1);
        }
        private byte[] ConvertTimingReportPtrToTimingReportAsByteArrayAndFree(IntPtr timingReportAsIntPtr,int byteArrayLength) {
                byte[] timingReportAsByteArray = new byte[byteArrayLength];
                Marshal.Copy(timingReportAsIntPtr, timingReportAsByteArray,0 , byteArrayLength);
                MediaFlowPC._tm_freeChar(timingReportAsIntPtr);
                return timingReportAsByteArray;
            }

        public override TimingReport _GetTimingReport(TimingTypes argTimingType) {
            TimingReport timingReport;
            int byteArrayLength = 0;
            IntPtr timingReportAsIntPtr = MediaFlowPC._tm_getTimingReport((Int32)TimingTypesMethods.ToCoreProtobuf(argTimingType), ref byteArrayLength); // Returns null if no TimingReport is available.
            if(timingReportAsIntPtr == IntPtr.Zero) {
                return TIMING_REPORT_NOT_AVAILABLE;
            }
            byte[] timingReportAsByteArray = ConvertTimingReportPtrToTimingReportAsByteArrayAndFree(timingReportAsIntPtr,byteArrayLength);
            bool result = TimingReport.FromCoreProtobuf(timingReportAsByteArray, out timingReport);
            if(!GetIsPlayerBusy() || !result) {
                return TIMING_REPORT_NOT_AVAILABLE;
            }
            return timingReport;
        }

        public override ClearVRAsyncRequest _Seek(SeekParameters argSeekParameters) {
            if (!GetIsPlayerBusy()) {
                return InvokeClearVRCoreWrapperInvalidStateEvent(String.Format("Cannot seek when in {0} state.", GetCurrentStateAsString()), RequestTypes.Seek);
            }
            byte[] rawProto = argSeekParameters.ToCoreProtobuf().ToByteArray();
            int rawProtoLength = rawProto != null ? rawProto.Length : 0;
            return ConvertClearVRAsyncRequestCIntPtrToClearVRAsyncRequestAndFree(MediaFlowPC._tm_seekProto(rawProto, rawProtoLength,ClearVRCoreWrapperExternalInterface.CbClearVRCoreWrapperRequestCompleted));
        }

        public override bool SetMuteAudio(bool argIsMuted) {
            if (argIsMuted) {
                return (MediaFlowPC._tm_muteAudio() == 1); // returns a bool in byte disguise
            } else {
                bool returnedValue = (MediaFlowPC._tm_unmuteAudio() == 1);
                if (returnedValue) {
                    // Successfully unmuted, the SetMuteAudio() API returns false in this case
                    return false;
                }
                // Unable to unmute, we fall-through to returning false.
            }
            return false;
        }

        public override void SetAudioGain(float gain) {
            MediaFlowPC._tm_setAudioGain(gain);
        }

        public override float GetAudioGain() {
            if(!hasReachedPreparingContentForPlayoutState) { // Only by this point, the initial settings have been signalled to wrapper.
                return _platformOptions.initialAudioGain;
            }
            return MediaFlowPC._tm_getAudioGain();
        }

        public override bool GetIsAudioMuted() {
            if(!hasReachedPreparingContentForPlayoutState) { // Only by this point, the initial settings have been signalled to wrapper.
                return false;
            }
            return (MediaFlowPC._tm_getIsAudioMuted() == 1);
        }

        public override float GetMuteState() {
            if(!hasReachedPreparingContentForPlayoutState) { // Only by this point, the initial settings have been signalled to wrapper.
                return _platformOptions.muteState;
            }
            return MediaFlowPC._tm_getMuteState();
        }

        public override ClearVRAsyncRequest _SwitchContent(SwitchContentParameters argSwitchContentParameters) {
            byte[] rawProto = argSwitchContentParameters.ToCoreProtobuf().ToByteArray();
            return ConvertClearVRAsyncRequestCIntPtrToClearVRAsyncRequestAndFree(MediaFlowPC._tm_switchContentProto(rawProto, rawProto.Length,ClearVRCoreWrapperExternalInterface.CbClearVRCoreWrapperRequestCompleted));
        }

        public override bool SetLoopContent(bool argIsContentLoopEnabled) {
            return SetClearVRCoreParameter("playback.loop_content", argIsContentLoopEnabled.ToString());
        }

        public override ClearVRAsyncRequest _SetStereoMode(bool argStereo) {
            if (!GetIsPlayerBusy()) {
                return InvokeClearVRCoreWrapperInvalidStateEvent(String.Format("Cannot set stereo mode when in {0} state.", GetCurrentStateAsString()), RequestTypes.ChangeStereoMode);
            }
            return ConvertClearVRAsyncRequestCIntPtrToClearVRAsyncRequestAndFree(MediaFlowPC._tm_setStereoscopicMode(argStereo ? (byte)1 : (byte)0,ClearVRCoreWrapperExternalInterface.CbClearVRCoreWrapperRequestCompleted));
        }

        public override ClearVRAsyncRequest _StartPlayout() {
            if (!GetIsPlayerBusy()) {
                return InvokeClearVRCoreWrapperInvalidStateEvent("You can only call Play() once. If you would like to unpause, use the Unpause() API instead.", RequestTypes.Start);
            }
            return ConvertClearVRAsyncRequestCIntPtrToClearVRAsyncRequestAndFree(MediaFlowPC._tm_startPlayout(ClearVRCoreWrapperExternalInterface.CbClearVRCoreWrapperRequestCompleted));
        }

        public override ClearVRAsyncRequest _Pause() {
            if (!GetIsPlayerBusy()) {
                return InvokeClearVRCoreWrapperInvalidStateEvent(String.Format("Cannot pause when in {0} state.", GetCurrentStateAsString()), RequestTypes.Pause);
            }
            return ConvertClearVRAsyncRequestCIntPtrToClearVRAsyncRequestAndFree(MediaFlowPC._tm_pause(ClearVRCoreWrapperExternalInterface.CbClearVRCoreWrapperRequestCompleted));
        }

        public override ClearVRAsyncRequest _Unpause(TimingParameters argTimingParameters) {
            if (!GetIsPlayerBusy()) {
                return InvokeClearVRCoreWrapperInvalidStateEvent(String.Format("Cannot unpause when in {0} state.", GetCurrentStateAsString()), RequestTypes.Unpause);
            }
            byte[] rawProto = null;
            int rawProtoLength = 0;
            if(argTimingParameters != null) {
                rawProto = argTimingParameters.ToCoreProtobuf().ToByteArray();
                rawProtoLength = rawProto.Length;
            }
            return ConvertClearVRAsyncRequestCIntPtrToClearVRAsyncRequestAndFree(MediaFlowPC._tm_unpauseProto(rawProto, rawProtoLength, ClearVRCoreWrapperExternalInterface.CbClearVRCoreWrapperRequestCompleted));
        }

        public override ClearVRAsyncRequest _CallCore(byte[] argRawMessage) {
			String base64Message = System.Convert.ToBase64String(argRawMessage);
            return ConvertClearVRAsyncRequestCIntPtrToClearVRAsyncRequestAndFree(MediaFlowPC._tm_callCore(base64Message,ClearVRCoreWrapperExternalInterface.CbClearVRCoreWrapperRequestCompleted));
        }

        public override String _CallCoreSync(String base64Message) {
            return MarshalStringAndFree(MediaFlowPC._tm_callCoreStaticSync(base64Message));
        }

        protected static ClearVRAsyncRequest ConvertClearVRAsyncRequestCIntPtrToClearVRAsyncRequestAndFree(IntPtr clearVRAsyncRequestCIntPtr) {
            ClearVRCoreWrapperExternalInterface.ClearVRAsyncRequestC clearVRAsyncRequestC = (ClearVRCoreWrapperExternalInterface.ClearVRAsyncRequestC)Marshal.PtrToStructure(clearVRAsyncRequestCIntPtr, typeof(ClearVRCoreWrapperExternalInterface.ClearVRAsyncRequestC));
            ClearVRAsyncRequest clearVRAsyncRequest = new ClearVRAsyncRequest((RequestTypes)clearVRAsyncRequestC.requestType, Convert.ToInt32(clearVRAsyncRequestC.requestId));
             MediaFlowPC._tm_freeClearVRAsyncRequestC(clearVRAsyncRequestCIntPtr); // Free the clearVRAsyncRequestCIntPtr after usage
            return clearVRAsyncRequest;
        }

        /// <summary>
        /// Stop ClearVR, wait for CbClearVRCoreWrapperStopped() to know when this process has completed.
        /// </summary>
        public override ClearVRAsyncRequest _Stop(bool argForceLastMinuteCleanUpAfterPanic) {
			ClearVRAsyncRequest _clearVRAsyncRequest = base._Stop(argForceLastMinuteCleanUpAfterPanic);
			if(_clearVRAsyncRequest == null || _clearVRAsyncRequest.requestType == RequestTypes.Stop) {
				return _clearVRAsyncRequest;
			} // else: succes, _clearVRAsyncRequest can be safely ignored.
            // This will stop ClearVR. MUST be called before leaving the scene!
            IntPtr clearVRAsyncRequestMaybe = MediaFlowPC._tm_stop(ClearVRCoreWrapperExternalInterface.CbClearVRCoreWrapperRequestCompleted); // Might return nil is _tm_bootstrap() was never called. This is unlikely but not impossible.
            if (clearVRAsyncRequestMaybe != IntPtr.Zero) {
                return ConvertClearVRAsyncRequestCIntPtrToClearVRAsyncRequestAndFree(clearVRAsyncRequestMaybe);
            } // else: _stop() returned a null pointer, so we fake a state change to Stopped as we will never get one from the MediaFlow.
			// We will: 
			// 1. fake our Stop request and response.
			ClearVRAsyncRequest clearVRAsyncRequest = new ClearVRAsyncRequest(RequestTypes.Stop);
			// 2. fake our response to this request.
			// Note that we MUST call CbClearVRCoreWrapperRequestCompleted() rather than ScheduleClearVREvent() as we rely on the state-specific logic in the former method
			CbClearVRCoreWrapperRequestCompleted(new ClearVRAsyncRequestResponse(clearVRAsyncRequest.requestType, 
					clearVRAsyncRequest.requestId),
					ClearVRMessage.GetGenericOKMessage());

            // Remember that in MediaPlayerBase.CbClearVRCoreWrapperRequestCompleted() we trigger the state change to Stopped, so there is NO need for that here.
            return clearVRAsyncRequest;
        }

        /// <summary>
        /// Only call this helper function if the caller owns the ownership (i.e., is responsible for freeing the char*, protobuf message, or base64 string after usage) of the char pointer 
        /// </summary>
        protected static String MarshalStringAndFree(IntPtr argStringAsIntPtr) {
            String marshaledString = MarshalString(argStringAsIntPtr);
            MediaFlowPC._tm_freeChar(argStringAsIntPtr);
            return marshaledString;
        }

        /// <summary>
        /// Copy the string from the char pointer. Return empty string if the conversion is invalid
        /// </summary>
        protected static String MarshalString(IntPtr argStringAsIntPtr) {
            String marshaledString = Marshal.PtrToStringAnsi(argStringAsIntPtr);
            if(marshaledString == null) {
                marshaledString = "";
            }
            return marshaledString;
        }

        /// <summary>
        /// Called to destroy the object
        /// </summary>
        public override void Destroy() {
            base.Destroy();
            if (!isPlayerShuttingDown) {
                StopInternal();
            }
        }

        public override void CleanUpAfterStopped() {
            MediaFlowPC._tm_destroy();
        }

        public static void UnloadCBridge() {
            MediaFlowPC._tm_unloadCBridge();
        }

#if USE_MF_BYPASS
        [PluginAttr("libClearVRPC")]
#endif
        internal class MediaFlowPC {
#if !USE_MF_BYPASS

            const string MEDIA_FLOW_LIBRARY_NAME = "ClearVRPC";

            [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
            internal static extern void _tm_bootstrap();
            [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
            internal static extern void _tm_destroy();
            [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            internal static extern void _tm_unloadCBridge();
            [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            internal static extern IntPtr _tm_initializeProto(byte[] rawProto, int rawProtoLength, ClearVRCoreWrapperExternalInterface.CbClearVRCoreWrapperRequestCompletedDelegate callback);
            [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr _tm_populateMediaInfo(byte[] rawProto, int rawProtoLength,ClearVRCoreWrapperExternalInterface.CbClearVRCoreWrapperRequestCompletedDelegate callback);

            [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr _tm_stop(ClearVRCoreWrapperExternalInterface.CbClearVRCoreWrapperRequestCompletedDelegate callback);

            [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr _tm_prepareContentForPlayoutProto(byte[] rawProto, int rawProtoLength, ClearVRCoreWrapperExternalInterface.CbClearVRCoreWrapperRequestCompletedDelegate callback);

            [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr _tm_startPlayout(ClearVRCoreWrapperExternalInterface.CbClearVRCoreWrapperRequestCompletedDelegate callback);

            [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr _tm_switchContentProto(byte[] rawProto, int rawProtoLength, ClearVRCoreWrapperExternalInterface.CbClearVRCoreWrapperRequestCompletedDelegate callback);

            [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            internal static extern IntPtr _tm_getClearVRCoreVersion();

            [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            internal static extern void _tm_log(String msg, int logLevel, int logComponent);

            [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr _tm_getProxyParameters(string base64Message);

            [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr _tm_callCoreStatic(string base64Message, ClearVRCoreWrapperExternalInterface.CbClearVRCoreWrapperRequestCompletedDelegate callback);

            [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr _tm_callCoreStaticSync(string base64Message);

            [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
            internal static extern void _tm_setCbClearVRCoreWrapperMessageDelegate(ClearVRCoreWrapperExternalInterface.CbClearVRCoreWrapperMessageDelegate callback);

            [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
            internal static extern void _tm_setCbClearVRCoreWrapperRendererFrameAvailableDelegate(IntPtr callback);

            [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
            internal static extern void _tm_freeChar(IntPtr argCharPointerToFree);

            [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
            internal static extern void _tm_freeClearVRMessageC(IntPtr pointerToFree);

            [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
            internal static extern void _tm_freeClearVRAsyncRequestC(IntPtr pointerToFree);

            [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
            internal static extern void _tm_freeClearVRAsyncRequestResponseC(IntPtr pointerToFree);

            [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr _tm_getTimingReport(Int32 flag, ref int length);


            [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
            internal static extern Int32 _tm_getAverageBitrateInKbps();

            [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr _tm_getParameterSafely(String key);

            [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
            internal static extern byte _tm_setParameterSafely(String key, String value);

            [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr _tm_getArrayParameterSafely(String key, int argIndex);

            [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr _tm_getDeviceAppId();


            [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr _tm_pause(ClearVRCoreWrapperExternalInterface.CbClearVRCoreWrapperRequestCompletedDelegate callback);

            [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr _tm_unpauseProto(byte[] rawProto, int rawProtoLength, ClearVRCoreWrapperExternalInterface.CbClearVRCoreWrapperRequestCompletedDelegate callback);

            [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr _tm_seekProto(byte[] rawProto, int rawProtoLength,ClearVRCoreWrapperExternalInterface.CbClearVRCoreWrapperRequestCompletedDelegate callback);

            [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr _tm_setStereoscopicMode(byte /* it's a 1-byte bool actually */ monoscopicOrStereoscopic,ClearVRCoreWrapperExternalInterface.CbClearVRCoreWrapperRequestCompletedDelegate callback);

            [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
            internal static extern void _tm_sendSensorData(_ClearVRViewportAndObjectPose viewportAndObjectPose);

            [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr _getUpdateTextureFunction();

            [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
            internal static extern byte _tm_muteAudio();

            [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
            internal static extern byte _tm_unmuteAudio();

            [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
            internal static extern byte _tm_getIsAudioMuted();

            [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
            internal static extern float _tm_getMuteState();

            [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
            internal static extern void _tm_setAudioGain(float argGain);

            [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
            internal static extern float _tm_getAudioGain();

            [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
            internal static extern void _tm_log(IntPtr msg, byte component, byte lvl);

            [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr _tm_callCore(String argBase64Message,ClearVRCoreWrapperExternalInterface.CbClearVRCoreWrapperRequestCompletedDelegate callback);
#else
            [PluginFunctionAttr("_tm_bootstrap")]
            public static _tm_bootstrapDelegate _tm_bootstrap = null;
            public delegate void _tm_bootstrapDelegate();

            [PluginFunctionAttr("_tm_destroy")]
            public static _tm_destroyDelegate _tm_destroy = null;
            public delegate void _tm_destroyDelegate();

             [PluginFunctionAttr("_tm_unloadCBridge")]
            public static _tm_unloadCBridgeDelegate _tm_unloadCBridge = null;
            public delegate void _tm_unloadCBridgeDelegate();

            [PluginFunctionAttr("_tm_initializeProto")]
            public static _tm_initializeProtoDelegate _tm_initializeProto = null;
            public delegate IntPtr _tm_initializeProtoDelegate(byte[] rawProto, int rawProtoLength, ClearVRCoreWrapperExternalInterface.CbClearVRCoreWrapperRequestCompletedDelegate callback);

            [PluginFunctionAttr("_tm_populateMediaInfo")]
            public static _tm_populateMediaInfoDelegate _tm_populateMediaInfo = null;
            public delegate IntPtr _tm_populateMediaInfoDelegate(byte[] rawProto, int rawProtoLength, ClearVRCoreWrapperExternalInterface.CbClearVRCoreWrapperRequestCompletedDelegate callback);

            [PluginFunctionAttr("_tm_stop")]
            public static _tm_stopDelegate _tm_stop = null;
            public delegate IntPtr _tm_stopDelegate(ClearVRCoreWrapperExternalInterface.CbClearVRCoreWrapperRequestCompletedDelegate callback);

            [PluginFunctionAttr("_tm_prepareContentForPlayoutProto")]
            public static _tm_prepareContentForPlayoutProtoDelegate _tm_prepareContentForPlayoutProto = null;
            public delegate IntPtr _tm_prepareContentForPlayoutProtoDelegate(byte[] rawProto, int rawProtoLength, ClearVRCoreWrapperExternalInterface.CbClearVRCoreWrapperRequestCompletedDelegate callback));

            [PluginFunctionAttr("_tm_startPlayout")]
            public static _tm_startPlayoutDelegate _tm_startPlayout = null;
            public delegate IntPtr _tm_startPlayoutDelegate(ClearVRCoreWrapperExternalInterface.CbClearVRCoreWrapperRequestCompletedDelegate callback);

            [PluginFunctionAttr("_tm_switchContentProto")]
            public static _tm_switchContentProtoDelegate _tm_switchContentProto = null;
            public delegate IntPtr _tm_switchContentProtoDelegate(byte[] rawProto, int rawProtoLength,ClearVRCoreWrapperExternalInterface.CbClearVRCoreWrapperRequestCompletedDelegate callback);

            [PluginFunctionAttr("_tm_getClearVRCoreVersion")]
            public static _tm_getClearVRCoreVersionDelegate _tm_getClearVRCoreVersion = null;
            public delegate IntPtr _tm_getClearVRCoreVersionDelegate();

            [PluginFunctionAttr("_tm_clearVRCoreLog")]
            internal static _tm_clearVRCoreLogDelegate _tm_clearVRCoreLog = null;
            internal delegate void _tm_clearVRCoreLogDelegate(String msg, int logLevel, int logComponent);

            [PluginFunctionAttr("_tm_getProxyParametrs")]
            public static _getProxyParametrsDelegate _tm_getProxyParametrs = null;
            public delegate IntPtr _tm_getProxyParametrsDelegate(String base64Message);

             [PluginFunctionAttr("_tm_callCoreStatic")]
            public static _tm_callCoreStaticDelegate _tm_callCoreStatic = null;
            public delegate IntPtr _tm_callCoreStaticDelegate(String base64Message,ClearVRCoreWrapperExternalInterface.CbClearVRCoreWrapperMessageDelegate callback);

            [PluginFunctionAttr("_tm_callCoreStaticSync")]
            public static _callCoreStaticSyncDelegate _tm_callCoreStaticSync = null;
            public delegate IntPtr _tm_callCoreStaticSyncDelegate(String base64Message);

            [PluginFunctionAttr("_tm_setCbClearVRCoreWrapperMessageDelegate")]
            public static _tm_setCbClearVRCoreWrapperMessageDelegateDelegate _tm_setCbClearVRCoreWrapperMessageDelegate = null;
            public delegate void _tm_setCbClearVRCoreWrapperMessageDelegateDelegate(ClearVRCoreWrapperExternalInterface.CbClearVRCoreWrapperMessageDelegate callback);

            [PluginFunctionAttr("_tm_setCbClearVRCoreWrapperRendererFrameAvailableDelegate")]
            public static _tm_setCbClearVRCoreWrapperRendererFrameAvailableDelegateDelegate _tm_setCbClearVRCoreWrapperRendererFrameAvailableDelegate = null;
            public delegate void _tm_setCbClearVRCoreWrapperRendererFrameAvailableDelegateDelegate(IntPtr callback);

            [PluginFunctionAttr("_tm_freeChar")]
            public static _tm_freeCharDelegate _tm_freeChar = null;
            public delegate void _tm_freeCharDelegate(IntPtr charPointerToFree);

            [PluginFunctionAttr("_tm_freeClearVRMessageC")]
            public static _tm_freeClearVRMessageCDelegate _tm_freeClearVRMessageC = null;
            public delegate void _tm_freeClearVRMessageCDelegate(IntPtr pointerToFree);

            [PluginFunctionAttr("_tm_freeClearVRAsyncRequestC")]
            public static _freeClearVRAsyncRequestCDelegate _tm_freeClearVRAsyncRequestC = null;
            public delegate void _tm_freeClearVRAsyncRequestCDelegate(IntPtr pointerToFree);


            [PluginFunctionAttr("_tm_freeClearVRAsyncRequestResponseC")]
            public static _freeClearVRAsyncRequestResponseCDelegate _tm_freeClearVRAsyncRequestResponseC = null;
            public delegate void _tm_freeClearVRAsyncRequestResponseCDelegate(IntPtr pointerToFree);

            [PluginFunctionAttr("_tm_getTimingReport")]
            public static _tm_getTimingReportDelegate _tm_getTimingReport = null;
            public delegate IntPtr _tm_getTimingReportDelegate(Int32 flag, ref int length);

            [PluginFunctionAttr("_tm_getAverageBitrateInKbps")]
            public static _tm_getAverageBitrateInKbpsDelegate _tm_getAverageBitrateInKbps = null;
            public delegate Int32 _tm_getAverageBitrateInKbpsDelegate();

            [PluginFunctionAttr("_tm_getParameterSafely")]
            public static _tm_getParameterSafelyDelegate _tm_getParameterSafely = null;
            public delegate IntPtr _tm_getParameterSafelyDelegate(String key);

            [PluginFunctionAttr("_tm_setParameterSafely")]
            public static _tm_setParameterSafelyDelegate _tm_setParameterSafely = null;
            public delegate byte _tm_setParameterSafelyDelegate(String key, String value);

            [PluginFunctionAttr("_tm_getArrayParameterSafely")]
            public static _tm_getArrayParameterSafelyDelegate _tm_getArrayParameterSafely = null;
            public delegate IntPtr _tm_getArrayParameterSafelyDelegate(String key, int index);

            [PluginFunctionAttr("_tm_getDeviceAppId")]
            public static _tm_getDeviceAppIdDelegate _tm_getDeviceAppId = null;
            public delegate IntPtr _tm_getDeviceAppIdDelegate();

            [PluginFunctionAttr("_tm_pause")]
            public static _tm_pauseDelegate _tm_pause = null;
            public delegate IntPtr _tm_pauseDelegate(ClearVRCoreWrapperExternalInterface.CbClearVRCoreWrapperRequestCompletedDelegate callback);

            [PluginFunctionAttr("_tm_unpauseProto")]
            public static _tm_unpauseProtoDelegate _tm_unpauseProto= null;
            public delegate IntPtr _tm_unpauseProtoDelegate(byte[] rawProto, int rawProtoLength, ClearVRCoreWrapperExternalInterface.CbClearVRCoreWrapperRequestCompletedDelegate callback);

            [PluginFunctionAttr("_tm_seekProto")]
            public static _tm_seekProtoDelegate _tm_seekProto = null;
            public delegate IntPtr _tm_seekProtoDelegate(byte[] rawProto, int rawProtoLength,ClearVRCoreWrapperExternalInterface.CbClearVRCoreWrapperRequestCompletedDelegate callback);

            [PluginFunctionAttr("_tm_setStereoscopicMode")]
            public static _tm_setStereoscopicModeDelegate _tm_setStereoscopicMode = null;
            public delegate IntPtr _tm_setStereoscopicModeDelegate(byte /* it's a 1-byte bool actually */ monoscopicOrStereoscopic,ClearVRCoreWrapperExternalInterface.CbClearVRCoreWrapperRequestCompletedDelegate callback);

            [PluginFunctionAttr("_tm_sendSensorData")]
            public static _tm_sendSensorDataDelegate _tm_sendSensorData = null;
            public delegate void _tm_sendSensorDataDelegate(_ClearVRViewportAndObjectPose viewportAndObjectPose);

            [PluginFunctionAttr("_tm_muteAudio")]
            public static _tm_muteAudioDelegate _tm_muteAudio = null;
            public delegate byte _tm_muteAudioDelegate();

            [PluginFunctionAttr("_tm_unmuteAudio")]
            public static _tm_unmuteAudioDelegate _tm_unmuteAudio = null;
            public delegate byte _tm_unmuteAudioDelegate();

            [PluginFunctionAttr("_tm_getIsAudioMuted")]
            public static _tm_getIsAudioMutedDelegate _tm_getIsAudioMuted = null;
            public delegate byte _tm_getIsAudioMutedDelegate();

            [PluginFunctionAttr("_tm_getMuteState")]
            public static _tm_getMuteStateDelegate _tm_getMuteState = null;
            public delegate float _tm_getMuteStateDelegate();

            [PluginFunctionAttr("_tm_setAudioGain")]
            public static _tm_setAudioGainDelegate _tm_setAudioGain = null;
            public delegate void _tm_setAudioGainDelegate(float gain);

            [PluginFunctionAttr("_tm_getAudioGain")]
            public static _tm_getAudioGainDelegate _tm_getAudioGain = null;
            public delegate float _tm_getAudioGainDelegate();

            [PluginFunctionAttr("_tm_log")]
            public static _tm_logDelegate _tm_log = null;
            public delegate void _tm_logDelegate(IntPtr msg, byte component, byte lvl);

            [PluginFunctionAttr("_tm_callCore")]
            public static _tm_callCoreDelegate _tm_callCore = null;
            public delegate IntPtr _tm_callCoreDelegate(String argBase64Message,ClearVRCoreWrapperExternalInterface.CbClearVRCoreWrapperRequestCompletedDelegate callback);
#endif
        }
    }
}
#endif
