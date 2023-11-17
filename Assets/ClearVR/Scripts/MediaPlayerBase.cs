using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Assertions;
using System;
using System.Runtime.InteropServices;
using AOT;
using System.Runtime.CompilerServices;

using com.tiledmedia.clearvr.protobuf;
using cvri = com.tiledmedia.clearvr.cvrinterface;

namespace com.tiledmedia.clearvr {
	internal abstract class MediaPlayerBase : InternalInterface, MediaPlayerInterface, MediaInfoInterface, MediaControllerInterface, PerformanceInterface, SyncInterface, DebugInterface {
		/* InternalInterface */
		public abstract void Update();
		public abstract void CleanUpAfterStopped();
		public abstract void SendSensorInfo();
		public abstract ClearVRAsyncRequest _PrepareContentForPlayout(PrepareContentParameters argPrepareContentParameters);
		public abstract ClearVRAsyncRequest _StartPlayout();
		public abstract ClearVRAsyncRequest _PrepareCore();
		public abstract ClearVRAsyncRequest _Pause();
		public abstract ClearVRAsyncRequest _Unpause(TimingParameters argTimingParameters = null);
		public abstract ClearVRAsyncRequest _PopulateMediaInfo(PopulateMediaInfoParameters argPopulateMediaInfoParameters);
		public abstract ClearVRAsyncRequest _Seek(SeekParameters argSeekParameters);
		public abstract ClearVRAsyncRequest _SetStereoMode(bool argStereo);
		public abstract ClearVRAsyncRequest _SwitchContent(SwitchContentParameters argSwitchContentParameters);
		public abstract TimingReport _GetTimingReport(TimingTypes argTimingType);
		public abstract ClearVRAsyncRequest _CallCore(byte[] argMessage);
		public abstract String _CallCoreSync(String argRawMessage);

		/* MediaPlayerInterface */
		public abstract String GetClearVRCoreParameter(String argKey);
		public abstract String GetClearVRCoreArrayParameter(String argKey, int argIndex);
        public abstract bool SetClearVRCoreParameter(String argKey, String argValue);

		public abstract String GetDeviceAppId();

		/* MediaControllerInterface */
        public abstract bool SetLoopContent(bool argIsContentLoopEnabled);
		public abstract bool SetMuteAudio(bool argIsMuted);
		public abstract void SetAudioGain(float argGain);
		public abstract float GetAudioGain();
		public abstract bool GetIsAudioMuted();

		/* MediaInfoInterface */
		public abstract float GetMuteState();

		/* PerformanceInterface */
		public abstract float GetAverageBitrateInMbps();
		protected StatisticsBase _clearVRCoreWrapperStatistics = null;

		/* Other */
		 // We need the ClearVREvents proxy class for 2018 compatibility
		protected ClearVREvents _clearVREvents = new ClearVREvents();
		protected class ClearVREvents : UnityEngine.Events.UnityEvent<MediaPlayerBase, ClearVREvent> {}
        protected bool hasReachedPreparingContentForPlayoutState { // True if state >= PrepareContentForPlayout && < Stopping
			get {
				return ___hasReachedPreparingContentForPlayoutState;
			}
			set {
				___hasReachedPreparingContentForPlayoutState = value;
				// Guaranteed to be never null by this point as it is set int he constructor.
				_clearVRLayoutManager.SetMediaPlayerCanAcceptNRPVSyncs(___hasReachedPreparingContentForPlayoutState);
			}
		}
		public bool GetDidReachPreparingContentForPlayoutStateButNotInStoppingOrStoppedState() {
			return hasReachedPreparingContentForPlayoutState;
		}
		private bool ___hasReachedPreparingContentForPlayoutState = false;
        protected bool isPlayerShuttingDown = false; // Set to true in StopClearVR() when shutting down the application.
		protected PlatformOptionsBase _platformOptions;
		public PlatformOptionsBase platformOptions {
			get {return _platformOptions; }
		}
		private readonly String API_OBSOLETE_MESSAGE = "This API is obsolete and cannot be used. Refer to the documentation on how to update this API call.";
		protected RenderModes _renderMode = RenderModes.Native;
		internal static string DEFAULT_CLEAR_VR_CORE_VERSION_STRING = "Unknown";
		protected static string _clearVRCoreVersionString = DEFAULT_CLEAR_VR_CORE_VERSION_STRING;
		protected LinkedList<ClearVREvent> clearVREventToInvokeLinkedList = new LinkedList<ClearVREvent>();
		protected AudioTrackAndPlaybackParameters audioTrackAndPlaybackParameters;
		public AudioTrackAndPlaybackParameters GetAudioTrackAndPlaybackParameters() {return audioTrackAndPlaybackParameters;}
		protected bool isMediaInfoParsed = false;
		protected List<ClearVRAsyncRequest> _clearVRAsyncRequests = new List<ClearVRAsyncRequest>();
        // Used for static asynchronous APIs that return a ClearVRAsyncRequest
        protected static List<ClearVRAsyncRequest> _staticClearVRAsyncRequests = new List<ClearVRAsyncRequest>();
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
		/// <summary>
		/// Since we always signal the max(screenWidth, screenHeight) as screen width to the core (regardless of orientation), we also have to make sure that we send the matching roll when in touch only mode (e.g. not magic window mode)
		/// If you change this, be sure to also refer to DeviceParameters.Verify() as things will probably have to change there as well.
		/// </summary>
		private readonly ScreenOrientation _initialScreenOrientation = ScreenOrientation.LandscapeLeft;
#endif
		protected readonly TimingReport TIMING_REPORT_NOT_AVAILABLE = new TimingReport(new ClearVRMessage(ClearVRMessageTypes.Warning, ClearVRMessageCodes.ClearVRCoreWrapperGenericOK, "Unable to query timing report in current state.", ClearVRResult.Failure), TimingTypes.ContentTime, 0, 0, 0, 0, EventTypes.Unknown);
		public List<ClearVRAsyncRequest> clearVRAsyncRequests {
			get {
				return _clearVRAsyncRequests;
			}
		}

        internal struct RequestCompletedStruct {
            public ClearVRAsyncRequestResponse clearVRAsyncRequestResponse;
            public ClearVRMessage clearVRMessage;
            internal RequestCompletedStruct(ClearVRAsyncRequestResponse argClearVRAsyncRequestResponse, ClearVRMessage argClearVRMessage) {
                clearVRAsyncRequestResponse = argClearVRAsyncRequestResponse;
                clearVRMessage = argClearVRMessage;
            }
        }
		public static readonly _ClearVRViewportAndObjectPose defaultViewportAndObjectPose_C = new _ClearVRViewportAndObjectPose(1.0);

		/// <summary>
		/// Must be in degree (read the variable name :) )
		///
		/// This is used to compensate roll when the screen orientation changes from what it was during player initialization (ref. #3489)
		/// </summary>
		private float compensatedRollInDegree = 0.0f;
        protected enum States {
			Undefined,
			Uninitialized,
			Initializing,
			Initialized,
			PreparingCore,
			CorePrepared,
			PreparingContentForPlayout,
			ContentPreparedForPlayout,
			Buffering,
			Playing,
			Pausing,
			Paused,
			Seeking,
			SwitchingContent,
			Finished,
			Stopping,
			Stopped,
		}

	    // For debugging only
        [MethodImpl(MethodImplOptions.NoInlining)]
        protected string GetCurrentMethod() {
            StackTrace st = new StackTrace();
            StackFrame sf = st.GetFrame(1);
            return sf.GetMethod().Name;
        }

        protected String GetCurrentStateAsString() {
			return _state.ToString();
		}

		// Obsolete API since 2022-03-01, do not use
		public bool GetIsMediaInfoParsed() {
			return isMediaInfoParsed && GetIsInitialized();
		}

		public bool GetIsInitialized() {
			return (_state != States.Uninitialized &&
					_state != States.Initializing &&
					_state != States.Stopping &&
					_state != States.Stopped);
		}

		public bool GetIsPauseAllowed() {
			return GetIsPlayerBusy() &&
				! (_state == States.Initialized) &&
				! (_state == States.CorePrepared) &&
				! (_state == States.PreparingContentForPlayout) &&
				! (_state == States.ContentPreparedForPlayout) &&
				! (_state == States.Undefined) &&
				! GetIsInPausingOrPausedState() &&
				! GetIsInFinishedState();
		}

		public bool GetIsInPlayingState() {
			return (_state == States.Playing);
		}

		public bool GetIsInPausedState() {
			return (_state == States.Paused);
		}

		public bool GetIsInFinishedState() {
		        return (_state == States.Finished);
		}
		public bool GetIsInStoppedState() {
			return (_state == States.Stopped);
		}

		public bool GetCanPerformanceMetricesBeQueried() {
			return (GetIsInPausingOrPausedState() ||
                GetIsInPlayingState() ||
                _state == States.Buffering ||
                _state == States.Seeking ||
				_state == States.SwitchingContent ||
				_state == States.ContentPreparedForPlayout ||
				_state == States.Finished);
		}

		/* The player is regarded busy if it has started preparing the core */
		public bool GetIsPlayerBusy() {
			return (GetIsInitialized() &&
					_state != States.PreparingCore);
		}

		public bool GetIsInPausingOrPausedState() {
			return (_state == States.Pausing ||
					GetIsInPausedState());
		}

		public StatisticsBase clearVRCoreWrapperStatistics {
			get {return _clearVRCoreWrapperStatistics; }
		}

		protected States _state = States.Uninitialized;
		protected bool _isViewportTrackingEnabled = true;
		private ClearVRLayoutManager _clearVRLayoutManager;

