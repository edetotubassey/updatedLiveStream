using System;
using System.Text;
using UnityEngine;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine.Rendering;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
#if UNITY_2019_1_OR_NEWER
using UnityEngine.XR;
#endif
namespace com.tiledmedia.clearvr {
    /// <summary>
    /// The utils class provides various convenience methods at your disposal.
    /// Public APIs on this class will maintained.
    /// </summary>
    public static class Utils {
        private static bool _isVrDevicePresent; // Do not set a value yet, assume "default"
        private static DeviceTypes _deviceType = DeviceTypes.Unknown;
        private static bool _isDeviceTypeDetected = false;
        private static VRAPITypes _vrAPIType = VRAPITypes.Unknown;
        private static bool _isVRAPITypeDetected = false;
#if UNITY_2019_1_OR_NEWER
        // These string literals are the pretty-typed version of the XRLoader class (which implements the XRLoaderHelper interface)
        public readonly static String PICO_XR_LOADER_NAME = "PXR Loader"; // PXR_Loader
        public readonly static String OCULUS_XR_LOADER_NAME = "Oculus Loader"; // OculusLoader
        public readonly static String SKYWORTH_XR_LOADER_NAME = "Skyworth Loader"; // SkyworthLoader
        public readonly static String WAVE_XR_LOADER_NAME = "Wave XR Loader"; // WaveXRLoader
#else
        public readonly static String PICO_XR_LOADER_NAME = "CLEAR_VR_UNSUPPORTED"; // XRLoaders ares only supported on Unity 2019_1+
        public readonly static String OCULUS_XR_LOADER_NAME = "CLEAR_VR_UNSUPPORTED";
        public readonly static String SKYWORTH_XR_LOADER_NAME = "CLEAR_VR_UNSUPPORTED";
        public readonly static String WAVE_XR_LOADER_NAME = "CLEAR_VR_UNSUPPORTED";
#endif
#if UNITY_2019_3_OR_NEWER
        private class XRDisplaySubSystemToDeviceType {
            // Case insensitive!
            // Can be found in the UnitySubsystemsManifest.json
            // When adding, double check that this values matches up with the one in the Runtime XR_Loader.cs: `CreateSubsystem<XRDisplaySubsystemDescriptor, XRDisplaySubsystem>(displaySubsystemDescriptors, "ABCDE Display");`
            public String[] displaySubSystemIDs;
            public Tuple<DeviceTypes, RuntimePlatform>[] deviceTypeOnPlatform;
            public override String ToString() {
                StringBuilder tupleString = new StringBuilder();
                foreach (var deviceTypeOnPlatform in this.deviceTypeOnPlatform) {
                    tupleString.Append(String.Format("{{{0} - {1}}} ,", deviceTypeOnPlatform.Item1, String.Join(",", deviceTypeOnPlatform.Item2)));
                }
                tupleString.Length = Math.Max(0, tupleString.Length - 2);
                return String.Format("{0}, {1}", String.Join(",", displaySubSystemIDs), tupleString);
            }
        }

        // Some of the XRDisplaySubsystems are detected via other means, notably Oculus where we directly invoke an OVRPlugin API to get the exact headset type.
        private readonly static XRDisplaySubSystemToDeviceType[] AUTO_DETECT_XR_DISPLAY_SUBSYSTEMS = new XRDisplaySubSystemToDeviceType[] {
            new XRDisplaySubSystemToDeviceType() {
                displaySubSystemIDs = new String[] { "cardboard display", "cardboarddisplay", "cardboard", /* "display": very old cardboard SDKs (pre 2022) just had "display" as name. This has been patched after we proposed a patch to them. */  },
                deviceTypeOnPlatform = new Tuple<DeviceTypes, RuntimePlatform>[] {
                    new Tuple<DeviceTypes, RuntimePlatform>(DeviceTypes.AndroidGenericCardboard, RuntimePlatform.Android),
                    new Tuple<DeviceTypes, RuntimePlatform>(DeviceTypes.IOSGenericCardboard, RuntimePlatform.IPhonePlayer),
                }
            },
            new XRDisplaySubSystemToDeviceType() {
                displaySubSystemIDs = new String[] { "picoxr display", "picoxr", "pico" },
                deviceTypeOnPlatform = new Tuple<DeviceTypes, RuntimePlatform>[] {
                    new Tuple<DeviceTypes, RuntimePlatform>(DeviceTypes.AndroidPicoVRGeneric, RuntimePlatform.Android),
                }
            },
            new XRDisplaySubSystemToDeviceType() {
                displaySubSystemIDs = new String[] { "oculus display", "oculus" },
                deviceTypeOnPlatform = new Tuple<DeviceTypes, RuntimePlatform>[] {
                    new Tuple<DeviceTypes, RuntimePlatform>(DeviceTypes.AndroidOculusGeneric, RuntimePlatform.Android),
                }
            },
            new XRDisplaySubSystemToDeviceType() {
                displaySubSystemIDs = new String[] { "gsxr display", "gsxr" },
                deviceTypeOnPlatform = new Tuple<DeviceTypes, RuntimePlatform>[] {
                    new Tuple<DeviceTypes, RuntimePlatform>(DeviceTypes.AndroidGSXRGeneric, RuntimePlatform.Android),
                }
            },
            new XRDisplaySubSystemToDeviceType() {
                displaySubSystemIDs = new String[] { "skyworth display", "skyworth" },
                deviceTypeOnPlatform = new Tuple<DeviceTypes, RuntimePlatform>[] {
                    new Tuple<DeviceTypes, RuntimePlatform>(DeviceTypes.AndroidSkyworthVRGeneric, RuntimePlatform.Android),
                }
            },
            new XRDisplaySubSystemToDeviceType() {
                displaySubSystemIDs = new String[] { "WVR Display Provider", "wvr", "wavevr" },
                deviceTypeOnPlatform = new Tuple<DeviceTypes, RuntimePlatform>[] {
                    new Tuple<DeviceTypes, RuntimePlatform>(DeviceTypes.AndroidWaveVRGeneric, RuntimePlatform.Android),
                }
            }
        };
#endif

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
        /// <summary>
        /// Holds path to home folder
        /// </summary>
        internal static string HOME_FOLDER = (Environment.OSVersion.Platform == PlatformID.Unix ||
            Environment.OSVersion.Platform == PlatformID.MacOSX)
            ? Environment.GetEnvironmentVariable("HOME")
            : Environment.GetEnvironmentVariable("HOMEDRIVE") + Environment.GetEnvironmentVariable("HOMEPATH", EnvironmentVariableTarget.Process);
#endif

