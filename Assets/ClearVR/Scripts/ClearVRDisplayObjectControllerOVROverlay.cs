using UnityEngine;
using System;
namespace com.tiledmedia.clearvr {
    /// <summary>
    /// Holds the ClearVRDisplayObjectController gameobject which is rendered on an OVROverlay
    /// </summary>
    public class ClearVRDisplayObjectControllerOVROverlay : ClearVRDisplayObjectControllerMesh {
        
        private const MeshTextureModes _meshTextureMode = MeshTextureModes.OVROverlay;

        public override MeshTextureModes meshTextureMode { 
            get {
                return _meshTextureMode;
            } 
            set { }
        }
        
        // This field is used in ClearVRLayoutParametersPropertyDrawer. Do not remove. 
#pragma warning disable 0414
        [HideInInspector]
        [SerializeField]
        private int _editorGUI_ID_01 = (int) _meshTextureMode;
#pragma warning restore 0414

#if CVR_SUPPORT_OCULUS_RUNTIME_EXTENSIONS
        private OVRPlugin.OverlayShape currentOverlayShape = OVRPlugin.OverlayShape.Quad;
        private OVRPlugin.Bool _overridePerLayerColorScaleAndOffsetB {
            get {
                return overridePerLayerColorScaleAndOffset ? OVRPlugin.Bool.True : OVRPlugin.Bool.False;
            }
            set {
                overridePerLayerColorScaleAndOffset = value == OVRPlugin.Bool.True;
            }
        }

        /// <summary>
        /// Use this public boolean to override per layer color scale and offset attribute in a similar way as found on a traditional OVROverlay object.
        /// </summary>
        [NonSerialized]
        [HideInInspector]
        public bool overridePerLayerColorScaleAndOffset = false;
        
        /// <summary>
        /// Determines whether this OVROverlay should be rendered as Overlay or Underlay.
        /// As Underlay is very costly GPU-wise, so one is strongly recommended to always use Overlay mode. <br/>
        /// The initial value can be set through platformOptions.ovrOverlayOptions.isOverlay.
        /// </summary>
        /// <param name="argIsOverlay">true = overlay, false = underlay</param>
        [NonSerialized]
        [HideInInspector]
        public bool isOverlay = true;

        private OVRPlugin.Bool _isOverlayB {
            get {
                return isOverlay ? OVRPlugin.Bool.True : OVRPlugin.Bool.False;
            } 
            set {
                isOverlay = value == OVRPlugin.Bool.True;
            }
        }


        /// <summary>
        /// Use this public Vector4 to override the color scale in a similar way as found on a traditional OVROverlay object. Default value: Vector4.one
        /// </summary>
        [NonSerialized]
        [HideInInspector]
        public Vector4 colorScale = Vector4.one;
        /// <summary>
        /// Use this public Vector4 to override the color offset in a similar way as found on a traditional OVROverlay object. Default value: Vector4.zero
        /// </summary>
        [NonSerialized]
        [HideInInspector]
        public Vector4 colorOffset = Vector4.zero;
        // These fields are mapped OVRPlugin (C#) version of Pose and Scale
        private OVRPose _pose;
	    private Vector3 _scale;
        // Fields mapped to native OVRPlugin/NRP
        private OVRPlugin.Posef _posef;
	    private OVRPlugin.Vector3f _scalef;

        private Vector3 overlayScaleUsedAsMonoEquivalentAspectRatio = new Vector3(1.0F, 1.0F, 1.0F);
        // can be flipped on the fly, see EnableOrDisableStereoscopicRendering()
        private bool forceMonoscopicRendering = false;
        private OVRPlugin.Bool _forceMonoscopicRenderingB {
            get {
                return forceMonoscopicRendering ? OVRPlugin.Bool.True : OVRPlugin.Bool.False;
            }   
            set {
                forceMonoscopicRendering = value == OVRPlugin.Bool.True;
            } 
        }
        private bool isHeadLocked = false; // Always false, the video is world locked.

        private OVRPlugin.Bool _isHeadLockedB {
            get {
                return isHeadLocked ? OVRPlugin.Bool.True : OVRPlugin.Bool.False;
            }
            set {
                isHeadLocked = value == OVRPlugin.Bool.True;
            }
        }

        public override void Initialize(PlatformOptionsBase argPlatformOptions, SharedPointersWithSDK sharedPointersWithSDK, object argReserved) {
            base.Initialize(argPlatformOptions, sharedPointersWithSDK, argReserved);
            isOverlay = argPlatformOptions.ovrOverlayOptions.isOverlay;
            UpdateShader();
            UpdateApplicationMeshState(); // Force update the Pose of the OVROverlay prior to rendering it for the first time.
            forceDisableMeshRendererAtAllTimes = true; // We NEVER want to see the MeshRenderer, so we permanently disable it.
            LateUpdate();
        }