		public MediaPlayerBase(PlatformOptionsBase argPlatformOptions, ClearVRLayoutManager argClearVRLayoutManager, UnityEngine.Events.UnityAction<MediaPlayerBase,ClearVREvent> argCbClearVREvent) {
			/* Attach ClearVREvents event listener. This will relay any event to the ClearVRPlayer parent which in turn can relay any message to the base constructor class. */
			_platformOptions = argPlatformOptions;
			_clearVRLayoutManager = argClearVRLayoutManager;
			_renderMode = _platformOptions.preferredRenderMode;
			ClearVRDisplayObjectControllerBase.LoadShaders();
            if (argCbClearVREvent != null) {
				_clearVREvents.AddListener(argCbClearVREvent);
			}
            if (!_InitializePlatformBindings()) {
                ScheduleClearVREvent(new ClearVREvent(ClearVREventTypes.UnableToInitializePlayer, ClearVRMessageTypes.FatalError, ClearVRMessageCodes.GenericFatalError, String.Format("An error occured while initializing {0}}-specific platform bindings. Cannot continue.", argPlatformOptions.platform), ClearVRResult.Failure));
                return;
            }
            if (_state != States.Initializing) {
                throw new Exception("[ClearVR] You must call InitializePlatformBindings() before you can instantiate this class.");
            }
            SetState(States.Initialized, ClearVRMessage.GetGenericOKMessage());
		}

		public virtual bool _InitializePlatformBindings() {
			SetState(States.Initializing, ClearVRMessage.GetGenericOKMessage());
			return true;
		}

		protected bool GetIsRenderingInStereoscopicMode() {
			return _renderMode == RenderModes.Stereoscopic;
		}

		//This is now depreciated and will be removed once we remove the function from the interface
        public ContentFormat GetContentFormat() {
			return ContentFormat.Unknown;
		}

		//This is now depreciated and will be removed once we remove the function from the interface
		public bool GetIsContentFormatStereoscopic() {
			return false;
		}


		protected void UpdateClearVREventsToInvokeQueue() {
			lock (clearVREventToInvokeLinkedList) {
				while(clearVREventToInvokeLinkedList.Count > 0) {
					LinkedListNode<ClearVREvent> clearVREvent = clearVREventToInvokeLinkedList.First;
					clearVREventToInvokeLinkedList.RemoveFirst();
					InvokeEvent(clearVREvent.Value);
				}
			}
		}

		public RenderModes GetRenderMode() {
			return _renderMode;
		}

		public void EnableOrDisableViewportTracking(bool argIsEnabledOrDisabled) {
			_isViewportTrackingEnabled = argIsEnabledOrDisabled;
		}

		protected ClearVRAsyncRequest InvokeClearVRCoreWrapperInvalidStateEvent(String argMessage, RequestTypes argRequestType) {
			ClearVRMessage clearVRMessage = new ClearVRMessage(ClearVRMessageTypes.Warning,
						ClearVRMessageCodes.ClearVRCoreWrapperInvalidState,
						argMessage,
					ClearVRResult.Failure);

			ClearVRAsyncRequest clearVRAsyncRequest = new ClearVRAsyncRequest(argRequestType);
			ScheduleClearVREvent(new ClearVREvent(ClearVREventTypes.GenericMessage,
				new ClearVRAsyncRequestResponse(clearVRAsyncRequest.requestType,
					clearVRAsyncRequest.requestId),
					clearVRMessage
				)
			);
			return clearVRAsyncRequest;
		}

		protected ClearVRAsyncRequest InvokeAPINotSupportedOnThisPlatformEvent(String argMessage, RequestTypes argRequestType, bool argIsFatal = true) {
			return InvokeDefaultEvent(ClearVRMessageCodes.APINotSupportedOnThisPlatform, argMessage, argRequestType, argIsFatal);
		}

		protected ClearVRAsyncRequest InvokeCannotCompleteRequestEvent(String argMessage, RequestTypes argRequestType, bool argIsFatal = true) {
			return InvokeDefaultEvent(ClearVRMessageCodes.ClearVRCoreWrapperRequestCancelled, argMessage, argRequestType, argIsFatal);
		}

		protected ClearVRAsyncRequest InvokeAPIObsoleteEvent(String argMessage, RequestTypes argRequestType, bool argIsFatal = true) {
			return InvokeDefaultEvent(ClearVRMessageCodes.APIObsolete, argMessage, argRequestType, argIsFatal);
		}

		// Do not call directly, use helper method instead (See e.g. InvokeAPINotSupportedOnThisPlatformEvent())
		private ClearVRAsyncRequest InvokeDefaultEvent(ClearVRMessageCodes argClearVRMessageCode, String argMessage, RequestTypes argRequestType, bool argIsFatal = true) {
			ClearVRMessage clearVRMessage = new ClearVRMessage(argIsFatal ? ClearVRMessageTypes.FatalError : ClearVRMessageTypes.Warning,
						argClearVRMessageCode,
						argMessage,
					ClearVRResult.Failure);

			ClearVRAsyncRequest clearVRAsyncRequest = new ClearVRAsyncRequest(argRequestType);
			ScheduleClearVREvent(new ClearVREvent(ClearVREventTypes.GenericMessage,
				new ClearVRAsyncRequestResponse(clearVRAsyncRequest.requestType,
					clearVRAsyncRequest.requestId),
					clearVRMessage
				)
			);
			return clearVRAsyncRequest;
		}

		public PlatformOptionsBase GetPlatformOptions() {
			return _platformOptions;
		}

		public void SetRenderMode(RenderModes argNewRenderMode) {
            // Obsoleted since 2022-06-30
		}

        public void SetRenderModeOnAllDisplayObjects(RenderModes argNewRenderMode) {
			if(_clearVRLayoutManager != null) {
				_clearVRLayoutManager.SetRenderModeOnAllDisplayObjects(argNewRenderMode);
			} // else: nothing to do, and in general impossible to happen.
        }

        public void SetRenderModeOnAllDisplayObjectsConditionally(RenderModes argNewRenderMode, RenderModes argRequiredCurrentRenderMode) {
			if(_clearVRLayoutManager != null) {
				_clearVRLayoutManager.SetRenderModeOnAllDisplayObjectsConditionally(argNewRenderMode, argRequiredCurrentRenderMode);
			} // else: nothing to do, and in general impossible to happen.
        }

#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
		/// <summary>
		/// We apply a roll on the SendSensorData call when the orientation of the screen has changed in flat mode (e.g. no headset/no magic window mode active).
		/// This is only active on Android and iOS platform, and only triggered on certain DeviceTypes.
		/// </summary>
		/// <param name="argScreenOrientation">The new (current) screen orientation.</param>
		public void UpdateScreenOrientation(ScreenOrientation argScreenOrientation) {
			if(_initialScreenOrientation == argScreenOrientation) {
				compensatedRollInDegree = 0;
				return;
			}
			switch(_initialScreenOrientation) {
				//case ScreenOrientation.Landscape: // is the same as LandscapeLeft
				case ScreenOrientation.LandscapeLeft:
					switch(argScreenOrientation) {
						case ScreenOrientation.LandscapeLeft:
							compensatedRollInDegree = 0F;
							break;
						case ScreenOrientation.LandscapeRight:
							compensatedRollInDegree = +180F;
							break;
						case ScreenOrientation.Portrait:
							compensatedRollInDegree = +90F;
							break;
						case ScreenOrientation.PortraitUpsideDown:
							compensatedRollInDegree = -90F;
							break;
						default:
							compensatedRollInDegree = 0F;
							break;
					}
					break;
				case ScreenOrientation.LandscapeRight:
					switch(argScreenOrientation) {
						case ScreenOrientation.LandscapeLeft:
							compensatedRollInDegree = +180F;
							break;
						case ScreenOrientation.LandscapeRight:
							compensatedRollInDegree = 0F;
							break;
						case ScreenOrientation.Portrait:
							compensatedRollInDegree = -90F;
							break;
						case ScreenOrientation.PortraitUpsideDown:
							compensatedRollInDegree = +90F;
							break;
						default:
							compensatedRollInDegree = 0F;
							break;
					}
					break;
				case ScreenOrientation.Portrait:
					switch(argScreenOrientation) {
						case ScreenOrientation.LandscapeLeft:
							compensatedRollInDegree = -90F;
							break;
						case ScreenOrientation.LandscapeRight:
							compensatedRollInDegree = +90F;
							break;
						case ScreenOrientation.Portrait:
							compensatedRollInDegree = 0F;
							break;
						case ScreenOrientation.PortraitUpsideDown:
							compensatedRollInDegree = +180F;
							break;
						default:
							compensatedRollInDegree = 0F;
							break;
					}
					break;
				case ScreenOrientation.PortraitUpsideDown:
					switch(argScreenOrientation) {
						case ScreenOrientation.LandscapeLeft:
							compensatedRollInDegree = +90F;
							break;
						case ScreenOrientation.LandscapeRight:
							compensatedRollInDegree = -90F;
							break;
						case ScreenOrientation.Portrait:
							compensatedRollInDegree = +180F;
							break;
						case ScreenOrientation.PortraitUpsideDown:
							compensatedRollInDegree = 0F;
							break;
						default:
							compensatedRollInDegree = 0F;
							break;
					}
					break;
				//case ScreenOrientation.AutoRotation: // Never set.
				default:
					compensatedRollInDegree = 0F;
					break;
			}
		}
#endif

