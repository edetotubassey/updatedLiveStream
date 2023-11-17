using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace com.tiledmedia.clearvr.demos {
    [RequireComponent(typeof(ScrollRect))]
    public class ButtonSelectionController : MonoBehaviour {
        public List<Button> buttons;
        public GameObject buttonHighlighter;
        private int index;
        public void Start() {
            if (buttons[index] != null) {
                buttons[index].Select();
            }
            SelectThumbnail(0);
        }

        public void Update() {

        }

        /// <summary>
        /// Determines if this RectTransform is fully visible from the specified camera.
        /// Works by checking if each bounding box corner of this RectTransform is inside the cameras screen space view frustrum.
        /// </summary>
        /// <returns><c>true</c> if is fully visible from the specified camera; otherwise, <c>false</c>.</returns>
        /// <param name="rectTransform">Rect transform.</param>
        /// <param name="camera">Camera.</param>
        public bool IsFullyVisibleFrom(RectTransform rectTransform, Camera camera) {
            return CountCornersVisibleFrom(rectTransform) == 4; // True if all 4 corners are visible
        }

        /// <summary>
        /// Counts the bounding box corners of the given RectTransform that are visible from the given Camera in screen space.
        /// </summary>
        /// <returns>The amount of bounding box corners that are visible from the Camera.</returns>
        /// <param name="rectTransform">Rect transform.</param>
        /// <param name="camera">Camera.</param>
        private int CountCornersVisibleFrom(RectTransform rectTransform, Camera camera = null) {
            Rect screenBounds = new Rect(0f, 0f, Screen.width, Screen.height); // Screen space bounds (assumes camera renders across the entire screen)
            Vector3[] objectCorners = new Vector3[4];
            rectTransform.GetWorldCorners(objectCorners);

            int visibleCorners = 0;
            Vector3 tempScreenSpaceCorner = Vector3.zero; // Cached
            for (var i = 0; i < objectCorners.Length; i++) // For each corner in rectTransform
            {
                if (camera != null)
                    tempScreenSpaceCorner = camera.WorldToScreenPoint(objectCorners[i]); // Transform world space position of corner to screen space
                else {
                    tempScreenSpaceCorner = objectCorners[i]; // If no camera is provided we assume the canvas is Overlay and world space == screen space
                }

                if (screenBounds.Contains(tempScreenSpaceCorner)) // If the corner is inside the screen
                {
                    visibleCorners++;
                }
            }
            return visibleCorners;
        }

        /// <summary>
        /// Keeps track of the currently selected thumbnail.
        /// </summary>
        /// <param name="indexOfSelectedThumbnail"></param>
        public void SelectThumbnail(int indexOfSelectedThumbnail) {
            index = indexOfSelectedThumbnail;
        }

        /// <summary>
        /// Move the button graphic highlighter under the selected button.
        /// </summary>
        /// <param name="button">The newly selected button.</param>
        public void HighlightButton(Transform button) {
            buttonHighlighter.transform.SetParent(button, false);
            buttonHighlighter.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        }
    }
}