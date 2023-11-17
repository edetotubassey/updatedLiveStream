using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace com.tiledmedia.clearvr.demos {
    [RequireComponent(typeof(Canvas))]
    /// <summary>
    /// Helper class that fits a world space canvas to the screen.
    /// </summary>
    public class CanvasFitter : MonoBehaviour {
        float canvasDistance; // canvas distance from camera
        float vFov; // camera vertical field of view
        Camera cam;
        int previousAspectRatio;

        // Start is called before the first frame update
        void Start() {
            cam = Camera.main != null ? Camera.main : FindObjectOfType<Camera>();
            if (cam != null) {
                vFov = cam.fieldOfView;
                canvasDistance = Vector3.Distance(cam.transform.position, transform.position);
                RescaleCanvas();
            }
        }

        private void Update() {
#if UNITY_EDITOR
            int currentAspectRatio = Screen.width - Screen.height;
            if (cam != null && (previousAspectRatio != currentAspectRatio)) {
                RescaleCanvas();
                previousAspectRatio = currentAspectRatio;
            }
#endif
        }

        /// <summary>
        /// Rescale a world space canvas to fit the screen.
        /// </summary>
        private void RescaleCanvas() {
            RectTransform rect = GetComponent<RectTransform>(); 
            float newCanvasWidth = (rect.rect.height * Screen.width) / Screen.height;
            rect.sizeDelta = new Vector2(newCanvasWidth, rect.rect.height);
            float newScale = Mathf.Tan((Mathf.Deg2Rad * vFov) / 2) * (2 * canvasDistance) / rect.rect.height;
            rect.localScale = new Vector3(newScale, newScale, newScale);
        }
    }
}