using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace com.tiledmedia.clearvr.demos {
    public class VRPlayerMenu : UserInterfaceBase {
        // List of references to objects in the scene that are specific to functionality that is not shared between VR and Flat
        private bool shouldVRMenuFollow; // weather the vr menu should follow the player gaze or not
        private bool isVRMenuPinned; // whether the vr menus is pinned in a position or not
        private float previousY;
        //private RectTransform rtUI;
        public float spawnDistanceUI = 4.0f;
        public bool shouldDrag { get; set; }
        public GameObject secondaryCanvas; // the secondary canvas that is used to display the buffer icon
        public GameObject playerMenuGameObject; // the player menu game object
        public Canvas playerMenuMainCanvas; // the player menu main canvas

        protected override void OnEnable() {
            base.OnEnable();
            if (isVRMenuPinned) {
                PinMenu();
            } else {
                if (VRInputModuleBase.Instance != null) {
                    // Spawn the menu at a certain position in the scene depending on where the player aims with the controller
                    Vector3 laserStartPoint = VRInputModuleBase.Instance.GetPrimaryControllerPosition() + new Vector3(0, 0.25f, 0);
                    Vector3 laserDirection = Quaternion.Euler(VRInputModuleBase.Instance.GetPrimaryControllerRotation()) * Vector3.forward;
                    Vector3 spawnLoc = laserStartPoint + (laserDirection * spawnDistanceUI);
                    playerMenuGameObject.transform.position = spawnLoc;
                    StartCoroutine(UpdateUI()); // restart the UI update coroutine
                }
            }
            shouldVRMenuFollow = false;
            // Rotate the interface to the camera
            transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);

            if (clearVRPlayer != null && clearVRPlayer.mediaPlayer != null) {
                clearVRPlayer.mediaPlayer.SetRenderModeOnAllDisplayObjectsConditionally(RenderModes.ForcedMonoscopic, RenderModes.Stereoscopic);
            }
        }

        protected override void OnDisable() {
            base.OnDisable();
            shouldVRMenuFollow = false;
            StopCoroutine(UpdateUI()); // Make sure to stop the UI update coroutine to save performance
            if (clearVRPlayer != null && clearVRPlayer.mediaPlayer != null) {
                clearVRPlayer.mediaPlayer.SetRenderModeOnAllDisplayObjectsConditionally(RenderModes.Stereoscopic, RenderModes.ForcedMonoscopic);
            }
        }

        protected override void Start() {
            base.Start();
            ToggleVRPlayerMenu(false);
        }

        // Update is called once per frame
        protected override void Update() {
            base.Update();
            // Used to smoothly make the VR user interface follow the player.
            if (shouldVRMenuFollow) {
                FollowPlayer(playerMenuGameObject);
            }
            // Smoothly move the menu to the new point given.
            if (shouldDrag) {
                Vector3 prevPos = transform.position;
                DragInterface(prevPos);
            }
        }

        /// <summary>
        /// Toggle the VR Player menu on and off
        /// </summary>
        public void ToggleVRPlayerMenu() {
            if (VRInputModuleBase.Instance.pointer.cursor.GetCurrentHitTargetObject() != gameObject && VRInputModuleBase.Instance && VRInputModuleBase.Instance.pointer) {
                playerMenuGameObject.SetActive(!playerMenuGameObject.activeInHierarchy);
                VRInputModuleBase.Instance.pointer.Show = playerMenuGameObject.activeInHierarchy;
            }
        }

        /// <summary>
        /// Toggle the VR Player menu on and off
        /// </summary>
        public void ToggleVRPlayerMenu(bool argShouldEnable) {
            if (VRInputModuleBase.Instance.pointer.cursor.GetCurrentHitTargetObject() != playerMenuGameObject && VRInputModuleBase.Instance != null && VRInputModuleBase.Instance.pointer) {
                playerMenuGameObject.SetActive(!argShouldEnable);
                VRInputModuleBase.Instance.pointer.Show = argShouldEnable;
            }
        }

        /// <summary>
        /// Function to make the menu rotate around the player character.
        /// </summary>
        void FollowPlayer(GameObject targetObject) {
            // Calculates the amount of rotation needed for the rotate function
            float currentY = Camera.main.transform.rotation.eulerAngles.y;
            float rotationAmount = currentY - previousY;
            RotateAroundCam(targetObject, Camera.main.transform.position, Vector3.up, rotationAmount);
            previousY = currentY;
        }

        /// <summary>
        /// Function to rotate an object around another object in the scene.
        /// </summary>
        /// <param name="center"></param>
        /// <param name="axis"></param>
        /// <param name="angle"></param>
        private void RotateAroundCam(GameObject targetObject, Vector3 center, Vector3 axis, float angle) {
            Vector3 pos = targetObject.transform.position; // get current position
            Quaternion rot = Quaternion.AngleAxis(angle, axis); // get the desired rotation
            Vector3 dir = pos - center; // find current direction relative to center
            dir = rot * dir; // rotate the direction

            targetObject.transform.position = center + dir; // define new position

            // rotate object to keep looking at the center
            Quaternion myRot = targetObject.transform.rotation;
            targetObject.transform.rotation *= Quaternion.Inverse(myRot) * rot * myRot;
        }

        /// <summary>
        /// Function to lock the menu in place.
        /// </summary>
        public void PinMenu() {
            Vector3 middlePoint = (Vector3.forward + Vector3.down).normalized * Camera.main.transform.forward.magnitude;
            Vector3 middleMiddlePoint = (Vector3.forward + middlePoint).normalized * Camera.main.transform.forward.magnitude;
            Vector3 spawnLoc = Camera.main.transform.position + (middleMiddlePoint.normalized * spawnDistanceUI);
            transform.position = spawnLoc;
            float rotationAmount = Camera.main.transform.rotation.eulerAngles.y;
            RotateAroundCam(playerMenuGameObject, Camera.main.transform.position, Vector3.up, rotationAmount);
            transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);
        }

        /// <summary>
        /// Function to make the VR menu move by dragging it with the controller
        /// </summary>
        /// <param name="prevPos"></param>
        private void DragInterface(Vector3 prevPos) {
            if (VRInputModuleBase.Instance != null && VRInputModuleBase.Instance.pointer != null) {
                Vector3 laserStartPoint = VRInputModuleBase.Instance.GetPrimaryControllerPosition();
                Vector3 laserDirection = VRInputModuleBase.Instance.pointer.transform.forward;
                Vector3 newLoc = laserStartPoint + (laserDirection.normalized * spawnDistanceUI);
                transform.position += (newLoc - prevPos) * Time.deltaTime * 10;
                transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);
            }
        }

        /// <summary>
        /// Generates a location for the buffer icon to spawn in (Only for Vr)
        /// </summary>
        public override void SpawnBufferIcon(bool shouldActivate) {
            base.SpawnBufferIcon(shouldActivate);
            if(shouldActivate){
                Vector3 spawnLoc = Camera.main.transform.position + (Camera.main.transform.forward * spawnDistanceUI);
                secondaryCanvas.transform.position = spawnLoc;
                previousY = Camera.main.transform.rotation.eulerAngles.y;
                secondaryCanvas.transform.LookAt(Camera.main.transform);
            }
        }
    }
}