using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainInputManager : MonoBehaviour {
    public XRIDefaultInputActions inputActions;

    private void Awake() {
        if (inputActions == null) {
            inputActions = new XRIDefaultInputActions();
        }
    }

    private void OnEnable() {
        inputActions.XRILeftHand.Enable();
        inputActions.XRIRightHand.Enable();
    }

    private void OnDisable() {
        inputActions.XRILeftHand.Disable();
        inputActions.XRIRightHand.Disable();
    }
}
