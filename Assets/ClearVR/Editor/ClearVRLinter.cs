using UnityEditor;
using UnityEngine;
using System;
#if UNITY_EDITOR
namespace com.tiledmedia.clearvr {

    [InitializeOnLoad]
	public class ClearVRLinter {
		private static bool isUnityRunningInBatchmode = false;
		private static readonly string clearVRWelcomeNoticeShownKey = "clear_vr_is_welcome_notice_shown";
		public static String ANDROID_MANIFEST_XML_FILE_FULL_PATH = Application.dataPath + "/Plugins/Android/AndroidManifest.xml";
		public static String CVR_SUPPORT_OCULUS_RUNTIME_EXTENSIONS = "CVR_SUPPORT_OCULUS_RUNTIME_EXTENSIONS";

		private static bool clearVRIsWelcomeNoticeShown {
			get {
				return PlayerPrefs.GetInt(clearVRWelcomeNoticeShownKey, 0) == 1;
			}

			set {
				PlayerPrefs.SetInt(clearVRWelcomeNoticeShownKey, value ? 1 : 0);
				PlayerPrefs.Save();
			}
		}

		static ClearVRLinter() {
			EditorApplication.delayCall += OnDelayedCall;
		}

		public static bool GetIsUnityRunningInBatchmode() {
			return isUnityRunningInBatchmode;
		}

		static void OnDelayedCall() {
			if (System.Environment.CommandLine.Contains("-batchmode")) {
				isUnityRunningInBatchmode = true;
			}
#if UNITY_ANDROID
			if(!clearVRIsWelcomeNoticeShown && !isUnityRunningInBatchmode) {
				EditorUtility.DisplayDialog("ClearVR",
						"ClearVR requires a custom activity to be set in your AndroidManifest.xml\n" + 
						"Use the ClearVR menu item to change your AndroidManifest.xml according to\n" + 
						"the platform (e.g. flat/oculus/PicoVR) you target.",
						"Ok",
						"");
				clearVRIsWelcomeNoticeShown = true;
			}
#endif
		}

        [MenuItem("ClearVR/Android/Setup Manifest XML/All other targets")]
		public static void SetupAndroidManifestForAllOtherTargets() {
			XMLHelpers.UpdateAndroidManifestXml(ANDROID_MANIFEST_XML_FILE_FULL_PATH, "/manifest/application/activity", "android:name", "com.tiledmedia.clearvrforunityandroid.ClearVRForUnityAndroidActivity", true);
		}

        [MenuItem("ClearVR/Android/Setup Manifest XML/WaveVR")]
		public static void SetupAndroidManifestForWaveVR() {
			if(ClearVREditorUtils.GetIsXRLoaderActive(Utils.WAVE_XR_LOADER_NAME, BuildTargetGroup.Android)) {
				XMLHelpers.UpdateAndroidManifestXml(ANDROID_MANIFEST_XML_FILE_FULL_PATH, "/manifest/application/activity", "android:name", "com.tiledmedia.clearvrforunityandroid.ClearVRForUnityAndroidActivity", true);
			} else {
				XMLHelpers.UpdateAndroidManifestXml(ANDROID_MANIFEST_XML_FILE_FULL_PATH, "/manifest/application/activity", "android:name", "com.tiledmedia.clearvrforunityandroid.ClearVRForUnityForWaveVRAndroidActivity", true);
			}
		}

