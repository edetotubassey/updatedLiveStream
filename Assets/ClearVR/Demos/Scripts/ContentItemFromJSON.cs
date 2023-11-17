using UnityEngine;
using System;
namespace com.tiledmedia.clearvr.demos {
    /// <summary>
    /// This object is used to load an initial clip and to switch to a new clip.
    /// </summary>
    [Serializable]
    [Obsolete("This class has been forcefully deprecated. Please use ContentItem instead.", true)]
    public class ContentItemFromJSON {
        [SerializeField]
        private String manifestUrl;
        [SerializeField]
        private String url;
        [SerializeField]
        private String description;
        [SerializeField]
        private String overrideMediaProjectionType;
        [SerializeField]
        private DRMInfoFromJSON drm_info;
        [SerializeField]
        private FishEyeSettingsFromJSON fish_eye_settings;
        [SerializeField]
        private ClearVRViewportAndObjectPose viewportAndObjectPose;
        /// <summary>
        /// Convenience method that converts a ContentItem as read from a json into a ClearVR ContentItem that can be used when interfacing with the SDK.
        /// </summary>
        /// <returns></returns>
        public com.tiledmedia.clearvr.ContentItem ConvertToContentItem() {
            ContentFormat contentFormat = ContentFormat.Unknown;
            ClearVRViewportAndObjectPose clearVRViewportAndObjectPose = new ClearVRViewportAndObjectPose();
            if (!String.IsNullOrEmpty(GetOverrideMediaProjectionType())) {
                contentFormat = Utils.ConvertMediaProjectionTypeToContentFormat(GetOverrideMediaProjectionType());
            }
            if (viewportAndObjectPose != null && viewportAndObjectPose.viewportPose != null && viewportAndObjectPose.displayObject != null && viewportAndObjectPose.displayObject.pose != null) {
                if (viewportAndObjectPose.viewportPose.w != 0 && viewportAndObjectPose.displayObject.pose.w != 0) {
                    clearVRViewportAndObjectPose = viewportAndObjectPose;
                }
            }
            DRMInfo drmInfo = drm_info != null ? drm_info.GetAsDRMInfo() : null; 
            FishEyeSettings fishEyeSettings = fish_eye_settings != null ? fish_eye_settings.GetAsFishEyeSettings() : null;
            return new com.tiledmedia.clearvr.ContentItem(GetURL(), clearVRViewportAndObjectPose, contentFormat, drmInfo, fishEyeSettings);
        }

        public override String ToString() {
            return String.Format("Url: {0}, description: {1}, override content format: {2}, pose: {3} drm info: {4}", Utils.GetAsStringEvenIfNull(GetURL()), Utils.GetAsStringEvenIfNull(GetDescription()), Utils.GetAsStringEvenIfNull(GetOverrideMediaProjectionType(), "not set"), viewportAndObjectPose != null ? viewportAndObjectPose.ToString() : "not set", Utils.GetAsStringEvenIfNull(String.IsNullOrEmpty(drm_info.license_server_type) ? null : drm_info));
        }

        /// <summary>
        /// Simple method to get the content url depending on which one is supplied in the content list
        /// </summary>
        /// <returns>Returns the content URL in order of which one is not null. </returns>
        public String GetURL() { return !String.IsNullOrEmpty(url) ? url : !String.IsNullOrEmpty(manifestUrl) ? manifestUrl : ""; }
        /// <summary>
        /// Simple method to get the content description.
        /// </summary>
        /// <returns>description</returns>
        public String GetDescription() { return !String.IsNullOrEmpty(description) ? description : ""; }
        /// <summary>
        /// Simple method to get the override media projection type
        /// </summary>
        /// <returns>overrideMediaProjectionType</returns>
        public String GetOverrideMediaProjectionType() { return !String.IsNullOrEmpty(overrideMediaProjectionType) ? overrideMediaProjectionType : ""; }
        /// <summary>
        /// Simple method to get the DRM info
        /// </summary>
        /// <returns>DRMInfoFromJSON</returns>
        public DRMInfoFromJSON GetDRMInfo() { return drm_info ?? null; }
        /// <summary>
        /// Simple method to get the Fish eye settings
        /// </summary>
        /// <returns>FishEyeSettingsFromJSON</returns>
        public FishEyeSettingsFromJSON GetFishEyeSettings() { return fish_eye_settings ?? null; }
        /// <summary>
        /// Simple method to get the Clear VR Viewport and Object pFose
        /// </summary>
        /// <returns>ClearVRViewportAndObjectPose</returns>
        public ClearVRViewportAndObjectPose GetViewportAndObjectPose() { return viewportAndObjectPose ?? null; }

    }
    [Serializable]
    [Obsolete("This class has been forcefully deprecated. Please use DRMInfo instead.", true)]
    public class DRMInfoFromJSON {
        public String url;
        public String license_server_type;
        public String certificate;
        public String key;
        public String ca_chain;
        public String token;
        public String password;
        
