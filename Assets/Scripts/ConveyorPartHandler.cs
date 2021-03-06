using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;


[Serializable]
public class GameObjectSubPartInfoDictionary : SerializableDictionary<GameObject, SubPartInfo> { }

[RequireComponent(typeof(XRGrabInteractable))]
public class ConveyorPartHandler : MonoBehaviour {
    public string partType = "unassigned";
    public GameObjectSubPartInfoDictionary subPartInfoByObject = new GameObjectSubPartInfoDictionary();
    public string[] completedObjectNameByPartType = null;

    public bool isCompleteObject = false;

    private List<DropZoneHandler> allDropZoneHandlers = new List<DropZoneHandler>();

    private string startingPartType;
    private List<GameObject> acquiredSubPartObjects = new List<GameObject>();

    private List<string> allAttatchedBasePartTypes = new List<string>();

    private HandGrabbingController handGrabbingController;
    private PartsSpawnManager partsSpawnManager;

    private GeneralSoundEffectPlayer generalSoundEffectPlayer;

    private LayerMask justLeftHandPartLayerMask;
    private LayerMask justRightHandPartLayerMask;

    private GeneralPhysicsObjectHandler physicsObjectHandler;

    private XRGrabInteractable grabInteractableScript = null;
    private Renderer mainRenderer = null;
    private Collider mainPartColl = null;
    private Rigidbody rb = null;

    private LayerMask initialInteractableGrabbableLayerMask;

    private bool hasInitializedOnce = false;

    private const float throwAudioVelocityCutoff = 1.0f;

	private void Start() {
		if (!hasInitializedOnce) {
            InitializeHandler();
		}
	}
	public void InitializeHandler() {
        startingPartType = partType;
        allAttatchedBasePartTypes.Add(startingPartType);

        justLeftHandPartLayerMask = LayerMask.GetMask(new string[] { "LeftHandPart" });
        justRightHandPartLayerMask = LayerMask.GetMask(new string[] { "RightHandPart" });

        physicsObjectHandler = GetComponent<GeneralPhysicsObjectHandler>();

        grabInteractableScript = GetComponent<XRGrabInteractable>();
        mainRenderer = GetComponent<Renderer>();

        mainPartColl = GetComponent<Collider>();

        rb = GetComponent<Rigidbody>();

        generalSoundEffectPlayer = FindObjectOfType<GeneralSoundEffectPlayer>();

        // If it has sub parts to manage...
        if (subPartInfoByObject.Count > 0) {
            handGrabbingController = FindObjectOfType<HandGrabbingController>();
            if (handGrabbingController == null) {
                Debug.LogError($"ERROR - ({gameObject.name}) - Could not find object of type HandGrabbingController");
                return;
            }

            foreach (GameObject subPart in subPartInfoByObject.Keys) {
				Collider subPartBaseColl = subPart.GetComponent<Collider>();
                if (subPartBaseColl != null) {
                    subPartBaseColl.enabled = false;
                }
                // Disable the extra colls and renderers
                subPart.transform.GetChild(0).gameObject.SetActive(false);
                Renderer subPartRenderer = subPart.GetComponent<Renderer>();
                if (subPartRenderer != null) {
                    subPartRenderer.enabled = false;
                }

                subPart.transform.GetChild(1).GetChild(1).gameObject.SetActive(false);

                DropZoneHandler dropZoneHandler = subPart.transform.GetChild(1).GetChild(0).GetComponent<DropZoneHandler>();
                dropZoneHandler.SetConnectedConveyorBeltHandler(this);
                dropZoneHandler.SetActiveTriggerLayerMask(0);
                // Notify it when an object is released to check if it is under a trigger
                handGrabbingController.AddOnObjectReleasedListener(dropZoneHandler.OnObjectReleased);

                allDropZoneHandlers.Add(dropZoneHandler);
            }
        }

        initialInteractableGrabbableLayerMask = grabInteractableScript.interactionLayerMask;

        partsSpawnManager = FindObjectOfType<PartsSpawnManager>();

        physicsObjectHandler.InitializeHandler();

        // Initialize with all parts disabled
        OnPartDisabled(false);

        hasInitializedOnce = true;
    }

