using System;
using UnityEngine;
using System.Runtime.InteropServices;
using System.Text;

namespace com.tiledmedia.clearvr {
    
    [DisallowMultipleComponent]
    [Serializable]
    /// <summary>
    /// This is an abstract class. Please refer to its implementations [Mesh](xref:com.tiledmedia.clearvr.ClearVRDisplayObjectControllerMesh), [Sprite](xref:com.tiledmedia.clearvr.ClearVRDisplayObjectControllerSprite), [OVROverlay (Android Only)](xref:com.tiledmedia.clearvr.ClearVRDisplayObjectControllerOVROverlay) and [UnmanagedMesh (advanced)](xref:com.tiledmedia.clearvr.ClearVRDisplayObjectControllerUnmanagedMesh).
    /// </summary>
    public abstract class ClearVRDisplayObjectControllerBase : MonoBehaviour, ClearVRDisplayObjectControllerInterface, ClearVRDisplayObjectControllerInterfaceInternal {

        [HideInInspector] 
        public abstract MeshTextureModes meshTextureMode { get; set;}


        /// <summary>
        /// You can use a placeholder mesh to design your scene more efficiently. By setting this boolean to true, this placeholder mesh is immediately hidden upon awake.
        /// If set to false, the mesh placeholder will not be hidden and simply replaced by video once that video has been loaded. The latter mode can be useful when the placeholder mesh contains e.g. a "loading..." texture.
        /// </summary>
        [Tooltip("Hide placeholder mesh (if any) directly in Awake. Default: true. If set to false, the placeholder mesh will remain visible until the video starts playing.")]
        public bool hideInAwake = true;

        /// <summary>
        /// Get the feed index that is currently present on the associated gameObject.
        /// Note that a DisplayObjectController can be _mapped_ to a feed index (through the [LayoutManager](xref:com.tiledmedia.clearvr.ClearVRLayoutManager)), but this mapping might not be active yet (because the feed is still loading for example). In this case, this API returns -1.
        /// </summary>
        /// <value>The currently active feed index, or -1 if no Feed is active yet.</value>
        public int activeFeedIndex {
            get {
                return _displayObjectDescriptorWrapper.feedIndex;
            }
        }

        protected ContentFormat _contentFormat = ContentFormat.Unknown;
		protected RenderModes _renderMode = RenderModes.Native;

        /// <summary>
        /// Get and set the RenderMode of this DisplayObject.
        /// </summary>
        /// <value></value>
        public RenderModes renderMode {
            get {
                return _renderMode;
            }
            set {
                SetRenderMode(value);
            }
        }

        public ContentFormat contentFormat {
            get {
                return _contentFormat;
            }
        }

        /// <summary>
        /// Returns the current DisplayObjectClassType of the DisplayObject. This value is always up-to-date. 
        /// > [!NOTE]
        /// > The DisplayObjectClassType can change during the lifecycle of a DisplayObject based on the Layout you request. The ClearVRDisplayObjectEventTypes.ClassTypeChanged event is emitted when it has changed.
        /// </summary>
        /// <value>The DisplayObjectClassType of the DisplayObject, or DisplayObjectClassTypes.Unknown if not available (when the DisplayObject is not [active](xref:com.tiledmedia.clearvr.ClearVRDisplayObjectControllerBase)).</value>
        public DisplayObjectClassTypes displayObjectClassType {
            get {
                return  _displayObjectDescriptorWrapper.displayObjectClassType  ;
            }
        }

        /// <summary>
        /// Returns the current DisplayObjectClassType of the DisplayObject. This value is always up-to-date. 
        /// > [!NOTE]
        /// > The DisplayObjectClassType can change during the lifecycle of a DisplayObject based on the Layout you request. The ClearVRDisplayObjectEventTypes.ClassTypeChanged event is emitted when it has changed.
        /// </summary>
        /// <value>The DisplayObjectClassType of the DisplayObject, or DisplayObjectClassTypes.Unknown if not available (when the DisplayObject is not [active](xref:com.tiledmedia.clearvr.ClearVRDisplayObjectControllerBase)).</value>
        public DisplayObjectClassTypes GetDisplayObjectClassType() {
            return displayObjectClassType;
        }

        /// <summary>
        /// Returns the active state of this mesh. This is NOT the same as `gameObject.activeSelf`.
        /// </summary>
        /// <value>True if the DisplayObject is activated, false otherwise.</value>
        // Intentionally, this API always returns the latest avalable value.
        public bool isActive {
            get {
                return _displayObjectDescriptorWrapper.isActive;
            }
        }

        /// <summary>
        /// Returns the active state of this mesh. This is NOT the same as `gameObject.activeSelf`.
        /// </summary>
        /// <value>True if the DisplayObject is activated, false otherwise.</value>
        // Intentionally, this API always returns the latest avalable value.
        public bool GetIsActive() {
            return isActive;
        }

        // Do not make public for now until we have decided to do so anyway.
        internal int displayObjectID {
            get {
                return _displayObjectID;
            }
        }

        private int _displayObjectID = -1;
        
        protected ClearVRLayoutManager clearVRLayoutManager {
            get {
                if(_clearVRLayoutManager == null) {
                    _clearVRLayoutManager = FindObjectOfType<ClearVRLayoutManager>();
                }
                return _clearVRLayoutManager;
            }
        }

        private ClearVRLayoutManager _clearVRLayoutManager;


        protected bool _isReadyForDestruction = false;
        protected Material _material;
        protected bool _isStereoCustomUVsOnKeywordEnabled = false;
        protected bool _isOESFastPathOnKeywordEnabled = false;
        protected bool _isTransparencyOnKeywordEnabled = false;
        protected bool _isPicoVREyeIndexKeywordEnabled = false;
        protected bool _isZWriteEnabled = true /* default value is true */;
        protected Color _shaderColor = new Color(-1, -1, -1, -1);
        protected Matrix4x4 _shaderColorSpaceYUVtoRGBTransformMatrix = Matrix4x4.identity;
        private Texture[] _nativeTextures = null;
        protected IntPtr _lastTextureId = IntPtr.Zero;
        protected PlatformOptionsBase _platformOptions = null;
        protected static int _cameraPositionId;
        protected static int _viewMatrixId = 0;

