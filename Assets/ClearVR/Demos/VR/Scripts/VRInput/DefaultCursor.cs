using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace com.tiledmedia.clearvr.demos {
	[RequireComponent(typeof(SpriteRenderer))]
	public class DefaultCursor : GenericCursor {
		private SpriteRenderer cursorVisual; // the cursor's visual, usually a white dot
		private void Start() {
			cursorVisual = gameObject.GetComponent<SpriteRenderer>();
		}

		public override void OnHover() {
			if (cursorVisual != null)
				cursorVisual.color = HoverColor;
		}

		public override void OnExit() {
			if (cursorVisual != null)
				cursorVisual.color = DefaultColor;
		}
	}
}