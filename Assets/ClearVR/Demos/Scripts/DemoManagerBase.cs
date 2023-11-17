using System;
using UnityEngine;

namespace com.tiledmedia.clearvr.demos {
    public abstract class DemoManagerBase : MonoBehaviour {
        protected static DemoManagerBase instance;
        public static DemoManagerBase Instance { get { return instance; } }
        public ClearVRPlayer clearVRPlayer = null; // the ClearVRPlayer currently in use.
        public UserInterfaceBase userInterface = null; // a reference to the user interface script that controls the UI elements

        /// <summary>
        /// This specifies the content list to load. The file is assumed to be stored in Assets/Resources/ if only a file name is specified without folder.  
        /// You can also specify a URL. Note that the file name MUST end with .json
        /// </summary>
        protected string contentListFileName {
            get {
                return _contentListFileName;
            }
            set {
                _contentListFileName = value;
            }
        }

        [SerializeField] private GameObject _subtitleToggleControlGroup;

        [Header("Settings")]
        [SerializeField]
        private string _contentListFileName = "content-list.json";
        
        public abstract string activeLayoutName  {
            get;
        }

        [NonSerialized]
        public ContentItemModel[] contentItemModelList = null; // a list of content items to be loaded
        [NonSerialized]
        public int currentContentItemModelIndex = 0; // the index of the current content item in the content list

        /// <summary>
        /// Determines if we have extra SDK logging turned on
        /// Do note that using this will incur a performance penalty due to all the extensive logging.
        /// </summary>
        public bool enableLogging = false;

        // The current content being played 
        public ContentItemModel currentContentItemModel {
            get {
                if (contentItemModelList != null && contentItemModelList.Length != 0) {
                    return contentItemModelList[currentContentItemModelIndex];
                }
                throw new Exception("[ClearVR] Content list is empty!");
            }
        }
        protected byte[] licenseFileBytes {
            get {
                /* Read license file from default license file folder */
                byte[] _licenseFileBytes = Helpers.ReadLicenseFile();
                if (_licenseFileBytes == null) {
                    throw new Exception("[ClearVR] Unable to read license file!");
                } else {
                    return _licenseFileBytes;
                }
            }
        }
        /// <summary>
        /// Contains all information about the the currently playing audio and video tracks. Can be null.
        /// This object gets updated in ClearVREventTypes::ActiveTracksChanged event, see below.
        /// </summary>
        private ContentInfo contentInfo = null;

        /// <summary>
        /// Create a platform-specific congifuration that the ClearVRPlayer will be running with.
        /// </summary>
        /// <returns>The platfrom options that will configure a ClearVRPlayer</returns>
        protected abstract PlatformOptionsBase CreatePlatformOptions();
        protected virtual void Awake() {
            if (instance != null && instance != this) {
                Destroy(this.gameObject);
            } else {
                instance = this;
            }
            Application.targetFrameRate = 60;
            Debug.Log(String.Format("ClearVR version: {0}", ClearVRPlayer.GetClearVRCoreVersion()));
        }

        /// <summary>
        /// Set-up the scene
        /// </summary>
        protected virtual void Start() {
#if UNITY_ANDROID && !UNITY_EDITOR
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
#endif
            // Asynchronously load the content list.
            Helpers.LoadContent(
                contentListFileName,
                onSuccess: (contentItems) => {
                    InitializeContentItems(contentItems);
                    if (clearVRPlayer != null) {
                        if (userInterface != null && userInterface.isActiveAndEnabled) {
                            userInterface.Initialize(clearVRPlayer, this);
                        }
                        // Start the the ClearVRPlayer initialization
                        PlatformOptionsBase platformOptions = CreatePlatformOptions();

                        InitializeClearVRPlayer(platformOptions);
                    }
                },
                onFailure: (clearVRMessage) => {
                    throw new Exception(string.Format("[ClearVR] {0}", clearVRMessage.GetFullMessage()));
                }
            );
        }

