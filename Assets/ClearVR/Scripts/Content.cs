using System;

using cvri = com.tiledmedia.clearvr.cvrinterface;

namespace com.tiledmedia.clearvr {
    /// <summary>
    /// This object is used to load an initial clip and to switch to a new clip.
    /// </summary>
    [Serializable]
    public class ContentItem {
        /// <summary>
        /// The manifest URL that should be loaded
        /// </summary>
        public string manifestUrl;

        /// <summary>
        /// Since v7
        /// 
        /// Default value: null
        ///
        /// Allows one to specify DRM-specific information for this clip. Please note the limitations of playing back DRM protected content.
        /// 
        /// Notes:
        /// 1. Widevine L1 protected content can only be played on an OVROverlay. Requires platformOptions.contentProtectionRobustnessLevel = [HWSecureAll](xref:com.tiledmedia.clearvr.ContentProtectionRobustnessLevels.HWSecureAll) 
        /// 2. Widevine L3 protected or SampleAES encrypted content requires platformOptions.contentProtectionRobustnessLevel = [SWSecureDecode](xref:com.tiledmedia.clearvr.ContentProtectionRobustnessLevels.SWSecureDecode)
        /// 3. Setting this field to null equals to specifying that this particular clip is NOT DRM protected.
        /// 4. One can freely switch between protected and non-protected content.
        /// 4. Playback of Widevine protected content is only available on Android.
        /// </summary>
        public DRMInfo drmInfo = null;
        
        /// <summary>
        /// Since v7.4
        /// Optional in case of explicitly overriding the content format to configurable fish eye, i.e. setting ContentFormat.MonoscopicFishEye or ContentFormat.StereoscopicFishEyeSBS
        /// The class allows to configure camera and lens specific fish eye settings. 
        /// Default value: null
        /// </summary>
        public FishEyeSettings fishEyeSettings {
            get {
                return _fishEyeSettings;
            }
            set {
                _fishEyeSettings = value;
            }
        }
        internal FishEyeSettings _fishEyeSettings = null;

        /// <summary>
        /// Since v7
        /// Deprecated since v9.0
        /// 
        /// startViewportAndObjectPose specify the start pose of the viewport and start pose of the display object for the first frame of the new content.
        /// 
        /// Setting this value to null will assume that you want the ClearVRPlayer to decide where the object should be placed. This would be preferred behaviour.
        /// If you set this to non-null AND if you change more than just the display object's scale, automatic mesh placement will be disabled.
        /// 
        /// > [!NOTE
        /// > Normally, you would only set a custom ClearVRViewportAndObjectPose if you want to adjust the initial orientation of an omnidirectional video (e.g. you want to apply a 90 degree offset). In that case, set the values of choice on clearVRViewportAndObjectPose.displayobject.pose 
        /// > 
        /// </summary>
        [Obsolete("This field has been deprecated in v9.x and can no longer be used. Please refer to the LayoutManager instead to achieve the same thing.", true)]
        public ClearVRViewportAndObjectPose startViewportAndObjectPose;
        
        /// <summary>
        /// When playing non-ClearVR and non-Mosaic content (e.g. HLS or pMP4), you have to explicitly set the ContentFormat (aka projection type). <br/>
        /// When set to the default value ContentFormat.Unknown, the SDK will: <br/>
        /// 1. in case of ClearVR content:     infer the ConfentFormat automatically. This will always be correct as the ContentFormat is embedded in the stream.<br/>
        /// 2. in case of non-ClearVR content: assume the ContentFormat to be Rectiinear (aka traditional rectangular video).<br/>
        /// If you want to play content of another projection type (e.g. monocopic ERP360), please configure this field accordingly, ref. [ContentFormat enum](xref:com.tiledmedia.clearvr.ContentFormat).<br/>
        /// When playing ClearVR or Mosaic content, you can always leave this field at its default value: `ContentFormat.Unknown`.
        /// </summary>
        public ContentFormat overrideContentFormat = ContentFormat.Unknown;
        
