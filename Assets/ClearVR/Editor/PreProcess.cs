using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.Diagnostics;
using UnityEditor.Build;
using System.Linq;
#if UNITY_IOS || UNITY_TVOS
using UnityEditor.iOS.Xcode.Extensions;
using UnityEditor.iOS.Xcode;
#endif
#if UNITY_2018_1_OR_NEWER
using UnityEditor.Build.Reporting;
#endif

namespace com.tiledmedia.clearvr {
#if UNITY_2018_1_OR_NEWER
    public class PreProcess : IPreprocessBuildWithReport {
#else
    public class PreProcess : IPreprocessBuild {
#endif    
		private static readonly string clearVRIsMissingClearVRAndroidActivityWarningDismissedKey = "clear_vr_is_missing_clear_vr_aandroid_activity_warning_dismissed";

		private static bool clearVRIsMissingClearVRAndroidActivityWarningDismissed {
			get {
				return PlayerPrefs.GetInt(clearVRIsMissingClearVRAndroidActivityWarningDismissedKey, 1) == 1;
			}

			set {
				PlayerPrefs.SetInt(clearVRIsMissingClearVRAndroidActivityWarningDismissedKey, value ? 1 : 0);
				PlayerPrefs.Save();
			}
		}



        public int callbackOrder { get { return 0; } }
#if UNITY_2018_1_OR_NEWER
        public void OnPreprocessBuild(BuildReport argReport) {
            BuildTarget buildTarget = argReport.summary.platform;
            String buildPath = argReport.summary.outputPath;
#else
        public void OnPreprocessBuild(BuildTarget buildTarget, string buildPath) {
#endif
            switch(buildTarget) {
                case BuildTarget.tvOS:
                case BuildTarget.iOS:
                    if(!IOSSelectProperNativeRendererPlugin(buildTarget, buildPath)) {
                        throw new Exception("[ClearVR] Unable to include correct native renderer plugin on iOS. The generated Xcode project might be broken. Please contact Tiledmedia.");
                    }

                    if (!IOSSelectProperMediaFlow(buildTarget, buildPath)) {
                        throw new Exception("[ClearVR] Unable to enable / disable the correct MediaFlow plugin. The generated Xcode project might be broken. Please contact Tiledmedia.");
                    }
                    break;
                case BuildTarget.Android:
                    AndroidCheckAndroidManifestXML(buildTarget, buildPath);
                    break;
                default:
                    break;
            } 
        }
        private struct ImportedPlugin {
            internal String version;
            internal bool isEnabled;
            internal BuildTarget BuildTarget;
            internal String architecture;

            internal ImportedPlugin(
                String version,
                bool isEnabled,
                BuildTarget buildTarget,
                String architecture
                ) {
                this.version = version;
                this.isEnabled = isEnabled;
                this.BuildTarget = buildTarget;
                this.architecture = architecture;
            }
        }


        private bool IOSSelectProperMediaFlow(BuildTarget buildTarget, String buildPath) {
#if UNITY_IOS || UNITY_TVOS
            String pluginsFolderRelativePath = "Assets/"; // Some people move our SDK around, so we should search the entire Assets folder.
            if (!System.IO.Directory.Exists(pluginsFolderRelativePath)) { 
                // Nothing to be done, the folder that should contain our file of interest does not even exist.
                return true;
            }
            String expectedFolderPath = "Assets/ClearVR/Plugins/iOS/";
            string xcframeworkName = "MediaFlow.xcframework";
            string frameworkName = "MediaFlow.framework";

            // All the frameworks inside the xcframework
            string[] xcframeworkFrameworks = new string[]{ "ios-x86_64-simulator", "ios-arm64", "tvos-arm64" };

            String xcframeworkPathInAssetsFolder = Path.Combine(expectedFolderPath, xcframeworkName);
            String frameworkPathInAssetsFolder = Path.Combine(expectedFolderPath, frameworkName);
            bool isXCFrameworkFound = System.IO.Directory.Exists(xcframeworkPathInAssetsFolder);
            bool isFrameworkFound = System.IO.Directory.Exists(frameworkPathInAssetsFolder);
            if (!isXCFrameworkFound) {
                string[] fileEntries = Directory.GetDirectories("Assets", xcframeworkName, SearchOption.AllDirectories);
                if(fileEntries.Length >= 1) {
                    // Even if > 1 file is found we continue.
                    isXCFrameworkFound = true;
                    xcframeworkPathInAssetsFolder = fileEntries[0];
                    if(fileEntries.Length > 1) {
                        UnityEngine.Debug.LogWarning(String.Format("Found multiple files with name {0} in project. This is not supported. Please check your project and remove duplicate files.", xcframeworkName));
                    }
                } // else: no files found
            }

            if(!isFrameworkFound) {
                string[] fileEntries = Directory.GetDirectories("Assets", frameworkName, SearchOption.AllDirectories);
                if(fileEntries.Length >= 1) {
                    // Even if > 1 file is found we continue.
                    isFrameworkFound = true;
                    frameworkPathInAssetsFolder = fileEntries[0];
                    if(fileEntries.Length > 1) {
                        UnityEngine.Debug.LogWarning(String.Format("Found multiple files with name {0} in project. This is not supported. Please check your project and remove duplicate files.", frameworkName));
                    }
                } // else: no files found
            }
            if (Convert.ToDouble(PlayerSettings.iOS.targetOSVersionString) < 13.0) {
                // iOS deployment target is lower than 13.0 which means that we should use the legacy MediaFlow package and disable the XCFramework.
                if (isXCFrameworkFound) {
                    foreach (string name in xcframeworkFrameworks) {
                        PluginImporter pi = PluginImporter.GetAtPath(Path.Combine(xcframeworkPathInAssetsFolder, name, frameworkName)) as PluginImporter;
                        if (pi == null) {
                            UnityEngine.Debug.LogWarning(String.Format("Cannot find {0} inside {1}. trying to continue disabling the other ones.", name, xcframeworkName));
                            continue;
                        }
                        pi.SetCompatibleWithPlatform(BuildTarget.iOS, false);
                    }
                }

                if (isFrameworkFound) {
                    PluginImporter pi = PluginImporter.GetAtPath(frameworkPathInAssetsFolder) as PluginImporter;
                    if (pi == null) {
                        UnityEngine.Debug.LogWarning(String.Format("Cannot find {0}. Unable to enable it.", frameworkPathInAssetsFolder));
                        return true;
                    }
                    pi.SetCompatibleWithPlatform(BuildTarget.iOS, true);
                }
            } else {
                // Recent iOS version. we can disable the legacy framework and enable the xcframework.
                if (isXCFrameworkFound) {
                    foreach (string name in xcframeworkFrameworks) {
                        PluginImporter pi = PluginImporter.GetAtPath(Path.Combine(xcframeworkPathInAssetsFolder, name, frameworkName)) as PluginImporter;
                        if (pi == null) {
                            UnityEngine.Debug.LogWarning(String.Format("Cannot find {0} inside {1}. trying to continue enabling the other ones.", name, xcframeworkName));
                            continue;
                        }
                        switch (name)
                        {
                            case "ios-x86_64-simulator":
                                pi.SetCompatibleWithPlatform(BuildTarget.iOS, true);
                                break;
                            case "ios-arm64":
                                pi.SetCompatibleWithPlatform(BuildTarget.iOS, true);
                                break;
                            case "tvos-arm64":
                                pi.SetCompatibleWithPlatform(BuildTarget.tvOS, true);
                                break;
                            default:
                                break;
                        }
                    }
                }

                if (isFrameworkFound) {
                    PluginImporter pi = PluginImporter.GetAtPath(frameworkPathInAssetsFolder) as PluginImporter;
                    if (pi == null) {
                        UnityEngine.Debug.LogWarning(String.Format("Cannot find {0}. Unable to disable it.", frameworkPathInAssetsFolder));
                        return true;
                    }
                    pi.SetCompatibleWithPlatform(BuildTarget.iOS, false);
                }
            }
#endif
            return true;
        }

        /// <summary>
        /// Include the Unity 2017, Unity 2018 or Unity 2020-compatible NativeRendererPlugin.
        /// </summary>
        /// <param name="argBuildTarget"></param>
        /// <param name="argBuildPath"></param>
        /// <returns></returns>
        private bool IOSSelectProperNativeRendererPlugin(BuildTarget argBuildTarget, String argBuildPath) {

            List<ImportedPlugin> importedPlugins = new List<ImportedPlugin> ();
#if UNITY_2018_1_OR_NEWER
#if UNITY_2020_2_OR_NEWER
            importedPlugins.Add(new ImportedPlugin("2017", false, BuildTarget.iOS, "AnyCPU"));
            importedPlugins.Add(new ImportedPlugin("2018", false, BuildTarget.iOS, "AnyCPU"));
            importedPlugins.Add(new ImportedPlugin("2020.2", true, BuildTarget.iOS, "AnyCPU"));
            importedPlugins.Add(new ImportedPlugin("2020.2-tvOS", true, BuildTarget.tvOS, "arm64"));
#else
            importedPlugins.Add(new ImportedPlugin("2017", false, BuildTarget.iOS, "AnyCPU"));
            importedPlugins.Add(new ImportedPlugin("2018", true, BuildTarget.iOS, "AnyCPU"));
            importedPlugins.Add(new ImportedPlugin("2020.2", false, BuildTarget.iOS, "AnyCPU"));
            importedPlugins.Add(new ImportedPlugin("2020.2-tvOS", false, BuildTarget.tvOS, "arm64"));
#endif
#else
            importedPlugins.Add(new ImportedPlugin("2017", true, BuildTarget.iOS, "AnyCPU"));
            importedPlugins.Add(new ImportedPlugin("2018", false, BuildTarget.iOS, "AnyCPU"));
            importedPlugins.Add(new ImportedPlugin("2020.2", false, BuildTarget.iOS, "AnyCPU"));
            importedPlugins.Add(new ImportedPlugin("2020.2-tvOS", false, BuildTarget.tvOS, "arm64"));
#endif
            String pluginsFolderRelativePath = "Assets/"; // Some people move our SDK around, so we should search the entire Assets folder.
            if(!System.IO.Directory.Exists(pluginsFolderRelativePath)) { 
                // Nothing to be done, the folder that should contain our file of interest does not even exist.
                return true;
            }
            string [] disabledFileEntries = Directory.GetFiles(pluginsFolderRelativePath, "libClearVRNativeRendererPlugin*.a.disabled", SearchOption.AllDirectories);
            if(disabledFileEntries.Length > 0) {
                throw new Exception("[ClearVR] Found 'libClearVRNativeRendererPlugin-XXXX.a.disabled' plugins. Since v8.0.6 renaming these files is no longer supported. Please rename these files by removing '.disabled' from their filename and try again.");
            }

            String filenameHeader = "libClearVRNativeRendererPlugin";
            String filenameWithVersionPlaceholder = filenameHeader + "-{0}.a";
            String expectedFolderPath = "Assets/ClearVR/Plugins/iOS/";
            foreach(ImportedPlugin importedPlugin in importedPlugins) {
                string filenameWithVersion = String.Format(filenameWithVersionPlaceholder, importedPlugin.version);
                String filePathInAssetsFolder = expectedFolderPath + filenameWithVersion;
                bool isFound = System.IO.File.Exists(filePathInAssetsFolder);
                if(!isFound) {
                    string[] fileEntries = Directory.GetFiles("Assets", filenameWithVersion, SearchOption.AllDirectories);
                    if(fileEntries.Length >= 1) {
                        // Even if > 1 file is found we continue.
                        isFound = true;
                        filePathInAssetsFolder = fileEntries[0];
                        if(fileEntries.Length > 1) {
                            UnityEngine.Debug.LogWarning(String.Format("Found multiple files with name {0} in project. This is not supported. Please check your project and remove duplicate files.", filenameWithVersion));
                        }
                    } // else: no files found
                }
                if(isFound) {
                    ClearVRSDKUpdater.EnableOrDisableNativePluginAndSetPlatform(filePathInAssetsFolder, new List<BuildTarget> {importedPlugin.BuildTarget}, new List<bool> {importedPlugin.isEnabled}, new List<string> {importedPlugin.architecture}, false, false);
                }
            }
            return true;
        }

        private bool AndroidCheckAndroidManifestXML(BuildTarget argBuildTarget, String argBuildPath) {
            int returnCode = ClearVRLinter.CheckClearVRActivityInAndroidManifest(); // 0 if ClearVR activity is set, -1 if xml path could not be found, -2 if no ClearVR activity was set.
            if(returnCode != 0 && !clearVRIsMissingClearVRAndroidActivityWarningDismissed) {
                String dialogBody = "";
                switch(returnCode) {
                    case -1:
                        dialogBody = String.Format("Unable to determine android activity from {0}. Please make sure to use the ClearVR activity through the ClearVR -> Android menu.", ClearVRLinter.ANDROID_MANIFEST_XML_FILE_FULL_PATH);
                        break;
                    case -2:
                        dialogBody = String.Format("The activity specified in {0} does not point to a known ClearVR compatible activity. Please make sure to setup the proper ClearVR activity through the ClearVR -> Android menu or contact Tiledmedia if you use your own Android activity.", ClearVRLinter.ANDROID_MANIFEST_XML_FILE_FULL_PATH);
                        break;
                    default:
                        throw new Exception("[ClearVR] An unknown return code was given by ClearVRLinter.CheckClearVRActivityInAndroidManifest(). Please contact Tiledmedia.");
                }
                if(!ClearVRLinter.GetIsUnityRunningInBatchmode()) {
                    bool result = EditorUtility.DisplayDialog("Check Android Activity", dialogBody, "Ok", "Don't warn me again");
                    if(result) { 
                        // Ok, understood. Return false to cancel the build
                        return false;
                    } else {
                        // Don't bother me again.
                        clearVRIsMissingClearVRAndroidActivityWarningDismissed = true;
                        EditorUtility.DisplayDialog("Warning", "You must take care of specifying the correct Andorid Activity yourself. If you fail to do so, random crashes can be expected.", "Ok");
                    }
                } else {
                    // running in batch mode, only log the text to the console.
                    if(returnCode != 0) {
                        UnityEngine.Debug.Log("[ClearVR] " + dialogBody);
                    }
                }
                // fallthrough to return true
            }
            return true;
        }

    }    
}