using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace com.tiledmedia.clearvr.demos {
    public class FlatMosaicDemoManager : DemoManagerBase {
        [SerializeField]
        private List<string> layoutNames;
        [Header("Flat Mosaic Properties")]
        public ClearVRDisplayObjectControllerBase pictureInPictureDisplayObjectController;
        public override string activeLayoutName {
            get {
                return layoutNames[currentContentItemModelIndex];
            }
        }
        protected override void Awake() {
            base.Awake();
            contentListFileName = "content-list-mosaic.json";
        }

        protected override void Start() {
            base.Start();
        }

        /// <summary>
        /// Create a platform-specific congifuration that the <see cref="ClearVRPlayer"/> will be running with.
        /// </summary>
        /// <returns>The platfrom options that will configure a ClearVRPlayer</returns>
        protected override PlatformOptionsBase CreatePlatformOptions() {
            /* Set platform/player options. Note that changing any of these options AFTER calling clearVRPlayer.Initialize() will result in undefined behavior and is NOT supported. */
            PlatformOptionsBase platformOptions = (PlatformOptionsBase) clearVRPlayer.GetDefaultPlatformOptions();
            platformOptions.licenseFileBytes = licenseFileBytes; /* Byte array containing your private license file data */
            if (Camera.main != null) {
                platformOptions.trackingTransform = Camera.main.transform; /* The transform of the currently active Camera, e.g. MainCamera or the CenterEyeAnchor on your OVRCameraRig */
            } else {
                // use main camera instead if user interface is not found
                Debug.LogWarning("[ClearVR] Main camera not found. Using first camera found in scene instead.");
                platformOptions.trackingTransform = GameObject.FindObjectOfType<Camera>().transform;
            }
            /* Specify ContentItem to load and preferred playout start position. */
            ContentItem firstContentItemToPlay = contentItemModelList[currentContentItemModelIndex].clearVRContentItem;
            // Get the layoutParameters to pass to the SwitchContent call to specify the mosaic object mapping
            LayoutParameters layoutParameters = clearVRPlayer.GetLayoutParametersByName(activeLayoutName);
            platformOptions.prepareContentParameters = new PrepareContentParameters(firstContentItemToPlay, null, layoutParameters); // The content item to load, overloaded constructor takes multiple arguments.
            platformOptions.loopContent = true; /* whether to loop content or not, note that for LIVE events looping should typically be disabled. */

            if (enableLogging) {
                // Setting up a logging configuration for debug purposes

                // First we create a logging configuration, the default does the following:
                // - global log level: debug
                // - log to a file called clearvr.log, this path will be logged in the console.
                // - record interactions that were made with our sdk to recorder.tmerp
                LoggingConfiguration loggingConfiguration = LoggingConfiguration.GetDefaultLoggingConfiguration();

                // Enable logging on the ClearVRPlayer. This is debug logging, don't overuse this because it can influence performance.
                // To disable this feature you can pass null.
                ClearVRPlayer.EnableLogging(loggingConfiguration);
            }

            /* Use recommended mechanism to handle application focus/backgrounding. See [](xref:com.tiledmedia.clearvr.ApplicationFocusAndPauseHandlingTypes) for details. */
            platformOptions.applicationFocusAndPauseHandling = ApplicationFocusAndPauseHandlingTypes.Recommended;
            return platformOptions;
        }

        /// <summary>
        /// Change the feed index of the <see cref="ClearVRDisplayObjectController"/> flagged as FullScreen.
        /// </summary>
        /// <param name="thumbnailIndex"></param>
        
        public void SetFullScreenFeed(int thumbnailIndex) {
            LayoutParameters layoutParameters = clearVRPlayer.GetLayoutParametersByName(activeLayoutName);
            DisplayObjectMapping fullScreenDisplayObjectMapping = layoutParameters.GetFullScreenDisplayObjectMapping(); // convenience helper to get the full screen mapping
            int index = layoutParameters.displayObjectMappings.IndexOf(fullScreenDisplayObjectMapping);
            layoutParameters.displayObjectMappings[index].feedIndex = thumbnailIndex;
            layoutParameters.audioTrackID = new TrackID(-2, -2);
            clearVRPlayer.mediaPlayer.SetLayout(layoutParameters,
                onSuccess: (cbClearVREvent, cbClearVRPlayer) => Debug.Log("[ClearVR] Feeds layout changed successfully."),
                onFailure: (cbClearVREvent, cbClearVRPlayer) => Debug.LogWarning(string.Format("[ClearVR] Something went wrong changing feed layout! Error Code: {0}; Message: {1} .", cbClearVREvent.message.code, cbClearVREvent.message.message))
            );
        }

        /// <summary>
        /// Change the feed index of the picture-in-picture <see cref="ClearVRDisplayObjectController"/>.
        /// </summary>
        /// <param name="thumbnailIndex"></param>
        public void SetPictureInPictureFeed(int thumbnailIndex) {
            try {
                LayoutParameters layoutParameters = clearVRPlayer.GetLayoutParametersByName(activeLayoutName);
                DisplayObjectMapping overlayDisplayObjectMapping = layoutParameters.displayObjectMappings.First(dom => dom.clearVRDisplayObjectController == pictureInPictureDisplayObjectController);
                int pipDisplayObjectMappingIndex = layoutParameters.displayObjectMappings.IndexOf(overlayDisplayObjectMapping);
                layoutParameters.displayObjectMappings[pipDisplayObjectMappingIndex].feedIndex = thumbnailIndex;
                layoutParameters.audioTrackID = new TrackID(-2, -2);
                clearVRPlayer.mediaPlayer.SetLayout(layoutParameters,
                    onSuccess: (cbClearVREvent, cbClearVRPlayer) => Debug.Log("[ClearVR] Feeds layout changed successfully."),
                    onFailure: (cbClearVREvent, cbClearVRPlayer) => Debug.LogWarning(string.Format("[ClearVR] Something went wrong changing feed layout! Error Code: {0}; Message: {1} .", cbClearVREvent.message.code, cbClearVREvent.message.message))
                );
            } catch (Exception e) {
                Debug.LogError("[ClearVR] Exception while setting the picture-in-picture feed: " + e.Message);
            }
        }

        /// <summary>
        /// Swap the feeds between the picture-in-picture <see cref="ClearVRDisplayObjectController"/> and the one flagged as FullScreen.
        /// </summary>
        public void SwapMainAndOverlayFeeds() {
            LayoutParameters layoutParameters = clearVRPlayer.GetLayoutParametersByName(activeLayoutName);
            DisplayObjectMapping fullScreenDisplayObjectMapping = layoutParameters.GetFullScreenDisplayObjectMapping(); // convenience helper to get the full screen mapping
            if (fullScreenDisplayObjectMapping == null) {
               Debug.LogError("[ClearVR] Full screen mapping not found. Check your LayoutManager configuration.");
                return;
            }
            DisplayObjectMapping overlayDisplayObjectMapping = null;
            try {
                overlayDisplayObjectMapping = layoutParameters.displayObjectMappings.First(dom => dom.clearVRDisplayObjectController == pictureInPictureDisplayObjectController);
            } catch (Exception e) {
                Debug.LogError("[ClearVR] Exception while swapping feeds: " + e.Message);
                return;
            }
            // Get the DisplayObjectMapping objects' indexes
            int indexMain = layoutParameters.displayObjectMappings.IndexOf(fullScreenDisplayObjectMapping);
            int pipDisplayObjectMappingIndex = layoutParameters.displayObjectMappings.IndexOf(overlayDisplayObjectMapping);
            // Cache the current feed indexes
            int oldMainFeedID = layoutParameters.displayObjectMappings[indexMain].feedIndex;
            int oldOverlayFeedID = layoutParameters.displayObjectMappings[pipDisplayObjectMappingIndex].feedIndex;
            // Swap the full screen index with the overlay index
            layoutParameters.displayObjectMappings[pipDisplayObjectMappingIndex].feedIndex = oldMainFeedID;
            layoutParameters.displayObjectMappings[indexMain].feedIndex = oldOverlayFeedID;
            // Set the new layout with the new layout parameters
            clearVRPlayer.mediaPlayer.SetLayout(layoutParameters,
                onSuccess: (cbClearVREvent, cbClearVRPlayer) => Debug.Log("[ClearVR] Feeds layout changed successfully."),
                onFailure: (cbClearVREvent, cbClearVRPlayer) => Debug.LogWarning(string.Format("[ClearVR] Something went wrong changing feed layout! Error Code: {0}; Message: {1} .", cbClearVREvent.message.code, cbClearVREvent.message.message))
            );
        }
    }
}