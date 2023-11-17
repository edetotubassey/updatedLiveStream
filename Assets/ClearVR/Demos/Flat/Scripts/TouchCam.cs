using com.tiledmedia.clearvr;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// This script demonstrates how to handle touch gestures to interact with the video. Feel free to duplicate it and add your own logic to it.
/// Attach this script to a ClearVRDisplayObjectController object after it has been created by your ClearVRPlayer object.
/// One MUST call Initialize(ClearVRPlayer) after creation for it to function properly. 
/// One MUST also call Initialize(ClearVRPlayer) after the ClearVRDisplayObjectController has changed. It is suggested to do this in ClearVREventTypes.FirstFrameRendered.
/// </summary>
namespace com.tiledmedia.clearvr.demos {
    public class TouchCam : InteractionScript {
        private Vector3 _firstPoint = new Vector3(0, 0, 0);
        private Vector3 _secondPoint = new Vector3(0, 0, 0);
        private float _xAngle = 0.0f; //angle for axes x for rotation
        private float _yAngle = 0.0f;
        private float _xAngleTemp = 0.0f; //temp variable for angle
        private float _yAngleTemp = 0.0f;
        private bool _touchBeganOnUI;
        protected override void MoveViewport() {
            //Check count touches
            if (Input.touchCount > 0 && flatPlayerMenu != null && mainCamera != null) {
                if (!IsInputOverGameObject(Input.GetTouch(0).fingerId) && !flatPlayerMenu.isDraggingBar) {
                    //Touch began, save position
                    if (Input.GetTouch(0).phase == TouchPhase.Began) {
                        _firstPoint = Input.GetTouch(0).position;
                        _xAngleTemp = _xAngle;
                        _yAngleTemp = _yAngle;
                    }
                    switch (_interactionMode) {
                        case InteractionModes.OmniDirectional: {
                                //Move finger by screen
                                if (Input.GetTouch(0).phase == TouchPhase.Moved) {
                                    _secondPoint = Input.GetTouch(0).position;
                                    //Mainly, about rotate camera. For example, for Screen.width rotate on 180 degree
                                    _yAngle = _yAngleTemp + ((_secondPoint.y - _firstPoint.y) * 90.0f / Screen.height);

                                    if (_yAngle < 0)
                                        _yAngle += 360;
                                    if (_yAngle > 360)
                                        _yAngle -= 360;
                                    if (_yAngle < 270 && _yAngle >= 180)
                                        _yAngle = 270;
                                    if (_yAngle > 90 && _yAngle < 180)
                                        _yAngle = 90;

                                    if (_yAngle > 90 && _yAngle < 270)
                                        _xAngle = _xAngleTemp + ((_secondPoint.x - _firstPoint.x) * 180.0f / Screen.width);
                                    else
                                        _xAngle = _xAngleTemp - ((_secondPoint.x - _firstPoint.x) * 180.0f / Screen.width);

                                    if (_xAngle < 0)
                                        _xAngle += 360;

                                    if (_xAngle > 360)
                                        _xAngle -= 360;

                                    transform.rotation = Quaternion.Euler(_yAngle, _xAngle, 0.0f);
                                }
                                break;
                            }
                        case InteractionModes.Planar: {
                                if (Input.touchCount == 1) { // translate
                                    Vector3 current_position = displayObjectController.transform.position;

                                    Touch touch = Input.GetTouch(0);
                                    if (touch.phase == TouchPhase.Began) {
                                        _firstPoint = touch.position;
                                    }

                                    Vector2 prevPos = touch.position - touch.deltaPosition;

                                    // Get the world space point that is being dragged
                                    Vector3 pointOne = mainCamera.ScreenToWorldPoint(new Vector3(touch.position.x, touch.position.y, current_position.z));
                                    Vector3 pointTwo = mainCamera.ScreenToWorldPoint(new Vector3(prevPos.x, prevPos.y, current_position.z));

                                    // Calculate the position offest
                                    Vector3 meshPosDelta = pointTwo - pointOne;

                                    // Move the mesh
                                    current_position.x -= meshPosDelta.x;
                                    current_position.y -= meshPosDelta.y;

                                    displayObjectController.transform.position = current_position;
                                } else if (Input.touchCount == 2) { // zoom
                                    Vector3 current_position = displayObjectController.transform.position;

                                    // Store both touches.
                                    Touch touchZero = Input.GetTouch(0);
                                    Touch touchOne = Input.GetTouch(1);

                                    //We do nothing if the fingers are not moving enough
                                    if (Mathf.Abs(touchZero.deltaPosition.magnitude) < 5f && Mathf.Abs(touchOne.deltaPosition.magnitude) < 5f) {
                                        return;
                                    }

                                    // Find the position in the previous frame of each touch.
                                    Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
                                    Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

                                    Vector3 touchZeroPrevWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(touchZeroPrevPos.x, touchZeroPrevPos.y, current_position.z));
                                    Vector3 touchOnePrevWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(touchOnePrevPos.x, touchOnePrevPos.y, current_position.z));

                                    // Find the position in the current frame of each touch.
                                    Vector3 touchZeroWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(touchZero.position.x, touchZero.position.y, current_position.z));
                                    Vector3 touchOneWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(touchOne.position.x, touchOne.position.y, current_position.z));

                                    // Find the magnitude of the vector (the distance) between the touches in each frame.
                                    float prevTouchDeltaMag = (touchZeroPrevWorldPos - touchOnePrevWorldPos).magnitude;
                                    float currentTouchDeltaMag = (touchZeroWorldPos - touchOneWorldPos).magnitude;

                                    // Find the difference in the distances between each frame.
                                    float deltaMagnitudeDiff = prevTouchDeltaMag - currentTouchDeltaMag;

                                    float fieldOfView = mainCamera.fieldOfView;

                                    float halfViewportSize = Mathf.Tan((Mathf.Deg2Rad * fieldOfView) / 2) * current_position.z;

                                    // Calculate the zoom amount
                                    float deltaZTranslation = current_position.z - ((current_position.z * (halfViewportSize - deltaMagnitudeDiff)) / halfViewportSize);

                                    // Calculate the world space center between the two pinching fingers
                                    Vector2 center = (touchOne.position + touchZero.position) / 2;
                                    Vector3 screenSpaceCenter = new Vector3(center.x, center.y, current_position.z);
                                    Vector3 worldSpaceCenter = mainCamera.ScreenToWorldPoint(screenSpaceCenter);

                                    // Calculate zoom direction
                                    Vector3 zoomDir = new Vector3(-worldSpaceCenter.x, -worldSpaceCenter.y, 0);

                                    // Calculate the translation needed to zoom over the pinched portion of the screen
                                    Vector3 zoomTranslation = zoomDir * Mathf.Min(deltaMagnitudeDiff, zoomDir.magnitude) / zoomDir.magnitude;

                                    // Move the mesh forward/backward to zoom
                                    current_position.z += deltaZTranslation * zoomSpeed;

                                    if (checkViewportBounds != null && current_position.z >= checkViewportBounds.GetMinZoom()) {
                                        // Move the mesh to center the zoom
                                        current_position.x -= zoomTranslation.x;
                                        current_position.y -= zoomTranslation.y;

                                        displayObjectController.transform.position = current_position;
                                    }
                                }
                                break;
                            }
                        default:
                            break;
                    }
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
            if (Input.GetTouch(0).phase == TouchPhase.Began) {
                _touchBeganOnUI = !IsInputOverGameObject(Input.GetTouch(0).fingerId);
            }
            if (Input.GetTouch(0).phase == TouchPhase.Ended) {
                _touchBeganOnUI = (_touchBeganOnUI && Vector3.Distance(Input.GetTouch(0).position, _firstPoint) <= dragThreshold);
                return _touchBeganOnUI;
            }
            return false;
        }

        /// <summary>
        /// Checks if the touch input doesn't hit the UI.
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