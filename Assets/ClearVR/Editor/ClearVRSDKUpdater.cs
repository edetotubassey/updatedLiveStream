using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
#if UNITY_EDITOR
namespace com.tiledmedia.clearvr {
    [InitializeOnLoad]
	public class ClearVRSDKUpdater {
        private static string SHELL_COMMAND = "<SHELL_COMMAND>"; // dynamically replaced by /bin/sh or wsl.exe depending on platform
		private static readonly string clearVRIsIOSNativePluginUpdateCheckCompletedKey = "clear_vr_is_ios_native_plugin_update_check_completed";
		private static readonly string clearVRIsV8_1_X86_64CleanUpCompletedKey = "clear_vr_is_v8_1_x86_64_clean_up_completed";
        private static readonly string clearVRIsLibRtAudioCleanUpCompletedKey = "clear_vr_is_lib_rtaudio_clean_up_completed";
        private static readonly string clearVRIsUcrtbaseCleanUpCompletedKey = "clear_vr_is_ucrtbase_clean_up_completed";
        private static readonly string clearVRIsV9_ScriptCleanUpCompletedKey  = "clear_vr_is_v9_script_clean_up_completed";
        private static readonly string clearVRIsIOSNIBCleanUpCompletedKey  = "clear_vr_is_ios_nib_clean_up_completed";

		static ClearVRSDKUpdater() {
			EditorApplication.delayCall += OnDelayCall;
		}

		private static bool clearVRIsIOSNativePluginUpdateCheckCompleted {
			get {
				return PlayerPrefs.GetInt(clearVRIsIOSNativePluginUpdateCheckCompletedKey, 0) == 1;
			}

			set {
				PlayerPrefs.SetInt(clearVRIsIOSNativePluginUpdateCheckCompletedKey, value ? 1 : 0);
				PlayerPrefs.Save();
			}
		}

		private static bool clearVRIsV8_1_X86_64CleanUpCompleted {
			get {
				return PlayerPrefs.GetInt(clearVRIsV8_1_X86_64CleanUpCompletedKey, 0) == 1;
			}

			set {
				PlayerPrefs.SetInt(clearVRIsV8_1_X86_64CleanUpCompletedKey, value ? 1 : 0);
				PlayerPrefs.Save();
			}
		}

        private static bool clearVRIsLibRtAudioCleanUpCompleted {
			get {
				return PlayerPrefs.GetInt(clearVRIsLibRtAudioCleanUpCompletedKey, 0) == 1;
			}

			set {
				PlayerPrefs.SetInt(clearVRIsLibRtAudioCleanUpCompletedKey, value ? 1 : 0);
				PlayerPrefs.Save();
			}
		}

        private static bool clearVRIsUcrtbaseCleanUpCompleted {
			get {
				return PlayerPrefs.GetInt(clearVRIsUcrtbaseCleanUpCompletedKey, 0) == 1;
			}

			set {
				PlayerPrefs.SetInt(clearVRIsUcrtbaseCleanUpCompletedKey, value ? 1 : 0);
				PlayerPrefs.Save();
			}
		}

        private static bool clearVRIsV9_ScriptCleanUpCompleted {
			get {
				return PlayerPrefs.GetInt(clearVRIsV9_ScriptCleanUpCompletedKey, 0) == 1;
			}

			set {
				PlayerPrefs.SetInt(clearVRIsV9_ScriptCleanUpCompletedKey, value ? 1 : 0);
				PlayerPrefs.Save();
			}
		}
        private static bool clearVRIsIOSNIBCleanUpCompleted {
			get {
				return PlayerPrefs.GetInt(clearVRIsIOSNIBCleanUpCompletedKey, 0) == 1;
			}

			set {
				PlayerPrefs.SetInt(clearVRIsIOSNIBCleanUpCompletedKey, value ? 1 : 0);
				PlayerPrefs.Save();
			}
		}
		private static void OnDelayCall() {
			if(ShouldAttemptPluginUpdate()) {
				AttemptPluginUpdate();
				AttemptPicoVRPatch();
				RemoveDeprecatedClearVRMeshClasses();
                RemoveDeprecatedClearVRShaders();
				RemoveSupportCompat2531JAR();
				RenameLibClearVRNativeRendererPluginPluginsForiOS();
				CleanUpV8_1_X86_64Libraries();
                RemoveIOSNIBFiles();
#if UNITY_EDITOR_LINUX
				CleanUpRtAudioLibrariesOnLinux();
                CheckForLibAVSymlinkOnLinux();
                CheckForVulkanSymlinkOnLinux();
#endif
#if UNITY_EDITOR_WIN
				CleanUpUcrtbaseLibrariesOnWindows();
#endif
			}
		} 
		private static void AttemptPluginUpdate() {
			CheckForUpgradeToClearVRSDKv51Android();
		}

		private static bool ShouldAttemptPluginUpdate() {
			return !Application.isPlaying;
		}
        
