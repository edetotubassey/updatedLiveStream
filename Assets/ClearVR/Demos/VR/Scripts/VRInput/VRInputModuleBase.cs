using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace com.tiledmedia.clearvr.demos {
	public abstract class VRInputModuleBase : BaseInputModule {
		private static VRInputModuleBase _instance;
		public static VRInputModuleBase Instance { get { return _instance; } }

		[Tooltip("The distance in pixels to drag before we start firing a drag event")]
		public int DragThreshold = 20;
		[SerializeField]
		[Tooltip("List of pointers managed by this input module")]
		protected List<Pointer> pointers;
		public bool isPointerEventDataDirty = true; // determines if we need to update the pointer event data, often after a pointer has been added or removed
		public PointerEventData pointerData { get; set; } // the pointer data for the current event
		public Pointer pointer { get { return _pointer; } set { _pointer = value; SetUpWorldCanvasesRenderCameras(); SetUpPointer(); } }
		private Pointer _pointer;
		private Vector2 pointerPressPos = new Vector2(0, 0);
		private RectTransform cachedGraphicRect = null;
		
		/// <summary>
		/// Get the position of primary controller, this method must be SDK specific.
		/// </summary>
		/// <returns>A dummy vector.</returns>
		public abstract Vector3 GetPrimaryControllerPosition();

		/// <summary>
		/// Get the rotation of primary controller, this method must be SDK specific.
		/// </summary>
		/// <returns>A dummy vector.</returns>
		public abstract Vector3 GetPrimaryControllerRotation();
		
		protected override void Start() {
			SingletonInit();
			SetActivePointer();
		}

		virtual protected void Update() {
			if (isPointerEventDataDirty && pointer != null) {
				SetUpPointer();
				isPointerEventDataDirty = false;
			}
		}

		/// <summary>
		/// Sets up the singleton instance of this class
		/// </summary>
		private void SingletonInit() {
			if (_instance != null && _instance != this) {
				Destroy(this.gameObject);
			} else {
				_instance = this;
			}
		}

		/// <summary>
		/// Set up the world canvases by assinging the correct render camera to each canvas
		/// </summary>
		/// <returns></returns>
		public bool SetUpWorldCanvasesRenderCameras() {
			bool isSuccess = false;
			if (pointer != null && pointer.cam != null) {
				Canvas[] worldCanvases = GameObject.FindObjectsOfType<Canvas>();
				foreach (Canvas canvas in worldCanvases) {
					canvas.worldCamera = Instance.pointer.cam;
				}
				isSuccess = true;
			}
			return isSuccess;
		}

		public override void Process() {
			if (pointer != null && pointerData == null) {
				pointerData = new PointerEventData(eventSystem) {
					position = new Vector2(1080, 924)
				};
			}
			// Raycast to find the current pointer target
			eventSystem.RaycastAll(pointerData, m_RaycastResultCache);
			pointerData.pointerCurrentRaycast = FindFirstRaycast(m_RaycastResultCache);

			if (pointer != null && pointer.cursor != null) {
				pointer.cursor.OnExit();
			}

			GraphicRaycaster graphicRaycaster = pointerData.pointerCurrentRaycast.module as GraphicRaycaster;
			// Raycast may not actually contain a result
			if (graphicRaycaster && graphicRaycaster.eventCamera != null) {
				// The Unity UI system expects event data to have a screen position
				// so even though this raycast came from a world space ray we must get a screen
				// space position for the camera attached to this raycaster for compatability
				Vector2 position = graphicRaycaster.eventCamera.WorldToScreenPoint(pointerData.pointerCurrentRaycast.worldPosition);

				// Find the world position and normal the Graphic the ray intersected
				RectTransform graphicRect = pointerData.pointerCurrentRaycast.gameObject.GetComponent<RectTransform>();
				if (graphicRect != cachedGraphicRect) {
					ClearSelection();
					cachedGraphicRect = graphicRect;
				}
				if (graphicRect != null) {
					if (pointer != null && pointer.cursor != null) {
						pointer.cursor.OnHover();
					}
				}

				// Hover handler
				HandlePointerExitAndEnter(pointerData, pointerData.pointerCurrentRaycast.gameObject);

				if (pointerData.pointerDrag != null && !pointerData.dragging) {
					// Begin Dragging by executing its handler
					ExecuteEvents.Execute(pointerData.pointerDrag, pointerData, ExecuteEvents.beginDragHandler);
					pointerData.dragging = true;
				}

				ExecuteEvents.Execute(pointerData.pointerDrag, pointerData, ExecuteEvents.dragHandler);
			} else {
				// In case we exit the canvas we clear all the selected UI elements
				ClearSelection();
			}
		}

		/// <summary>
		/// Button Press handler
		/// </summary>
		protected void ProcessPress() {
			pointerData.pointerPressRaycast = pointerData.pointerCurrentRaycast;

			pointerData.pointerPress = ExecuteEvents.GetEventHandler<IPointerClickHandler>(pointerData.pointerPressRaycast.gameObject);
			pointerData.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(pointerData.pointerPressRaycast.gameObject);

			ExecuteEvents.Execute(pointerData.pointerPress, pointerData, ExecuteEvents.pointerDownHandler);
			ExecuteEvents.Execute(pointerData.pointerDrag, pointerData, ExecuteEvents.beginDragHandler);
			GraphicRaycaster graphicRaycaster = pointerData.pointerCurrentRaycast.module as GraphicRaycaster;
			if (graphicRaycaster) // raycast may not actually contain a result
			{
				pointerPressPos = graphicRaycaster.eventCamera.WorldToScreenPoint(pointerData.pointerCurrentRaycast.worldPosition);
			}
		}

		/// <summary>
		/// Button Release handler
		/// </summary>
		protected void ProcessRelease() {
			GraphicRaycaster graphicRaycaster = pointerData.pointerCurrentRaycast.module as GraphicRaycaster;
			if (graphicRaycaster) // raycast may not actually contain a result
			{
				if (Vector2.Distance(pointerPressPos, GetScreenPosition(pointerData.pointerCurrentRaycast)) > DragThreshold) {
					// When dragging we should cancel any pointer down state
					// And clear selection!
					pointerData.eligibleForClick = false;
					pointerData.pointerPress = null;
					pointerData.rawPointerPress = null;
				}
			}

			// Check for click collider
			GameObject pointerUpHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(pointerData.pointerCurrentRaycast.gameObject);

			// Execute End Drag
			if (pointerData.pointerDrag != null && pointerData.dragging) {
				ExecuteEvents.Execute(pointerData.pointerDrag, pointerData, ExecuteEvents.endDragHandler);
			}

			// Execute Pointer Up
			if (pointerData.pointerPress == pointerUpHandler) {
				ExecuteEvents.Execute(pointerData.pointerPress, pointerData, ExecuteEvents.pointerClickHandler);
				ExecuteEvents.Execute(pointerData.pointerPress, pointerData, ExecuteEvents.pointerUpHandler);
			}

			// Resetting to default values
			pointerData.dragging = false;
			pointerData.eligibleForClick = false;

			pointerData.pointerPress = null;
			pointerData.pointerDrag = null;
			pointerData.rawPointerPress = null;
			pointerData.pointerDrag = null;

			// Clear cached raycast result
			pointerData.pointerCurrentRaycast.Clear();
		}

		/// <summary>
		/// Get the only pointer.
		/// </summary>
		/// <returns>The only pointer found in the scene</returns>
		public Pointer GetPointer() {
			return pointer;
		}

		/// <summary>
		/// Get the pointers list.
		/// </summary>
		/// <returns>All the pointers that had been found in the scene</returns>
		public List<Pointer> GetPointers() {
			return pointers;
		}

		/// <summary>
		/// Get screen position of worldPosition contained in this RaycastResult
		/// </summary>
		/// <param name="worldPosition"></param>
		/// <returns></returns>
		public Vector2 GetScreenPosition(RaycastResult raycastResult) {
			// In future versions of Uinty RaycastResult will contain screenPosition so this will not be necessary
			if (pointer != null) {
				return pointer.GetComponent<Camera>().WorldToScreenPoint(raycastResult.worldPosition);
			} else {
				return Vector2.zero;
			}
		}

		/// <summary>
		/// For RectTransform, calculate it's normal in world space
		/// </summary>
		private static Vector3 GetRectTransformNormal(RectTransform rectTransform) {
			Vector3[] corners = new Vector3[4];
			rectTransform.GetWorldCorners(corners);
			Vector3 BottomEdge = corners[3] - corners[0];
			Vector3 LeftEdge = corners[1] - corners[0];
			rectTransform.GetWorldCorners(corners);
			return Vector3.Cross(BottomEdge, LeftEdge).normalized;
		}

		/// <summary>
		/// Weather the pointer is over a UI element
		/// </summary>
		/// <returns>True if the pointer is over an UI element </returns>
		public bool IsPointerHitting() {
			if (pointer != null) {
				return pointer.cursor.IsHitting();
			} else {
				return false;
			}
		}

		/// <summary>
		/// Clear all elements that were selected by the pointer
		/// </summary>
		protected void ClearSelection() {
			var baseEventData = GetBaseEventData();

			// clear all selection
			HandlePointerExitAndEnter(pointerData, null);

			eventSystem.SetSelectedGameObject(null, baseEventData);
		}

		/// <summary>
		/// Set up a new pointer by creating a new PointerEventData
		/// </summary>
		public void SetUpPointer() {
			pointerData = new PointerEventData(EventSystem.current) {
				position = new Vector2(pointer.GetComponent<Camera>().pixelWidth / 2, pointer.GetComponent<Camera>().pixelHeight / 2)
			};
		}

		/// <summary>
		/// Set the only pointer found in the scene as the active one
		/// </summary>
		public void SetActivePointer() {
			if (pointers.Count == 1) {
				pointer = pointers[0];
			} else if (pointers.Count == 0) {
				Debug.Log("No pointers found in the list, will look for one in the scene");
				SetUpPointer();
			} else {
				Debug.Log("More than one pointer found in the list, set the pointer to the right type manually with the overloaded method.");
			}
		}

		/// <summary>
		/// Sets the active Pointer to the specific type requested.
		/// </summary>
		/// <param name="argControllerType"></param>
		/// <returns>true if succeeded, false if failed.</returns>
		public void SetActivePointer(Pointer.VRControllerType argControllerType) {
			foreach (Pointer pointer in pointers) {
				if (pointer != null) {
					if (pointer.controllerType == argControllerType) {
						pointer.gameObject.SetActive(true);
						this.pointer = pointer;
						pointer.Show = true;
					} else {
						pointer.gameObject.SetActive(false);
						pointer.Show = false;
					}
				}
			}
		}
	}
}