        // <summary>
        // Initialize ClearVR Player using the platform-specific implementation 
        // </summary>
        // <param name="platformOptions">The configuration the ClearVRPlayer will be running with.</param>
        public void InitializeClearVRPlayer(PlatformOptionsBase platformOptions) {
            if (clearVRPlayer != null) {
                // Check if ClearVR is supported on the current platform 
                if (!ClearVRPlayer.GetIsPlatformSupported()) {
                    throw new Exception("[ClearVR] Sorry, ClearVR is not yet supported on this platform!");
                }
                // Add event listener 
                clearVRPlayer.clearVREvents.AddListener(CbClearVREvent);
                clearVRPlayer.clearVRDisplayObjectEvents.AddListener(CbClearVRDisplayObjectEvent);

                if(string.IsNullOrEmpty(activeLayoutName )){
                    throw new Exception("[ClearVR] The layout name is null or empty. Make sure to set it in your application logic accordingly to what content you want layout you want to use.");
                }

               

                // Schedule a request to Initialize a ClearVRPlayer object. Depending on the specified platformOptions, you will receive a callback sooner or later.
                clearVRPlayer.Initialize(platformOptions,
                    onSuccess: (cbClearVREvent, cbClearVRPlayer) =>
                    UnityEngine.Debug.Log("[ClearVR] Player Initialized."),
                    onFailure: (cbClearVREvent, cbClearVRPlayer) =>
                    UnityEngine.Debug.LogWarning(String.Format("[ClearVR] Something went wrong while initializing the ClearVRPlayer! Error Code: {0}; Message: {1} .", cbClearVREvent.message.code, cbClearVREvent.message.message))
                );
            }
        }

        /// <summary>
        /// Parse the content list array and create a list of ContentItemModel objects for future use.
        /// </summary>
        /// <param name="contentItems">The array of ContentItem loaded from disk or web.</param>
        protected virtual void InitializeContentItems(ContentItem[] contentItems) {
            contentItemModelList = new ContentItemModel[contentItems.Length];
            for (int i = 0; i < contentItems.Length; i++) {
                contentItemModelList[i] = new ContentItemModel(String.Format("Video {0}", i), contentItems[i]);
            }
            UnityEngine.Debug.Log(String.Format("[ClearVR] Content list loaded successfully. Read {0} items.", contentItemModelList.Length));
        }

