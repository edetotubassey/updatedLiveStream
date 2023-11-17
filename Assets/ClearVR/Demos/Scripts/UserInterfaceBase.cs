using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR;

namespace com.tiledmedia.clearvr.demos {
    public abstract class UserInterfaceBase : MonoBehaviour {
        [Header("UI Elements")]
        public TextMeshProUGUI statusText; // the text that displays the status of the player.
        public TextMeshProUGUI currentTime; // the current time in the video.
        public Button nextAudioTrackButton; // the button that switches to the next audio track.
        public Button previousAudioTrackButton; // the button that switches to the previous audio track.
        public Image playPauseButton; // the button's image that toggles play/pause.
        public Slider mediaSlider; // the media tracker slider.
        public Image bufferImage; // the buffer image.

        [Header("Graphic Resources")]
        public Sprite pauseButtonImage; // the image to display when the player is playing.
        public Sprite playButtonImage; // the image to display when the player is paused.

        [Header("UI Settings")]
        [Tooltip("How often (seconds) we pull data from the ClearVR player and update the UI accordingly.")]
        public float uiRefreshRateInSeconds = 0.3f;
        [Tooltip("How close to the live threshold we need to be to show the live indicator.")]
        public float trackerPositionLiveThreshhold = 0.9f;
        [Tooltip("Whether or not the user is currently dragging the seek bar.")]
        public bool isDraggingBar = false;

        // Whether the live content currently playing on the live edge
        public bool isOnLiveEdge {
            get { return _isOnLiveEdge; }
            set { if (value) { mediaSlider.value = trackerPositionLiveThreshhold; currentTime.color = Color.red; } else { currentTime.color = Color.gray; } _isOnLiveEdge = value; }
        }

        public ClearVRPlayer clearVRPlayer { get; set; } // the currently active ClearVR player.
        public DemoManagerBase demoManager { get; set; } // the demo manager reference.
        [NonSerialized]
        protected EventTypes contentType = EventTypes.Vod; // the type of content we're playing.
        protected bool _isOnLiveEdge = false; // is the content playing on the live edge

        // Stats and status text
        protected string currentContentPlaying; // the title of current content playing.
        protected string clearVRPlayerStatusString; // the status of the ClearVR player (Running, Idle, Buffering, etc.).
        protected string averageBitrateInMegabitPerSecondString; // the average bitrate of the current content.
        protected string currentContentTimeText; // the current playback time.
        protected string isMonoOrStereoString; // whether the current content is being played as mono or stereo.
        protected string currentQualityResolutionAndFramerate; // the current quality resolution and framerate of the current content.
        protected string audioTrackString; // the current audio track being played.
        private bool showStats = false;
        private Coroutine updateUICoroutine = null;

        protected virtual void Awake() { }
        protected virtual void Start() { }
        protected virtual void OnEnable() {
            if (mediaSlider != null) {
                // Add the media slider's value changed event that enables seek
                AddEventListener(mediaSlider.gameObject, EventTriggerType.BeginDrag, (x) => isDraggingBar = true);
                AddEventListener(mediaSlider.gameObject, EventTriggerType.EndDrag, (x) => { isDraggingBar = false; OnSliderRelease(); });
                AddEventListener(mediaSlider.gameObject, EventTriggerType.PointerDown, (x) => OnSliderRelease());
                AddEventListener(mediaSlider.gameObject, EventTriggerType.Drag, (x) => OnSliderDrag());
            }
            if (currentTime != null) {
                // Add the current time text's click event that seeks to the live edge, if possible
                AddEventListener(currentTime.gameObject, EventTriggerType.PointerClick, (x) => OnCurrentTimeClick());
            }
        }
        protected virtual void Update() { }
        protected virtual void OnDisable() {
            if (mediaSlider != null) {
               RemoveEventListeners(mediaSlider.gameObject);
            }
            if (currentTime != null) {
                RemoveEventListeners(currentTime.gameObject);
            }
        }

        /// <summary>
        /// Initialize the UI
        /// </summary>
        /// <param name="newClearVRPlayer">The ClearVR player to monitor</param>
        /// <param name="demoManagerReference">The reference to the demo manager </param>
        public virtual void Initialize(ClearVRPlayer newClearVRPlayer, DemoManagerBase demoManagerReference) {
            clearVRPlayer = newClearVRPlayer;
            demoManager = demoManagerReference;
            clearVRPlayer.clearVREvents.AddListener(UICbClearVREvent); // add a callback for when the player fires an event so that the UI can update accordingly. 
            clearVRPlayer.clearVRDisplayObjectEvents.AddListener(UICbClearVRDisplayObjectEvent); // add a callback for when the player fires an event so that the UI can update accordingly.
            Reset(); // start a fresh UI
        }