		/// <summary>
		/// Any roll smaller than this value will be regarded as "no-roll", which in turn will be interpreted as: there is no magic window mode/headset active when manually applying roll because of screen orientation change.
		/// </summary>
		private const float _minimumRollThresholdInDegree = 0.001f;
		private Quaternion _trackingTransformRotation;
		private Vector3 _trackingTransformPosition;
		private Vector3 _trackingTransformRotationEuler;
		private Transform _meshTransform;
		protected void UpdatePoseForSendSensorInfo(ref _ClearVRViewportAndObjectPose vaop) {
			_trackingTransformPosition = _platformOptions.trackingTransform.position;
			_trackingTransformRotation = _platformOptions.trackingTransform.rotation;
			_trackingTransformRotationEuler = _trackingTransformRotation.eulerAngles;
			// If the roll of the camera is smaller than a certain threshold, assume that no magic window mode/headset is active
			if(_trackingTransformRotationEuler.z < _minimumRollThresholdInDegree) {
				_trackingTransformRotationEuler.z = _trackingTransformRotationEuler.z + compensatedRollInDegree;
				_trackingTransformRotation.eulerAngles = _trackingTransformRotationEuler;
			} // else: roll is bigger than _minimumRollThresholdInDegree, we assume that a headset/magic window mode is active.
			vaop.viewportPose.posX = _trackingTransformPosition.x;
			vaop.viewportPose.posY = _trackingTransformPosition.y;
			vaop.viewportPose.posZ = _trackingTransformPosition.z;
			vaop.viewportPose.w = _trackingTransformRotation.w;
			vaop.viewportPose.x = _trackingTransformRotation.x;
			vaop.viewportPose.y = _trackingTransformRotation.y;
			vaop.viewportPose.z = _trackingTransformRotation.z;
			if(_clearVRLayoutManager.mainDisplayObjectController != null) {
				_meshTransform = _clearVRLayoutManager.mainDisplayObjectController.transform;
				vaop.displayObject.pose.posX = _meshTransform.position.x;
				vaop.displayObject.pose.posY = _meshTransform.position.y;
				vaop.displayObject.pose.posZ = _meshTransform.position.z;
				vaop.displayObject.pose.w = _meshTransform.rotation.w;
				vaop.displayObject.pose.x = _meshTransform.rotation.x;
				vaop.displayObject.pose.y = _meshTransform.rotation.y;
				vaop.displayObject.pose.z = _meshTransform.rotation.z;
				vaop.displayObject.scale.x = _meshTransform.lossyScale.x;
				vaop.displayObject.scale.y = _meshTransform.lossyScale.y;
				vaop.displayObject.scale.z = _meshTransform.lossyScale.z;
			}
		}
		/// <summary>
		/// This is a custom version of UpdatePoseForSendSensorInfo() above taking additional bool arguments.
		/// **** NOTE ***
		/// This code is duplicated intentionally because of performance considerations. UpdatePoseForSendSensorInfo() is called every vsync and should be as fast as possible.
		/// </summary>
		protected _ClearVRViewportAndObjectPose UpdatePoseInfoCustom() {
			_ClearVRViewportAndObjectPose vaop = new _ClearVRViewportAndObjectPose();
			_trackingTransformPosition = _platformOptions.trackingTransform.position;
			_trackingTransformRotation = _platformOptions.trackingTransform.rotation;
			_trackingTransformRotationEuler = _trackingTransformRotation.eulerAngles;
			// If the roll of the camera is smaller than a certain threshold, assume that no magic window mode/headset is active
			if(_trackingTransformRotationEuler.z < _minimumRollThresholdInDegree) {
				_trackingTransformRotationEuler.z = _trackingTransformRotationEuler.z + compensatedRollInDegree;
				_trackingTransformRotation.eulerAngles = _trackingTransformRotationEuler;
			} // else: roll is bigger than _minimumRollThresholdInDegree, we assume that a headset/magic window mode is active.
			vaop.viewportPose.posX = _trackingTransformPosition.x;
			vaop.viewportPose.posY = _trackingTransformPosition.y;
			vaop.viewportPose.posZ = _trackingTransformPosition.z;
			vaop.viewportPose.w = _trackingTransformRotation.w;
			vaop.viewportPose.x = _trackingTransformRotation.x;
			vaop.viewportPose.y = _trackingTransformRotation.y;
			vaop.viewportPose.z = _trackingTransformRotation.z;
			if(_clearVRLayoutManager.mainDisplayObjectController != null) {
				_meshTransform = _clearVRLayoutManager.mainDisplayObjectController.transform;
				vaop.displayObject.pose.posX = _meshTransform.position.x;
				vaop.displayObject.pose.posY = _meshTransform.position.y;
				vaop.displayObject.pose.posZ = _meshTransform.position.z;
				vaop.displayObject.pose.w = _meshTransform.rotation.w;
				vaop.displayObject.pose.x = _meshTransform.rotation.x;
				vaop.displayObject.pose.y = _meshTransform.rotation.y;
				vaop.displayObject.pose.z = _meshTransform.rotation.z;
				vaop.displayObject.scale.x = _meshTransform.localScale.x;
				vaop.displayObject.scale.y = _meshTransform.localScale.y;
				vaop.displayObject.scale.z = _meshTransform.localScale.z;
			}
			return vaop;
		}

		public Pose GetDefaultViewportPose() {
			Pose outputPose = new Pose();
			outputPose.position = new Vector3(0, 0, 0);
			outputPose.rotation = new Quaternion(0, 0, 0, 1);
			return outputPose;
		}

		public void LateUpdate() {
			/* sending sensor data is only allowed after the first frame has been rendered. */
			if(___hasReachedPreparingContentForPlayoutState /* use direct field */ && !isPlayerShuttingDown) {
				if(_isViewportTrackingEnabled) {
					SendSensorInfo();
				}
			}
		}

		public  bool _GetIsPlayerShuttingDown() {
			return isPlayerShuttingDown;
		}
		public EventTypes GetEventType() {
			ContentInfo contentInfo = GetContentInfo(null);
			if(contentInfo == null) {
				return EventTypes.Unknown;
			}
			return contentInfo.eventType;
		}

		public ContentInfo GetContentInfo(ContentItem contentItem) {
			if (!GetIsInitialized()) {
				UnityEngine.Debug.LogWarning("[ClearVR] Cannot call GetContentInfo() in the current ClearVRPlayer state. First the ClearVRPlayer needs to be initialized.");
				return null;
			}
			cvri.ContentInfoRequest builder = new cvri.ContentInfoRequest();
			if(contentItem != null) {
				builder = new cvri.ContentInfoRequest {
					ContentItem = contentItem.ToCoreProtobuf()
				};
			}
			cvri.CallCoreRequest request = new cvri.CallCoreRequest() {
				CallCoreRequestType = cvri::CallCoreRequestType.ContentInfo,
				ContentInfoRequest = builder
			};

			String message = CallCoreSync(System.Convert.ToBase64String(request.ToByteArray()));
			if (String.IsNullOrEmpty(message)) {
				// Returns null if platform is not supported or the Wrapper is not initialized yet.
				UnityEngine.Debug.LogWarning("[ClearVR] Cannot call getContentInfo() in the current ClearVRPlayer state. It can only be called after the ContentPreparedForPlayout state.");
				return null;
			}
			byte[] raw = System.Convert.FromBase64String(message);
			cvri.CallCoreResponse callCoreResponse = cvri.CallCoreResponse.Parser.ParseFrom(raw);
			cvri.ContentInfoMessage contentInfoMessage = callCoreResponse.ContentInfoMessage;
			if (callCoreResponse.ErrorCode != 0) {
				UnityEngine.Debug.LogError(String.Format("[ClearVR] Unable to call GetContentInfo(). returned with error code: {0}. Error message: {1}", callCoreResponse.ErrorMessage, callCoreResponse.ErrorMessage));
				return null;
			}
			if (contentInfoMessage == null || contentInfoMessage.Feeds.Count == 0) {
				UnityEngine.Debug.LogWarning("[ClearVR] ContentInfo request response does not contain any feeds. Use an active content item please.");
				return null;
			}
			return new ContentInfo(contentInfoMessage);
		}


		/// <summary>
		/// This base method returns null in case of success, a ClearVRAsyncRequest in case of failure.
		/// It must be called from the MediaPlayer-[Platform] class.
		/// </summary>
		/// <param name="PrepareContentParameters">The parameters to verify, passed by reference and changed in place where need be.</param>
		/// <returns>null if everything went OK, a ClearVRAsyncRequest in case of any failure</returns>
		private ClearVRAsyncRequest VerifyPrepareContentForPlayoutRequest(ref PrepareContentParameters argPrepareContentParameters) {
			RequestTypes requestType = RequestTypes.PrepareContentForPlayout;
			if(_state != States.CorePrepared) {
				return InvokeClearVRCoreWrapperInvalidStateEvent(String.Format("Cannot prepare content for playout when in {0} state.", GetCurrentStateAsString()), requestType);
			}
			if(!argPrepareContentParameters.contentItem.Verify()) {
				return InvokeCannotCompleteRequestEvent("Cannot prepare content for playout. Provided content item invalid!", requestType);
			}
            SetClearVRCoreParameter("config.input_coordinate_system", "ZmXY");
			VerifyAudioTrackAndPlaybackParameters(ref argPrepareContentParameters._audioTrackAndPlaybackParameters);
			audioTrackAndPlaybackParameters = argPrepareContentParameters.audioTrackAndPlaybackParameters;
			if(argPrepareContentParameters.layoutParameters == null) {
				argPrepareContentParameters.layoutParameters = _clearVRLayoutManager.GetLayoutParametersByNameNoCopy(ClearVRLayoutManager.LEGACY_LAYOUT_NAME);
			}
			if(argPrepareContentParameters.layoutParameters.displayObjectMappings.Count == 0) {
				return InvokeCannotCompleteRequestEvent(String.Format("Cannot prepare content for playout. No DisplayObjectMappings specified on provided LayoutParameters {0}!", argPrepareContentParameters.layoutParameters.name), requestType);
			}

			// There is a check in PlatformOptionsAndroid::Verify() to make sure the right blit mode is configured.

			// This ensures that the LayoutParameters are properly set-up and valid (e.g. all DOs have a DOID).
			ClearVRAsyncRequest clearVRAsyncRequest = AddOrUpdateAndVerifyLayoutParametersOnLayoutManager(argPrepareContentParameters.layoutParameters,requestType, true);
			if(clearVRAsyncRequest != null) {
				return clearVRAsyncRequest;
			}
			_clearVRLayoutManager.mainDisplayObjectController = argPrepareContentParameters.layoutParameters.displayObjectMappings[0].clearVRDisplayObjectController;
            // We set the current VDOP on the SwitchCOntentParameters, this relies on _clearVRLayoutManager.mainDisplayObjectController being set.
            argPrepareContentParameters.clearVRViewportAndObjectPose = UpdatePoseInfoCustom();
			SetState(States.PreparingContentForPlayout, ClearVRMessage.GetGenericOKMessage());
			// For now we force_mono on WaveVR headsets as stereoscopic rendering is broken. See #2235 and #2245 respectively.
			bool forceMono = !Utils.GetIsVrDevicePresent() || !_platformOptions.deviceParameters.deviceType.GetIsVRDeviceThatCanRenderStereoscopicContent();
			SetClearVRCoreParameter("config.force_mono", forceMono.ToString().ToLower());
			if(forceMono) {
				SetRenderMode(RenderModes.Monoscopic);
			}
			return null;
		}