        /// <summary>
        /// Callback function that handles ClearVRPlayer events.
        /// </summary>
        /// <param name="argClearVRPlayer">the ClearVRPlayer instance that fired the event.</param>
        /// <param name="argClearVREvent">the ClearVREvent that was fired.</param>
        private void CbClearVREvent(ClearVRPlayer argClearVRPlayer, ClearVREvent argClearVREvent) {
             argClearVREvent.Print(); // Enable this line to print the events that are received. Can be handy for debugging purposes.
            /* Parse the event */
            switch (argClearVREvent.type) {
                case ClearVREventTypes.StateChangedInitializing:
                    /* Transient state, do not interfere */
                    break;
                case ClearVREventTypes.StateChangedInitialized:
                    /* ClearVRPlayer object will already take care of preparing the core, do not interfere */
                    break;
                    /* Remember that StateChanged* events are ALWAYS successful as the Core will only change state in case of success. */
                case ClearVREventTypes.StateChangedPreparingContentForPlayout:
                    break;
                case ClearVREventTypes.StateChangedContentPreparedForPlayout:
                    /* if platformOptions.autoPlay == true, ClearVRPlayer will make sure that content will start playing automatically next. */
                    break;
                case ClearVREventTypes.StateChangedBuffering:
                    break;
                case ClearVREventTypes.StateChangedPlaying:
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
                    break;
                case ClearVREventTypes.StateChangedStopped:
                    // The player came to a full stop. Remove the CbClearVREvent and CbClearVRDisplayObjectEvent listeners.
                    clearVRPlayer.clearVREvents.RemoveListener(CbClearVREvent);
                    clearVRPlayer.clearVRDisplayObjectEvents.RemoveListener(CbClearVRDisplayObjectEvent);
                    break;
                    /* Events not tied to state changes. These events can fail. */
                case ClearVREventTypes.AudioTrackSwitched:
                    ParseClearVRMessageCallback(argClearVREvent, argClearVRPlayer,
                        onSuccess: (cbClearVREvent, cbClearVRPlayer) => UnityEngine.Debug.Log("[ClearVR] AudioTrack switched."),
                        onFailure: (cbClearVREvent, cbClearVRPlayer) => UnityEngine.Debug.LogWarning(String.Format("[ClearVR] Something went wrong while switching audio tracks! Error Code: {0}; Message: {1} .", cbClearVREvent.message.code, cbClearVREvent.message.message))
                    );
                    break;
                case ClearVREventTypes.ContentSwitched:
                    break;
                case ClearVREventTypes.UnableToInitializePlayer:
                    ParseClearVRMessageCallback(argClearVREvent, argClearVRPlayer);
                    break;
                case ClearVREventTypes.ResumingPlaybackAfterApplicationRegainedFocus:
                    UnityEngine.Debug.Log(String.Format("[ClearVR] Resuming playback after application regained focus again."));
                    if(userInterface!=null && userInterface.playPauseButton!=null){ 
                        var playPauseToggle = userInterface.playPauseButton.GetComponentInParent<UnityEngine.UI.Toggle>();
                        playPauseToggle.isOn = false;
                    } 
                    break;
                case ClearVREventTypes.StereoModeSwitched:
                    ParseClearVRMessageCallback(argClearVREvent, argClearVRPlayer,
                        onSuccess: (cbClearVREvent, cbClearVRPlayer) => UnityEngine.Debug.Log("[ClearVR] Stereo mode switched successfully."),
                        onFailure: (cbClearVREvent, cbClearVRPlayer) => UnityEngine.Debug.LogWarning(String.Format("[ClearVR] Something went wrong while switching stereo mode! Error Code: {0}; Message: {1} .", cbClearVREvent.message.code, cbClearVREvent.message.message))
                    );
                    break;
                case ClearVREventTypes.ActiveTracksChanged:
                    // This event indicates that the active audio/video/subtitle track has changed and will be triggered after content playback has started, for both the first clip and after each SwitchContent.
                    // Furthermore, this is triggered when an ABR event happened.
                    if (argClearVREvent.message.ParseClearVRCoreWrapperActiveTracksChanged(out contentInfo)) {
                        if (_subtitleToggleControlGroup) {
                            _subtitleToggleControlGroup.SetActive(contentInfo.GetNumberOfSelectableSubtitlesTracks() > 0);
                        }
                    } else {
                        UnityEngine.Debug.LogWarning(String.Format("[ClearVR] Unable to parse ClearVREvent {0} as ActiveTracksChanged event. Check the logs.", argClearVREvent));
                        return;
                    }
                    break;
                case ClearVREventTypes.GenericMessage:
                    switch (argClearVREvent.message.type) {
                        case ClearVRMessageTypes.FatalError:
                            UnityEngine.Debug.LogError(String.Format("[ClearVR] Fatal error received. {0}", argClearVREvent.message.GetFullMessage()));
                            break;
                        case ClearVRMessageTypes.Warning:
                            UnityEngine.Debug.LogWarning(String.Format("[ClearVR] Warning received. {0}", argClearVREvent.message.GetFullMessage()));
                            break;
                        case ClearVRMessageTypes.Info:
                            /* Parse harmless info messages */
                            switch (argClearVREvent.message.code) {
                                case (int) ClearVRMessageCodes.ClearVRCoreWrapperGenericInfo:
                                    break;
                                case (int) ClearVRMessageCodes.ClearVRCoreWrapperGenericOK:
                                    break;
                                default:
                                    UnityEngine.Debug.Log(String.Format("[ClearVR] Info message received. {0}", argClearVREvent.message.GetFullMessage()));
                                    break;
                            }
                            break;
                    }
                    break;
                default:
                    break;
            }
        }

