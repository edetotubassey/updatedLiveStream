using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace com.tiledmedia.clearvr.demos {
	[RequireComponent(typeof(OVROverlay))]
	public class OverlayCursor : GenericCursor {
		private OVROverlay cursorVisual;

		private void Start() {
			cursorVisual = gameObject.GetComponent<OVROverlay>();
		}
		public override void OnHover() {
			if (cursorVisual) {
				cursorVisual.colorOffset = new Vector4(HoverColor.r, HoverColor.g, HoverColor.b, HoverColor.a);
			}
		}

		public override void OnExit() {
			if (cursorVisual) {
				cursorVisual.colorOffset = new Vector4(DefaultColor.r, DefaultColor.g, DefaultColor.b, DefaultColor.a);
			}
		}
	}
}