        public ClearVRMeshTypes clearVRMeshType { get; private set; }
        internal NativeRendererPluginBase.DisplayObjectDescriptorWrapper _displayObjectDescriptorWrapper = new NativeRendererPluginBase.DisplayObjectDescriptorWrapper();
        protected bool forceDisableMeshRendererAtAllTimes = false;
        /// <summary>
        /// Raised when a register-request is requested on the LayoutManager. When true, the registration at the NRP might have completed already, but this is not guaranteed. TO check for a complete registration, isRegisteredAndInitialized must be raised.
        /// This flag is only lowered once we requested to unregister the DisplayObject on the LayoutManager.
        /// </summary>
        private bool _isRegisteredOrRegisterPending = false;
        /// <summary>
        /// When raised, the DO has been registered at the NRP and is fully initialized.
        /// </summary>
        internal bool isRegisteredAndInitialized {
            get {
                return _isRegisteredAndInitialized;
            }
        }
        protected bool _isRegisteredAndInitialized = false;
        protected static Shader clearVRShader = null;
        protected static Shader genericNonClearVROmniShader = null;
        internal static Shader clearVRUIShader = null;
		internal ClearVRDisplayObjectEventsInternal clearVRDisplayObjectEvents = new ClearVRDisplayObjectEventsInternal();
        protected Material  _originalMaterial = null;
        protected Shader _originalShader = null;
        protected bool _originalActiveState = true;
        public int frameWidth {
            get {
                if (!_isRegisteredAndInitialized || !_displayObjectDescriptorWrapper.IsInitialized()) {
                    return 0;
                }
                return _displayObjectDescriptorWrapper.frameWidth;
            }
        }

        public int frameHeight {
            get {
                if (!_isRegisteredAndInitialized || !_displayObjectDescriptorWrapper.IsInitialized()) {
                    return 0;
                }
                return _displayObjectDescriptorWrapper.frameHeight;
            }
        }

        void OnEnable() {
            MaybeRegisterAtLayoutManager();
        }
        
        // Should only be called from OnEnable
        internal void MaybeRegisterAtLayoutManager() {
            if(_isRegisteredOrRegisterPending) {
                return;
            }
            if(clearVRLayoutManager != null) {
                _isRegisteredOrRegisterPending = true; // Though registering is async, we immediately raise the flag as to make sure we do not accidentally register multiple times.
                clearVRLayoutManager.RegisterClearVRDisplayObjectControllerSyncMaybe(this);
            }
        }

        private void _MaybeUnregisterAtLayoutManager() {
            if(!_isRegisteredOrRegisterPending) {
                return; 
            }
            if(clearVRLayoutManager != null) {
                _isRegisteredOrRegisterPending = false; // Lower the flag to make sure we do not request an unregister multiple times.
                clearVRLayoutManager.UnregisterClearVRDisplayObjectControllerSyncMaybe(this);
            }             
        }

        private static void GetColorSpaceConstants(ColorSpaceStandards colorSpace, out float wr, out float wb, out int black, out int white, out int max) {
            // Default is BT709 (so unspecified is defaulted to BT709)
            wr = 0.2126f;
            wb = 0.0722f;
            black = 16;
            white = 235;
            max = 255;
            if (colorSpace == ColorSpaceStandards.BT601) {
                wr = 0.2990f; wb = 0.1140f;
            }
            else if (colorSpace == ColorSpaceStandards.BT2020_NCL || colorSpace == ColorSpaceStandards.BT2020_CL) {
                wr = 0.2627f; wb = 0.0593f;
                // 10-bit only
                black = 64 << 6; white = 940 << 6;
                max = (1 << 16) - 1;
            }
            return;
        }

        private static Matrix4x4 GetColorSpaceYUVtoRGBTransformMatrix(ColorSpaceStandards colorSpace) {
            float wr;
            float wb;
            int black;
            int white;
            int max;

            GetColorSpaceConstants(colorSpace, out wr, out wb, out black, out white, out max);
            float low = (float)black / (float)max;
            float mid = (float)(max + 1.0f) / (2.0f * (float)max);
            Matrix4x4 mat = new Matrix4x4(
                new Vector4(1.0f, 0.0f, (1.0f - wr) / 0.5f, 0.0f),
                new Vector4(1.0f, -wb * (1.0f - wb) / 0.5f / (1 - wb - wr), -wr * (1 - wr) / 0.5f / (1 - wb - wr), 0.0f),
                new Vector4(1.0f, (1.0f - wb) / 0.5f, 0.0f, 0.0f),
                new Vector4(-low, -mid, -mid, 0.0f)
            );
            mat = mat.transpose;
            for (int i = 0; i < 3; i++) {
                for (int j = 0; j < 3; j++) {
                    mat[i, j] = (float)(1.0f * (float)(max) / (float)(white - black) * mat[i, j]);
                }
            }
            return mat;
        }

        public virtual void UpdateApplicationMeshState() { 
            // noop except for OVR Overlay
        }

        public static void LoadShaders() {
            if (clearVRShader != null) { // Shaders are already loaded.
                return;
            }
            // Load suitable shader
            String[] shaderNames = { "ClearVR/ClearVRShader", "ClearVR/GenericNonClearVROmni", "ClearVR/ClearVRUIShader" };
            Shader tempShader;
            for (int i = 0; i < shaderNames.Length; i++) {
                try {
                    tempShader = Shader.Find(shaderNames[i]);
                    if (tempShader == null) {
                        throw new Exception("Shader appears to be missing from project.");
                    }
                    switch (i) {
                        case 0:
                            clearVRShader = tempShader;
                            break;
                        case 1:
                            genericNonClearVROmniShader = tempShader;
                            break;
                        case 2:
                            clearVRUIShader = tempShader;
                            break;
                        default:
                            throw new Exception("Shader load logic not implemented.");
                    }
                }
                catch (Exception e) {
                    throw new Exception(String.Format("[ClearVR] Unable to load shader {0}. Error: {1}", shaderNames[i], e));
                }
            }
        }

        // the base classes need to implement this method to initialize the renderer componant (if any) and set the _orignalMaterial and _material variable with a proper value
        abstract protected void InitializeRendererAndMaterial();

        protected void ControllerAwake() {
            // Disable MeshRenderer until its texture is available (otherwise it would show up as a texture-less (purple) sphere)
            _originalActiveState = IsMeshRendererEnabled();
            if(hideInAwake) {
                EnableOrDisableMeshRenderer(false);
            }
        }

        void Awake() {
            ControllerAwake();
        }

        /// <summary>
        /// Initialize the ClearVRDisplayObjectController for video playback.
        /// </summary>
        /// <param name="argPlatformOptions"></param>
        /// <param name="argMeshDescriptionStructIntPtr"></param>
        /// <param name="argReserved"></param>
        public virtual void Initialize(PlatformOptionsBase argPlatformOptions,
                                       SharedPointersWithSDK sharedPointersWithSDK,
                                       System.Object argReserved) {
            _isRegisteredAndInitialized = true;
            _displayObjectDescriptorWrapper.Initialize(sharedPointersWithSDK);
            _displayObjectID = _displayObjectDescriptorWrapper.displayObjectID;
            // Note: meshType is still UNKNOWN by this point.
            _platformOptions = argPlatformOptions;
            InitializeRendererAndMaterial();
        }

