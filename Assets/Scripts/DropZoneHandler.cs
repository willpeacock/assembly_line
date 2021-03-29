using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DropZoneHandler : MonoBehaviour {
	private LayerMask activeTriggerLayerMask = 0;
	private ConveyorPartHandler objectWithinDropZone = null;
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

	public bool CheckIfObjectInDropZone() {
		return objectWithinDropZone;
	}

	private bool LayerIsInLayerMask(int layer, LayerMask layerMask) {
		return layerMask == (layerMask | (1 << layer));
	}

	private void CheckForObjectInTrigger(Collider other) {
		if (objectWithinDropZone == null && LayerIsInLayerMask(other.gameObject.layer, activeTriggerLayerMask)
			&& partHandler.CheckIfAttatchedDropZoneCanBeUsed()) {
			if (checkForFullTriggerExitCoOn && checkForFullTriggerExitCo != null) {
				StopCoroutine(checkForFullTriggerExitCo);
				checkForFullTriggerExitCoOn = false;
			}

			dropZoneRenderersObject.SetActive(true);
			objectWithinDropZone = other.transform.GetComponentInParent<ConveyorPartHandler>();
			if (objectWithinDropZone.gameObject.layer == LayerMask.NameToLayer("LeftHandPart")) {
				objectWithinDropZoneType = "left";
			}
			if (objectWithinDropZone.gameObject.layer == LayerMask.NameToLayer("RightHandPart")) {
				objectWithinDropZoneType = "right";
			}
		}
	}

	private void OnTriggerEnter(Collider other) {
		CheckForObjectInTrigger(other);
	}

	private void OnTriggerStay(Collider other) {
		CheckForObjectInTrigger(other);
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