        /// <summary>
        /// Callback function that handles ClearVRPlayer events.
        /// </summary>
        /// <param name="argClearVRPlayer">the ClearVRPlayer instance that fired the event.</param>
        /// <param name="argClearVREvent">the ClearVREvent that was fired.</param>
        private void UICbClearVREvent(ClearVRPlayer argClearVRPlayer, ClearVREvent argClearVREvent) {
            // argClearVREvent.Print(); // Enable this line to print the events that are received. Can be handy for debugging purposes.
            /* Parse the event */
            switch (argClearVREvent.type) {
                case ClearVREventTypes.StateChangedInitializing:
                    // Start the update UI Loop in the User Interface class that updates the UI elements that need to be updated more often.
                    /* Transient state, do not interfere */
                    SpawnBufferIcon(true);
                    break;
                case ClearVREventTypes.StateChangedInitialized:
                    /* ClearVRPlayer object will already take care of preparing the core, do not interfere */
                    break;
                    /* Remember that StateChanged* events are ALWAYS successful as the Core will only change state in case of success. */
                case ClearVREventTypes.StateChangedPreparingContentForPlayout:
                    break;
                case ClearVREventTypes.StateChangedContentPreparedForPlayout:
                    /* if platformOptions.autoPlay == true, ClearVRPlayer will make sure that content will start playing automatically next. */
                    /* Yay! We just rendered the first frame of the selected video. */
                    if (!argClearVREvent.message.GetIsSuccess()) {
                        UnityEngine.Debug.LogError(String.Format("[ClearVR] An error was reported while rendering the first video frame. {0}", argClearVREvent.message.GetFullMessage()));
                    } else {
                        // Do your own stuff here.
                        StartUpdateUICoroutine();
                    }
                    break;
                case ClearVREventTypes.StateChangedBuffering:
                    SpawnBufferIcon(true);
                    break;
                case ClearVREventTypes.StateChangedPlaying:
                    PlayPauseStateHandler(true);
                    break;
                case ClearVREventTypes.StateChangedPausing:
                    break;
                case ClearVREventTypes.StateChangedPaused:
                    PlayPauseStateHandler(false);
                    break;
                case ClearVREventTypes.StateChangedSeeking:
                    break;
                case ClearVREventTypes.StateChangedSwitchingContent:
                    break;
                case ClearVREventTypes.StateChangedFinished:
                    break;
                case ClearVREventTypes.StateChangedStopping:
                    StoppedStateHandler();
                    break;
                case ClearVREventTypes.StateChangedStopped:
                    ParseStateChangedStopped();
                    StopUpdateUICoroutine();
                    break;
                    /* Events not tied to state changes. These events can fail. */
                case ClearVREventTypes.AudioTrackSwitched:

                    break;
                case ClearVREventTypes.ContentSwitched:

                    break;
                case ClearVREventTypes.UnableToInitializePlayer:
                    break;
                case ClearVREventTypes.SuspendingPlaybackAfterApplicationLostFocus:
                    StopUpdateUICoroutine();
                    break;
                case ClearVREventTypes.ResumingPlaybackAfterApplicationRegainedFocus:
                    break;
                case ClearVREventTypes.StereoModeSwitched:
                    break;
                case ClearVREventTypes.ActiveTracksChanged:
                    ContentInfo contentInfo;
                    if (argClearVREvent.message.ParseClearVRCoreWrapperActiveTracksChanged(out contentInfo)) {
                        UpdateAudioTrackStringText(contentInfo);
                        CanContentSwitchAudioTracks(contentInfo);
                        currentQualityResolutionAndFramerate = contentInfo.GetFeedsWithActiveVideoTrack()[0].GetActiveVideoTrack().GetQualityDescriptor();
                        // Configure the UI depending on if we're watching a live clip or vod.
                        switch (contentInfo.eventType) {
                            case EventTypes.Live:
                                isOnLiveEdge = true;
                                mediaSlider.fillRect.GetComponent<Image>().color = Color.red;
                                currentTime.text = "<b>Live</b>";
                                break;
                            case EventTypes.Vod:
                            case EventTypes.FinishedLive:
                                currentTime.color = Color.white;
                                mediaSlider.fillRect.GetComponent<Image>().color = new Color(0.227451f, 0.3372549f, 0.3921569f);
                                break;
                            default:
                                break;
                        }                        
                    } else {
                        return;
                    }
                    break;
                default:
                    break;
            }
        }