        void LateUpdate() {
            DoBaseLateUpdate();
        }

#if NET45_OR_GREATER
        // Request  inlining on .NET 4.5+
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public void DoBaseLateUpdate() {
            if (!_isRegisteredAndInitialized) {
                return;
            }
            if (_material != null) {
                _material.SetVector(_cameraPositionId, _platformOptions.renderCamera.transform.position);
                if (_viewMatrixId != 0) {
                    _material.SetMatrix(_viewMatrixId, _platformOptions.renderCamera.worldToCameraMatrix.transpose);
                }
            }
        }

        internal enum LocalUpdateState : UInt32 {
            NothingToDo = 0,
            UpdateMeshType = 1 << 0,
            UpdateShader = 1 << 1,
            RecreateNativeTextures = 1 << 2,
            EnableOrDisableMeshRenderer = 1 << 3,
            MaybeUpdateNativeTextures = 1 << 4,
            // UpdateFallbackLayout  = 1 << 5, // This has been removed and can be re-used for other purposes.
            UpdateBounds  = 1 << 6,
            UpdateContentFormat = 1 << 7,
            ForceRefreshShaderState = 1 << 8,
        };

        internal bool UpdateMDS(int vsyncCounter) {
            if (!_isRegisteredAndInitialized) {
                return false;
            }
            // Refresh the struct we get from the NRP
            var (somethingGotUpdated, wasLocked) = _displayObjectDescriptorWrapper.UpdateState();
            if (wasLocked) {
                return wasLocked;
            }
            if (!somethingGotUpdated) {
                // The displayObjectDescriptor has not changed. We can return immediately.
                return false;
            }
            UpdateApplicationMeshState(); // We send the update to the NRP if needed (for mainly used by OVR Overlay).

            //Will contains the local state
            LocalUpdateState localUpdateState = LocalUpdateState.NothingToDo;
            DisplayObjectDescriptorFlags flags = _displayObjectDescriptorWrapper.displayObjectDescriptorFlags;
            // Populate the local state based on the MDS flags
            if (flags.IsUnknown()) {
                return false;
            }
            if(flags.HasCreated()) {
                // NOOP
            }
            if (flags.HasTextureChanged()) {
                //we should set the shader flag if we allow the texture type to dynamically change too (for instance from nv12 to RGBA)
                localUpdateState.SetFlags(LocalUpdateState.RecreateNativeTextures);
                localUpdateState.SetFlags(LocalUpdateState.EnableOrDisableMeshRenderer);
                localUpdateState.SetFlags(LocalUpdateState.ForceRefreshShaderState);
            }
            if (flags.HasTextureUpdated()) {
                localUpdateState.SetFlags(LocalUpdateState.MaybeUpdateNativeTextures);
                localUpdateState.SetFlags(LocalUpdateState.ForceRefreshShaderState);
            }
            if (flags.HasTextureLatched()) {
                // NOOP
            }
            if (flags.HasActiveStateChanged()) {
                localUpdateState.SetFlags(LocalUpdateState.EnableOrDisableMeshRenderer);
            }
            if (flags.HasMeshChanged()) {
                localUpdateState.SetFlags(LocalUpdateState.UpdateBounds);
                localUpdateState.SetFlags(LocalUpdateState.EnableOrDisableMeshRenderer);
                localUpdateState.SetFlags(LocalUpdateState.ForceRefreshShaderState);
            }
            if (flags.HasMeshUpdated()) {
                // NOOP
            }
            
            if (flags.HasShaderParameterChanged()) {
                localUpdateState.SetFlags(LocalUpdateState.UpdateMeshType);
                localUpdateState.SetFlags(LocalUpdateState.UpdateShader);
                localUpdateState.SetFlags(LocalUpdateState.UpdateContentFormat);
                localUpdateState.SetFlags(LocalUpdateState.ForceRefreshShaderState);
            }
            if (HasLocalShaderParameterChange()) {
                localUpdateState.SetFlags(LocalUpdateState.UpdateShader);
            }
            if (flags.HasLateVertexUpload()) {
                /// NOOP, todo
            }

            // Populate the local state based on the MDS flags DONE
            // Now we trigger the updates based on the local state
                
            if (localUpdateState.HasFlag(LocalUpdateState.UpdateMeshType)) {
                UpdateMeshType();
            }
            if (localUpdateState.HasFlag(LocalUpdateState.UpdateContentFormat)) {
                _UpdateContentFormat();
            }
            if (localUpdateState.HasFlag(LocalUpdateState.UpdateShader)) {
                UpdateShader();
            }
            if (localUpdateState.HasFlag(LocalUpdateState.RecreateNativeTextures)) {
                RecreateNativeTextures();
            }

            if (localUpdateState.HasFlag(LocalUpdateState.EnableOrDisableMeshRenderer)) {
                EnableOrDisableMeshRenderer(GetIsActive());
                if (!GetIsActive()) {// the DO is not used anymore; Let release the native texture objects
                    UnbindAndReleaseNativeTextures();
                }
            }
            if (localUpdateState.HasFlag(LocalUpdateState.UpdateBounds)) {
                UpdateBounds();
            }
            if (localUpdateState.HasFlag(LocalUpdateState.MaybeUpdateNativeTextures)) {
                if (_lastTextureId != _displayObjectDescriptorWrapper.textureIDPlane0) {
                    UpdateNativeTextures(_displayObjectDescriptorWrapper.textureIDPlane0, _displayObjectDescriptorWrapper.textureIDPlane1, _displayObjectDescriptorWrapper.textureIDPlane2);
                }
            }
                        
            if (localUpdateState.HasFlag(LocalUpdateState.ForceRefreshShaderState)) {
                ForceRefreshShaderState();
            }


            // We can only call _UpdateContentFormat() if the mesh is active in the NRP (e.g. it is mapped to a feed).
            // Also, we should only call FFR if the same holds.
            if (GetIsActive() && (
                    flags.HasMeshChanged() || 
                    flags.HasTextureChanged())) {
                ScheduleClearVRDisplayObjectEvent(ClearVRDisplayObjectEvent.GetGenericOKEvent(ClearVRDisplayObjectEventTypes.FirstFrameRendered));
            }
            if (flags.HasActiveStateChanged()) {
                ScheduleClearVRDisplayObjectEvent(ClearVRDisplayObjectEvent.GetGenericOKEvent(ClearVRDisplayObjectEventTypes.ActiveStateChanged));
            }
            if (flags.HasFeedIndexChanged()) {
                // NOOP, when this holds, FirstFrameRendered is (also) already triggered by this point. This parser is added for future use
            }
            if (flags.HasClassTypeChanged()) {
                ScheduleClearVRDisplayObjectEvent(ClearVRDisplayObjectEvent.GetGenericOKEvent(ClearVRDisplayObjectEventTypes.ClassTypeChanged));
            }
            return false;
        }