        /// <summary>
        /// This method checks if we upgrade to a ClearVRSDK v5.1. ClearVRSDK v5.1 introduced fat multi-arch AAR Android libraries and changes the names of the existing libraries.
		/// We need to remove the old ones as they will clash with the new ones, resulting in failing builds.
        /// </summary>
        private static void CheckForUpgradeToClearVRSDKv51Android() {
			// Check for any older -arm-v7a-*.aar files. They have been renamed to filenames without an architecture now that we support ARM64 and arm-v7a on Android through fat AAR files.
            String androidPluginsFolderRelativePath = "Assets/ClearVR/Plugins/Android/";
            string [] fileEntries = Directory.GetFiles(androidPluginsFolderRelativePath, "*ClearVR*-arm-v7a*");
			bool isOldSDKFileFound = false;
            foreach(string fileName in fileEntries) {
				if(Path.GetExtension(fileName) != ".meta") { // ignore residual meta files, they are harmless and will be removed by Unity lateron anyway.
					isOldSDKFileFound = true;
					break;
				}
            }
			if(isOldSDKFileFound) {
				bool result = EditorUtility.DisplayDialog("ClearVR - Removing obsolete libraries", "One or multiple obsolete ClearVR Android libraries (.aar) files have been detected. As part of upgrading to the latest ClearVR SDK they must be removed. This process will not touch any other file.", "Remove", "Cancel");
				if(result) {
					foreach(string fileName in fileEntries) {
						AssetDatabase.DeleteAsset(fileName);
					}
				}
			}
        }


        /// <summary>
        /// Older Pico SDKs (2.7.9 and older) included android-support-v4.jar which clashed with support-compat-25.3.1.aar that was included in the ClearVR SDK.
        /// We removed the android-support-v4.jar file. It would've been better if we removed the ClearVR-provided file instead but alas.
        /// To repair this design flaw, we check if android-support-v4.jar is missing and hbcvserviceclient.jar is present. This indicates an older SDK and we shhould notify the user to repair the mess we left behind
        /// Since v7.3.2, we do not need support-compat-25.3.1.aar anymore, see below.
        /// </summary>
		private static void AttemptPicoVRPatch() {
            String androidPluginsFolderRelativePath = "Assets/Plugins/Android/";
            if(!System.IO.Directory.Exists(androidPluginsFolderRelativePath)) { // Yes, System.IO.Directory.Exists() also takes relative folders to the Directory.GetCurrentDirectory(), which is the Unity project's root folder
                // Nothing to be done, the folder that should contain our file of interest does not even exist.
                return;
            }
            string [] fileEntries = Directory.GetFiles(androidPluginsFolderRelativePath, "hbcvserviceclient.jar");
            if(fileEntries.Length == 1) {
                // Means older Pico SDK was found
                fileEntries = Directory.GetFiles(androidPluginsFolderRelativePath, "android-support-v4.jar");
                if(fileEntries.Length == 0) {
                    // Means android-support-v4.jar was not found (and thus most likely removed by previous ClearVR SDK)
                    androidPluginsFolderRelativePath = "Assets/ClearVR/Plugins/Android/"; // The next file is found in a different folder
                    fileEntries = Directory.GetFiles(androidPluginsFolderRelativePath, "support-compat-25.3.1.aar");
                    if(fileEntries.Length == 1) {
                        // Means that support-compat-25.3.1.aar is still around (which will be removed soon-after)
                        // If this is the case, we warn the user. 
                        EditorUtility.DisplayDialog("ClearVR - Reimport PicoVR support library", "A previous version of the ClearVR SDK included the support-compat-25.3.1.aar library that clashed with Assets/Plugins/Android/android-support-v4.jar as provided by the PicoVR SDK. Having both libraries results in linker issues, so the former was automatically removed from your project in the past. The ClearVR SDK no longer depends on support-compat-25.3.1.aar library. To repair your PicoVR SDK, please manually reimport the PicoVR SDK unitypackage. Note that newer versions of the PicoVR SDK (v2.8.6+) are not affected by this issue as they do not depend (and include) on android-support-v4.jar.", "OK");
                    }
                }
            }
		}

