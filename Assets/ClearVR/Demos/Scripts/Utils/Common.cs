using System;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using com.tiledmedia.clearvr.protobuf;
#if UNITY_2018_1_OR_NEWER
using UnityEngine.XR;
#endif
namespace com.tiledmedia.clearvr.demos {
    public static class Helpers {
        private static String CUSTOM_CONTENT_LIST_FILE_FULL_PATH = Application.persistentDataPath + "/customcontentlist.txt";

        /// <summary>
        /// It is the resposibility of the customers to securely store their private license file.
        /// This code is a mere example on how to read the license file and should not be considered "secure" in any way.
        /// </summary>
        /// <returns>The license file as a byte array or null if no license file data could be found.</returns>
        public static byte[] ReadLicenseFile(string argOverrideFolderAndOrFilename = "") {
            byte[] licenseFileBytes = null;
            // First, we try to read the license file from the Resources folder.
            licenseFileBytes = ReadLicenseFileFromResourceFolder(argOverrideFolderAndOrFilename);
            if (licenseFileBytes == null) {
                // Attempt to read a license file that might have been burned in as source code 
                licenseFileBytes = ReadBurnedInLicenseFile();
            }
#if UNITY_ANDROID && !UNITY_EDITOR
            if(licenseFileBytes == null) {
                // For backwards compatibility, we also read from the main storage root folder on the Android platform.
				// This is deprecated and will be removed after 2020/07/31.
                licenseFileBytes = ReadLicenseFileFromPhoneStorage("/storage/emulated/0/");
            }
#endif
            return licenseFileBytes;
        }