        private void UpdateMeshType() {
            clearVRMeshType = _displayObjectDescriptorWrapper.clearVRMeshType;
        }

        public virtual void UpdateShader() {
            // We might have to attach a different shader.
            Shader shader;
            switch (clearVRMeshType) {
                case ClearVRMeshTypes.Cubemap: {
                        shader = clearVRShader;
                        break;
                    }
                case ClearVRMeshTypes.Planar: {
                        shader = clearVRShader;
                        break;
                    }
                case ClearVRMeshTypes.Cubemap180: {
                        shader = clearVRShader;
                        break;
                    }
                case ClearVRMeshTypes.ERP: {
                        shader = genericNonClearVROmniShader;
                        break;
                    }
                case ClearVRMeshTypes.ERP180: {
                        shader = genericNonClearVROmniShader;
                        break;
                    }
                case ClearVRMeshTypes.Rectilinear: {
                        shader = clearVRShader;
                        break;
                    }
                case ClearVRMeshTypes.FishEye: {
                        switch (_displayObjectDescriptorWrapper.clearVRFishEyeType) {
                            case ClearVRFishEyeTypes.NotSet:
                                throw new Exception("[ClearVR] Fish Eye parameters were not set but projection type is Fish Eye. Cannot continue.");
                            case ClearVRFishEyeTypes.EquiSolid:
                            case ClearVRFishEyeTypes.EquiDistant:
                            case ClearVRFishEyeTypes.Polynomial:
                                shader = genericNonClearVROmniShader;
                                break;
                            default:
                                {
                                    throw new Exception(String.Format("[ClearVR] FishEyeType {0} not implemented.", _displayObjectDescriptorWrapper.clearVRFishEyeType));
                                }
                        }
                        break;
                    }
                case ClearVRMeshTypes.Unknown:
                default: {
                        throw new Exception(String.Format("[ClearVR] MeshType {0} not implemented.", clearVRMeshType));
                    }
            }
            ReconfigureShader(shader);
        }

        protected void ReconfigureShader(Shader argShader) {
            if (_material != null && argShader != null) {
                _material.shader = argShader;
            }
#if UNITY_ANDROID && !UNITY_EDITOR
            EnableOrDisableOESFastPath(_platformOptions.textureBlitMode == TextureBlitModes.UVShufflingZeroCopy);
#endif
            // #if UNITY_ANDROID && !UNITY_EDITOR
            //             NativeRendererPluginAndroid.CVR_NRP_RestoreContext();
            // #endif
            _cameraPositionId = Shader.PropertyToID("_cameraPosition");
#if UNITY_ANDROID
            // Only on GLSLPROGRAM Shader paths (gles/gles3 only) needs the _viewMatrix variable. CGPROGRAM (metal/d3d11/glcore) uses the UNITY_MATRIX_V[0].xyz shader variable to achieve the same.
            if(SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3 || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.OpenGLES2) {
                _viewMatrixId = Shader.PropertyToID("_viewMatrix");
            }
#endif
            SetMainColor(_shaderColor);
            SetShaderColorSpace();
            SetTextureTransformMatrix();
            SetZWrite(_isZWriteEnabled, true);
            if (argShader == genericNonClearVROmniShader) {
                SetGenericNonClearVROmniParameters();
            } // else no need to set additional parameters on standard shader

        }

        internal virtual void UpdateBounds() {

        }

        internal virtual void ForceRefreshShaderState() {}

        public virtual void EnableOrDisableStereoscopicRendering(bool argValue, bool argForceUpdate = false) {
            if (_material != null) {
                if (argValue != _isStereoCustomUVsOnKeywordEnabled || argForceUpdate) {
                    if (argValue) {
                        _material.EnableKeyword("STEREO_CUSTOM_UV_ON");
                        _material.DisableKeyword("STEREO_CUSTOM_UV_OFF");
                    } else {
                        _material.DisableKeyword("STEREO_CUSTOM_UV_ON");
                        _material.EnableKeyword("STEREO_CUSTOM_UV_OFF");
                    }
                    _isStereoCustomUVsOnKeywordEnabled = argValue;
                }
            }
        }

        public void EnableOrDisableOESFastPath(bool argValue, bool argForceUpdate = false) {
            if (_material != null) {
                if (argValue != _isOESFastPathOnKeywordEnabled || argForceUpdate) {
                    if (argValue) {
                        _material.EnableKeyword("USE_OES_FAST_PATH_ON");
                        _material.DisableKeyword("USE_OES_FAST_PATH_OFF");
                    } else {
                        _material.DisableKeyword("USE_OES_FAST_PATH_ON");
                        _material.EnableKeyword("USE_OES_FAST_PATH_OFF");
                    }
                    _isOESFastPathOnKeywordEnabled = argValue;
                }
            }
        }

        private void EnableOrDisableTransparency(bool argValue, bool argForceUpdate = false) {
            if (_material != null) {
                if (argValue != _isTransparencyOnKeywordEnabled || argForceUpdate) {
                    if (argValue) {
                        _material.SetOverrideTag("RenderType", "Transparent");
                        _material.SetOverrideTag("Queue", "Transparent");
                        _material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        _material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        _material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

                    } else {
                        _material.SetOverrideTag("RenderType", "Opaque");
                        _material.SetOverrideTag("Queue", "Opaque");
                        _material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        _material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                        _material.renderQueue = -1;
                    }
                    _isTransparencyOnKeywordEnabled = argValue;
                }
            }
        }

        public void SetZWrite(bool argValue, bool argForceUpdate = false) {
            if (_material != null) {
                if (argValue != _isZWriteEnabled || argForceUpdate) {
                    _material.SetInt("_ZWrite", Convert.ToInt32(argValue));
                    _isZWriteEnabled = argValue;
                }
            }
        }

        public ContentFormat GetContentFormat() {
            return _contentFormat;
        }

