#if (UNITY_IOS || UNITY_TVOS) && !UNITY_EDITOR
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
using System.Threading;
using AOT;
using cvri = com.tiledmedia.clearvr.cvrinterface;
using com.tiledmedia.clearvr.protobuf;
using System.Runtime.CompilerServices;
using UnityEngine.Rendering;
namespace com.tiledmedia.clearvr {
    internal class MediaPlayerIOS : MediaPlayerBase {
        internal static class ClearVRCoreWrapperExternalInterface {
            /* The underlying SDK makes sure that this callback is always invoked from the main thread. Otherwise, random SEGFAULTS are to be expected! */
            private static MediaPlayerIOS mediaPlayer;

            public delegate void CbClearVRCoreWrapperRequestCompletedDelegate(IntPtr clearVRAsyncRequestResponseStruct);
            public delegate void CbClearVRCoreWrapperMessageDelegate(IntPtr clearVRMessageStruct);
            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
            public struct ClearVRAsyncRequestStruct {
                public Int32 requestType;
                public UInt32 requestId;
            }

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
            public struct ClearVRAsyncRequestResponseStruct {
                public Int32 requestType;
                public UInt32 requestId;
                public ClearVRMessageStruct clearVRMessageStruct;
            }

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
            public struct ClearVRMessageStruct {
                public Int32 messageType;
                public Int32 messageCode;
                public IntPtr messageAsIntPtr;
                public byte isSuccess;
                private byte padding1;
                private Int16 padding2;
                private Int32 padding3;
            }

            public static void SetMediaPlayer(MediaPlayerIOS argMediaPlayer) {
                mediaPlayer = argMediaPlayer;
            }

            public static ClearVRMessage ConvertClearVRMessageStructToClearVRMessage(ClearVRMessageStruct argClearVRMessageStruct) {
                ClearVRMessage clearVRMessage = new ClearVRMessage(argClearVRMessageStruct.messageType,
                                                   argClearVRMessageStruct.messageCode,
                                                   MarshalString(argClearVRMessageStruct.messageAsIntPtr),
                                                   ClearVRMessage.ConvertBooleanToClearVRResult(argClearVRMessageStruct.isSuccess == 1));
                return clearVRMessage;
            }

            public static ClearVRMessage ConvertClearVRMessageStructPtrToClearVRMessageAndFree(IntPtr clearVRMessageStructPtr) {
                ClearVRCoreWrapperExternalInterface.ClearVRMessageStruct clearVRMessageStruct = (ClearVRMessageStruct)Marshal.PtrToStructure(clearVRMessageStructPtr, typeof(ClearVRMessageStruct));
                ClearVRMessage clearVRMessage = ConvertClearVRMessageStructToClearVRMessage(clearVRMessageStruct);
                _tm_freeClearVRMessageStruct(clearVRMessageStructPtr);
                return clearVRMessage;
            }

            public static RequestCompletedStruct ClearVRAsyncRequestResponseStructPtrToRequestCompletedStructAndFree(IntPtr clearVRAsyncRequestResponseStructPtr) {
                ClearVRAsyncRequestResponseStruct clearVRAsyncRequestResponseStruct= (ClearVRAsyncRequestResponseStruct)Marshal.PtrToStructure(clearVRAsyncRequestResponseStructPtr,typeof(ClearVRAsyncRequestResponseStruct));
                ClearVRMessage clearVRMessage = ConvertClearVRMessageStructToClearVRMessage(clearVRAsyncRequestResponseStruct.clearVRMessageStruct);
                ClearVRAsyncRequestResponse clearVRAsyncRequestResponse = new ClearVRAsyncRequestResponse((RequestTypes)clearVRAsyncRequestResponseStruct.requestType, (int)clearVRAsyncRequestResponseStruct.requestId);
                _tm_freeClearVRAsyncRequestResponseStruct(clearVRAsyncRequestResponseStructPtr);
                return new RequestCompletedStruct(clearVRAsyncRequestResponse, clearVRMessage);
            }

