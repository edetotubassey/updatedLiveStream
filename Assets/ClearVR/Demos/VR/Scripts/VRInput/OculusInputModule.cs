using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace com.tiledmedia.clearvr.demos {
	public class OculusInputModule : VRInputModuleBase {
		public Transform trackingSpace; // reference to the tracking space
		[Tooltip("How to identify primary controller.")]
		public OVRInput.Controller primaryController;
		[Tooltip("How to identify secondary controller.")]
		public OVRInput.Controller secondaryController;
		private OVRInput.Controller activeCotroller; // the controller that is currently active

		[Space(5)]
		[Header("UI Button")]
		[Tooltip("The button that will be used to interact with the UI")]
		public OVRInput.Button UIClickButton = OVRInput.Button.PrimaryIndexTrigger;

		[Space(5)]
		[Header("Button Events")]
		// Listener for the controllers buttons (NOTE: primary indicates the currently active controller!!)
		public UnityEvent onPrimaryIndexTriggerClick;
		public UnityEvent onPrimaryHandTriggerDown;
		public UnityEvent onPrimaryHandTriggerUp;
		public UnityEvent onButtonOneClick;
		public UnityEvent onButtonTwoClick;
		public UnityEvent onPrimaryThumbstickIdle;
		public UnityEvent onPrimaryThumbstickRight;
		public UnityEvent onPrimaryThumbstickLeft;

		/// <summary>
		/// Returns the primary controller position.
		/// </summary>
		/// <returns>The primary controller position.</returns>
		public override Vector3 GetPrimaryControllerPosition() {
			return trackingSpace.TransformPoint(OVRInput.GetLocalControllerPosition(primaryController));
		}

		/// <summary>
		/// Returns the primary controller rotation.
		/// </summary>
		/// <returns>The primary controller rotation.</returns>
		public override Vector3 GetPrimaryControllerRotation() {
			return trackingSpace.TransformDirection(OVRInput.GetLocalControllerRotation(primaryController).eulerAngles);
		}

		public override void Process() {
			base.Process();

			// Press
			if (OVRInput.GetDown(UIClickButton, activeCotroller)) {
				ProcessPress();
			}

			// Release
			if (OVRInput.GetUp(UIClickButton, activeCotroller)) {
				ProcessRelease();
			}
		}

		protected override void Start() {
			base.Start();
			SetActiveController();
			Invoke("SetUpPointer", 1); // wait for the pointer to be set up
		}

		protected override void Update() {
			base.Update();

			if (OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick).x > 0.7f) {
				onPrimaryThumbstickRight.Invoke();
			} else {
				onPrimaryThumbstickIdle.Invoke();
			}
			if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, activeCotroller)) {
				onPrimaryIndexTriggerClick.Invoke();
			}
			if (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, activeCotroller)) {
				onPrimaryHandTriggerDown.Invoke();
			}
			if (OVRInput.GetUp(OVRInput.Button.PrimaryHandTrigger, activeCotroller)) {
				onPrimaryHandTriggerUp.Invoke();
			}
			if (isPointerEventDataDirty) {
				SetActiveController();
				isPointerEventDataDirty = false;
			}
			ShouldSwitchActiveController(); // switch the active controller if the user has changed it
		}

		/// <summary>
		/// Sets the active controller depending on the handedness set in the Oculus OS settings.
		/// </summary>
		private void SetActiveController() {
			if (OVRInput.GetDominantHand() == OVRInput.Handedness.RightHanded) {
				primaryController = OVRInput.Controller.RTouch;
				secondaryController = OVRInput.Controller.LTouch;
			} else {
				primaryController = OVRInput.Controller.LTouch;
				secondaryController = OVRInput.Controller.RTouch;
			}
			SetActiveController(primaryController);
		}

		/// <summary>
		/// Sets the active controller passed as a parameter.
		/// </summary>
		/// <param name="argController">The new active controller.</param>
		private void SetActiveController(OVRInput.Controller argController) {
			activeCotroller = argController;
			isPointerEventDataDirty = true;
			SetActivePointer(GetGenericControllerType(activeCotroller));
		}

		/// <summary>
		/// Check if the user pressed a button on the inactive controller, if so make it the active controller.
		/// </summary>
		private void ShouldSwitchActiveController() {
			if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, primaryController) && primaryController != activeCotroller) {
				SetActiveController(primaryController);
			}
			if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, secondaryController) && secondaryController != activeCotroller) {
				SetActiveController(secondaryController);
			}
		}

		/// <summary>
		/// Handy method to get the generic controller type.
		/// </summary>
		/// <param name="argController">The controller.</param>
		/// <returns>The generic controller type.</returns>
		private Pointer.VRControllerType GetGenericControllerType(OVRInput.Controller argController) {
			switch (argController) {
				case OVRInput.Controller.LTouch:
					return Pointer.VRControllerType.LTracked;
				case OVRInput.Controller.RTouch:
					return Pointer.VRControllerType.RTracked;
				default:
					return Pointer.VRControllerType.Unknown;
			}
		}
	}
}