		/// <summary>
		/// Various ClearVRDisplayObjectController* classes have been removed in v7.0. This method checks for any lingering files and removes them as needed.
		/// </summary>
		private static void RemoveDeprecatedClearVRMeshClasses() {
            String clearVRScriptsFolderRelativePath = "Assets/ClearVR/Scripts/";
            if(!System.IO.Directory.Exists(clearVRScriptsFolderRelativePath)) { // Yes, System.IO.Directory.Exists() also takes relative folders to the Directory.GetCurrentDirectory(), which is the Unity project's root folder
                // Nothing to be done, the folder that should contain our file of interest does not even exist.
                return;
            }
            string [] fileEntries = Directory.GetFiles(clearVRScriptsFolderRelativePath, "ClearVRMesh*.cs");
			if(fileEntries.Length > 0) {
				String[] fileNamesToRemove = new String[] {"ClearVRMeshCubemap.cs", "ClearVRMeshCubemap180.cs", "ClearVRMeshPlanar.cs"};
				int numberOfFilesRemoved = 0;
				foreach(String fileEntry in fileEntries) {
					foreach(String fileNameToRemove in fileNamesToRemove) {
						if(fileEntry.Contains(fileNameToRemove)) {
							numberOfFilesRemoved++;
						}
					}
				}
				if(numberOfFilesRemoved > 0) {
					bool result = EditorUtility.DisplayDialog("ClearVR - Removing deprecated files", String.Format("The following files are obsolete since v7.0 of the ClearVR SDK:\n{0}.\nBy clicking Remove these files will be removed.", string.Join("\n", fileNamesToRemove)), "Remove", "Cancel");
					if(result) {
						foreach(String fileEntry in fileEntries) {
							foreach(String fileNameToRemove in fileNamesToRemove) {
								if(fileEntry.Contains(fileNameToRemove)) {
									AssetDatabase.DeleteAsset(fileEntry);
								}
							}
						}					
					}
				}
			}
		}
		/// <summary>
		/// Dedicated ERP and FishEye ClearVRShaders have been removed in v8.1. This method checks for any lingering files and removes them as needed.
		/// </summary>
		private static void RemoveDeprecatedClearVRShaders() {
            String clearVRShaderFolderRelativePath = "Assets/ClearVR/Resources/Shaders/";
            if(!System.IO.Directory.Exists(clearVRShaderFolderRelativePath)) { // Yes, System.IO.Directory.Exists() also takes relative folders to the Directory.GetCurrentDirectory(), which is the Unity project's root folder
                // Nothing to be done, the folder that should contain our file of interest does not even exist.
                return;
            }
            string [] fileEntries = Directory.GetFiles(clearVRShaderFolderRelativePath, "ClearVRShader*.shader");
			if(fileEntries.Length > 0) {
				String[] fileNamesToRemove = new String[] {"ClearVRShaderERP.cs", "ClearVRShaderFishEyeEquidistant.cs", "ClearVRShaderFishEyeEquiSolid.cs"};
				int numberOfFilesRemoved = 0;
				foreach(String fileEntry in fileEntries) {
					foreach(String fileNameToRemove in fileNamesToRemove) {
						if(fileEntry.Contains(fileNameToRemove)) {
							numberOfFilesRemoved++;
						}
					}
				}
				if(numberOfFilesRemoved > 0) {
					bool result = EditorUtility.DisplayDialog("ClearVR - Removing deprecated shader files", String.Format("The following shaders are obsolete since v8.1 of the ClearVR SDK:\n{0}.\nBy clicking Remove these files will be removed.", string.Join("\n", fileNamesToRemove)), "Remove", "Cancel");
					if(result) {
						foreach(String fileEntry in fileEntries) {
							foreach(String fileNameToRemove in fileNamesToRemove) {
								if(fileEntry.Contains(fileNameToRemove)) {
									AssetDatabase.DeleteAsset(fileEntry);
								}
							}
						}					
					}
				}
			}
		}

        /// <summary>
        /// Since v7.3.2, we no longer depend on support-compat-25.3.1.aar. This means that we no longer include it in our ClearVR SDK for Unity unitypackages and that the file can be safely removed.
        /// </summary>
        private static void RemoveSupportCompat2531JAR() {
            String androidPluginsFolderRelativePath = "Assets/ClearVR/Plugins/Android/";
            String fileNameToRemove = "support-compat-25.3.1.aar";
            String[] fileEntries = Directory.GetFiles(androidPluginsFolderRelativePath, fileNameToRemove);
            if(fileEntries.Length > 0) {
                bool result = EditorUtility.DisplayDialog("ClearVR - Removing obsolete file", String.Format("The following file, previously included in the ClearVR SDK, is obsolete since v7.4:\n{0}{1}.\nBy clicking Remove this file will be removed from your project (highly recommended). No other files will be affected.", androidPluginsFolderRelativePath, fileNameToRemove), "Remove", "Cancel");
                if(result) {
                    AssetDatabase.DeleteAsset(androidPluginsFolderRelativePath + fileNameToRemove);
                }
            }
        }

        /// <summary>
        /// Since v8.0.6 we no longer rename our iOS library to/from .disabled based on the Unity version that was detected. 
        /// Here we rename any libClearVRNativeRendererPlugin*.a.disabled to libClearVRNativeRendererPlugin*.a
        /// </summary>
        private static void RenameLibClearVRNativeRendererPluginPluginsForiOS() {
            if(clearVRIsIOSNativePluginUpdateCheckCompleted) {
                return; // Nothing to be done.
            }
            String pluginsFolderRelativePath = "Assets/"; // Some people move our SDK around, so we should search the entire Assets folder.
            if(!System.IO.Directory.Exists(pluginsFolderRelativePath)) { 
                // Nothing to be done, the folder that should contain our file of interest does not even exist.
                return;
            }
            string [] fileEntries = Directory.GetFiles(pluginsFolderRelativePath, "libClearVRNativeRendererPlugin*.a.disabled", SearchOption.AllDirectories);
            if(fileEntries.Length > 0) {
                bool result = EditorUtility.DisplayDialog("ClearVR - iOS plugin management has changed", String.Format("The ClearVR SDK provides Unity-version specific builds of its libClearVRNativeRendererPlugin.a file, located in {0}. Previously, the not-needed version was renamed to .disabled, but since v8.0.6 we enable/disable the plugin using Unity's Plugin Management interface. Any libClearVRNativeRendererPlugin-XXXX.a.disabled file will now be renamed to the equivalent without .disabled. Please click OK to continue.", pluginsFolderRelativePath), "OK");
                foreach(String fileEntry in fileEntries) {
                    String oldFileName = fileEntry;
                    String newFileName = oldFileName.Replace(".disabled", "");
                    AssetDatabase.MoveAsset(oldFileName, newFileName);
                    AssetDatabase.ImportAsset(newFileName, ImportAssetOptions.ForceUpdate);
                }
            }
            clearVRIsIOSNativePluginUpdateCheckCompleted = true;
        }