        [MenuItem("ClearVR/Android/Setup Manifest XML/PicoVR")]
		public static void SetupAndroidManifestForPicoVR() {
			if(ClearVREditorUtils.GetIsXRLoaderActive(Utils.PICO_XR_LOADER_NAME, BuildTargetGroup.Android)) {
				XMLHelpers.UpdateAndroidManifestXml(ANDROID_MANIFEST_XML_FILE_FULL_PATH, "/manifest/application/activity", "android:name", "com.tiledmedia.clearvrforunityandroid.ClearVRForUnityAndroidActivity", true);
			} else {
				XMLHelpers.UpdateAndroidManifestXml(ANDROID_MANIFEST_XML_FILE_FULL_PATH, "/manifest/application/activity", "android:name", "com.tiledmedia.clearvrforunityandroid.ClearVRForUnityForPicoVRAndroidActivity", true);
			}
		}
        [MenuItem("ClearVR/Android/Setup Manifest XML/SkyworthVR")]
		public static void SetupAndroidManifestForSkyworthVR() {
			if(ClearVREditorUtils.GetIsXRLoaderActive(Utils.SKYWORTH_XR_LOADER_NAME, BuildTargetGroup.Android)) {
				XMLHelpers.UpdateAndroidManifestXml(ANDROID_MANIFEST_XML_FILE_FULL_PATH, "/manifest/application/activity", "android:name", "com.tiledmedia.clearvrforunityandroid.ClearVRForUnityAndroidActivity", true);
			} else {
				XMLHelpers.UpdateAndroidManifestXml(ANDROID_MANIFEST_XML_FILE_FULL_PATH, "/manifest/application/activity", "android:name", "com.tiledmedia.clearvrforunityandroid.ClearVRForUnityForSkyworthVRAndroidActivity", true);
			}
		}

        [MenuItem("ClearVR/Oculus Extensions (Android Only)/Enable")]
		public static void EnableOculusExtensions() {
			EnableOrDisableOculusExtensions(true);
		}

        [MenuItem("ClearVR/Oculus Extensions (Android Only)/Disable")]
		public static void DisableOculusExtensions() {
			EnableOrDisableOculusExtensions(false);
		}

		/// <summary>
		/// Opens the online docs for this SDK version
		/// </summary>
        [MenuItem("ClearVR/About and help", false, 10000)]
		public static void About() {
			String UNKNOWN = "Unknown"; // Must be synchronized with ClearVRPlayer.GetClearVRCoreVersion()
			String sdkVersionFromCore = ClearVRPlayer.GetClearVRCoreVersion().Trim();
			String sdkVersionFromFile = UNKNOWN;
			String sdkDateFromFile = "1900-01-01"; // Can only be queried from version.txt
			
			// Fallback to reading Assets/ClearVR/Docs/version.txt
			String fileFullPath = String.Format("{0}/ClearVR/Docs/version.txt", Application.dataPath);
			if(System.IO.File.Exists(fileFullPath)) {
				string text = System.IO.File.ReadAllText(fileFullPath);
				if(!String.IsNullOrEmpty(text)) {
					foreach(String line in text.Split(new[] { Environment.NewLine }, StringSplitOptions.None)) {
						if(line.Contains("SDK version")) {
							String[] parts = line.Split(':');
							if(parts.Length == 2) {
								sdkVersionFromFile = parts[1].Trim();
							}
						}
						if(line.Contains("Date")) {
							String[] parts = line.Split(new[] { ": " }, StringSplitOptions.None);
							if(parts.Length == 2) {
								sdkDateFromFile = parts[1].Trim();
							}
						}
					}
				}// else: fallthrough
			}// else: fallthrough
			String sdkVersion = UNKNOWN;
			if(sdkVersionFromCore != UNKNOWN) {
				if(sdkVersionFromFile != UNKNOWN) {
					if(sdkVersionFromCore != sdkVersionFromFile) {
						UnityEngine.Debug.LogWarning(String.Format("[ClearVR] LIbrary ({0}) and version.txt ({1}) report different SDK versions. If you updated the SDK, please restart your editor to reload the dynamic libraries.", sdkVersionFromCore, sdkVersionFromFile));
					} //else: core and txt report the same value. That is great :)
				} //else: not core version reported in txt file, core-reported version is leading
				sdkVersion = sdkVersionFromCore;
			} else {
				sdkVersion = sdkVersionFromFile;
			}
			
			String sdkVersionAndDate = String.Format("{0} ({1})", sdkVersion, sdkDateFromFile);
			if(String.IsNullOrEmpty(sdkVersion) || sdkVersion == UNKNOWN) { 
				// Still not set, don't know the sdk version so cannot op online docs :(
				EditorUtility.DisplayDialog("About", String.Format("ClearVR SDK for Unity version: {0}\nPlease refer to the PDFs in Assets/ClearVR/Docs/ for documentation.", sdkVersionAndDate), "Ok");
				return;
			}
			String sdkMajorVersion;
			String[] sdkVersionParts = sdkVersion.Split('-');
			if(sdkVersionParts.Length >= 1) {
				// No - in sdkVersion, e.g. v7.2.1
				sdkMajorVersion = sdkVersionParts[0];
			} else {
				EditorUtility.DisplayDialog("About", String.Format("ClearVR SDK for Unity version: {0}\nPlease refer to the PDFs in Assets/ClearVR/Docs/ for documentation.", sdkVersionAndDate), "Ok");
				return;
			}
			bool result = EditorUtility.DisplayDialog("About", String.Format("ClearVR SDK for Unity version: {0}\nPlease refer to the PDFs in Assets/ClearVR/Docs/ or online for documentation.", sdkVersionAndDate), "Open online docs", "Close");
			if(result /* = open online docs button */) {
				String httpUrl = String.Format("http://docs.api.tiled.media/{0}/{1}/unity-sdk/readme/introduction.html", sdkMajorVersion, sdkVersion);
				Application.OpenURL(httpUrl);
			}
		}

