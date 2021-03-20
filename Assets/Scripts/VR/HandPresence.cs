using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class HandPresence : MonoBehaviour {
    public InputDeviceCharacteristics controllerCharacteristics;
    public GameObject handModelPrefab;

    private Animator handAnim;
    private InputDevice targetDevice;
    void Start() {
        AttemptGetTargetDevice();

        GameObject spawnedHandModel = Instantiate(handModelPrefab, transform);
        handAnim = spawnedHandModel.GetComponent<Animator>();
    }

    private void UpdateHandAnimation() {
        if (targetDevice.TryGetFeatureValue(CommonUsages.trigger, out float triggerValue)) {
            handAnim.SetFloat("trigger", triggerValue);
		}
        else {
            handAnim.SetFloat("trigger", 0.0f);
        }

        if (targetDevice.TryGetFeatureValue(CommonUsages.grip, out float gripValue)) {
            handAnim.SetFloat("grip", gripValue);
        }
        else {
            handAnim.SetFloat("grip", 0.0f);
        }
    }

	private void Update() {
        if (!targetDevice.isValid) {
            AttemptGetTargetDevice();
        }
        else {
            UpdateHandAnimation();
        }
    }

    private void AttemptGetTargetDevice() {
        List<InputDevice> devices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(controllerCharacteristics, devices);

        if (devices.Count > 0) {
            targetDevice = devices[0];
        }
    }
}