        /// <summary>
        /// In v8.1 some files x86_64 libs were renamed or removed.
        /// 
        /// Renamed:
        /// * MediaFlow_SA.dll --> ClearVRPC.dll
        /// * libClearVRLinux.so --> libClearVRPC.so
        /// 
        /// Removed:
        /// * ClearVRNativeRendererPlugin.dll
        /// 
        /// This method cleans-up by removing the old files in case they are found.
        /// </summary>
        private static void CleanUpV8_1_X86_64Libraries() {
            if(clearVRIsV8_1_X86_64CleanUpCompleted) {
                return; // Nothing to be done.
            }
            String pluginsFolderRelativePath = "Assets/"; // Some people move our SDK around, so we should search the entire Assets folder.
            if(!System.IO.Directory.Exists(pluginsFolderRelativePath)) { 
                // Nothing to be done, the folder that should contain our file of interest does not even exist.
                return;
            }
            String[] fileNamesToRemove = new String[] {"MediaFlow_SA.dll", "libClearVRLinux.so", "ClearVRNativeRendererPlugin.dll"};
            List<String> fileEntriesFoundForRemoval = new List<String>();
            foreach(String fileNameToRemove in fileNamesToRemove) {
                string [] fileEntries = Directory.GetFiles(pluginsFolderRelativePath, fileNameToRemove, SearchOption.AllDirectories);
                if(fileEntries.Length > 0) {
                    fileEntriesFoundForRemoval.AddRange(fileEntries);
                }
            }
            if(fileEntriesFoundForRemoval.Count > 0) {
                bool result = EditorUtility.DisplayDialog("ClearVR - PC library clean-up", String.Format("Some PC libraries have been renamed in ClearVR SDK v8.1+. The following libraries (old names) will be removed from your project:\n\n{0}\n\nPlease click OK to continue.", string.Join("\n", fileEntriesFoundForRemoval.ToArray())), "OK");
                foreach(String fileEntry in fileEntriesFoundForRemoval) {
                    AssetDatabase.DeleteAsset(fileEntry);
                }
            }
            clearVRIsV8_1_X86_64CleanUpCompleted = true;
        }

        /// <summary>
        /// In v8.1 Rtaudi libs were removed.
        /// 
        /// Removed:
        /// * librtaudio.*
        /// 
        /// This method cleans-up by removing the old files in case they are found.
        /// </summary>
        private static void CleanUpRtAudioLibrariesOnLinux() {
            if(clearVRIsLibRtAudioCleanUpCompleted) {
                return; // Nothing to be done.
            }
            String pluginsFolderRelativePath = "Assets/"; // Some people move our SDK around, so we should search the entire Assets folder.
            if(!System.IO.Directory.Exists(pluginsFolderRelativePath)) { 
                // Nothing to be done, the folder that should contain our file of interest does not even exist.
                return;
            }
            String[] fileNamesToRemove = new String[] {"librtaudio.so","librtaudio.so.6"};
            List<String> fileEntriesFoundForRemoval = new List<String>();
            foreach(String fileNameToRemove in fileNamesToRemove) {
                string [] fileEntries = Directory.GetFiles(pluginsFolderRelativePath, fileNameToRemove, SearchOption.AllDirectories);
                if(fileEntries.Length > 0) {
                    fileEntriesFoundForRemoval.AddRange(fileEntries);
                }
            }
            if(fileEntriesFoundForRemoval.Count > 0) {
                bool result = EditorUtility.DisplayDialog("ClearVR - PC library clean-up", String.Format("Some PC libraries have been removed in ClearVR SDK v8.1+. The following libraries (old names) will be removed from your project:\n\n{0}\n\nPlease click OK to continue.", string.Join("\n", fileEntriesFoundForRemoval.ToArray())), "OK");
                foreach(String fileEntry in fileEntriesFoundForRemoval) {
                    AssetDatabase.DeleteAsset(fileEntry);
                }
            }
            clearVRIsLibRtAudioCleanUpCompleted = true;
        }

        /// <summary>
        /// Removed:
        /// * ucrtbase(d).*
        /// 
        /// This method cleans-up by removing the old files in case they are found.
        /// </summary>
        private static void CleanUpUcrtbaseLibrariesOnWindows() {
            if(clearVRIsUcrtbaseCleanUpCompleted) {
                return; // Nothing to be done.
            }
            String pluginsFolderRelativePath = "Assets/"; // Some people move our SDK around, so we should search the entire Assets folder.
            if(!System.IO.Directory.Exists(pluginsFolderRelativePath)) { 
                // Nothing to be done, the folder that should contain our file of interest does not even exist.
                return;
            }
            String[] fileNamesToRemove = new String[] {"ucrtbase.dll","ucrtbased.dll"};
            List<String> fileEntriesFoundForRemoval = new List<String>();
            foreach(String fileNameToRemove in fileNamesToRemove) {
                string [] fileEntries = Directory.GetFiles(pluginsFolderRelativePath, fileNameToRemove, SearchOption.AllDirectories);
                if(fileEntries.Length > 0) {
                    fileEntriesFoundForRemoval.AddRange(fileEntries);
                }
            }
            if(fileEntriesFoundForRemoval.Count > 0) {
                bool result = EditorUtility.DisplayDialog("ClearVR - PC library clean-up", String.Format("Some PC libraries have been removed in ClearVR SDK v8.2+. The following libraries (old names) will be removed from your project:\n\n{0}\n\nPlease click OK to continue.", string.Join("\n", fileEntriesFoundForRemoval.ToArray())), "OK");
                foreach(String fileEntry in fileEntriesFoundForRemoval) {
                    AssetDatabase.DeleteAsset(fileEntry);
                }
            }
            clearVRIsUcrtbaseCleanUpCompleted = true;
        }

