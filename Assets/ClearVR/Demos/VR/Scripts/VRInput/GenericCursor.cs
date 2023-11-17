using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace com.tiledmedia.clearvr.demos {
	abstract public class GenericCursor : MonoBehaviour {
		public float maxLength = 10.0f; // max distance from the pointer that the cursor will be placed
		[Tooltip("Angular scale of pointer")]
		public float depthScaleMultiplier = 0.03f; // scale factor for angular distance from the pointer
		public Color32 DefaultColor; // the color of the cursor when it is not intersecting anything
		public Color32 HoverColor; // the color of the cursor when it is hovering an object
		protected Vector3 startPoint; // the point where the raycast starts
		protected Vector3 forward; // the direction of the raycast
		protected Vector3 endPoint; // the point where the raycast ends
		protected bool hitTarget; // were we able to hit something?
		protected GameObject currentHitTargetObject; // the object that we hit
		public abstract void OnHover();
		public abstract void OnExit();
		public virtual bool IsHitting() {
			return hitTarget;
		}

		/// <summary>
		/// Set the cursor position based on the intersection point of the object it hits and the pointer's position.
		/// </summary>
		/// <param name="start"></param>
		/// <param name="dest"></param>
		/// <param name="normal"></param>
		/// <param name="argObjectHit"></param>
		public virtual void SetCursorStartDest(Vector3 start, Vector3 dest, Vector3 normal, GameObject argObjectHit) {
			startPoint = start;
			endPoint = dest;
			hitTarget = true;
			currentHitTargetObject = argObjectHit;
		}

		/// <summary>
		/// Set the cursor position based on the intersection point in case it didn't hit something
		/// </summary>
		/// <param name="t"></param>
		public virtual void SetCursorRay(Transform t) {
			startPoint = t.position;
			forward = t.forward;
			hitTarget = false;
			currentHitTargetObject = null;
		}

		/// <summary>
		///  Return the object that the cursor is currently hitting
		/// </summary>
		/// <returns></returns>
		public GameObject GetCurrentHitTargetObject(){
			return currentHitTargetObject;
		}

		protected void LateUpdate() {
			if (hitTarget) {
				this.transform.position = endPoint;
				float scaleOffset = Vector3.Distance(Camera.main.transform.position, endPoint) * depthScaleMultiplier;
				this.transform.localScale = new Vector3(scaleOffset, scaleOffset, scaleOffset);
				this.transform.LookAt(Camera.main.transform);
			} else {
				this.transform.position = startPoint + (maxLength * forward);
				float scaleOffset = Vector3.Distance(Camera.main.transform.position, startPoint + (maxLength * forward)) * depthScaleMultiplier;
				this.transform.localScale = new Vector3(scaleOffset / 2, scaleOffset / 2, scaleOffset / 2);
				this.transform.LookAt(Camera.main.transform);
			}
		}

		protected void OnDisable() {
			this.gameObject.SetActive(false);
		}
	}
}