        private void CbClearVRDisplayObjectEvent(ClearVRPlayer argClearVRPlayer, ClearVRDisplayObjectControllerBase argClearVRDisplayObjectController, ClearVRDisplayObjectEvent argClearVRDisplayObjectEvent) {
            // WARNING: this callback is called on the main unity thread. Heavy computations or IO operations done here may result in rendering artifacts
            if (argClearVRDisplayObjectEvent.type == ClearVRDisplayObjectEventTypes.Subtitle) {
				if (argClearVRDisplayObjectController.TryGetComponent<demos.ClearVRSubtitleListener>(out demos.ClearVRSubtitleListener subtitleListener)) {
					if (argClearVRDisplayObjectEvent.message.ParseClearVRSubtitle(out ClearVRSubtitle clearVRSubtitle)) {
						subtitleListener.SetText(clearVRSubtitle.GetText());
					} else {
						UnityEngine.Debug.LogWarning(String.Format("[ClearVR] Unable to parse ClearVREvent {0} as Subtitle event. Check the logs.", argClearVRDisplayObjectEvent));	
					}
				}
				return;
			} else if (argClearVRDisplayObjectEvent.type == ClearVRDisplayObjectEventTypes.ActiveStateChanged) {
                // If the DOC was deactivated and it has a subtitleListener component, we actively empty the displayed text
                if (argClearVRDisplayObjectController.TryGetComponent<demos.ClearVRSubtitleListener>(out demos.ClearVRSubtitleListener subtitleListener)) {
                    if (!argClearVRDisplayObjectController.isActive) {
                        subtitleListener.SetText("");
                    }
                }
            }
        }

        /// <summary>
        /// Parse a ClearVRMessageCallback event and execute the appropriate callback. This is just an example of how to use the callback.
        /// </summary>
        /// <param name="onSuccess"> The callback to invoke in case the ClearVRMessage was successful </param>
        /// <param name="onFailure"> The callback to invoke in case the ClearVRMessage was a failure</param>
        public void ParseClearVRMessageCallback(ClearVREvent clearVREvent, ClearVRPlayer clearVRPlayer, Action<ClearVREvent, ClearVRPlayer> onSuccess = null, Action<ClearVREvent, ClearVRPlayer> onFailure = null) {
            if (clearVREvent.message.GetIsSuccess()) {
                if (onSuccess != null) {
                    onSuccess.Invoke(clearVREvent, clearVRPlayer);
                }
            } else if (clearVREvent.message.GetIsFatalError()) {
                if (onFailure != null) {
                    onFailure.Invoke(clearVREvent, clearVRPlayer);
                }
            }
        }

        /// <summary>
        /// Toggle between pause and unpause the active ClearVRPlayer or initializes a new instance in case we previously stopped.
        /// </summary>
        /// <param name="argShouldPause"></param>
        public virtual void TogglePauseUnpausePlayback(bool argShouldPause) {
            if (clearVRPlayer != null && clearVRPlayer.controller != null) {
                if (argShouldPause) {
                    // A player is active right now, try to pause it
                    clearVRPlayer.controller.Pause(
                        onSuccess: (argClearVREvent, argClearVRPlayer) => { },
                        onFailure: (argClearVREvent, argClearVRPlayer) => UnityEngine.Debug.LogWarning(String.Format("[ClearVR] Something went wrong while pausing! Error Code: {0}; Message: {1} .", argClearVREvent.message.code, argClearVREvent.message.message))
                    );
                } else {
                    // A player is active right now, try to unpause it
                    clearVRPlayer.controller.Unpause(
                        onSuccess: (argClearVREvent, argClearVRPlayer) => { },
                        onFailure: (argClearVREvent, argClearVRPlayer) => UnityEngine.Debug.LogWarning(String.Format("[ClearVR] Something went wrong while unpausing! Error Code: {0}; Message: {1} .", argClearVREvent.message.code, argClearVREvent.message.message))
                    );
                }
            } else {
                // No player active, we should create one.
                PlatformOptionsBase platformOptions = CreatePlatformOptions();
                InitializeClearVRPlayer(platformOptions);
            }
        }

        /// <summary>
        /// Stop the active ClearVRPlayer and call the callback function.
        /// </summary>
        public virtual void Stop() {
            if (clearVRPlayer != null && clearVRPlayer.controller != null) {
                clearVRPlayer.controller.Stop(
                    null,
                    null, // No need to manually stop as the ClearVRPlayer is already shutting-down anyway!
                    "Requested to stop playback.", // Just a simple string as the optional args
                    this // by passing this, we can call methods on the parent in the callback 
                );
            }
        }