        public void SetRenderMode(RenderModes argNewRenderMode) {
			// RenderModes.Native is of no use to the outside world. Therefor we always have to convert it into effective RenderMode.
			RenderModes effectiveRenderMode = argNewRenderMode;
			ClearVRDisplayObjectEvent clearVRDisplayObjectEvent = ClearVRDisplayObjectEvent.GetGenericOKEvent(ClearVRDisplayObjectEventTypes.RenderModeChanged);
			if(_contentFormat.GetIsStereoscopic()) {
				if(_platformOptions.deviceParameters.deviceType.GetIsVRDeviceThatCanRenderStereoscopicContent()) {
					switch(argNewRenderMode) {
						case RenderModes.Native:
							effectiveRenderMode = RenderModes.Stereoscopic;
							break;
						case RenderModes.Stereoscopic:
						case RenderModes.Monoscopic:
						case RenderModes.ForcedMonoscopic:
							break;
					}
				} else {
					switch(argNewRenderMode) {
						case RenderModes.Stereoscopic:
							clearVRDisplayObjectEvent.clearVRMessage.Update(ClearVRMessageTypes.Warning, ClearVRMessageCodes.SetRenderModeFailed, "Cannot switch to stereoscopic rendering, no headset detected.", ClearVRResult.Failure);
							goto case RenderModes.Native; // C#'s way of a fallthrough...
						case RenderModes.Native:
						case RenderModes.Monoscopic:
							effectiveRenderMode = RenderModes.Monoscopic;
							break;
						case RenderModes.ForcedMonoscopic:
							break;
					}
				}
			} else {
				// Content is monoscopic so we do not care for the DeviceType.
				switch(argNewRenderMode) {
					case RenderModes.Stereoscopic:
						// cannot comply
						clearVRDisplayObjectEvent.clearVRMessage.Update(ClearVRMessageTypes.Warning, ClearVRMessageCodes.SetRenderModeFailed, "Cannot switch to stereoscopic rendering, content is monoscopic.", ClearVRResult.Failure);
						goto case RenderModes.Native;
					case RenderModes.Native:
						effectiveRenderMode = RenderModes.Monoscopic;
						break;
					case RenderModes.Monoscopic:
					case RenderModes.ForcedMonoscopic:
						break;
				}
			}
			bool setMonoOrStereoMode = effectiveRenderMode == RenderModes.Stereoscopic; // true if .Stereoscopic, mono if .Monoscopic or .ForcedMonoscopic. At this point, it is never .Native
			if(String.IsNullOrEmpty(clearVRDisplayObjectEvent.clearVRMessage.message)) {
				clearVRDisplayObjectEvent.clearVRMessage.message = setMonoOrStereoMode ? "stereo" : "mono";
			}
            EnableOrDisableStereoscopicRendering(setMonoOrStereoMode);
			_renderMode = effectiveRenderMode;
			ScheduleClearVRDisplayObjectEvent(clearVRDisplayObjectEvent);
		}

        private bool IsStereoscopicModeActive() {
            return _displayObjectDescriptorWrapper.isStereoscopicModeActive;
        }

        /// <summary    
		/// Update the content format using the DisplayObjectDescriptor structure. Note that ContentFormat is something different than RenderMode
		/// </summary>
		private void _UpdateContentFormat() {
			// We never switch to RenderMOdes.ForcedMonoscopic internally.
			bool stereoscopicModeActive = IsStereoscopicModeActive(); // To fix #2140
			bool oldIsStereoscopic =  _contentFormat.GetIsStereoscopic();
			ContentFormat oldContentFormat = _contentFormat; // ContentFormat.Unknown by default.
			_contentFormat = Utils.ConvertInternalProjectionTypeToContentFormat(_displayObjectDescriptorWrapper.projectionType);
			bool newIsStereoscopic = _contentFormat.GetIsStereoscopic() && stereoscopicModeActive;
			if(_platformOptions.enableAutomaticRenderModeSwitching) {
				// Force synchronous render mode change. If we would've waited for the callback we would end-up with a blank right eye for one video frame
				RenderModes preferredRenderMode = RenderModes.Native;
				if(oldContentFormat == ContentFormat.Unknown) {
					// This is the very first clip that is played.
					if(newIsStereoscopic) {
						if(Utils.GetIsVrDevicePresent()) {
							switch(_platformOptions.preferredRenderMode) {
								case RenderModes.Native:
								case RenderModes.Stereoscopic:
									preferredRenderMode = RenderModes.Stereoscopic;
									break;
								case RenderModes.ForcedMonoscopic:
								case RenderModes.Monoscopic:
									preferredRenderMode = _platformOptions.preferredRenderMode;
									break;
							}
						} else {
							// In all cases we default to Monoscopic (or ForcedMonoscopic) rendering if no HMD is present.
							switch(_platformOptions.preferredRenderMode) {
								case RenderModes.Stereoscopic:
								case RenderModes.Native: 
									preferredRenderMode = RenderModes.Monoscopic;
									break;
								case RenderModes.ForcedMonoscopic:
								case RenderModes.Monoscopic:
									preferredRenderMode = _platformOptions.preferredRenderMode;
									break;
							}
						}
					} else {
						// In all cases we default to Monoscopic (or ForcedMonoscopic) rendering if no HMD is present.
						switch(_platformOptions.preferredRenderMode) {
							case RenderModes.Stereoscopic:
							case RenderModes.Native: 
								preferredRenderMode = RenderModes.Monoscopic;
								break;
							case RenderModes.ForcedMonoscopic:
							case RenderModes.Monoscopic:
								preferredRenderMode = _platformOptions.preferredRenderMode;
								break;
						}
					}
				} else {
					if(_renderMode != RenderModes.ForcedMonoscopic) {
						// This is a clip _after_ the first one (e.g. after one or more SwitchContents)
						if(oldIsStereoscopic && !newIsStereoscopic) {
							// We always have to explicitly switch to monoscopic rendering, otherwise the right eye will break down.
							preferredRenderMode = RenderModes.Monoscopic;
						} else if (!oldIsStereoscopic && newIsStereoscopic) {
							if(Utils.GetIsVrDevicePresent()) {
								preferredRenderMode = RenderModes.Stereoscopic;
							} else {
								preferredRenderMode = RenderModes.Monoscopic;
							}
						} else {
							// Keeps things as they are
							preferredRenderMode = _renderMode;
						}
					} else {
						preferredRenderMode = _renderMode; // = RenderModes.ForcedMonoscopic
					}
				}
				SetRenderMode(preferredRenderMode);
			}
			if(_contentFormat != oldContentFormat) {
				ScheduleClearVRDisplayObjectEvent(ClearVRDisplayObjectEvent.GetGenericOKEvent(ClearVRDisplayObjectEventTypes.ContentFormatChanged));
			}
		}

