using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace com.tiledmedia.clearvr.demos {
    public class FlatPlayerMenu : UserInterfaceBase {
        [Header("Flat UI Settings")]
        [Tooltip("The animator which controls the UI panel with the video player controllers buttons.")]
        public Animator videoPlayerControllerPanelAnimator;

        [Tooltip("How long the video player controller panel is visible")]
        public float videoControllersPanleVisibleTimeInSeconds = 5.0f;

        [Tooltip("Weather the video player controller panel should be hidden automatically.")]
        public bool automaticallyHideVideoPlayerControllerPanel = true;

        private float videoControllersPanelHideTimer; // helper timer to show/hide the video controllers
        private InteractionModes _previousInteractionMode = InteractionModes.Unknown; // dictates how to handle touch inputs from the user
        private bool isVideoPlayerControllerPanelVisible = false; // weather the video player controller panel is shown
        private InteractionScript interactionScript;

        protected override void Start() {
            base.Start();
            SetUpInteractionScript();
            ShowVideoControllersPanel();
        }

        protected override void Update() {
            base.Update();
            HideVideoControllersPanel();
        }

        public override void Initialize(ClearVRPlayer newClearVRPlayer, DemoManagerBase demoManagerReference) {
            base.Initialize(newClearVRPlayer, demoManagerReference);
            clearVRPlayer.clearVRDisplayObjectEvents.AddListener(InteractionScriptInitCB);
        }

        private void InteractionScriptInitCB(ClearVRPlayer clearVRPlayer, ClearVRDisplayObjectControllerBase clearVRDisplayObjectController, ClearVRDisplayObjectEvent clearVRDisplayObjectEvent) {
            switch (clearVRDisplayObjectEvent.type) {
                case ClearVRDisplayObjectEventTypes.FirstFrameRendered:
                    interactionScript.Initialize(clearVRPlayer);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        ///  We attach an InteractionScript that will check for user input on the screen and depending on the content type will perform rotation or translation of the camera.
        ///  To get different behaviour one can define its own InteractionScript (e.g.: TouchCam, MouseCam) class or equivalent.
        /// </summary>
        private void SetUpInteractionScript() {
            InteractionScript newInteractionScript;
            if (Utils.GetDeviceType() == DeviceTypes.PCFlat) {
                newInteractionScript = Camera.main.gameObject.GetComponent<MouseCam>() ?? Camera.main.gameObject.AddComponent<MouseCam>();
                InteractionScript touchIteractionScript = Camera.main.gameObject.GetComponent<TouchCam>();
                if (touchIteractionScript) {
                    DestroyImmediate(touchIteractionScript);
                }
            } else {
                newInteractionScript = Camera.main.gameObject.GetComponent<TouchCam>() ?? Camera.main.gameObject.AddComponent<TouchCam>();
                InteractionScript mouseInteractionScript = Camera.main.gameObject.GetComponent<MouseCam>();
                if (mouseInteractionScript) {
                    DestroyImmediate(mouseInteractionScript);
                }
            }
            newInteractionScript.enabled = true;
            interactionScript = newInteractionScript;
        }

        /// <summary>
        /// Show and hide the video cotrols.
        /// </summary>
        public void ShowVideoControllersPanel() {
            if (!isVideoPlayerControllerPanelVisible) {
                videoPlayerControllerPanelAnimator.SetBool("VideoControllersPanelVisible", true);
                isVideoPlayerControllerPanelVisible = true;
            } else {
                videoPlayerControllerPanelAnimator.SetBool("VideoControllersPanelVisible", false);
                isVideoPlayerControllerPanelVisible = false;
                videoControllersPanelHideTimer = videoControllersPanleVisibleTimeInSeconds;
            }
        }

        public void HideVideoControllersPanel() {
            // Make the flat UI move smoothly down or up.
            if (isVideoPlayerControllerPanelVisible && automaticallyHideVideoPlayerControllerPanel) {
                if (videoControllersPanelHideTimer >= 0.0f) {
                    videoControllersPanelHideTimer -= Time.deltaTime;
                } else {
                    videoPlayerControllerPanelAnimator.SetBool("VideoControllersPanelVisible", false);
                    isVideoPlayerControllerPanelVisible = false;
                    videoControllersPanelHideTimer = videoControllersPanleVisibleTimeInSeconds;
                }
            }
        }

        /// <summary>
        /// Set-up the UI on the first frame rendered depending on the content.
        /// </summary>
        /// <param name="argClearVRPlayer">Current active ClearVRPlayer</param>
        public override void FirstFrameRenderedHandler(ClearVRPlayer argClearVRPlayer, ClearVRDisplayObjectControllerBase argClearVRDisplayObjectController) {
            base.FirstFrameRenderedHandler(argClearVRPlayer, argClearVRDisplayObjectController);
            UpdateInteractionScripts(argClearVRPlayer, argClearVRDisplayObjectController);
        }

        /// <summary>
        /// Handle the UI behavior after a stop event fired by the ClearVRPlayer
        /// </summary>
        public override void ParseStateChangedStopped() {
            base.ParseStateChangedStopped();
            UpdateInteractionScripts(null, null);
        }

        /// <summary>
        /// Update the interaction script.
        /// </summary>
        /// <param name="argClearVRPlayer">the active clearVRPlayer</param>
        public void UpdateInteractionScripts(ClearVRPlayer argClearVRPlayer /* might be null */, ClearVRDisplayObjectControllerBase argClearVRDisplayObjectController /* Might be null */ ) {
            // TODO-V9
            // ClearVRDisplayObjectController clearVRMeshBase = null;
            // if (argClearVRPlayer != null) {
            //     clearVRMeshBase = argClearVRPlayer.mediaPlayer.GetActiveClearVRDisplayObjectController();
            // }
            // if (clearVRMeshBase != null) {
            //     InteractionModes newInteractionMode = clearVRMeshBase.clearVRMeshType.GetAsInteractionMode();
            //     if (newInteractionMode != _previousInteractionMode) {
            //         // Interaction mode has changed, so we always reset the viewport and pose.
            //         CheckViewportBounds cvb = clearVRMeshBase.gameObject.GetComponent<CheckViewportBounds>();
            //         if (cvb != null) {
            //             Destroy(cvb);
            //         }
            //         if (newInteractionMode == InteractionModes.Planar) {
            //             cvb = clearVRMeshBase.gameObject.AddComponent<CheckViewportBounds>();
            //             cvb.Initialize(argClearVRPlayer);
            //         }
            //         _previousInteractionMode = newInteractionMode;
            //     }
            // }
            // interactionScript.Initialize(argClearVRPlayer);
        }

        /// <summary>
        /// Resets the UI and the inraction scripts.
        /// </summary>
        public override void Reset() {
            base.Reset();
            _previousInteractionMode = InteractionModes.Unknown;
            if (Camera.main != null && interactionScript != null) {
                interactionScript.Reset();
            }
        }
    }
}