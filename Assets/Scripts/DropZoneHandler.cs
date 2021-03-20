using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DropZoneHandler : MonoBehaviour {
	private LayerMask activeTriggerLayerMask = 0;
	private GameObject objectWithinDropZone = null;
	private string objectWithinDropZoneType = "none";

	private ConveyorPartHandler partHandler;

	private Coroutine checkForFullTriggerExitCo;
	private bool checkForFullTriggerExitCoOn = false;

	private GameObject dropZoneRenderersObject;

	private void Start() {
		dropZoneRenderersObject = transform.parent.GetChild(1).gameObject;
		dropZoneRenderersObject.SetActive(false);
	}

	public void SetActiveTriggerLayerMask(LayerMask newLayerMask) {
		if (newLayerMask != activeTriggerLayerMask) {
			dropZoneRenderersObject.SetActive(false);
			objectWithinDropZone = null;
			objectWithinDropZoneType = "none";
		}
		activeTriggerLayerMask = newLayerMask;
	}

	public void OnObjectReleased(string handThatReleasedObject) {
		if (objectWithinDropZone != null) {
			if (handThatReleasedObject.Equals("left") && objectWithinDropZoneType.Equals("left")
				|| handThatReleasedObject.Equals("right") && objectWithinDropZoneType.Equals("right")) {
				partHandler.OnSubPartConnected(transform.parent.parent.gameObject, objectWithinDropZone);
			}
		}
	}

	public void SetConnectedConveyorBeltHandler(ConveyorPartHandler partHandler) {
		this.partHandler = partHandler;
	}

	private bool LayerIsInLayerMask(int layer, LayerMask layerMask) {
		return layerMask == (layerMask | (1 << layer));
	}

	private void OnTriggerEnter(Collider other) {
		if (objectWithinDropZone == null && LayerIsInLayerMask(other.gameObject.layer, activeTriggerLayerMask)) {
			if (checkForFullTriggerExitCoOn && checkForFullTriggerExitCo != null) {
				StopCoroutine(checkForFullTriggerExitCo);
				checkForFullTriggerExitCoOn = false;
			}

			dropZoneRenderersObject.SetActive(true);
			objectWithinDropZone = other.transform.root.gameObject;
			if (objectWithinDropZone.layer == LayerMask.NameToLayer("LeftHandPart")) {
				objectWithinDropZoneType = "left";
			}
			if (objectWithinDropZone.layer == LayerMask.NameToLayer("RightHandPart")) {
				objectWithinDropZoneType = "right";
			}
		}
	}

	private void OnTriggerStay(Collider other) {
		if (objectWithinDropZone != null && LayerIsInLayerMask(other.gameObject.layer, activeTriggerLayerMask)) {
			if (checkForFullTriggerExitCoOn && checkForFullTriggerExitCo != null) {
				StopCoroutine(checkForFullTriggerExitCo);
				checkForFullTriggerExitCoOn = false;
			}
		}
	}

	private void OnTriggerExit(Collider other) {
		if (objectWithinDropZone != null && LayerIsInLayerMask(other.gameObject.layer, activeTriggerLayerMask)) {
			if (checkForFullTriggerExitCo != null) {
				StopCoroutine(checkForFullTriggerExitCo);
			}

			checkForFullTriggerExitCoOn = true;
			checkForFullTriggerExitCo = StartCoroutine(CheckForFullTriggerExitCo());
		}
	}

	// Wait a little while to verify that the object has fully left the drop zone
	private IEnumerator CheckForFullTriggerExitCo() {
		yield return new WaitForSeconds(0.25f);
		dropZoneRenderersObject.SetActive(false);
		objectWithinDropZone = null;
		objectWithinDropZoneType = "none";
		checkForFullTriggerExitCoOn = false;
	}
}