        /// <summary>
        /// On V9+
        /// Removed:
        /// * ClearVRMeshManager.cs
        /// 
        /// This method cleans-up by removing the old files in case they are found.
        /// </summary>
        private static void CleanUpV9_ScriptFiles() {
            if(clearVRIsV9_ScriptCleanUpCompleted) {
                return; // Nothing to be done.
            }
            String pluginsFolderRelativePath = "Assets/"; // Some people move our SDK around, so we should search the entire Assets folder.
            if(!System.IO.Directory.Exists(pluginsFolderRelativePath)) { 
                // Nothing to be done, the folder that should contain our file of interest does not even exist.
                return;
            }
            String[] fileNamesToRemove = new String[] {"ClearVRMeshManager.cs"};
            List<String> fileEntriesFoundForRemoval = new List<String>();
            foreach(String fileNameToRemove in fileNamesToRemove) {
                string [] fileEntries = Directory.GetFiles(pluginsFolderRelativePath, fileNameToRemove, SearchOption.AllDirectories);
                if(fileEntries.Length > 0) {
                    fileEntriesFoundForRemoval.AddRange(fileEntries);
                }
            }
            if(fileEntriesFoundForRemoval.Count > 0) {
                bool result = EditorUtility.DisplayDialog("ClearVR - V9 Scripts clean-up", String.Format("Some scripts files have been removed in ClearVR SDK v9+. The following files will be removed from your project:\n\n{0}\n\nPlease click OK to continue.", string.Join("\n", fileEntriesFoundForRemoval.ToArray())), "OK");
                foreach(String fileEntry in fileEntriesFoundForRemoval) {
                    AssetDatabase.DeleteAsset(fileEntry);
                }
            }
            clearVRIsV9_ScriptCleanUpCompleted = true;
        }

        /// <summary>
        /// In V10, we removed all NIB files from the MediaFlow target. They were not used in the UnitySDK anyway.
        /// 
        /// Removed:
        /// * TMClearVRUIView.nib
        /// * TMClearVRAVSampleBufferDisplayLayer.nib
        /// 
        /// This method cleans-up by removing the old files in case they are found.
        /// </summary>
        private static void RemoveIOSNIBFiles() {
            if(clearVRIsIOSNIBCleanUpCompleted) {
                return; // Nothing to be done.
            }
            String pluginsFolderRelativePath = "Assets/"; // Some people move our SDK around, so we should search the entire Assets folder.
            if(!System.IO.Directory.Exists(pluginsFolderRelativePath)) { 
                // Nothing to be done, the folder that should contain our file of interest does not even exist.
                return;
            }
            // We have multiple (xc)frameworks, and each contains both of these files.
            String[] fileNamesToRemove = new String[] {"TMClearVRUIView.nib", "TMClearVRAVSampleBufferDisplayLayer.nib"};
            List<String> fileEntriesFoundForRemoval = new List<String>();
            foreach(String fileNameToRemove in fileNamesToRemove) {
                string [] fileEntries = Directory.GetFiles(pluginsFolderRelativePath, fileNameToRemove, SearchOption.AllDirectories);
                if(fileEntries.Length > 0) {
                    fileEntriesFoundForRemoval.AddRange(fileEntries);
                }
            }
            if(fileEntriesFoundForRemoval.Count > 0) {
                bool result = EditorUtility.DisplayDialog("ClearVR - iOS plugin file clean-up", String.Format("The following files that were part of the Tiledmedia MediaFlow.framework for iOS have been removed from Tiledmedia SDK v10+:\n\n{0}\n\nPlease click OK to continue.", string.Join("\n", fileNamesToRemove)), "OK");
                foreach(String fileEntry in fileEntriesFoundForRemoval) {
                    AssetDatabase.DeleteAsset(fileEntry);
                }
            }
            clearVRIsIOSNIBCleanUpCompleted = true;
        }

#if UNITY_EDITOR_LINUX
		/// <summary>
		/// Check if libav*.so symlink that links to libav*.so.* is available. If not, create it. If libav*.so.* is not available, but libav*.so symlink is found, the symlink gets removed as well. 
        /// Note: it's a known Unity issue that it doesn't accept libraries with multiple extensions. Therefore, we need to create symlinks. 
        /// Reference: https://issuetracker.unity3d.com/issues/versioned-linux-libraries-are-ignored-by-the-editor-when-they-are-imported
		/// </summary>
        public static void CheckForLibAVSymlinkOnLinux() {
            string x86_64PluginsFolderFullPath = Application.dataPath + "/ClearVR/Plugins/x86_64/";
            if(!System.IO.Directory.Exists(x86_64PluginsFolderFullPath)) {
                // Nothing to be done, the folder that should contain our file of interest does not even exist.
                return;
            }
            string[] libAVlibs = {"libavcodec.so.58","libavdevice.so.58","libavformat.so.58","libavutil.so.56","libswresample.so.3","libswscale.so.5","libavfilter.so.7","libpostproc.so.55"};
            string[] libAVSymlinks = {"libavcodec.so","libavdevice.so","libavformat.so","libavutil.so","libswresample.so","libswscale.so","libavfilter.so","libpostproc.so"};
            string relPath = "/ClearVR/Plugins/x86_64/"; 
            string absPath = Application.dataPath + relPath;
            bool printCreateSymlinkSuccess = false;
            for (int i = 0; i < libAVlibs.Length; i++) {
                string libAVlib = libAVlibs[i];
                string libAVSymlink = libAVSymlinks[i];
                string [] nonSymlinkEntries = Directory.GetFiles(x86_64PluginsFolderFullPath, libAVlib);
                string [] symlinkEntries = Directory.GetFiles(x86_64PluginsFolderFullPath, libAVSymlink);
			    if(nonSymlinkEntries.Length >= 1) {
                    if(symlinkEntries.Length == 0) {
                        // Symlink is missing
                        string stdout, stderr;
                        int returnCode;
                        if(!StartBlockingProcess("ln", String.Format(" -s {0} {1}",libAVlib,libAVSymlink), absPath, out stdout, out stderr, out returnCode, null, false, false)) {
                            EditorUtility.DisplayDialog("ClearVR - libav library setup failed", String.Format("Unable to automatically create a symlink from {0} to {1}. See Unity Console for details. Without this symlink, you cannot use ClearVR in your Unity editor on Linux nor in a build. Please create manually.",libAVlib,libAVSymlink), "OK");
                            UnityEngine.Debug.LogWarning(String.Format("[ClearVR] Unable to create symlink. Stdout: {0}, stderr: {1}. Returncode: {2}", stdout, stderr, returnCode));
                            printCreateSymlinkSuccess = false;
                        } else {
                            // Success!
                            printCreateSymlinkSuccess = true;
                            AssetDatabase.ImportAsset("Assets" + relPath + libAVSymlink);
                        }
                    } // else: both original file and symlink found, all OK!
			    } else {
                    if(symlinkEntries.Length >= 1) {
                    // The source of our symlink was removed, we should remove the symlink as well.
					    AssetDatabase.DeleteAsset("Assets" + relPath + libAVSymlink);
                        System.IO.File.Delete(x86_64PluginsFolderFullPath + libAVSymlink);
					    EditorUtility.DisplayDialog("ClearVR - libav library removed", String.Format("{0} (required for playback in the Linux Unity Editor) has been removed. Removing stale symlink {1} that was creating previously.",libAVlib,libAVSymlink), "OK");
                    } // else: no original nor symlink found, all OK!
                }

            }
            if(printCreateSymlinkSuccess){
                EditorUtility.DisplayDialog("ClearVR - libav library setup success", "Symlinks are required for libav in the Linux Unity Editor. This might trigger a one-time warning in the Unity Editor console about how a symlink can corrupt your project in certain cases. This can be ignored as the symlink is properly managed by the ClearVR SDK.", "OK");
            }

        }