        private void UICbClearVRDisplayObjectEvent(ClearVRPlayer argClearVRPlayer, ClearVRDisplayObjectControllerBase argClearVRDisplayObjectController, ClearVRDisplayObjectEvent argClearVRDisplayObjectEvent) {
            switch(argClearVRDisplayObjectEvent.type) {
                case ClearVRDisplayObjectEventTypes.FirstFrameRendered:
                    FirstFrameRenderedHandler(argClearVRPlayer, argClearVRDisplayObjectController);
                    break;
            }
            
        }

        /// <summary>
        /// Update the status text on every frame
        /// </summary>
        public void RefreshStatusTextLabel() {
            if (string.IsNullOrEmpty(currentContentTimeText) && clearVRPlayer != null && clearVRPlayer.performance != null) {
                // shorten the amount of lines in the status text for when there is less information available.
                string description = "";
                if (DemoManagerBase.Instance != null && DemoManagerBase.Instance.contentItemModelList != null) {
                    description = DemoManagerBase.Instance.contentItemModelList[DemoManagerBase.Instance.currentContentItemModelIndex].description;
                }
                statusText.text = String.Format("<b>{0}</b> {1}  |  {2}  |  {3}",
                    clearVRPlayerStatusString,
                    currentQualityResolutionAndFramerate,
                    averageBitrateInMegabitPerSecondString,
                    description);
            } else {
                statusText.text = String.Format("<b><i>{0}</i></b> {1}\n{2} {3}  |  {4}  |  {5}  |  {6}",
                    currentContentPlaying,
                    currentQualityResolutionAndFramerate,
                    averageBitrateInMegabitPerSecondString,
                    string.IsNullOrEmpty(isMonoOrStereoString) ? "" : String.Format("({0})", isMonoOrStereoString),
                    currentContentTimeText,
                    audioTrackString,
                    clearVRPlayerStatusString);
            }
        }

        /// <summary>
        /// Reset the status text string content back to their original state.
        /// </summary>
        public virtual void Reset() {
            currentContentPlaying = "No content playing";
            clearVRPlayerStatusString = "Idle";
            averageBitrateInMegabitPerSecondString = "0 Mbps";
            currentQualityResolutionAndFramerate = "";
            isMonoOrStereoString = "";
            currentContentTimeText = "";
            audioTrackString = "";
            currentTime.text = "00:00";
            currentTime.color = Color.white;
            contentType = EventTypes.Vod;
        }

        /// <summary>
        /// Update clearVRPlayer status text.
        /// </summary>
        /// <param name="argText">The new text</param>
        public void UpdateCurrentContent(string argText) {
            currentContentPlaying = argText;
        }

        /// <summary>
        /// Update clearVRPlayer status text.
        /// </summary>
        /// <param name="argText">The new text</param>
        public void UpdateOSDStatusText(string argText) {
            clearVRPlayerStatusString = argText;
        }

        /// <summary>
        /// Update average bitrate text.
        /// </summary>
        /// <param name="argText">The new text</param>
        public void UpdateAverageBitrateText(string argText) {
            averageBitrateInMegabitPerSecondString = argText;
        }

        /// <summary>
        /// Update current content time.
        /// </summary>
        /// <param name="argText">The new text</param>
        public void UpdateCurrentContentTimeText(string argCurrentTimeText, string argDurationText) {
            if (contentType == EventTypes.Live) {
                currentContentTimeText = argCurrentTimeText;
            } else {
                currentContentTimeText = argCurrentTimeText + "/" + argDurationText;
            }
        }

        /// <summary>
        /// Update the audio track indicator text
        /// </summary>
        /// <param name="argContentInfo">The ContentInfo of the currently active content item.</param>
        public void UpdateAudioTrackStringText(ContentInfo argContentInfo) {
            if (argContentInfo != null && argContentInfo.GetFeedWithActiveAudioTrack() != null && argContentInfo.GetFeedWithActiveAudioTrack().GetActiveAudioTrack() != null) {
                audioTrackString = String.Format("{0} / {1}", argContentInfo.GetFeedWithActiveAudioTrack().GetActiveAudioTrack().trackIndex + 1, argContentInfo.GetNumberOfSelectableAudioTracks());
            } else {
                audioTrackString = "disabled";
            }
        }

