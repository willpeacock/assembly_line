using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PipeTriggerHandler : MonoBehaviour {
	public ParticleSystem mainPipeParticleSystem;
	public LayerMask validObjectLayerMask;

	private bool particleSystemActive = false;

    private List<ConveyorPartHandler> completedHandlersInTrigger = new List<ConveyorPartHandler>();

	private Dictionary<GameObject, ConveyorPartHandler> handlersByValidObjectsInTrigger
		= new Dictionary<GameObject, ConveyorPartHandler>();

	private BuildJobManager buildJobManager;
    void Start() {
		buildJobManager = FindObjectOfType<BuildJobManager>();
	}

	private bool LayerIsInLayerMask(int layer, LayerMask layerMask) {
		return layerMask == (layerMask | (1 << layer));
	}

	private void CheckForCompletedObject(ConveyorPartHandler objectPartHandler) {
		if (!completedHandlersInTrigger.Contains(objectPartHandler)) {
			if (objectPartHandler != null) {
				string objectPartType = objectPartHandler.GetPartType();

				if (buildJobManager.CheckIfValidJobObject(objectPartType)) {
					// If object can now be submitted
					if (objectPartHandler.gameObject.layer == LayerMask.NameToLayer("Grabbable")) {
						buildJobManager.OnValidObjectSubmitted(objectPartHandler);
					}
					completedHandlersInTrigger.Add(objectPartHandler);
					if (!particleSystemActive) {
						mainPipeParticleSystem.Play();
						particleSystemActive = true;
					}
				}
			}
		}
	}

	private void OnTriggerEnter(Collider other) {
		if (LayerIsInLayerMask(other.gameObject.layer, validObjectLayerMask)
			&& !handlersByValidObjectsInTrigger.ContainsKey(other.gameObject)) {
			ConveyorPartHandler objectPartHandler = other.gameObject.GetComponentInParent<ConveyorPartHandler>();
			if (objectPartHandler != null) {
				handlersByValidObjectsInTrigger.Add(other.gameObject, objectPartHandler);
				CheckForCompletedObject(objectPartHandler);
			}
		}
	}

	private void OnTriggerStay(Collider other) {
		// Check stored objects that have called OnTriggerEnter but not yet OnTriggerExit
		if (LayerIsInLayerMask(other.gameObject.layer, validObjectLayerMask) &&
			handlersByValidObjectsInTrigger.ContainsKey(other.gameObject)) {
			ConveyorPartHandler objectPartHandler = handlersByValidObjectsInTrigger[other.gameObject];
			CheckForCompletedObject(objectPartHandler);

			if (completedHandlersInTrigger.Contains(objectPartHandler)) {
				if (objectPartHandler.gameObject.layer == LayerMask.NameToLayer("Grabbable")) {
					buildJobManager.OnValidObjectSubmitted(objectPartHandler);
				}
			}
		}
	}

	private void OnTriggerExit(Collider other) {
		if (handlersByValidObjectsInTrigger.ContainsKey(other.gameObject)) {
			ConveyorPartHandler objectPartHandler = handlersByValidObjectsInTrigger[other.gameObject];
			completedHandlersInTrigger.Remove(objectPartHandler);

			handlersByValidObjectsInTrigger.Remove(other.gameObject);

			if (particleSystemActive && completedHandlersInTrigger.Count <= 0) {
				mainPipeParticleSystem.Stop();
				particleSystemActive = false;
			}
		}
	}
}
