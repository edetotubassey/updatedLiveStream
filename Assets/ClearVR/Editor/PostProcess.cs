using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.Diagnostics;
using UnityEditor.Build;
#if UNITY_IOS || UNITY_TVOS
using UnityEditor.iOS.Xcode;
using UnityEditor.iOS.Xcode.Extensions;
#endif
#if UNITY_2018_1_OR_NEWER
using UnityEditor.Build.Reporting;
#endif

namespace com.tiledmedia.clearvr {
#if UNITY_2018_1_OR_NEWER
    public class PostProcess : IPostprocessBuildWithReport {
#else
    public class PostProcess : IPostprocessBuild {
#endif
        public int callbackOrder { get { return 0; } }
#if UNITY_2018_1_OR_NEWER
        public void OnPostprocessBuild(BuildReport argReport) {
            BuildTarget buildTarget = argReport.summary.platform;
            String buildPath = argReport.summary.outputPath;
#else
        public void OnPostprocessBuild(BuildTarget buildTarget, string buildPath) {
#endif
            switch(buildTarget) {
                case BuildTarget.iOS:
#if UNITY_IOS
                    var projPath = buildPath + "/Unity-iPhone.xcodeproj/project.pbxproj";
                    var proj = new PBXProject();
                    proj.ReadFromFile(projPath);
#if UNITY_2019_3_OR_NEWER
                    // On Unity 2019, there are two targets:
                    // 1. The main target: this MUST "Embed and Sign" the MediaFlow.framework
                    // 2. If MediaFlow.framework is added to the "Link Binary with Framework" section, we have to update the FRAMEWORK_SEARCH_PATHS to include $(PROJECT_DIR)/Frameworks/ClearVR/Plugins/iOS
                    // 3. The UnityFramework target: MUST include MediaFlow.framework but NOT embed it and MUST "Link Binary with Framework" (required)
                    // 4. "Link Binary with Framework" has been removed from recent Xcode version (12.x +).
                    // 5. XCFrameworks are not officially supported by Unity yet. this means that we need to do some post process magic here to use only the .framework that we target in PlayerSettings.iOS.sdkVersion.
                    var mainTargetGuid = proj.GetUnityMainTargetGuid();
                    var frameworkTargetGuid = proj.GetUnityFrameworkTargetGuid();
#else
                    var mainTargetGuid = proj.TargetGuidByName(PBXProject.GetUnityTargetName());
#endif
                    // Note: in Unity, bitcode is enabled by default.
                    proj.AddBuildProperty(mainTargetGuid, "LD_RUNPATH_SEARCH_PATHS", "@executable_path/Frameworks $(PROJECT_DIR)/lib/$(CONFIGURATION) $(inherited)");
                    proj.AddBuildProperty(mainTargetGuid, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "YES");
                    proj.AddBuildProperty(mainTargetGuid, "DYLIB_INSTALL_NAME_BASE", "@rpath");
                    proj.AddBuildProperty(mainTargetGuid, "LD_DYLIB_INSTALL_NAME",
                        "@executable_path/../Frameworks/$(EXECUTABLE_PATH)");
                    proj.AddBuildProperty(mainTargetGuid, "DEFINES_MODULE", "YES");
                    proj.AddBuildProperty(mainTargetGuid, "COREML_CODEGEN_LANGUAGE", "Swift");
                    proj.SetBuildProperty(mainTargetGuid, "LD_RUNPATH_SEARCH_PATHS", "$(inherited) @executable_path/Frameworks");

                    // Get path for MediaFlow.framework.
                    // Some customers move the ClearVR SDK around in their project, so we should search the entire Assets folder.
                    String pluginsFolderRelativePath = "Assets/"; 
                    if(!System.IO.Directory.Exists(pluginsFolderRelativePath)) { 
                        // Nothing to be done, the folder that should contain our file of interest does not even exist. We continue silently.
                        return;
                    }

                    // Under the assumption than no one renames the MediaFlow.xcframework and the folder names, this is hard-coded.
                    String expectedFolderPath = "Assets/ClearVR/Plugins/iOS/";
                    string xcframeworkName = "MediaFlow.xcframework";
                    string frameworkName = "MediaFlow.framework";
                    string simulatorFolderName = "ios-x86_64-simulator";
                    string deviceFolderName = "ios-arm64";
                    string tvosFolderName = "tvos-arm64";

                    // Remove (broken) links to MediaFlow.xcframework (and its derivatives) and MediaFlow.framework which were added by Unity automatically. We add it manually down-below
                    foreach(String realRelativePath in new String[] {"Frameworks/ClearVR/Plugins/iOS/MediaFlow.xcframework", 
                            "Frameworks/ClearVR/Plugins/iOS/MediaFlow.framework",
                            Path.Combine(Path.Combine("Frameworks/ClearVR/Plugins/iOS/MediaFlow.xcframework/", simulatorFolderName), frameworkName),
                            Path.Combine(Path.Combine("Frameworks/ClearVR/Plugins/iOS/MediaFlow.xcframework/", tvosFolderName), frameworkName),
                            Path.Combine(Path.Combine("Frameworks/ClearVR/Plugins/iOS/MediaFlow.xcframework/", deviceFolderName), frameworkName)}) {
                        String fileGuid2 = proj.FindFileGuidByRealPath(realRelativePath);
                        if (fileGuid2 != null) {
                            UnityEngine.Debug.Log(String.Format("Removing reference to {0}", realRelativePath));
                            proj.RemoveFrameworkFromProject(fileGuid2, System.IO.Path.GetFileName(realRelativePath));
                            proj.RemoveFile(fileGuid2);
                        }
                    }

                    String mediaFlowPathInProjectWithoutAssetsFolder = null;

                    String xcframeworkPathInAssetsFolder = Path.Combine(expectedFolderPath + xcframeworkName);
                    String frameworkPathInAssetsFolder = Path.Combine(expectedFolderPath + frameworkName);
                    bool isXCFrameworkFound = System.IO.Directory.Exists(xcframeworkPathInAssetsFolder);
                    bool isFrameworkFound = System.IO.Directory.Exists(frameworkPathInAssetsFolder);

                    if (!isXCFrameworkFound) {
                        string[] entries = Directory.GetDirectories(pluginsFolderRelativePath, xcframeworkName, SearchOption.AllDirectories);
                        if(entries.Length >= 1) {
                            // Even if > 1 file is found we continue.
                            isXCFrameworkFound = true;
                            xcframeworkPathInAssetsFolder = entries[0];
                            if(entries.Length > 1) {
                                UnityEngine.Debug.LogWarning(String.Format("Found multiple files with name {0} in project. This is not supported. Please check your project and remove duplicate files.", xcframeworkName));
                            }
                        } // else: no files found
                    }

                    if(!isFrameworkFound) {
                        string[] entries = Directory.GetDirectories(pluginsFolderRelativePath, frameworkName, SearchOption.AllDirectories);
                        if(entries.Length >= 1) {
                            // Even if > 1 file is found we continue.
                            isFrameworkFound = true;
                            frameworkPathInAssetsFolder = entries[0];
                            if(entries.Length > 1) {
                                UnityEngine.Debug.LogWarning(String.Format("Found multiple files with name {0} in project. This is not supported. Please check your project and remove duplicate files.", frameworkName));
                            }
                        } // else: no files found
                    }
                    
                    string deviceFrameworkPath = Path.Combine(deviceFolderName, frameworkName);
                    string simulatorFrameworkPath = Path.Combine(simulatorFolderName, frameworkName);
                    string tvosFrameworkPath = Path.Combine(tvosFolderName, frameworkName);
                    iOSSdkVersion target = PlayerSettings.iOS.sdkVersion;

                    if (Convert.ToDouble(PlayerSettings.iOS.targetOSVersionString) < 13.0) {
                        if (target == iOSSdkVersion.SimulatorSDK) {
                            throw new Exception("[ClearVR] Simulator is not supported in the ClearVR legacy SDK. Please upgrade to 13.0 and start using the XCFramework.");
                        }

                        string[] xcframeworkGUIDs = new string[]{
                            proj.FindFileGuidByProjectPath(Path.Combine(Path.Combine("Frameworks/ClearVR/Plugins/iOS/MediaFlow.xcframework/", tvosFolderName), frameworkName)),
                            proj.FindFileGuidByProjectPath(Path.Combine(Path.Combine("Frameworks/ClearVR/Plugins/iOS/MediaFlow.xcframework/", simulatorFolderName), frameworkName)),
                            proj.FindFileGuidByProjectPath(Path.Combine(Path.Combine("Frameworks/ClearVR/Plugins/iOS/MediaFlow.xcframework/", deviceFolderName), frameworkName))
                        };
                        if (isXCFrameworkFound) {
                            foreach (string fw in xcframeworkGUIDs) {
                                if (!String.IsNullOrEmpty(fw)) {
#if UNITY_2019_3_OR_NEWER
                                    // Remove the unused framework from the UnityFramework.framework target inside the xcode project.
                                    proj.RemoveFileFromBuild(frameworkTargetGuid, fw);
#else
                                    // In Unity version < 2019 there is no UnityFramework so we must remove it from the main target instead.
                                    proj.RemoveFileFromBuild(mainTargetGuid, fw);
#endif
                                }
                            }
                        }

                        // Legacy framework is being used.
                        if (isFrameworkFound) {
                            mediaFlowPathInProjectWithoutAssetsFolder = frameworkPathInAssetsFolder.Replace("Assets/", "").Replace(frameworkName, "");
                        } else {
                            throw new Exception("Trying to build for iOS minimum target < 13.0 but MediaFlow.framework could not be found. Please fix your plugin setup.");
                        }
                    } else {
                        // XCFramework is being used.
                        if (isXCFrameworkFound) {
                            // The GUID of the framework inside the xcframework that is NOT the target of the build.
                            string mediaFlowPathWithinXCFramework = xcframeworkPathInAssetsFolder;

                            string[] unusedFrameworkGUIDs = new string[2];
                            mediaFlowPathWithinXCFramework = Path.Combine(mediaFlowPathWithinXCFramework, tvosFrameworkPath);
                            string relativeTVOSFrameworkPath = Path.Combine("Frameworks/ClearVR/Plugins/iOS/MediaFlow.xcframework/", tvosFolderName);
                            unusedFrameworkGUIDs[0] = proj.FindFileGuidByProjectPath(Path.Combine(relativeTVOSFrameworkPath, frameworkName));
                            // reset back to original value of "Assets/ClearVR/Plugins/iOS/MediaFlow.xcframework"
                            mediaFlowPathWithinXCFramework = xcframeworkPathInAssetsFolder;
                            
                            // Find the path to the .framework file within the .xcframework depending on the iOSSDKVersion specified in the player settings.
                            switch (target) {
                                case iOSSdkVersion.DeviceSDK:
                                    mediaFlowPathWithinXCFramework = Path.Combine(mediaFlowPathWithinXCFramework, deviceFrameworkPath);
                                    // Make sure to remove the reference of the simulator mediaflow framework or else XCode refuses to build as the framework conflicts with the target device.
                                    string relativeDeviceFrameworkPath = Path.Combine("Frameworks/ClearVR/Plugins/iOS/MediaFlow.xcframework/", simulatorFolderName);
                                    unusedFrameworkGUIDs[1] = proj.FindFileGuidByProjectPath(Path.Combine(relativeDeviceFrameworkPath, frameworkName));
                                    break;
                                case iOSSdkVersion.SimulatorSDK:
                                    mediaFlowPathWithinXCFramework = Path.Combine(mediaFlowPathWithinXCFramework, simulatorFrameworkPath);
                                    // Make sure to remove the reference of the simulator mediaflow framework or else XCode refuses to build as the framework conflicts with the target device.
                                    string relativeSimulatorFrameworkPath = Path.Combine("Frameworks/ClearVR/Plugins/iOS/MediaFlow.xcframework/", deviceFolderName);
                                    unusedFrameworkGUIDs[1] = proj.FindFileGuidByProjectPath(Path.Combine(relativeSimulatorFrameworkPath, frameworkName));
                                    break;
                                default:
                                    throw new Exception(String.Format("[ClearVR] Unable to find MediaFlow.framework with IOS SDK Version: {0}", target));
                            }
                            foreach (string fw in unusedFrameworkGUIDs) {
                                if (!String.IsNullOrEmpty(fw)) {
        #if UNITY_2019_3_OR_NEWER
                                    // Remove the unused framework from the UnityFramework.framework target inside the xcode project.
                                    proj.RemoveFileFromBuild(frameworkTargetGuid, fw);
        #else
                                    // In Unity version < 2019 there is no UnityFramework so we must remove it from the main target instead.
                                    proj.RemoveFileFromBuild(mainTargetGuid, fw);
        #endif   
                                }
                            }
                            // We strip Assets/ folder and the filename.
                            mediaFlowPathInProjectWithoutAssetsFolder = mediaFlowPathWithinXCFramework.Replace("Assets/", "").Replace(frameworkName, "");
                        }
                    }

                    if (mediaFlowPathInProjectWithoutAssetsFolder == null) {
                        return;
                    }
                    string frameworkRelativePath = String.Format("Frameworks/{0}", mediaFlowPathInProjectWithoutAssetsFolder);

                    string frameworkRelativeFilePath = frameworkRelativePath + frameworkName;
                    // This is the ¨virtual¨ path in the xcode project structure.
                    string frameworkProjectPath = "Frameworks/" + frameworkName; 
                    string mainTargetFrameworkPhaseGuid = proj.AddFrameworksBuildPhase(mainTargetGuid);
                    // Add MediaFlow.framework to project and to "Linked Frameworks and Libraries"
                    String fileGuid = proj.AddFile(frameworkRelativeFilePath, frameworkProjectPath, PBXSourceTree.Source);
#if UNITY_2019_3_OR_NEWER
                    string frameworkFrameworkPhaseGuid = proj.AddFrameworksBuildPhase(frameworkTargetGuid);
                    proj.RemoveFileFromBuild(frameworkFrameworkPhaseGuid, fileGuid);
                    proj.AddFileToBuildSection(frameworkTargetGuid, frameworkFrameworkPhaseGuid, fileGuid);
                    /// Update the FRAMEWORK_SEARCH_PATHS. This adds, not overwrites and duplicate entries are ignored (ref. https://docs.unity3d.com/ScriptReference/iOS.Xcode.PBXProject.AddBuildProperty.html)
                    proj.AddBuildProperty(mainTargetGuid, "FRAMEWORK_SEARCH_PATHS", String.Format("$(inherited)\n$(PROJECT_DIR)/{0}", frameworkRelativePath));
#endif
                    proj.AddFileToBuildSection(mainTargetGuid, mainTargetFrameworkPhaseGuid, fileGuid);
                    // Add MediaFlow.framework to "EmbeddedBinaries"
                    PBXProjectExtensions.AddFileToEmbedFrameworks(proj, mainTargetGuid, fileGuid);
                    proj.WriteToFile(projPath);
#endif
                    break;

                case BuildTarget.tvOS:
#if  UNITY_TVOS
                    var projPath = buildPath + "/Unity-iPhone.xcodeproj/project.pbxproj";
                    var proj = new PBXProject();
                    proj.ReadFromFile(projPath);
#if UNITY_2019_3_OR_NEWER
                    // On Unity 2019, there are two targets:
                    // 1. The main target: this MUST "Embed and Sign" the MediaFlow.framework
                    // 2. If MediaFlow.framework is added to the "Link Binary with Framework" section, we have to update the FRAMEWORK_SEARCH_PATHS to include $(PROJECT_DIR)/Frameworks/ClearVR/Plugins/iOS
                    // 3. The UnityFramework target: MUST include MediaFlow.framework but NOT embed it and MUST "Link Binary with Framework" (required)
                    // 4. "Link Binary with Framework" has been removed from recent Xcode version (12.x +).
                    // 5. XCFrameworks are not officially supported by Unity yet. this means that we need to do some post process magic here to use only the .framework that we target in PlayerSettings.iOS.sdkVersion.
                    var mainTargetGuid = proj.GetUnityMainTargetGuid();
                    var frameworkTargetGuid = proj.GetUnityFrameworkTargetGuid();
#else
                    var mainTargetGuid = proj.TargetGuidByName(PBXProject.GetUnityTargetName());
#endif
                    // Note: in Unity, bitcode is enabled by default.
                    proj.AddBuildProperty(mainTargetGuid, "LD_RUNPATH_SEARCH_PATHS", "@executable_path/Frameworks $(PROJECT_DIR)/lib/$(CONFIGURATION) $(inherited)");
                    proj.AddBuildProperty(mainTargetGuid, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "YES");
                    proj.AddBuildProperty(mainTargetGuid, "DYLIB_INSTALL_NAME_BASE", "@rpath");
                    proj.AddBuildProperty(mainTargetGuid, "LD_DYLIB_INSTALL_NAME",
                        "@executable_path/../Frameworks/$(EXECUTABLE_PATH)");
                    proj.AddBuildProperty(mainTargetGuid, "DEFINES_MODULE", "YES");
                    proj.AddBuildProperty(mainTargetGuid, "COREML_CODEGEN_LANGUAGE", "Swift");
                    proj.SetBuildProperty(mainTargetGuid, "LD_RUNPATH_SEARCH_PATHS", "$(inherited) @executable_path/Frameworks");

                    // Remove broken link to MediaFlow.xcframework which was added by Unity automatically.
                    string fileGuid = proj.FindFileGuidByProjectPath("Frameworks/ClearVR/Plugins/iOS/MediaFlow.xcframework");
                    if (fileGuid != null) {
                        proj.RemoveFrameworkFromProject(fileGuid, "MediaFlow.xcframework");
                        proj.RemoveFile(fileGuid);
                    }

                    // Get path for MediaFlow.framework.
                    // Some customers move the ClearVR SDK around in their project, so we should search the entire Assets folder.
                    string pluginsFolderRelativePath = "Assets/";
                    if(!System.IO.Directory.Exists(pluginsFolderRelativePath)) {
                        // Nothing to be done, the folder that should contain our file of interest does not even exist. We continue silently.
                        return;
                    }

                    // Under the assumption than no one renames the MediaFlow.xcframework and the folder names, this is hard-coded.
                    string expectedFolderPath = "Assets/ClearVR/Plugins/iOS/";
                    string xcframeworkName = "MediaFlow.xcframework";
                    string frameworkName = "MediaFlow.framework";
                    string tvosFolderName = "tvos-arm64";

                    string xcframeworkPathInAssetsFolder = Path.Combine(expectedFolderPath + xcframeworkName);
                    bool isXCFrameworkFound = System.IO.Directory.Exists(xcframeworkPathInAssetsFolder);

                    if (!isXCFrameworkFound) {
                        string[] entries = Directory.GetDirectories(pluginsFolderRelativePath, xcframeworkName, SearchOption.AllDirectories);
                        if(entries.Length >= 1) {
                            // Even if > 1 file is found we continue.
                            isXCFrameworkFound = true;
                            xcframeworkPathInAssetsFolder = entries[0];
                            if(entries.Length > 1) {
                                UnityEngine.Debug.LogWarning(String.Format("Found multiple files with name {0} in project. This is not supported. Please check your project and remove duplicate files.", xcframeworkName));
                            }
                        } else {
                            return;
                        }
                    }
                    string tvosFrameworkPath = Path.Combine(tvosFolderName, frameworkName);
                    string mediaFlowPathWithinXCFramework = Path.Combine(xcframeworkPathInAssetsFolder, tvosFrameworkPath);
                    string mediaFlowPathInProjectWithoutAssetsFolder = mediaFlowPathWithinXCFramework.Replace("Assets/", "").Replace(frameworkName, "");

                    string frameworkRelativePath = String.Format("Frameworks/{0}", mediaFlowPathInProjectWithoutAssetsFolder);

                    string frameworkRelativeFilePath = frameworkRelativePath + frameworkName;
                    // This is the ¨virtual¨ path in the xcode project structure.
                    string frameworkProjectPath = "Frameworks/" + frameworkName; 
                    string mainTargetFrameworkPhaseGuid = proj.AddFrameworksBuildPhase(mainTargetGuid);
                    // Add MediaFlow.framework to project and to "Linked Frameworks and Libraries"
                    fileGuid = proj.AddFile(frameworkRelativeFilePath, frameworkProjectPath, PBXSourceTree.Source);
#if UNITY_2019_3_OR_NEWER
                    string frameworkFrameworkPhaseGuid = proj.AddFrameworksBuildPhase(frameworkTargetGuid);
                    proj.AddFileToBuildSection(frameworkTargetGuid, frameworkFrameworkPhaseGuid, fileGuid);
                    /// Update the FRAMEWORK_SEARCH_PATHS. This adds, not overwrites and duplicate entries are ignored (ref. https://docs.unity3d.com/ScriptReference/iOS.Xcode.PBXProject.AddBuildProperty.html)
                    proj.AddBuildProperty(mainTargetGuid, "FRAMEWORK_SEARCH_PATHS", String.Format("$(inherited)\n$(PROJECT_DIR)/{0}", frameworkRelativePath));
#endif
                    proj.AddFileToBuildSection(mainTargetGuid, mainTargetFrameworkPhaseGuid, fileGuid);
                    // Add MediaFlow.framework to "EmbeddedBinaries"
                    PBXProjectExtensions.AddFileToEmbedFrameworks(proj, mainTargetGuid, fileGuid);
                    proj.WriteToFile(projPath);
#endif
                    break;
                case BuildTarget.Android:
                    // Intentionally left empty.
                    break;
                default:
                    break;
            }
        }
    }
}