            [MonoPInvokeCallback(typeof(CbClearVRCoreWrapperRequestCompletedDelegate))]
            /* The underlying SDK makes sure that this callback is always invoked from the main thread. Otherwise, random SEGFAULTS are to be expected! */
            public static void CbClearVRCoreWrapperRequestCompleted(IntPtr clearVRAsyncRequestResponseStructPtr) {
                RequestCompletedStruct requestCompletedStruct = ClearVRAsyncRequestResponseStructPtrToRequestCompletedStructAndFree(clearVRAsyncRequestResponseStructPtr);
                mediaPlayer.CbClearVRCoreWrapperRequestCompleted(requestCompletedStruct.clearVRAsyncRequestResponse, requestCompletedStruct.clearVRMessage);
            }

            [MonoPInvokeCallback(typeof(CbClearVRCoreWrapperMessageDelegate))]
            /* The underlying SDK makes sure that this callback is always invoked from the main thread. Otherwise, random SEGFAULTS are to be expected! */
            public static void CbClearVRCoreWrapperMessage(IntPtr clearVRMessageStructPtr) {
                mediaPlayer.CbClearVRCoreWrapperMessage(ConvertClearVRMessageStructPtrToClearVRMessageAndFree(clearVRMessageStructPtr));
            }
        }
        const int MAX_CONTENT_ITEM_COUNT = 100; // The maximum number of ContentItems than can be passed through the marshaller (used for the ContentTester)

        const string MEDIA_FLOW_LIBRARY_NAME = "__Internal";
        
