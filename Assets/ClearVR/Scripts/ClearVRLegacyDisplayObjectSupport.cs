using UnityEngine;
using System;
namespace com.tiledmedia.clearvr {
    class ClearVRLegacyDisplayObjectSupport : MonoBehaviour {
        private ClearVRDisplayObjectControllerMesh _displayObjectController;
        internal InteractionModes recommendedInteractionMode = InteractionModes.Unknown;
        void Awake() {
            _displayObjectController = gameObject.GetComponent<ClearVRDisplayObjectControllerMesh>();
            if(_displayObjectController == null) {
                ClearVRLogger.LOGW("The ClearVRLegacyDisplayObjectSupport component can only be added to a GameObject that also has the ClearVRdisplayObjectController script attached to it.");
            }
        }

		/// <summary>
        /// GetDefaultDisplayObjectPose return the position and the orientation that the default display object should have. 
		/// * For omnidirectional content the default pose is at the origin with no rotation. 
		/// * For planar/rectilinear content the default pose should be at the totally unzoomed position with no rotation.
        /// </summary>
		internal Pose GetDefaultDisplayObjectPose(PlatformOptionsBase argPlatformOptionsBase) {
			Pose outputPose = new Pose();
            // Create a default Pose object
            outputPose.position = new Vector3(0, 0, 0);
            outputPose.rotation = new Quaternion(0 /* x */, 0 /* y */, 0 /* z */, 1 /* w */);

			if(_displayObjectController != null && argPlatformOptionsBase.renderCamera != null) {
				switch(_displayObjectController.clearVRMeshType) {
					case ClearVRMeshTypes.Cubemap:
					case ClearVRMeshTypes.FishEye:
					case ClearVRMeshTypes.ERP:
					case ClearVRMeshTypes.Cubemap180:
					case ClearVRMeshTypes.ERP180:
						//Omnidirectional content, return default pose
						break;
					case ClearVRMeshTypes.Planar:
					case ClearVRMeshTypes.Rectilinear:
						//Planar content:
						float halfHorizontalSize =  _displayObjectController.mesh.bounds.extents.x;
						float halfVerticalSize = _displayObjectController.mesh.bounds.extents.y;
						float cameraVFoV = argPlatformOptionsBase.renderCamera.fieldOfView * Mathf.Deg2Rad;
						float cameraHFoV = 2 * Mathf.Atan(Mathf.Tan(cameraVFoV/2) *  argPlatformOptionsBase.renderCamera.aspect);
						float zH = halfHorizontalSize / Mathf.Tan(cameraHFoV/2f);
						float zV = halfVerticalSize / Mathf.Tan(cameraVFoV/2f);
						outputPose.position = new Vector3(0, 0, Mathf.Max(zH, zV));
						break;
					default:
						throw new Exception(String.Format("GetDefaultDisplayObjectPose call with an unknown Mesh Type: {0}", _displayObjectController.clearVRMeshType));
				}
			}  
			return outputPose;
		}

		/// <summary>
        /// GetDefaultViewportPose return the default Pose of the camera, which is at the center of the world without rotation.
        /// </summary>
		private Pose GetDefaultViewportPose() {
			Pose outputPose = new Pose();
			outputPose.position = new Vector3(0, 0, 0);
			outputPose.rotation = new Quaternion(0, 0, 0, 1);
			return outputPose;
		}

		internal void ResetViewportAndDisplayObjectToDefaultPoses(PlatformOptionsBase argPlatformOptionsBase) {
#pragma warning disable 0618 // Silence deprecated API usage warning
			if (argPlatformOptionsBase.cameraAndContentPlacementMode == CameraAndContentPlacementModes.Disabled) {
				return;
			}
			Pose displayObjectPose = GetDefaultDisplayObjectPose(argPlatformOptionsBase);
			if (_displayObjectController != null) {
				// Reset DisplayObject
				_displayObjectController.transform.localPosition = displayObjectPose.position;
				_displayObjectController.transform.localRotation = displayObjectPose.rotation;
				// Reset Viewport
				if (argPlatformOptionsBase.cameraAndContentPlacementMode == CameraAndContentPlacementModes.MoveDisplayObjectResetCamera) {
					Pose viewportDefaultPose = GetDefaultViewportPose();
					if (argPlatformOptionsBase.renderCamera != null) {
						argPlatformOptionsBase.renderCamera.transform.SetPositionAndRotation(viewportDefaultPose.position, viewportDefaultPose.rotation);
					} else {
						UnityEngine.Debug.LogWarning("[ClearVR] ResetViewportAndDisplayObjectToDefaultPoses() cannot complete. No platformOptions.renderCamera specified.");
					}
				}				
			} else {
				UnityEngine.Debug.LogWarning("[ClearVR] ResetViewportAndDisplayObjectToDefaultPoses() cannot complete. No ClearVRDisplayObjectControllerBase (DisplayObject) active. Cannot reset viewport.");
			}
#pragma warning restore 0618 // Silence deprecated API usage warning
		}

		internal void GetRecommendedZoomRange(out float argMin, out float argMax, PlatformOptionsBase argPlatformOptionsBase) {
			Pose defaultDisplayObjPose = GetDefaultDisplayObjectPose(argPlatformOptionsBase);
			// TODO: 0.10 (10 percent) is a hardcoded constant that can be calculated once we have added LOD.
			argMin = 0.10f * Mathf.Abs(defaultDisplayObjPose.position.z);
			argMax = Mathf.Abs(defaultDisplayObjPose.position.z);
		}
    }
}