        /// <summary>
		/// Check if libvulkan.so symlink that links to libvulkan.so.* is available. If not, create it. If libvulkan.so.* is not available, but libvulkan.so symlink is found, the symlink gets removed as well. 
        /// Note: it's a known Unity issue that it doesn't accept libraries with multiple extensions. Therefore, we need to create symlinks. 
        /// Reference: https://issuetracker.unity3d.com/issues/versioned-linux-libraries-are-ignored-by-the-editor-when-they-are-imported
		/// </summary>
        public static void CheckForVulkanSymlinkOnLinux() {
            string x86_64PluginsFolderFullPath = Application.dataPath + "/ClearVR/Plugins/x86_64/";
            if(!System.IO.Directory.Exists(x86_64PluginsFolderFullPath)) {
                // Nothing to be done, the folder that should contain our file of interest does not even exist.
                return;
            }
            string[] vulkanlibs = {"libvulkan.so.1"};
            string[] vulkanSymlinks = {"libvulkan.so"};
            string relPath = "/ClearVR/Plugins/x86_64/"; 
            string absPath = Application.dataPath + relPath;
            bool printCreateSymlinkSuccess = false;
            for (int i = 0; i < vulkanlibs.Length; i++) {
                string vulkanlib = vulkanlibs[i];
                string vulkanSymlink = vulkanSymlinks[i];
                string [] nonSymlinkEntries = Directory.GetFiles(x86_64PluginsFolderFullPath, vulkanlib);
                string [] symlinkEntries = Directory.GetFiles(x86_64PluginsFolderFullPath, vulkanSymlink);
			    if(nonSymlinkEntries.Length >= 1) {
                    if(symlinkEntries.Length == 0) {
                        // Symlink is missing
                        string stdout, stderr;
                        int returnCode;
                        if(!StartBlockingProcess("ln", String.Format(" -s {0} {1}",vulkanlib,vulkanSymlink), absPath, out stdout, out stderr, out returnCode, null, false, false)) {
                            EditorUtility.DisplayDialog("ClearVR - vulkan library setup failed", String.Format("Unable to automatically create a symlink from {0} to {1}. See Unity Console for details. Without this symlink, you cannot use ClearVR in your Unity editor on Linux nor in a build. Please create manually.",vulkanlib,vulkanSymlink), "OK");
                            UnityEngine.Debug.LogWarning(String.Format("[ClearVR] Unable to create symlink. Stdout: {0}, stderr: {1}. Returncode: {2}", stdout, stderr, returnCode));
                            printCreateSymlinkSuccess = false;
                        } else {
                            // Success!
                            printCreateSymlinkSuccess = true;
                            AssetDatabase.ImportAsset("Assets" + relPath + vulkanSymlink);
                        }
                    } // else: both original file and symlink found, all OK!
			    } else {
                    if(symlinkEntries.Length >= 1) {
                    // The source of our symlink was removed, we should remove the symlink as well.
					    AssetDatabase.DeleteAsset("Assets" + relPath + vulkanSymlink);
                        System.IO.File.Delete(x86_64PluginsFolderFullPath + vulkanSymlink);
					    EditorUtility.DisplayDialog("ClearVR - vulkan library removed", String.Format("{0} (required for playback in the Linux Unity Editor) has been removed. Removing stale symlink {1} that was creating previously.",vulkanlib,vulkanSymlink), "OK");
                    } // else: no original nor symlink found, all OK!
                }

            }
            if(printCreateSymlinkSuccess){
                EditorUtility.DisplayDialog("ClearVR - vulkan library setup success", "Symlinks are required for vulkan in the Linux Unity Editor. This might trigger a one-time warning in the Unity Editor console about how a symlink can corrupt your project in certain cases. This can be ignored as the symlink is properly managed by the ClearVR SDK.", "OK");
            }
        }

