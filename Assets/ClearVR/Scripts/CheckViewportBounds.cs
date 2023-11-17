using UnityEngine;
using System.Collections;

namespace com.tiledmedia.clearvr {
    /// <summary>
    /// Component used to verify if the camera is within the expected bounds. If not the camera will move to the closest allowed pose.
    /// This component has to be reloaded each time a new content start to be displayed. Calling initialize with the current ClearVRPlayer is enough to reload the component.
    /// </summary>
    public class CheckViewportBounds : MonoBehaviour {
        private ClearVRPlayer _clearVRPlayer = null;
        private Camera _camera = null;
        private float _zoomMin = 0;
        private float _zoomMax = 0;
        private ClearVRDisplayObjectControllerMesh _displayObjectController = null;

        void Awake() {
            _displayObjectController = GetComponent<ClearVRDisplayObjectControllerMesh>();
            if(_displayObjectController == null) {
                ClearVRLogger.LOGW("The CheckViewportBounds script can only be attached to a gameobject that also has the ClearVRDisplayObjectControllerBase script attached to it.");
            }
        }
        
        /// <summary>
        /// Each time the content has changed (e.g. after a SwitchContent), one MUST call Initialize() again. A suggested place for calling this method is ClearVREventTypes.FirstFrameRendered.
        /// </summary>
        /// <param name="argClearVRPlayer">The ClearVRPlayer object that triggered the creation of the ClearVRDisplayObjectControllerBase.</param>
        public void Initialize(ClearVRPlayer argClearVRPlayer) {
            if(argClearVRPlayer == null) {
                return;
            }
            _clearVRPlayer = argClearVRPlayer;
            _camera = _clearVRPlayer.mediaPlayer.GetPlatformOptions().renderCamera;
            _clearVRPlayer.mediaPlayer.GetRecommendedZoomRange(out _zoomMin, out _zoomMax);
            ClearVRLayoutManager layoutManager = GameObject.FindObjectOfType<ClearVRLayoutManager>();
            if(layoutManager != null) {
                _displayObjectController = (ClearVRDisplayObjectControllerMesh) layoutManager.mainDisplayObjectController;
            }
        }

        void Update() {
            if (_clearVRPlayer != null && _displayObjectController != null) {
                if(_displayObjectController.clearVRMeshType.GetIsPlanar()) {
                    //Planar content: forbid rotation and constrain position within the recommended zoom range and within the video bounds
                    // First we check the zoom level

                    Vector3 previousPosition = _displayObjectController.transform.position;
                    previousPosition.z = Mathf.Clamp(previousPosition.z, _zoomMin, _zoomMax);

                    //Check the x and y bounds:
                    float cameraVFoV = _camera.fieldOfView * Mathf.Deg2Rad;
                    float cameraHFoV = 2 * Mathf.Atan(Mathf.Tan(cameraVFoV/2) * _camera.aspect);

                    float minX = _displayObjectController.mesh.bounds.min.x + previousPosition.z * Mathf.Tan(cameraHFoV/2);
                    float maxX = _displayObjectController.mesh.bounds.max.x - previousPosition.z * Mathf.Tan(cameraHFoV/2);

                    if (minX < maxX) {
                        previousPosition.x = Mathf.Clamp(previousPosition.x, minX, maxX);
                    } else {
                        previousPosition.x = 0;
                    }

                    float minY = _displayObjectController.mesh.bounds.min.y + previousPosition.z * Mathf.Tan(cameraVFoV/2);
                    float maxY = _displayObjectController.mesh.bounds.max.y - previousPosition.z * Mathf.Tan(cameraVFoV/2);
                    if (minY < maxY) {
                        previousPosition.y = Mathf.Clamp(previousPosition.y, minY, maxY);
                    } else {
                        previousPosition.y = 0;
                    }
                    _displayObjectController.transform.position = previousPosition;
                    _displayObjectController.transform.rotation = new Quaternion(0, 0, 0, 1); //we force having no rotation (the rotation should never change)
                }
            }
        }
        /// <summary>
        /// Returns the min level of zoom allowed for planar content.
        /// </summary>
        /// <returns>The min z value the mesh can be placed at.</returns>
        public float GetMinZoom(){
            return this._zoomMin;
        }

        /// <summary>
        /// Returna the max level of zoom that is allowed for planar content.
        /// </summary>
        /// <returns>The max z value the mesh can be placed at.</returns>
        public float GetMaxZoom(){
            return this._zoomMax;
        }
    }
}