    public void OnPartEnabled() {
        // Restore to original part type
        partType = startingPartType;
        // Restore to initial state of other tracked attributes
        acquiredSubPartObjects = new List<GameObject>();
        allAttatchedBasePartTypes = new List<string>();
        allAttatchedBasePartTypes.Add(startingPartType);
        isCompleteObject = false;

        // If it has sub parts to manage...
        if (subPartInfoByObject.Count > 0) {
            foreach (GameObject subPart in subPartInfoByObject.Keys) {
                Collider subPartBaseColl = subPart.GetComponent<Collider>();
                if (subPartBaseColl != null) {
                    subPartBaseColl.enabled = false;
                }
                // Disable the extra colls and renderers
                subPart.transform.GetChild(0).gameObject.SetActive(false);
                Renderer subPartRenderer = subPart.GetComponent<Renderer>();
                if (subPartRenderer != null) {
                    subPartRenderer.enabled = false;
                }

                subPart.transform.GetChild(1).GetChild(1).gameObject.SetActive(false);

                DropZoneHandler dropZoneHandler = subPart.transform.GetChild(1).GetChild(0).GetComponent<DropZoneHandler>();
                dropZoneHandler.SetActiveTriggerLayerMask(0);
            }
        }

        grabInteractableScript.interactionLayerMask = initialInteractableGrabbableLayerMask;
        grabInteractableScript.enabled = true;
        if (mainRenderer != null) {
            mainRenderer.enabled = true;
        }
        if (mainPartColl != null) {
            mainPartColl.enabled = true;
        }
        // Enable extra colliders and renderers
        for (int i = 0; i < transform.childCount; i++) {
            transform.GetChild(i).gameObject.SetActive(true);
        }
        rb.isKinematic = false;
        rb.useGravity = true;

        physicsObjectHandler.OnPartEnabled();
    }

    public void OnPartDisabled(bool sendsSignalToManager = true) {
        grabInteractableScript.interactionLayerMask = 0;
        grabInteractableScript.enabled = false;
        if (mainRenderer != null) {
            mainRenderer.enabled = false;
        }
        if (mainPartColl != null) {
            mainPartColl.enabled = false;
        }
        // Disable extra colliders and renderers
        for (int i = 0; i < transform.childCount; i++) {
            transform.GetChild(i).gameObject.SetActive(false);
        }
        rb.isKinematic = true;
        rb.useGravity = false;

        physicsObjectHandler.OnPartDisabled();

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        // Add it back to the available pool when disabled
        if (sendsSignalToManager) {
            partsSpawnManager.OnPartInstanceDisabled(gameObject);
		}
    }

    public void OnValidObjectSubmitted() {
		partType = "invalid";

        physicsObjectHandler.OnObjectDissolve(true);
    }

    public void OnPartConnectedAsSubPart() {
        OnPartDisabled();
    }

    public bool CheckIfAttatchedDropZoneCanBeUsed() {
        // If any other drop zone is active, can't use drop zone
        foreach (DropZoneHandler dropZoneHandler in allDropZoneHandlers) {
            if (dropZoneHandler.CheckIfObjectInDropZone()) {
                return false;
			}
		}
        return true;
	}