        // TODO: this is not yet working on Windows. For now, it is only used in a Linux specific code-path so we're good.
        public static bool StartBlockingProcess(string argCommand, string argCommandLineArguments, string argWorkingFolder, out string stdout, out string stderr, out int returnCode, Hashtable argEnvironmentVariables = null, bool argIsNonZeroExitCodeFatal = true, bool argBeVerbose = false) {
            int ExitCode;
            if(argCommand == SHELL_COMMAND) {
#if UNITY_EDITOR_WIN
                argCommand = "wsl.exe";
                String wslEnvExposedEnvVars = ":";
                if (argEnvironmentVariables != null) {
                    foreach (DictionaryEntry environmentVariable in argEnvironmentVariables) {
                        wslEnvExposedEnvVars = wslEnvExposedEnvVars + ":" + environmentVariable.Key;
                    }
                }
                wslEnvExposedEnvVars.TrimEnd(new char[]{':'});
                wslEnvExposedEnvVars = wslEnvExposedEnvVars.Replace("::", ":");
                
                argEnvironmentVariables.Add("WSLENV", Environment.GetEnvironmentVariable("WSLENV", EnvironmentVariableTarget.User) + wslEnvExposedEnvVars);
#else
                argCommand = "/bin/sh";
#endif
            }            
            Process process = new Process();
            process.StartInfo.FileName = argCommand;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.Arguments = argCommandLineArguments;
            process.StartInfo.WorkingDirectory = argWorkingFolder;
            String debugEnvironmentVariables = "";
            if (argEnvironmentVariables != null) {
                foreach(DictionaryEntry environmentVariable in argEnvironmentVariables) {
                    if(process.StartInfo.EnvironmentVariables.ContainsKey(environmentVariable.Key.ToString())) {
                        process.StartInfo.EnvironmentVariables[environmentVariable.Key.ToString()] = environmentVariable.Value.ToString();
                    } else {
                        process.StartInfo.EnvironmentVariables.Add(environmentVariable.Key.ToString(), environmentVariable.Value.ToString());
                    }
                    debugEnvironmentVariables = debugEnvironmentVariables + environmentVariable.Key.ToString() + " = " + environmentVariable.Value.ToString() + ",";
                }
            }
            if(argBeVerbose) {
                UnityEngine.Debug.Log(String.Format("Now running command: {0} with arguments: {1} in folder {2} and with env vars: {3}", argCommand, argCommandLineArguments, argWorkingFolder, debugEnvironmentVariables));
            }
            // Redirect the output
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardOutput = true;
            // Use asynchronous stdout and stderr reader, otherwise the process could block.
            // Source: https://stackoverflow.com/questions/139593/processstartinfo-hanging-on-waitforexit-why
            StringBuilder output = new StringBuilder();
            StringBuilder error = new StringBuilder();

            using (AutoResetEvent outputWaitHandle = new AutoResetEvent(false))
            using (AutoResetEvent errorWaitHandle = new AutoResetEvent(false))
            {
                process.OutputDataReceived += (sender, e) => {
                    if (e.Data == null)
                    {
                        outputWaitHandle.Set();
                    }
                    else
                    {
                        output.AppendLine(e.Data);
                    }
                };
                process.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data == null)
                    {
                        errorWaitHandle.Set();
                    }
                    else
                    {
                        error.AppendLine(e.Data);
                    }
                };

                process.Start();

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit(4000);
                process.WaitForExit();
            }

            ExitCode = process.ExitCode;
            stdout = output.ToString();
            stderr = error.ToString();
            returnCode = ExitCode;
            process.Close();
            if(argBeVerbose) {
                UnityEngine.Debug.Log(String.Format("Command completed. Exitcode; {0}, stdout: {1}, stderr: {2}", ExitCode, stdout, stderr));
            }
            if(ExitCode != 0) {
                string exception = String.Format("Command {0} could not complete. Stderr: {1}, stdout: {2}. Exit code: {3}", argCommand, output, error, returnCode);
                if(argIsNonZeroExitCodeFatal) {
                    throw new Exception(exception);
                } else {
                    UnityEngine.Debug.LogWarning(exception + ". This was a non-fatal error as argIsNonZeroExitCodeFatal is set to false");
                }
            }

            return (ExitCode == 0);
        }
