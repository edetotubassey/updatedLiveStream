using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace com.tiledmedia.clearvr.demos {
    public class InputManager : MonoBehaviour {
        /*
        shift : Makes camera accelerate
        space : Moves camera on X and Z axis only. So the camera doesn't gain any height */

        public float mainSpeed = 100.0f; //regular speed
        public float shiftAdd = 250.0f; // multiplied by how long shift is held
        public float maxShift = 1000.0f; // maximum speed when holding shift
        private float totalRun = 1.0f;
        [SerializeField] private bool allowViewportMoving = true;

        void Update() {
#if !UNITY_ANDROID && !UNITY_IOS
            if (Input.GetKeyUp(KeyCode.Escape)) {
                Application.Quit(); //This will quit the game when the user presses the escape key
            }
#endif
            if (allowViewportMoving) {
                MoveViewport();
            }
        }

        /// <summary>
        /// Move the viewport using WASD.
        /// </summary>
        private void MoveViewport() {
            //Keyboard commands
            Vector3 p = GetBaseInput();
            if (p.sqrMagnitude > 0) { // only move while a direction key is pressed
                if (Input.GetKey(KeyCode.LeftShift)) {
                    totalRun += Time.deltaTime;
                    p = p * totalRun * shiftAdd;
                    p.x = Mathf.Clamp(p.x, -maxShift, maxShift);
                    p.y = Mathf.Clamp(p.y, -maxShift, maxShift);
                    p.z = Mathf.Clamp(p.z, -maxShift, maxShift);
                } else {
                    totalRun = Mathf.Clamp(totalRun * 0.5f, 1f, 1000f);
                    p *= mainSpeed;
                }

                p *= Time.deltaTime;
                Vector3 newPosition = transform.position;
                if (Input.GetKey(KeyCode.Space)) { //If the user wants to move on X and Z axis only
                    transform.Translate(p);
                    newPosition.x = transform.position.x;
                    newPosition.z = transform.position.z;
                    transform.position = newPosition;
                } else {
                    transform.Translate(p);
                }
            }
        }

        /// <summary>
        /// Listens to keyboard events to move the camera
        /// </summary>
        /// <returns>the normalized movement values, if it's 0 then nothing is being pressed </returns>
        private Vector3 GetBaseInput() {
            Vector3 p_Velocity = new Vector3();
            if (Input.GetKey(KeyCode.W)) {
                p_Velocity += new Vector3(0, 0, 1);
            }
            if (Input.GetKey(KeyCode.S)) {
                p_Velocity += new Vector3(0, 0, -1);
            }
            if (Input.GetKey(KeyCode.A)) {
                p_Velocity += new Vector3(-1, 0, 0);
            }
            if (Input.GetKey(KeyCode.D)) {
                p_Velocity += new Vector3(1, 0, 0);
            }
            return p_Velocity;
        }
    }
}