        /// <summary>
        /// Whether playback of this clip is known to work or not on the current device. By default, this is unknown. One should use the [TestIsContentSupported](xref:com.tiledmedia.clearvr.ClearVRPlayer.TestIsContentSupported(com.tiledmedia.clearvr.ContentSupportedTesterParameters,Action{com.tiledmedia.clearvr.ContentSupportedTesterReport,System.Object[]},Action{com.tiledmedia.clearvr.ClearVRMessage,System.Object[]},System.Object[])) API to test for content compatibility.
        /// </summary>
        public ContentSupportedStatus contentSupportedStatus {
            get {
                return _contentSupportedStatus;
            }
        }
        internal ContentSupportedStatus _contentSupportedStatus = ContentSupportedStatus.Unknown;

        /// <summary>
        /// Since v8
        /// ContentItem constructor
        /// 
        /// <param name="argManifestUrl">The manifest url pointing to the clip that needs to be loaded.</param>
        /// <param name="argStartViewportAndObjectPose">Start pose of the viewport and start pose and scale of the display object. One can use the ClearVRViewportAndObjectPose(double) constructor to set the default object. If set to null, the default will be used, which is position (0, 0, 0), orientation (w=1, x=0, y=0, z=0). If you set this to non-null AND if you change more than just the display object's scale, automatic mesh placement will be disabled. Typically, this field is only set to non-null to set a rotation offset for omnidirectional content.</param>
        /// <param name="argOverrideContentFormat">Override content format. Keep at itś default value ContentFormat.Unknown unless needed otherwise. See [overrideCOntentFormat](xref:com.tiledmedia.clearvr.ContentItem.overrideContentFormat) for details.</param>
        /// <param name="argDRMInfo">Specify DRM info, required to decrypt this content item.</param>
        /// <param name="argFishEyeSettings">Configure camera and lens specific fish eye settings for ContentFormat.MonoscopicFishEye or ContentFormat.StereoscopicFishEyeSBS, ignored otherwise. Default value: null</param>
        /// </summary>
        [Obsolete("This constructor has been deprecated in v9.x and cannot not be used anymore. The argStartViewportAndObjectPose argument has been removed, use the LayoutManager instead ot achieve initial mesh placement.", true)]
        public ContentItem(String argManifestUrl, ClearVRViewportAndObjectPose argStartViewportAndObjectPose = null, ContentFormat argOverrideContentFormat = ContentFormat.Unknown, DRMInfo argDRMInfo = null, FishEyeSettings argFishEyeSettings = null) {
            // Obsolete, intentionally left empty.
        }

        /// <summary>
        /// ContentItem constructor
        /// 
        /// <param name="argManifestUrl">The manifest url pointing to the clip that needs to be loaded.</param>
        /// <param name="argOverrideContentFormat">Override content format. Keep at itś default value ContentFormat.Unknown unless needed otherwise. See [overrideCOntentFormat](xref:com.tiledmedia.clearvr.ContentItem.overrideContentFormat) for details.</param>
        /// <param name="argDRMInfo">Specify DRM info, required to decrypt this content item.</param>
        /// <param name="argFishEyeSettings">Configure camera and lens specific fish eye settings for ContentFormat.MonoscopicFishEye or ContentFormat.StereoscopicFishEyeSBS, ignored otherwise. Default value: null</param>
        /// > [!NOTE]
        /// > Since v9.x, one can no longer specify a custom initial mesh positon. Instead, use the [LayoutManager](xref:com.tiledmedia.clearvr.ClearVRLayoutManager). 
        /// </summary>
        [Obsolete("This constructor has been deprecated and should no longer be used. Use the ContentItem(String) constructor instead and set the appropriate fields manually.", true)]
        public ContentItem(String argManifestUrl, ContentFormat argOverrideContentFormat = ContentFormat.Unknown, DRMInfo argDRMInfo = null, FishEyeSettings argFishEyeSettings = null) {
            // Obsolete, intentionally left empty
        }

        /// <summary>
        /// The default constructor for a ContentItem. One can set additional fields, like [drmInfo](xref:com.tiledmedia.clearvr.ContentItem.drmInfo) and [fishEyeSettings](xref:com.tiledmedia.clearvr.ContentItem.fishEyeSettings) by directly setting the appropriate fields.
        /// </summary>
        /// <param name="argManifestUrl">The url pointing to the clip that needs to be loaded.</param>
        public ContentItem(String argManifestUri) {
            manifestUrl = argManifestUri;
        }