        /// <summary>
        /// Switch to the previous content item model and call the callback function.
        /// </summary>
        /// <param name="newContentItemModelIndexRelativeToCurrent"> The number of content items to jump (e.g.: +1: next content item; -1: previous content item).</param>
        public virtual void SwitchContentItem(int newContentItemModelIndexRelativeToCurrent) {
            // Select the previous content item
            ContentItemModel nextContentItemModel = SelectContentItemModel(newContentItemModelIndexRelativeToCurrent);
            if(nextContentItemModel == null) {
                // SelectContentItemModel(Int) returns null if there is no next content item to select (e.g. if the list has size 0 or 1).
                return;
            }
            // Specify with TimingType how the new content position in TimingParameters() should be interpreted
            TimingParameters newTimingParams = new TimingParameters(0, TimingTypes.ContentTime);
            // Get the layoutParameters to pass to the SwitchContent call to specify the mosaic object mapping
            LayoutParameters layoutParameters = clearVRPlayer.GetLayoutParametersByName(activeLayoutName );
            SwitchContentParameters newSwitchContentParameters = new SwitchContentParameters(nextContentItemModel.clearVRContentItem, newTimingParams, layoutParameters);
            newSwitchContentParameters.transitionType = TransitionTypes.Fast;
            // To enable sync create custom SyncSettings here (null = sync disabled)
            newSwitchContentParameters.syncSettings = null;
            // Toggle low latency mode  by specifying the approximate distance from the live edge, live content only! (0 = low latency disabled , 1 = enabled) 
            newSwitchContentParameters.approximateDistanceFromLiveEdgeInMilliseconds = 0;
            if (clearVRPlayer != null && clearVRPlayer.controller != null) {
                // The SwitchContentParameters constructor has a number of default arguments that you might want to customize, especially when targetting playback of LIVE streams.
                // By default, this will switch to the provided ContentItem and start playback at th beginning of the clip (VOD) or at the live edge (LIVE)
                clearVRPlayer.controller.SwitchContent(newSwitchContentParameters,
                    onSuccess: (argClearVREvent, argClearVRPlayer) => {
                    String.Format("Switched to content url: {0}", nextContentItemModel.clearVRContentItem.manifestUrl); // optional arguments
                    },
                    onFailure: (argClearVREvent, argClearVRPlayer) => { }, // no
                    String.Format("Unable to switch to content url: {0}", nextContentItemModel.clearVRContentItem.manifestUrl) // optional arguments
                );
            }
        }

        /// <summary>
        /// Select the next content item in the content list.
        /// </summary>
        /// <param name="newContentItemModelIndexRelativeToCurrent">The number of content items to jump (e.g.: +1: next content item; -1: previous content item).</param>
        /// <returns>The next content item in the conten list.</returns>
        public virtual ContentItemModel SelectContentItemModel(int newContentItemModelIndexRelativeToCurrent) {
            ContentItemModel newContentItemModel = null;
            if (contentItemModelList != null) {
                switch (contentItemModelList.Length) {
                    case 0:
                        UnityEngine.Debug.Log(String.Format("[ClearVR] Content list is empty. Cannot switch content."));
                        break;
                    case 1:
                        UnityEngine.Debug.Log(String.Format("[ClearVR] Content list only contains a single clip. Cannot switch content."));
                        break;
                    default:
                        currentContentItemModelIndex += newContentItemModelIndexRelativeToCurrent;
                        if (currentContentItemModelIndex >= contentItemModelList.Length) {
                            currentContentItemModelIndex = 0;
                        } else if (currentContentItemModelIndex < 0) {
                            currentContentItemModelIndex = contentItemModelList.Length - 1;
                        }
                        newContentItemModel = contentItemModelList[currentContentItemModelIndex];
                        break;
                }
            }
            return newContentItemModel;
        }