		/// <summary>
		/// Check if ClearVR Android Activity is set in provided AndroidManifest.xml at path manifest/application/activity.
		/// </summary>
		/// <returns>0 if all is OK, -1 if the xml path cannot be found or -2 if the specified activity is not a ClearVR activity.</returns>
		internal static int CheckClearVRActivityInAndroidManifest() {
			String activitySpecifiedInAndroidManifest = XMLHelpers.GetValueFromKeyInAndroidManifestXmlSafely(ANDROID_MANIFEST_XML_FILE_FULL_PATH, "/manifest/application/activity", "android:name");
			if(String.IsNullOrEmpty(activitySpecifiedInAndroidManifest)) {
				return -1;
			}
			if(!(activitySpecifiedInAndroidManifest.ToLower().Contains("clearvr"))) {
				return -2;
			}
			return 0;
		}

		internal static void EnableOrDisableOculusExtensions(bool argEnableOrDisable) {
			String dialogBoxText = "";
#if UNITY_ANDROID
			// Get selected target defines.
			BuildTargetGroup buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
			if(buildTargetGroup == BuildTargetGroup.Android) {
				string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);

				if(argEnableOrDisable) {
		            Type ovrPluginType = com.tiledmedia.clearvr.Utils.GetType("OVRPlugin");
					if(ovrPluginType != null) {
						if (defines.Contains(CVR_SUPPORT_OCULUS_RUNTIME_EXTENSIONS)) {
							dialogBoxText = "ClearVR Oculus Extensions already enabled.\n";
						} else {
							PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, (defines + ";" + CVR_SUPPORT_OCULUS_RUNTIME_EXTENSIONS));
							dialogBoxText = "Oculus Runtime detected. Enabling ClearVR Oculus Extensions.\n";
						}
					} else {
						dialogBoxText = "Unable to detect Oculus Runtime. Cannot enable ClearVR Oculus Utilties.";
					}
				} else {
					// Only if not defined already.
					if (defines.Contains(CVR_SUPPORT_OCULUS_RUNTIME_EXTENSIONS)) {
						defines = defines.Replace(CVR_SUPPORT_OCULUS_RUNTIME_EXTENSIONS, "");
						defines = defines.Replace(";;", ";");
						PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, defines);
						dialogBoxText = "ClearVR Oculus Extensions disabled.\n";
					} else {
						dialogBoxText = "ClearVR Oculus Extensions already disabled.\n";
					}
				} 
			} else {
				dialogBoxText = "ClearVR only supports Oculus Extensions when targeting the Android platform.\n";
			}
#else
			dialogBoxText = "ClearVR only supports Oculus Extensions when targeting the Android platform.\n";
#endif
			EditorUtility.DisplayDialog("ClearVR",
					dialogBoxText,
					"Ok",
					"");
		}	
	}
}
#endif