#endif
        public static void EnableOrDisableNativePluginAndSetPlatform(String argFileFullPath, List<BuildTarget> argBuildTargets, List<bool> argEnabledOrDisabled, List<String> argArchitectures, bool argIsCompatibleWithAnyPlatform = false, bool argIsCompatbleWithEditor = false) {
            if(argFileFullPath.Contains(".meta")) {
                return;
            }
            // Parse any relative path (/../) into a proper absolute path
            argFileFullPath = Path.GetFullPath(argFileFullPath);
            // Shame on you Dropbox, only required for local builds
            // The (tiledmedia) part breaks the System.IO.Path.GetFullPath() API.
            argFileFullPath = argFileFullPath.Replace(" (tiledmedia)", "");
            string basePath = Path.GetFullPath(Directory.GetParent(Path.GetFullPath(Application.dataPath)).FullName);
            basePath = Path.GetFullPath(Application.dataPath + "/..");
            basePath = basePath.Replace(" (tiledmedia)", "");
            string relPath = argFileFullPath.Substring(basePath.Length + 1);
            // We now no longer rename files to .disabled. We just properly enable/disable them through the plugin inspector down-below.
            if(argFileFullPath.Contains(".disabled")) {
                if(System.IO.File.Exists(argFileFullPath)) {
                    String newFileFullPath = argFileFullPath.Replace(".disabled", "");
                    String newRelPath = relPath.Replace(".disabled", "");
                    AssetDatabase.RenameAsset(relPath, newRelPath);
                    if(!System.IO.File.Exists(newFileFullPath)) {
                        // Brute-force move
                        System.IO.File.Move(argFileFullPath, newFileFullPath);
                        System.IO.File.Move(argFileFullPath + ".meta", newFileFullPath + ".meta");
                    }
                    AssetDatabase.ImportAsset(newRelPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                    if(!System.IO.File.Exists(newFileFullPath)) {
                        throw new Exception(String.Format("Unable to copy {0} to {1}. Unity is messing things up again!", argFileFullPath, newFileFullPath));
                    }
                    relPath = newRelPath;
                    argFileFullPath = newFileFullPath;
                }
            }
            
            PluginImporter pi = PluginImporter.GetAtPath(relPath) as PluginImporter;
            if (pi == null) {
                if(argEnabledOrDisabled.Contains(true) /* any item needs enabling? */) {
                    throw new Exception(String.Format("Cannot find plugin: {0} (relative path: {1} for platforms: {2}", argFileFullPath, relPath, argBuildTargets));
                } else {
                    UnityEngine.Debug.LogWarning(String.Format("Cannot find {0}, but as we need to disable it anyway for BuildTarget {1} we will be skipping this file.", argFileFullPath, argBuildTargets[0]));
                    return;
                }
            }
            pi.SetCompatibleWithAnyPlatform(argIsCompatibleWithAnyPlatform);
            if(argIsCompatibleWithAnyPlatform) {
                foreach(BuildTarget buildTarget in Enum.GetValues(typeof(BuildTarget))) {
                    pi.SetExcludeFromAnyPlatform(buildTarget, false);
                }
                pi.SetExcludeEditorFromAnyPlatform(false);
            }
            if(!argIsCompatibleWithAnyPlatform) {
                pi.SetCompatibleWithEditor(argIsCompatbleWithEditor);
                if(argIsCompatbleWithEditor) {
                    if(argArchitectures != null) {
                        for(int i = 0; i < argArchitectures.Count; i++) {
                            if(argArchitectures[i] != null) {
                                pi.SetPlatformData("Editor", "CPU", argArchitectures[i]);
                            }
                        }
                    }
                }

                if(!(argBuildTargets.Count == argEnabledOrDisabled.Count && argEnabledOrDisabled.Count == argArchitectures.Count)) {
                    throw new Exception(String.Format("Fix your code, argument lists have unequal length when configuring plugin {0}.", argFileFullPath));
                }
                foreach(BuildTarget buildTarget in Enum.GetValues(typeof(BuildTarget))) {
                    if(buildTarget > 0) {
                        pi.SetCompatibleWithPlatform(buildTarget, false);
                    }
                }

                for(int i = 0; i < argBuildTargets.Count; i++) {
                    UnityEngine.Debug.Log(String.Format("About to set flag {0} on plugin {1} for BuildTarget: {2} (argIsCompatbleWithEditor: {3})", argEnabledOrDisabled[i], argFileFullPath, argBuildTargets[i], argIsCompatbleWithEditor));
                    pi.SetCompatibleWithPlatform(argBuildTargets[i], argEnabledOrDisabled[i]);
                    if(argArchitectures[i] != null) {
                        pi.SetPlatformData(argBuildTargets[i], "CPU", argArchitectures[i]);
                    }
                    // Provisioning for setting custom TargetPlatform specific plugin properties.
                    switch (argBuildTargets[i]) {
                        case BuildTarget.iOS:
                        case BuildTarget.tvOS:
                            break;
                        case BuildTarget.Android:
                            break;
                        case BuildTarget.StandaloneWindows:
                            if(argIsCompatbleWithEditor) {
                                pi.SetEditorData("OS", "WINDOWS");
                            }
                            break;
                        case BuildTarget.StandaloneWindows64:
                            if(argIsCompatbleWithEditor) {
                                pi.SetEditorData("OS", "WINDOWS");
                            }
                            break;
                        case BuildTarget.StandaloneLinux64:
                            if(argIsCompatbleWithEditor) {
                                pi.SetEditorData("OS", "LINUX");
                            }
                            break;
                        default:
                            throw new ArgumentException("Attempted EnablePluginPackage() for unsupported BuildTarget: " + argBuildTargets[i]);
                    }
                }
            }
            AssetDatabase.ImportAsset(relPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            // We MUST refrsh the AssetDatabase here, otherwise the UBE will randomly hangt at the end of the provisioning stage (yet another unity bug)
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();
        }
	}
}
#endif