		private ClearVRAsyncRequest VerifySwitchContentRequest(ref SwitchContentParameters argSwitchContentParameters) {
			RequestTypes requestType = RequestTypes.SwitchContent;
			if(!GetIsInitialized()) {
				return InvokeClearVRCoreWrapperInvalidStateEvent(String.Format("Cannot switch content when in {0} state.", GetCurrentStateAsString()), requestType);
			}
			if(argSwitchContentParameters == null) {
				return InvokeCannotCompleteRequestEvent("Cannot switch content. No SwitchContentParameters provided.", RequestTypes.SwitchContent);
			}
			if(!argSwitchContentParameters.contentItem.Verify()) {
				return InvokeCannotCompleteRequestEvent("Cannot switch content. Provided content item invalid!", requestType);
			}
			if(argSwitchContentParameters.layoutParameters == null) {
				argSwitchContentParameters.layoutParameters = _clearVRLayoutManager.GetLayoutParametersByNameNoCopy(ClearVRLayoutManager.LEGACY_LAYOUT_NAME);
			}
			if(argSwitchContentParameters.layoutParameters.displayObjectMappings.Count == 0) {
				return InvokeCannotCompleteRequestEvent(String.Format("Cannot switch content. No DisplayObjectMappings specified on provided LayoutParameters {0}!!", argSwitchContentParameters.layoutParameters.name), RequestTypes.SwitchContent);
			}
			// This ensures that the LayoutParameters are properly set-up and valid (e.g. all DOs have a DOID).
			ClearVRAsyncRequest clearVRAsyncRequest = AddOrUpdateAndVerifyLayoutParametersOnLayoutManager(argSwitchContentParameters.layoutParameters,requestType, false);
			if(clearVRAsyncRequest != null) {
				return clearVRAsyncRequest;
			}

			_clearVRLayoutManager.mainDisplayObjectController = argSwitchContentParameters.layoutParameters.displayObjectMappings[0].clearVRDisplayObjectController;
            // We set the current VDOP on the SwitchCOntentParameters, this relies on _clearVRLayoutManager.mainDisplayObjectController being set.
            argSwitchContentParameters.clearVRViewportAndObjectPose = UpdatePoseInfoCustom();

			// Need three steps to assign, check and re-assign.
			AudioTrackAndPlaybackParameters atapp = argSwitchContentParameters.audioTrackAndPlaybackParameters;
			VerifyAudioTrackAndPlaybackParameters(ref atapp);
			argSwitchContentParameters.audioTrackAndPlaybackParameters = atapp;
			return null;
		}


		private ClearVRAsyncRequest VerifySeekRequest(ref SeekParameters argSeekParameters) {
			if(!GetIsInitialized()) {
				return InvokeClearVRCoreWrapperInvalidStateEvent(String.Format("Cannot seek when in {0} state.", GetCurrentStateAsString()), RequestTypes.Seek);
			}
			if(argSeekParameters == null) {
				return InvokeCannotCompleteRequestEvent("Cannot seek. No SeekParameters provided.", RequestTypes.Seek);
			}
			return null;
		}

		/// <summary>
		/// Nifty helper that checks the provided argument. Sets it to default values if need be.
		/// </summary>
		/// <param name="argAudioTrackAndPlaybackParameters">The AudioTrackAndPlaybackParameters to check. Can be null as input, but will never be null upon return.</param>
		internal static void VerifyAudioTrackAndPlaybackParameters(ref AudioTrackAndPlaybackParameters argAudioTrackAndPlaybackParameters) {
			if(argAudioTrackAndPlaybackParameters == null) {
				argAudioTrackAndPlaybackParameters = AudioTrackAndPlaybackParameters.GetDefault();
			} else {
				if(argAudioTrackAndPlaybackParameters.audioDecoder == null) {
					argAudioTrackAndPlaybackParameters.audioDecoder = AudioDecoder.GetDefaultAudioDecoderForPlatform();
				}
				if(argAudioTrackAndPlaybackParameters.audioPlaybackEngine == null) {
					argAudioTrackAndPlaybackParameters.audioPlaybackEngine = AudioPlaybackEngine.GetDefaultAudioPlaybackEngineForPlatform();
				}
			}
		}

        public void CbClearVRCoreWrapperRequestCompleted(ClearVRAsyncRequestResponse argClearVRAsyncRequestResponse, ClearVRMessage argClearVRMessage) {
			ClearVREvent clearVREvent = null;
			bool isSuccess = (argClearVRMessage.result == ClearVRResult.Success);
			switch(argClearVRAsyncRequestResponse.requestType) {
				case RequestTypes.Unknown:
					break;
				case RequestTypes.Initialize:
					// Note that we ONLY set the state in case of success, otherwise our state-model would get desynchronized.
					if(isSuccess) {
						// Only by this point we are *really* initialized
						SetState(States.CorePrepared, argClearVRAsyncRequestResponse, argClearVRMessage);
					} else {
						// This means that we have to forward a custom event that signals that we were unable to initialize our player.
						clearVREvent = new ClearVREvent(ClearVREventTypes.UnableToInitializePlayer, argClearVRAsyncRequestResponse, argClearVRMessage);
					}
					break;
				case RequestTypes.ParseMediaInfo:
					clearVREvent = new ClearVREvent(ClearVREventTypes.MediaInfoParsed, argClearVRAsyncRequestResponse, argClearVRMessage);
					isMediaInfoParsed = isSuccess;
					break;
				case RequestTypes.PrepareContentForPlayout:
					// Note that we ONLY set the state in case of success, otherwise our state-model would get desynchronized.
					if(isSuccess) {
						SetState(States.ContentPreparedForPlayout, argClearVRAsyncRequestResponse, argClearVRMessage);
					} else {
						// This means that we have to forward a custom event that signals that we were unable to initialize our player.
						clearVREvent = new ClearVREvent(ClearVREventTypes.UnableToInitializePlayer, argClearVRAsyncRequestResponse, argClearVRMessage);
					}
					break;
				case RequestTypes.Start:
					clearVREvent = new ClearVREvent(ClearVREventTypes.GenericMessage, argClearVRAsyncRequestResponse, argClearVRMessage);
					break;
				case RequestTypes.Pause:
					clearVREvent = new ClearVREvent(ClearVREventTypes.GenericMessage, argClearVRAsyncRequestResponse, argClearVRMessage);
					break;
				case RequestTypes.Unpause:
					clearVREvent = new ClearVREvent(ClearVREventTypes.GenericMessage, argClearVRAsyncRequestResponse, argClearVRMessage);
					break;
				case RequestTypes.Seek:
					clearVREvent = new ClearVREvent(ClearVREventTypes.GenericMessage, argClearVRAsyncRequestResponse, argClearVRMessage);
					break;
				case RequestTypes.Stop:
					// End of playback, we are done shutting down the core libraries
					if(isSuccess) {
						// all is well
					} else {
						// Note that we internally take care to handle clean-up correctly.
						// This merely signals that something unexpectedly went wrong.
						if(argClearVRMessage.GetIsFatalError()) {
							UnityEngine.Debug.LogError("[ClearVR] A fatal error was reported while shutting down, but it is safe to continue.");
						} else {
							// Harmless warning.
						}
					}
					// Clean-up last data
					CleanUpAfterStopped();
					// Notice how we relay the response to a request through a state change rather than a dedicated event.
					SetState(States.Stopped, argClearVRAsyncRequestResponse, argClearVRMessage);

					break;
				case RequestTypes.SwitchAudioTrack:
					clearVREvent = new ClearVREvent(ClearVREventTypes.AudioTrackSwitched, argClearVRAsyncRequestResponse, argClearVRMessage);
					break;
				case RequestTypes.SwitchContent:
					clearVREvent = new ClearVREvent(ClearVREventTypes.ContentSwitched, argClearVRAsyncRequestResponse, argClearVRMessage);
					break;
				case RequestTypes.ChangeStereoMode:
					clearVREvent = new ClearVREvent(ClearVREventTypes.StereoModeSwitched, argClearVRAsyncRequestResponse, argClearVRMessage);
					break;
				case RequestTypes.CallCore:
					clearVREvent = new ClearVREvent(ClearVREventTypes.CallCoreCompleted, argClearVRAsyncRequestResponse, argClearVRMessage);
					break;
				default:
					break;
			}
			if(clearVREvent != null) {
				ScheduleClearVREvent(clearVREvent);
			}
		}

