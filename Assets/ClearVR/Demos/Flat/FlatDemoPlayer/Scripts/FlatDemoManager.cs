using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace com.tiledmedia.clearvr.demos {
    public class FlatDemoManager : DemoManagerBase {
        public override string activeLayoutName  => "OmnidirectionalLayout"; // The name of the layout to be played by this demo, contents specific

        protected override void Awake() {
            base.Awake();
            contentListFileName = "content-list.json";
        }
        protected override void Start() {
            base.Start();
            // make sure we enforce VSYNC 
            QualitySettings.vSyncCount = 1;
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
    }
}