using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class LocomotionController : MonoBehaviour {
    public GameObject teleportReticleObject;
    public LineRenderer teleportLineRenderer;
    public XRInteractorLineVisual xrInteractorLineVisual;

    private MainInputManager inputManager;

    void Start() {
        inputManager = FindObjectOfType<MainInputManager>();

        inputManager.inputActions.XRIRightHand.TeleportSelect.canceled +=
            ctx => OnTeleportSelectCancelled();

        inputManager.inputActions.XRIRightHand.TeleportModeActivate.performed +=
            ctx => OnTeleportModeActivate();

        xrInteractorLineVisual.enabled = false;
        teleportReticleObject.SetActive(false);
        teleportLineRenderer.enabled = false;
    }

    private void OnTeleportSelectCancelled() {
        xrInteractorLineVisual.enabled = false;
        teleportReticleObject.SetActive(false);
        teleportLineRenderer.enabled = false;
    }

    private void OnTeleportModeActivate() {
        xrInteractorLineVisual.enabled = true;
        teleportReticleObject.SetActive(true);
        teleportLineRenderer.enabled = true;
    }
}
