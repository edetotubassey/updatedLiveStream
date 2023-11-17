using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace com.tiledmedia.clearvr.demos {
    public class InputExtensions : EventTrigger {
        public float longPressTime = 0.5f;
        public UnityEvent tapAction;
        public UnityEvent longPressAction;
        private float timer;
        private bool pressed = false;
        private bool longPressed = false;
        private RectTransform rectTransform;
        bool shouldSwap = true;
        private Vector3 pointerPos;

        // Start is called before the first frame update
        void Start() {
            rectTransform = gameObject.GetComponent<RectTransform>();
        }

        // Update is called once per frame
        void Update() {
            if (timer > longPressTime && pressed && !longPressed) {
                if (longPressAction != null) {
                    longPressAction.Invoke(); // long press
                }
            } else {
                if (pressed) {
                    timer += Time.deltaTime;
                }
            }
        }

        public override void OnPointerDown(PointerEventData eventData) {
            base.OnPointerDown(eventData);
            pressed = true;
            pointerPos = eventData.position;
        }

        public override void OnPointerUp(PointerEventData eventData) {
            base.OnPointerUp(eventData);
            pressed = false;
            longPressed = false;
            if (timer <= longPressTime && shouldSwap) {
                if (tapAction != null) {
                    tapAction.Invoke();
                }
            }
            shouldSwap = true;
            timer = 0;
        }

        public override void OnDrag(PointerEventData eventData) {
            base.OnDrag(eventData);
            pointerPos = eventData.position;
        }
    }
}