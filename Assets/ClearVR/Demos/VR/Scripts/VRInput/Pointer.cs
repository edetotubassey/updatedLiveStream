using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace com.tiledmedia.clearvr.demos {
	public class Pointer : MonoBehaviour {
		[SerializeField] public float DefaultLength = 10; // the default length of the ray
		public GenericCursor cursor; // the cursor associated to this pointer
		public Camera cam { get; private set; } // dummy camera just used for raycasting
		private bool show;
		private LineRenderer lineRenderer = null; // the visual component of the pointer.
		public VRControllerType controllerType = VRControllerType.Unknown; // the controller type associated to this pointer.

		// Should the pointer and the cursor be visible
		public bool Show {
			get {
				if (cursor != null) {
					return cursor.gameObject.activeInHierarchy;
				} else {
					return true;
				}
			}

			set {
				if (cursor != null && cursor.gameObject != null) {
					this.show = value;
					cursor.gameObject.SetActive(value);
					}
				if (lineRenderer != null) {
					lineRenderer.enabled = value;
				}
			}
		}

		private void Awake() {
			cam = GetComponent<Camera>();
			lineRenderer = GetComponent<LineRenderer>();

			cam.enabled = false;
			Show = true;
		}

		// Update is called once per frame
		private void Update() {
			if (!cursor.gameObject.activeInHierarchy) {
				cursor.gameObject.SetActive(true);
			}
			if (cursor == null) {
				cursor = transform.GetComponentInChildren<GenericCursor>();
			}
			if (cursor != null) {
				UpdateLine();
			}
		}

		public void DisableLineRenderer() {
			if (lineRenderer != null) {
				lineRenderer.enabled = false;
			}
		}

		private void UpdateLine() {
			// Use default or distance
			PointerEventData data = VRInputModuleBase.Instance.pointerData;

			if (cursor != null && transform != null) {
				// Set default cursor poition
				cursor.SetCursorRay(transform);
			}
			// Raycast
			RaycastHit hit;

			// If nothing is hit, set do default length
			float canvasDistance = data.pointerCurrentRaycast.distance == 0 ? DefaultLength : data.pointerCurrentRaycast.distance;
			float colliderDistance = DefaultLength;

			if (CreateRaycast(out hit)) {
				colliderDistance = hit.distance;
			}

			// Get the closest one
			float targetLength = Mathf.Min(colliderDistance, canvasDistance);

			// Default
			Vector3 endPosition = transform.position + (transform.forward * targetLength);

			if (lineRenderer != null) {
				// Set the line renderer
				lineRenderer.SetPosition(0, transform.position);
				lineRenderer.SetPosition(1, (endPosition + transform.position) / 2);
			}
			if (hit.collider != null && cursor != null) {
				cursor.SetCursorStartDest(transform.position, endPosition, hit.normal, hit.collider.gameObject);
			}
		}

		private bool CreateRaycast(out RaycastHit argHit) {
			Ray ray = new Ray(transform.position, transform.forward);
			return Physics.Raycast(ray, out argHit, DefaultLength);
		}

		/// <summary>
		/// Whether the pointer is over a UI element
		/// </summary>
		/// <returns></returns>
		public bool IsCursorHitting() {
			if (cursor != null) {
				return cursor.IsHitting();
			} else {
				return false;
			}
		}

		public enum VRControllerType {
			Unknown,
			Gaze,
			RUntracked,
			LUntracked,
			RTracked,
			LTracked
		}
	}
}