        /// <summary>
        /// Returns a deep copy of this object.
        /// </summary>
        /// <returns>A deep copy of this object.</returns>
        public ContentItem Copy() {
            ContentItem copy = new ContentItem(this.manifestUrl);
            copy.overrideContentFormat = this.overrideContentFormat;
            copy.drmInfo = this.drmInfo != null ? this.drmInfo.Copy() : null;
            copy.fishEyeSettings = this.fishEyeSettings != null ? this.fishEyeSettings.Copy() : null;
            return copy;
        }

        public bool Verify() {
            return !String.IsNullOrEmpty(manifestUrl);
        }

        /// <summary>
        /// Create a deepcopy of the object.
        /// </summary>
        /// <returns>A deepcopy on the object.</returns>
        [Obsolete("This API has been deprecated in v9.0 and replaced by Copy()", true)]
        public ContentItem Clone() {
            return this.Copy();
        }

        /// <summary>
        /// Returns a properly formatted description of this object as a string
        /// </summary>
        /// <returns>A verbose print of all object's fields..</returns>
        public override String ToString() {
            return String.Format("Url: {0}, is supported: {1}, override content format: {2}, drm info: {3}, fish eye settings: {4}", Utils.GetAsStringEvenIfNull(manifestUrl), contentSupportedStatus, overrideContentFormat, Utils.GetAsStringEvenIfNull(drmInfo), Utils.GetAsStringEvenIfNull(fishEyeSettings));
        }

        internal cvri.ContentItem ToCoreProtobuf() {
            cvri.Projection projectionOverride = null;
            if (overrideContentFormat != ContentFormat.Unknown) {
                String[] stringValues = overrideContentFormat.GetStringValues();
                String stringValue = "";
                if(stringValues.Length > 0) {
                    stringValue = stringValues[0];
                }
                projectionOverride = new cvri.Projection {
                    ProjectionType = stringValue
                };
            }

            if ((overrideContentFormat == ContentFormat.MonoscopicFishEye || overrideContentFormat == ContentFormat.StereoscopicFishEyeSBS) && fishEyeSettings != null) {
                projectionOverride.FishEyeSettings = fishEyeSettings.ToCoreProtobuf();
            }

            return new cvri.ContentItem{
                URL = manifestUrl,
                ProjectionOverride = projectionOverride,
                DRM = drmInfo != null ? drmInfo.ToCoreProtobuf() : null
            };
        }

        internal static ContentItem FromCoreProtobuf(cvri.ContentItem coreContentItem) {
            if(coreContentItem == null) {
                return null;
            }
            ContentItem contentItem = new ContentItem(coreContentItem.URL);
            if (coreContentItem.DRM != null) {
                contentItem.drmInfo = new DRMInfo(coreContentItem.DRM);
            }
            if (coreContentItem.ProjectionOverride != null) {
                if (coreContentItem.ProjectionOverride.FishEyeSettings != null) {
                    contentItem.fishEyeSettings = FishEyeSettings.FromCoreProtobuf(coreContentItem.ProjectionOverride.FishEyeSettings);
                }
                if (coreContentItem.ProjectionOverride.ProjectionType != null) {
                    contentItem.overrideContentFormat = ContentFormatMethods.FromStringValue(coreContentItem.ProjectionOverride.ProjectionType);
                }
            }
            return contentItem;
        }
    }

    /// <summary>
    /// Convenience array of ContentItems
    /// </summary>
    [Obsolete("This class has been forcefully deprecated. Please make an array yourself.", true)]
    [Serializable]
    public class ContentItemList {
        [Obsolete("The `content_items` field has been renamed to `contentItems` (see [contentItems](xref:com.tiledmedia.clearvr.ContentItemList.contentItems)).", true)]
        public com.tiledmedia.clearvr.ContentItem[] content_items;
        public com.tiledmedia.clearvr.ContentItem[] contentItems;
    }
}