        public override void UpdateShader() {
            // We do not call base.UpdateShader().
            // Mesh positioning logic in ClearVRDisplayObjectControllerBase::GetDefaultDisplayObjectPose() assumes the latter as mesh extents, not the former.
            overlayScaleUsedAsMonoEquivalentAspectRatio = transform.localScale;
            switch(clearVRMeshType) {
                case ClearVRMeshTypes.Unknown:
                case ClearVRMeshTypes.Cubemap:
                case ClearVRMeshTypes.FishEye:
                case ClearVRMeshTypes.Cubemap180: {
                    currentOverlayShape = OVRPlugin.OverlayShape.Cubemap;
                    break;
                }
                case ClearVRMeshTypes.Planar:
                case ClearVRMeshTypes.Rectilinear: {
                    currentOverlayShape = OVRPlugin.OverlayShape.Quad;
                    break;
                }
                case ClearVRMeshTypes.ERP:
                case ClearVRMeshTypes.ERP180: {
                    currentOverlayShape = OVRPlugin.OverlayShape.Equirect;
                    break;
                }
                default:
                    ClearVRLogger.LOGE("OVROverlay based rendering does not support mesh type {0}. Please report this issue to Tiledmedia.", clearVRMeshType);
                    break;
            }
        }

        public override void UpdateApplicationMeshState() {
#if UNITY_ANDROID && !UNITY_EDITOR
            NativeRendererPluginAndroid.CVR_NRP_UpdateApplicationMeshStateWithOVR(_displayObjectDescriptorWrapper.displayObjectID, _isHeadLockedB, _posef, _scalef, _overridePerLayerColorScaleAndOffsetB, colorScale, colorOffset, _forceMonoscopicRenderingB, _isOverlayB);
#else
            ClearVRLogger.LOGE("Unable to handle OVROVerlay::UpdateApplicationMeshState(). Not implemented!");
#endif
        }

        public override void RecreateNativeTextures() {
            // Intentionally left empty, NRP is managing textures.
            DestroyNativeTextures();
        }
        public override void UpdateNativeTextures(IntPtr argTextureId0, IntPtr argTextureId1, IntPtr argTextureId2) {
            // Intentionally left empty, to override default behaviour.
        }

        public override void DestroyNativeTextures() {
            // This method is intentionally left empty, as the Texture life-cycle is left to the NRP
        }

        /// <summary>
        /// We do not create local Texture2D / TextureCubemap objects from the texture handles bound to the swapchain.
        /// Therefor, this API is defunct and will always return null.
        /// </summary>
        /// <returns>null, always.</returns>
        public override Texture GetTexture() {
            ClearVRLogger.LOGW("You cannot get a reference to the currently active Texture object when running in OVROverlay mode.");
            return null;
        }

        void LateUpdate() {
            if(_isRegisteredAndInitialized) {
                DoBaseLateUpdate();
                ComputeSubmit(ref _posef,
                    ref _scalef,
                    ref isOverlay,
                    ref isHeadLocked);
                UpdateApplicationMeshState();
            }
        }

        public override void BindNativeTextureToShader(Texture argNativeTexture) {
            // Do nothing
        }

        private bool ComputeSubmit(ref OVRPlugin.Posef argPose, ref OVRPlugin.Vector3f argScale, ref bool argOverlay, ref bool argHeadLocked) {
            bool _isHeadLocked = false;
            bool _isOverlay = true;
            bool isSuccess = ComputeSubmit(ref _pose, ref _scale, ref _isOverlay, ref _isHeadLocked);
            argPose = _pose.flipZ().ToPosef_Legacy(); // note that OVR uses -z axis
            argScale = _scale.ToVector3f();
            argOverlay = _isOverlay;
            argHeadLocked = _isHeadLocked;
            return isSuccess;
        }

        /// <summary>
        /// This is a slightly simplified version of OVROverlay.ComputeSubmit()
        /// </summary>
        private bool ComputeSubmit(ref OVRPose argPose, ref Vector3 argScale, ref bool argOverlay, ref bool argHeadLocked) {
            argOverlay = isOverlay;
            argHeadLocked = isHeadLocked; /* We only support world-locked OVROverlay mode, head locked is NOT supported! */
            argPose = transform.ToTrackingSpacePose(_platformOptions.renderCamera);
            argScale = overlayScaleUsedAsMonoEquivalentAspectRatio;
            for (int i = 0; i < 3; ++i) {
                argScale[i] /= _platformOptions.renderCamera.transform.lossyScale[i];
            }
            if (currentOverlayShape == OVRPlugin.OverlayShape.Cubemap) { // Cubemap is always at the center.
    #if UNITY_ANDROID && !UNITY_EDITOR
                //HACK: VRAPI cubemaps assume are yawed 180 degrees relative to LibOVR.
                argPose.orientation = argPose.orientation * Quaternion.AngleAxis(180, Vector3.up);
    #endif
                argPose.position = _platformOptions.renderCamera.transform.position;
            }
            return true;
        }
        
        public override void EnableOrDisableStereoscopicRendering(bool argValue, bool argForceUpdate = false) {
            // argValue = true means: stereoscopic rendering required
            // argValue = false means:  monoscopic rendering required
            forceMonoscopicRendering = !argValue; // So we invert the bool here.
        }

        public override string ToString() {
            return String.Format("Overlay: {0}. HeadLocked: {1}, pose: {2}, scale: {3}, forceMono: {4}, {5}", isOverlay, isHeadLocked, _posef, _scalef, forceMonoscopicRendering, base.ToString());
        }
#endif
    }
}