        // <summary>
        // Seek to the specified time relative to the current time and call the callback function.
        // </summary>
        // <param name="argMilSecondsToSeekTo">Time (ms) to seek to.</param>
        public virtual void SeekWithRelativeTime(long milSecondsToSeekTo) {
            TimingParameters timingParameters = new TimingParameters(milSecondsToSeekTo, TimingTypes.RelativeTime);
            SeekParameters seekParams = new SeekParameters(timingParameters);
            if (clearVRPlayer != null && clearVRPlayer.controller != null) {
                clearVRPlayer.controller.Seek(seekParams,
                    onSuccess: (argClearVREvent, argClearVRPlayer) => { },
                    onFailure: (argClearVREvent, argClearVRPlayer) => { /* no */ },
                    String.Format("Requested seek to {0}.", milSecondsToSeekTo)
                );
            }
        }

        /// <summary>
        /// Convenience method that handles seeking to the live edge of the live clip.
        /// </summary>
        public virtual void SeekToLiveEdge() {
            TimingParameters timingParameters = new TimingParameters(0, TimingTypes.LiveEdge);
            SeekParameters seekParams = new SeekParameters(timingParameters);
            if (clearVRPlayer != null && clearVRPlayer.controller != null) {
                clearVRPlayer.controller.Seek(seekParams,
                    onSuccess: (argClearVREvent, argClearVRPlayer) => {
                        if (userInterface != null) {
                            userInterface.isOnLiveEdge = true;
                            userInterface.mediaSlider.value = userInterface.trackerPositionLiveThreshhold;
                        }
                    },
                    onFailure: (argClearVREvent, argClearVRPlayer) => { },
                    "Requested seek to the live edge."
                );
            }
        }

        /// <summary>
        /// Seek to the specified time in the current content item using the UI tracker.
        /// </summary>
        /// <param name="argProgressBarProgress"></param>
        public virtual void SeekWithProgressBarValue(float argProgressBarProgress) {
            TimingTypes timingType = TimingTypes.ContentTime;
            TimingReport timingReport;
            long milSecondsToSeek = 0;
            if (clearVRPlayer != null && clearVRPlayer.mediaInfo != null && clearVRPlayer.controller != null && userInterface != null && contentInfo != null) {
                if (contentInfo.eventType == EventTypes.Live) {
                    if (argProgressBarProgress >= userInterface.trackerPositionLiveThreshhold) {
                        // In case the user drags the tracker over the progress bar's live threshold we want to seek to the current live edge
                        SeekToLiveEdge();
                        return;
                    } else {
                        timingType = TimingTypes.WallclockTime;
                        timingReport = clearVRPlayer.controller.GetTimingReport(timingType);
                        milSecondsToSeek = timingReport.lowerSeekBoundInMilliseconds + (long) ((timingReport.upperSeekBoundInMilliseconds - timingReport.lowerSeekBoundInMilliseconds) * argProgressBarProgress);
                    }
                } else {
                    timingType = TimingTypes.ContentTime;
                    timingReport = clearVRPlayer.controller.GetTimingReport(timingType);
                    milSecondsToSeek = (long) (timingReport.contentDurationInMilliseconds * argProgressBarProgress);
                }
                TimingParameters timingParameters = new TimingParameters(milSecondsToSeek, timingType);
                SeekParameters seekParams = new SeekParameters(timingParameters);
                clearVRPlayer.controller.Seek(seekParams,
                    onSuccess: (argClearVREvent, argClearVRPlayer) => { },
                    onFailure: (argClearVREvent, argClearVRPlayer) => UnityEngine.Debug.LogWarning(String.Format("[ClearVR] Something went wrong while seeking! Error Code: {0}; Message: {1} .", argClearVREvent.message.code, argClearVREvent.message.message)),
                    String.Format("Requested seek to {0}.", milSecondsToSeek)
                );
            }
        }