        /// <summary>
        /// Attempts to find the "LicenseFile" class and read the license file from it if available.
		/// This method uses introspection to find the class, and will return null if this class cannot be found.
        /// </summary>
		/// <returns>The license file data as a byte array if available, null if the LicenseFile class could not be found.</returns>
		private static byte[] ReadBurnedInLicenseFile() {
            Type licenseFileType = Utils.GetType("LicenseFile");
            if (licenseFileType != null) {
                FieldInfo info = licenseFileType.GetField("licenseFileBytes", BindingFlags.Public | BindingFlags.Static);
                if (info != null) {
                    Array licenseFileData = (Array)info.GetValue(null);
                    return (byte[])licenseFileData;
                }
            }
            return null;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        /// <summary>
        /// Searches for a .tml (Tiledmedia License file) in the root of the phone storage.
		/// Subsequently, it will read its contents in a byte array which is used by ClearVRForUnity. 
		/// If you are building your own player, you would typically include this license file as a Resource
        /// in the Assets folder or burn it into the source code on buildtime.
        /// </summary>
        /// <param name="argFolderFullPath">Full path to the folder that must be checked.</param>
        /// <returns>The contents of the first found .tml file, or null if no file was found.</returns>
		private static byte[] ReadLicenseFileFromPhoneStorage(string argFolderFullPath) {
            String licenseFileFullPath = "";
            string[] files;
            try {
                files = System.IO.Directory.GetFiles(argFolderFullPath, "*.tml");
            } catch (Exception e) {
                throw new Exception(String.Format("[ClearVR] An error was reported while looking for license (.tml) file in {0}. Error: {1}", argFolderFullPath, e));
            }
            if(files.Length > 0){
                licenseFileFullPath = files[0];	
            } else {
                return null;
            }
			return ReadFileFromLocalStorage(licenseFileFullPath);
        }
#endif

        /// <summary>
        /// Read the specific file from local storage as binary data.
        /// </summary>
        /// <param name="argFileFullPath">The full path to the file to read.</param>
        /// <returns>The contents of the specified file as a byte[]</returns>
        public static byte[] ReadFileFromLocalStorage(String argFileFullPath, bool argDoNoThrowExceptionButLogWarning = false) {
            try {
                return System.IO.File.ReadAllBytes(argFileFullPath);
            }
            catch (Exception e) {
                String message = String.Format("Unable to read file: {0}", argFileFullPath);
#if UNITY_ANDROID && !UNITY_EDITOR
                message = String.Format("[ClearVR] Cannot read file from {0}. Error: {1}. Please allow READ permission via Settings -> Apps -> AppName -> Permissions.", argFileFullPath, e);
#else
                message = String.Format("[ClearVR] Cannot read license file from {0}. Error: {1}.", argFileFullPath, e);
#endif
                if (argDoNoThrowExceptionButLogWarning) {
                    UnityEngine.Debug.LogWarning(message);
                }
                else {
                    throw new Exception(message);
                }
            }
            return null;
        }

        private static byte[] ReadLicenseFileFromResourceFolder(String argOverrideFilename = "") {
            argOverrideFilename = argOverrideFilename.Replace(".tml", "");
            try {
                return (Resources.Load(String.IsNullOrEmpty(argOverrideFilename) ? "license" : argOverrideFilename) as TextAsset).bytes;
            }
            catch {
                // Empty catch, fail silently.
            }
            return null;
        }

        // Enum for player interactions with the media player
        public enum PlayerInteractionRequests {
            None,
            Pause,
            Play,
            Stop,
            SeekForward,
            SeekBackward,
            NextClip,
            PreviousClip,
            NextAudioTrack,
            PreviousAudioTrack
        }

        public static string GetTimeInMillisecondsAsPrettyString(long argTimeInMilliseconds) {
            TimeSpan t = TimeSpan.FromMilliseconds(argTimeInMilliseconds);
            if (t.Hours >= 1) {
                return String.Format("{0:D2}:{1:D2}:{2:D2}.{3:D3}",
                                        t.Hours,
                                        t.Minutes,
                                        t.Seconds,
                                        t.Milliseconds);
            }
            return String.Format("{0:D2}:{1:D2}.{2:D3}",
                                    t.Minutes,
                                    t.Seconds,
                                    t.Milliseconds);
        }

        /// <summary>
        /// Convenient function to get a prettier number to display on the UI timer.
        /// </summary>
        /// <param name="argTimeInMilliseconds"></param>
        /// <returns></returns>
        public static string GetTimeAsPrettyString(long argTimeInMilliseconds) {
            TimeSpan t = TimeSpan.FromMilliseconds(argTimeInMilliseconds);
            if (t.Hours >= 1) {
                return String.Format("{0:D2}:{1:D2}:{2:D2}",
                    t.Hours,
                    t.Minutes,
                    t.Seconds);
            }
            return String.Format("{0:D2}:{1:D2}",
                t.Minutes,
                t.Seconds);
        }

        /// <summary>
        /// This method loads a content list json file located in the Assets/Resources/ folder.
        /// </summary>
        /// <param name="argContentListFileNameOrUrl"></param>
        /// <param name="argContext">An object that inherits from MonoBehaviour, e.g. your calling class. It is used to offload some work to a Coroutine.</param>
        /// <param name="argCbCompleted">This callback is triggered upon completion.</param>
        /// <returns>true in case of success, false if something went wrong.</returns>
        [Obsolete("This API has been forcefully deprecated. Please use one of the other LoadContentList methods on the Helpers class instead.", true)]
        public static void LoadContentList(String argContentListFileNameOrUrl, MonoBehaviour argContext, Action<ClearVRMessage, ContentItemListFromJSON> argCbCompleted) {
            throw new Exception("This API has been forcefully deprecated. Please use one of the other LoadContentList methods on the Helpers class instead.");
        }


        /// <summary>
        /// Convenient method to load content list without having to think if it's an url or local list. Returns array of ContentItem objects on success.
        /// </summary>
        /// <param name="fileNameOrUrl">The url or file name to load a content list from</param>
        /// <param name="onSuccess">Triggered on successfully loading the content items.</param>
        /// <param name="onFailure">Triggered on failure, contains a message describing the failure.</param>
        public static void LoadContent(String fileNameOrUrl, Action<ContentItem[]> onSuccess, Action<ClearVRMessage> onFailure) {
            if (fileNameOrUrl.ToLower().Contains("http://") || fileNameOrUrl.ToLower().Contains("https://")) {
                LoadContentList(fileNameOrUrl, onSuccess, onFailure);
                return;
            } 
            LoadLocalContentList(fileNameOrUrl, onSuccess, onFailure);
        }


        /// <summary>
        /// Convenience method that loads the specified file from persistent storage or resources and parses it as an array of ContentItem.
        /// </summary>
        /// <param name="fileName">The file name of the content list that you'd like to be loaded.</param>
        /// <param name="onSuccess">An optional callback that should be triggered after the request was succesfully completed. You are highly encouraged to implement the callback, but it can be null.</param>
        /// <param name="onFailure">An optional callback that should be triggered in case the request has failed. You are highly encouraged to implement the callback, but it can be null.</param>
        public static void LoadLocalContentList(String fileName, Action<ContentItem[]> onSuccess, Action<ClearVRMessage> onFailure) {
            // In case the user adds a txt file in persistent data path named 'customcontentlist.txt' with a valid url, we prioritize this in loading the content list.
#if UNITY_IOS
            System.IO.FileInfo fileInfo = new System.IO.FileInfo(CUSTOM_CONTENT_LIST_FILE_FULL_PATH);
            bool fileExists = fileInfo.Exists;
#else
            bool fileExists = System.IO.File.Exists(CUSTOM_CONTENT_LIST_FILE_FULL_PATH);
#endif
            if (fileExists) {
                string customContentListText = "";
                try {
                    customContentListText = System.IO.File.ReadAllText(CUSTOM_CONTENT_LIST_FILE_FULL_PATH);
                }
                catch (Exception argException) {
                    UnityEngine.Debug.LogWarning(String.Format("[ClearVR] An error was thrown while reading the custom content list specified in {0}. Error: {1}. Will use default content list", CUSTOM_CONTENT_LIST_FILE_FULL_PATH, argException));
                }

                if (customContentListText.ToLower().Contains("http://") || customContentListText.ToLower().Contains("https://")) {
                    LoadContentList(customContentListText, onSuccess, onFailure);
                    return;
                } else {
                    UnityEngine.Debug.LogWarning("[ClearVR] Custom content list was found in persistent data path but it did not contain a url. Please clean up your project. defaulting to loading a content list from resources..");
                }
            }


            // If no customcontentlist.txt is found in persistent data path we check for either a content list (JSON file) in the persistent data path or in the resource folder.
            fileName = fileName.Replace(".json", ""); // we have to strip the file extension
            TextAsset contentListTextAsset = null;

            String filePathInPersistentDataPath = fileName.Contains("/") ? fileName : Application.persistentDataPath + "/" + fileName;
#if UNITY_IOS
            fileInfo = new System.IO.FileInfo(filePathInPersistentDataPath);
            bool persistentFileExists = fileInfo.Exists;
#else
            bool persistentFileExists = System.IO.File.Exists(filePathInPersistentDataPath);
#endif
            if (persistentFileExists) {
                // persistent data path contains a JSON content list. let's load it instead of the standard JSON content list in the resources folder.
                fileName = filePathInPersistentDataPath;
            }

            try {
                contentListTextAsset = Resources.Load<TextAsset>(fileName);
                if (contentListTextAsset == null) {
                    throw new Exception(String.Format("Unable to load {0} as TextAsset. Does not exist or incorrect file extension?", fileName));
                }
            } catch (Exception argException) {
                UnityEngine.Debug.LogError(String.Format("An error was thrown while reading content list: {0}. Error: {1}", fileName, argException));
                return;
            }
            byte[] bytes = System.Text.Encoding.ASCII.GetBytes(contentListTextAsset.text);
            cvrinterface.ContentListRequest contentListRequest = new cvrinterface.ContentListRequest();
            ProxyParameters proxyParametersHttp = ClearVRPlayer.GetProxyParameters(new ProxyParameters(ProxyTypes.Http));
            if(proxyParametersHttp != null) {
                contentListRequest.HttpProxyParamsMediaFlow = proxyParametersHttp.ToCoreProtobuf();
            }
            ProxyParameters proxyParametersHttps = ClearVRPlayer.GetProxyParameters(new ProxyParameters(ProxyTypes.Https));
            if(proxyParametersHttps != null) {
                contentListRequest.HttpsProxyParamsMediaFlow = proxyParametersHttps.ToCoreProtobuf();
            }
            contentListRequest.JSONBytes = ByteString.AttachBytes(bytes);
            _LoadContentList(contentListRequest, onSuccess, onFailure);
        }

         /// <summary>
        /// Convenience method that loads the specified file and parses it as an array of ContentItem
        /// </summary>
        /// <param name="url">The url pointing to the content list json file.</param>
        /// <param name="onSuccess">An optional callback that should be triggered after the request was succesfully completed. You are highly encouraged to implement the callback, but it can be null.</param>
        /// <param name="onFailure">An optional callback that should be triggered in case the request has failed. You are highly encouraged to implement the callback, but it can be null.</param>
        public static void LoadContentList(String url, Action<ContentItem[]> onSuccess, Action<ClearVRMessage> onFailure) {
            cvrinterface.ContentListRequest contentListRequest = new cvrinterface.ContentListRequest();
            ProxyParameters proxyParametersHttp = ClearVRPlayer.GetProxyParameters(new ProxyParameters(ProxyTypes.Http));
            if(proxyParametersHttp != null) {
                contentListRequest.HttpProxyParamsMediaFlow = proxyParametersHttp.ToCoreProtobuf();
            }
            ProxyParameters proxyParametersHttps = ClearVRPlayer.GetProxyParameters(new ProxyParameters(ProxyTypes.Https));
            if(proxyParametersHttps != null) {
                contentListRequest.HttpsProxyParamsMediaFlow = proxyParametersHttps.ToCoreProtobuf();
            }
            contentListRequest.URLs.Add(url);
            _LoadContentList(contentListRequest, onSuccess, onFailure);
        }

        private static void _LoadContentList(cvrinterface.ContentListRequest contentListRequest, Action<ContentItem[]> onSuccess, Action<ClearVRMessage> onFailure) {
            cvrinterface.CallCoreRequest callCoreRequest = new cvrinterface.CallCoreRequest {
                CallCoreRequestType = cvrinterface.CallCoreRequestType.ContentList,
                ContentListRequest = contentListRequest
            };
            string msg = Convert.ToBase64String(callCoreRequest.ToByteArray());
            ClearVRPlayer.CallCoreStatic(
                msg, 
                onSuccess: (base64Message, optionalArguments) => { 
                    cvrinterface.CallCoreResponse response = cvrinterface.CallCoreResponse.Parser.ParseFrom(System.Convert.FromBase64String(base64Message));

                    if (!string.IsNullOrEmpty(response.ErrorMessage)) {
                        onFailure(
                            new ClearVRMessage(
                                ClearVRMessageTypes.FatalError, 
                                ClearVRMessageCodes.GenericFatalError, 
                                String.Format("[ClearVR] Load content returned with an error. message: {0}", response.ErrorMessage), 
                                ClearVRResult.Failure
                            )
                        );
                    }

                    cvrinterface.ContentListMessage contentListMessage = response.ContentListMessage;

                    if (response == null || contentListMessage == null) {
                        onFailure(
                            new ClearVRMessage(
                                ClearVRMessageTypes.FatalError, 
                                ClearVRMessageCodes.GenericFatalError, 
                                "[ClearVR] Load content returned without a ContentListMessage. Cannot Proceed",
                                ClearVRResult.Failure
                            )
                        );
                    }

                    ContentItem[] contentList = new ContentItem[contentListMessage.AppContentItems.Count];
                    for (var i = 0; i < contentListMessage.AppContentItems.Count; i++) {
                        if(!String.IsNullOrEmpty(contentListMessage.AppContentItems[i].CertPath)) {
                            byte[] certKey = TryReadFileFromKnownLocations(contentListMessage.AppContentItems[i].CertPath);
                            if(certKey != null) {
                                contentListMessage.AppContentItems[i].SDKContentItem.DRM.CertificatePEMAsBase64 = Convert.ToBase64String(certKey);
                            }
                        }
                        if(!String.IsNullOrEmpty(contentListMessage.AppContentItems[i].KeyPath)) {
                            byte[] key = TryReadFileFromKnownLocations(contentListMessage.AppContentItems[i].KeyPath);
                            if(key != null) {
                                contentListMessage.AppContentItems[i].SDKContentItem.DRM.KeyPEMAsBase64 = Convert.ToBase64String(key);
                            }
                        }
                        if(!String.IsNullOrEmpty(contentListMessage.AppContentItems[i].CAChainPath)) {
                            byte[] caChain = TryReadFileFromKnownLocations(contentListMessage.AppContentItems[i].CAChainPath);
                            if(caChain != null) {
                                contentListMessage.AppContentItems[i].SDKContentItem.DRM.CAChainPEMAsBase64 = Convert.ToBase64String(caChain);
                            }
                        }
                        contentList[i] = ContentItem.FromCoreProtobuf(contentListMessage.AppContentItems[i].SDKContentItem);
                    }
                    onSuccess(contentList);
                 },
                onFailure: (clearVRMessage, optionalArguments) => { onFailure(clearVRMessage); }, 
                null
            );
        }
        /// <summary>
        /// Reads the provided file from the "known" locations.
        /// </summary>
        /// <param name="argFileOrFileFullPath">The file, or file with full path to read.</param>
        /// <returns>The contents of the file if the file could be read from any of the well known paths. Null if nould not be read.</returns>
        private static byte[] TryReadFileFromKnownLocations(String argFileOrFileFullPath) {
            if(String.IsNullOrEmpty(argFileOrFileFullPath)) {
                return null;
            }
            byte[] contents = null;
            List<String> knownFileLocations = new List<String>();
            knownFileLocations.Add("");
            knownFileLocations.Add(Application.dataPath);
            knownFileLocations.Add(Application.persistentDataPath);
#if UNITY_ANDROID && !UNITY_EDITOR
            knownFileLocations.Add("/storage/emulated/0/"); // Only working on API <30
#endif
            foreach(String fileLocation in knownFileLocations) {
                // "If path1 does not end with a valid separator character DirectorySeparatorChar is appended to path1 prior to the concatenation."
                String fileFullPath = fileLocation.Length > 0 ? Path.Combine(fileLocation, argFileOrFileFullPath) : argFileOrFileFullPath;
                try {
                    contents = Helpers.ReadFileFromLocalStorage(fileFullPath, true);
                } catch {
                    // Intentionally left empty.
                }
                if(contents != null) {
                    break;
                }
            }
            return contents;
        }

        /// <summary>
        /// Log a standard message when a warning or fatal error was returned. 
        /// Notes:
        /// 1. Do not use this to parse a success message.
        /// </summary>
        /// <param name="argClearVREvent">The event that needs to be parsed.</param>  /// 
        /// <param name="argOptionalString">String describing what was going on when the specified event was triggered.</param>
        /// <return>true if a FatalError was parsed, false otherwise.</return>
        public static bool ParseClearVRMessage(ClearVREvent argClearVREvent, ClearVRPlayer argClearVRPlayer, String argOptionalString, Action<ClearVREvent, ClearVRPlayer> onSuccess = null, Action<ClearVREvent, ClearVRPlayer> onFailure = null, Action<ClearVREvent, ClearVRPlayer> onWarning = null) {
           if (argClearVREvent.message.GetIsSuccess()) {
                UnityEngine.Debug.LogFormat("[ClearVR] {0} event fired successfully: success. Optional message: {1}", argClearVREvent.type, argClearVREvent.optionalArguments.GetValue(0));
                if(onSuccess != null){
                    onSuccess.Invoke(argClearVREvent, argClearVRPlayer);
                }
            } else if (argClearVREvent.message.GetIsWarning()) {
                UnityEngine.Debug.LogWarning(String.Format("[ClearVR] Warning reported while {0}. {1}", argOptionalString, argClearVREvent.message.GetFullMessage()));
                if(onWarning != null){
                    onWarning.Invoke(argClearVREvent, argClearVRPlayer);
                }
            } else if (argClearVREvent.message.GetIsFatalError()) {
                UnityEngine.Debug.LogError(String.Format("[ClearVR] Fatal error reported while {0}. {1}", argOptionalString, argClearVREvent.message.GetFullMessage()));
                 if(onFailure != null){
                    onFailure.Invoke(argClearVREvent, argClearVRPlayer);
                }
                return true;
            }
            return false;
        }

        //TODO: REMOVE THIS AS THEY ARE THE EXACT SAME THING AS THE THING ABOVE
        public static void ParseClearVRCoreWrapperStereoscopicModeChangedEvent(ClearVREvent argClearVREvent, ClearVRPlayer argClearVRPlayer) {
            if (argClearVREvent.message.GetIsSuccess()) {
                UnityEngine.Debug.Log(String.Format("[ClearVR] Stereo mode changed to: {0}.", argClearVREvent.message.GetFullMessage()));
            } else {
                Helpers.ParseClearVRMessage(argClearVREvent, argClearVRPlayer, "changing stereo mode", 
                    onFailure: (clearVREvent, clearVRPlayer) => UnityEngine.Debug.Log("An error was reported while Stereoscopic Mode was changed."));
            }
        }

        public static void ParseMediaInfoParsedEvent(ClearVREvent argClearVREvent, ClearVRPlayer argClearVRPlayer) {
            /* Make sure that parsing media info was successful */
            if (argClearVREvent.message.GetIsSuccess()) {
                if (argClearVRPlayer.platformOptions.prepareContentParameters == null) {
                    /* No content item specified to prepare automatically. It is suggested to do this here. */
                    //argClearVRPlayer.mediaPlayer.PrepareContentForPlayout(new PrepareContentParameters(new PrepareContentParameters(ContentItem, ...)));
                }
            }
            else {
                throw new Exception("[ClearVR] An error was reported while parsing media info. Cannot continue");
            }
        }

        /// <summary>
        /// Close/push the the application to the background.
        /// 
        /// Notes:
        /// 1. One should not call Application.Quit() on iOS. It is against the rationale of the platform (refer to https://docs.unity3d.com/ScriptReference/Application.Quit.html and https://developer.apple.com/library/archive/qa/qa1561/_index.html#//apple_ref/doc/uid/DTS40007952). If you want to quit the app on exit, specify the "Behaviour in Background" option under Unity's Project Settings.
        /// 2. On Android, calling Application.Quit() can result in instabilities. One should generally consider putting the application to the background instead.
        /// </summary>
        public static void CloseApplication() {
#if UNITY_ANDROID
            // We push the app to the background instead of calling Application.Quit()
            if (Application.platform == RuntimePlatform.Android) {
                AndroidJavaObject activity = new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity");
                activity.Call<bool>("moveTaskToBack", true);
                return;
            }
#endif
            Application.Quit();
        }

        public static void SimpleTryRecenter() {
#if UNITY_2019_3_OR_NEWER
			List<XRInputSubsystem> subsystems = new List<XRInputSubsystem>();
			SubsystemManager.GetInstances<XRInputSubsystem>(subsystems);
			for (int i = 0; i < subsystems.Count; i++) {
				subsystems[i].TrySetTrackingOriginMode(TrackingOriginModeFlags.Device);
				subsystems[i].TryRecenter();
			}
#elif UNITY_2018_1_OR_NEWER
            UnityEngine.XR.InputTracking.Recenter();
#else
			UnityEngine.VR.InputTracking.Recenter();
#endif
        }
    }
}