        [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void _tm_bootstrap();

        [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void _tm_destroy();

        [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern IntPtr _tm_initialize(byte[] rawProto, int rawProtoLength);

        [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr _tm_populateMediaInfo(byte[] rawProto, int rawProtoLength);

        [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr _tm_stop();

        [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr _tm_prepareContentForPlayout(byte[] rawProto, int rawProtoLength);

        [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr _tm_startPlayout();

        [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr _tm_switchContent(byte[] rawProto, int rawProtoLength);

        [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr _tm_getClearVRCoreVersion();

        [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void _tm_clearVRCoreLog(String argMessage, int argLogComponent, int argLogLevel);

        [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr _tm_getProxyParameters(string base64Message);

        [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr _tm_testIsContentSupported(byte[] rawProto, int rawProtoLength,
                                 ClearVRCoreWrapperExternalInterface.CbClearVRCoreWrapperRequestCompletedDelegate callback);

        [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr _tm_callCoreStatic(string base64Message, ClearVRCoreWrapperExternalInterface.CbClearVRCoreWrapperRequestCompletedDelegate callback);

        [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr _tm_callCoreStaticSync(string base64Message);

        [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void _tm_setCbClearVRCoreWrapperMessageDelegate(ClearVRCoreWrapperExternalInterface.CbClearVRCoreWrapperMessageDelegate callback);

        [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void _tm_setCbClearVRCoreWrapperRequestCompletedDelegate(ClearVRCoreWrapperExternalInterface.CbClearVRCoreWrapperRequestCompletedDelegate callback);

        [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void _tm_setCbClearVRCoreWrapperRendererFrameAvailableDelegate(IntPtr callback);

        [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void _tm_freeChar(IntPtr argCharPointerToFree);
                
        [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void _tm_freeClearVRMessageStruct(IntPtr ptr);
        
        [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void _tm_freeClearVRAsyncRequestResponseStruct(IntPtr ptr);

        [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr _tm_getTimingReport(Int32 argFlag);

        [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern Int32 _tm_getAverageBitrateInKbps();

        [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr _tm_getParameterSafely(String argKey);
        
        [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern byte _tm_setParameterSafely(String argKey, String argValue);

        [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr _tm_getArrayParameterSafely(String argKey, int argIndex);
        
        [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr _tm_getDeviceAppId();

        [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr _tm_pause();

        [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr _tm_unpause(byte[] rawProto, int rawProtoLength);

        [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr _tm_seek(byte[] argBytes, int argLength);

        [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr _tm_setStereoscopicMode(byte /* it's a 1-byte bool actually */ argMonoscopicOrStereoscopic);

        [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void _tm_sendSensorData(double argViewportPosePositionX,
                                double argViewportPosePositionY,
                                double argViewportPosePositionZ,
                                double argViewportPoseW,
                                double argViewportPoseX,
                                double argViewportPoseY,
                                double argViewportPoseZ,
                                double argDisplayObjectPosePositionX,
                                double argDisplayObjectPosePositionY,
                                double argDisplayObjectPosePositionZ,
                                double argDisplayObjectPoseW,
                                double argDisplayObjectPoseX,
                                double argDisplayObjectPoseY,
                                double argDisplayObjectPoseZ,
                                double argDisplayObjectPoseScaleX,
                                double argDisplayObjectPoseScaleY,
                                double argDisplayObjectPoseScaleZ);

        [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern byte _tm_muteAudio();

        [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern byte _tm_unmuteAudio();

        [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern byte _tm_getIsAudioMuted();

        [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void _tm_setAudioGain(float argGain);

        [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern float _tm_getAudioGain();

        [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern float _tm_getMuteState();
        
        [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr _tm_callCore(string argBase64Message);

        [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr _tm_callCoreSync(string base64Message);
        
        [DllImport(MEDIA_FLOW_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool _tm_getIsHardwareHEVCDecoderAvailable();
        
        // Allocate on the stack to make sure that it doesn't get GC-ed.
        IntPtr cbRendererFrameAsByteArrayAvailable;

        /// <summary>
        /// Class constructor
        /// </summary>
        /// <param name="argPlatformOptions">The options to use when constructing the class</param>
        /// <param name="argCbClearVREvent">The app-level ClearVREvent callback to trigger.</param>
        public MediaPlayerIOS(PlatformOptionsBase argPlatformOptions, ClearVRLayoutManager argClearVRLayoutManager, UnityEngine.Events.UnityAction<MediaPlayerBase,ClearVREvent> argCbClearVREvent) : base(argPlatformOptions, argClearVRLayoutManager, argCbClearVREvent) {
        }

        public override bool _InitializePlatformBindings() {
            if(!base._InitializePlatformBindings()) {
                return false;
            }
            GetClearVRCoreVersion();
            return true;
        }
    
        /// <summary>
        /// Called to prepare the underlying libraries for playout.
        /// This will yield a single frame that is used to construct the ClearVRDisplayObjectController.
        /// </summary>
        public override ClearVRAsyncRequest _PrepareCore() {
            if(!_state.Equals(States.Initialized)) {
                return InvokeClearVRCoreWrapperInvalidStateEvent(String.Format("Cannot prepare core when in {0} state.", GetCurrentStateAsString()), RequestTypes.Initialize);
            }
            SetState(States.PreparingCore, ClearVRMessage.GetGenericOKMessage());
            // Bootstrap the C++ -> objective-C -> Swift bridge.
            _tm_bootstrap();
            // This can only be called after _tm_boostrap() has completed.
            ClearVRCoreWrapperExternalInterface.SetMediaPlayer(this);
            // GEt and register delegates.
            _tm_setCbClearVRCoreWrapperRequestCompletedDelegate(ClearVRCoreWrapperExternalInterface.CbClearVRCoreWrapperRequestCompleted);
            _tm_setCbClearVRCoreWrapperMessageDelegate(ClearVRCoreWrapperExternalInterface.CbClearVRCoreWrapperMessage);
            byte[] rawProto = _platformOptions.ToCoreProtobuf().ToByteArray();
            ClearVRAsyncRequest clearVRAsyncRequest = ConvertClearVRAsyncRequestStructIntPtrToClearVRAsyncRequest(_tm_initialize(rawProto, rawProto.Length));
            cbRendererFrameAsByteArrayAvailable = NativeRendererPluginIOS.CVR_NRP_GetCbRendererFrameAsByteArrayAvailable();
            _tm_setCbClearVRCoreWrapperRendererFrameAvailableDelegate(cbRendererFrameAsByteArrayAvailable);
            // TODO: Port to iOS
            ///* Initialize statistics */
            //_clearVRCoreWrapperStatistics = new StatisticsAndroid(clearVRCoreWrapperRawClassGlobalRef, clearVRCoreWrapperGlobalRef);
            return clearVRAsyncRequest;
        }

        public static string GetClearVRCoreVersion() {
            if(_clearVRCoreVersionString.Equals(MediaPlayerBase.DEFAULT_CLEAR_VR_CORE_VERSION_STRING)) {
                _clearVRCoreVersionString = MarshalStringAndFree( _tm_getClearVRCoreVersion());
            }
            return _clearVRCoreVersionString;
        }

        internal static bool GetIsHardwareHEVCDecoderAvailable() {
            return _tm_getIsHardwareHEVCDecoderAvailable();
        }

        [MonoPInvokeCallback(typeof(ClearVRCoreWrapperExternalInterface.CbClearVRCoreWrapperRequestCompletedDelegate))]
        /* The underlying SDK makes sure that this callback is always invoked from the main thread. Otherwise, random SEGFAULTS are to be expected! */
        public static void CbClearVRCoreWrapperRequestCompletedForCTS(IntPtr clearVRAsyncRequestResponseStructPtr) {
            RequestCompletedStruct requestCompletedStruct = ClearVRCoreWrapperExternalInterface.ClearVRAsyncRequestResponseStructPtrToRequestCompletedStructAndFree(clearVRAsyncRequestResponseStructPtr);
            for (int i = 0; i < _staticClearVRAsyncRequests.Count; i++) {
                if(_staticClearVRAsyncRequests[i].requestId == requestCompletedStruct.clearVRAsyncRequestResponse.requestId) {
                    if(_staticClearVRAsyncRequests[i].requestType == RequestTypes.ContentSupportedTest || _staticClearVRAsyncRequests[i].requestType == RequestTypes.CallCoreStatic) {
                        // Callback is hidden in the optional arguments of the request
                        Action<ClearVRMessage> callback = (Action<ClearVRMessage>) _staticClearVRAsyncRequests[i].optionalArguments[0];
                        callback(requestCompletedStruct.clearVRMessage);
                        _staticClearVRAsyncRequests.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        public static string GetProxyParameters(string base64Message) {
            return MarshalStringAndFree(_tm_getProxyParameters(base64Message));
        }

        public static void TestIsContentSupported(ContentSupportedTesterParameters contentSupportedTesterParameters, Action<ClearVRMessage> cbClearVRMessage) {
            byte[] rawProto = contentSupportedTesterParameters.ToCoreProtobuf().ToByteArray();
            ClearVRAsyncRequest clearVRAsyncRequest = ConvertClearVRAsyncRequestStructIntPtrToClearVRAsyncRequest(_tm_testIsContentSupported(rawProto, rawProto.Length, CbClearVRCoreWrapperRequestCompletedForCTS));
            clearVRAsyncRequest.UpdateActionAndOptionalArguments(null, null, new System.Object[] { cbClearVRMessage } );
            _staticClearVRAsyncRequests.Add(clearVRAsyncRequest);
		}

        public static void CallCoreStatic(String base64Message, Action<ClearVRMessage> cbClearVRMessage) {
            ClearVRAsyncRequest clearVRAsyncRequest = ConvertClearVRAsyncRequestStructIntPtrToClearVRAsyncRequest(_tm_callCoreStatic(base64Message, CbClearVRCoreWrapperRequestCompletedForCTS));
            clearVRAsyncRequest.UpdateActionAndOptionalArguments(null, null, new System.Object[] { cbClearVRMessage } );
            _staticClearVRAsyncRequests.Add(clearVRAsyncRequest);
        }

        public static string CallCoreStaticSync(String base64Message) {
            return MarshalStringAndFree(_tm_callCoreStaticSync(base64Message));
        }

        public override void Update() {
            base.UpdateClearVREventsToInvokeQueue();
        }


		private _ClearVRViewportAndObjectPose _vopC = new _ClearVRViewportAndObjectPose();
        public override void SendSensorInfo() {
            // Intentionnaly, we use the optimized API without VOP marshalling
			UpdatePoseForSendSensorInfo(ref _vopC);
            _tm_sendSensorData(_vopC.viewportPose.posX,
                    _vopC.viewportPose.posY,
                    _vopC.viewportPose.posZ,
                    _vopC.viewportPose.w,
                    _vopC.viewportPose.x,
                    _vopC.viewportPose.y,
                    _vopC.viewportPose.z,
                    _vopC.displayObject.pose.posX,
                    _vopC.displayObject.pose.posY,
                    _vopC.displayObject.pose.posZ,
                    _vopC.displayObject.pose.w,
                    _vopC.displayObject.pose.x,
                    _vopC.displayObject.pose.y,
                    _vopC.displayObject.pose.z,
                    _vopC.displayObject.scale.x,
                    _vopC.displayObject.scale.y,
                    _vopC.displayObject.scale.z);
        }

        /// <summary>
        /// Actually load and start playout of the content item
        /// </summary>
        /// <param name="argContentItem">The content item to prepare</param>
        public override ClearVRAsyncRequest _PrepareContentForPlayout(PrepareContentParameters argPrepareContentParameters) {
            byte[] rawProto = argPrepareContentParameters.ToCoreProtobuf(_platformOptions).ToByteArray();
            return ConvertClearVRAsyncRequestStructIntPtrToClearVRAsyncRequest(_tm_prepareContentForPlayout(rawProto, rawProto.Length));
        }

        public override String GetDeviceAppId() {
            return MarshalStringAndFree(_tm_getDeviceAppId());
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
            return MarshalStringAndFree(_tm_getParameterSafely(argKey));
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
            return MarshalStringAndFree(_tm_getArrayParameterSafely(argKey, argIndex));
        }


        /// <summary>
        /// This method is directly linked to ClearVRCoreWrapper.setParameter(). Return true is successful, false otherwise.
        /// See the ClearVRCore docs for details about valid keys and when they can be queried.
        /// </summary>
        /// <param name="argKey"> The key to set.</param>
        /// <param name="argValue">The value they key should be set to.</param>
        /// <returns>true if success, false otherwise.</returns>
        public override bool SetClearVRCoreParameter(String argKey, String argValue) {
            if(!GetIsPlayerBusy())
                return false;
            bool isSuccess = (_tm_setParameterSafely(argKey, argValue) == 1);

            if(!isSuccess) { 
                ClearVRLogger.LOGW("An exception was thrown while setting key '{0}' to {1} on ClearVRCore.", argKey, argValue);
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
            if(!GetIsPlayerBusy()) {
                return InvokeClearVRCoreWrapperInvalidStateEvent(String.Format("Cannot parse media info when in {0} state.", GetCurrentStateAsString()), RequestTypes.ParseMediaInfo);
            }
            byte[] rawProto = argPopulateMediaInfoParameters.ToCoreProtobuf().ToByteArray();
            return ConvertClearVRAsyncRequestStructIntPtrToClearVRAsyncRequest(_tm_populateMediaInfo(rawProto, rawProto.Length));
        }

        /*
        Get the current average bitrate in megabit per second. Notice that the lower level method returns this value in kilobit per second.
        */
        public override float GetAverageBitrateInMbps() {
            if(!GetIsPlayerBusy())
                return 0f; // Cannot query this parameter in these states
            return (float)Math.Round((float) _tm_getAverageBitrateInKbps() / 1024f, 1);
        }

        /// <summary>
        /// Get the TimingReport containing information about the content times for both VOD and Live.
        /// </summary>
        /// <param name="argTimingType">the current TimingType used for the playback</param>
        /// <returns>TimingReport</returns>
        public override TimingReport _GetTimingReport(TimingTypes argTimingType) {
            TimingReport timingReport;
            IntPtr timingReportAsIntPtr = _tm_getTimingReport((Int32)argTimingType); // Returns null if no TimingReport is available.
            if(timingReportAsIntPtr == IntPtr.Zero) {
                return TIMING_REPORT_NOT_AVAILABLE;
            }
            string timingReportAsString = MarshalStringAndFree(timingReportAsIntPtr);
            byte[] timingReportAsByteArray = System.Convert.FromBase64String(timingReportAsString);
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
            return ConvertClearVRAsyncRequestStructIntPtrToClearVRAsyncRequest(_tm_seek(rawProto, rawProto.Length));
        }

        public override bool SetMuteAudio(bool argIsMuted) {
            if (argIsMuted) {
               return (_tm_muteAudio() == 1); // returns a bool in byte disguise
            } else {
                bool returnedValue = (_tm_unmuteAudio() == 1);
				if(returnedValue) {
					// Successfully unmuted, the SetMuteAudio() API returns false in this case
					return false;
				}
				// Unable to unmute, we fall-through to returning false.
            }
            return false;
        }

        public override void SetAudioGain(float argGain) {
            _tm_setAudioGain(argGain);
        }

        public override float GetAudioGain() {
			if(!hasReachedPreparingContentForPlayoutState) { // Only by this point, the initial settings have been signalled to wrapper.
                return _platformOptions.initialAudioGain;
            }
            return _tm_getAudioGain();
        }

        public override bool GetIsAudioMuted() {
			if(!hasReachedPreparingContentForPlayoutState) { // Only by this point, the initial settings have been signalled to wrapper.
                return false;
            }
            return (_tm_getIsAudioMuted() == 1);
        }
        
        public override float GetMuteState() {
			if(!hasReachedPreparingContentForPlayoutState) { // Only by this point, the initial settings have been signalled to wrapper.
                return _platformOptions.muteState;
            }
            return _tm_getMuteState();
        }

        public override ClearVRAsyncRequest _SwitchContent(SwitchContentParameters argSwitchContentParameters) {
            byte[] rawProto = argSwitchContentParameters.ToCoreProtobuf().ToByteArray();
            return ConvertClearVRAsyncRequestStructIntPtrToClearVRAsyncRequest(_tm_switchContent(rawProto, rawProto.Length));
        }

        public override bool SetLoopContent(bool argIsContentLoopEnabled) {
            return SetClearVRCoreParameter("playback.loop_content", argIsContentLoopEnabled.ToString());
        }

        public override ClearVRAsyncRequest _SetStereoMode(bool argStereo) {
            if(!GetIsPlayerBusy()) {
                return InvokeClearVRCoreWrapperInvalidStateEvent(String.Format("Cannot set stereo mode when in {0} state.", GetCurrentStateAsString()), RequestTypes.ChangeStereoMode);
            }
            return ConvertClearVRAsyncRequestStructIntPtrToClearVRAsyncRequest(_tm_setStereoscopicMode(argStereo ? (byte) 1 : (byte) 0));
        }

        public override ClearVRAsyncRequest _StartPlayout() {
            if(!GetIsPlayerBusy()) {
                return InvokeClearVRCoreWrapperInvalidStateEvent("You can only call Play() once. If you would like to unpause, use the Unpause() API instead.", RequestTypes.Start);
            }
            return ConvertClearVRAsyncRequestStructIntPtrToClearVRAsyncRequest(_tm_startPlayout());
        }

        public override ClearVRAsyncRequest _Pause() {
            if(!GetIsPlayerBusy()) {
                return InvokeClearVRCoreWrapperInvalidStateEvent(String.Format("Cannot pause when in {0} state.", GetCurrentStateAsString()), RequestTypes.Pause);
            }
            return ConvertClearVRAsyncRequestStructIntPtrToClearVRAsyncRequest(_tm_pause());
        }

        public override ClearVRAsyncRequest _Unpause(TimingParameters argTimingParameters) {
            if(!GetIsPlayerBusy()) {
                return InvokeClearVRCoreWrapperInvalidStateEvent(String.Format("Cannot unpause when in {0} state.", GetCurrentStateAsString()), RequestTypes.Unpause);
            }
            byte[] rawProto = null;
            int rawProtoLength = 0;
            if(argTimingParameters != null) {
                rawProto = argTimingParameters.ToCoreProtobuf().ToByteArray();
                rawProtoLength = rawProto.Length;
            }
            return ConvertClearVRAsyncRequestStructIntPtrToClearVRAsyncRequest(_tm_unpause(rawProto, rawProtoLength));
        }


        public override ClearVRAsyncRequest _CallCore(byte[] argRawMessage) {
            String base64Message = System.Convert.ToBase64String(argRawMessage);
            return ConvertClearVRAsyncRequestStructIntPtrToClearVRAsyncRequest(_tm_callCore(base64Message));
        }

        public override String _CallCoreSync(String base64Message) {
            return MarshalStringAndFree(_tm_callCoreSync(base64Message));
        }

        protected static ClearVRAsyncRequest ConvertClearVRAsyncRequestStructIntPtrToClearVRAsyncRequest(IntPtr argClearVRAsyncRequestStructIntPtr) {
            ClearVRCoreWrapperExternalInterface.ClearVRAsyncRequestStruct clearVRAsyncRequestStruct = (ClearVRCoreWrapperExternalInterface.ClearVRAsyncRequestStruct)Marshal.PtrToStructure(argClearVRAsyncRequestStructIntPtr, typeof(ClearVRCoreWrapperExternalInterface.ClearVRAsyncRequestStruct));
            ClearVRAsyncRequest clearVRAsyncRequest = new ClearVRAsyncRequest((RequestTypes)clearVRAsyncRequestStruct.requestType, Convert.ToInt32(clearVRAsyncRequestStruct.requestId));
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
            IntPtr clearVRAsyncRequestMaybe = _tm_stop(); // Might return nil is _bootstrap() was never called. This is unlikely but not impossible.
            if(clearVRAsyncRequestMaybe != IntPtr.Zero) {
                return ConvertClearVRAsyncRequestStructIntPtrToClearVRAsyncRequest(clearVRAsyncRequestMaybe);
            } // else: _stop() returned a null pointer, so we fake a state change to Stopped as we will never get one from the MediaFlow.
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
        /// Copy the string from the char pointer. Return empty string if the conversion is invalid
        /// </summary>
        protected static String MarshalString(IntPtr stringAsIntPtr) {
            String marshaledString = Marshal.PtrToStringAnsi(stringAsIntPtr);
            if(marshaledString == null) {
                marshaledString = "";
            }
            return marshaledString;
        }

        /// <summary>
        /// Copy the string from the char pointer AND frees the pointer. Return empty string if the conversion is invalid
        /// </summary>
        protected static String MarshalStringAndFree(IntPtr argStringAsIntPtr)
        {
            String marshaledString = MarshalString(argStringAsIntPtr);
            _tm_freeChar(argStringAsIntPtr);
            return marshaledString;
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
            _tm_destroy();
        }

    }
}
#endif