		[Obsolete("This API has been deprecated in v9.0 and will be removed after 2023-01-01. Please implement the required logic yourself, refer to the source code for details.", false)]
		public void GetRecommendedZoomRange(out float argMin, out float argMax) {
			bool isNonZeroValueSet = false;
			if(_clearVRLayoutManager != null && _clearVRLayoutManager.mainDisplayObjectController != null) {
				ClearVRLegacyDisplayObjectSupport ldos = _clearVRLayoutManager.mainDisplayObjectController.GetComponent<ClearVRLegacyDisplayObjectSupport>();
				if(ldos != null) {
					ldos.GetRecommendedZoomRange(out argMin, out argMax, _platformOptions);
					isNonZeroValueSet = true;
				} else {
					argMin = 0.0f;
					argMax = 0.0f;
				}
			} else {
				argMin = 0.0f;
				argMax = 0.0f;
			}
			if(!isNonZeroValueSet) {
				UnityEngine.Debug.Log("[ClearVR] The mediaPlayer.GetRecommendedZoomRange() API is deprecated and only works when the LayoutManager is running in Legacy mode. No Legacy DisplayObject was found in the scene (which is good, as legacy mode will be removed after 2023-01-01) but you are still calling this API (which is no longer supported). Please implement the GetRecommendedZoomRange logic yourself. You can refer to the source code for inspiration");
				argMin = 0.0f;
				argMax = 0.0f;
			}
		}

		// This API has been deprecated in v9.x
		public void ResetViewportAndDisplayObjectToDefaultPoses() {
			// NOOP
		}

		public void CbClearVRCoreWrapperMessage(ClearVRMessage argClearVRMessage) {
			ClearVREvent clearVREvent = new ClearVREvent(ClearVREventTypes.GenericMessage, argClearVRMessage);
			bool isSuccess = (argClearVRMessage.result == ClearVRResult.Success);
			switch(clearVREvent.message.type) {
				case ClearVRMessageTypes.FatalError:
					break;
				case ClearVRMessageTypes.Warning:
					break;
				case ClearVRMessageTypes.Info:
                    // First, see if this is a state changed message. These messages need special handling.
                    switch (argClearVRMessage.code) {
                        case (int)ClearVRMessageCodes.ClearVRCoreWrapperClearVRCoreStateUninitialized:
                        case (int)ClearVRMessageCodes.ClearVRCoreWrapperClearVRCoreStateInitializing:
                        case (int)ClearVRMessageCodes.ClearVRCoreWrapperClearVRCoreStateInitialized:
                        case (int)ClearVRMessageCodes.ClearVRCoreWrapperClearVRCoreStateRunning:
                        case (int)ClearVRMessageCodes.ClearVRCoreWrapperClearVRCoreStatePausing:
                        case (int)ClearVRMessageCodes.ClearVRCoreWrapperClearVRCoreStatePaused:
                        case (int)ClearVRMessageCodes.ClearVRCoreWrapperClearVRCoreStateBuffering:
                        case (int)ClearVRMessageCodes.ClearVRCoreWrapperClearVRCoreStateSeeking:
                        case (int)ClearVRMessageCodes.ClearVRCoreWrapperClearVRCoreStateSwitchingContent:
                        case (int)ClearVRMessageCodes.ClearVRCoreWrapperClearVRCoreStateFinished:
                        case (int)ClearVRMessageCodes.ClearVRCoreWrapperClearVRCoreStateStopped:
                            ClearVRCoreWrapperStateChanged(argClearVRMessage);
                            return; // make sure we do not emit an event, that is being taken care of by ClearVRCoreWrapperStateChanged()
                        default:
                            break;
                    }

                    /* Parse harmless info messages */
                    switch (argClearVRMessage.code) {
						case (int) ClearVRMessageCodes.ClearVRCoreWrapperGenericInfo:
							break;
						case (int) ClearVRMessageCodes.ClearVRCorWrapperOpenGLVersionInfo:
							break;
						case (int) ClearVRMessageCodes.ClearVRCoreWrapperVideoDecoderCapabilities:
							break;
						case (int) ClearVRMessageCodes.ClearVRCoreWrapperGenericOK:
							break;
						case (int) ClearVRMessageCodes.ClearVRCoreWrapperAudioTrackChanged:
							if(isSuccess) {
								/* Update currently active audio decoder and audio playback engine */
								AudioTrackAndPlaybackParameters atapp;
								if(argClearVRMessage.ParseAudioTrackChanged(out atapp)) {
									if(atapp != null) { // always true, but better be safe than sorry.
										this.audioTrackAndPlaybackParameters = atapp;
									}
								}
							}
							break;
						case (int) ClearVRMessageCodes.ClearVRCoreWrapperStereoscopicModeChanged:
							// This involves rendering specific operations (a call to SetRenderMode()) which needs to be performed from the Unity main thread.
							// ClearVRPlayer will take care of this.
							break;
						case (int) ClearVRMessageCodes.ClearVRCoreWrapperActiveTracksChanged:
							// ActiveTracksChanged enjoys the perks of having its own dedicated ClearVREventType.
							clearVREvent = new ClearVREvent(ClearVREventTypes.ActiveTracksChanged, argClearVRMessage);
							break;
						case (int) ClearVRMessageCodes.ClearVRCoreWrapperAudioFocusGained:
						case (int) ClearVRMessageCodes.ClearVRCoreWrapperAudioFocusLost:
							clearVREvent = new ClearVREvent(ClearVREventTypes.AudioFocusChanged, argClearVRMessage);
							break;
						default:
							break;
					}
				break;
			}

			ScheduleClearVREvent(clearVREvent);
		}

		public void ClearVRCoreWrapperStateChanged(ClearVRMessage argClearVRMessage) {
			States newState = States.Undefined;
			/* Please note that these are ClearVRCore states, there are more events in this Unity SDK which are NOT handled by this callback. */
			switch (argClearVRMessage.code) {
				/* These state changes in the Core have their own dedicated callback handlers and MUST be ignored here */
				case (int)ClearVRMessageCodes.ClearVRCoreWrapperClearVRCoreStateUninitialized:
				case (int)ClearVRMessageCodes.ClearVRCoreWrapperClearVRCoreStateInitializing:
				case (int)ClearVRMessageCodes.ClearVRCoreWrapperClearVRCoreStateInitialized:
				case (int)ClearVRMessageCodes.ClearVRCoreWrapperClearVRCoreStateStopped:
					return;
				/* These _state changes in the Core MUST be handled here */
				case (int)ClearVRMessageCodes.ClearVRCoreWrapperClearVRCoreStateRunning:
					newState = States.Playing;
					break;
				case (int)ClearVRMessageCodes.ClearVRCoreWrapperClearVRCoreStatePausing:
					newState = States.Pausing;
					break;
				case (int)ClearVRMessageCodes.ClearVRCoreWrapperClearVRCoreStatePaused:
					newState = States.Paused;
					break;
				case (int)ClearVRMessageCodes.ClearVRCoreWrapperClearVRCoreStateBuffering:
					newState = States.Buffering;
					break;
				case (int)ClearVRMessageCodes.ClearVRCoreWrapperClearVRCoreStateSeeking:
					newState = States.Seeking;
					break;
				case (int)ClearVRMessageCodes.ClearVRCoreWrapperClearVRCoreStateSwitchingContent:
					newState = States.SwitchingContent;
					break;
				case (int)ClearVRMessageCodes.ClearVRCoreWrapperClearVRCoreStateFinished:
					newState = States.Finished;
					break;
				default:
					// This should've covered all Core States
					return;
			}
			SetState(newState, ClearVRMessage.GetGenericOKMessage());
		}

		public void ScheduleClearVRAsyncRequest(ClearVRAsyncRequest clearVRAsyncRequest, Action<ClearVREvent, ClearVRPlayer> onSuccess, Action<ClearVREvent, ClearVRPlayer> onFailure, params object[] optionalArguments) {
			clearVRAsyncRequest.UpdateActionAndOptionalArguments(onSuccess, onFailure, optionalArguments);
			_clearVRAsyncRequests.Add(clearVRAsyncRequest);
		}

		protected void SetState(States argNewState, ClearVRAsyncRequestResponse argClearVRAsyncRequestResponse, ClearVRMessage argClearVRMessage) {
			ScheduleClearVREvent(new ClearVREvent(ConvertStateToClearVREventType(argNewState), argClearVRAsyncRequestResponse, argClearVRMessage));
		}

		protected void SetState(States argNewState, ClearVRMessage argClearVRMessage) {
			ScheduleClearVREvent(new ClearVREvent(ConvertStateToClearVREventType(argNewState), argClearVRMessage));
		}

		public void UpdateOverrideUserAgent(String argNewUserAgent) {
			if(!String.IsNullOrEmpty(argNewUserAgent)) {
				SetClearVRCoreParameter("advanced.user_agent", argNewUserAgent);
			} // else: ignore error silently
		}

		// We have a temporary fallback API call while not all platforms have this API implemented yet. When implemented, this method should become abstract.
		public virtual String GetClearVRCoreContentParameter(int argContentID, String argKey) {
			UnityEngine.Debug.LogWarning(String.Format("[ClearVR] Falling back to GetClearVRCoreParameter() API, as GetClearVRCoreContentParameter() API is not implemented on platform: {0}. Please report this issue to Tiledmedia.", _platformOptions.platform));
			return GetClearVRCoreParameter(argKey);
		}

		// We have a temporary fallback API call while not all platforms have this API implemented yet. When implemented, this method should become abstract.
		public virtual String GetClearVRCoreContentArrayParameter(int argContentID, String argKey, int argIndex) {
			UnityEngine.Debug.LogWarning(String.Format("[ClearVR] Falling back to GetClearVRCoreArrayParameter() API, as GetClearVRCoreContentArrayParameter() API is not implemented on platform: {0}. Please report this issue to Tiledmedia.", _platformOptions.platform));
			return GetClearVRCoreArrayParameter(argKey, argIndex);
		}

		/* Prepare core */
		public void PrepareCore() {
			// This is the only spot in the code where we are allowed to always pass null for onSuccess and onFailure because:
			// 1. we only call this API internally.
			// 2. we rely on the StateChangedCorePrepared event to continue our flow rather than any callback.
			// 3. This never fails.
			ScheduleClearVRAsyncRequest(_PrepareCore(), null, null);
		}

