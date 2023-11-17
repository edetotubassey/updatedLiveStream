using UnityEngine;
using UnityEngine.UI;
using System;
namespace com.tiledmedia.clearvr {
    /// <summary>
    /// Holds the ClearVRDisplayObjectControllerBase component attached to the gameobject which render the Sprite
    /// </summary>
    [RequireComponent(typeof(ClearVRImage), typeof(CanvasRenderer))]
    [Serializable]
    public class ClearVRDisplayObjectControllerSprite : ClearVRDisplayObjectControllerBase {
        
        private const MeshTextureModes _meshTextureMode = MeshTextureModes.Sprite;
        public override MeshTextureModes meshTextureMode { 
            get {
                return _meshTextureMode;
            } 
            set { }
        }

        public void UpdateMaterial(Material m) { 
            if(m != null) { // #5768
                _material = m;
            }
        }

        // This field is used in ClearVRLayoutParametersPropertyDrawer. Do not remove.
#pragma warning disable 0414
        [HideInInspector] 
        [SerializeField] 
        private int _editorGUI_ID_02 = (int) _meshTextureMode;
#pragma warning restore 0414

        private Material  _originalImageMaterial = null;
        private Sprite _originalImageSprite = null;
        private Image _image = null;
        private CanvasRenderer _canvasRenderer;
  
        override protected void InitializeRendererAndMaterial() {
                ClearVRDisplayObjectControllerBase.LoadShaders();
                _image = this.GetComponent<ClearVRImage>();
                if (_image == null) {
                    ClearVRLogger.LOGE("Could find the ClearVRImage component associated to the Sprite DisplayObject controller");
                }
                _canvasRenderer = this.GetComponent<CanvasRenderer>();
                if (_canvasRenderer == null) {
                    ClearVRLogger.LOGE("Could find the CanvasRenderer component associated to the Sprite DisplayObject controller");
                }
                _originalImageMaterial =  _image.material;
                _originalImageSprite = _image.sprite;
                _image.sprite = null;
                _material = new Material(clearVRUIShader);
                _material.EnableKeyword("USE_UI");
                ForceRefreshShaderState();
        }

        public override void EnableOrDisableMeshRenderer(bool argIsEnabled) {
            if (_image != null) {
                _image.enabled = argIsEnabled && !forceDisableMeshRendererAtAllTimes;
            }
        }

        public override bool IsMeshRendererEnabled() {
            if (_image != null) {
                return _image.enabled;
            }
            return this.GetComponent<ClearVRImage>().enabled;
        }

        internal override void Stop() {
            base.Stop();
             if(_image != null) {
                _image.material = _originalImageMaterial;
                _image.sprite = _originalImageSprite;
            }
            ForceRefreshShaderState();
            _canvasRenderer = null;
        } 

        public override void UpdateShader() {
               // We might have to attach a different shader.
            Shader shader;
            switch (clearVRMeshType) {
                case ClearVRMeshTypes.Cubemap: {
                        shader = clearVRUIShader;
                        break;
                    }
                case ClearVRMeshTypes.Planar: {
                        shader = clearVRUIShader;
                        break;
                    }
                case ClearVRMeshTypes.Cubemap180: {
                        shader = clearVRUIShader;
                        break;
                    }
                case ClearVRMeshTypes.ERP: {
                        shader = genericNonClearVROmniShader;
                        throw new Exception("[ClearVR] Cannot render an ERP feed on a UI element. Cannot continue.");
                    }
                case ClearVRMeshTypes.ERP180: {
                        shader = genericNonClearVROmniShader;
                        throw new Exception("[ClearVR] Cannot render an ERP feed on a UI element. Cannot continue.");
                    }
                case ClearVRMeshTypes.Rectilinear: {
                        shader = clearVRUIShader;
                        break;
                    }
                case ClearVRMeshTypes.FishEye: {
                        switch (_displayObjectDescriptorWrapper.clearVRFishEyeType) {
                            case ClearVRFishEyeTypes.NotSet:
                                throw new Exception("[ClearVR] Fish Eye parameters were not set but projection type is Fish Eye. Cannot continue.");
                            case ClearVRFishEyeTypes.EquiSolid:
                            case ClearVRFishEyeTypes.EquiDistant:
                            case ClearVRFishEyeTypes.Polynomial:
                                throw new Exception("[ClearVR] Cannot render a FishEye feed on a UI element. Cannot continue.");
                            default:
                                {
                                    throw new Exception(String.Format("[ClearVR] FishEyeType {0} not implemented.", _displayObjectDescriptorWrapper.clearVRFishEyeType));
                                }
                        }
                    }
                case ClearVRMeshTypes.Unknown:
                default: {
                        throw new Exception(String.Format("[ClearVR] MeshType {0} not implemented.", clearVRMeshType));
                    }
            }
            UpdateShaderAndMaySetDirty(shader, true);
        }

        public void UpdateShaderAndMaySetDirty(Shader shader, bool shallSetDirty) {
            if (Application.isPlaying && isRegisteredAndInitialized) {
                ReconfigureShader(shader);
                if(shallSetDirty) {
                    ForceRefreshShaderState();
                }
                if (_canvasRenderer != null && _canvasRenderer.materialCount > 0) {
                    _canvasRenderer.SetMaterial(_material, 0);
                }
            }
        }

        // ForceRefreshShaderState When called, this method will force unity to update the shader state
        internal override void ForceRefreshShaderState() {
            if(_image != null) {
                _image.SetAllDirty();
                if ( _material != null) {
                    _material = new Material(_material);
                    _material.EnableKeyword("USE_UI");
                }
                _image.material = _material;
                if (_canvasRenderer != null && _canvasRenderer.materialCount > 0) {
                    _canvasRenderer.SetMaterial(_material, 0);
                }
                SetMainColor(_image.color);
            }
        }
    };

}