        /// <summary>
        /// Converts the object into the [DRMInfo](xref:com.tiledmedia.clearvr.DRMInfo) equivalent. Returns null if an empty string if `license_server_type` is an empty string (as that value must be set).
        /// </summary>
        /// <returns></returns>
        internal DRMInfo GetAsDRMInfo() {
            if(String.IsNullOrEmpty(license_server_type)) {
                return null;
            }
            byte[] certificateBytes = null;
            byte[] keyBytes = null;
            byte[] caChainBytes = null;
            if (!String.IsNullOrEmpty(certificate)) {
                certificateBytes = Helpers.ReadFileFromLocalStorage(certificate, true);
            }
            if (!String.IsNullOrEmpty(ca_chain)) {
                caChainBytes = Helpers.ReadFileFromLocalStorage(ca_chain, true);
            }
            if (!String.IsNullOrEmpty(key)) {
                keyBytes = Helpers.ReadFileFromLocalStorage(key, true);
            }
            return new DRMInfo(ClearVRDRMLicenseServerTypesMethods.GetFromStringValue(license_server_type), url, token, certificateBytes, keyBytes, caChainBytes, password);
        }

        public override String ToString() {
            return String.Format("DRM Server type: {0}, url: {1}, certificate file: {2}, key file: {3}, ca chain file: {4}, token: {5}, password: {6}", Utils.GetAsStringEvenIfNull(license_server_type), Utils.GetAsStringEvenIfNull(url), Utils.GetAsStringEvenIfNull(certificate), Utils.GetAsStringEvenIfNull(key), Utils.GetAsStringEvenIfNull(ca_chain), Utils.GetAsStringEvenIfNull(token), Utils.GetAsStringEvenIfNull(password));
        }
    }

    [Serializable]
    [Obsolete("This class has been forcefully deprecated. Please use FishEyeSettings instead.", true)]
    public class FishEyeSettingsFromJSON {
        public int camera_and_lens;
        public int lens_type;
        public float focal_length;
        public float sensor_pixel_density;
        public int reference_width;
        public int reference_height;

        /// <summary>
        /// Converts the object into the [FishEyeSettings](xref:com.tiledmedia.clearvr.FishEyeSettings) equivalent or null if only default values for its field are found.
        /// </summary>
        /// <returns>null if default values are found, an equivalent FishEyeSettings object otherwise.</returns>
        internal FishEyeSettings GetAsFishEyeSettings() {
            // camera_and_lens is the Int32 equivalent of FishEyeCameraAndLensTypes.CustomCameraAndLens, which would require the other params to be set to a non-null value.
            // We use this as a crude way to check if the struct contains non-default values.
            if(camera_and_lens == 0 && lens_type == 0 && focal_length == 0 && sensor_pixel_density == 0 && reference_width == 0 && reference_height == 0) {
                return null;
            }
            return new FishEyeSettings((FishEyeCameraAndLensTypes)camera_and_lens, (FishEyeLensTypes)lens_type, focal_length, sensor_pixel_density, reference_width, reference_height);
        }

        public override String ToString() {
            return String.Format("Camera and lens: {0}, lens type: {1}, focal length: {2}, sensor pixel density: {3}, reference width: {4}, reference height: {5}", Utils.GetAsStringEvenIfNull(camera_and_lens), Utils.GetAsStringEvenIfNull(lens_type), Utils.GetAsStringEvenIfNull(focal_length), Utils.GetAsStringEvenIfNull(sensor_pixel_density), Utils.GetAsStringEvenIfNull(reference_width), Utils.GetAsStringEvenIfNull(reference_height));
        }
   }

    [Serializable]
    [Obsolete("This class has been forcefully deprecated because ContentItemFromJSON has been deprecated.", true)]
    public class ContentItemListFromJSON {
        public com.tiledmedia.clearvr.demos.ContentItemFromJSON[] content_items;

        public ClearVRMessage VerifyList() {
            if (this.Length() > 0) {
                ContentItem[] contentItems;
                try {
                    contentItems = this.ConvertToContentItemList().contentItems;
                }
                catch (Exception) {
                    return ClearVRMessage.GetGenericFatalErrorMessage("Cannot convert JSON list to content item list.");
                }
                for (int i = 0; i < this.Length(); i++) {
                    if (String.IsNullOrEmpty(contentItems[i].manifestUrl)) {
                        return ClearVRMessage.GetGenericFatalErrorMessage(String.Format("Item url at index {0} in the contentlist is null or empty.", i));
                    }
                }
                return null;
                
            }
            else {
                return ClearVRMessage.GetGenericFatalErrorMessage("Content list is empty");
            }
        }
        public ContentItemList ConvertToContentItemList() {
            ContentItemList contentItemList = new ContentItemList();
            contentItemList.contentItems = new ContentItem[content_items.Length];
            int i = 0;
            foreach (ContentItemFromJSON contentItemFromJSON in content_items) {
                contentItemList.contentItems[i] = contentItemFromJSON.ConvertToContentItem();
                i++;
            }
            return contentItemList;
        }
        public ContentItem GetContentItemAtIndex(int index) {
            return this.ConvertToContentItemList().contentItems[index];
        }
        public int Length() {
            return this.ConvertToContentItemList().contentItems.Length;
        }
    }
}