    public void OnSubPartConnected(GameObject subPartObject, ConveyorPartHandler outsidePartHandler) {

        subPartObject.transform.GetChild(1).GetChild(1).GetComponent<Renderer>().enabled = false;
        DropZoneHandler dropZoneHandler = subPartObject.transform.GetChild(1).GetChild(0).GetComponent<DropZoneHandler>();
        dropZoneHandler.SetActiveTriggerLayerMask(0);

        Collider subPartBaseColl = subPartObject.GetComponent<Collider>();
        if (subPartBaseColl != null) {
            subPartBaseColl.enabled = true;
        }
        // Disable the extra colls and renderers
        subPartObject.transform.GetChild(0).gameObject.SetActive(true);
        Renderer subPartRenderer = subPartObject.GetComponent<Renderer>();
        if (subPartRenderer != null) {
            subPartRenderer.enabled = true;
        }

        acquiredSubPartObjects.Add(subPartObject);

        if (outsidePartHandler != null) {
            string[] previousBasePartTypes = partType.Split(new char[] { '@' });
            List<string> sortedBasePartTypes = new List<string>(previousBasePartTypes);
            string[] newBasePartTypes = outsidePartHandler.GetPartType().Split(new char[] { '@' });
            sortedBasePartTypes.AddRange(newBasePartTypes);

            sortedBasePartTypes.Sort();
            // Update the stored list of base part types to avoid having to do this string parsing more than once
            allAttatchedBasePartTypes = sortedBasePartTypes;

            partType = string.Join("@", sortedBasePartTypes);

            // Free the other part instance
            outsidePartHandler.OnPartConnectedAsSubPart();
        }

        // Update controllers on the potentially changed objects being held
        HandleActiveHeldPartTypeChanged(
            handGrabbingController.GetActiveLeftHandPartHandler(),
            handGrabbingController.GetActiveRightHandPartHandler()
        );

        // Play audio sound effect
        generalSoundEffectPlayer.PlayConnectSound();

        // Check if a complete object has been formed
        if (completedObjectNameByPartType != null && completedObjectNameByPartType.Length == 2) {
            if (partType.Equals(completedObjectNameByPartType[0])) {
                partType = completedObjectNameByPartType[1];
                isCompleteObject = true;
			}
		}
    }

    public void HandleActiveHeldPartTypeChanged(ConveyorPartHandler activeLeftHandPartHandler, ConveyorPartHandler activeRightHandPartHandler) {
        string activeLeftHandPartType = activeLeftHandPartHandler != null ? activeLeftHandPartHandler.GetPartType() : "unassigned";
        string activeRightHandPartType = activeRightHandPartHandler != null ? activeRightHandPartHandler.GetPartType() : "unassigned";

        foreach (GameObject subPart in subPartInfoByObject.Keys) {
            // If this sub part has already been acquired, continue
            if (acquiredSubPartObjects.Contains(subPart)) {
                continue;
			}

            // If this event occurred while the object was highlighted, don't modify anything, as this should
            // trigger OnSubPartConnected after the release
            if (subPart.transform.GetChild(1).GetChild(1).gameObject.activeSelf) {
                continue;
            }

            SubPartInfo subPartInfo = subPartInfoByObject[subPart];

            bool shouldBeSkipped = false;

            // To continue, all required sub part objects must already be attatched to this object
            foreach (GameObject requiredSubPart in subPartInfo.GetRequiredOtherSubPartObjects()) {
                if (!acquiredSubPartObjects.Contains(requiredSubPart)) {
                    shouldBeSkipped = true;
                    break;
                }
            }
            if (shouldBeSkipped) {
                continue;
            }

            // If this feature is being used
            if (subPartInfo.GetRequiredOneOfOtherSubPartObjects().Count > 0) {
                shouldBeSkipped = true;
                // To continue, AT LEAST ONE of these sub part objects must already be attatched to this object
                foreach (GameObject requiredSubPart in subPartInfo.GetRequiredOneOfOtherSubPartObjects()) {
                    if (acquiredSubPartObjects.Contains(requiredSubPart)) {
                        shouldBeSkipped = false;
                        break;
                    }
                }
                if (shouldBeSkipped) {
                    continue;
                }
            }

            // To continue, no prohibiting sub parts must already be attatched to this object
            foreach (GameObject prohibitingSubPartObject in subPartInfo.GetProhibitingOtherSubPartObjects()) {
                if (acquiredSubPartObjects.Contains(prohibitingSubPartObject)) {
                    shouldBeSkipped = true;
                    break;
                }
            }
            if (shouldBeSkipped) {
                continue;
            }

            string subPartType = subPartInfo.GetSubPartType();

            DropZoneHandler dropZoneHandler = subPart.transform.GetChild(1).GetChild(0).GetComponent<DropZoneHandler>();

            // If player is now holding desired part in LEFT hand
            if (activeLeftHandPartType.Equals(subPartType)) {
                dropZoneHandler.SetActiveTriggerLayerMask(justLeftHandPartLayerMask);
            }
            // Else If player is now holding desired part in RIGHT hand
            else if (activeRightHandPartType.Equals(subPartType)) {
                dropZoneHandler.SetActiveTriggerLayerMask(justRightHandPartLayerMask);
            }
            // Else if holding in both hands or is not holding the part in either hand
            else {
                dropZoneHandler.SetActiveTriggerLayerMask(0);
            }
        }
    }

