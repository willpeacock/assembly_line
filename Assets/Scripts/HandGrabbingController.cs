using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

[Serializable]
public class StringEvent : UnityEvent<string> {
}

public class HandGrabbingController : MonoBehaviour {
    public XRDirectInteractor leftHandDirectInteractor;
    public XRDirectInteractor rightHandDirectInteractor;

    public ConveyorPartHandler activeLeftHandPartHandler = null;
    public ConveyorPartHandler activeRightHandPartHandler = null;

    private StringEvent onObjectReleased;

    private LayerMask defaultLayerMask;
    private LayerMask justLeftHandPartLayerMask;
    private LayerMask justRightHandPartLayerMask;
    void Awake() {
        leftHandDirectInteractor.onSelectEntered.AddListener(OnLeftHandObjectGrab);
        leftHandDirectInteractor.onSelectExited.AddListener(OnLeftHandObjectRelease);

        rightHandDirectInteractor.onSelectEntered.AddListener(OnRightHandObjectGrab);
        rightHandDirectInteractor.onSelectExited.AddListener(OnRightHandObjectRelease);

        defaultLayerMask = leftHandDirectInteractor.interactionLayerMask;
        justLeftHandPartLayerMask = LayerMask.GetMask(new string[] { "LeftHandPart" });
        justRightHandPartLayerMask = LayerMask.GetMask(new string[] { "RightHandPart" });

        onObjectReleased = new StringEvent();
    }

	public void AddOnObjectReleasedListener(UnityAction<string> onObjectReleasedListener) {
        onObjectReleased.AddListener(onObjectReleasedListener);
    }

    public ConveyorPartHandler GetActiveLeftHandPartHandler() {
        return activeLeftHandPartHandler;
    }

    public ConveyorPartHandler GetActiveRightHandPartHandler() {
        return activeRightHandPartHandler;
    }

    public void OnLeftHandObjectGrab(XRBaseInteractable inInteractable) {
        if (inInteractable == null) {
            return;
		}

        activeLeftHandPartHandler = inInteractable.GetComponent<ConveyorPartHandler>();
        if (activeLeftHandPartHandler != null) {
            activeLeftHandPartHandler.OnPickedUpByLeftHand();

            activeLeftHandPartHandler.HandleActiveHeldPartTypeChanged(activeLeftHandPartHandler, activeRightHandPartHandler);
        }
        if (activeRightHandPartHandler != null) {
            activeRightHandPartHandler.HandleActiveHeldPartTypeChanged(activeLeftHandPartHandler, activeRightHandPartHandler);
        }

        leftHandDirectInteractor.interactionLayerMask = justLeftHandPartLayerMask;
    }
    public void OnLeftHandObjectRelease(XRBaseInteractable inInteractable) {
        if (inInteractable == null) {
            return;
        }

        if (activeLeftHandPartHandler != null) {
            activeLeftHandPartHandler.OnPutDownFromHand();

            activeLeftHandPartHandler.HandleActiveHeldPartTypeChanged(null, null);
            activeLeftHandPartHandler = null;
        }
        if (activeRightHandPartHandler != null) {
            activeRightHandPartHandler.HandleActiveHeldPartTypeChanged(activeLeftHandPartHandler, activeRightHandPartHandler);
        }

        leftHandDirectInteractor.interactionLayerMask = defaultLayerMask;

        onObjectReleased.Invoke("left");
    }

    public void OnRightHandObjectGrab(XRBaseInteractable inInteractable) {
        if (inInteractable == null) {
            return;
        }
        activeRightHandPartHandler = inInteractable.GetComponent<ConveyorPartHandler>();
        if (activeRightHandPartHandler != null) {
            activeRightHandPartHandler.OnPickedUpByRightHand();

            activeRightHandPartHandler.HandleActiveHeldPartTypeChanged(activeLeftHandPartHandler, activeRightHandPartHandler);
        }
        if (activeLeftHandPartHandler != null) {
            activeLeftHandPartHandler.HandleActiveHeldPartTypeChanged(activeLeftHandPartHandler, activeRightHandPartHandler);
        }

        rightHandDirectInteractor.interactionLayerMask = justRightHandPartLayerMask;
    }
    public void OnRightHandObjectRelease(XRBaseInteractable inInteractable) {
        if (inInteractable == null) {
            return;
        }

        if (activeRightHandPartHandler != null) {
            activeRightHandPartHandler.OnPutDownFromHand();

            activeRightHandPartHandler.HandleActiveHeldPartTypeChanged(null, null);
            activeRightHandPartHandler = null;
        }
        if (activeLeftHandPartHandler != null) {
            activeLeftHandPartHandler.HandleActiveHeldPartTypeChanged(activeLeftHandPartHandler, activeRightHandPartHandler);
        }

        rightHandDirectInteractor.interactionLayerMask = defaultLayerMask;

        onObjectReleased.Invoke("right");
    }
}