        /// <summary>
        /// Notes:
        /// This assumes:
        /// 1. Stereo ERP (360) is ALWAYS TB
        /// 2. Stereo ERP180 is ALWAYS SBS
        /// </summary>
        private void SetGenericNonClearVROmniParameters() {
            bool isStereo = false;
            if (clearVRMeshType == ClearVRMeshTypes.FishEye) {
                switch (_displayObjectDescriptorWrapper.clearVRFishEyeStereoType) {
                    case ClearVRFishEyeStereoTypes.StereoTypeMono:
                        isStereo = false;
                        break;
                    case ClearVRFishEyeStereoTypes.StereoTypeStereoSideBySide:
                        isStereo = true;
                        break;
                    case ClearVRFishEyeStereoTypes.StereoTypeStereoTopBottom:
                        throw new Exception(String.Format("[ClearVR] Fish eye stereo top-bottom not implemented yet."));
                }
            }
            else {
                isStereo = _displayObjectDescriptorWrapper.hasRightEye;
            }
            bool isVPStereo = _displayObjectDescriptorWrapper.hasRightEye;
            EnableOrDisableStereoscopicRendering(isStereo && isVPStereo, true);

            if (_material != null) {
                switch (clearVRMeshType) {
                    case ClearVRMeshTypes.FishEye:
                        if (_displayObjectDescriptorWrapper.clearVRFishEyeType == ClearVRFishEyeTypes.Polynomial) {
                            _material.EnableKeyword("FISH_EYE_POLYNOME");
                            _material.DisableKeyword("FISH_EYE_EQUISOLID");
                            _material.DisableKeyword("FISH_EYE_EQUIDISTANT");
                            _material.DisableKeyword("ERP");
                            _material.SetFloat("_CircularRadiusInRad", _displayObjectDescriptorWrapper.circularRadiusInRad);
                            _material.SetFloat("_CenterU", _displayObjectDescriptorWrapper.centerU);
                            _material.SetFloat("_CenterV", _displayObjectDescriptorWrapper.centerV);
                            _material.SetFloat("_AffineParameterC", _displayObjectDescriptorWrapper.affineParameterC);
                            _material.SetFloat("_AffineParameterD", _displayObjectDescriptorWrapper.affineParameterD);
                            _material.SetFloat("_AffineParameterE", _displayObjectDescriptorWrapper.affineParameterE);
                            _material.SetFloat("_ReferenceWidth", (float)_displayObjectDescriptorWrapper.referenceWidth);
                            _material.SetFloat("_ReferenceHeight", (float)_displayObjectDescriptorWrapper.referenceHeight);
                            Matrix4x4 sphereToPlanPolynome = new Matrix4x4();
                            for (int j = 0; j < 4; j++) {
                                for (int i = 0; i < 4; i++) {
                                    int index = i + j * 4;
                                    sphereToPlanPolynome[j, i] = _displayObjectDescriptorWrapper.sphereToPlanPolynome[index];
                                }
                            }
                            _material.SetMatrix("_SphereToPlanPolynome", sphereToPlanPolynome);
                        }
                        else {
                            if (_displayObjectDescriptorWrapper.clearVRFishEyeType == ClearVRFishEyeTypes.EquiDistant) {
                                _material.EnableKeyword("FISH_EYE_EQUIDISTANT");
                                _material.DisableKeyword("FISH_EYE_EQUISOLID");
                            }
                            else if (_displayObjectDescriptorWrapper.clearVRFishEyeType == ClearVRFishEyeTypes.EquiSolid) {
                                _material.EnableKeyword("FISH_EYE_EQUISOLID");
                                _material.DisableKeyword("FISH_EYE_EQUIDISTANT");
                            }
                            _material.DisableKeyword("FISH_EYE_POLYNOME");
                            _material.DisableKeyword("ERP");
                            _material.SetFloat("_CircularRadiusInRad", _displayObjectDescriptorWrapper.circularRadiusInRad);
                            _material.SetFloat("_SensorDensity", _displayObjectDescriptorWrapper.sensorDensity);
                            _material.SetFloat("_FocalLength", _displayObjectDescriptorWrapper.focalLength);
                            _material.SetFloat("_ReferenceWidth", _displayObjectDescriptorWrapper.referenceWidth);
                            _material.SetFloat("_ReferenceHeight", _displayObjectDescriptorWrapper.referenceHeight);
                        }
                        _material.SetFloat("_StereoUOffset", isStereo ? 0.5F : 0.0F);
                        _material.SetFloat("_StereoUConstantOffset", isStereo ? 0.0F : 0.0F);
                        _material.SetFloat("_StereoVOffset", 0.0F);
                        _material.SetFloat("_StereoVConstantOffset", 0.0F);
                        _material.SetFloat("_MonoUFactor", isStereo ? 0.5F : 1.0F);
                        _material.SetFloat("_MonoVFactor", 1.0F);
                        break;
                    case ClearVRMeshTypes.ERP: // Only mono and stereo TB is supported
                        _material.EnableKeyword("ERP");
                        _material.DisableKeyword("FISH_EYE_POLYNOME");
                        _material.DisableKeyword("FISH_EYE_EQUISOLID");
                        _material.DisableKeyword("FISH_EYE_EQUIDISTANT");
                        _material.SetFloat("_StereoUOffset", 0F);
                        _material.SetFloat("_StereoVOffset", isStereo ? 0.5F : 0.0F);
                        _material.SetFloat("_MonoUFactor", 1.0F);
                        _material.SetFloat("_MonoVFactor", isStereo ? 0.5F : 1.0F);
                        _material.SetFloat("_LongitudeOffsetInRad", -(float)Math.PI / 2.0F);
                        break;
                    case ClearVRMeshTypes.ERP180: // Only mono and stereo SBS is supported
                        _material.EnableKeyword("ERP");
                        _material.DisableKeyword("FISH_EYE_POLYNOME");
                        _material.DisableKeyword("FISH_EYE_EQUISOLID");
                        _material.DisableKeyword("FISH_EYE_EQUIDISTANT");
                        _material.SetFloat("_StereoUOffset", isStereo ? 0.5F : 0.0F);
                        _material.SetFloat("_StereoVOffset", 0.0F);
                        _material.SetFloat("_MonoUFactor", isStereo ? 1.0F : 2.0F);
                        _material.SetFloat("_MonoVFactor", 1.0F);
                        _material.SetFloat("_LongitudeOffsetInRad", 0.0F);
                        break;
                    default:
                        ClearVRLogger.LOGE("Mesh type {0} not implemented. Cannot configure generic non-ClearVR omnidirectional parameters on shader.", clearVRMeshType);
                        break;
                }
            }
        }

