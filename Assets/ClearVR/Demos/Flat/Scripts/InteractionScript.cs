using UnityEngine;

namespace com.tiledmedia.clearvr.demos {
    public abstract class InteractionScript : MonoBehaviour {
        /// <summary>
        /// Zoom speed factor (planar only).
        /// Default value: 0.01f
        /// </summary>
        public readonly float zoomSpeed = 0.2f;
        public readonly float dragThreshold = 20;
        /// <summary>
        /// This script can run in three modes.
        /// </summary>
        protected InteractionModes _interactionMode = InteractionModes.Unknown;

        /// <summary>
        /// Note that this might be null.
        /// </summary>
        protected ClearVRPlayer clearVRPlayer;
        protected Camera mainCamera;
        protected ClearVRDisplayObjectControllerBase displayObjectController;
        protected FlatPlayerMenu flatPlayerMenu;
        protected CheckViewportBounds checkViewportBounds;
        protected abstract void MoveViewport();
        protected abstract bool ShouldShowMenu();
        protected abstract bool IsInputOverGameObject(int argPointerId);

        protected void Awake() {
            flatPlayerMenu = GameObject.FindObjectOfType<FlatPlayerMenu>();
            mainCamera = GetComponent<Camera>();
        }

        protected void Update() {
            MoveViewport();
        }

        /// <summary>
        /// One must call Initialize() after attaching this script to a GameObject (through AddComponent<>()) and after a the mesh has changed.
        /// The latter typically happened after SwitchContent and one should check ClearVREventTypes.FirstFrameRendered to trigger on that event.
        /// </summary>
        /// <param name="argClearVRPlayer">The ClearVRPlayer instance that created the ClearVRDisplayObjectControllerBase object. Cannot be null.</param>
        public void Initialize(ClearVRPlayer argClearVRPlayer) {
            if (argClearVRPlayer == null) {
                return; // Assume default behaviour, which is omnidirectional movement.
            }
            clearVRPlayer = argClearVRPlayer;
            if (clearVRPlayer == null || clearVRPlayer.mediaPlayer == null) {
                UnityEngine.Debug.LogError("[ClearVR] Trying to call InteractionScript.Initialize(null). You must provide a non-null and initialized ClearVRPlayer object as argument.");
                return;
            }
            ClearVRLayoutManager layoutManager = GameObject.FindObjectOfType<ClearVRLayoutManager>();
            if(layoutManager != null) {
                displayObjectController = layoutManager.mainDisplayObjectController;
            }
            mainCamera = clearVRPlayer.mediaPlayer.GetPlatformOptions().renderCamera;
            if(displayObjectController != null) {
                if (_interactionMode != displayObjectController.clearVRMeshType.GetAsInteractionMode()) {
                    Reset();
                }
                _interactionMode = displayObjectController.clearVRMeshType.GetAsInteractionMode();
            }
            if (_interactionMode == InteractionModes.Planar) {
                checkViewportBounds = FindObjectOfType<CheckViewportBounds>();
            }
        }

        /// <summary>
        /// Reset the cached camera orientation.
        /// Must be called when:
        /// 1. the interaction mode changes (happens automatically when calling Initialize() to (re-) initialize this component.)
        /// 2. the UI is reset after playback has ended.
        /// </summary>
        public virtual void Reset() {
            if(mainCamera != null) {
                mainCamera.transform.rotation = Quaternion.identity;
                mainCamera.transform.position = Vector3.zero;
            }
            _interactionMode = InteractionModes.Unknown;
        }
    }
}