using UnityEngine;
using UnityEngine.UI;
using System;
namespace com.tiledmedia.clearvr {
    /// <summary>
    /// The ClearVRDisplayObjectControllerUnmanagedMesh is to be used to render a video on a custom mesh, NOT managed by the ClearVRPlayer.
    /// This control properly update the shader parameters, in sync with the video frame currently displayed on the object, but does not interact with the mesh itself besides attaching the proper Material (and associated Shader) to it.
    /// 
    /// You should subscribe to the [ClearVRDisplayObjectEvents](com.tiledmedia.clearvr.ClearVRPlayer.clearVRDisplayObjectEvents) to be notified of essential Display Object life-cycle events.
    /// 
    /// > [!WARNING]
    /// > This is considered an advanced Display Object. It is fundamentally incompatible when playing ClearVR content.
    /// </summary>
    [RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
    [Serializable]
    public class ClearVRDisplayObjectControllerUnmanagedMesh : ClearVRDisplayObjectControllerBase {
        
        [Tooltip("If set to true, VerticalFlip will vertically mirror the texture.")]
        public bool VerticalFlip = false;
        private bool _lastVerticalFlipState = false;
        [Tooltip("If set to true, HorizontalFlip will horizontally mirror the texture.")]
        public bool HorizontalFlip = false;

        protected MeshRenderer _meshRenderer;
        private const MeshTextureModes _meshTextureMode = MeshTextureModes.UnmanagedMesh;
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
        private int _editorGUI_ID_03 = (int) _meshTextureMode;
#pragma warning restore 0414


        override protected void InitializeRendererAndMaterial() {
            MeshFilter meshFilter = this.GetComponent<MeshFilter>();
            _meshRenderer = meshFilter.GetComponent<MeshRenderer>();
            _originalMaterial = _meshRenderer.material;
            if(_originalMaterial != null) {
                _originalShader = _originalMaterial.shader;
            }
            _material = _meshRenderer.material;
        }
       
        public override void EnableOrDisableMeshRenderer(bool argIsEnabled) {
            if(_meshRenderer != null) {
                _meshRenderer.enabled = argIsEnabled && !forceDisableMeshRendererAtAllTimes;
            }
        }

        public override bool IsMeshRendererEnabled() {
            if (_meshRenderer != null) {
                return _meshRenderer.enabled;
            } 
            return this.GetComponent<MeshFilter>().GetComponent<MeshRenderer>().enabled;
        }

        internal override void Stop() {
            base.Stop();
            MeshFilter meshFilter = this.GetComponent<MeshFilter>();
            if(_meshRenderer != null) {
                _meshRenderer.material = _originalMaterial;
                if(_meshRenderer.material != null) {
                    _meshRenderer.material.shader = _originalShader;
                }
            }
        }

        protected override Matrix4x4 GetTransformationMatrixFromMeshDescriptionStruct() {
            Matrix4x4 textureTransformMatrix = base.GetTransformationMatrixFromMeshDescriptionStruct();
            if (!VerticalFlip) {
                Matrix4x4 verticalMirror = new Matrix4x4();
                verticalMirror[0,0] =  1;
                verticalMirror[1,1] =  -1;
                verticalMirror[1,3] =  1;
                verticalMirror[2,2] =  1;
                verticalMirror[3,3] =  1;
                textureTransformMatrix =  textureTransformMatrix * verticalMirror;
            }
            if (!HorizontalFlip) {
                Matrix4x4 horizontalMirror = new Matrix4x4();
                horizontalMirror[0,0] =  -1;
                horizontalMirror[0,3] =  1;
                horizontalMirror[1,1] =  1;
                horizontalMirror[2,2] =  1;
                horizontalMirror[3,3] =  1;
                textureTransformMatrix = textureTransformMatrix * horizontalMirror;
            }
            return textureTransformMatrix;
        }

        public override void UpdateShader() {
            // We always attache the UV base shader. Projection overrides that requires a specific shader to be properly render when managed by Tiledmedia will now requires that the attached mesh as proper UVs + verticies to render the video properly
            Shader shader = clearVRShader;
            ReconfigureShader(shader);
        }

        // ForceRefreshShaderState When called, this method will force unity to update the shader state
        internal override void ForceRefreshShaderState() {
           _meshRenderer.material = _material;
        }

        protected override bool HasLocalShaderParameterChange() {
#if UNITY_EDITOR
            bool hasChanged = VerticalFlip != _lastVerticalFlipState;
            _lastVerticalFlipState = VerticalFlip;
            return hasChanged;
#else
            return false;
#endif
        }
    };

}
