using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace com.tiledmedia.clearvr.demos {
    public class VRMosaicDemoManager : DemoManagerBase {
        [SerializeField]
        private  List<string> layoutNames;

        public override string activeLayoutName   {
            get {
                return layoutNames[currentContentItemModelIndex];
            }
        }

        protected override void Awake() {
            base.Awake();
            Helpers.SimpleTryRecenter();
            contentListFileName = "content-list-mosaic.json";
        }

        protected override void Start() {
            base.Start();
#if UNITY_2017_2_OR_NEWER
            UnityEngine.XR.XRSettings.eyeTextureResolutionScale = 1.5f;
#else
            UnityEngine.VR.VRSettings.eyeTextureWidth = 1.5f;
#endif
        }

        private void Update() {
#if UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.Keypad1)) {
                SetFeedAsLargePanel(1);
            }
            if (Input.GetKeyDown(KeyCode.Keypad2)) {
                SetFeedAsLargePanel(2);
            }
            if (Input.GetKeyDown(KeyCode.Keypad3)) {
                SetFeedAsLargePanel(3);
            }
            if (Input.GetKeyDown(KeyCode.Keypad0)) {
                SetFeedAsLargePanel(0);
            }
#endif
        }

        /// <summary>
        /// Create a platform-specific congifuration that the ClearVRPlayer will be running with.
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
            LayoutParameters layoutParameters = clearVRPlayer.GetLayoutParametersByName(activeLayoutName );
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
        /// Set a video feed to be played as the FullScreen display object.
        /// </summary>
        /// <param name="thumbnailIndex">The feed index that will be played as the FullScreen display object</param>
        public void SetFeedAsFullScreen(int thumbnailIndex) {
            LayoutParameters layoutParameters = clearVRPlayer.GetLayoutParametersByName(activeLayoutName );
            var fsDOM = layoutParameters.GetFullScreenDisplayObjectMapping(); // <-- convenience helper to get the DisplayObject Mapping set as fullscreen 
            var index = layoutParameters.displayObjectMappings.IndexOf(fsDOM);
            layoutParameters.displayObjectMappings[index].feedIndex = thumbnailIndex;
            layoutParameters.audioTrackID = new TrackID(thumbnailIndex, 0 /* Track index 0 */); // set the audio feed to the same feed as the large panel
            clearVRPlayer.mediaPlayer.SetLayout(layoutParameters,
                onSuccess: (cbClearVREvent, cbClearVRPlayer) =>
                    UnityEngine.Debug.Log("[ClearVR] Feeds layout changed successfully."),
                onFailure: (cbClearVREvent, cbClearVRPlayer) =>
                    UnityEngine.Debug.LogWarning(string.Format("[ClearVR] Something went wrong changing feed layout! Error Code: {0}; Message: {1} .", cbClearVREvent.message.code, cbClearVREvent.message.message))
            );
        }

        /// <summary>
        /// Set a video feed to be played as the LargePanel display object.
        /// </summary>
        /// <param name="thumbnailIndex">The index of the display object mapping that has to go to the large panel</param>
        public void SetFeedAsLargePanel(int thumbnailIndex) {
            LayoutParameters layoutParameters = clearVRPlayer.GetLayoutParametersByName(activeLayoutName );
            layoutParameters.displayObjectMappings[1].feedIndex = thumbnailIndex; // Hardcoded index (1) as we know that in the LayoutManager the second display object is the large panel
            layoutParameters.audioTrackID = new TrackID(thumbnailIndex, 0 /* Track index 0 */); // set the audio feed to the same feed as the large panel
            clearVRPlayer.mediaPlayer.SetLayout(layoutParameters,
                onSuccess: (cbClearVREvent, cbClearVRPlayer) =>
                    UnityEngine.Debug.Log("[ClearVR] Feeds layout changed successfully."),
                onFailure: (cbClearVREvent, cbClearVRPlayer) =>
                    UnityEngine.Debug.LogWarning(string.Format("[ClearVR] Something went wrong changing feed layout! Error Code: {0}; Message: {1} .", cbClearVREvent.message.code, cbClearVREvent.message.message))
            );
        }
    }
}