		/* StartPlayout */
		public void StartPlayout(Action<ClearVREvent, ClearVRPlayer> onSuccess, Action<ClearVREvent, ClearVRPlayer> onFailure, params object[] optionalArguments) {
			ScheduleClearVRAsyncRequest(_StartPlayout(), onSuccess, onFailure, optionalArguments);
		}

		public void StartPlayout(Action<ClearVREvent, ClearVRPlayer> argCbClearVRAsyncRequestResponseReceived, params object[] argOptionalArguments) {
			StartPlayout(argCbClearVRAsyncRequestResponseReceived, argCbClearVRAsyncRequestResponseReceived, argOptionalArguments);
		}

		/* Pause */
		public void Pause(Action<ClearVREvent, ClearVRPlayer> argOnClearVRAsyncRequestSucceded, Action<ClearVREvent, ClearVRPlayer> argOnClearVRAsyncRequestFailed, params object[] argOptionalArguments) {
			ScheduleClearVRAsyncRequest(_Pause(), argOnClearVRAsyncRequestSucceded, argOnClearVRAsyncRequestFailed, argOptionalArguments);
		}
		public void Pause(Action<ClearVREvent, ClearVRPlayer> argCbClearVRAsyncRequestResponseReceived, params object[] argOptionalArguments) {
			Pause(argCbClearVRAsyncRequestResponseReceived, argCbClearVRAsyncRequestResponseReceived, argOptionalArguments);
		}

		/* Unpause */
		public void Unpause(Action<ClearVREvent, ClearVRPlayer> onSuccess, Action<ClearVREvent, ClearVRPlayer> onFailure, params object[] optionalArguments) {
			ScheduleClearVRAsyncRequest(_Unpause(null), onSuccess, onFailure, optionalArguments);
		}

		public void Unpause(TimingParameters argTimingParameters, Action<ClearVREvent, ClearVRPlayer> onSuccess, Action<ClearVREvent, ClearVRPlayer> onFailure, params object[] optionalArguments) {
			ScheduleClearVRAsyncRequest(_Unpause(argTimingParameters), onSuccess, onFailure, optionalArguments);
		}

		public void Unpause(Action<ClearVREvent, ClearVRPlayer> argCbClearVRAsyncRequestResponseReceived, params object[] argOptionalArguments) {
			Unpause(argCbClearVRAsyncRequestResponseReceived, argCbClearVRAsyncRequestResponseReceived, argOptionalArguments);
		}

		/* Toggle Pause/Unpause */
		public void TogglePause(Action<ClearVREvent, ClearVRPlayer> onSuccess, Action<ClearVREvent, ClearVRPlayer> onFailure, params object[] optionalArguments) {
            if(_state == States.Paused) {
                Unpause(onSuccess, onFailure);
            } else if(_state == States.Playing || _state == States.Pausing) {
                Pause(onSuccess, onFailure);
            }
		}
		public void TogglePause(Action<ClearVREvent, ClearVRPlayer> argCbClearVRAsyncRequestResponseReceived, params object[] argOptionalArguments) {
			TogglePause(argCbClearVRAsyncRequestResponseReceived, argCbClearVRAsyncRequestResponseReceived, argOptionalArguments);
		}

		/* Populate media info*/
		public void PopulateMediaInfo(PopulateMediaInfoParameters populateMediaInfoParameters, Action<ClearVREvent, ClearVRPlayer> onSuccess, Action<ClearVREvent, ClearVRPlayer> onFailure, params object[] optionalArguments) {
			ScheduleClearVRAsyncRequest(_PopulateMediaInfo(populateMediaInfoParameters), onSuccess, onSuccess, optionalArguments);
		}

		public void PopulateMediaInfo(PopulateMediaInfoParameters argPopulateMediaInfoParameters, Action<ClearVREvent, ClearVRPlayer> argCbClearVRAsyncRequestResponseReceived, params object[] argOptionalArguments) {
			PopulateMediaInfo(argPopulateMediaInfoParameters, argCbClearVRAsyncRequestResponseReceived, argCbClearVRAsyncRequestResponseReceived, argOptionalArguments);
		}

		/* Set stereo mode */
		public ClearVRAsyncRequest SetStereoMode(bool argStereo) {
			return InvokeAPIObsoleteEvent("The SetStereoMode(bool) API is obsolete. Please use SetStereoMode(bool, Action, Action, params) instead.", RequestTypes.ChangeStereoMode, false);
		}

		public void SetStereoMode(bool stereo, Action<ClearVREvent, ClearVRPlayer> onSuccess, Action<ClearVREvent, ClearVRPlayer> onFailure, params object[] optionalArguments) {
			ScheduleClearVRAsyncRequest(_SetStereoMode(stereo), onSuccess, onFailure,  optionalArguments);
		}
		public void SetStereoMode(bool argStereo, Action<ClearVREvent, ClearVRPlayer> argCbClearVRAsyncRequestResponseReceived, params object[] argOptionalArguments) {
			SetStereoMode(argStereo, argCbClearVRAsyncRequestResponseReceived, argCbClearVRAsyncRequestResponseReceived, argOptionalArguments);
		}

		/* Prewarm cache (obsolete) */
		public void PrewarmCache(PrewarmCacheParameters argPrewarmCacheParameter, Action<ClearVREvent, ClearVRPlayer> argCbClearVRAsyncRequestResponseReceived, params object[] argOptionalArguments) {
			PrewarmCache(argPrewarmCacheParameter, argCbClearVRAsyncRequestResponseReceived, argCbClearVRAsyncRequestResponseReceived, argOptionalArguments);
		}

		/* Set audio track */
		// Deprecated since 2022-05-01
		public void SetAudioTrack(AudioTrackAndPlaybackParameters audioTrackAndPlaybackParameters, Action<ClearVREvent, ClearVRPlayer> onSuccess, Action<ClearVREvent, ClearVRPlayer> onFailure, params object[] optionalArguments) {
		}

		// Deprecated since 2022-05-01
		public void SetAudioTrack(AudioTrackAndPlaybackParameters argAudioTrackAndPlaybackParameters, Action<ClearVREvent, ClearVRPlayer> argCbClearVRAsyncRequestResponseReceived, params object[] argOptionalArguments) {
		}

		/* Switch content */
		public void SwitchContent(SwitchContentParameters switchContentParameters, Action<ClearVREvent, ClearVRPlayer> onSuccess, Action<ClearVREvent, ClearVRPlayer> onFailure, params object[] optionalArguments) {
            // We clone the SwitchContentParameters for use as we are going to modify the VDOP potentially
            SwitchContentParameters workingCopy = switchContentParameters.Copy();
			ClearVRAsyncRequest request = VerifySwitchContentRequest(ref workingCopy);
			if(request != null) {
				ScheduleClearVRAsyncRequest(request, onSuccess, onFailure, optionalArguments);
				return;
			}
			ScheduleClearVRAsyncRequest(_SwitchContent(workingCopy), onSuccess, onFailure, optionalArguments);
		}
        public void SwitchContent(SwitchContentParameters argSwitchContentParameters, Action<ClearVREvent, ClearVRPlayer> argCbClearVRAsyncRequestResponseReceived, params object[] argOptionalArguments) {
           SwitchContent(argSwitchContentParameters, argCbClearVRAsyncRequestResponseReceived, argCbClearVRAsyncRequestResponseReceived, argOptionalArguments);
		}

        /* Set feeds layout */
		public void SetLayout(LayoutParameters newLayoutParameters, Action<ClearVREvent, ClearVRPlayer> onSuccess, Action<ClearVREvent, ClearVRPlayer> onFailure, params object[] optionalArguments) {
			var action = new Action<ClearVREvent, ClearVRPlayer>((ClearVREvent argClearVREvent, ClearVRPlayer argClearVRPlayer2) => {
				CallCoreResponse callCoreResponse;
                CallCoreResponse.ParseCallCoreResponse(argClearVREvent.message.message, out callCoreResponse);
                ClearVRMessageTypes messageType = ClearVRMessageTypes.Info;
                ClearVRResult result = ClearVRResult.Success;
				ClearVREvent cvrEvent = null;
                if (callCoreResponse.errorCode != 0) {
                    messageType = ClearVRMessageTypes.Warning;
                    result = ClearVRResult.Failure;
                	cvrEvent = new ClearVREvent(ClearVREventTypes.SetLayoutCompleted, messageType, callCoreResponse.errorCode, callCoreResponse.errorMessage, result);
					if(onFailure != null) {
						onFailure(cvrEvent, argClearVRPlayer2);
					}
                } else {
					messageType = ClearVRMessageTypes.Info;
					result = ClearVRResult.Success;
					cvrEvent = new ClearVREvent(ClearVREventTypes.SetLayoutCompleted, messageType, callCoreResponse.errorCode, callCoreResponse.errorMessage, result);
					if(onSuccess != null) {
						onSuccess(cvrEvent, argClearVRPlayer2);
					}
				}
            });
			// This ensures that the LayoutParameters are properly set-up and valid (e.g. all DOs have a DOID).
			ClearVRAsyncRequest clearVRAsyncRequest = AddOrUpdateAndVerifyLayoutParametersOnLayoutManager(newLayoutParameters, RequestTypes.CallCore, false);
			if(clearVRAsyncRequest != null) {
				ScheduleClearVRAsyncRequest(clearVRAsyncRequest, onSuccess, onFailure, optionalArguments);
				return;
			}
			ScheduleClearVRAsyncRequest(_CallCore(newLayoutParameters.ToCallCoreCoreProtobuf().ToByteArray()), action, action, optionalArguments);
		}