        private static bool GetIsHeadsetDevice() {
            return ((int)_deviceType >= 1000 && _deviceType != DeviceTypes.Tester);
        }

#if UNITY_ANDROID
        // This function will be used to detect the WaveVR SDK.
        // Note that the WaveVR SDK does not expose itself through UnityEngine.XR
        [DllImport("wvrunity", CallingConvention = CallingConvention.Cdecl)]
        public static extern System.IntPtr GetRenderEventFunc();
        // This function will be used to detect the PicoVR SDK
        [DllImport("Pvr_UnitySDK", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr Pvr_GetSDKVersion();   
#endif
#if UNITY_ANDROID || UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
        /* Dummy load a simple function from the OVRPlugin. This is used lateron to verify whether the OVRPlugin is at all available or not. */
        [DllImport("OVRPlugin", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool ovrp_GetInitialized();
#endif
        public static bool GetIsVrDevicePresent() {
            if(!_isDeviceTypeDetected) {
                DetectDeviceType();
            }
            return _isVrDevicePresent;
        }

        /// <summary>
        /// Returns the detected Device Type. 
        /// Auto-detection is only performed during the first time you call this API. If you want to re-detect the Device Type, or override the automatically detected Device Type, see RedetectDeviceType().
        /// </summary>
        /// <returns>The current DeviceType</returns>
        public static DeviceTypes GetDeviceType() {
            if(!_isDeviceTypeDetected) {
                DetectDeviceType();
            }
            return _deviceType;
        }

        public static Type GetType(string argTypeName){
            // Try Type.GetType() first. This will work with types defined
            // by the Mono runtime, in the same assembly as the caller, etc.
            var type = Type.GetType(argTypeName);
            // If it worked, then we're done here
            if(type != null) {
                return type;
            }
            // If the argTypeName is a full name, then we can try loading the defining assembly directly
            if(argTypeName.Contains(".")) {
                // Get the name of the assembly (Assumption is that we are using 
                // fully-qualified type names)
                var assemblyName = argTypeName.Substring(0, argTypeName.IndexOf('.'));
                // Attempt to load the indicated Assembly
                try { 
                    Assembly assembly = Assembly.Load(assemblyName); // can throw an Exception
                    if(assembly != null) {
                        // Ask that assembly to return the proper Type
                        try {
                            type = assembly.GetType(argTypeName); // can throw an Exception
                            if(type != null) {
                                return type;
                            }
                        } catch {
                            // fallthrough
                        }
                    } else {
                        // fallthrough
                    }
                } catch {
                    // fallthrough
                }        
            }
            System.Reflection.Assembly currentAssembly = null;
            System.Reflection.AssemblyName[] referencedAssemblies = null;
            // Unity's default Assembly is called Assembly.CSharp. Let's first check this assembly before we go more brute-force.
            try {
                currentAssembly = Assembly.Load("Assembly-CSharp");
            } catch {
                // Silently fall-through to next brute-force detector
            }
            if(currentAssembly != null) {
                referencedAssemblies = currentAssembly.GetReferencedAssemblies();
                PrependToArray(referencedAssemblies, currentAssembly.GetName());
                foreach(var assemblyName in referencedAssemblies) {
                    Assembly assembly = null;
                    // Load the referenced assembly
                    try {
                        assembly = Assembly.Load(assemblyName);
                    } catch {
                        continue; // Failed for whatever reason, let's try the next one
                    }
                    if(assembly != null) {
                        // See if that assembly defines the named type
                        try {
                            type = assembly.GetType(argTypeName);
                            if(type != null) {
                                return type;
                            }
                        } catch {
                            continue;
                        }
                    }
                }
            }

            // If we still haven't found the proper type, we can enumerate all of the 
            // loaded assemblies and see if any of them define the type
            currentAssembly = Assembly.GetExecutingAssembly();
            if(currentAssembly != null) {
                referencedAssemblies = currentAssembly.GetReferencedAssemblies();
                PrependToArray(referencedAssemblies, currentAssembly.GetName());
                foreach(var assemblyName in referencedAssemblies) {
                    Assembly assembly = null;
                    // Load the referenced assembly
                    try {
                        assembly = Assembly.Load(assemblyName);
                    } catch {
                        continue; // Failed for whatever reason, let's try the next one
                    }
                    if(assembly != null) {
                        // See if that assembly defines the named type
                        try {
                            type = assembly.GetType(argTypeName);
                            if(type != null) {
                                return type;
                            }
                        } catch {
                            continue;
                        }
                    }
                }
            }
            // Lastly, we iterate over _all_ assemblies (FIX #3854)
            System.Reflection.Assembly[] assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            if(assemblies != null) {
                foreach (System.Reflection.Assembly asm in assemblies) {
                    try {
                        type = asm.GetType(argTypeName, false, false);
                        if (type != null) { 
                            return type;
                        }
                    } catch {
                        continue;
                    }
                }
            }
            // The type just couldn't be found...
            return null;
        }
        
        /// <summary>
        /// Redetect the device type. Call this API if you switched to a different mode (e.g. from traditional flat to cardboard). Use Utils.GetDeviceType() to get the newly detected device type.
        /// Notes:
        /// 1. This is a SLOW and EXPENSIVE call. We highly recommend you to query once and cache your result afterwards.
        /// 2. This does NOT return the same value as UnityEngine.XR.XRSettings.loadedDeviceName or UnityEngine.XR.XRDevice.system. Some VR platforms, notably WaveVR, do not expose any usable values on these Unity specific APIs.
        /// 3. This could theoreticaly change on run-time, for example if you would switch from flat (None) to cardboard (Cardboard) mode.
        /// </summary>
        public static void RedetectDeviceType(DeviceTypes argOverrideDeviceType = DeviceTypes.Unknown) {
            if(argOverrideDeviceType == DeviceTypes.Unknown) {
                DetectDeviceType();
            } else {
                _deviceType = argOverrideDeviceType;
                _isVrDevicePresent = GetIsHeadsetDevice();
                _isDeviceTypeDetected = true;
            }
        }

        /// <summary>
        /// Prepend object to existing array.
        /// If the existing array is null, an array will be created and the new object will be added.
        /// If the new object is null, the original array will be returned.
        /// </summary>
        /// <param name="argArray">The array to prepend to. Can be null.</param>
        /// <param name="argObjectToPrepend">The object to prepend to the array. If null, nothing will be added.</param>
        /// <typeparam name="T">The object's Type.</typeparam>
        /// <returns>A deep copy of the original array, plus the specified object added to the head of the list (at position 0).</returns>
        private static T[] PrependToArray<T>(T[] argArray, T argObjectToPrepend) {
            if(argObjectToPrepend == null) {
                return argArray;
            }
            int newLength = argArray != null ? argArray.Length + 1 : 1;
            T[] newArray = new T[newLength];
            newArray[0] = argObjectToPrepend;
            if(argArray != null) {
                Array.Copy(argArray, 0, newArray, 1, argArray.Length);
            }
            return newArray;
        }
        
        internal static void DetectDeviceType() {
            _deviceType = DeviceTypes.Unknown;

#if !UNITY_2019_3_OR_NEWER
            String loadedDeviceName = "";
#if !UNITY_TVOS 
#if UNITY_2018_1_OR_NEWER
            loadedDeviceName = UnityEngine.XR.XRSettings.loadedDeviceName; // Deprecated in 2019_3_OR_NEWER
#else
            loadedDeviceName = UnityEngine.VR.VRSettings.loadedDeviceName;
#endif
#endif
            switch(loadedDeviceName.ToLower()) {
                case "daydream":
#if UNITY_ANDROID && !UNITY_EDITOR
                    _deviceType = DeviceTypes.AndroidGenericDaydream;
#endif // else is fallthrough to unknown device type
                    break;
                case "cardboard":
#if UNITY_ANDROID && !UNITY_EDITOR
                    _deviceType = DeviceTypes.AndroidGenericCardboard;
#elif UNITY_IOS && !UNITY_EDITOR
                    _deviceType = DeviceTypes.IOSGenericCardboard;
#endif // else is fallthrough to unknown device type
                    break;
                case "oculus":
                    // needs magic detection
                    break;
                // Note that we cannot check for empty string "" or "none" as that will also be set in case of Wave VR
                default:
                    break; // Let's try some other mechanisms.
            }
#endif

#if UNITY_ANDROID || UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
            // Check for Oculus Headsets, we will use introspection to figure out the exact headset using OVRPlugin.GetSystemHeadsetType().
            Type ovrPluginType = GetType("OVRPlugin");
            if(ovrPluginType != null && _deviceType == DeviceTypes.Unknown) { // Good, the OVRPlugin object is available
                bool isOVRPluginLibraryFound = false;
                try {
                    ovrp_GetInitialized();
                    isOVRPluginLibraryFound = true;
                } catch (DllNotFoundException) {
                    // OVR source code is there, but the OVR plugin cannot be loaded.
                }
                if(isOVRPluginLibraryFound) {
                    try {
                        MethodInfo getSystemHeadsetTypeMethodInfo = ovrPluginType.GetMethod("GetSystemHeadsetType", BindingFlags.Public | BindingFlags.Static);
                        if (getSystemHeadsetTypeMethodInfo != null) {
                            System.Object headsetTypeObject = getSystemHeadsetTypeMethodInfo.Invoke(null /* static */, null /* no args */);
                            if(headsetTypeObject != null) {
                                // Note that the System.Type of the HeadsetType enum is "OVRPlugin+SystemHeadset"
                                String headsetTypeString = headsetTypeObject.ToString();
                                // fuzzy match on "GearVR", as there are various GearVR models
                                if (headsetTypeString.IndexOf("GearVR") != -1) {
                                    _deviceType = DeviceTypes.AndroidOculusGearVR;
                                } else if (headsetTypeString.Equals("Oculus_Go")) {
                                    _deviceType = DeviceTypes.AndroidOculusGo;
                                } else if (headsetTypeString.Equals("Oculus_Quest")) {
                                    _deviceType = DeviceTypes.AndroidOculusQuest;
                                } else if (headsetTypeString.Equals("Oculus_Quest_2")) {
                                    _deviceType = DeviceTypes.AndroidOculusQuest2;
                                } else if (headsetTypeString.Equals("Rift_DK1")) {
                                    _deviceType = DeviceTypes.PCOculusRiftDK1;
                                } else if (headsetTypeString.Equals("Rift_DK2")) {
                                    _deviceType = DeviceTypes.PCOculusRiftDK2;
                                } else if (headsetTypeString.Equals("Rift_CV1")) {
                                    _deviceType = DeviceTypes.PCOculusRiftCV1;
                                } else if (headsetTypeString.Equals("Rift_S")) {
                                    _deviceType = DeviceTypes.PCOculusRiftS;
                                } else if (headsetTypeString.Equals("Oculus_Link_Quest")) {
                                    _deviceType = DeviceTypes.PCOculusLinkQuest;
                                } else if (headsetTypeString.Equals("None")) {
                                    // pass
                                } else {
    #if UNITY_STANDALONE || UNITY_EDITOR_WIN
                                    _deviceType = DeviceTypes.PCOculusGeneric;
    #elif UNITY_ANDROID
                                    _deviceType = DeviceTypes.AndroidOculusGeneric;
    #endif // else is fallthrough to unknown.
                                }
                            }
                        }
                    } catch (Exception e) { // explicit fallthrough as we do not care for the exception in this case
                        ClearVRLogger.LOGE("An error was thrown while detecting headset type. Details: {0}" , e);
                    }
                } else {
                    // no OVRPlugin.so present in application
                }
            }
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
            // Check for SteamVR compatible headsets on Windows
            if(_deviceType == DeviceTypes.Unknown) {
                // Get the SteamVR type, note that we mustuse the FQDN here
                Type steamVRObjectType = GetType("Valve.VR.SteamVR");
                if(steamVRObjectType != null) { // Good, the SteamVR object is available
                    // The SteamVR object is a singleton, we need to get a reference to this singleton's instance and there should be a hmd_ModelNumber property
                    PropertyInfo steamVRInstancePropertyInfo = steamVRObjectType.GetProperty("instance");
                    PropertyInfo hmd_ModelNumberPropertyInfo = steamVRObjectType.GetProperty("hmd_ModelNumber");
                    if(steamVRInstancePropertyInfo != null && hmd_ModelNumberPropertyInfo != null) {
                        // Grab a reference to the getter of the instance property of the SteamVR object
                        MethodInfo steamVRObjectInstanceGetterMethodInfo = steamVRInstancePropertyInfo.GetGetMethod();
                        // Grab a reference to the hmd_ModelNumber property found on the SteamVR object
                        MethodInfo hmd_ModelNumberGetterMethodInfo = hmd_ModelNumberPropertyInfo.GetGetMethod();
                        if(steamVRObjectInstanceGetterMethodInfo != null && hmd_ModelNumberGetterMethodInfo != null) {
                            // Grab a reference to the SteamVR object (singleton) instance.
                            System.Object steamVRObjectInstance = steamVRObjectInstanceGetterMethodInfo.Invoke(steamVRObjectType, null);
                            if(steamVRObjectInstance != null) {
                                System.Object hmd_ModelNumberObject = hmd_ModelNumberGetterMethodInfo.Invoke(steamVRObjectInstance, null);
                                if(hmd_ModelNumberObject != null) {
                                    String hmd_ModelNumberString = hmd_ModelNumberObject.ToString();
                                    switch (hmd_ModelNumberString.Trim()) {
                                        case "None": // Fallthrough to _deviceType is still Unknown.
                                            break;
                                        case "Vive MV": 
                                        case "Vive MV.":
                                        case "Vive. MV": {
                                            _deviceType = DeviceTypes.PCHTCVive;
                                            break;
                                        }
                                        default: // Assuming a generic HTC device for now.
                                            _deviceType = DeviceTypes.PCHTCGeneric;
                                            break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
#endif
#if UNITY_ANDROID && !UNITY_EDITOR
            // Check for WaveVR
            if(_deviceType == DeviceTypes.Unknown) {
                Type waveVRType = GetType("WaveVR");
                if(waveVRType != null) {
                    try {
                        GetRenderEventFunc();
                        _deviceType = DeviceTypes.AndroidWaveVRGeneric;
                    } catch {
                        // Assume that there is no WaveVR library active.
                    }
                }
            }
            // Check for PicoVR
            if(_deviceType == DeviceTypes.Unknown) {
                // The Pvr_UnitySDKAPI.Render namespace is not available in the PicoVR-XR plugin
                // We will fallthrough to the alternative check dow-below.
                Type picoVRType = GetType("Pvr_UnitySDKAPI.Render");
                if(picoVRType != null) {
                    try {
                        MethodInfo upvr_GetSupportHMDTypesMethodInfo = picoVRType.GetMethod("UPvr_GetSupportHMDTypes", BindingFlags.Public | BindingFlags.Static);
                        if(upvr_GetSupportHMDTypesMethodInfo != null) {
                            String supportedHMDType = upvr_GetSupportHMDTypesMethodInfo.Invoke(null /* static */, null /* no args */).ToString();
                            if(!String.IsNullOrEmpty(supportedHMDType)) {
                                MethodInfo upvr_GetHmdSerialNumberMethodInfo = picoVRType.GetMethod("UPvr_GetHmdSerialNumber", BindingFlags.Public | BindingFlags.Static);
                                if(upvr_GetHmdSerialNumberMethodInfo != null) {
                                    String hmdSerialNumber = upvr_GetHmdSerialNumberMethodInfo.Invoke(null /* static */, null /* no args */).ToString();
                                    if(!String.IsNullOrEmpty(supportedHMDType)) {
                                        _deviceType = DeviceTypes.AndroidPicoVRGeneric;
                                    }
                                }
                                if(_deviceType == DeviceTypes.Unknown) {
                                    // We could not get the serial number, but perhaps we can read the firmware version?
                                    MethodInfo upvr_GetHmdFirmwareVersionMethodInfo = picoVRType.GetMethod("UPvr_GetHmdFirmwareVersion", BindingFlags.Public | BindingFlags.Static);
                                    if(upvr_GetHmdFirmwareVersionMethodInfo != null) {
                                        String hmdFirmwareVersion = upvr_GetHmdFirmwareVersionMethodInfo.Invoke(null /* static */, null /* no args */).ToString();
                                        if(!String.IsNullOrEmpty(hmdFirmwareVersion)) {
                                            _deviceType = DeviceTypes.AndroidPicoVRGeneric;
                                        }
                                    }
                                }
                            }
                        }
                    } catch {
                        // Assume that there is no PicoVR library active.
                    }
                }
            }
            // Check for SkyworthVR
            if(_deviceType == DeviceTypes.Unknown) {
                Type _svrLoaderType = GetType("SvrLoader");
                if(_svrLoaderType == null) {
                    _svrLoaderType = GetType("TMSvrLoader"); // This is a fallback for shadowed SWVR SDKs
                }
                Type _svrTrackDevicesType = GetType("SvrTrackDevices"); // Attached to the "Player" Prefab only
                if(_svrTrackDevicesType == null) {
                    _svrTrackDevicesType = GetType("TMSvrTrackDevices"); // This is a fallback for shadowed SWVR SDKs
                }
                Type _svr_AtWAPIType = GetType("SVR.AtwAPI"); // This should always be != null IF SWVR SDK is loaded inside the project.
                Type _stereoControllerType = GetType("StereoController"); // Attached to the "Player" and "Player (simple)" prefab. Note that this is the same component as found in the GoogleVR SDK. This must always be available.
                if(_stereoControllerType != null && (_svrLoaderType != null || _svrTrackDevicesType != null || _svr_AtWAPIType != null)) {
                    // SWVR is part of this project, but we are not yet completely sure whether it is active in the scene.
                    UnityEngine.Object stereoControllerObject = GameObject.FindObjectOfType(_stereoControllerType);
                    if(stereoControllerObject != null) {
                        // There is a component in the scene with the StereoController attached to it. As GameObject.FindObjectOfType only checks active components, we know for sure that the thing is alive
                        _deviceType = DeviceTypes.AndroidSkyworthVRGeneric;
                    }
                }
            }
#endif // #if UNITY_ANDROID
#endif // #if UNITY_IOS
            if (_deviceType == DeviceTypes.Unknown) {
                Type mobfishCardboardCameraType = GetType("MobfishCardboard.CardboardMainCamera");
                Type mobfishCardboardManagerType = GetType("MobfishCardboard.CardboardManager");
                if (mobfishCardboardCameraType != null && mobfishCardboardManagerType != null) {
                    var cardboardCamera = GameObject.FindObjectOfType(mobfishCardboardCameraType);
                    var enabledVRGetMethod = mobfishCardboardManagerType.GetProperty("enableVRView", BindingFlags.Public | BindingFlags.Static).GetGetMethod();
                    if (cardboardCamera != null && enabledVRGetMethod != null) {
                        // Get the private variable "defaultEnableVRView" that's set in the editor ìnside the CardboardMainCamera script. 
                        // Not using the public variable MobfishCardboard.CardboardManager.enableVRView because this is false and will only be set to true after DetectDeviceType()
                        // is called. This is the only way to see if inside the editor we force cardboard or not. 
                        FieldInfo defaultEnableVRViewFieldInfo = mobfishCardboardCameraType.GetField("defaultEnableVRView", BindingFlags.NonPublic | BindingFlags.Instance);
                        if(defaultEnableVRViewFieldInfo != null) {
                            try {
                                bool defaultVRViewEnabled = (bool)defaultEnableVRViewFieldInfo.GetValue(cardboardCamera);
                                // Also check "enableVRView" inside the CardboardManager class in case you set the EnableVRView to true in the menu before initializing the player. 
                                bool isVREnabled = (bool)enabledVRGetMethod.Invoke(null, null);
                                if (defaultVRViewEnabled || isVREnabled) {
#if UNITY_ANDROID && !UNITY_EDITOR
                                    _deviceType = DeviceTypes.AndroidMobfishCardboard;
#elif UNITY_IOS && !UNITY_EDITOR
                                    _deviceType = DeviceTypes.IOSMobfishCardboard;
#endif
                                }
                            } catch {
                                // Silent fallthrough
                            }
                        }
                    }
                }
            }
#if !UNITY_TVOS
            if (_deviceType == DeviceTypes.Unknown) {
#if UNITY_2019_1_OR_NEWER
#if !UNITY_2019_3_OR_NEWER
            
                // Warning: XRSettings is deprecated in 2020_1_OR_NEWER.

                // We attempt to detect the Google Cardboard XR Plugin.
                // A feature request to expose a more-accurate display device name has been proposed and merged in v1.8.0 of the Google Cardboard SDK, ref. https://github.com/googlevr/cardboard/issues/282
                // The XRDisplaySubsystem does not contain any information that can help to detect the device type
                // var xrDisplaySubsystems = new List<UnityEngine.XR.XRDisplaySubsystem>();
                if(UnityEngine.XR.XRSettings.enabled) {
                    // UnityEngine.XR.XRSettings.enabled:
                    // Set to true to enable XR mode for the application.
                    if(UnityEngine.XR.XRSettings.isDeviceActive) {
                        // UnityEngine.XR.XRSettings.isDeviceActive
                        // Read-only value that can be used to determine if the XR device is active.
                        // When true, Unity accepts input from the device and attempts to render to the device's display(s). Note that this returns true even if the device is not currently rendering due to lack of user presence (see XRDevice.userPresence). 
                        // This can become false if a device is disconnected, could not be initialized (see XRSettings.LoadDeviceByName), or XRSettings.enabled is set to false.
                        String xrDisplayDeviceName = UnityEngine.XR.XRSettings.loadedDeviceName;
                        if(!String.IsNullOrEmpty(xrDisplayDeviceName)) {
                            // On Cardboard, the displayDeviceName is supposed to read "Display" pre v1.8.0. Since v1.8.0 this will now read "CardboardDisplay", ref. https://github.com/googlevr/cardboard-xr-plugin/pull/21#issuecomment-894349067
                            xrDisplayDeviceName = xrDisplayDeviceName.ToLower().Trim();
                            if((Array.IndexOf(new String[]{"display", "cardboard display", "cardboarddisplay"}, xrDisplayDeviceName) >= 0)) {
#if UNITY_ANDROID && !UNITY_EDITOR 
                                _deviceType = DeviceTypes.AndroidGenericCardboard;
#elif UNITY_IOS && !UNITY_EDITOR 
                                _deviceType = DeviceTypes.IOSGenericCardboard;
#endif
                            }
                        }
                    }
                }
#else // UNITY_2019_3_OR_NEWER
                // This is available from 2019_3_OR_NEWER.
                List<XRDisplaySubsystemDescriptor> displaysDescs = new List<XRDisplaySubsystemDescriptor>();
                List<XRDisplaySubsystem> displays = new List<XRDisplaySubsystem>();
                displaysDescs.Clear();
                displays.Clear();
                SubsystemManager.GetSubsystemDescriptors(displaysDescs);
                SubsystemManager.GetInstances(displays);
                // This code works under the assumption that there will be at most one display subsystem running.
                // Iterate over all display subsystems
                bool wasAtLeastOneRunningXRDisplaySubSystemFound = false;
                foreach (var displaySubsystem in displays) { 
                    // Check if the display subsystem is running
                    if (displaySubsystem.running) {
                        wasAtLeastOneRunningXRDisplaySubSystemFound= true;
                        // Iterate over all display subsystem descriptors
                        foreach(var displayDesc in displaysDescs) {
                            // Iterate over all supported XR display subsystems that we can auto-detect here.
                            foreach(var autoDetectXRDisplaySubsystem in AUTO_DETECT_XR_DISPLAY_SUBSYSTEMS) {
                                // If the display subsystem descriptor id matches the name
                                foreach(var displaySubSystemID in autoDetectXRDisplaySubsystem.displaySubSystemIDs) {
                                    if(displayDesc.id.ToLower().Contains(displaySubSystemID.ToLower())) {
                                        // Iterate over all runtime platforms this subsystem is supported on.
                                        foreach(var tuple in autoDetectXRDisplaySubsystem.deviceTypeOnPlatform) {
                                            // If the running platform matches the specififed platform.
                                            if(Application.platform == tuple.Item2) {
                                                _deviceType = tuple.Item1;
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                if(_deviceType == DeviceTypes.Unknown && wasAtLeastOneRunningXRDisplaySubSystemFound){ 
                    // there is at least one XRDisplay SubSystem active, but we did not recognize it.
                    // We default to a generic platform-specific HMD device type. That is good enough for now.
#if UNITY_ANDROID && !UNITY_EDITOR
                    _deviceType = DeviceTypes.AndroidGenericHMD;
#elif UNITY_IOS && !UNITY_EDITOR
                    _deviceType = DeviceTypes.IOSGenericHMD;
#elif UNITY_EDITOR || UNITY_STANDALONE
                    _deviceType = DeviceTypes.PCGenericHMD;
#endif
                }
#endif
#endif
            }

#endif // !UNITY_TVOS
            // We exhausted all means to detect a VR device type. Now we will default to flat devices.
            if (_deviceType == DeviceTypes.Unknown) {
#if UNITY_ANDROID && !UNITY_EDITOR
                _deviceType = DeviceTypes.AndroidFlat;
#elif UNITY_IOS && !UNITY_EDITOR
                _deviceType = DeviceTypes.IOSFlat;
#elif UNITY_TVOS && !UNITY_EDITOR
                _deviceType = DeviceTypes.AppleTV;
#elif UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
                _deviceType = DeviceTypes.PCFlat;
#endif
            }
            _isVrDevicePresent = GetIsHeadsetDevice();
            _isDeviceTypeDetected = true;
        }

        public static VRAPITypes GetVRAPIType() {
            if(!_isVRAPITypeDetected) {
                DetectVRAPIType();
            }
            return _vrAPIType;
        }
        private static void DetectVRAPIType() {
            _vrAPIType = VRAPITypes.Unknown; // Assume unknown.
            GetDeviceType(); // Force device type detection if it was not already done.
            if(_deviceType.GetIsOculusDevice()) {
                // Headset is detected as an Oculus compatible device.
                // Oculus headsets can run in OpenVR and native OculusVR mode (See OVRManager::XRDevice enum). We detect what mode we are currently in (and silenty assume it never changes throughout the lifecycle of the app)
                Type ovrManagerType = Utils.GetType("OVRManager");
                if(ovrManagerType != null) {
                    FieldInfo info = ovrManagerType.GetField("loadedXRDevice", BindingFlags.Public | BindingFlags.Static);
                    if(info != null) {
                        object xrDevice = info.GetValue(null); 
                        if(xrDevice != null) {
                            try {
                                switch(xrDevice.ToString().ToLower()) {
                                    case "oculus": {
                                        _vrAPIType = VRAPITypes.OculusVR;
                                        break;
                                    }
                                    case "openvr": {
                                        _vrAPIType = VRAPITypes.OpenVR;
                                        break;
                                    }
                                    default: {
                                        break; // Fallthrought to unknown.
                                    }
                                }
                            } catch (Exception) {
                                // Silent fallthrough
                            }
                        }
                    }
                } else {
                    // No OVRManager component found, the OVRManager is only included in the Oculus Utilties package and not in the Oculus XR plugin.
                    // We assume VRAPITypes.OculusVR;
                    _vrAPIType = VRAPITypes.OculusVR;
                } 
            }
            _isVRAPITypeDetected = true;
        }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN 
        /// <summary>
        /// Checks whether the ClearVR MediaFlow library is available.
        /// This check is needed because not all v6.x ClearVR UnitySDKs will ship with the required libraries while they do include the MediaPlayerPC wrapper scripts.
        /// Notes:
        /// 1. This check can be removed when v7.x is released.
        /// </summary>
        /// <returns>True if ClearVRPC.dll is found, false otherwise.</returns>
        internal static bool GetIsMediaFlowWindowsLibraryFound() {
            try {
                return (System.IO.Directory.GetFiles(Application.dataPath, "ClearVRPC.dll", System.IO.SearchOption.AllDirectories).Length > 0);
            } catch (Exception e) {
                // Silent fallthrough
            }
            return false;
        }
#endif
#if UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
        /// <summary>
        /// Checks whether the ClearVR MediaFlow library is available.
        /// This check is needed because not all v6.x ClearVR UnitySDKs will ship with the required libraries while they do include the MediaPlayerPC wrapper scripts.
        /// Notes:
        /// 1. This check can be removed when v7.x is released.
        /// </summary>
        /// <returns>True if libClearVRPC.so is found, false otherwise.</returns>
        internal static bool GetIsMediaFlowLinuxLibraryFound() {
            try {
                return (System.IO.Directory.GetFiles(Application.dataPath, "libClearVRPC.so", System.IO.SearchOption.AllDirectories).Length > 0);
            } catch (Exception) {
                // Silent fallthrough
            }
            return false;
        }
#endif

        /// <summary>
        /// Little helper method that can be used when handling an object that might be null. 
        /// </summary>
        /// <param name="argString">The string to check, might be null.</param>
        /// <param name="argAlternative">The alternative if the given string is null. Default value: "not specified"</param>
        /// <returns></returns>
        internal static String GetAsStringEvenIfNull(System.Object argObject, String argAlternative = "not specified") {
            if(argObject is String) {
                String asString = argObject as String;
                return String.IsNullOrEmpty(asString) ? argAlternative : asString;
            }
            return argObject == null ? argAlternative : argObject.ToString();
        }

        /// <summary>
        /// Helper method that determines whether the specified folder is writable or not.
        /// </summary>
        /// <param name="argDirPath">The path to check.</param>
        /// <param name="argThrowIfFails">Whether to throw the exception upon failure ot nor.</param>
        /// <returns>True if the path is writable, false otherwise.</returns>
        internal static bool IsDirectoryWritable(string argDirPath, bool argThrowIfFails = false) {
            try {
                String dummyFileFullPath = System.IO.Path.Combine(argDirPath, System.IO.Path.GetRandomFileName());
                using (System.IO.FileStream fs = System.IO.File.Create(dummyFileFullPath, 1))
                { }
                // Remove the dummy file.
                System.IO.File.Delete(dummyFileFullPath);
                return true;
            } catch {
                if (argThrowIfFails) {
                    throw;
                } else {
                    return false;
                }
            }
        }

        /// <summary>
        /// This method facilitates converting a projection type represented as a string into a ContentFormat enum as supported by the ContentItem.overrideContentFormat field. 
        /// This is especially useful when playing back non ClearVR 180/360/rectilinear content.
        /// </summary>
        /// <param name="argMediaProjectionType">The media projection type as a canonical string</param>
        /// <returns>The matching ContentFormat, or ContentFormat.Unknown if the media projection type is not supported.</returns>
        public static ContentFormat ConvertMediaProjectionTypeToContentFormat(String argMediaProjectionType) {
            switch(argMediaProjectionType) {
                // Deprecated, replaced by omnidirectional-mono
                case "360-cubemap-mono":
                    return ContentFormat.MonoscopicOmnidirectional;
                // Deprecated, replaced by omnidirectional-stereo
                case "360-cubemap-stereo":
                    return ContentFormat.StereoscopicOmnidirectional;
                // Deprecated, replaced by omnidirectional-mono
                case "180-cubemap-mono":
                    return ContentFormat.MonoscopicOmnidirectional;
                // Deprecated, replaced by omnidirectional-stereo
                case "180-cubemap-stereo":
                    return  ContentFormat.StereoscopicOmnidirectional;
                case "planar":
                    return ContentFormat.Planar;
                case "rectilinear":
                    return ContentFormat.MonoscopicRectilinear;
                case "rectilinear-stereo-tb":
                    return ContentFormat.StereoscopicRectilinearTB;
                case "rectilinear-stereo-sbs":
                    return ContentFormat.StereoscopicRectilinearSBS;
                case "360-erp-mono":
                    return ContentFormat.MonoscopicERP360;
                case "360-erp-stereo-tb":
                    return ContentFormat.StereoscopicERP360TB;
                case "180-erp-mono":
                    return ContentFormat.MonoscopicERP180;
                case "180-erp-stereo-sbs":
                    return ContentFormat.StereoscopicERP180SBS;
                case "fish-eye-mono":
                    return ContentFormat.MonoscopicFishEye;
                case "fish-eye-stereo-sbs":
                    return ContentFormat.StereoscopicFishEyeSBS;
                case "omnidirectional-mono":
                    return ContentFormat.MonoscopicOmnidirectional;
                case "omnidirectional-stereo":
                    return ContentFormat.StereoscopicOmnidirectional;
                default:
                    ClearVRLogger.LOGW("Unable to convert {0} into a valid content format. Assuming default value: Unknown.", argMediaProjectionType);
                    break;
            }            
            return ContentFormat.Unknown;
        }

        internal static ContentFormat ConvertInternalProjectionTypeToContentFormat(ProjectionTypes argFeedProjectionType) {
            switch(argFeedProjectionType) {
                case ProjectionTypes.MeshBoxMono:
                    return ContentFormat.MonoscopicOmnidirectional;
                case ProjectionTypes.MeshBoxStereo:
                    return ContentFormat.StereoscopicOmnidirectional;
                case ProjectionTypes.RectilinearMono:
                    return ContentFormat.MonoscopicRectilinear;
                case ProjectionTypes.RectilinearStereoTopBottom:
                    return ContentFormat.StereoscopicRectilinearTB;
                case ProjectionTypes.RectilinearStereoSideBySide:
                    return ContentFormat.StereoscopicRectilinearSBS;
                case ProjectionTypes.Erp360Mono:
                    return ContentFormat.MonoscopicERP360;
                case ProjectionTypes.Erp360StereoTopBottom:
                    return ContentFormat.StereoscopicERP360TB;
                case ProjectionTypes.Erp180Mono:
                    return ContentFormat.MonoscopicERP180;
                case ProjectionTypes.Erp180StereoSideBySide:
                    return ContentFormat.StereoscopicERP180SBS;
                case ProjectionTypes.FisheyeMono:
                    return ContentFormat.MonoscopicFishEye;
                case ProjectionTypes.FisheyeStereoSideBySide:
                    return ContentFormat.StereoscopicFishEyeSBS;
                default:
                    ClearVRLogger.LOGW("Unable to convert {0} into a valid content format. Assuming default value: Unknown.", argFeedProjectionType);
                    break;
            }            
            return ContentFormat.Unknown;
        }

        /// <summary>
        /// Helper method that masks a Sting by partially replacing it with "*" (handy when print passwords)
        /// </summary>
        /// <param name="argInput">The input to mask</param>
        /// <returns>The masked equivalent.</returns>
        internal static String MaskString(string argInput) {
            if(argInput == null || argInput.Length == 0){
                return "";
            }
            int len = argInput.Length;
            if(len <= 2){
                return "*****";
            }
            if(len <= 5){
                return argInput.Substring(0, 1) + "***" + argInput.Substring(len - 1, 1);
            }
            //take first 6 characters
            string firstPart = argInput.Substring(0, 6);

            //take last 4 characters
            string lastPart = argInput.Substring(len - 4, 4);

            //take the middle part (****)
            int middlePartLenght = len - (firstPart.Length + lastPart.Length);
            string middlePart = new String('*', middlePartLenght);

            return firstPart + middlePart + lastPart;
        }
        /// <summary>
        /// Get the length of provided byte array, or 0 if the argument is null.
        /// </summary>
        /// <param name="argByteArray">The byte array of which to get the length. Null is a valid value.</param>
        /// <returns>The length of the provided array, or 0 if the argument was null.</returns>
        internal static int GetLengthSafely(byte[] argByteArray) {
            if (argByteArray != null) {
                return argByteArray.Length;
            }
            return 0;
        }
        
        internal static void LongLog(String argLine) {           
#if UNITY_ANDROID && !UNITY_EDITOR
            int MAX_INDEX = 1000;
            int MIN_INDEX = 500;

            // String to be logged is longer than the max...
            if (argLine.Length > MAX_INDEX) {
                String theSubstring = argLine.Substring(0, MAX_INDEX);
                int    theIndex = MAX_INDEX;

                // Try to find a substring break at a line end.
                theIndex = theSubstring.LastIndexOf('\n');
                if (theIndex >= MIN_INDEX) {
                    theSubstring = theSubstring.Substring(0, theIndex);
                } else {
                    theIndex = MAX_INDEX;
                }

                // Log the substring.
                ClearVRLogger.LOGI(theSubstring);

                // Recursively log the remainder.
                LongLog(" \n" + argLine.Substring(theIndex));
            } else {// String to be logged is shorter than the max...
                ClearVRLogger.LOGI(argLine);
            }
#else
            ClearVRLogger.LOGI(argLine);
#endif
        }
 
        /// <summary>
        /// Returns the type of render pipeline that is currently active.
        /// </summary>
        /// <returns>The currently active render pipeline type.</returns>
        internal static RenderPipelineTypes GetRenderPipelineType() {
#if UNITY_2019_1_OR_NEWER
            if (GraphicsSettings.renderPipelineAsset != null) {
                // SRP
                Type srpType = GraphicsSettings.renderPipelineAsset.GetType();
                if(srpType != null) {
                    String srpName = srpType.ToString();
                    if (srpName.Contains("HDRenderPipelineAsset")) {
                        return RenderPipelineTypes.HighDefinitionPipeline;
                    } else if (srpName.Contains("UniversalRenderPipelineAsset") || srpName.Contains("LightweightRenderPipelineAsset")) {
                        return RenderPipelineTypes.UniversalPipeline;
                    } else {
                        return RenderPipelineTypes.Unknown;
                    }
                }
            }
#elif UNITY_2017_1_OR_NEWER
            if (GraphicsSettings.renderPipelineAsset != null) {
                // SRP not supported before 2019
                return RenderPipelineTypes.Unknown;
            }
#endif
            // no SRP
            return RenderPipelineTypes.BuiltInPipeline;
        }

        public static int GetListenerCount(this UnityEngine.Events.UnityEventBase unityEvent) {
            var field = typeof(UnityEngine.Events.UnityEventBase).GetField("m_Calls", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly );
            var invokeCallList = field.GetValue(unityEvent);
            var property = invokeCallList.GetType().GetProperty("Count");
            return (int)property.GetValue(invokeCallList);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void PrintCurrentStackTrace() {
            ClearVRLogger.LOGI(new System.Diagnostics.StackTrace().ToString());
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        public class AndroidVersion {
            static AndroidJavaClass versionInfo;

            static AndroidVersion() {
                versionInfo = new AndroidJavaClass("android.os.Build$VERSION");
            }
            
            public static int SDK_INT {
                get {
                    return versionInfo.GetStatic<int>("SDK_INT");
                }
            }
            
            /// <summary>
            /// On API 29 you can still leverage android:requestLegacyExternalStorage="true" to grant full access, but this is no longer possible on API 30.
            /// </summary>
            /// <returns>True for API 28 or older, false for API 29+</returns>
            public static bool CanHaveFreeAccessToSDCard() {
                return SDK_INT < 29;
            }
        }
#endif
    }
}
