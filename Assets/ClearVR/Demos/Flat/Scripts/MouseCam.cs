using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace com.tiledmedia.clearvr.demos {
    public class MouseCam : InteractionScript {
        private Vector3 _firstPoint = new Vector3(0, 0, 0);
        private Vector3 _secondPoint = new Vector3(0, 0, 0);
        private float _xAngle = 0.0f; //angle for axes x for rotation
        private float _yAngle = 0.0f;
        private float _xAngleTemp = 0.0f; //temp variable for angle
        private float _yAngleTemp = 0.0f;

        private Vector3 _lastMousePos;

        protected override void MoveViewport() {
            if (!IsInputOverGameObject(0) && flatPlayerMenu != null && !flatPlayerMenu.isDraggingBar && mainCamera != null) {
                //Click began, save position
                if (Input.GetMouseButtonDown(0)) {
                    _firstPoint = Input.mousePosition;
                    _xAngleTemp = _xAngle;
                    _yAngleTemp = _yAngle;
                }
                switch (_interactionMode) {
                    case InteractionModes.OmniDirectional: {
                            //Move finger by screen
                            if (Input.GetMouseButton(0)) {
                                _secondPoint = Input.mousePosition;
                                //Mainly, about rotate camera. For example, for Screen.width rotate on 180 degree
                                _yAngle = _yAngleTemp + (_secondPoint.y - _firstPoint.y) * 90.0f / Screen.height;

                                if (_yAngle < 0)
                                    _yAngle += 360;
                                if (_yAngle > 360)
                                    _yAngle -= 360;
                                if (_yAngle < 270 && _yAngle >= 180)
                                    _yAngle = 270;
                                if (_yAngle > 90 && _yAngle < 180)
                                    _yAngle = 90;

                                if (_yAngle > 90 && _yAngle < 270)
                                    _xAngle = _xAngleTemp + (_secondPoint.x - _firstPoint.x) * 180.0f / Screen.width;
                                else
                                    _xAngle = _xAngleTemp - (_secondPoint.x - _firstPoint.x) * 180.0f / Screen.width;

                                if (_xAngle < 0)
                                    _xAngle += 360;

                                if (_xAngle > 360)
                                    _xAngle -= 360;

                                transform.rotation = Quaternion.Euler(_yAngle, _xAngle, 0.0f);
                            }
                            break;
                        }
                    case InteractionModes.Planar: {
                            if (Input.GetMouseButton(0)) { // translate
                                Vector3 current_position = displayObjectController.transform.position;

                                Vector3 mousePosition = Input.mousePosition;
                                Vector2 prevPos = mousePosition - _lastMousePos;

                                // Get the world space point that is being dragged
                                Vector3 pointOne = mainCamera.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, current_position.z));
                                Vector3 pointTwo = mainCamera.ScreenToWorldPoint(new Vector3(_lastMousePos.x, _lastMousePos.y, current_position.z));

                                // Calculate the position offest
                                Vector3 meshPosDelta = pointTwo - pointOne;

                                // Move the mesh
                                current_position.x -= meshPosDelta.x;
                                current_position.y -= meshPosDelta.y;

                                displayObjectController.transform.position = current_position;
                                _lastMousePos = mousePosition;
                            } else if (Input.mouseScrollDelta.y != 0) { // zoom 
                                Vector3 current_position = displayObjectController.transform.position;

                                Vector2 center = Input.mousePosition;
                                Vector3 screenSpaceCenter = new Vector3(center.x, center.y, Mathf.Abs(current_position.z));
                                Vector3 worldSpaceCenter = mainCamera.ScreenToWorldPoint(screenSpaceCenter);

                                // Find the difference in the distances between each frame.
                                float deltaMagnitudeDiff = -Input.mouseScrollDelta.y;

                                float fieldOfView = mainCamera.fieldOfView;

                                float halfViewportSize = Mathf.Tan((Mathf.Deg2Rad * fieldOfView) / 2) * current_position.z;

                                // Calculate the zoom amount
                                float deltaZTranslation = current_position.z - (current_position.z * (halfViewportSize - deltaMagnitudeDiff)) / halfViewportSize;

                                // Calculate zoom direction
                                Vector3 zoomDir = new Vector3(-worldSpaceCenter.x, -worldSpaceCenter.y, 0);

                                // Calculate the translation need to zoom over the pinched portion of the screen
                                Vector3 zoomTranslation = zoomDir * Mathf.Min(deltaMagnitudeDiff, zoomDir.magnitude) / zoomDir.magnitude;
                                // Move the mesh forward/backward to zoom
                                current_position.z += deltaZTranslation * zoomSpeed;

                                Vector3 newWorldSpaceCenter = mainCamera.ScreenToWorldPoint(new Vector3(center.x, center.y, current_position.z));

                                if (checkViewportBounds != null && current_position.z >= checkViewportBounds.GetMinZoom()) {
                                    // Move the mesh to center the zoom
                                    current_position.x += newWorldSpaceCenter.x - worldSpaceCenter.x;
                                    current_position.y += newWorldSpaceCenter.y - worldSpaceCenter.y;

                                    displayObjectController.transform.position = current_position;
                                }
                            }
                            break;
                        }
                    default:
                        break;
                }
                if (ShouldShowMenu()) {
                    flatPlayerMenu.ShowVideoControllersPanel();
                }
            }
        }

        /// <summary>
        /// Determines if the last click was valid to show the video player controllers menu
        /// </summary>
        /// <returns></returns>
        protected override bool ShouldShowMenu() {
            if (Input.GetMouseButtonUp(0)) {
                return !IsInputOverGameObject(-1) && (Vector3.Distance(Input.mousePosition, _firstPoint) < dragThreshold);
            }
            return false;
        }

        /// <summary>
        /// Checks if the mouse input doesn't hit the UI.
        /// </summary>
        protected override bool IsInputOverGameObject(int argPointerId) {
            return EventSystem.current.IsPointerOverGameObject(argPointerId);
        }

        public override void Reset() {
            base.Reset();
            _xAngle = 0;
            _yAngle = 0;
        }
    }
}