		public void SetPlaybackRate(float value) {
			// Create PlaybackRate request and configure the values.
			cvrinterface.ChangePlaybackRateRequest changePlaybackRateRequest = new cvri.ChangePlaybackRateRequest() {
				PlaybackRate = value
			};
			// Create callCoreRequest and set the type.
			cvrinterface.CallCoreRequest callCoreRequest = new cvri.CallCoreRequest() {
				CallCoreRequestType = cvri.CallCoreRequestType.ChangePlaybackRate,
				ChangePlaybackRateRequest = changePlaybackRateRequest
			};

			// Create empty action.
			var action = new Action<ClearVREvent, ClearVRPlayer>((ClearVREvent argClearVREvent, ClearVRPlayer argClearVRPlayer2) => {
				CallCoreResponse callCoreResponse;
                CallCoreResponse.ParseCallCoreResponse(argClearVREvent.message.message, out callCoreResponse);
            });

			// Fire and forget
			ScheduleClearVRAsyncRequest(_CallCore(callCoreRequest.ToByteArray()), action, action, null);
		}

		public float GetPlaybackRate() {
			float result = 0.0f;
			string param = GetClearVRCoreParameter("playback.playback_rate");
			if (param == "") {
				return result;
			}

			float.TryParse(param, out result);
			return result;
		}

		/// <summary>
		/// Private API that updates/adds the provided LayoutParameters to the LayoutManager and verifies them for correctness.
		/// </summary>
		/// <param name="argNewLayoutParameters">The LayoutParameters that need to be updated or added to the LayoutManager</param>
		/// <returns>Null in case everything went OK, a ClearVRAsyncRequest otherwise. The callee is resposible for scheduling the request (using `ScheduleClearVRAsyncRequest()`)</returns>
		private ClearVRAsyncRequest AddOrUpdateAndVerifyLayoutParametersOnLayoutManager(LayoutParameters argNewLayoutParameters, RequestTypes argRequestType, bool argIsFatal) {
			if(!_clearVRLayoutManager.AddOrUpdateAndVerifyLayoutParameters(argNewLayoutParameters)) {
				return InvokeCannotCompleteRequestEvent(String.Format("Unable to validate LayoutParameters. Cannot perform request. LayoutParameters: {0}", argNewLayoutParameters), argRequestType, argIsFatal);
			}
#if UNITY_ANDROID && !UNITY_EDITOR
			// We need one additional check here as sprite meshes are only supported in TextureBlitModes.UVShufflingCopy on Android
			if(argNewLayoutParameters != null) {
				if(argNewLayoutParameters.GetIsAnyDisplayObjectControllerSprite()) {
					if(_platformOptions.textureBlitMode != TextureBlitModes.UVShufflingCopy) {
						return InvokeCannotCompleteRequestEvent(String.Format("ClearVRDisplayObjectControllerSprite found on LayoutParameters, but player was started with platformOptions.textureBlitMode = {0}. Sprites are only supported in TextureBlitModes.UVShufflingCopy. This is required. LayoutParameters: {1}", _platformOptions.textureBlitMode, argNewLayoutParameters), argRequestType, argIsFatal);
					}
				}
			}
#endif
			return null;
		}

		/* Prepare content for playout */
		public void PrepareContentForPlayout(PrepareContentParameters prepareContentParameters, Action<ClearVREvent, ClearVRPlayer> onSuccess, Action<ClearVREvent, ClearVRPlayer> onFailure, params object[] optionalArguments) {
			// We clone the PrepareContentParameters for use as we are going to modify the VDOP potentially
			PrepareContentParameters workingCopy = prepareContentParameters.Copy();
			ClearVRAsyncRequest request = VerifyPrepareContentForPlayoutRequest(ref workingCopy);
			if(request != null) {
				ScheduleClearVRAsyncRequest(request, onSuccess, onFailure, optionalArguments);
				return;
			}
			ScheduleClearVRAsyncRequest(_PrepareContentForPlayout(workingCopy), onSuccess, onFailure, optionalArguments);
		}

		public void PrepareContentForPlayout(PrepareContentParameters argPrepareContentParameters, Action<ClearVREvent, ClearVRPlayer> argCbClearVRAsyncRequestResponseReceived, params object[] argOptionalArguments) {
			PrepareContentForPlayout(argPrepareContentParameters, argCbClearVRAsyncRequestResponseReceived, argCbClearVRAsyncRequestResponseReceived, argOptionalArguments);
		}

		/* Seek */
		public void Seek(SeekParameters seekParameters, Action<ClearVREvent, ClearVRPlayer> onSuccess, Action<ClearVREvent, ClearVRPlayer> onFailure, params object[] optionalArguments) {
			ClearVRAsyncRequest request = VerifySeekRequest(ref seekParameters);
			if(request != null) {
				ScheduleClearVRAsyncRequest(request, onSuccess, onFailure, optionalArguments);
				return;
			}
			ScheduleClearVRAsyncRequest(_Seek(seekParameters), onSuccess, onFailure, optionalArguments);
		}
		public void Seek(SeekParameters argSeekParameters, Action<ClearVREvent, ClearVRPlayer> argCbClearVRAsyncRequestResponseReceived, params object[] argOptionalArguments) {
			Seek(argSeekParameters, argCbClearVRAsyncRequestResponseReceived, argCbClearVRAsyncRequestResponseReceived, argOptionalArguments);
		}

		// Deprecated since 2022-05-01
		public string GetHighestQualityResolutionAndFramerate() {
			return "0x0p0";
		}

		// Deprecated since 2022-05-01
		public string GetCurrentResolutionAndFramerate() {
			return "0x0p0";
		}

		// Deprecated since 2022-05-01
        public int GetAudioTrack() {
			return 0;
		}

		// Deprecated since 2022-05-01
        public long GetCurrentContentTimeInMilliseconds() {
			return 0;
		}

		// Deprecated since 2022-05-01
        public long GetCurrentWallclockContentTimeInMilliseconds() {
			return 0;
		}

		// Deprecated since 2022-05-01
        public long GetContentDurationInMilliseconds() {
			return 0;
		}

		/* Stop (obsolete) */
		public ClearVRAsyncRequest Stop() {
			StopInternal();
			return null;
		}

		/* Stop */
		public void StopInternal() {
			Stop(onSuccess: null, onFailure: null);
		}

		public void Stop(Action<ClearVREvent, ClearVRPlayer> onSuccess, Action<ClearVREvent, ClearVRPlayer> onFailure, params object[] optionalArguments) {
			ScheduleClearVRAsyncRequest(_Stop(false), onSuccess, onFailure, optionalArguments);
		}

        public void Stop(Action<ClearVREvent, ClearVRPlayer> argCbClearVRAsyncRequestResponseReceived, params object[] argOptionalArguments) {
			Stop(argCbClearVRAsyncRequestResponseReceived, argCbClearVRAsyncRequestResponseReceived, argOptionalArguments);
		}

		public TimingReport GetTimingReport(System.Object argSeekFlag) {
			// Dummy implementation to satisfy interface definition
			return new TimingReport(new ClearVRMessage(ClearVRMessageTypes.Warning, ClearVRMessageCodes.APIObsolete, API_OBSOLETE_MESSAGE, ClearVRResult.Failure), TimingTypes.ContentTime, 0, 0, 0, 0, EventTypes.Unknown);
		}

		public TimingReport GetTimingReport(TimingTypes argTimingType) {
			return _GetTimingReport(argTimingType);
		}

		/// <summary>
		/// Base implementation, returns ClearVRAsyncRequest(RequestTypes.Unknown) in case of SUCCESS, null OR InvalidState Event (with ClearVRAsyncRequest(RequestTypes.Stop)) in case of failure.
		/// </summary>
		/// <param name="argForceLastMinuteCleanUpAfterPanic"></param>
		/// <returns></returns>
		public virtual ClearVRAsyncRequest _Stop(bool argForceLastMinuteCleanUpAfterPanic) {
			if(isPlayerShuttingDown) {
				// already stopping/shutting down
				return InvokeClearVRCoreWrapperInvalidStateEvent("Unable to stop, already stopping.", RequestTypes.Stop);
			}
			isPlayerShuttingDown = true;
			// Note: typically, we do not allow platform specific code in the MediaPlayerBase. For Stop, we make a one-time exception to significantly reduce code duplication.
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
			// On PC we have to destroy out mesh(es) BEFORE we Destroy our NRP.
            // Otherwise, the texture would still be linked to a Unity GameObject while we try to destroy it in the NRP.
            // Surprisingly, this holds for Direct3D11 and OpenGL
			// TODO-V9
            // if (_clearVRMeshManager != null) {
            //     _clearVRMeshManager.Stop();
            // }
#endif
			if(argForceLastMinuteCleanUpAfterPanic) {
				// This is a last minute clean-up, forced after the core and/or wrapper paniced. There is no need to call Wrapper.Stop() again
				return null; // Only in this case, we are allowed to return null
			}
			SetState(States.Stopping, ClearVRMessage.GetGenericOKMessage());
			return new ClearVRAsyncRequest(RequestTypes.Unknown);

		}