        public bool SetMainColor(Color argNewColor) {
            if (_material != null) {
                if (argNewColor.a != -1 && argNewColor.r != -1 && argNewColor.g != -1 && argNewColor.b != -1) {
                    if (argNewColor.a != 1) {
                        EnableOrDisableTransparency(true);
                    } else {
                        EnableOrDisableTransparency(false);
                    }
                    _material.SetColor("_Color", argNewColor);

                    _shaderColor = argNewColor;
                    return true;
                }
            }
            return false;
        }

        private bool SetShaderColorSpace() {
            if (_material != null) {
                switch (_displayObjectDescriptorWrapper.textureType) {
                    case TextureTypes.RGBA:
                        _shaderColorSpaceYUVtoRGBTransformMatrix = Matrix4x4.identity;
                        _material.DisableKeyword("USE_NV12");
                        _material.DisableKeyword("USE_YUV420P");
                        break;
                    case TextureTypes.NV12:
                        _shaderColorSpaceYUVtoRGBTransformMatrix = GetColorSpaceYUVtoRGBTransformMatrix(_displayObjectDescriptorWrapper.colorSpace);
                        _material.EnableKeyword("USE_NV12");
                        _material.DisableKeyword("USE_YUV420P");
                        _material.SetMatrix("_ColorSpaceTransformMatrix", _shaderColorSpaceYUVtoRGBTransformMatrix);
                        break;
                    case TextureTypes.YUV420P:
                        _shaderColorSpaceYUVtoRGBTransformMatrix = GetColorSpaceYUVtoRGBTransformMatrix(_displayObjectDescriptorWrapper.colorSpace);
                        _material.DisableKeyword("USE_NV12");
                        _material.EnableKeyword("USE_YUV420P");
                        _material.SetMatrix("_ColorSpaceTransformMatrix", _shaderColorSpaceYUVtoRGBTransformMatrix);
                        break;
                    default:
                        ClearVRLogger.LOGE("Texture type {0} not implemented. Cannot configure shader color space.", _displayObjectDescriptorWrapper.textureType);
                        break;
                }
                if (_platformOptions != null && _platformOptions.overrideColorSpace != ColorSpaces.Gamma) {
                    _material.EnableKeyword("GAMMA_TO_LINEAR_CONVERSION");
                }
                else {
                    _material.DisableKeyword("GAMMA_TO_LINEAR_CONVERSION");
                }
                return true;
            }
            return false;
        }

        protected virtual bool HasLocalShaderParameterChange() {return false;}

        protected virtual Matrix4x4 GetTransformationMatrixFromMeshDescriptionStruct() {
            return _displayObjectDescriptorWrapper.textureTransformMatrix;
        }

        private bool SetTextureTransformMatrix() {
            if (_material != null) {
                Matrix4x4 textureTransformMatrix = GetTransformationMatrixFromMeshDescriptionStruct();
                _material.SetMatrix("_TextureTransformMatrix", textureTransformMatrix);
                return true;
            }
            return false;
        }

        public virtual void RecreateNativeTextures() {
            // Destroy the old texture if applicable
            DestroyNativeTextures();
            _lastTextureId = _displayObjectDescriptorWrapper.textureIDPlane0;
            if (_lastTextureId != IntPtr.Zero) {

                // Create external texture with correct dimensions
                // NOTE:
                // An "OPENGL NATIVE PLUG-IN ERROR: GL_INVALID_OPERATION: Operation illegal in current state" might be logged.
                // This bug is present since Unity 5.6.and has to do with a mismatch w.r.t. TextureFormat
                switch (_displayObjectDescriptorWrapper.textureType) {
                    case TextureTypes.RGBA:
                        if (_nativeTextures == null || _nativeTextures.Length != 1) {
                            _nativeTextures = new Texture[1] { null };
                        }
                        _nativeTextures[0] = Texture2D.CreateExternalTexture(_displayObjectDescriptorWrapper.frameWidth,
                                                                    _displayObjectDescriptorWrapper.frameHeight,
                                                                    TextureFormat.RGBA32,
                                                                    false,
                                                                    false,
                                                                    _lastTextureId);
                        BindNativeTextureToShader((Texture2D)_nativeTextures[0]);
                        break;
                    case TextureTypes.NV12:
                        if (_nativeTextures == null || _nativeTextures.Length != 2) {
                            _nativeTextures = new Texture[2] { null, null };
                        }
                        _nativeTextures[0] = Texture2D.CreateExternalTexture(_displayObjectDescriptorWrapper.frameWidth,
                                                                    _displayObjectDescriptorWrapper.frameHeight,
                                                                    TextureFormat.R8,
                                                                    false,
                                                                    false,
                                                                    _displayObjectDescriptorWrapper.textureIDPlane0);
                        _nativeTextures[1] = Texture2D.CreateExternalTexture(_displayObjectDescriptorWrapper.frameWidth / 2,
                                                                    _displayObjectDescriptorWrapper.frameHeight / 2,
                                                                    TextureFormat.RG16,
                                                                    false,
                                                                    false,
                                                                    _displayObjectDescriptorWrapper.textureIDPlane1);
                        BindNativeTextureToShader((Texture2D)_nativeTextures[0]);
                        BindNativeChromaTextureToShader((Texture2D)_nativeTextures[1]);
                        break;
                    case TextureTypes.YUV420P:
                        if (_nativeTextures == null || _nativeTextures.Length != 3) {
                            _nativeTextures = new Texture[3] { null, null, null };
                        }
                        _nativeTextures[0] = Texture2D.CreateExternalTexture(_displayObjectDescriptorWrapper.frameWidth,
                                                                    _displayObjectDescriptorWrapper.frameHeight,
                                                                    TextureFormat.R8,
                                                                    false,
                                                                    false,
                                                                    _displayObjectDescriptorWrapper.textureIDPlane0);
                        _nativeTextures[1] = Texture2D.CreateExternalTexture(_displayObjectDescriptorWrapper.frameWidth / 2,
                                                                    _displayObjectDescriptorWrapper.frameHeight / 2,
                                                                    TextureFormat.R8,
                                                                    false,
                                                                    false,
                                                                    _displayObjectDescriptorWrapper.textureIDPlane1);
                        _nativeTextures[2] = Texture2D.CreateExternalTexture(_displayObjectDescriptorWrapper.frameWidth / 2,
                                                                    _displayObjectDescriptorWrapper.frameHeight / 2,
                                                                    TextureFormat.R8,
                                                                    false,
                                                                    false,
                                                                    _displayObjectDescriptorWrapper.textureIDPlane2);
                        BindNativeTextureToShader((Texture2D)_nativeTextures[0]);
                        BindNativeChromaTextureToShader((Texture2D)_nativeTextures[1]);
                        BindNativeChromaTexture2ToShader((Texture2D)_nativeTextures[2]);
                        break;
                    default:
                        ClearVRLogger.LOGE("Unknown Texture Type {0}", _displayObjectDescriptorWrapper.textureType);
                        return;
                }
            } else {
                ClearVRLogger.LOGW("No textureID set (yet).");
            }
        }

