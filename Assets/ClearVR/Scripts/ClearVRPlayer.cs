using System;
using System.Collections;
using System.Collections.Generic;
using com.tiledmedia.clearvr.protobuf;

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace com.tiledmedia.clearvr {

	/// <summary>
	/// Enum specifying the actions that can only be triggered on the main Unity thread.
	/// </summary>
	enum MainThreadActionTypes {
		PrepareCore,
		Play,
		ForceStopAfterFatalError,
		ApplicationOrEditorQuit,
		ResumePlaybackAfterApplicationRegainedFocus
	}

	class MainThreadAction {
		public MainThreadActionTypes type;
		public System.Object payload;
		public int vsyncDelayCount;
		public MainThreadAction(MainThreadActionTypes argMainThreadActionType, System.Object argPayload, int argVSyncDelayCount = 0){
			type = argMainThreadActionType;
			payload = argPayload;
			vsyncDelayCount = argVSyncDelayCount;
		}
	}

	/// <summary>
	/// This helper class attaches a listener for ALT+F4 / click the cross button to close the application events.
	/// When triggered, it briefly postpones application shut down to allow the underlying library to release its claimed resources.
	/// </summary>
#if UNITY_EDITOR
	[InitializeOnLoad]
#endif
	internal static class WantsToQuitInterruptNotifier {
		internal static bool applicationWantsToQuit = false;
		private static System.Func<bool> registeredCallback = null;
		private static System.Threading.Timer delayedReleaseTimer = null;

		static WantsToQuitInterruptNotifier() {
		}

		internal static void RegisterCallback(System.Func<bool> argCallback) {
			MaybeCancelDelayedRelease();
			MaybeUnregisterCallback();
#if UNITY_EDITOR
			UnityEditor.EditorApplication.wantsToQuit += argCallback;
#elif UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX
			UnityEngine.Application.wantsToQuit += argCallback;
#endif
			registeredCallback = argCallback;
		}

		internal static void MaybeUnregisterCallback() {
			if(registeredCallback != null) {
#if UNITY_EDITOR
				UnityEditor.EditorApplication.wantsToQuit -= registeredCallback;
#elif UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX
				UnityEngine.Application.wantsToQuit -= registeredCallback;
#endif
			}
			registeredCallback = null;
		}

		internal static void ScheduleDelayedRelease(System.Func<bool> argCallback, System.Func<bool, bool> argCallbackToTriggerAfterDelay) {
			// For a short while ( approx. 1 second, the editor remains "alive" after we delayed the application quit.
			// Here we trigger a timeout after 500 msec. Note that this timeout is NOT triggered on the main unity thread.
			delayedReleaseTimer = new System.Threading.Timer((obj) => {
					MaybeUnregisterCallback();
					delayedReleaseTimer.Dispose();
					delayedReleaseTimer = null;
					if(applicationWantsToQuit && argCallbackToTriggerAfterDelay != null) {
						argCallbackToTriggerAfterDelay(true);
					}
				},
			null, 500, System.Threading.Timeout.Infinite);
		}

		internal static void MaybeCancelDelayedRelease() {
			if(delayedReleaseTimer != null) {
				// Cancel current/previous timer in a thread safe manner.
				delayedReleaseTimer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
				delayedReleaseTimer = null;
			}
		}
	}

#if UNITY_EDITOR
	[InitializeOnLoad]
	internal static class PlayStateNotifier {
		// use this to control allowing exiting play mode
		internal static bool editorWantsToStop = false;
		private static Action<PlayModeStateChange> registeredCallback = null;
		static PlayStateNotifier() {
		}

		internal static void RegisterCallback(Action<PlayModeStateChange> argCallback) {
			MaybeUnregisterCallback();
			PlayStateNotifier.editorWantsToStop = false;
			registeredCallback = argCallback;
			EditorApplication.playModeStateChanged += argCallback;
		}

		internal static void MaybeUnregisterCallback() {
			if(registeredCallback != null) {
				EditorApplication.playModeStateChanged -= registeredCallback;
				registeredCallback = null;
			}
		}
	}
#endif


#if UNITY_STANDALONE_LINUX || UNITY_STANDALONE_WIN
	/// <summary>
	/// This little MonoBehaviour is attached to a gameobject that is temporarily spawned during application quit.
	/// It listens for any delayed request for quitting the app.
	/// </summary>
	class ApplicationQuitListener : MonoBehaviour {
		private LinkedList<MainThreadAction> _mainThreadActionsLinkedList = new LinkedList<MainThreadAction>();

		internal void PushMainThreadAction(MainThreadAction argMainThreadAction) {
			_mainThreadActionsLinkedList.AddLast(argMainThreadAction);
		}

		void Update() {
			if(_mainThreadActionsLinkedList.Count > 0) {
				while(_mainThreadActionsLinkedList.Count > 0) {
					MainThreadAction mainThreadAction = _mainThreadActionsLinkedList.First.Value;
					if(mainThreadAction.vsyncDelayCount > 0) {
						// The head of the list needs another vsync before it can be executed. We break and wait for the next vsync.
						mainThreadAction.vsyncDelayCount = mainThreadAction.vsyncDelayCount - 1;
						break;
					}
					_mainThreadActionsLinkedList.RemoveFirst();
					switch (mainThreadAction.type) {
						case MainThreadActionTypes.ApplicationOrEditorQuit:
							ClearVRPlayer.TriggerQuit();
							// Kill the parent GameObject as it has served its purpose.
							UnityEngine.Object.Destroy(gameObject);
							break;
						default:
							break;
					}
				}
			}
		}
	}
#endif

	/// <summary>
	/// The ClearVRPlayer class is the primary entry point, also refer to [ClearVRPlayer](~/readme/clearvrplayer.md) for a detailed description.
	/// </summary>
    public class ClearVRPlayer : MonoBehaviour {
		/* Interfaces */
		private MediaPlayerInterface        _mediaPlayerInterface;
		private MediaControllerInterface    _mediaControllerInterface;
		private MediaInfoInterface          _mediaInfoInterface;
		private PerformanceInterface        _performanceInterface;
		private SyncInterface               _syncInterface;
		private DebugInterface              _debugInterface;
		private InternalInterface           _internalInterface; // the internal interface should never be exposed to the application.
		/// <summary>
		/// 0 = not set, 1 = was paused, 2 = was not paused.
		/// </summary>
		private int                         _wasPausedWhenHeadsetWasTakenOffOrFocusWasLost = 0;
		private bool                        _isStoppingStateTriggered = false; // triggered as soon as we hit finalizing state.
		private bool                        _triggerResumingPlaybackAfterApplicationRegainedFocusEvent = false; // set to true during a S/R cycle
		private bool                        _isInitializeCalled = false;
		private bool                        _isGameObjectDestroyed = false; // triggered if this object is destroyed (void OnDestroy() was called). NOT reset in Reset()
		private int                         _appPauseAndFocusState = (int)AppPauseAndFocusState.UnpausedOrPaused ^ (int)AppPauseAndFocusState.FocusOrNoFocus;
		private bool                        _forceAutoPlayAfterContentLoadCompleted = false;
		private bool                        _isFatalErrorHandled = false;
#if UNITY_STANDALONE_LINUX || UNITY_STANDALONE_WIN
		private GameObject                  _applicationQuitListenerGO = null;
		private ApplicationQuitListener     _applicationQuitListenerMB = null;
#endif
		private SuspendResumeState          _suspendResumeState = null;
		private bool                        _wasPausedBeforeAudioFocusLost = false;
		private ContentInfo                 _contentInfo = null;

		private enum AppPauseAndFocusState {
			UnpausedOrPaused = 0x01,
			FocusOrNoFocus = 0x02,
		}
		// internal ClearVREvents event channel
		private ClearVREvents 				_clearVREvents = new ClearVREvents();
		// internal ClearVRDisplayObjectEvents event channel
		private ClearVRDisplayObjectEvents 	_clearVRDisplayObjectEvents = new ClearVRDisplayObjectEvents();

		/// <summary>
		/// The Delegate that needs to be implemented for the loading of the previous state callback.
		/// This delegate has been renamed in v9.1 to [ClearVRApplicationRegainedFocus](xref:com.tiledmedia.clearvr.ClearVRApplicationRegainedFocus(com.tiledmedia.clearvr.ClearVRPlayer.ClearVRApplicationRegainedFocus), which takes ContentInfo as its third argument.
		/// </summary>
		/// <param name="argClearVRPlayer">The ClearVRPlayer that triggered the callback.</param>
		/// <param name="argPlatformOptions">The PlatformOptions of the ClearVRPlayer that are saved in the core.</param>
		[Obsolete("This delegate has been renamed to ClearVRApplicationRegainedFocus.", true)]
		public delegate void ClearVRLoadPreviousStateDelegate(ClearVRPlayer argClearVRPlayer, PlatformOptionsBase argPlatformOptions);
		/// <summary>
		/// The Delegate that needs to be implemented for the loading of the previous state callback.
		/// </summary>
		/// <param name="argClearVRPlayer">The ClearVRPlayer that triggered the callback.</param>
		/// <param name="argPlatformOptions">The PlatformOptions of the ClearVRPlayer that are saved in the core.</param>
		/// <param name="argLastPlayedContentInfo">Since v9.1: Contains the [ContentInfo](xref:com.tiledmedia.clearvr.ContentInfo) of the last played [ContentItem](xref:com.tiledmedia.clearvr.ContentItem). Note that this value _can be null_!</param>
		public delegate void ClearVRApplicationRegainedFocus(ClearVRPlayer argClearVRPlayer, PlatformOptionsBase argPlatformOptions, ContentInfo argLastPlayedContentInfo);
		/// <summary>
		/// This API has been renamed in v9.1 to applicationRegainedFocusDelegate.
		/// </summary>
		[Obsolete("This delegate has been renamed to applicationRegainedFocusDelegate.", true)]
		public ClearVRLoadPreviousStateDelegate clearVRLoadPreviousStateDelegate = null;
		/// <summary>
		/// Normally, you would leave the ClearVRPlayer in full control over what happens during an application suspend/resume cycle (e.g. when your app is backgrounded and brought back to the foreground or a headset was put down and picked up again). This is configured by setting [applicationFocusAndPauseHandling](xref:com.tiledmedia.clearvr.PlatformOptionsBase.applicationFocusAndPauseHandling) to [Recommended](xref:com.tiledmedia.clearvr.ApplicationFocusAndPauseHandlingTypes.Recommended).
		/// The `ClearVRLoadPreviousStateDelegate` allows greater control for your application over this automatic behaviour. When set, this delegate will be triggered _just_ prior to playback resume after your application has resumed.
		/// The `ClearVRPlayer` argument will be your ClearVRPlayer object, and the `PlatformOptions` argument will be equal to its platformOptions (as set by you during initial construction).
		/// In this moment, you can make any alteration to the PlatformOptions (like changing the ContentItem that should start playback). 
		/// When implemented, you will be in charge to call `ClearVRPlayer.Initialize()` again once you are done modifying the PlatformOptions. 
		/// 
		/// This API was known as `clearVRLoadPreviousStateDelegate` in versions prior to v9.1
		/// 
		/// Since v9.1:
		/// Added the `argLastPlayedContentInfo` argument, which contains the [ContentInfo](xref:com.tiledmedia.clearvr.ContentInfo) of the last played [ContentItem](xref:com.tiledmedia.clearvr.ContentItem). Note that this value _can be null_!
		/// 
		/// Example usage:
		/// 
		/// In this example we override the default playback resume logic for LIVE content after an application lost focus/regained focus (was suspended and resumed) cycle. By default, playback would resume from the last known position. With the demonstrated logic, playback will resume from the live edge instead.
		/// <code language="cs"><![CDATA[
		/// // When setting up your platformOptions
		/// clearVRPlayer.applicationRegainedFocusDelegate = new ClearVRPlayer.ClearVRApplicationRegainedFocusDelegate(ApplicationRegainedFocus);
		/// ...
		/// public void ApplicationRegainedFocus(ClearVRPlayer argClearVRPlayer, PlatformOptionsBase argPlatformOptions, ContentInfo argLastPlayedContentInfo) {
		///     // This delegate is triggered on the main unity thread. Do not perform blocking code here as it will freeze your app.
		///     // You cannot interact with the ClearVRPlayer in any way while it is in this state. Be sure to block any (looping) code from accessing the ClearVRPlayer object.
		///     if(argLastPlayedContentInfo != null && argLastPlayedContentInfo.eventType == EventTypes.Live) {
		///         argPlatformOptions.prepareContentParameters.timingParameters = null; // null == start from live edge for live content, from the beginning for VOD.
		///     }
		///     argClearVRPlayer.Initialize(argPlatformOptions,
		///         onSuccess: (cbClearVREvent, cbClearVRPlayer) => {
		///             // Handle success like you would handle failure when you initialized the player
		///         },
		///         onFailure: (cbClearVREvent, cbClearVRPlayer) => {
		///             // Handle failure like you would handle failure when you initialized the player
		///         }
		///     }));
		///  }
		/// ]]></code>
		/// 
		/// > [!NOTE]
		/// > This delegate will only be triggered when ApplicationFocusAndPauseHandlingType is set to Recommended.
		/// > [!NOTE]
		/// > If the application is quickly suspended before playback has even started (e.g. during player initialization), your original `Initialize()` callback will NOT be triggered. Instead, your new `Initialize()` callback will be triggered upon completion. The same holds for any subsequent suspend/resume cycle before content load completed.
		/// > [!NOTE]
		/// > The callback will only be triggered once until you call `Initialize()` again. If the application is suspened before `Initialize()` is called, you will not be notified again.
		/// > [!WARNING]
		/// > This delegate is triggered on the main thread, so any blocking operations (like querying your CMS) should be deferred to another thread. Remember that `ClearVRPlayer.Initialize()` _must_ be called from the main thread as well. Behaviour is undefined when called from any another thread.
		/// > [!WARNING]
		/// > All `ClearVRPlayer` interfaces (like `controller` and `mediaPlayer`) are `null` when this delegate is triggered.
		/// </summary>
		public ClearVRApplicationRegainedFocus applicationRegainedFocusDelegate = null;
		/// <summary>
		/// Configuration of the verbosity level and output configuration of the logging of the Tiledmedia SDK
		/// > [!NOTE]
		/// > The value of this field is fixed from the moment you call clearVRPlayer.Initialize() OR ClearVRPlayer.TestIsContentSupported(), whichever is called first.
		/// > Changing the value of this field afterwards has no effect.
		/// > [!WARNING]
		/// > Take special care in making sure that this value is always the default in release builds as it has a serious negative performance impact.
		/// </summary>
		public static LoggingConfiguration loggingConfig  = new LoggingConfiguration();
		/// <summary>
		/// A delegate of this signature will be triggered when the _application_ is unpaused after it got paused by the OS. Refer to [clearVRApplicationUnpausedDelegate](xref:com.tiledmedia.clearvr.ClearVRPlayer.applicationUnpausedDelegate) for details and how to subscribe to a delegate of this signature.
		/// </summary>
		/// <param name="argClearVRPlayer">The ClearVRPlayer that is about to unpause.</param>
		/// <param name="argPlatformOptions">The platform Options describing the configuration of the ClearVRPlayer. Making changes to this argument is not supported and will result in undefined behaviour.</param>
		/// <param name="argLastPlayedContentInfo">Contains the [ContentInfo](xref:com.tiledmedia.clearvr.ContentInfo) of the last played [ContentItem](xref:com.tiledmedia.clearvr.ContentItem).</param>
		/// <param name="wasPlaybackPausedBeforeApplicationPaused">True if playback was already paused (e.g. by the user) before the application was paused, false if playback was _not_ paused before the application was paused.</param>
		public delegate void ClearVRApplicationUnpausedDelegate(ClearVRPlayer argClearVRPlayer, PlatformOptionsBase argPlatformOptions, ContentInfo argLastPlayedContentInfo, bool wasPlaybackPausedBeforeApplicationPaused);
		/// <summary>
		/// When platformOptions.ApplicationFocusAndPauseHandlingType is set to Recommended, the player will unpause after the appplication was unpaused by the OS (after the application first got paused by the OS, e.g. because the notification drawer was pulled down on a mobile device).
		/// By default, both VOD and LIVE playback will continue from the last known position. Additionally, for LIVE content only, playback will resume from the live edge if the last played video segment fell out of the live window of the live stream by the time the application unpaused.
		/// To override this default behaviour, the application can subscribe to this callback.
		/// 
		/// When you subscribe to this delegate, it is the application's responsibility to take action. Otherwise, playback will be paused indefinitely. Please refer to the [Unpause(TimingParameters)](xref:com.tiledmedia.clearvr.MediaControllerInterface.Unpause(com.tiledmedia.clearvr.TimingParameters,Action{com.tiledmedia.clearvr.ClearVREvent,com.tiledmedia.clearvr.ClearVRPlayer},Action{com.tiledmedia.clearvr.ClearVREvent,com.tiledmedia.clearvr.ClearVRPlayer},System.Object[])) API for details. 
		/// 
		/// Example code:
		/// 
		/// In this example we demonstrate how one makes sure that playback will jump to lthe live edge after the _application_ as paused and unpaused by the OS.
		/// <code language="cs"><![CDATA[
		/// // When setting up your platformOptions
		/// clearVRPlayer.applicationUnpausedDelegate = new ClearVRPlayer.ClearVRApplicationUnpausedDelegate(ApplicationUnpaused);
		/// ...
		/// public void ApplicationUnpaused(ClearVRPlayer argClearVRPlayer, PlatformOptionsBase argPlatformOptions, ContentInfo argLastPlayedContentInfo, bool argWasPlaybackPausedByUser) {
		///   if(!argWasPlaybackPausedByUser) {
		///     TimingParameters tp = null; // We want default unpause behaviour, except for live content.
		///     if(argLastPlayedContentInfo != null && argLastPlayedContentInfo.contentType == ContentTypes.Live) {
		///       tp = new TimingParameters(0, TimingTypes.LiveEdge);
		///     }
		///     if(argClearVRPlayer.controller != null) {
		///       argClearVRPlayer.controller.Unpause(tp, 
		///         onSuccess: (cbClearVREvent, cbClearVRPlayer) =>
		///           UnityEngine.Debug.Log("[ClearVR] Player UNPAUSED CUSTOM."),
		///         onFailure: (cbClearVREvent, cbClearVRPlayer) =>
		///           UnityEngine.Debug.LogWarning(String.Format("[ClearVR] Something went wrong while unpausing the ClearVRPlayer after application was unpaused. Error Code: {0}; Message: {1} .", cbClearVREvent.message.code, cbClearVREvent.message.message))
		///       );
		///      } // else: the player is shutting down, perhaps application pause crossed with the application losing focus. Nothing we can and should do here.
		///   } // else: user explicitly paused playback before the application was paused, let's not unpause for him.
		/// }
		/// ]]></code>
		/// 
		/// > [!NOTE]
		/// > This delegate will only be triggered when ApplicationFocusAndPauseHandlingType is set to Recommended.
		/// > [!WARNING]
		/// > This delegate is triggered on the main thread, so any blocking operations should be deferred to another thread.
		/// </summary>
		public ClearVRApplicationUnpausedDelegate applicationUnpausedDelegate = null;

		/// <summary>
		/// ClearVRCore verbosity level. Please keep at the default value 0 at all times. Valid value: 0, 1, 2. Setting it to any other value will have a negative performance impact.
		/// Refer to ClearVRPlayer.coreLogFile to (optionally) write the core log file to disk.
		/// > [!NOTE]
		/// > The value of this field is fixed from the moment you call clearVRPlayer.Initialize() OR ClearVRPlayer.TestIsContentSupported(), whichever is called first.
		/// > Changing the value of this field afterwards has no effect.
		/// > [!WARNING]
		/// > Take special care in making sure that this value is always 0 in release builds as it has a serious negative performance impact.
		/// </summary>
		[Obsolete("This field has been replaced by loggingConfig.coreLogLevel. This will be removed after 2023-12-31.", false)]
		public static int coreLogLevel {
			get {
				return (int)loggingConfig.globalLogLevel;
			}
			set {
				if(value >= 0 && value <= 2) {
					loggingConfig.globalLogLevel = LogLevelsMethods.FromInt(value);
				} else {
					UnityEngine.Debug.LogWarning(String.Format("[ClearVR] ClearVRPlayer.coreLogLevel set to {0}, which is invalid. Valid range: [0, 2]. Defaulting to 0.", value));
				}
			}
		}

		/// <summary>
		/// Instead of writing to stdout, the core ClearVRCore log will be written to the specified file. Default value: "" (e.g. do not log to disk)
		/// Refer to ClearVRPlayer.coreLogLevel to configure the core log verbosity.
		/// > [!NOTE]
		/// > The value of this field is fixed from the moment you call clearVRPlayer.Initialize() OR ClearVRPlayer.TestIsContentSupported(), whichever is called first.
		/// > Changing the value of this field afterwards has no effect.
		/// </summary>
		[Obsolete("This field has been replaced by loggingConfig.globalLogFileName. This will be removed after 2023-12-31.", false)]
		public static String coreLogFile {
			get {
				return loggingConfig.globalLogFileName;
			}
			set {
				loggingConfig.globalLogFileName = value;
			}
		}


		/// <summary>
		/// ClearVRMediaFlow verbosity level. Please keep at the default value 0 at all times. Valid value: 0, 1, 2, 3, 4, 5. Setting it to any other value will have a negative performance impact.
		/// Refer to ClearVRPlayer.mediaflowLogFile to (optionally) write the mediaflow log file to disk.
		/// > [!NOTE]
		/// > The value of this field is fixed from the moment you call clearVRPlayer.Initialize() OR ClearVRPlayer.TestIsContentSupported(), whichever is called first.
		/// > Changing the value of this field afterwards has no effect.
		/// > [!WARNING]
		/// > Take special care in making sure that this value is always 0 in release builds as it has a serious negative performance impact.
		/// </summary>
		[Obsolete("This field has been replaced by loggingConfig.mfLogLevel. This will be removed after 2023-12-31.", false)]
		public static int mediaflowLogLevel {
			get {
				return ((int)loggingConfig.mfLogLevel + 3);
			}
			set {
				if(value >= 0 && value <= 5) {
					loggingConfig.mfLogLevel = LogLevelsMethods.FromInt(value -3);
				} else {
					UnityEngine.Debug.LogWarning(String.Format("[ClearVR] ClearVRPlayer.mediaflowLogLevel set to {0}, which is invalid. Valid range: [0, 5]. Defaulting to 0.", value));
				}
			}
		}

		/// <summary>
		/// Instead of writing to stdout, the core ClearVRCore log will be written to the specified file. Default value: "" (e.g. do not log to disk)
		/// Refer to ClearVRPlayer.coreLogLevel to configure the core log verbosity.
		/// > [!NOTE]
		/// > The value of this field is fixed from the moment you call clearVRPlayer.Initialize() OR ClearVRPlayer.TestIsContentSupported(), whichever is called first.
		/// > Changing the value of this field afterwards has no effect.
		/// </summary>
		[Obsolete("This field has been replaced by loggingConfig.mfLogFileName. This will be removed after 2023-12-31.", false)]
		public static String mediaflowLogFile {
			get {
				return loggingConfig.mfLogFileName;
			}
			set {
				loggingConfig.mfLogFileName = value;
			}
		}



		internal static void ConvertPersistentLogPathToPlatformDependentLogPath(String argVarName,ref String path) {
		if(!String.IsNullOrEmpty(path)) {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
					if(path.Contains("/storage/emulated/0/")) {
						// Default folder on Android, replace with $HOME folder instead.
						String value = path;
						path = Utils.HOME_FOLDER + System.IO.Path.DirectorySeparatorChar + System.IO.Path.GetFileName(path);
						UnityEngine.Debug.LogWarning(String.Format("[ClearVR] Replacing 'ClearVRPlayer.{0}' = '{1}' with '{2}' as you are running on the PC platform, not Android. Don't forget to change this value yourself!",argVarName, value, path));
					}
					String folderPath = System.IO.Path.GetDirectoryName(path);
					if(System.IO.Directory.Exists(folderPath)) {
						if(Utils.IsDirectoryWritable(folderPath)) {
							// Pass silently, this is OK!
						} else {
							UnityEngine.Debug.LogError(String.Format("[ClearVR] Unable to write to 'ClearVRPlayer.{0}' = '{1}'. No write permissions?",argVarName, path));
							path = "";
						}
					} else {
						UnityEngine.Debug.LogError(String.Format("[ClearVR] Unable to write to 'ClearVRPlayer.{0}' = '{1}'. Target folder does not exist.",argVarName, path));
						path = "";
					}
#elif UNITY_IOS || UNITY_TVOS
					if(path.Contains("/storage/emulated/0/")) {
						String value = path;
						// Default folder on Android, which is probably not intended on iOS as it does not exist.
						path = Application.persistentDataPath + System.IO.Path.DirectorySeparatorChar + System.IO.Path.GetFileName(path);
						UnityEngine.Debug.LogWarning(String.Format("[ClearVR] Replacing 'ClearVRPlayer.{0}' = '{1}' with '{2}' as you are running on the iOS platform, not Android. Don't forget to change this value yourself!",argVarName, value, path));
					}
#endif
#if UNITY_ANDROID && !UNITY_EDITOR
					if(path.Contains("/storage/emulated/0/")) {
						if(!Utils.AndroidVersion.CanHaveFreeAccessToSDCard()) {
							// We are on API 29+, so we cannot write to the SD card directly.
							path = Application.persistentDataPath + System.IO.Path.DirectorySeparatorChar + System.IO.Path.GetFileName(path);
						}
					}
#endif
				} //else: logging to disk not required, safe fallthrough
		}
        internal static readonly SDKTypes SDK_TYPE = SDKTypes.Unity;
		/* A list that allows us to schedule actions on the main Unity thread. */
		private LinkedList<MainThreadAction> _mainThreadActionsLinkedList = new LinkedList<MainThreadAction>();

		/// <summary>
		/// Returns the default [PlatformOptionsBase](xref:com.tiledmedia.clearvr.PlatformOptionsBase) for the current platform.
		/// </summary>
		/// <returns></returns>
		public PlatformOptionsBase GetDefaultPlatformOptions() {
#if UNITY_ANDROID && !UNITY_EDITOR
			return new PlatformOptionsAndroid();
#elif UNITY_IOS || UNITY_TVOS && !UNITY_EDITOR
            return new PlatformOptionsIOS();
#elif UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
            return new PlatformOptionsPC();
#else
            return new PlatformOptionsBase();
#endif
        }

		/// <summary>
		/// Getter for the currently configured platform options.
		/// > [!WARNING]
		/// > One must never change any value on the platformOptions after the ClearVRPlayer object has been initialized. Doing so will result in undefined behaviour.
		/// </summary>
		/// <value></value>
        public PlatformOptionsBase platformOptions {
			get{
				if(_mediaPlayerInterface != null) {
					return _mediaPlayerInterface.GetPlatformOptions();
				}
				return null;
			}
		}

		/// <summary>
		/// Change the RenderMode of the main ClearVRDisplayObject.
		/// </summary>
		/// <value></value>
		[Obsolete("This API has been removed in v9.x. To change the render mode of a specific ClearVRDisplayObject, call ClearVRDisplayObjectControllerBase.SetRenderMode(RenderModes) instead. Also, refer to ClearVRLayoutManager.SetRenderModeOnAllDisplayObjects(RenderModes)", true)] // Deprecared on 2022-06-27
		public RenderModes renderMode {
			get {
				if(_mediaPlayerInterface != null) {
					return _mediaPlayerInterface.GetRenderMode();
				} else {
					return RenderModes.Native;
				}
			}
			set {
				if(_mediaPlayerInterface != null) {
					_mediaPlayerInterface.SetRenderMode(value);
				}
			}
		}

		/// <summary>
		/// This interface can be used to access performance-related metrics.
		/// </summary>
		/// <value>The performance interface</value>
		public PerformanceInterface performance {
			get { return _performanceInterface; }
		}

		/// <summary>
		/// This interface can be used to access media related information (like content duration and format)
		/// </summary>
		/// <value>The mediainfo interface</value>
		public MediaInfoInterface mediaInfo {
			get { return _mediaInfoInterface; }
		}

		/// <summary>
		/// This interface can be used to control the media player.
		/// </summary>
		/// <value>The controller interface</value>
		public MediaControllerInterface controller {
			get { return _mediaControllerInterface; }
		}

		/// <summary>
		/// This interface can be used to access mediaplayer interface.
		/// </summary>
		/// <value>The mediaplayer interface</value>
		public MediaPlayerInterface mediaPlayer {
			get { return _mediaPlayerInterface; }
		}
		/// <summary>
		/// This interface can be used to control the livestream sync feature.
		/// </summary>
		/// <value>The sync interface</value>
		public SyncInterface sync {
			get { return _syncInterface; }
		}
		/// <summary>
		/// This interface can be used to access debug-related APIs.
		/// > [!WARNING]
		/// > Do not use any API on this interface. They are subject to change without notice not backwards compatibility.
		/// </summary>
		/// <value>The debug interface</value>
		public DebugInterface debug {
			get { return _debugInterface; }
		}

		/// <summary>
		/// Used to subscribe to [ClearVREvent](xref:com.tiledmedia.clearvr.ClearVREvent) events.
		/// </summary>
		public ClearVREvents clearVREvents {
			get {
				return _clearVREvents;
			}
		}
		/// <summary>
		/// Attach a listener to this event channel to be notified of any changes to any [Display Object](xref:com.tiledmedia.clearvr.ClearVRDisplayObjectControllerBase).
		/// </summary>
		/// <value></value>
		public ClearVRDisplayObjectEvents clearVRDisplayObjectEvents {
			get {
				return _clearVRDisplayObjectEvents;
			}
		}

		/// <summary>
		/// Query whether ClearVR supports the current active Application.platform
		/// </summary>
		/// <returns>true if the platform is supported, false otherwise</returns>
		public static bool GetIsPlatformSupported() {
			return GetIsPlatformSupported(Application.platform);
		}

		/// <summary>
		/// Returns the SDK version or "Unknown" if unknown.
		/// </summary>
		/// <returns>The SDK version as a String</returns>
		public static String GetClearVRCoreVersion() {
#if UNITY_ANDROID && !UNITY_EDITOR
            return MediaPlayerAndroid.GetClearVRCoreVersion();
#elif (UNITY_IOS || UNITY_TVOS) && !UNITY_EDITOR
            return MediaPlayerIOS.GetClearVRCoreVersion();
#elif UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
			if(GetIsPlatformSupported()) {
				return MediaPlayerPC.GetClearVRCoreVersion();
			} else {
				return MediaPlayerBase.DEFAULT_CLEAR_VR_CORE_VERSION_STRING;
			}
#else
			return MediaPlayerBase.DEFAULT_CLEAR_VR_CORE_VERSION_STRING;
#endif
        }

		/// <summary>
		/// Check whether an hardware HEVC video decoder is present or not.
		/// Having a hardware HEVC decoder is a prerequisit for being able to play Mosaic and ClearVR content.
		/// </summary>
		/// <returns>True if a hardware HEVC decoder is available, false otherwise.</returns>
		public static bool GetIsHardwareHEVCDecoderAvailable() {
#if UNITY_ANDROID && !UNITY_EDITOR
            return MediaPlayerAndroid.GetIsHardwareHEVCDecoderAvailable();
#elif (UNITY_IOS || UNITY_TVOS) && !UNITY_EDITOR
            return MediaPlayerIOS.GetIsHardwareHEVCDecoderAvailable();
#elif UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
			if(GetIsPlatformSupported()) {
				return MediaPlayerPC.GetIsHardwareHEVCDecoderAvailable();
			} else {
				return false;
			}
#else
			return false;
#endif
        }



		public static ProxyParameters GetProxyParameters(ProxyParameters proxyParameters) {
			cvrinterface.ProxyParamsMediaFlow coreProxyParameters = proxyParameters.ToCoreProtobuf();
			string base64Message = System.Convert.ToBase64String(coreProxyParameters.ToByteArray());
			bool isAPISupported = true;
#if (UNITY_IOS || UNITY_TVOS) && !UNITY_EDITOR
			String base64Result = MediaPlayerIOS.GetProxyParameters(base64Message);
#elif UNITY_ANDROID && !UNITY_EDITOR
			String base64Result = MediaPlayerAndroid.GetProxyParameters(base64Message);
#elif UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
			String base64Result = MediaPlayerPC.GetProxyParameters(base64Message);
#else
            String base64Result = null;
            isAPISupported = false;
#endif
			if(isAPISupported) {
				if (String.IsNullOrEmpty(base64Result)) {
					UnityEngine.Debug.LogWarning("[ClearVR] GetProxyParameters failed to return a valid value. Please report this problem to Tiledmedia.");
					return null;
				}

				byte[] raw = System.Convert.FromBase64String(base64Result);
				coreProxyParameters = cvrinterface.ProxyParamsMediaFlow.Parser.ParseFrom(raw);
				ProxyParameters resultParameters = new ProxyParameters(ProxyTypesMethods.FromCoreProtobuf(coreProxyParameters.ProxyType));
				resultParameters.SetProxyParameters(coreProxyParameters.Host, coreProxyParameters.Port, coreProxyParameters.Username, coreProxyParameters.Password);
				return resultParameters;
			} else {
				// GetProxyParameters() API not available on this platform
				return null;
			}

		}

		/// <summary>
		/// Query whether ClearVR supports a specific RuntimePlatform.
		/// </summary>
		/// <param name="argPlatform"> The platform to check for.</param>
		/// <returns></returns>
		public static bool GetIsPlatformSupported(RuntimePlatform argPlatform) {
			switch(argPlatform) {
				case RuntimePlatform.Android: {
#if UNITY_ANDROID && !UNITY_EDITOR
					return true;
#else
					break;
#endif
				}
				case RuntimePlatform.IPhonePlayer: {
#if UNITY_IOS && !UNITY_EDITOR
					return true;
#else
					break;
#endif
				}
				case RuntimePlatform.tvOS:
#if UNITY_TVOS && !UNITY_EDITOR
					return true;
#else
					break;
#endif
				case RuntimePlatform.WindowsEditor: {
#if UNITY_EDITOR_WIN
                    return Utils.GetIsMediaFlowWindowsLibraryFound();
#else
					break;
#endif
                    }
                case RuntimePlatform.WindowsPlayer: {
#if UNITY_STANDALONE_WIN
					return Utils.GetIsMediaFlowWindowsLibraryFound();
#else
					break;
#endif
				}
				case RuntimePlatform.LinuxEditor: {
#if UNITY_EDITOR_LINUX
                    return Utils.GetIsMediaFlowLinuxLibraryFound();
#else
					break;
#endif
                    }
                case RuntimePlatform.LinuxPlayer: {
#if UNITY_STANDALONE_LINUX
					return Utils.GetIsMediaFlowLinuxLibraryFound();
#else
					break;
#endif
				}
			}
			return false;
		}

		/// <summary>
		/// This private MonoBehaviour class is used to offload the TestIsContentSupported callback to the main Unity thread.
		/// </summary>
		private class TestIsContentSupportedCallbackHandler : MonoBehaviour {
			internal struct TestIsContentSupportedCallback {
				internal Action<ContentSupportedTesterReport, object[]> onSuccess;
				internal Action<ClearVRMessage, object[]> onFailure;
				internal ClearVRMessage clearVRMessage;
				internal ContentSupportedTesterReport report;
			};
			// Only one item will ever be pushed on this queue throughout the lifetime of this MonoBehaviour
			internal Queue<TestIsContentSupportedCallback> testIsContentSupportedCallbackQueue = new Queue<TestIsContentSupportedCallback>();

			void Update() {
				while(testIsContentSupportedCallbackQueue.Count > 0) {
					TestIsContentSupportedCallback testIsContentSupportedCallback = testIsContentSupportedCallbackQueue.Dequeue();
					if(testIsContentSupportedCallback.clearVRMessage.GetIsSuccess()) {
						testIsContentSupportedCallback.onSuccess(testIsContentSupportedCallback.report, testIsContentSupportedCallback.report.optionalArguments);
					} else {
						testIsContentSupportedCallback.onFailure(testIsContentSupportedCallback.clearVRMessage, testIsContentSupportedCallback.report.optionalArguments);
					}
					// The GameObject to which this MonoBehaviour is attached has served its purpose. Therefor, it is time to say good bye and destroy it.
					UnityEngine.Object.Destroy(this.gameObject);
					break; // There will be only max one item on the queue.
				}
			}
		}

		private class CallCoreCallbackHandler : MonoBehaviour {
			internal struct CallCoreCallback {
				internal Action<String, object[]> onSuccess;
				internal Action<ClearVRMessage, object[]> onFailure;
				internal ClearVRMessage clearVRMessage;
				internal String base64Message;
				internal object[] optionalArguments;
			};
			internal Queue<CallCoreCallback> callCoreCallbackQueue = new Queue<CallCoreCallback>();

			void Update() {
				while (callCoreCallbackQueue.Count > 0) {
					CallCoreCallback callCoreCallback = callCoreCallbackQueue.Dequeue();
					if(callCoreCallback.clearVRMessage.GetIsSuccess()) {
						callCoreCallback.onSuccess(callCoreCallback.base64Message, callCoreCallback.optionalArguments);
					} else {
						callCoreCallback.onFailure(callCoreCallback.clearVRMessage, callCoreCallback.optionalArguments);
					}
					// The GameObject to which this MonoBehaviour is attached has served its purpose. Therefor, it is time to say good bye and destroy it.
					UnityEngine.Object.Destroy(this.gameObject);
					break; // There will be only max one item on the queue.
				}
			}
		}

#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
		/// <summary>
		/// This private MonoBehaviour class is used to keep track of the Screen's DeviceOrientation
		/// Note that we are interested in the Screen.orientation (Specifies logical orientation of the screen), not the Input.deviceOrientation (Device physical orientation as reported by OS.)
		/// </summary>
		private class ScreenOrientationTracker : MonoBehaviour {
			private ScreenOrientation currentScreenOrientation;
			internal System.Action<ScreenOrientation /* new */> OnScreenOrientationChanged = null;

			void Awake() {
				currentScreenOrientation = Screen.orientation;
				StartCoroutine(CheckScreenOrientation());
			}

			void Start() {
				// Trigger event to notify listener of current ScreenOrientation
				TriggerOnScreenOrientationChanged();
			}

			IEnumerator CheckScreenOrientation() {
				ScreenOrientation newScreenOrientation = Screen.orientation;
				while(true) {
					// we check at vsync / 3 Hz to minimize the cost of the Screen.orientation call.
					yield return null; // wait one frame
					yield return null; // wait one frame
					yield return null; // wait one frame
					newScreenOrientation = Screen.orientation;
					if(newScreenOrientation != currentScreenOrientation) {
						if(OnScreenOrientationChanged != null) {
							OnScreenOrientationChanged(newScreenOrientation);
						}
						currentScreenOrientation = newScreenOrientation;
					}
				}
			}

			/// <summary>
			/// Triggers an event, signalling the current orientation.
			/// </summary>
			internal void TriggerOnScreenOrientationChanged() {
				if(OnScreenOrientationChanged != null) {
					OnScreenOrientationChanged(currentScreenOrientation);
				}
			}
		}
#endif

		/// <summary>
		/// <para>
		/// Test whether the list of ContentItems can be played on the current device or not.
		/// In the provided Action callback, query argClearVRMessage.GetIsSuccess() to determine whether the content check was successful or not. If the check was not successfull, ContentSupportedTesterReport will be `null`.
		/// If successful, the ContentItemList will have its `contentSupportedStatus` field set to the appropriate value.
		/// </para>
		/// > [!NOTE]
		/// > Since v7.4 the callback is guaranteed to be triggered on the main Unity thread IF this API is called on the main Unity thread.
		/// > The callback will be triggered on a random thread if this API is not called on the main Unity thread.
		/// </summary>
		/// <param name="contentSupportedTesterParameters">The parameters describing the test.</param>
		/// <param name="onSuccess">Callback to be triggered when the test is successful. Cannot be null</param>
		/// <param name="onFailure">Callback to be triggered when the test is not successful.</param>
		/// <param name="optionalArguments">Any optional arguments youw ant to pass along and receive in the callback.</param>
		public static void TestIsContentSupported(ContentSupportedTesterParameters contentSupportedTesterParameters, Action<ContentSupportedTesterReport, object[]> onSuccess, Action<ClearVRMessage, object[]> onFailure, params object[] optionalArguments) {
			if(onSuccess == null) {
				throw new Exception("[ClearVR] Calling ClearVRPlayer.TestIsContentSupported() without callback is not allowed.");
			}
			GameObject testIsContentSupportedCallbackGameObject = null;
			TestIsContentSupportedCallbackHandler handler = null; // Will remain null if not called from the main Unity thread
			try {
				testIsContentSupportedCallbackGameObject= new GameObject("ClearVR-Transient-TestIsContentSupported-Handler");
			} catch {
				// An exception means that we were unable to instantiate a GameObject. This is typically happening when this menthod is not called from the main thread.
				// This was allowed pre-v7.4, so we add this graceful fallback behaviour here.
			}
			try {
				handler = testIsContentSupportedCallbackGameObject.AddComponent<TestIsContentSupportedCallbackHandler>();
			} catch {
				// An exception is thrown if not called on the main thread.
				// In that case, we clean-up if need be.
				try {
					if(testIsContentSupportedCallbackGameObject != null) {
						UnityEngine.Object.Destroy(testIsContentSupportedCallbackGameObject);
						testIsContentSupportedCallbackGameObject = null;
					} // else: nothing to clean-up.
				} catch {
					// Silently ignore exception.
				}
			}
			// Convenience Action<>() that triggers the callback on the appropriate thread.
			var triggerCallback = new Action<ClearVRMessage, ContentSupportedTesterReport>((ClearVRMessage clearVRMessage, ContentSupportedTesterReport report) => {
				if(handler != null) {
					handler.testIsContentSupportedCallbackQueue.Enqueue(new TestIsContentSupportedCallbackHandler.TestIsContentSupportedCallback() { onSuccess = onSuccess, onFailure = onFailure, clearVRMessage = clearVRMessage, report = report});
				} else {
					if(clearVRMessage.GetIsSuccess()) {
						onSuccess(report, optionalArguments);
					} else {
						onFailure(clearVRMessage, optionalArguments);
					}
				}
			});

			if(!contentSupportedTesterParameters.Verify()) {
				triggerCallback(new ClearVRMessage(ClearVRMessageTypes.FatalError, ClearVRMessageCodes.GenericFatalError, "Invalid parameters provided.", ClearVRResult.Failure), null);
				return;
			}
			// TestIsContentSupported is available on Android and iOS
#if (UNITY_IOS || UNITY_TVOS || UNITY_ANDROID) && !UNITY_EDITOR
			Action<ClearVRMessage> cbTestIsContentSupported = new Action<ClearVRMessage>(delegate (ClearVRMessage argClearVRMessage) {
				ContentSupportedTesterReport report = new ContentSupportedTesterReport(ref contentSupportedTesterParameters.contentItemList, optionalArguments);
				cvrinterface.CheckIsSupportedReport cvriReport = null;
				ClearVRMessage clearVRMessage = null;
				try {
					var raw = System.Convert.FromBase64String(argClearVRMessage.message);
					cvriReport = cvrinterface.CheckIsSupportedReport.Parser.ParseFrom(raw);
				} catch (Exception e) {
					clearVRMessage = new ClearVRMessage(ClearVRMessageTypes.Warning, ClearVRMessageCodes.GenericWarning, String.Format("Unable to deserialize CheckIsSupportedReport. Error message: {0}.", e), ClearVRResult.Failure);
				}
				if (cvriReport != null) {
					if (cvriReport.ErrorCode != 0) {
						// An error was reported. the report cannot be trusted.
						clearVRMessage = new ClearVRMessage((int) ClearVRMessageTypes.Warning, cvriReport.ErrorCode /* contains a ClearVRCoreErrorCode */, cvriReport.ErrorMessage, ClearVRResult.Failure);
					} else {
						clearVRMessage = ClearVRMessage.GetGenericOKMessage(); // Overwrite the ClearVRMessage with payload to a GenericOK message
						for (int i = 0; i < report.contentItemList.Length; i++) {
							ContentSupportedStatus status = ContentSupportedStatusMethods.FromCoreStatus(cvriReport.IsSupported[i]);
							report.contentItemList[i]._contentSupportedStatus = status;
						}
					}
				} else {
					if(clearVRMessage == null) {
						clearVRMessage = argClearVRMessage;
					}
				}
				triggerCallback(clearVRMessage, report);
				return;
			});
#if UNITY_ANDROID
			MediaPlayerAndroid.TestIsContentSupported(contentSupportedTesterParameters, cbTestIsContentSupported);
#elif (UNITY_IOS || UNITY_TVOS) &&!UNITY_EDITOR
			MediaPlayerIOS.TestIsContentSupported(contentSupportedTesterParameters, cbTestIsContentSupported);
#endif
#else
			triggerCallback(new ClearVRMessage(ClearVRMessageTypes.FatalError, ClearVRMessageCodes.APINotSupportedOnThisPlatform, "API not available on this platform.", ClearVRResult.Failure), null);
#endif
		}

		/// <summary>
		/// Test whether the list of ContentItems can be played on the current device or not.
		/// In the provided Action callback, query argClearVRMessage.GetIsSuccess() to determine whether the content check was successful or not. If the check was not successfull, ContentSupportedTesterReport will be `null`.
		/// If successful, the ContentItemList will have its `contentSupportedStatus` field set to the appropriate value.
		///
		/// > [!NOTE]
		/// > Since v7.4 the callback is guaranteed to be triggered on the main Unity thread IF this API is called on the main Unity thread.
		/// > The callback will be triggered on a random thread if this API is not called on the main Unity thread.
		/// </summary>
		/// <param name="argContentSupportedTesterParameters">The parameters describing the test.</param>
		/// <param name="argCbReportReceived">The callback to trigger upon success. Cannot be null.</param>
		/// <param name="argOptionalArguments">Any optional arguments youw ant to pass along and receive in the callback.</param>
		[Obsolete("This API has been deprecated and will be removed 2023/01/31. Please use TestIsContentSupported(ContentSupportedTesterParameters, Action, Action, Params) instead.", false)]
		public static void TestIsContentSupported(ContentSupportedTesterParameters argContentSupportedTesterParameters, Action<ClearVRMessage, ContentSupportedTesterReport> argCbReportReceived, params object[] argOptionalArguments) {
			Action<ContentSupportedTesterReport, object[]> onSuccess = new Action<ContentSupportedTesterReport, object[]>((contentSupportedTesterReport, optionalArgs)=> {
				argCbReportReceived(ClearVRMessage.GetGenericOKMessage(), contentSupportedTesterReport);
			});
			Action<ClearVRMessage, object[]> onFailure = new Action<ClearVRMessage, object[]>((clearVRMessage, optionalArgs)=> {
				argCbReportReceived(clearVRMessage, null);
			});
			TestIsContentSupported(argContentSupportedTesterParameters, onSuccess, onFailure, argOptionalArguments);
		}

		/// <summary>
		/// Sends a message to the core.
		/// </summary>
		/// <param name="base64Message">The base64Message with instructions.</param>
		/// <param name="onSuccess">This is triggered when the CallCore call was succesful. Contains a base64 message. Cannot be null.</param>
		/// <param name="onFailure">This is triggered when the CallCore call was unsuccesful and inclides a ClearVRMessage object with the reason of this failiure.</param>
		/// <param name="optionalArguments">Any optional arguments you'd like to add.</param>
		public static void CallCoreStatic(String base64Message, Action<String, object[]> onSuccess, Action<ClearVRMessage, object[]> onFailure, params object[] optionalArguments) {
			if (onSuccess == null) {
				throw new Exception("[ClearVR] Calling ClearVRPlayer.CallCoreStatic() without onSucces callback is not supported.");
			}
			GameObject callCoreCallbackGameObject = null;
			CallCoreCallbackHandler handler = null; // Will remain null if not called from the main Unity thread
			try {
				callCoreCallbackGameObject = new GameObject("ClearVR-Transient-CallCoreStatic-Handler");
			} catch {
				// An exception means that we were unable to instantiate a GameObject. This is typically happening when this menthod is not called from the main thread.
				// This was allowed pre-v7.4, so we add this graceful fallback behaviour here.
			}
			try {
				handler = callCoreCallbackGameObject.AddComponent<CallCoreCallbackHandler>();
			} catch {
				// An exception is thrown if not called on the main thread.
				// In that case, we clean-up if need be.
				try {
					if(callCoreCallbackGameObject != null) {
						UnityEngine.Object.Destroy(callCoreCallbackGameObject);
						callCoreCallbackGameObject = null;
					} // else: nothing to clean-up.
				} catch {
					// Silently ignore exception.
				}
			}
			// Convenience Action<>() that triggers the callback on the appropriate thread.
			var triggerCallback = new Action<ClearVRMessage, String>((ClearVRMessage cbClearVRMessage, String cbBase64Message) => {
				if(handler != null) {
					handler.callCoreCallbackQueue.Enqueue(new CallCoreCallbackHandler.CallCoreCallback() { onSuccess = onSuccess, onFailure = onFailure, clearVRMessage = cbClearVRMessage, base64Message = cbBase64Message, optionalArguments = optionalArguments});
				} else {
					if (cbClearVRMessage.GetIsSuccess()) {
						onSuccess(cbBase64Message, optionalArguments);
					} else {
						onFailure(cbClearVRMessage, optionalArguments);
					}
				}
			});


            Action<ClearVRMessage> cbCallCoreStatic = new Action<ClearVRMessage>(delegate (ClearVRMessage argClearVRMessage) {
				String base64MessageResult = "";
				if (!String.IsNullOrEmpty(argClearVRMessage.message)) {
					base64MessageResult = argClearVRMessage.message;
				}
				triggerCallback(argClearVRMessage, base64MessageResult);
			});
#if UNITY_ANDROID && !UNITY_EDITOR
			MediaPlayerAndroid.CallCoreStatic(base64Message, cbCallCoreStatic);
#elif (UNITY_IOS || UNITY_TVOS) && !UNITY_EDITOR
			MediaPlayerIOS.CallCoreStatic(base64Message, cbCallCoreStatic);
#elif UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
            MediaPlayerPC._CallCoreStatic(base64Message, cbCallCoreStatic);
#endif
        }

		private static String CallCoreStaticSync(String base64Message) {
#if UNITY_ANDROID && !UNITY_EDITOR
            return MediaPlayerAndroid.CallCoreStaticSync(base64Message);
#elif (UNITY_IOS || UNITY_TVOS) && !UNITY_EDITOR
            return MediaPlayerIOS.CallCoreStaticSync(base64Message);
#elif UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
			if(GetIsPlatformSupported()) {
				return MediaPlayerPC._CallCoreStaticSync(base64Message);
			}
#endif
			UnityEngine.Debug.LogWarning("[ClearVR] CallCoreStaticSync called on unsupported platform. Unable to process this call.");
			return "";
		}

		/// <summary>
		/// Save the current state of the ClearVRPlayer to the core which can be loaded back with LoadState()
		/// Using the ClearVRLoadPreviousState callback after suspend/resume will return you the saved ClearVRPlayer state.
		/// </summary>
		private void SaveState() {
			if (!_internalInterface.GetDidReachPreparingContentForPlayoutStateButNotInStoppingOrStoppedState()) {
				return;
			}
			cvrinterface.SaveStateRequest saveStateRequest = new cvrinterface.SaveStateRequest();
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
			saveStateRequest.PersistenceFolderPath = Application.dataPath;
#endif
			cvrinterface.CallCoreRequest callCoreRequest = new cvrinterface.CallCoreRequest() {
				CallCoreRequestType = cvrinterface.CallCoreRequestType.SaveState,
				SaveStateRequest = saveStateRequest,
			};
			string base64Message = System.Convert.ToBase64String(callCoreRequest.ToByteArray());

			string base64Result = _debugInterface.CallCoreSync(base64Message);

			byte[] raw = System.Convert.FromBase64String(base64Result);
			cvrinterface.CallCoreResponse response = cvrinterface.CallCoreResponse.Parser.ParseFrom(raw);
			if (response.ErrorCode != 0) {
				// An error was reported. the save state cannot be trusted.
				UnityEngine.Debug.LogError(String.Format("[ClearVR] An error was reported while calling SaveState(). Error Code: {0}. Error Message: {1}", response.ErrorCode, response.ErrorMessage));
			}
		}

		/// <summary>
		/// Load the saved state of the ClearVRPlayer that is saved by the ClearVRPlayer
		/// Using the ClearVRLoadPreviousState callback after suspend/resume will return you the saved ClearVRPlayer state.
		/// </summary>
		/// <param name="argClearVRLayoutManager">The LayoutManager that contains the LayoutParameters and DisplayObjectMappings.</param>
		/// <param name="argPrepareContentParameters">Null in case of failure, the last used PrepareContentParameters-equivalent in case of success</param>
		/// <param name="argContentInfo">The last known ContentInfo, or null if not available.</param>
		/// <returns>True in case of success, false in case of failure. In case of failure, the out params are always null. In case of sucess, the out params _can_ be null.</returns>
		private static bool LoadState(ClearVRLayoutManager argClearVRLayoutManager, out PrepareContentParameters argPrepareContentParameters, out ContentInfo argContentInfo) {
			cvrinterface.LoadStateRequest loadStateRequest = new cvrinterface.LoadStateRequest();
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
			loadStateRequest.PersistenceFolderPath = Application.dataPath;
#endif
			cvrinterface.CallCoreRequest callCoreRequest = new cvrinterface.CallCoreRequest() {
				CallCoreRequestType = cvrinterface.CallCoreRequestType.LoadState,
				LoadStateRequest = loadStateRequest,
			};
			string base64Message = System.Convert.ToBase64String(callCoreRequest.ToByteArray());
			string base64Result;
#if UNITY_ANDROID && !UNITY_EDITOR
			// Special Load state method for android because we need the activity.
			base64Result = MediaPlayerAndroid.LoadState(base64Message);
#else
			base64Result = CallCoreStaticSync(base64Message);
#endif
			byte[] raw = System.Convert.FromBase64String(base64Result);
			cvrinterface.CallCoreResponse response = cvrinterface.CallCoreResponse.Parser.ParseFrom(raw);
			if (response.ErrorCode != 0) {
				// An error was reported. the load state cannot be trusted.
				UnityEngine.Debug.LogError(String.Format("[ClearVR] An error was reported while calling LoadState(). Error code: {0}. Error message: {1}", response.ErrorCode, response.ErrorMessage));
				argPrepareContentParameters = null;
				argContentInfo = null;
				return false;
			}
			cvrinterface.PrepareContentParametersMediaflow corePrepareContentParams = new cvrinterface.PrepareContentParametersMediaflow();
			corePrepareContentParams.InitializeParams = response.LoadStateResponse.InitializeParams;
			argPrepareContentParameters = PrepareContentParameters.FromCoreProtobuf(corePrepareContentParams, argClearVRLayoutManager);
			argContentInfo = ContentInfo.FromCoreProtobuf(response.LoadStateResponse.ContentInfo);
			return true;
		}

		private ClearVRLayoutManager _clearVRLayoutManager = null;
		void Awake() {
			DoLoggerReflection();
			EnableLogging(ClearVRPlayer.loggingConfig);
			ClearVRLayoutManager[] clearVRLayoutManagers = GameObject.FindObjectsOfType<ClearVRLayoutManager>();
			if(clearVRLayoutManagers.Length == 1) {
				// This is what we want.
				_clearVRLayoutManager = clearVRLayoutManagers[0];
			} else if(clearVRLayoutManagers.Length > 1) {
				ClearVRLogger.LOGE("Found {0} ClearVRLayoutManagers. This is not supported. There can only be exactly 1 ClearVRLayoutManager active in your scene at any point in time. Undefined behaviour expected, some or all of your DisplayObjects might show up black or empty. Please adjust your scene accordingly.", clearVRLayoutManagers.Length);
				// As a fallback, we assume the first layoutManager to be the "right" one. But this will cause undefined behaviour, as explained in the error message above.
				_clearVRLayoutManager = clearVRLayoutManagers[0];
			}
			if(_clearVRLayoutManager == null) {
				// There is no LayoutManager in the scene.
				// This can happen if this is a project that was created with a pre-v9.x ClearVR SDK. We add a ClearVRLayoutManager as fallback, but this is no longer officially supported.
				_clearVRLayoutManager = gameObject.AddComponent<ClearVRLayoutManager>();
			}
		}

		/// <summary>
		/// Create and initialize the ClearVR player. Depending on the platformOptions, content will be loaded automatically
		/// and playback could start as soon as soon as content loading completed.
		/// > [!NOTE]
		/// > This API must be called from the main thread.
		/// </summary>
		/// <param name="argPlatformOptions">the platform specific (player) options as set by the parent.</param>
		[Obsolete("This API has been deprecated and can no longer be called. Please use Initialize(PlatformOptionsBase, Action, Action, params) instead.", true)]
		public ClearVRAsyncRequest Initialize(PlatformOptionsBase argPlatformOptions) {
			return null;
		}

		/// <summary>
		/// Create and initialize the ClearVR player. Depending on the platformOptions, content will be loaded automatically
		/// and playback could start as soon as soon as content loading completed.
		/// > [!NOTE]
		/// > This API must be called from the main thread.
		/// </summary>
		/// <param name="argPlatformOptions">the platform specific (player) options as set by the parent.</param>
		/// <param name="onSuccess">An optional callback that should be triggered after the request was succesfully completed. You are highly encouragedto implement the callback, but it can be null.</param>
		/// <param name="onFailure">An optional callback that should be triggered in case the request has failed. You are highly encouragedto implement the callback, but it can be null.</param>
		/// <param name="argOptionalArguments">Any optional argument one might want to pass into the callback.</param>
		public void Initialize(PlatformOptionsBase argPlatformOptions, Action<ClearVREvent, ClearVRPlayer> onSuccess, Action<ClearVREvent, ClearVRPlayer> onFailure, params object[] argOptionalArguments) {
			_PreInitializeChecks(ref argPlatformOptions); // throws in case of a problem
			ClearVRAsyncRequest clearVRAsyncRequest = _Initialize(argPlatformOptions);
			// _Initialize() never returns null, so we can safely access _internalInterface here.
			_internalInterface.ScheduleClearVRAsyncRequest(clearVRAsyncRequest, onSuccess, onFailure, argOptionalArguments);
		}

		[Obsolete("This API has been deprecated and will be removed 2023/01/31. Please use Initialize(PlatformOptionsBase, Action, Action, Params) instead.", false)]
		public void Initialize(PlatformOptionsBase argPlatformOptions, Action<ClearVREvent, ClearVRPlayer> argCbClearVRAsyncRequestResponseReceived, params object[] argOptionalArguments) {
			Initialize(argPlatformOptions, argCbClearVRAsyncRequestResponseReceived, argCbClearVRAsyncRequestResponseReceived, argOptionalArguments);
		}

		private void _PreInitializeChecks(ref PlatformOptionsBase argPlatformOptions) {
			if(!GetIsPlatformSupported()) {
				throw new Exception(String.Format("[ClearVR] Platform {0} is not supported on host {1}.", Application.platform, SystemInfo.operatingSystem));
			}
			if(argPlatformOptions.prepareContentParameters != null && argPlatformOptions.prepareContentParameters.layoutParameters != null) {
				LayoutParameters layoutParameters = argPlatformOptions.prepareContentParameters.layoutParameters;
				if(!_clearVRLayoutManager.AddOrUpdateAndVerifyLayoutParameters(layoutParameters)) {
					throw new Exception(String.Format("[ClearVR] Unable to validate LayoutParameters. Cannot perform request. LayoutParameters: {0}", layoutParameters));
				}
			}
			if(!argPlatformOptions.Verify(_clearVRLayoutManager)) {
				throw new Exception("[ClearVR] Errors were reported while verifying platformOptions. Please check the logs and correct the problem. Cannot continue.");
			}
		}

        private ClearVRAsyncRequest _Initialize(PlatformOptionsBase argPlatformOptions) {
			if(_triggerResumingPlaybackAfterApplicationRegainedFocusEvent) {
				// Means _Initialize is called after a suspend/resume cycle
				_clearVREvents.Invoke(this, ClearVREvent.GetGenericOKEvent(ClearVREventTypes.ResumingPlaybackAfterApplicationRegainedFocus));
				_triggerResumingPlaybackAfterApplicationRegainedFocusEvent = false;
			}
			_isStoppingStateTriggered = false;
#if UNITY_EDITOR
			PlayStateNotifier.RegisterCallback(EditorModeChanged);
#endif
			WantsToQuitInterruptNotifier.RegisterCallback(ApplicationWantsToQuit);
			WantsToQuitInterruptNotifier.applicationWantsToQuit = false;
			MediaPlayerBase _mediaPlayer = CreateMediaPlayerForPlatform(argPlatformOptions);
			if(_mediaPlayer == null) {
				throw new Exception("[ClearVR] Cannot create platform specific media player. Platform not yet supported?");
			}
			_mediaPlayerInterface = _mediaPlayer;
			_mediaControllerInterface = _mediaPlayer;
			_mediaInfoInterface = _mediaPlayer;
			_performanceInterface = _mediaPlayer;
			_syncInterface = _mediaPlayer;
			_debugInterface = _mediaPlayer;
			_internalInterface = _mediaPlayer;
			_isInitializeCalled = true; // At this point, the interfaces have been created. Do not move this assignment around. Must be at the position just after creating the interfaces.
			// At this point we can safely add the callback listener.
			if(_clearVRLayoutManager != null) {
				_clearVRLayoutManager.clearVRDisplayObjectEvents.AddListener(CbClearVRDisplayObjectEvent);
			}
			ClearVRAsyncRequest clearVRAsyncRequest = new ClearVRAsyncRequest(RequestTypes.ClearVRPlayerInitialize);
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
			// Screen orientation tracking is only required on mobile platforms.
			// This implementation assumes that the DeviceType never changes mid-flight
			if(argPlatformOptions.deviceParameters.deviceType.GetIsFlatDeviceType() || argPlatformOptions.deviceParameters.deviceType.GetIsCardboardLikeDeviceType()) {
				ScreenOrientationTracker screenOrientationTracker = gameObject.GetComponent<ScreenOrientationTracker>();
				if(screenOrientationTracker == null) {
					// Only attach it once.
					screenOrientationTracker = gameObject.AddComponent<ScreenOrientationTracker>();
					screenOrientationTracker.OnScreenOrientationChanged = new System.Action<ScreenOrientation> (delegate(ScreenOrientation argNewScreenOrientation) {
						if(_internalInterface != null) {
							_internalInterface.UpdateScreenOrientation(argNewScreenOrientation);
						}
					});
				} else {
					// Already attached, no need to attach it again but we have to trigger the event to make sure the MediaPlayerBase knows the current ScreenOrientation
					screenOrientationTracker.TriggerOnScreenOrientationChanged();
				}
			} // else: This device type does not need the tracker, but it is attached. For now we assume that this cannot happen.
#endif

			return clearVRAsyncRequest;
		}

		internal void DoLoggerReflection() {
			try {
				object loggerSettings = Resources.Load("LoggerConfig");
				if (loggerSettings != null) {
					Type loggerSettingsType = Utils.GetType("LoggerSettings");
					if (loggerSettingsType != null) {
						bool clrvrUnityPlayerLogging = (bool)loggerSettingsType.GetField("ClrvrUnityPlayerLogging").GetValue(loggerSettings);
						bool enableClearVRLogging = (bool)loggerSettingsType.GetField("EnableClearVRLogging").GetValue(loggerSettings);
						if (clrvrUnityPlayerLogging)
						{
							clearVREvents.AddListener((_clrVRPlayer, _clrVREvent) => _clrVREvent.Print());
						}
						if (enableClearVRLogging)
						{
							LogLevels clearVRGlobalVerbosity = (LogLevels)loggerSettingsType.GetField("ClrvrVRGlobalVerbosity").GetValue(loggerSettings);
							String clrvrVRGlobalFileName = (String)loggerSettingsType.GetField("ClrvrVRGlobalFileName").GetValue(loggerSettings);
							String clrvrVRRecorderFileName = (String)loggerSettingsType.GetField("InteractionEventRecorderFileName").GetValue(loggerSettings);
							ClearVRPlayer.loggingConfig.globalLogLevel = clearVRGlobalVerbosity;
							ClearVRPlayer.loggingConfig.globalLogFileName = clrvrVRGlobalFileName;
							ClearVRPlayer.loggingConfig.globalLogFolder = LoggingConfiguration.GetDefaultLoggingFolder();
							ClearVRPlayer.loggingConfig.interactionEventRecorderFileName = clrvrVRRecorderFileName;

							// Core overrides
							bool enableOverrideCoreLogLevel = (bool)loggerSettingsType.GetField("EnableOverrideCoreLogLevel").GetValue(loggerSettings);
							if (enableOverrideCoreLogLevel) {
								LogLevels coreLogLevel = (LogLevels)loggerSettingsType.GetField("CoreLogLevel").GetValue(loggerSettings);
								ClearVRPlayer.loggingConfig.coreLogLevel = coreLogLevel;
							}
							bool enableOverrideCoreLogFileName = (bool)loggerSettingsType.GetField("EnableOverrideCoreLogFileName").GetValue(loggerSettings);
							if (enableOverrideCoreLogFileName) {
								String coreLogFileName = (String)loggerSettingsType.GetField("CoreLogFileName").GetValue(loggerSettings);
								ClearVRPlayer.loggingConfig.coreLogFileName = coreLogFileName;
							}

							// NRP overrides
							bool enableOverrideNRPLogLevel = (bool)loggerSettingsType.GetField("EnableOverrideNRPLogLevel").GetValue(loggerSettings);
							if (enableOverrideNRPLogLevel) {
								LogLevels NRPLogLevel = (LogLevels)loggerSettingsType.GetField("NRPLogLevel").GetValue(loggerSettings);
								ClearVRPlayer.loggingConfig.nrpLogLevel = NRPLogLevel;
							}
							bool enableOverrideNRPLogFileName = (bool)loggerSettingsType.GetField("EnableOverrideNRPLogFileName").GetValue(loggerSettings);
							if (enableOverrideNRPLogFileName) {
								String NRPLogFileName = (String)loggerSettingsType.GetField("NRPLogFileName").GetValue(loggerSettings);
								ClearVRPlayer.loggingConfig.nrpLogFileName = NRPLogFileName;
							}

							// Mediaflow overrides
							bool enableOverrideMediaflowLogLevel = (bool)loggerSettingsType.GetField("EnableOverrideMediaflowLogLevel").GetValue(loggerSettings);
							if (enableOverrideMediaflowLogLevel) {
								LogLevels MediaflowLogLevel = (LogLevels)loggerSettingsType.GetField("MediaflowLogLevel").GetValue(loggerSettings);
								ClearVRPlayer.loggingConfig.mfLogLevel = MediaflowLogLevel;
							}
							bool enableOverrideMediaflowLogFileName = (bool)loggerSettingsType.GetField("EnableOverrideMediaflowLogFileName").GetValue(loggerSettings);
							if (enableOverrideMediaflowLogFileName) {
								String MediaflowLogFileName = (String)loggerSettingsType.GetField("MediaflowLogFileName").GetValue(loggerSettings);
								ClearVRPlayer.loggingConfig.mfLogFileName = MediaflowLogFileName;
							}

							// SDK overrides
							bool enableOverrideSDKLogLevel = (bool)loggerSettingsType.GetField("EnableOverrideSDKLogLevel").GetValue(loggerSettings);
							if (enableOverrideSDKLogLevel) {
								LogLevels SDKLogLevel = (LogLevels)loggerSettingsType.GetField("SDKLogLevel").GetValue(loggerSettings);
								ClearVRPlayer.loggingConfig.sdkLogLevel = SDKLogLevel;
							}
							bool enableOverrideSDKLogFileName = (bool)loggerSettingsType.GetField("EnableOverrideSDKLogFileName").GetValue(loggerSettings);
							if (enableOverrideSDKLogFileName) {
								String SDKLogFileName = (String)loggerSettingsType.GetField("SDKLogFileName").GetValue(loggerSettings);
								ClearVRPlayer.loggingConfig.sdkLogFileName = SDKLogFileName;
							}

							// SigmaAudio overrides
							bool enableOverrideSigmaAudioLogLevel = (bool)loggerSettingsType.GetField("EnableOverrideSigmaAudioLogLevel").GetValue(loggerSettings);
							if (enableOverrideSigmaAudioLogLevel) {
								LogLevels SigmaAudioLogLevel = (LogLevels)loggerSettingsType.GetField("SigmaAudioLogLevel").GetValue(loggerSettings);
								ClearVRPlayer.loggingConfig.sigmaAudioLogLevel = SigmaAudioLogLevel;
							}
							bool enableOverrideSigmaAudioLogFileName = (bool)loggerSettingsType.GetField("EnableOverrideSigmaAudioLogFileName").GetValue(loggerSettings);
							if (enableOverrideSigmaAudioLogFileName) {
								String SigmaAudioLogFileName = (String)loggerSettingsType.GetField("SigmaAudioLogFileName").GetValue(loggerSettings);
								ClearVRPlayer.loggingConfig.sigmaAudioLogFileName = SigmaAudioLogFileName;
							}

						}
					}
				}
			} catch { // explicit fallthrough as we do not care for the exception in this case
			}
		}


		[Obsolete("This API has been replaced by EnableLogging(). This will be removed after 2024-04-04.", false)]
		public static void EnableLoggingConfiguration(LoggingConfiguration argLoggingConfiguration) {
			EnableLogging(argLoggingConfiguration);
		}

		/// <summary>
		/// Enable extra logging on the SDK
		/// To disable the extra logging you can pass null as an argument.
		/// </summary>
		/// <param name="loggingConfiguration">the configuration to change the path, log level etc. see <see cref="LoggingConfiguration"/> for more information.</param>
		public static void EnableLogging(LoggingConfiguration loggingConfiguration) {
			LoggingConfiguration config = new LoggingConfiguration();
			if (loggingConfiguration != null) {
				config = loggingConfiguration;
			}

			ClearVRPlayer.loggingConfig = config;

			cvrinterface.CallCoreRequest callCoreRequest = new cvrinterface.CallCoreRequest {
				CallCoreRequestType = cvrinterface.CallCoreRequestType.InitLogging,
				InitializeLoggingRequest = config.ToCoreProtobuf(),
			};
			string msg = Convert.ToBase64String(callCoreRequest.ToByteArray());

			ClearVRPlayer.CallCoreStatic(msg, 
			onSuccess: (base64Message, optionalArguments) => {
				cvrinterface.CallCoreResponse response = cvrinterface.CallCoreResponse.Parser.ParseFrom(System.Convert.FromBase64String(base64Message));
				if (string.IsNullOrEmpty(response.ErrorMessage)) {
					// Everything ok
				} else {
					// Something went wrong while starting logging
#if DEBUG
					UnityEngine.Debug.LogError("[ClearVR] Something went wrong while setting the logging configuration: " + response.ErrorMessage);
#endif
				}
			}, 
			onFailure: (clearVRMessage, optionalArguments) => {
#if DEBUG
				UnityEngine.Debug.LogError("[ClearVR] Failure while setting the logging configuration: " + clearVRMessage.GetFullMessage());
#endif
			});
		}

		/// <summary>
		/// This API is used to easily upload Tiledmedia player-generated logfiles to the Tiledmedia backend. This API should ONLY be used while debugging an issue that could be related to the Tiledmedia Player SDK, and should typically be disabled/not called in release builds of your product.
		/// 
		/// > [!WARNING]
		/// > You are advised to only use this API while debugging your application. 
		/// When debugging such an issue, you would typically follow these steps:
		/// 1) Enable Tiledmedia Player logging
		/// 2) When the problem has happened (just after, or after you have restarted the app in case of hard crash for example), you upload the logs.
		/// 
		/// See the following code snippet:
		/// <code language="cs"><![CDATA[
		/// LoggingConfiguration loggingConfiguration = LoggingConfiguration.GetDefaultLoggingConfiguration();
		/// ClearVRPlayer.EnableLogging(loggingConfiguration);
		/// ...
		/// ClearVRPlayer.UploadLogs(yourLicenseFile, 
		///     onSuccess: (uniqueUploadID, optionalArguments) => { 
		///         UnityEngine.Debug.Log("Tiledmedia log upload success. Unique upload id: " + uniqueUploadID);
		///     },
		///     onFailure: (clearVRMessage, optionalArguments) => { 
		///         UnityEngine.Debug.Log("Tiledmedia log upload failed. Error details: " + clearVRMessage);
		///     }
		/// );
		/// ClearVRPlayer.EnableLogging(loggingConfiguration);
		/// ]]></code>
		/// </summary>
		/// 
		/// <param name="licenseFileBytes">Your Tiledmedia Player license file</param>
		/// <param name="onSuccess">The success callback. The String argument contains a unique upload ID.</param>
		/// <param name="onFailure">The failFure callback, triggered if something unexpectedly went wrong.</param>
		/// <param name="optionalArguments">Any optional argument you would like to pass into the onSuccess and onFailure callbacks.</param>
		public static void UploadLogs(byte[] licenseFileBytes, Action<String, object[]> onSuccess, Action<ClearVRMessage, object[]> onFailure, params object[] optionalArguments) {
			UploadLogs(licenseFileBytes, LoggingConfiguration.GetDefaultLoggingFolder(), onSuccess, onFailure, optionalArguments);
		}


		/// <summary>
		/// This API is used to easily upload Tiledmedia player-generated logfiles to the Tiledmedia backend. This API should ONLY be used while debugging an issue that could be related to the Tiledmedia Player SDK, and should typically be disabled/not called in release builds of your product.
		/// 
		/// > [!WARNING]
		/// > You are advised to only use this API while debugging your application. 
		/// When debugging such an issue, you would typically follow these steps:
		/// 1) Enable Tiledmedia Player logging
		/// 2) When the problem has happened (just after, or after you have restarted the app in case of hard crash for example), you upload the logs.
		/// 
		/// See the following code snippet:
		/// <code language="cs"><![CDATA[
		/// LoggingConfiguration loggingConfiguration = LoggingConfiguration.GetDefaultLoggingConfiguration();
		/// ClearVRPlayer.EnableLogging(loggingConfiguration);
		/// ...
		/// ClearVRPlayer.UploadLogs(yourLicenseFile, 
		///     onSuccess: (uniqueUploadID, optionalArguments) => { 
		///         UnityEngine.Debug.Log("Tiledmedia log upload success. Unique upload id: " + uniqueUploadID);
		///     },
		///     onFailure: (clearVRMessage, optionalArguments) => { 
		///         UnityEngine.Debug.Log("Tiledmedia log upload failed. Error details: " + clearVRMessage);
		///     }
		/// );
		/// ClearVRPlayer.EnableLogging(loggingConfiguration);
		/// ]]></code>
		/// </summary>
		/// 
		/// <param name="licenseFileBytes">Your Tiledmedia Player license file</param>
		/// <param name="logFolder">The folder that contains the log fiels to upload. When you are not customizing the logging folder output, this value can be set to `LoggingConfiguration.GetDefaultLoggingFolder()`</param>
		/// <param name="onSuccess">The success callback. The String argument contains a unique upload ID.</param>
		/// <param name="onFailure">The failure callback, triggered if something unexpectedly went wrong.</param>
		/// <param name="optionalArguments">Any optional argument you would like to pass into the onSuccess and onFailure callbacks.</param>
		public static void UploadLogs(byte[] licenseFileBytes, String logFolder, Action<String, object[]> onSuccess, Action<ClearVRMessage, object[]> onFailure, params object[] optionalArguments) {
			if(licenseFileBytes == null) {
				onFailure(new ClearVRMessage(ClearVRMessageTypes.Warning, ClearVRMessageCodes.GenericWarning, "The licenseFile argument cannot be null. Please check your code.", ClearVRResult.Failure), optionalArguments);
				return;
			}
			if(String.IsNullOrEmpty(logFolder)) {
				onFailure(new ClearVRMessage(ClearVRMessageTypes.Warning, ClearVRMessageCodes.GenericWarning, "The LogFolder argument cannot be null or an empty String. Please check your code.", ClearVRResult.Failure), optionalArguments);
				return;
			}
			// Make sure that logFolder ends with a trailing directory separator.
			string sepChar = System.IO.Path.DirectorySeparatorChar.ToString();
			string altChar = System.IO.Path.AltDirectorySeparatorChar.ToString();
			if (!logFolder.EndsWith(sepChar) && !logFolder.EndsWith(altChar)) {
				logFolder += sepChar;
			}
			
			cvrinterface.CallCoreRequest callCoreRequest = new cvrinterface.CallCoreRequest {
				CallCoreRequestType = cvrinterface.CallCoreRequestType.UploadLogs,
				LogUploadRequest = new cvrinterface.LogUploadRequest() {
					LicenseBytes = ByteString.CopyFrom(licenseFileBytes),
					LogFilePath = logFolder
				}
			};
			string msg = Convert.ToBase64String(callCoreRequest.ToByteArray());
			ClearVRPlayer.CallCoreStatic(msg, 
				onSuccess: (base64Message, optionalArguments2) => {
					cvrinterface.CallCoreResponse response = cvrinterface.CallCoreResponse.Parser.ParseFrom(System.Convert.FromBase64String(base64Message));
					if (string.IsNullOrEmpty(response.ErrorMessage)) {
						if(response.LogUploadResponse != null) {
							onSuccess(response.LogUploadResponse.CoreLogID, optionalArguments2);
						} else {
							onFailure(new ClearVRMessage(ClearVRMessageTypes.Warning, ClearVRMessageCodes.GenericWarning, String.Format("The upload logs API returned an unexpected output. Got blob: '{0}'. Please report this problem to Tiledmedia.", response), ClearVRResult.Failure), optionalArguments2);
						}
						return;
					} else {
						// Something went wrong while uploading logs
						String message = String.Format("Encountered an error while uploading logs. error code: {0}, message: '{1}'", response.ErrorCode, response.ErrorMessage);
#if DEBUG
						ClearVRLogger.LOGW(message);
#endif
						onFailure(new ClearVRMessage(ClearVRMessageTypes.Warning, ClearVRMessageCodes.GenericWarning, message, ClearVRResult.Failure), optionalArguments2);
					}
				}, 
				onFailure: (clearVRMessage, optionalArguments2) => {
					// Something went wrong while uploading logs
#if DEBUG
					ClearVRLogger.LOGW("An error was reported while uploading logs: {0}", clearVRMessage);
#endif
					onFailure(clearVRMessage, optionalArguments2);
				});
		}

		private MediaPlayerBase CreateMediaPlayerForPlatform(PlatformOptionsBase argPlatformOptions) {
#if UNITY_ANDROID && !UNITY_EDITOR
			return new MediaPlayerAndroid(argPlatformOptions, _clearVRLayoutManager, CbClearVREvent);
#elif (UNITY_IOS || UNITY_TVOS) && !UNITY_EDITOR
            return new MediaPlayerIOS(argPlatformOptions, _clearVRLayoutManager, CbClearVREvent);
#elif UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
            return new MediaPlayerPC(argPlatformOptions, _clearVRLayoutManager, CbClearVREvent);
#else
            return null;
#endif
        }

        /// <summary>
        /// Called every frame on the main Unity thread. Will schedule video frame rendering as well as check for pending
        /// player actions scheduled to be performed on the Main Unity thread.
        /// </summary>
        void Update() {
            // Check if there is anything scheduled to be done on the main Unity thread.
			// It is important that this happens BEFORE we forward the Update() to the MediaPlayer
			if(_mainThreadActionsLinkedList.Count > 0) {
				while(_mainThreadActionsLinkedList.Count > 0) {
					LinkedListNode<MainThreadAction> mainThreadActionNode = _mainThreadActionsLinkedList.First;
					MainThreadAction mainThreadAction = mainThreadActionNode.Value;
					if(mainThreadAction.vsyncDelayCount > 0) {
						// The head of the list needs another vsync before it can be executed. We break and wait for the next vsync.
						mainThreadAction.vsyncDelayCount = mainThreadAction.vsyncDelayCount - 1;
						break;
					}
					_mainThreadActionsLinkedList.RemoveFirst();
					switch (mainThreadAction.type) {
						case MainThreadActionTypes.PrepareCore:
							if(! _isStoppingStateTriggered) {
								if(_mediaControllerInterface != null) {
									_internalInterface.PrepareCore();
								}
							} else {
								FindPendingClearVRAsyncRequestByRequestTypeAndTriggerCallback(RequestTypes.ClearVRPlayerInitialize, new ClearVREvent(ClearVREventTypes.UnableToInitializePlayer, ClearVRMessageTypes.Info, ClearVRMessageCodes.ClearVRCoreWrapperGenericOK, "Playback not starting as player wass stopped.", ClearVRResult.Success));	
							}
							break;
						case MainThreadActionTypes.Play:
							if(! _isStoppingStateTriggered) {
								if(_mediaControllerInterface != null) {
									_mediaControllerInterface.StartPlayout(
										onSuccess: (cbClearVREvent, cbClearVRPlayer) => { /* Handled in StateChangedPlaying, do not trigger any event here. */},
										onFailure: (cbClearVREvent, cbClearVRPlayer) => {
											// This never happens as StartPlayout never throws.
											FindPendingClearVRAsyncRequestByRequestTypeAndTriggerCallback(RequestTypes.ClearVRPlayerInitialize, cbClearVREvent);
										}
									);
								}
							} else {
								FindPendingClearVRAsyncRequestByRequestTypeAndTriggerCallback(RequestTypes.ClearVRPlayerInitialize, new ClearVREvent(ClearVREventTypes.UnableToInitializePlayer, ClearVRMessageTypes.Info, ClearVRMessageCodes.ClearVRCoreWrapperGenericOK, "Playback not starting as player was stopped.", ClearVRResult.Success));	
							}
							break;
						case MainThreadActionTypes.ForceStopAfterFatalError:
							if(_internalInterface != null) {
								_internalInterface.StopInternal();
							}
							break;
						case MainThreadActionTypes.ApplicationOrEditorQuit:
							TriggerQuit();
							break;
						case MainThreadActionTypes.ResumePlaybackAfterApplicationRegainedFocus:
							ResumePlaybackAfterApplicationRegainedFocus();
							break;
						default:
							break;
					}
				}
			}
			if(_internalInterface != null) {
				_internalInterface.Update();
			}
		}

		/// <summary>
		/// LateUpdate will be called at the end of each rendered frame.
		/// </summary>
		void LateUpdate() {
			if(_internalInterface != null) {
				_internalInterface.LateUpdate();
			}
		}

		/// <summary>
		/// Hndles a FatalError ClearVRMessage by pushing a MainThreadActionTypes.ForceStopAfterFatalError of none was pushed so far.
		/// </summary>
		/// <param name="argMediaPlayer">The mediaplayer that triggered the event.</param>
		/// <param name="argClearVREvent">The FatalError that we want to schedule a second time. Pass null if the ClearVREvent should not be scheduled again.</param>
		/// <returns>True if the FatalError was handled and MainThreadActionTypes.ForceStopAfterFatalError scheduled, false otherwise (e.g. when _isFatalErrorHandled is already raised). In case false is returned, the callee is resposible for forwarding the argClearVREvent to the application layer.</returns>
		private bool ShouldReturnAfterScheduleForceStopAfterFatalError(MediaPlayerBase argMediaPlayer,ClearVREvent argClearVREvent) {
			if(!_isFatalErrorHandled) {
				// We will only notify the app about the FatalError the next VSync.
				// This gives us time to start cleaning things up.
				_isFatalErrorHandled = true;
				if(!_isStoppingStateTriggered) {
					ScheduleMainThreadAction(MainThreadActionTypes.ForceStopAfterFatalError);
				} // else: we are already stopping, we should not schedule another stop as we may have also already scheduled another PrepareCore request after a quick S/R cycle. Besides, we are already in stopping so no need to schedule another Stop request.
				if(argClearVREvent != null){
					// We schedule the FatalError a second time at the front of the LinkeddList
					argMediaPlayer.ScheduleClearVREventFirst(argClearVREvent);
				}
				return true; // Here we should return as the event will be forwarded the next iteration.
			} else {
				// do nothing
			}
			return false;
		}

		/// <summary>
		/// Callback function that handles ClearVRCore events.
		/// </summary>
		/// <param name="argMediaPlayer">The mediaplayer that triggered the event.</param>
		/// <param name="argClearVREvent">The event that was triggered.</param>
		private void CbClearVREvent(MediaPlayerBase argMediaPlayer, ClearVREvent argClearVREvent) {
            // argClearVREvent.PrintShort(); // Enable this line to print the events that are received. Can be handy for debugging purposes.
            switch (argClearVREvent.type) {
				case ClearVREventTypes.StateChangedUninitialized:
					/* This StateChanged event will never be triggered since no listener is attached by the time this happens. */
					/* In other words, you can safely ignore this state transition. It is only listed for completeness sake */
					break;
				case ClearVREventTypes.StateChangedInitializing:
					break;
				case ClearVREventTypes.StateChangedInitialized:
					if(argClearVREvent.message.result == ClearVRResult.Success) {
						// On all platforms, NRP initialization is happening on the renderer thread (e.g. it is scheduled using IssuePluginEvent).
						// This takes at least one vsync to complete.
						// On Android only, we query the NRP for the Surafce object in MediaPlayerAndroid.PrepareCore().
						// In some cases, especially when hammering app suspend/resume, MediaPlayerAndroid.PrepareCore() can get called before the NRP initialization has been performed.
						// This causes a crash, ref #4905.
						// Here we give the process one more vsync to be sure the NRP initialization has completed.
						// As OVROverlay creation is asynchronous from the render thread, we need 3 frames to be sure it has been created (although in 99.9% of the cases it is created within 2 frames)
						// On Android:
						// 1. Schedule Async NRP Initialization on render thread.
						// 2. Wait 2 or 3 vsyncs until wel PrepareCore
						// 3. Call CVR_NRP_GetSurfaceObject() in PrepareCore, which needs the NRP to be initialized
						// On iOS, Unity Editor and Standalone:
						// 1. PrepareCore
						// 2. Schedule Async NRP Initialization on render thread.
						// See also ClearVREventTypes.StateChangedCorePrepared below.
						int frameDelay = platformOptions.textureBlitMode == TextureBlitModes.OVROverlayZeroCopy ? 3 : 2;
						_clearVRLayoutManager.LoadSync(platformOptions); // Synchronous call
#if UNITY_ANDROID && !UNITY_EDITOR
						_clearVRLayoutManager.InitializeAsync(); // Asynchronous call, will be executed on the Render Thread. This must be called before we reach StateChangedPreparingCore.
#endif
						ScheduleMainThreadAction(MainThreadActionTypes.PrepareCore, null, frameDelay);
					} else {
						// Errors were reported
						FindPendingClearVRAsyncRequestByRequestTypeAndTriggerCallback(RequestTypes.ClearVRPlayerInitialize, argClearVREvent);
					}
					break;
				case ClearVREventTypes.StateChangedPreparingCore:
					break;
				case ClearVREventTypes.StateChangedCorePrepared:
					if(!_isStoppingStateTriggered) {
						// MediaPlayerBase.CbClearVRCoreWrapperRequestCompleted already takes care of that this is ONLY triggered in case of success.
						// Failures are already converted to a UnableToInitializePlayer by this method, so they never end-up here
						if(argClearVREvent.message.result == ClearVRResult.Success) {
					        _clearVRLayoutManager.RegisterCallbackToTheCoreAsync();
#if !UNITY_ANDROID || UNITY_EDITOR
							_clearVRLayoutManager.InitializeAsync(); // Asynchronous call, will be executed on the Render Thread. This must be called before we reach StateChangedPreparingCore.
#endif
							if(argMediaPlayer.platformOptions.prepareContentParameters != null && argMediaPlayer.platformOptions.prepareContentParameters.contentItem != null) {
								_mediaPlayerInterface.PrepareContentForPlayout(platformOptions.prepareContentParameters,
								onSuccess: (cbClearVREvent, cbClearVRPlayer) => { /* we automatically continue in StateChangedContentPreparedForPlayout below (as we are in "autoplay" mode), do not trigger anything here */},
								onFailure: (cbClearVREvent, cbClearVRPlayer) => {
									FindPendingClearVRAsyncRequestByRequestTypeAndTriggerCallback(RequestTypes.ClearVRPlayerInitialize, cbClearVREvent);
								}
								);
							} else {
								// No content item specified to prepare
								FindPendingClearVRAsyncRequestByRequestTypeAndTriggerCallback(RequestTypes.ClearVRPlayerInitialize, new ClearVREvent(ClearVREventTypes.GenericMessage, new ClearVRMessage(ClearVRMessageTypes.Info,ClearVRMessageCodes.ClearVRCoreWrapperGenericOK, "ClearVRPlayer prepared, ready to load content.", ClearVRResult.Success)));
							}
						} else {
							// Errors were reported, this should be impossible to trigger
						}
					} else {
						// TODO: should we trigger the FindPendingClearVRAsyncRequestByRequestTypeAndTriggerCallback() here? We do not do it in the UnitySDK nor the Native Android SDK.
					}
					break;
				case ClearVREventTypes.StateChangedPreparingContentForPlayout:
					break;
				case ClearVREventTypes.StateChangedContentPreparedForPlayout:
					if(!_isStoppingStateTriggered) {
						// MediaPlayerBase.CbClearVRCoreWrapperRequestCompleted already takes care of that this is ONLY triggered in case of success.
						// Failures are already converted to a UnableToInitializePlayer by this method, so they never end-up here.
						if(argClearVREvent.message.result == ClearVRResult.Success) {
							controller.SetLoopContent(platformOptions.loopContent);
							if(argMediaPlayer.platformOptions.autoPlay || _forceAutoPlayAfterContentLoadCompleted) {
								ScheduleMainThreadAction(MainThreadActionTypes.Play);
							} else {
								// It's up to the parent to hit play
								FindPendingClearVRAsyncRequestByRequestTypeAndTriggerCallback(RequestTypes.ClearVRPlayerInitialize, new ClearVREvent(ClearVREventTypes.GenericMessage, new ClearVRMessage(ClearVRMessageTypes.Info, ClearVRMessageCodes.ClearVRCoreWrapperGenericOK, "ClearVRPlayer loaded content and is ready for playback.", ClearVRResult.Success)));
								// Note that we continue as we need to notify the parent of the state change.
							}
						} else {
							// Errors were reported, but this is code-path is impossible to get triggered
						}
					} else {
						// TODO: should we trigger the FindPendingClearVRAsyncRequestByRequestTypeAndTriggerCallback() here? We do not do it in the UnitySDK nor the Native Android SDK.
					}
					break;
				case ClearVREventTypes.StateChangedBuffering:
					break;
				case ClearVREventTypes.StateChangedPlaying:
					if(argClearVREvent.message.result == ClearVRResult.Success) {
						if(argMediaPlayer.platformOptions.autoPlay || _forceAutoPlayAfterContentLoadCompleted) {
							FindPendingClearVRAsyncRequestByRequestTypeAndTriggerCallback(RequestTypes.ClearVRPlayerInitialize, new ClearVREvent(ClearVREventTypes.GenericMessage, new ClearVRMessage(ClearVRMessageTypes.Info, ClearVRMessageCodes.ClearVRCoreWrapperGenericOK, "ClearVRPlayer is playing content.", ClearVRResult.Success)));
						} else {
							// No auto play
						}
					} else {
						FindPendingClearVRAsyncRequestByRequestTypeAndTriggerCallback(RequestTypes.ClearVRPlayerInitialize, argClearVREvent);
						if(ShouldReturnAfterScheduleForceStopAfterFatalError(argMediaPlayer, null /* We pass null as we do not want to push the ClearVREvent to the top of the queue. as it has already been handled */)) {
							return;
						} else {
							// do nothing, forward FatalError
						}
					}
					break;
				case ClearVREventTypes.StateChangedPausing:
					break;
				case ClearVREventTypes.StateChangedPaused:
					break;
				case ClearVREventTypes.StateChangedSeeking:
					break;
				case ClearVREventTypes.StateChangedSwitchingContent:
					break;
				case ClearVREventTypes.StateChangedFinished:
					break;
				case ClearVREventTypes.StateChangedStopping:
					_isStoppingStateTriggered = true;
					_clearVRLayoutManager.DestroyAsync();
					// Trigger ClearVRPlayerInitialize if applicable. We continue regardless to notify the parent of the state change except when we are in a suspend/resume cycle. This is a NOOP if no such request is pending.
					FindPendingClearVRAsyncRequestByRequestTypeAndTriggerCallback(RequestTypes.ClearVRPlayerInitialize, new ClearVREvent(ClearVREventTypes.GenericMessage, new ClearVRMessage(ClearVRMessageTypes.Warning, ClearVRMessageCodes.GenericWarning, "Stop request received while initializing player.", ClearVRResult.Failure)));
					if(_suspendResumeState != null) {
						if(_mediaPlayerInterface != null) { // Should always be true
							switch(_mediaPlayerInterface.GetPlatformOptions().applicationFocusAndPauseHandling) {
								case ApplicationFocusAndPauseHandlingTypes.Legacy:
									break; // do nothing
								case ApplicationFocusAndPauseHandlingTypes.Recommended:
									return; // According to the spec, we do not forward the StateChangedStopping to the application in Recommended mode
								case ApplicationFocusAndPauseHandlingTypes.Disabled:
									break; // do nothing
							}
						} else {
							// This shouldn't happen
						}
					}
					break;
				case ClearVREventTypes.StateChangedStopped:
					if(_internalInterface != null) {
						if(!_internalInterface._GetIsPlayerShuttingDown()) {
							// This indicates a sudden, unexpected stop from the Core and/or Wrapper. We need to force a quick clean-up before we continue (as part of #2710 and #2409)
							_internalInterface._Stop(true);
							// We delay the StateChangedStopped event propagation by one VSync, allowing for the _Stop() call to be executed, including calling IssuePluginEvent.Destroy()
							// By contract, if _internalInterface != null, argMediaPlayer != null as well.
							argMediaPlayer.ScheduleClearVREventFirst(argClearVREvent);
							return;
						}
					}
					if(_suspendResumeState != null) {
						if(_mediaPlayerInterface != null) { // Should always be true
							// By this time, the application is actually already resumed and we can schedule playback resume.
							switch(_mediaPlayerInterface.GetPlatformOptions().applicationFocusAndPauseHandling) {
								case ApplicationFocusAndPauseHandlingTypes.Legacy:
									break; // do nothing
								case ApplicationFocusAndPauseHandlingTypes.Recommended:
									_mainThreadActionsLinkedList.AddLast(new MainThreadAction(MainThreadActionTypes.ResumePlaybackAfterApplicationRegainedFocus, null));
									return; // According to the spec, we do not forward the StateChangedStopping to the application in Recommended mode
								case ApplicationFocusAndPauseHandlingTypes.Disabled:
									break; // do nothing
							}
						} else {
							// This shouldn't happen
						}
					}
					break;
				/* Non-state change related events */
				case ClearVREventTypes.ParsingMediaInfo:
					break;
				case ClearVREventTypes.MediaInfoParsed:
					break;
				case ClearVREventTypes.AudioTrackSwitched:
					break;
				case ClearVREventTypes.ContentSwitched:
					break;
				case ClearVREventTypes.UnableToInitializePlayer:
					if(argClearVREvent.message.GetIsFatalError()) {
						// If it is a FatalError, all hope is lost on a successful ClearVRPlayerInitialize.
						if(FindPendingClearVRAsyncRequestByRequestTypeAndTriggerCallback(RequestTypes.ClearVRPlayerInitialize, argClearVREvent)) {
							if(ShouldReturnAfterScheduleForceStopAfterFatalError(argMediaPlayer, null /* We pass null as we do not want to push the ClearVREvent to the top of the queue. as it has already been handled */)) {
								return;
							} else {
								// do nothing, forward FatalError
							}
						} // else: we fallthrough and reach the point below where we check for FatalError as well.
					} else {
						// There is still hope, so we continue
					}
					break;
				case ClearVREventTypes.SuspendingPlaybackAfterApplicationLostFocus:
					break;
				case ClearVREventTypes.ResumingPlaybackAfterApplicationRegainedFocus:
					break;
				case ClearVREventTypes.StereoModeSwitched:
					break;
				case ClearVREventTypes.ActiveTracksChanged:
                    argClearVREvent.message.ParseClearVRCoreWrapperActiveTracksChanged(out _contentInfo); // We ignore the return value and assume success.
					break;
				case ClearVREventTypes.AudioFocusChanged:
					// Fix for #4996 - No audio after audio interruption.
					if (controller == null || mediaPlayer == null) {
						break;
					}
					switch (this.platformOptions.audioFocusChangedHandlingType) {
						case AudioFocusChangedHandlingTypes.Recommended:
							switch (argClearVREvent.message.GetClearVRMessageCode()) {
								// The audio focus was lost here, meaning some other app has taken control of audio playback.
								// We will pause and wait for the callback that audio focus has been regained.
								case ClearVRMessageCodes.ClearVRCoreWrapperAudioFocusLost:
									// Check if we're in playing state so we can pause, we don't want to pause if we're not.
									// Could be that we're in a suspend resume and we don't want to pause if that's the case.
									if (mediaPlayer.GetCanPerformanceMetricesBeQueried()) {
										_wasPausedBeforeAudioFocusLost = true;
										controller.Pause(
											onSuccess: null,
											onFailure: null
										);
									}
									break;
								case ClearVRMessageCodes.ClearVRCoreWrapperAudioFocusGained:
									// The audio focus has been regained. meaning we can once again play audio.
									// Make sure we're paused from the AudioFocusLost event, otherwise we're in a suspend resume.
									if (_wasPausedBeforeAudioFocusLost) {
										_wasPausedBeforeAudioFocusLost = false;
										controller.Unpause(
											onSuccess: null,
											onFailure: null
										);
									}
									break;
								default:
									UnityEngine.Debug.LogWarning(String.Format("[ClearVR] AudioFocusChanged event returned with wrong message code: {0}.", argClearVREvent.message.GetClearVRMessageCode()));
									break;
							}
							break;
						default:
							UnityEngine.Debug.LogWarning(String.Format("[ClearVR] Cannot handle AudioFocusChangedHandlingType {0}. Not implemented. Please report this issue to Tiledmedia.", platformOptions.audioFocusChangedHandlingType));
							break;
					}
					break;
				case ClearVREventTypes.CallCoreCompleted:
					break;
				case ClearVREventTypes.GenericMessage:
					break;
				default:
					break;
			}
			switch(argClearVREvent.message.type) {
				case ClearVRMessageTypes.FatalError:
					// When we run into a FatalError, we can not rely on the application to properly handle Stop().
					// Therefor, we schedule this call to be serviced on the main thread the next Vsync.
					// This gives us ample time to IssuePluginEvent.Destroy our NativeRendererPlugin before we unload it through CVR_NRP_Unload().
					if(ShouldReturnAfterScheduleForceStopAfterFatalError(argMediaPlayer,argClearVREvent)) {
						return; // Here we return as the event will be forwarded the next iteration.
					} else {
						// do nothing, forward FatalError
					}
					break;
				case ClearVRMessageTypes.Warning:
					break; // intentionally ignored
				case ClearVRMessageTypes.Info:
					/* Parse harmless info messages */
                    switch (argClearVREvent.message.code) {
						case (int) ClearVRMessageCodes.ClearVRCoreWrapperGenericInfo:
							break; // intentionally ignored
						case (int) ClearVRMessageCodes.ClearVRCorWrapperOpenGLVersionInfo:
							break; // intentionally ignored
						case (int) ClearVRMessageCodes.ClearVRCoreWrapperVideoDecoderCapabilities:
							break; // intentionally ignored
						case (int) ClearVRMessageCodes.ClearVRCoreWrapperGenericOK:
							break; // intentionally ignored
						case (int) ClearVRMessageCodes.ClearVRCoreWrapperAudioTrackChanged:
							break; // intentionally ignored
						case (int) ClearVRMessageCodes.ClearVRCoreWrapperStereoscopicModeChanged:
							// This needs to be handled on the Main Unity Thread
							bool isSuccess = (argClearVREvent.message.result == ClearVRResult.Success);
							if(platformOptions.enableAutomaticRenderModeSwitching) {
								if(isSuccess) {
									switch(argClearVREvent.message.message){
										case "stereo":
										case "stereoscopic":
											_clearVRLayoutManager.SetRenderModeOnAllDisplayObjects(RenderModes.Stereoscopic);
											break;
										case "mono":
										case "monoscopic":
											_clearVRLayoutManager.SetRenderModeOnAllDisplayObjects(RenderModes.Stereoscopic);
											break;
										default:
											UnityEngine.Debug.LogWarning(String.Format("[ClearVR] Got an unexpected value when switching stereo mode. Got: {0}, allowed: 'mono' and 'stereo'.", argClearVREvent.message.message));
											break;
									}
								} else {
									UnityEngine.Debug.LogWarning(String.Format("[ClearVR] Unable to change stereo mode. Error: {0}.", argClearVREvent.message.message));
								}
							}// else, the application has to do the hard work himself.
							break;
						case (int) ClearVRMessageCodes.ClearVRCoreWrapperSubtitle: 
							if (argClearVREvent.message.ParseClearVRSubtitle(out ClearVRSubtitle clearVRSubtitle)) {
								var docs = _clearVRLayoutManager.GetClearVRDisplayObjectControllersByActiveFeedIndex(clearVRSubtitle.GetFeedIndex());
								foreach (var doc in docs) {
									CbClearVRDisplayObjectEvent(doc, new ClearVRDisplayObjectEvent(ClearVRDisplayObjectEventTypes.Subtitle, argClearVREvent.message));
								}
							} else {
								UnityEngine.Debug.LogWarning(String.Format("[ClearVR] Unable to parse ClearVREvent {0} as Subtitle event. Check the logs.", argClearVREvent));
							}
							return;
						default:
							break;
					}
					break;
			}
			// When we reach StateChangedStopped / receive a response to our Stop request we want to destroy the media player _before_ we trigger that event/response.
			// Destroying the mediaplayer means that all its interfaces are nullified, see DestroymediaPlayer() and Reset().
			// Pre-v8.0.7 SDKs nullified the interfaces and released the lock on the NRP (CVR_NRP_Unload()) upon destructing the ClearVRPlayer GameObject (see OnDestroy() below).
			// This is, however, too late as the application could already start a new player in the event / response handler.
			//
			// Note that we cannot call DestroyMediaPlayer() here as it would nullify the mediaplayer interfaces which are still needed for the request -> response look-up.
			// That's why we do that inside FindAndForwardClearVREventAsClearVRAsyncRequestResponse() when handling a response and down-below when handling an event.
			if (argClearVREvent.type == ClearVREventTypes.StateChangedStopped) {
				// We CANNOT call Reset() nor DestroyMediaPlayer() here!
				if(_clearVRLayoutManager != null) {
					_clearVRLayoutManager.UnloadSync();
				}
				if(_internalInterface != null) {
					_internalInterface.Destroy();
				}
			}

			// Forward event to any listener that may have been subscribed
			_clearVREvents.Invoke(this, argClearVREvent);

			if(argClearVREvent.__clearVRAsyncRequestResponse != null) {
				if(argClearVREvent.__clearVRAsyncRequestResponse.requestId != ClearVRAsyncRequest.CLEAR_VR_REQUEST_ID_NOT_SET) {
					// Possible return values:
					// 0: no matching request found
					// 1: matching request with Action<> callback found and triggered
					// 2: matching request without Action<> callback found.
					int returnCode = FindAndForwardClearVREventAsClearVRAsyncRequestResponse(argClearVREvent);
					if(returnCode == 2 /* returnCode == 2 means that there was a request, but there was no Action<> specified. */) {
						// there are two exception where we do not warn for a response that could not be matched to a request:
						// 1. A response to a Stop "request" can be received even if we didn't ask for it (e.g. when something bad happened). In that case, we don't need this warning and we should just forward the event.
						// 2. We receive a response on am Initialize request (triggered from MediaPlayerBase::PrepareCore()), but PrepareCore does not take any callback as it is a purely internal method.
						if(argClearVREvent.__clearVRAsyncRequestResponse.requestType != RequestTypes.Stop &&
							argClearVREvent.__clearVRAsyncRequestResponse.requestType != RequestTypes.Initialize) {
							UnityEngine.Debug.LogWarning(String.Format("No matching request found for request response. Request id: {0}, type: {1}. Message: {2}", argClearVREvent.__clearVRAsyncRequestResponse.requestId, argClearVREvent.__clearVRAsyncRequestResponse.requestType, argClearVREvent.message.GetFullMessage()));
						}
					}
				}
			}

			if (argClearVREvent.type == ClearVREventTypes.StateChangedStopped) {
				// Here we destroy the interfaces.
				DestroyMediaPlayer();
			}

            if (argClearVREvent.type == ClearVREventTypes.StateChangedStopped) {
				MaybeFinalizeApplicationQuit(false);
            }
        }

		private void CbClearVRDisplayObjectEvent(ClearVRDisplayObjectControllerBase argClearVRDisplayObjectController, ClearVRDisplayObjectEvent argClearVRDisplayObjectEvent) {
			if(_clearVRLayoutManager != null && _clearVRLayoutManager.GetIsLegacyDisplayObjectActive()) {
#pragma warning disable 0618 // Silence deprecated API usage warning
				if(argClearVRDisplayObjectEvent.type == ClearVRDisplayObjectEventTypes.FirstFrameRendered) {
					if(_clearVRLayoutManager != null && _clearVRLayoutManager.mainDisplayObjectController != null) {
						ClearVRLegacyDisplayObjectSupport clearVRLegacyDisplayObjectSupport = _clearVRLayoutManager.mainDisplayObjectController.GetComponent<ClearVRLegacyDisplayObjectSupport>();
						if(clearVRLegacyDisplayObjectSupport != null) {
							// If the InteractionMode has changed, or if the InteractionMode is Recilinear or Planar, we update the position of the displayobject (ref. #5548)
							if(clearVRLegacyDisplayObjectSupport.recommendedInteractionMode != _clearVRLayoutManager.mainDisplayObjectController.clearVRMeshType.GetAsInteractionMode() ||
									clearVRLegacyDisplayObjectSupport.recommendedInteractionMode == InteractionModes.Rectilinear ||
									clearVRLegacyDisplayObjectSupport.recommendedInteractionMode == InteractionModes.Planar) {
								clearVRLegacyDisplayObjectSupport.recommendedInteractionMode = _clearVRLayoutManager.mainDisplayObjectController.clearVRMeshType.GetAsInteractionMode();
								switch (platformOptions.cameraAndContentPlacementMode) {
									case CameraAndContentPlacementModes.Disabled:
										//The SDK does not manage the content and camera placement
										break;
									case CameraAndContentPlacementModes.Default:
										// Illegal value, already protected when we verified the platformOptions.
										break;
									case CameraAndContentPlacementModes.MoveDisplayObjectAndIgnoreCamera:
									case CameraAndContentPlacementModes.MoveDisplayObjectResetCamera:
										//Apply the default camera and content placement strategy
										clearVRLegacyDisplayObjectSupport.ResetViewportAndDisplayObjectToDefaultPoses(platformOptions);
										break;
									default:
										throw new Exception("[ClearVR] Unknown CameraAndContentPlacementModes set in the PlatformOption");
								}
							}
						}
					}
				}

				// We forward the event to the main event handler for backwards compatibility.
				ClearVREventTypes clearVREventType = ClearVREventTypes.None;
				switch(argClearVRDisplayObjectEvent.type) {
					case ClearVRDisplayObjectEventTypes.FirstFrameRendered:
						clearVREventType = ClearVREventTypes.FirstFrameRendered;
						break;
					case ClearVRDisplayObjectEventTypes.ContentFormatChanged:
						clearVREventType = ClearVREventTypes.ContentFormatChanged;
						break;
					case ClearVRDisplayObjectEventTypes.RenderModeChanged:
						clearVREventType = ClearVREventTypes.RenderModeChanged;
						break;
					default:
						break;
				}
#pragma warning restore 0618 // Silence deprecated API usage warning
				if(clearVREventType != ClearVREventTypes.None) {
					ClearVREvent clearVREvent = new ClearVREvent(clearVREventType, argClearVRDisplayObjectEvent.message);
					CbClearVREvent(null /* we get away with passing Null as MediaPlayer since it is not used in the FirstFrameRendered code path */, clearVREvent);
				}
			}
			_clearVRDisplayObjectEvents.Invoke(this, argClearVRDisplayObjectController, argClearVRDisplayObjectEvent);
		}

		/// <summary>
		/// This is called from the main thread.
		/// </summary>
		private void ResumePlaybackAfterApplicationRegainedFocus() {
			if (_suspendResumeState != null) {
				PlatformOptionsBase platformOptions = _mediaPlayerInterface.GetPlatformOptions();
				AudioTrackAndPlaybackParameters restoredAtapp = _suspendResumeState.audioTrackAndPlaybackParameters.Clone();
				ClearVRAsyncRequest clearVRAsyncRequestToReinject = _suspendResumeState.clearVRAsyncRequest;
				platformOptions.muteState = _suspendResumeState.muteState;
				// MuteState is the aggregation of: 
				// * muted or unmuted
				// * the current gain (in case of unmuted) OR
				// * the gain prior to muting (in case of muted).
				// Note that the audio gain, as found on AudioTrackAndPlaybackParameters, will be blatently ignored by the MediaFlows. The MuteState trumps it.
				if(platformOptions.muteState >= 1 && platformOptions.muteState <= 2) {
					// not muted
					platformOptions.initialAudioGain = platformOptions.muteState - 1;
				} else if(platformOptions.muteState >= -2 && platformOptions.muteState <= -1) {
					// muted
					platformOptions.initialAudioGain = platformOptions.muteState + 2;
				}
				
				// Do not move the following line around. It MUST be nulled at this spot. Other pieces of code rely on it.
				_suspendResumeState = null; // Nuke it here, so that we do not get bothered by it when suddenly stopping during initialization (otherwise you would get stuck in stopping)

				// Reset the mediaPlayer
				Reset();
				// Restart player
				_isStoppingStateTriggered = false;
				_triggerResumingPlaybackAfterApplicationRegainedFocusEvent = true; // Raise flag so that we trigger the ResumingPlaybackAfterApplicationRegainedFocus event as soon as playback resumes (in _Initialize() above)
				_forceAutoPlayAfterContentLoadCompleted = _wasPausedWhenHeadsetWasTakenOffOrFocusWasLost != 0 ? true : false ; // set to true to automatically start playback only if the player was explicitly paused or unpaused.
				PrepareContentParameters prepareContentParameters ;
				ContentInfo contentInfo;
				if(!LoadState(_clearVRLayoutManager, out prepareContentParameters, out contentInfo)) {
					// This happens when the app was suspended before we called ClearVRCore.Initialize(). Ref. #5289
					prepareContentParameters = platformOptions.prepareContentParameters; // Guaranteed to be never null.
					// We do not cache ContentInfo, so we have to assume it is really not there.
				}
				if(prepareContentParameters != null){
					platformOptions.prepareContentParameters = new PrepareContentParameters(
						prepareContentParameters.contentItem,
						prepareContentParameters.timingParameters,
						prepareContentParameters.layoutParameters);
					platformOptions.prepareContentParameters.audioTrackAndPlaybackParameters = restoredAtapp;
					platformOptions.prepareContentParameters.syncSettings = prepareContentParameters.syncSettings;
					platformOptions.prepareContentParameters.timeoutInMilliseconds = prepareContentParameters.timeoutInMilliseconds;
					platformOptions.prepareContentParameters.approximateDistanceFromLiveEdgeInMilliseconds = prepareContentParameters.approximateDistanceFromLiveEdgeInMilliseconds;
					if (applicationRegainedFocusDelegate != null) {
						// By contract, old requests will NOT be finalized, see also the documentation of ClearVRPlayer.clearVRApplicationRegainedFocusDelegate above.
						// Hence, we completely ignore the clearVRAsyncRequestToReinject in this code path. It was purged from the pending list when we null-ed the interafces in Reset() above.
						// Forward the loadState call to all listeners.
						applicationRegainedFocusDelegate.Invoke(this, platformOptions, contentInfo);
						// We return if the app subscribed on the callback because the next step will be made on app level.
						return;
					}
					// We do our own initialize if the applicationRegainedFocusDelegate is not set on app level.
					// Restart player
					ClearVRAsyncRequest clearVRAsyncRequest = _Initialize(platformOptions);
					if(clearVRAsyncRequestToReinject != null) {
						// Overwrite old requestId from previous session with new requestId
						clearVRAsyncRequestToReinject.requestId = clearVRAsyncRequest.requestId;
						_internalInterface.ScheduleClearVRAsyncRequest(clearVRAsyncRequestToReinject, clearVRAsyncRequestToReinject.onSuccess, clearVRAsyncRequestToReinject.onFailure, clearVRAsyncRequestToReinject.optionalArguments);
					}
				} else {
					// Just in case this is null
					Debug.LogError("[ClearVR] Unable to initialize player from previous state and platformOptions.prepareContentParameters == null. Be sure to not change your platformOptions after you've called clearVRPlayer.Initialize().");
					// FIX #6110. Note that at this point, Reset() was called so all interfaces are null.
					CbClearVREvent(null, ClearVREvent.GetGenericOKEvent(ClearVREventTypes.StateChangedStopped));
					return;
				}
			} // else: nothing to be done.
		}

		/// <summary>
		/// This method will look for a pending ClearVRAsyncRequest of type RequestTypes.ClearVRPlayerInitialize and return it.
		/// Remember that there will be only max one of such ClearVRAsyncRequest at any point in time.
		/// </summary>
		/// <returns>The matching ClearVRAsyncRequest if found, null otherwise.</returns>
		private ClearVRAsyncRequest MaybeFindPendingInitializeClearVRAsyncRequest() {
			int count = _internalInterface.clearVRAsyncRequests.Count;
			for(int i = 0; i < count; i++) {
				ClearVRAsyncRequest clearVRAsyncRequest = _internalInterface.clearVRAsyncRequests[i];
				if(clearVRAsyncRequest.requestType == RequestTypes.ClearVRPlayerInitialize) {
					return clearVRAsyncRequest;
				}
			}
			return null;
		}

		/// <summary>
		/// Find and forwards ClearVREvent as ClearVRAsyncRequestResponse if an Action was specified when firing the Request.false If no Action was specified in the Request, this method will not trigger any callback and return 2.
		/// </summary>
		/// <param name="argClearVREvent"></param>
		/// <param name="argClearAsyncRequest"></param>
		/// <returns>0 if no matching ClearVRAsyncRequest could be found, 1 if a matching ClearVRAsyncRequest was found with Action<> specified and 2 if a matching ClearVRAsyncRequest was found without Action<> specified.</returns>
		int FindAndForwardClearVREventAsClearVRAsyncRequestResponse(ClearVREvent argClearVREvent, ClearVRAsyncRequest argClearAsyncRequest = null) {
			int returnCode = 0;
			ClearVRAsyncRequest clearVRAsyncRequest = argClearAsyncRequest;
			if(clearVRAsyncRequest == null) {
				int count = _internalInterface.clearVRAsyncRequests.Count;
				for(int i = 0; i < count; i++) {
					clearVRAsyncRequest = _internalInterface.clearVRAsyncRequests[i];
					if(clearVRAsyncRequest.requestId == argClearVREvent.__clearVRAsyncRequestResponse.requestId) {
						returnCode = 1;
						break;
					}
				}
				if(returnCode == 0) {
					clearVRAsyncRequest = argClearAsyncRequest; // reset to original value.
				}
			} else {
				returnCode = 1;
			}
			if(returnCode == 1) {
				argClearVREvent.__clearVRAsyncRequestResponse.optionalArguments = clearVRAsyncRequest.optionalArguments;
				_internalInterface.clearVRAsyncRequests.Remove(clearVRAsyncRequest); // First remove from queue, then trigger callback (FIX #4096).
				if(argClearVREvent.message.GetIsSuccess()){
					if(clearVRAsyncRequest.onSuccess != null) {
						clearVRAsyncRequest.onSuccess(argClearVREvent, this);
					} else {
						// We found a matching Request, but there was no Action<> specified (e.g. it was set to null) to be fired upon receiving a response.
						// This means that we have to consider our Request to be serviced, but we need to forward the ClearVREvent to the player-level event handler.
						returnCode = 2;
					}
				} else {
					if(clearVRAsyncRequest.onFailure != null) {
						clearVRAsyncRequest.onFailure(argClearVREvent, this);
					} else {
						// We found a matching Request, but there was no Action<> specified (e.g. it was set to null) to be fired upon receiving a response.
						// This means that we have to consider our Request to be serviced, but we need to forward the ClearVREvent to the player-level event handler.
						returnCode = 2;
					}
				}
			}
			return returnCode;
		}

		bool FindPendingClearVRAsyncRequestByRequestTypeAndTriggerCallback(RequestTypes argRequestType, ClearVREvent argClearVREvent) {
			// Check if we are in an app suspend/resume cycle. In that case, we will not trigger the RequestTypes.ClearVRPlayerInitialize callback.
			if(_suspendResumeState != null && argRequestType == RequestTypes.ClearVRPlayerInitialize) {
				// We are in an app suspend-resume cycle, we should not forward the ClearVRPlayerInitialize completion yet.
				return false;
			}
			if(_internalInterface == null) {
				// Player already stopped, we ignore this request.
				return false;
			}
			ClearVRAsyncRequest clearVRAsyncRequest;
			for(int i = 0; i < _internalInterface.clearVRAsyncRequests.Count; i++) {
				clearVRAsyncRequest = _internalInterface.clearVRAsyncRequests[i];
				if(clearVRAsyncRequest.requestType == argRequestType) {
					FindAndForwardClearVREventAsClearVRAsyncRequestResponse(new ClearVREvent(argClearVREvent.type, new ClearVRAsyncRequestResponse(argRequestType, clearVRAsyncRequest.requestId), argClearVREvent.message), clearVRAsyncRequest);
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Triggered as soon as the application looses or regains focus. Note that loss of focus typically happens when a pop-up modal surfaces.
		/// Application pause, on the other hand, is triggered if the app is pushed to the background.
		/// </summary>
		/// <param name="argHasFocus"></param>
		void OnApplicationFocus(bool argHasFocus) {
            //UnityEngine.Debug.Log("OnApplicationFocus: " + argHasFocus + ", _appPauseAndFocusState: " + _appPauseAndFocusState);
            // TODO: Port to PC.
            _appPauseAndFocusState = _appPauseAndFocusState ^ (int) AppPauseAndFocusState.FocusOrNoFocus;
			if(_mediaPlayerInterface != null) {
				switch(_mediaPlayerInterface.GetPlatformOptions().applicationFocusAndPauseHandling) {
					case ApplicationFocusAndPauseHandlingTypes.Legacy:
                        /*
						In case the application looses focus we have to at least pause playback, otherwise the application
						would keep draining our battery in the background. Note that even when in paused, our
						ClearVR	library still consumes some resources! This is more of a work-around than a proper solution!
						 */
#if (UNITY_ANDROID || UNITY_IOS || UNITY_TVOS) && !UNITY_EDITOR
						OnApplicationPause(!argHasFocus);
#endif
						break;
					case ApplicationFocusAndPauseHandlingTypes.Recommended:
                        /*
						This will completely destroy the player object, freeing any resources that might have been claimed
						 */
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
                        PausePlaybackAfterAppFocusOrPauseChangedRecommended(argHasFocus);
                        // On windows we rely on OnApplicationPause to be triggered.
#endif
                        break;
					case ApplicationFocusAndPauseHandlingTypes.Disabled:
					default:
						break;
				}
			}
		}

		/// <summary>
		/// Triggered when the application is pushed to the background. Please read the note at OnApplicationFocus() for more details.
		/// </summary>
		void OnApplicationPause(bool argPauseStatus) {
            _appPauseAndFocusState = _appPauseAndFocusState ^ (int) AppPauseAndFocusState.UnpausedOrPaused;
            // ClearVRLogger.LOGI("OnApplicationPause: " + argPauseStatus + ", _appPauseAndFocusState: " + _appPauseAndFocusState);

            if (_mediaPlayerInterface == null) {
				return;
			}
			switch(_mediaPlayerInterface.GetPlatformOptions().applicationFocusAndPauseHandling) {
				case ApplicationFocusAndPauseHandlingTypes.Legacy:
					PauseOrUnpausePlaybackAfterAppFocusOrPauseChangedLegacy(argPauseStatus);
					break;
				case ApplicationFocusAndPauseHandlingTypes.Recommended:
                    if (argPauseStatus && _suspendResumeState == null) {
						SaveState();
						ClearVRAsyncRequest clearVRAsyncRequest = MaybeFindPendingInitializeClearVRAsyncRequest();
						_suspendResumeState = new SuspendResumeState(
							_internalInterface.GetAudioTrackAndPlaybackParameters() != null ?
								/* Note that this aduio gain will be ignored by the media flows, we rely on the MuteState instead */
								_internalInterface.GetAudioTrackAndPlaybackParameters().Clone(controller != null ?
										controller.GetAudioGain() :
										platformOptions.initialAudioGain) :
								platformOptions.prepareContentParameters.audioTrackAndPlaybackParameters,
							_internalInterface != null ? _internalInterface.GetMuteState() : platformOptions.muteState,
							clearVRAsyncRequest);
						// We trigger the event here, as by the time we reach StateChangedStopping the event is no longer reaching the app before the app goes to sleep.
						// This is also the most logical spot, as we trigger the event *just* before we are going to stop playback.
						_clearVREvents.Invoke(this, ClearVREvent.GetGenericOKEvent(ClearVREventTypes.SuspendingPlaybackAfterApplicationLostFocus));
						_internalInterface.StopInternal();
                    } else {
						// Cannot happen
					}
					break;
				case ApplicationFocusAndPauseHandlingTypes.Disabled:
				default:
					break;
			}
		}

		void PausePlaybackAfterAppFocusOrPauseChangedRecommended(bool argValue) {
            if (_mediaPlayerInterface == null || _mediaControllerInterface == null || _internalInterface == null || (_appPauseAndFocusState & (int)AppPauseAndFocusState.UnpausedOrPaused) == 0) {
				return;
			}
			if(!argValue) {
				// We lost focus
				bool isInStateThatAllowsPausing = _internalInterface.GetIsPauseAllowed();
				bool isPaused = _mediaPlayerInterface.GetIsInPausingOrPausedState();
				if(isInStateThatAllowsPausing) {
					_wasPausedWhenHeadsetWasTakenOffOrFocusWasLost = isPaused ? 1 : 2;
					if(!isPaused) {
                        _mediaControllerInterface.Pause(onSuccess: null, onFailure: null);
					} else{
                        // already paused
                    }
				} else {
					// Not playing
					_wasPausedWhenHeadsetWasTakenOffOrFocusWasLost = 0;
				}
			} else {
				// regained focus, only triggered when we are still alive
				if(applicationUnpausedDelegate != null) {
					if(_wasPausedWhenHeadsetWasTakenOffOrFocusWasLost != 0 /* 0 means there was nothing playing */) {
						applicationUnpausedDelegate(this, platformOptions, _contentInfo, (_wasPausedWhenHeadsetWasTakenOffOrFocusWasLost == 1));
					}
					// Explicit fallthrough to setting _wasPausedWhenHeadsetWasTakenOffOrFocusWasLost = 0 down below.
				} else  {
					if(_wasPausedWhenHeadsetWasTakenOffOrFocusWasLost == 2) {
						_mediaControllerInterface.Unpause(onSuccess: null, onFailure: null);
					}
				}
				_wasPausedWhenHeadsetWasTakenOffOrFocusWasLost = 0;
			}
		}

		void PauseOrUnpausePlaybackAfterAppFocusOrPauseChangedLegacy(bool argValue) {
            if (_mediaPlayerInterface == null || _mediaControllerInterface == null || _internalInterface == null)
				return;
			if(argValue) { // true = we should pause, false = we should unpause.
				bool isInStateThatAllowsPausing = _internalInterface.GetIsPauseAllowed();
				bool isPaused = _mediaPlayerInterface.GetIsInPausingOrPausedState();
				if(isInStateThatAllowsPausing) {
					if(_wasPausedWhenHeadsetWasTakenOffOrFocusWasLost == 0) {
						_wasPausedWhenHeadsetWasTakenOffOrFocusWasLost = isPaused ? 1 : 2;
                        if (!isPaused) {
                            _mediaControllerInterface.Pause(onSuccess: null, onFailure: null);
						} else {
							// already paused
						}
					} else {
						// no need to pause again.
					}
				} else {
					_wasPausedWhenHeadsetWasTakenOffOrFocusWasLost = 0;
				}
			} else {
				if(_wasPausedWhenHeadsetWasTakenOffOrFocusWasLost == 2) {
					// We give it a little bit of delay so that Unity and (optionally) the OVR stack can resume
					// If we would unpause right away, some video frames could get dropped.
					StartCoroutine(DelayedUnpause(0.35f));
				}
				_wasPausedWhenHeadsetWasTakenOffOrFocusWasLost = 0;
			}
		}

		private IEnumerator DelayedUnpause(float argDelayInSeconds) {
			yield return new WaitForSeconds(argDelayInSeconds);
			_mediaControllerInterface.Unpause(
				onSuccess: null,
				onFailure: null
			);
		}

		/// <summary>
        /// Fetch a COPY of the LayoutParameters with the specified name. This API can return null in case no such Layout is found.
		/// > [!WARNING]
		/// > Note that this API returns a COPY of the LayoutParameters as specified on the LayoutManager. Any changes you make to it will not be reflected on the LayoutManager _until_ you call `clearVRPlayer.AddOrUpdateLayoutParameters()` OR you pass your LayoutParameters into `PrepareContentParameters` or `SwitchContentParameters`.
        /// </summary>
        /// <param name="argName">The name of the Layout to fetch</param>
        /// <returns>A COPY of the LayoutParameters as specified on the LayoutManager, or null if no such named Layout is configured on the ClearVRLayoutManager</returns>
		public LayoutParameters GetLayoutParametersByName(String argName) {
			LayoutParameters noCopy = _clearVRLayoutManager.GetLayoutParametersByNameNoCopy(argName);
			if(noCopy == null) {
				UnityEngine.Debug.LogWarning(String.Format("[ClearVR] Unable to find Layout Parameters '{0}' in the Layout Manager. Playback might break, please review your code.", argName));
			}
			return noCopy != null ? noCopy.Clone() : null;
		}

		/// <summary>
		/// Adds a new, or updates an existing, [LayoutParameters](xref:com.tiledmedia.clearvr.LayoutParameters) set on the LayoutManager. Remember that the name of the LayoutParameters functions as a unique identifier.
		/// </summary>
		/// <param name="argLayoutParameters">The LayoutParameters to add or update on the LayoutManager.</param>
		/// <returns>True in case the LayoutParameters are correct and added or updated on the LayoutManager or if the argument is null. False will be returned in any other case. </returns>
		public bool AddOrUpdateLayoutParameters(LayoutParameters argLayoutParameters) {
			if(argLayoutParameters == null) {
				return true;
			}
			if(_clearVRLayoutManager != null) {
				return _clearVRLayoutManager.AddOrUpdateAndVerifyLayoutParameters(argLayoutParameters);
			}
			return false;
		}

		/// <summary>
		/// Remove
		/// </summary>
		/// <param name="argLayoutParameters">The LayoutParameters to remove</param>
		/// <returns>True in case of success, false otherwise. If the argument is null, true will be returned.</returns>
		public bool RemoveLayoutParameters(LayoutParameters argLayoutParameters) {
			if(argLayoutParameters == null) {
				return true;
			}
			if(_clearVRLayoutManager != null) {
				return _clearVRLayoutManager.RemoveLayoutParameters(argLayoutParameters);
			}
			return false;
		}

		void ScheduleMainThreadAction(MainThreadActionTypes argMainThreadActionType, System.Object argPayload = null, int argVSyncDelayCount = 0) {
			/* Schedule action to be invoked in the next Update() cycle */
			_mainThreadActionsLinkedList.AddLast(new MainThreadAction(argMainThreadActionType, argPayload, argVSyncDelayCount));
		}

		/// <summary>
		/// Public since v4.1.2
		/// Private since: v9.0
		/// Destroys the internal MediaPlayer object. When destroyed, one is free to create a new mediaplayer by calling clearVRPlayer.Initialize() again.
		/// </summary>
		private void DestroyMediaPlayer() {
			if(!_isInitializeCalled) {
				// Nothing to be done
				return;
			}
			if(_mediaPlayerInterface == null) {
				// Reset() was already called, nothing to be done.
				return;
			}
			if(_mediaPlayerInterface.GetIsInStoppedState()) {
				Reset();
			} else {
				if(_isGameObjectDestroyed) {
					// First call stop before we can destroy the MediaPlayer.
					// Next, in StateChangedStopped state above, we call DestroyMediaPlayer again to do the final clean-up.
					_mediaControllerInterface.Stop(onSuccess: null, onFailure: null);
				} else {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
					_mediaControllerInterface.Stop(onSuccess: null, onFailure: null);
#else
					throw new Exception("[ClearVR] Cannot destroy a mediaPlayer if it is not in Stopped state. Call clearVRPlayer.controller.Stop() first and wait for the ClearVREventTypes.StateChangedStopped event.");
#endif
				}
			}

		}

		/// <summary>
		/// This will give us one more frame to flush any pending AsyncMeshEvents.
		/// This is required to destroy our ClearVRDisplayObjectControllerBase in case a fatal error occured in the ClearVRCore or ClearVRCoreWrapper.
		/// </summary>
		/// <returns></returns>
		void Reset() {
			if(_internalInterface != null) {
				_clearVRLayoutManager.UnloadSync();
				_internalInterface.Destroy();
				_internalInterface.Update();
			}
			if(_clearVRLayoutManager != null) {
				_clearVRLayoutManager.clearVRDisplayObjectEvents.RemoveListener(CbClearVRDisplayObjectEvent);
			}
			_mediaPlayerInterface = null;
			_mediaControllerInterface = null;
			_mediaInfoInterface = null;
			_performanceInterface = null;
			_syncInterface = null;
			_debugInterface = null;
			_internalInterface = null;
			_isInitializeCalled = false;
			_wasPausedBeforeAudioFocusLost = false;
			_isInitializeCalled = false;
			_appPauseAndFocusState = (int)AppPauseAndFocusState.UnpausedOrPaused ^ (int)AppPauseAndFocusState.FocusOrNoFocus;
			_isFatalErrorHandled = false;
			_forceAutoPlayAfterContentLoadCompleted = false;
			_suspendResumeState = null;
			// _isGameObjectDestroyed <-- this value is never reset. Once the ClearVRPlayer has been destroyed, it cannot be revived. You need to create a new one (via gameObject.AddComponent<>())
		}

#if UNITY_EDITOR
        private void EditorModeChanged(PlayModeStateChange argState) {
            if (argState == PlayModeStateChange.ExitingPlayMode) {
				// Always remove listener
				PlayStateNotifier.MaybeUnregisterCallback();
				if(_isInitializeCalled) {
					PlayStateNotifier.editorWantsToStop = true;
					// Remove delegate right-away.
					// We force the editor in isPlaying mode while we Destroy the ClearVRPlayer object.
					EditorApplication.isPlaying = true;
					OnDestroy();
				}
            }
        }
#endif

		/// <summary>
		/// Triggered when application or EditorApplication wants to quit.
		/// When a ClearVRPlayer is active, we hold off actual destruction until the ClearVRPlayer object has successfully reached stopped state.
		/// </summary>
		/// <returns>True if quit should continue, false if we want to interrupt the quit request.</returns>
		private bool ApplicationWantsToQuit() {
			WantsToQuitInterruptNotifier.applicationWantsToQuit = true;
#if UNITY_EDITOR
			// Application wants to quit, so we can disable the PlayStateNotifier.
			PlayStateNotifier.MaybeUnregisterCallback();
#endif
			if(_internalInterface == null) {
				// We are done, so we can remove listener
				WantsToQuitInterruptNotifier.MaybeUnregisterCallback();
				return true;
			} else {
				// By this time we have very limited time available before the parent GameObject gets destroyed by Unity.
				// So little time actually, that sometimes the StateChangedDestroyed is never received.
				// In that case, our final clean-up is never triggered via the regular code path.
				// Therefor, we force final clean-up in MaybeFinalizeApplicationQuit() below.
				DestroyMediaPlayer();
				// When running in stand-alone mode, we create a new game object that will outlive the ClearVRPlayer object.
				// We use this GameObject to offload our asynchronous request to stop the application if a timeout occurs.
#if UNITY_STANDALONE_LINUX || UNITY_STANDALONE_WIN
				 _applicationQuitListenerGO = new GameObject("ApplicationQuitListener");
				_applicationQuitListenerMB = _applicationQuitListenerGO.AddComponent<ApplicationQuitListener>();
#endif
				// At this point holds that the user has asked the editor/application to quit and the ClearVRPlayer has started clean-up.
				// For this request to complete we need to receive StateChangedStopped, but in rare cases this never happens (ref. #4112).
				// To work-around this "deadlock", we schedule a delayed Quit.
				WantsToQuitInterruptNotifier.ScheduleDelayedRelease(ApplicationWantsToQuit, MaybeFinalizeApplicationQuit);
				return false;
			}
		}

		/// <summary>
		/// Triggered internally when finalizing player stop. This will:
		/// 1. finalize the delayed applicationstop if an application quit was scheduled.
		/// 2. stop the in-editor play mode if the editor play mode was stopped by the user.
		/// </summary>
		/// <param name="argIsTriggeredAfterTimeout">Set to true if triggered after a timeout, false if triggered through the ordinary path.</param>
		/// <returns>True if application quit was triggered, false otherwise.</returns>
		private bool MaybeFinalizeApplicationQuit(bool argIsTriggeredAfterTimeout) {
			bool toReturn = WantsToQuitInterruptNotifier.applicationWantsToQuit;
#if UNITY_EDITOR
			EditorApplication.isPlaying = !PlayStateNotifier.editorWantsToStop;
#endif
			if(WantsToQuitInterruptNotifier.applicationWantsToQuit) {
				// At this point we do not care for new wantsToQuit callbacks, so we remove the listener
				WantsToQuitInterruptNotifier.MaybeUnregisterCallback();
				// Here we trigger editor/application quit in two ways:
				// 1. Schedule a quit on the main loop, required if triggered after a timeout.
				// 2. An ordinary quit, which will be handled if this method was called from the main Unity thread after a gracefull full Stop. Note that we the main loop is already finished by this point, hence we call directly.
				if(argIsTriggeredAfterTimeout) {
					// We timed out waiting for StateChangedStopped while quiting the application. Force destruction and clean-up.
					Reset();
#if UNITY_STANDALONE_LINUX || UNITY_STANDALONE_WIN
					// As we are not on the Unity main thread (timeout trigger is fired from a random thread) we have to defer our call on TriggerQuit to the main Unity thread.
					if(_applicationQuitListenerMB != null) {
						_applicationQuitListenerMB.PushMainThreadAction(new MainThreadAction(MainThreadActionTypes.ApplicationOrEditorQuit, null));
					} // else: should never happen
#endif
				} else {
#if UNITY_STANDALONE_LINUX || UNITY_STANDALONE_WIN
					// We are on the main unity thread and no time-out was triggered. We can safely destroy the temporary clean-up GameObject
					if(_applicationQuitListenerGO != null) {
						UnityEngine.Object.Destroy(_applicationQuitListenerGO);
						_applicationQuitListenerGO = null;
					}
#endif
					TriggerQuit();
				}
			}
			return toReturn;
		}

		internal static void TriggerQuit() {
			WantsToQuitInterruptNotifier.MaybeCancelDelayedRelease();
#if UNITY_EDITOR
			UnityEditor.EditorApplication.Exit(0);
#elif UNITY_STANDALONE_LINUX || UNITY_STANDALONE_WIN
			UnityEngine.Application.Quit();
#endif
		}

		void OnDestroy() {
			_isGameObjectDestroyed = true;
			DestroyMediaPlayer();
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
            MediaPlayerPC.UnloadCBridge();
#endif
		}

		public void DestroyGameComponentAndCleanUp() {
			// intentionally left blank for now
		}

		public void OnApplicationQuit() {
			// The application is shutting down, this means that the entire scene will be torn down and all objects destroyed..
			// Rather than destroying the object using UnityEngine.Destroy(this) we trigger the OnDestroy() logic as the appliation might have its own logic.
			OnDestroy();
		}
	}
}