        protected ClearVREventTypes ConvertStateToClearVREventType(States argNewState) {
			_state = argNewState;
			ClearVREventTypes clearVREventType = ClearVREventTypes.None;
			switch(argNewState) {
				case States.Undefined:
					break;
				case States.Uninitialized:
					clearVREventType = ClearVREventTypes.StateChangedUninitialized;
					break;
				case States.Initializing:
					clearVREventType = ClearVREventTypes.StateChangedInitializing;
					break;
				case States.Initialized:
					clearVREventType = ClearVREventTypes.StateChangedInitialized;
					break;
				case States.PreparingCore:
					clearVREventType = ClearVREventTypes.StateChangedPreparingCore;
					break;
				case States.CorePrepared:
					if(_platformOptions.abrStartMode != ABRStartModes.Default) {
						SetClearVRCoreParameter("advanced.abr.start_mode", _platformOptions.abrStartMode.GetStringValue());
					}
					SetClearVRCoreParameter("advanced.abr.enable", _platformOptions.enableABR.ToString());
					clearVREventType = ClearVREventTypes.StateChangedCorePrepared;
					break;
				case States.PreparingContentForPlayout:
					hasReachedPreparingContentForPlayoutState = true; // triggers custom setter
					clearVREventType = ClearVREventTypes.StateChangedPreparingContentForPlayout;
					break;
				case States.ContentPreparedForPlayout:
					clearVREventType = ClearVREventTypes.StateChangedContentPreparedForPlayout;
					break;
				case States.Buffering:
					clearVREventType = ClearVREventTypes.StateChangedBuffering;
					break;
				case States.Playing:
					clearVREventType = ClearVREventTypes.StateChangedPlaying;
					break;
				case States.Pausing:
					clearVREventType = ClearVREventTypes.StateChangedPausing;
					break;
				case States.Paused:
					clearVREventType = ClearVREventTypes.StateChangedPaused;
					break;
				case States.Seeking:
					clearVREventType = ClearVREventTypes.StateChangedSeeking;
					break;
				case States.SwitchingContent:
					clearVREventType = ClearVREventTypes.StateChangedSwitchingContent;
					break;
				case States.Finished:
					clearVREventType = ClearVREventTypes.StateChangedFinished;
					break;
				case States.Stopping:
					hasReachedPreparingContentForPlayoutState = false; // triggers custom setter
					clearVREventType = ClearVREventTypes.StateChangedStopping;
					break;
				case States.Stopped:
					clearVREventType = ClearVREventTypes.StateChangedStopped;
					break;
			}
			if(clearVREventType.Equals(ClearVREventTypes.None))
				throw new Exception(String.Format("[ClearVR] state -> ClearVREvent conversion not implemented for state {0}.", argNewState));
			return clearVREventType;
		}

		public void ScheduleClearVREvent(ClearVREvent argClearVREvent) {
			lock (clearVREventToInvokeLinkedList) {
				clearVREventToInvokeLinkedList.AddLast(argClearVREvent);
			}
		}
		public void ScheduleClearVREventFirst(ClearVREvent argClearVREvent) {
			lock (clearVREventToInvokeLinkedList) {
				clearVREventToInvokeLinkedList.AddFirst(argClearVREvent);
			}
		}

		protected void InvokeEvent(ClearVREvent argClearVREvent) {
			_clearVREvents.Invoke(this, argClearVREvent);
		}

		// MediaInfo interface, deprecated

		public int GetNumberOfAudioTracks() {
			return 0;
		}


		/* SyncInterface */
		public void EnableSync(SyncSettings syncSettings,Action<ClearVREvent, ClearVRPlayer> onSuccess, Action<ClearVREvent, ClearVRPlayer> onFailure, params object[] optionalArguments) {
			var request = new cvri.CallCoreRequest() {
				CallCoreRequestType = cvri::CallCoreRequestType.EnableSync,
				SyncSettings = syncSettings.ToCoreProtobuf(),
			};
			ScheduleClearVRAsyncRequest(_CallCore(request.ToByteArray()), onSuccess, onFailure, optionalArguments);
		}
        public void EnableSync(SyncSettings argSyncSettings, Action<ClearVREvent, ClearVRPlayer> argCbClearVRAsyncRequestResponseReceived, params object[] argOptionalArguments) {
			EnableSync(argSyncSettings, argCbClearVRAsyncRequestResponseReceived, argCbClearVRAsyncRequestResponseReceived, argOptionalArguments);
		}

		public void DisableSync(Action<ClearVREvent, ClearVRPlayer> onSuccess, Action<ClearVREvent, ClearVRPlayer> onFailure, params object[] optionalArguments) {
			var request = new cvri.CallCoreRequest() {
				CallCoreRequestType = cvri::CallCoreRequestType.DisableSync,
			};
			ScheduleClearVRAsyncRequest(_CallCore(request.ToByteArray()), onSuccess, onFailure, optionalArguments);
		}
		public void DisableSync(Action<ClearVREvent, ClearVRPlayer> argCbClearVRAsyncRequestResponseReceived, params object[] argOptionalArguments) {
			DisableSync(argCbClearVRAsyncRequestResponseReceived, argCbClearVRAsyncRequestResponseReceived, argOptionalArguments);
		}

		public void PollSyncStatus(Action<SyncStatus, ClearVRPlayer, object[]> onSuccess, Action<ClearVRMessage, ClearVRPlayer, object[]> onFailure, params object[] optionalArguments) {
			var action = new System.Action<ClearVREvent, ClearVRPlayer>((ClearVREvent argClearVREvent, ClearVRPlayer argClearVRPlayer2) => {
				SyncStatus syncStatus;
                if (SyncStatus.ParseCallCoreResponseToSyncStatus(argClearVREvent.message.message, out syncStatus)) {
                    syncStatus.optionalArguments = optionalArguments;
                    onSuccess.Invoke(syncStatus, argClearVRPlayer2, optionalArguments);
                } else {
                    onFailure.Invoke(argClearVREvent.message, argClearVRPlayer2, optionalArguments);
                }
            });
			var request = new cvri.CallCoreRequest() {
				CallCoreRequestType = cvri::CallCoreRequestType.PollSyncStatus,
			};
			ScheduleClearVRAsyncRequest(_CallCore(request.ToByteArray()), action, action, optionalArguments);
		}

		public void PollSyncStatus(Action<SyncStatus, ClearVRPlayer> argCbClearVRAsyncRequestResponseReceived, params object[] argOptionalArguments) {
			Action<SyncStatus, ClearVRPlayer, object[]> onSuccess = new Action<SyncStatus, ClearVRPlayer, object[]>((syncStatus, clearVRPlayer, optionalArgs) => {
				argCbClearVRAsyncRequestResponseReceived.Invoke(syncStatus, clearVRPlayer);
			});
			Action<ClearVRMessage, ClearVRPlayer, object[]> onFailure = new System.Action<ClearVRMessage, ClearVRPlayer, object[]>((clearVRMessage, clearVRPlayer, optionalArgs) => {
                argCbClearVRAsyncRequestResponseReceived.Invoke(null, clearVRPlayer);
            });
			PollSyncStatus(onSuccess, onFailure, argOptionalArguments);
		}

		/* DebugInterface */
		// Please do not use them as they can change without notice.
		public void CallCore(byte[] rawMessage, Action<ClearVREvent, ClearVRPlayer> onSuccess, Action<ClearVREvent, ClearVRPlayer> onFailure, params object[] optionalArguments) {
			ScheduleClearVRAsyncRequest(_CallCore(rawMessage), onSuccess, onFailure, optionalArguments);
		}
        public void CallCore(byte[] argRawMessage, Action<ClearVREvent, ClearVRPlayer> argCbClearVRAsyncRequestResponseReceived, params object[] argOptionalArguments) {
			CallCore(argRawMessage, argCbClearVRAsyncRequestResponseReceived, argCbClearVRAsyncRequestResponseReceived, argOptionalArguments);
		}

		public String CallCoreSync(String base64Message) {
			return _CallCoreSync(base64Message);
		}

		public void SwitchABRLevel(bool up, Action<ClearVREvent, ClearVRPlayer> onSuccess, Action<ClearVREvent, ClearVRPlayer> onFailure, params object[] optionalArguments) {
			cvri.SwitchAbrLevelRequest switchABRLevelRequest = new cvri.SwitchAbrLevelRequest();
			switchABRLevelRequest.Up = up;
			var request = new cvri.CallCoreRequest() {
				CallCoreRequestType = cvri::CallCoreRequestType.SwitchAbrLevel,
				SwitchAbrLevelRequest = switchABRLevelRequest
			};
			ScheduleClearVRAsyncRequest(_CallCore(request.ToByteArray()), onSuccess, onFailure, optionalArguments);
		}

		public void SwitchABRLevel(bool argUp, Action<ClearVREvent, ClearVRPlayer> argCbClearVRAsyncRequestResponseReceived, params object[] argOptionalArguments) {
			SwitchABRLevel(argUp, argCbClearVRAsyncRequestResponseReceived, argCbClearVRAsyncRequestResponseReceived, argOptionalArguments);
		}

		public virtual void ForceClearVRCoreCrash(Action<ClearVREvent, ClearVRPlayer> onSuccess, Action<ClearVREvent, ClearVRPlayer> onFailure, params object[] optionalArguments) {
			var request = new cvri.CallCoreRequest() {
				CallCoreRequestType = cvri.CallCoreRequestType.ForceCoreCrash
			};
			ScheduleClearVRAsyncRequest(_CallCore(request.ToByteArray()), onSuccess, onFailure, optionalArguments);
		}

		public void TelemetryUpdateCustomData(TelemetryUpdateCustomData telemetryUpdateCustomData, Action<ClearVREvent, ClearVRPlayer> onSuccess, Action<ClearVREvent, ClearVRPlayer> onFailure, params object[] optionalArguments) {
			var request = new cvri.CallCoreRequest() {
				CallCoreRequestType = cvri.CallCoreRequestType.TelemetryUpdateCustomData
			};
			if(telemetryUpdateCustomData != null) {
				request.TelemetryUpdateCustomDataRequest = telemetryUpdateCustomData.ToCoreProtobuf();
			}
			ScheduleClearVRAsyncRequest(_CallCore(request.ToByteArray()), onSuccess, onFailure, optionalArguments);
		}

		public virtual void ForceClearVRCoreCrash(Action<ClearVREvent, ClearVRPlayer> argCbClearVRAsyncRequestResponseReceived, params object[] argOptionalArguments) {
			ForceClearVRCoreCrash(argCbClearVRAsyncRequestResponseReceived, argCbClearVRAsyncRequestResponseReceived, argOptionalArguments);
		}

		public virtual void Destroy() {
			if(_clearVRCoreWrapperStatistics != null) {
				_clearVRCoreWrapperStatistics.Destroy();
				_clearVRCoreWrapperStatistics = null;
			}
		}
    }
}