    public bool CheckIfCompleteObject() {
        return isCompleteObject;
	}

    public string GetCompleteObjectName() {
        // Check if a complete object has been formed
        if (isCompleteObject && completedObjectNameByPartType != null
            && completedObjectNameByPartType.Length == 2) {
            return completedObjectNameByPartType[1];
        }
        return "unassigned";
    }

    public string GetPartType() {
        return partType;
	}

    private void TryPlayThrowAudio() {
        if (rb.velocity.magnitude >= throwAudioVelocityCutoff) {
            generalSoundEffectPlayer.PlayWhipSound();
		}
	}

    public void OnPickedUpByLeftHand() {
        grabInteractableScript.interactionLayerMask = LayerMask.GetMask(new string[] { "LeftHandPart" });

        gameObject.layer = LayerMask.NameToLayer("LeftHandPart");
        if (transform.childCount > 0) {
            transform.GetChild(0).gameObject.layer = LayerMask.NameToLayer("LeftHandPart");
            for (int i = 0; i < transform.GetChild(0).childCount; i++) {
                transform.GetChild(0).GetChild(i).gameObject.layer = LayerMask.NameToLayer("LeftHandPart");
            }
        }

        physicsObjectHandler.SetCanPlayAudioAfterRelease(false);
        physicsObjectHandler.PlayObjectBangSound();
    }

    public void OnPickedUpByRightHand() {
        grabInteractableScript.interactionLayerMask = LayerMask.GetMask(new string[] { "RightHandPart" });

        gameObject.layer = LayerMask.NameToLayer("RightHandPart");
        if (transform.childCount > 0) {
            transform.GetChild(0).gameObject.layer = LayerMask.NameToLayer("RightHandPart");
            for (int i = 0; i < transform.GetChild(0).childCount; i++) {
                transform.GetChild(0).GetChild(i).gameObject.layer = LayerMask.NameToLayer("RightHandPart");
            }
        }

        physicsObjectHandler.SetCanPlayAudioAfterRelease(false);
        physicsObjectHandler.PlayObjectBangSound();
    }

    public void OnPutDownFromHand() {
        grabInteractableScript.interactionLayerMask = LayerMask.GetMask(new string[] { "Grabbable" });

        gameObject.layer = LayerMask.NameToLayer("Grabbable");
        if (transform.childCount > 0) {
            transform.GetChild(0).gameObject.layer = LayerMask.NameToLayer("Grabbable");
            for (int i = 0; i < transform.GetChild(0).childCount; i++) {
                transform.GetChild(0).GetChild(i).gameObject.layer = LayerMask.NameToLayer("Grabbable");
            }
        }

        TryPlayThrowAudio();

        physicsObjectHandler.SetCanPlayAudioAfterRelease(true);
        physicsObjectHandler.PlayObjectBangSound();
    }
}
