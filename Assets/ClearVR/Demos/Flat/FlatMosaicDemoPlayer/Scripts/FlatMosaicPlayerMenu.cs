using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace com.tiledmedia.clearvr.demos {
    public class FlatMosaicPlayerMenu : UserInterfaceBase {
        [Header("Flat Mosaic UI Elements")]
        public Animator thumbnailMenuAnimator;
        public Animator playerConrollerMenuAnimator;
        public Toggle thumbnailMenuToggle;
        public GameObject arrowUp;
        public GameObject arrowDown;
        public Transform pictureInPictureContainer;
        public Graphic selectionOverlay;
        public Graphic pipSelectionOverlay;
        protected override void Start() {
            base.Start();
            if (selectionOverlay != null) {
                StartCoroutine(FadeOut(selectionOverlay, 0.2f));
            }
            if (pipSelectionOverlay != null) {
                StartCoroutine(FadeOut(pipSelectionOverlay, 0.2f));
            }
        }

        protected override void Update() {
            base.Update();
        }

        /// <summary>
        /// Toggles the thumbnail scroll menu slide in/slide out animations.
        /// </summary>
        /// <param name="shouldShow">Whether the thumbnail menu should slid in (true) or out (false).</param>
        public void ToggleThumbnailsMenu(bool shouldShow) {
            if (thumbnailMenuAnimator) {
                thumbnailMenuAnimator.SetBool("toggleThumbnailMenu", shouldShow);
            }
        }

        /// <summary>
        /// Toggles the player controller menu slide in/slide out animations.
        /// </summary>
        /// <param name="shouldShow">Whether the player controller menu should slid in (true) or out (false).</param>
        public void TogglePlayerControllerMenu(bool shouldShow) {
            if (playerConrollerMenuAnimator) {
                playerConrollerMenuAnimator.SetBool("VideoControllersPanelVisible", shouldShow);
            }
        }

        /// <summary>
        /// Invert the arrow icons direction according to the thumbnail menu visibility.
        /// </summary>
        /// <param name="isUp">Whether the arrow should be pointing up (true) = the thumbnails menu is not visible.</param>
        public void SwapArrowIcons(bool isUp) {
            if (arrowUp && arrowDown) {
                arrowUp.SetActive(!isUp);
                arrowDown.SetActive(isUp);
            }
        }

        /// <summary>
        /// Close the picture in picture overlay.
        /// </summary>
        public void ClosePictureInPicture() {
            if (pictureInPictureContainer) {
                pictureInPictureContainer.transform.localScale = Vector3.zero;
            }
        }

        /// <summary>
        /// Fade in a colored graphic element that overlays the pressed thumbnail to give the user visual feedback.
        /// </summary>
        public void OnThumbnailPress() {
            if (selectionOverlay != null) {
                selectionOverlay.CrossFadeColor(new Color(0.7843137f, 0.7843137f, 0.7843137f, 0.8f), 0.05f, true, true);
                StartCoroutine(FadeOut(selectionOverlay, 0.2f));
            }
        }

        /// <summary>
        /// Fade in a colored graphic element that overlays the pressed picture in picture to give the user visual feedback.
        /// </summary>
        public void OnOverlayPress(Graphic overlay) {
            if (pipSelectionOverlay != null) {
                pipSelectionOverlay.CrossFadeColor(new Color(0.7843137f, 0.7843137f, 0.7843137f, 0.8f), 0.05f, true, true);
                StartCoroutine(FadeOut(pipSelectionOverlay, 0.2f));
            }
        }

        /// <summary>
        /// Fades out a graphic element over a given time.
        /// </summary>
        /// <param name="graphic">The graphic element to fade.</param>
        /// <param name="delay">How long the fading should last (s)</param>
        IEnumerator FadeOut(Graphic graphic, float delay) {
            yield return new WaitForSeconds(delay);
            graphic.CrossFadeColor(new Color(0.7843137f, 0.7843137f, 0.7843137f, 0.0f), delay, true, true);
        }
    }
}