        public virtual void UpdateNativeTextures(IntPtr argTextureId0, IntPtr argTextureId1, IntPtr argTextureId2) {
            if (_nativeTextures != null && _nativeTextures[0] != null) {
                _lastTextureId = argTextureId0;
                // Do not bind texture to Intptr.Zero. Direct3D11 does not seem to like that (tries to use it in the shader which obviously does not work).
                if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Direct3D11 && _lastTextureId == IntPtr.Zero) {
                    // Do nothing
                } else {
                    // #if UNITY_ANDROID && !UNITY_EDITOR
                    //             NativeRendererPluginAndroid.CVR_NRP_MakeSecureContextCurrent();
                    // #endif
                    ((Texture2D)_nativeTextures[0]).UpdateExternalTexture(argTextureId0);
                    if (_nativeTextures.Length >= 2 && _nativeTextures[1] != null) {
                        ((Texture2D)_nativeTextures[1]).UpdateExternalTexture(argTextureId1);
                    }
                    if (_nativeTextures.Length >= 3 && _nativeTextures[2] != null) {
                        ((Texture2D)_nativeTextures[2]).UpdateExternalTexture(argTextureId2);
                    }
                    // #if UNITY_ANDROID && !UNITY_EDITOR
                    //             NativeRendererPluginAndroid.CVR_NRP_RestoreContext();
                    // #endif
                }
            }
        }

        public virtual void DestroyNativeTextures() {
            if (_nativeTextures != null) {
                BindNativeTextureToShader(null);
                BindNativeChromaTextureToShader(null);
                BindNativeChromaTexture2ToShader(null);
                for (int i = 0; i < _nativeTextures.Length; i++) {
                    if (_nativeTextures[i] != null) {
                        UnityEngine.GameObject.DestroyImmediate(_nativeTextures[i]);
                    }
                }
                _lastTextureId = IntPtr.Zero;
                _nativeTextures = null;
            }
        }

        // Bind the texture to the shader
        public virtual void BindNativeTextureToShader(Texture argNativeTexture) {
            if (_material != null) {
                _material.mainTexture = argNativeTexture;
                _material.SetTexture("_MainTex", argNativeTexture);
            }
        }

        public virtual void BindNativeChromaTextureToShader(Texture argNativeChromaTexture) {
            if (_material != null) {
                _material.SetTexture("_ChromaTex", argNativeChromaTexture);
            }
        }
        public virtual void BindNativeChromaTexture2ToShader(Texture argNativeChromaTexture) {
            if (_material != null) {
                _material.SetTexture("_ChromaTex2", argNativeChromaTexture);
            }
        }

        // Enable or disable whether we actually see the mesh that rendering video
        public abstract void EnableOrDisableMeshRenderer(bool argIsEnabled);
        public abstract bool IsMeshRendererEnabled();

        public virtual Texture GetTexture() {
            return _nativeTextures[0];
        }

        public void UnbindAndReleaseNativeTextures() {
            UpdateNativeTextures(IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            DestroyNativeTextures();
        }
        
        // This API was deprecated in v9.0
        public System.Object /* FallbackLayout */ GetFallbackLayout() {
            return null;
        }

        private void ScheduleClearVRDisplayObjectEvent(ClearVRDisplayObjectEvent argClearVRDisplayObjectEvent) {
            clearVRDisplayObjectEvents.Invoke(this, argClearVRDisplayObjectEvent);
        }

        public override String ToString() {
            if(gameObject != null) {
                return String.Format("{0}. {1} Pos: {2}, or: {3}, scale: {4} (Cam: {5}, {6})\n", gameObject.name, _displayObjectDescriptorWrapper.ToString(), gameObject.transform.position.ToString(), gameObject.transform.eulerAngles.ToString(), gameObject.transform.localScale.ToString(), _platformOptions != null ? _platformOptions.trackingTransform.position.ToString() : "<unknown>", _platformOptions != null ? _platformOptions.trackingTransform.localRotation.eulerAngles.ToString() : "<unknown>");
            } else {
                // The DOM is referencing the DOC, but the underlying gameobject has already been destroyed.
                // Fix issue #5774. 
                return "not available"; 
            }
        }

        protected virtual void OnDestroy() {
            Stop();
        }

        void OnDisable() {
            Stop();
        }
        
        // Contract: it is safe to call this multiple times, and we can even call Stop() if Initialize() was never called.
        internal virtual void Stop() {
            if (!_isRegisteredAndInitialized) {
                _MaybeUnregisterAtLayoutManager();
                return;
            }
            _isRegisteredAndInitialized = false;
            UnbindAndReleaseNativeTextures();
            EnableOrDisableMeshRenderer(_originalActiveState);
            _MaybeUnregisterAtLayoutManager();
            _displayObjectDescriptorWrapper.Reset();
            _displayObjectID = -1;
        }
    }

        static class LocalUpdateStateMethods {
            public static String GetAsPrettyString(this ClearVRDisplayObjectControllerBase.LocalUpdateState argFlags) {
			StringBuilder sb = new StringBuilder();
			var values = Enum.GetValues(typeof(ClearVRDisplayObjectControllerBase.LocalUpdateState));
			foreach(var value in values) {
				if (argFlags.HasFlag((ClearVRDisplayObjectControllerBase.LocalUpdateState)value)) {
					sb.Append(value);
					sb.Append(",");
				}
			}
			return sb.ToString().TrimEnd(',');
		}

            public static bool HasNothingToDo(this ClearVRDisplayObjectControllerBase.LocalUpdateState flags) {
                return flags ==  ClearVRDisplayObjectControllerBase.LocalUpdateState.NothingToDo;
            }
            public static bool HasFlag(this ClearVRDisplayObjectControllerBase.LocalUpdateState flags, ClearVRDisplayObjectControllerBase.LocalUpdateState flagToTest) {
                if (flagToTest == ClearVRDisplayObjectControllerBase.LocalUpdateState.NothingToDo) {
                    return flags  ==  ClearVRDisplayObjectControllerBase.LocalUpdateState.NothingToDo;
                }
                return ((UInt32)flags & (UInt32) flagToTest) != 0;
            }

            public static ClearVRDisplayObjectControllerBase.LocalUpdateState SetFlags(ref this ClearVRDisplayObjectControllerBase.LocalUpdateState flags, ClearVRDisplayObjectControllerBase.LocalUpdateState flagsToSet) {
                flags |= flagsToSet;
                return flags;
            }
        }
}
