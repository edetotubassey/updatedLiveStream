using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace com.tiledmedia.clearvr.demos {
    public class DragDrop : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IEndDragHandler {
        private Vector2 _firstPoint = new Vector2(0, 0);
        private Vector2 _secondPoint = new Vector2(0, 0);
        public int feedIndex;
        public bool dragging;
        public GameObject thumbnailMockup;
        public bool mockupSetup = false;
        public ScrollRect scrollRect;
        public MeshRenderer doRenderer;
        private float scrollRectValue;
        public RectTransform overlayRect;
        public GameObject mainFeedCover;
        public GameObject overlayFeedCover;
        private RectTransform canvasRect;
        private FlatMosaicDemoManager flatMosaicDemoManager;
        private void Awake() { }

        // Start is called before the first frame update
        void Start() {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas != null) {
                canvasRect = canvas.GetComponent<RectTransform>();
            }
            flatMosaicDemoManager = GameObject.FindObjectOfType<FlatMosaicDemoManager>();
        }

        /// <summary>
        /// Toggle the white overlays that indicate the user where the dragged feed will end up to.
        /// </summary>
        /// <param name="inputPositionXNormalized">The pointer/finger x position normalized to the screen size</param>
        /// <param name="inputPositionYNormalized">The pointer/finger y position normalized to the screen size</param>
        private void ToggleDisplayObjectsCovers(float inputPositionXNormalized, float inputPositionYNormalized) {
            if (inputPositionYNormalized > 0.7f) {
                mainFeedCover.SetActive(false);
                overlayFeedCover.SetActive(true);
            } else if (0.25f <= inputPositionYNormalized && inputPositionYNormalized <= 0.5f) {
                mainFeedCover.SetActive(true);
                overlayFeedCover.SetActive(false);
            }
        }

        /// <summary>
        /// Change the feeds layout accordingly to where the user dragged the feed.
        /// </summary>
        /// <param name="inputPositionYNormalized">The pointer/finger y position normalized to the screen size</param>
        private void OnThumbnailEndDrag(float inputPositionYNormalized) {
            mockupSetup = false;
            if (inputPositionYNormalized > 0.7f) {
                if (flatMosaicDemoManager != null) {
                    flatMosaicDemoManager.SetPictureInPictureFeed(feedIndex);
                }
                overlayRect.GetComponent<RectTransform>().localScale = Vector3.one;
            } else {
                if (Input.mousePosition.y - _firstPoint.y > 20) {
                    flatMosaicDemoManager.SetFullScreenFeed(feedIndex);
                }
            }
            thumbnailMockup.GetComponent<RectTransform>().anchoredPosition = new Vector2(-1000, -1000);
            dragging = false;
            overlayFeedCover.SetActive(false);
            mainFeedCover.SetActive(false);
        }

        /// <summary>
        /// Move the thumbnail mockup to the position of pointer while the user is dragging the thumbnail.
        /// </summary>
        /// <param name="inputPositionXNormalized">The pointer/finger x position normalized to the screen size</param>
        /// <param name="inputPositionYNormalized">The pointer/finger y position normalized to the screen size</param>
        private void OnThumbnailDrag(float inputPositionXNormalized, float inputPositionYNormalized) {
            if (_secondPoint.y - _firstPoint.y > 20) {
                if (!mockupSetup) {
                    // An example of how we can copy the display object controller's renderer material and assign it to the placeholder to temporarily display its content while dragging
                    thumbnailMockup.GetComponent<MeshRenderer>().material = doRenderer.material;
                    mockupSetup = true;
                }
                scrollRect.horizontalNormalizedPosition = scrollRectValue; // fix the thumbnails scroll view to the same position to avoid scrolling when dragging
                if (canvasRect != null) {
                    // Move the Thumbnail mockup to the position of the pointer
                    thumbnailMockup.GetComponent<RectTransform>().anchoredPosition = new Vector2(inputPositionXNormalized * canvasRect.rect.width, inputPositionYNormalized * canvasRect.rect.height);
                }
            }
        }

        // Update is called once per frame
        void Update() {
            if (dragging) {
                float positionYNormalized;
                float positionXNormalized;
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
                Touch touch = Input.GetTouch(0);
                positionYNormalized = (touch.position.y / Screen.height);
                positionXNormalized = (touch.position.x / Screen.width);

                ToggleDisplayObjectsCovers(positionXNormalized, positionYNormalized);

                // Handle finger movements based on TouchPhase
                switch (touch.phase) {
                    //When a touch has first been detected, change the message and record the starting position
                    case TouchPhase.Began:
                        // Record initial touch position.
                        break;

                        //Determine if the touch is a moving touch
                    case TouchPhase.Moved:
                        // Determine direction by comparing the current touch position with the initial one
                        _secondPoint = touch.position;
                        OnThumbnailDrag(positionXNormalized, positionYNormalized);
                        break;

                    case TouchPhase.Ended:
                        // Report that the touch has ended when it ends
                        OnThumbnailEndDrag(positionYNormalized);
                        break;
                }
#endif
#if UNITY_EDITOR || UNITY_STANDALONE
                positionYNormalized = (Input.mousePosition.y / Screen.height);
                positionXNormalized = (Input.mousePosition.x / Screen.width);

                ToggleDisplayObjectsCovers(positionXNormalized, positionYNormalized);

                if (Input.GetMouseButton(0)) {
                    _secondPoint = Input.mousePosition;
                    OnThumbnailDrag(positionXNormalized, positionYNormalized);
                }
                if (Input.GetMouseButtonUp(0)) {
                    OnThumbnailEndDrag(positionYNormalized);
                }
#endif
            }
        }

        public void OnPointerDown(PointerEventData eventData) {
            dragging = true;
            scrollRectValue = scrollRect.horizontalNormalizedPosition;
            _firstPoint = eventData.position;
        }

        public void OnPointerUp(PointerEventData eventData) {

        }

        public void OnBeginDrag(PointerEventData eventData) {

        }

        public void OnEndDrag(PointerEventData eventData) {
            mockupSetup = false;
            if (thumbnailMockup.GetComponent<RectTransform>().anchoredPosition.y > 350) {
                overlayRect.GetComponent<RectTransform>().localScale = Vector3.one;

            }
            thumbnailMockup.GetComponent<RectTransform>().anchoredPosition = new Vector2(-1000, -1000);
            dragging = false;
        }

        public void OnDrag(PointerEventData eventData) {

        }
    }
}