        /// <summary>
        /// Toggle the status text on the screen from on to off
        /// </summary>
        public void ShowStats() {
            showStats = !showStats;
        }

        /// <summary>
        /// Set-up the UI according to the player state.
        /// </summary>
        /// <param name="isPaused"></param>
        public virtual void PlayPauseStateHandler(bool isPaused) {
            SpawnBufferIcon(false);
            TogglePlayPauseButtonSprites(isPaused);
            mediaSlider.interactable = true;
        }

        /// <summary>
        /// Set-up the UI after the player has stopped.
        /// </summary>
        public virtual void StoppedStateHandler() {
            bufferImage.gameObject.SetActive(false);
            mediaSlider.interactable = false;
            mediaSlider.value = 0.0f;
        }

        /// <summary>
        /// Set-up the UI on the first frame rendered depending on the content.
        /// </summary>
        /// <param name="argClearVRPlayer">Current active ClearVRPlayer.</param>
        public virtual void FirstFrameRenderedHandler(ClearVRPlayer argClearVRPlayer, ClearVRDisplayObjectControllerBase argClearVRDisplayObjectController) {
            mediaSlider.interactable = true;
            SpawnBufferIcon(false);
        }

        /// <summary>
        /// Setting the right time for the video length and current time in the UI.
        /// </summary>
        protected virtual void SetMediaTime(TimingReport argReport) {
            if (!argReport.GetIsSuccess()) {
                return;
            }
            switch (contentType) {
                case EventTypes.Live:
                    if (isDraggingBar) {
                        return;
                    }
                    isOnLiveEdge = argReport.GetDistanceFromUpperBoundInMilliseconds() <= 3000; // define the live edge as 3 seconds before the end of the live clip.
                    break;
                case EventTypes.Vod:
                case EventTypes.FinishedLive:
                    currentTime.text = string.Format("{0} / {1}", Helpers.GetTimeAsPrettyString(argReport.currentPositionInMilliseconds), Helpers.GetTimeAsPrettyString(argReport.contentDurationInMilliseconds));
                    if (!isDraggingBar && argReport.contentDurationInMilliseconds != 0) {
                        float currentTimeValue = (float) argReport.currentPositionInMilliseconds / (float) argReport.contentDurationInMilliseconds;
                        mediaSlider.value = currentTimeValue;
                    }
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Show The buffer icon when the video is buffering.
        /// </summary>
        /// <param name="shouldActivate"></param>
        public virtual void SpawnBufferIcon(bool shouldActivate) {
            if (bufferImage != null) {
                bufferImage.gameObject.SetActive(shouldActivate);
            }
        }

        /// <summary>
        /// Function gray out the next and previous audio button when there's only one or less audio tracks available in the content.
        /// </summary>
        public void CanContentSwitchAudioTracks(ContentInfo argContentInfo) {
            previousAudioTrackButton.interactable = nextAudioTrackButton.interactable = argContentInfo != null ? argContentInfo.GetNumberOfSelectableAudioTracks() > 0 : false;
        }

        /// <summary>
        /// This perpetual loop is started on scene load and will query clearVRPlayer for the current average bitrate and content time.
        /// Note that we can only request this parameter after clearVRPlayer has competely loaded the content. The platform specific implementation of the player
        /// (e.g. MediaPlayerAndroid) will make sure that when you call any of these methods at a bad time, you will simply get 0 as return value.
        /// See also the clearVRPlayer documentation.
        /// </summary>
        public IEnumerator UpdateUI() {
            yield return null;
            WaitForSeconds delay = new WaitForSeconds(uiRefreshRateInSeconds);
            while (clearVRPlayer != null) {
                if (clearVRPlayer.mediaPlayer != null && clearVRPlayer.controller != null && clearVRPlayer.mediaPlayer.GetIsInPlayingState()) {
                    TimingTypes timingType = TimingTypes.ContentTime;
                    TimingReport report = clearVRPlayer.controller.GetTimingReport(timingType);
                    if (report.GetIsSuccess()) {
                        // Update the video player timer
                        SetMediaTime(report);
                    }

                    if (clearVRPlayer.mediaPlayer.GetCanPerformanceMetricesBeQueried()) {
                        UpdateAverageBitrateText(Math.Round(clearVRPlayer.performance.GetAverageBitrateInMbps(), 1).ToString() + " Mbps");
                        UpdateCurrentContentTimeText(Helpers.GetTimeInMillisecondsAsPrettyString(report.currentPositionInMilliseconds),
                            Helpers.GetTimeInMillisecondsAsPrettyString(report.contentDurationInMilliseconds));
                        UpdateCurrentContent(DemoManagerBase.Instance.contentItemModelList[DemoManagerBase.Instance.currentContentItemModelIndex].description);
                    }
                }

                // update the status text
                RefreshStatusTextLabel();

                yield return delay;
            }
        }

        public void StartUpdateUICoroutine() {
            updateUICoroutine = StartCoroutine(UpdateUI());
        }

        public void StopUpdateUICoroutine() {
            if(updateUICoroutine != null) {
                StopCoroutine(updateUICoroutine);
                updateUICoroutine = null;
            }
        }

        /// <summary>
        /// Seeks to a new position in the video once the player releases the button on the media player bar
        /// </summary>
        public virtual void OnSliderRelease() {
            if (DemoManagerBase.Instance != null) {
                DemoManagerBase.Instance.SeekWithProgressBarValue(mediaSlider.value);
                this.isDraggingBar = false;
            }
        }

        /// <summary>
        /// manages the behavior when dragging the media bar.
        /// </summary>
        public void OnSliderDrag() {
            if (clearVRPlayer == null || clearVRPlayer.controller == null) {
                return;
            }
            switch (contentType) {
                case EventTypes.Live:
                    TimingReport report = clearVRPlayer.controller.GetTimingReport(TimingTypes.WallclockTime);
                    if (!report.GetIsSuccess()) {
                        break;
                    }
                    currentTime.color = Color.white;
                    float milSecondsToSeek = report.upperSeekBoundInMilliseconds - (report.lowerSeekBoundInMilliseconds + (long) ((report.upperSeekBoundInMilliseconds - report.lowerSeekBoundInMilliseconds) * mediaSlider.value));
                    currentTime.text = "-" + Helpers.GetTimeAsPrettyString((long) milSecondsToSeek);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Signal the UI that the tracker handle is being dragged.
        /// </summary>
        public void OnSliderBeginDrag() {
            this.isDraggingBar = true;
        }

        /// <summary>
        /// Toggle between the pause and play icons depending on the player state.
        /// </summary>
        /// <param name="argisPlaying"></param>
        public void TogglePlayPauseButtonSprites(bool argisPlaying) {
            playPauseButton.sprite = argisPlaying ? pauseButtonImage : playButtonImage;
        }

        /// <summary>
        /// Handles the behaviour when the state changed to stopped.
        /// </summary>
        public virtual void ParseStateChangedStopped() {
            Reset();
            playPauseButton.sprite = playButtonImage;
            mediaSlider.interactable = false;
            mediaSlider.value = 0.0f;
            SpawnBufferIcon(false);
        }

        /// <summary>
        /// Method called when the current time tracker in the UI is pressed with touch input, will jump to live edge if we're live.
        /// </summary>
        public void OnCurrentTimeClick() {
            if (DemoManagerBase.Instance == null) {
                return;
            }

            switch (contentType) {
                case EventTypes.Live:
                    DemoManagerBase.Instance.SeekToLiveEdge();
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Handy method to add event listener at run-time
        /// </summary>
        /// <param name="obj">The graphic object to add the event triggers</param>
        /// <param name="eventTriggerType"></param>
        /// <param name="unityEvent"></param>
        protected void AddEventListener(GameObject obj, EventTriggerType eventTriggerType, Action<BaseEventData> unityEvent) {
            EventTrigger trigger = obj.GetComponent<EventTrigger>() ?? obj.AddComponent<EventTrigger>();
            EventTrigger.Entry entry = new EventTrigger.Entry {
                eventID = eventTriggerType
            };
            entry.callback.AddListener((data) => unityEvent.Invoke((PointerEventData) data));
            trigger.triggers.Add(entry);
        }

        /// <summary>
        /// Handy method to remove all event listener at run-time
        /// </summary>
        /// <param name="obj">The graphic object to remove listeners to</param>
        protected void RemoveEventListeners(GameObject obj) {
            EventTrigger trigger = obj.GetComponent<EventTrigger>();
            if(trigger != null) {
                trigger.triggers.Clear();
            }
        }
    }
}