using System;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

namespace com.tiledmedia.clearvr {
    public static class ClearVREditorUtils {
        public static GUIStyle errorStyle = new GUIStyle() {
            fontSize = 11,
            fontStyle = FontStyle.Italic,
            normal = new GUIStyleState() {
            textColor = Color.red
            }
        };
        public static GUIStyle warningStyle = new GUIStyle() {
            fontSize = 11,
            fontStyle = FontStyle.Italic,
            normal = new GUIStyleState() {
            textColor = Color.yellow
            }
        };
        public static GUIStyle infoStyle = new GUIStyle() {
            fontSize = 11,
            fontStyle = FontStyle.Italic,
            normal = new GUIStyleState() {
            textColor = Color.green,
            }
        };


        /// <summary>
        /// Check if the specified XRLoader is activated and loaded.
        /// This is equivalent to querying the UnityEditor.XR.Management.XRManager.activeLoaders list.
        /// </summary>
        /// <param name="argXRLoaderName">The XRLoader name to check for. Case insensitive.</param>
        /// <returns>True if found, false otherwise. This will always return false on UNITY_2018 and older.</returns>
        public static bool GetIsXRLoaderActive(String argXRLoaderName, BuildTargetGroup argBuildTargetGroup) {
#if UNITY_2019_3_OR_NEWER
            // We use reflection as we cannot know for sure that these symbols are available. For example, in flat apps they will be not present.
            Type xrGeneralSettingsPerBuildTargetType = Utils.GetType("UnityEditor.XR.Management.XRGeneralSettingsPerBuildTarget");
            Type xrGeneralSettingsType = Utils.GetType("UnityEngine.XR.Management.XRGeneralSettings");
            Type xrManagerSettingsType = Utils.GetType("UnityEngine.XR.Management.XRManagerSettings");
            Type xrLoaderType = Utils.GetType("UnityEngine.XR.Management.XRLoader");
            if(xrGeneralSettingsPerBuildTargetType != null && xrGeneralSettingsType != null && xrManagerSettingsType != null && xrLoaderType != null) {
                var ctor = xrGeneralSettingsPerBuildTargetType.GetConstructor(new Type[] { });
                UnityEngine.Object buildTargetSettings = ScriptableObject.CreateInstance(xrGeneralSettingsPerBuildTargetType);
                FieldInfo k_SettingsKeyFieldInfo = xrGeneralSettingsType.GetField("k_SettingsKey", BindingFlags.Public | BindingFlags.Static);
                if(k_SettingsKeyFieldInfo != null) {
                    EditorBuildSettings.TryGetConfigObject((String) k_SettingsKeyFieldInfo.GetValue(null), out buildTargetSettings);
                    if(buildTargetSettings != null) {
                        MethodInfo settingsForBuildTargetMethodInfo = xrGeneralSettingsPerBuildTargetType.GetMethod("SettingsForBuildTarget");
                        var settings = settingsForBuildTargetMethodInfo.Invoke(buildTargetSettings, new object[] {argBuildTargetGroup});
                        if(settings != null) {
                            PropertyInfo managerPropertyInfo = xrGeneralSettingsType.GetProperty("Manager");
                            var manager = managerPropertyInfo.GetValue(settings);
                            if(manager != null) {
                                PropertyInfo loadersProperty = xrManagerSettingsType.GetProperty("activeLoaders");
                                if(loadersProperty == null) {
                                    // Try the, in newer versions deprecated, property.
                                    loadersProperty = xrManagerSettingsType.GetProperty("loaders");
                                }
                                if(loadersProperty != null) {
                                    IReadOnlyList<ScriptableObject> loaders = (IReadOnlyList<ScriptableObject>)loadersProperty.GetValue(manager);
                                    if(loaders != null) {
                                        foreach(ScriptableObject loader in loaders) {
                                            if(loader.name.ToLower().Trim() == argXRLoaderName.ToLower().Trim()) {
                                                return true;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
				}
			}
            return false;
#else
            return false;
#endif
        }
    }
}