        /// <summary>
        /// Switch to the next audio track.
        /// </summary>
        /// <param name="audioTrackIDIndexToJumpToRelativeToCurrent">The number of audio track to jump (e.g.: +1: next audio track; -1: previous audio track).</param>
        public virtual void SwitchAudioTrack(int audioTrackIDIndexToJumpToRelativeToCurrent) {
            if (clearVRPlayer != null && clearVRPlayer.controller != null && contentInfo != null) {
                LayoutParameters layoutParameters = clearVRPlayer.GetLayoutParametersByName(activeLayoutName );
                TrackID currentAudioTrackID = contentInfo.GetActiveAudioTrackID();
                TrackID[] selectableAudioTrackIDs = contentInfo.GetSelectableAudioTrackIDs();
                int currentTrackIDIndex = Array.IndexOf(selectableAudioTrackIDs, currentAudioTrackID);
                int newTrackIDIndex = contentInfo.GetNumberOfSelectableAudioTracks() > 0 ? 0 : -1;
                if (currentTrackIDIndex != -1) {
                    newTrackIDIndex = currentTrackIDIndex + audioTrackIDIndexToJumpToRelativeToCurrent;
                    if (newTrackIDIndex >= selectableAudioTrackIDs.Length) {
                        newTrackIDIndex = 0;
                    } else if (newTrackIDIndex < 0) {
                        newTrackIDIndex = selectableAudioTrackIDs.Length - 1;
                    }
                } // else case already covered above.
                if (newTrackIDIndex != -1) {
                    layoutParameters.audioTrackID = selectableAudioTrackIDs[newTrackIDIndex];
                }
                clearVRPlayer.mediaPlayer.SetLayout(layoutParameters,
                    onSuccess: (argClearVREvent, argClearVRPlayer) => { },
                    onFailure: (argClearVREvent, argClearVRPlayer) => UnityEngine.Debug.LogWarning(String.Format("[ClearVR] Something went wrong while switching audio track! Error Code: {0}; Message: {1} .", argClearVREvent.message.code, argClearVREvent.message.message))
                );
            }
        }

        /// <summary>
        /// Switch to the next subtitles track.
        /// </summary>
        /// <param name="subtitlesTrackIDIndexToJumpToRelativeToCurrent">The number of subtitles track to jump (e.g.: +1: next subtitles track; -1: previous subtitles track).</param>
        public virtual void SwitchSubtitlesTrack(int subtitlesTrackIDIndexToJumpToRelativeToCurrent) {
            if (clearVRPlayer != null && clearVRPlayer.controller != null && contentInfo != null) {
                LayoutParameters layoutParameters = clearVRPlayer.GetLayoutParametersByName(activeLayoutName );
                if (layoutParameters == null) {
                    Debug.LogWarning($"[ClearVR] Unable to obtain layoutParameters by name {activeLayoutName}");
                    return;
                }
                TrackID currentSubtitlesTrackID = contentInfo.GetActiveSubtitleTrackID();
                TrackID[] selectableSubtitlesTrackIDs = contentInfo.GetSelectableSubtitlesTrackIDs();
                int currentTrackIDIndex = Array.IndexOf(selectableSubtitlesTrackIDs, currentSubtitlesTrackID);
                int newTrackIDIndex = contentInfo.GetNumberOfSelectableSubtitlesTracks() > 0 ? 0 : -1;
                if (currentTrackIDIndex != -1) {
                    newTrackIDIndex = currentTrackIDIndex + subtitlesTrackIDIndexToJumpToRelativeToCurrent;
                    if (newTrackIDIndex >= selectableSubtitlesTrackIDs.Length) {
                        newTrackIDIndex = 0;
                    } else if (newTrackIDIndex < 0) {
                        newTrackIDIndex = selectableSubtitlesTrackIDs.Length - 1;
                    }
                } // else case already covered above.
                if (newTrackIDIndex != -1) {
                    layoutParameters.subtitleTrackID = selectableSubtitlesTrackIDs[newTrackIDIndex];
                }
                clearVRPlayer.mediaPlayer.SetLayout(layoutParameters,
                    onSuccess: (argClearVREvent, argClearVRPlayer) => { },
                    onFailure: (argClearVREvent, argClearVRPlayer) => UnityEngine.Debug.LogWarning(String.Format("[ClearVR] Something went wrong while switching subtitles track! Error Code: {0}; Message: {1} .", argClearVREvent.message.code, argClearVREvent.message.message))
                );
            }
        }

        /// <summary>
        /// Called when leaving the scene. Note that the clearVRPlayer MUST be properly stopped by calling its Stop() method at all times to free its resources.
        /// </summary>
        private void OnApplicationQuit() {
            UnityEngine.Debug.Log("[ClearVR] Quiting player");
            Stop();
        }
    }
}
