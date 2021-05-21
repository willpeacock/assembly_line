using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Rigidbody))]
public class GeneralPhysicsObjectHandler : MonoBehaviour {
    public string materialType = "metal";

    private bool objectIsEnabled = true;

    private bool hasTriggeredDissolve = false;

    private Shader dissolveShader = null;

    private ConveyorPartHandler partHandler;

    private Renderer[] allObjectRenderers;
    private Rigidbody[] allRBs;
    private float[] allRBInitialMasses;
    private List<Material[]> allDefaultMaterialsPerRenderer;
    private List<Material[]> allDissolveMaterialsPerRenderer;
    private List<Material> allDissolveMaterials;

    private Tween dissolveTween;
    private Vector3 randomTorqueDir = Vector3.up;

    private LayerMask dissolveOnContactLayerMask = 0;

    private Dictionary<Transform, int> originalLayerByChildObject = new Dictionary<Transform, int>();

    private GeneralSoundEffectPlayer generalSoundEffectPlayer;

    private Coroutine delayBeforeHitSoundCo;
    private bool delayBeforeHitSoundCoOn = false;

    private bool canPlayAudioAfterRelease = false;

    private const float outOfBoundsCutoffYPos = -5.0f;
    private const string dissolveShaderPath = "Universal Render Pipeline/Autodesk Interactive/Autodesk Dissolve";

    private const float dissolveTime = 4.0f;
    private const float dissolveUpwardsForce = 1.5f;
    private const float dissolveTorque = 5.0f;
    public void InitializeHandler() {
        dissolveOnContactLayerMask = LayerMask.GetMask(new string[] { "FactoryObject", "Floor", "Wall" });

        originalLayerByChildObject = new Dictionary<Transform, int>();
        RecursiveStoreInitialChildLayer(transform);

        dissolveShader = Shader.Find(dissolveShaderPath);
        if (dissolveShader == null) {
            Debug.Log($"WARNING - Unable to find shader: '{dissolveShaderPath}'");
        }

        generalSoundEffectPlayer = FindObjectOfType<GeneralSoundEffectPlayer>();

        partHandler = GetComponent<ConveyorPartHandler>();

        allRBs = GetComponentsInChildren<Rigidbody>(true);
        allRBInitialMasses = new float[allRBs.Length];
        for (int i = 0; i < allRBs.Length; i++) {
            allRBInitialMasses[i] = allRBs[i].mass;
		}

        allObjectRenderers = GetComponentsInChildren<Renderer>(true);
        allDefaultMaterialsPerRenderer = new List<Material[]>();
        foreach (Renderer objectRenderer in allObjectRenderers) {
            // Create copies of all original materials in the renderer
            Material[] rendererMaterials = new Material[objectRenderer.materials.Length];
            for (int i = 0; i < objectRenderer.materials.Length; i++) {
                // Make a copy then add it to stored array
                Material newMaterial = new Material(objectRenderer.materials[i]);
                rendererMaterials[i] = newMaterial;
            }

            allDefaultMaterialsPerRenderer.Add(rendererMaterials);
        }

        allDissolveMaterials = new List<Material>();
        allDissolveMaterialsPerRenderer = new List<Material[]>();
        foreach (Renderer objectRenderer in allObjectRenderers) {
            // Create copies of all original materials in the renderer
            Material[] rendererMaterials = new Material[objectRenderer.materials.Length];
            for (int i = 0; i < objectRenderer.materials.Length; i++) {
                // Make a copy then change the shader, then add it to stored array
                Material newMaterial = new Material(objectRenderer.materials[i]);
                newMaterial.shader = dissolveShader;
                newMaterial.SetFloat("_DissolveRatio", 0.0f);
                rendererMaterials[i] = newMaterial;

                allDissolveMaterials.Add(newMaterial);
            }

            allDissolveMaterialsPerRenderer.Add(rendererMaterials);
        }
    }

    void Update() {
        if (!objectIsEnabled) {
            return;
		}

        if (!hasTriggeredDissolve
            && gameObject.layer != LayerMask.NameToLayer("LeftHandPart")
            && gameObject.layer != LayerMask.NameToLayer("RightHandPart")
            && transform.position.y < outOfBoundsCutoffYPos) {
            OnObjectDestroy();
        }
    }

    private void FixedUpdate() {
        if (!objectIsEnabled) {
            return;
        }

        if (hasTriggeredDissolve) {
            foreach (Rigidbody rb in allRBs) {
                rb.AddForce(Vector3.up * dissolveUpwardsForce);

                rb.AddTorque(randomTorqueDir * dissolveTorque);
            }
        }
    }

    public void OnPartEnabled() {
        RecursiveRestoreInitialChildLayer(transform);

        hasTriggeredDissolve = false;
        canPlayAudioAfterRelease = false;
        if (delayBeforeHitSoundCo != null) {
            StopCoroutine(delayBeforeHitSoundCo);
            delayBeforeHitSoundCo = null;
        }
        delayBeforeHitSoundCoOn = false;

        for (int i = 0; i < allRBs.Length; i++) {
            Rigidbody rb = allRBs[i];

            rb.isKinematic = false;
            rb.useGravity = true;
            rb.mass = allRBInitialMasses[i];
        }

        for (int i = 0; i < allObjectRenderers.Length; i++) {
            Renderer objectRenderer = allObjectRenderers[i];
            objectRenderer.materials = allDefaultMaterialsPerRenderer[i];
        }

        objectIsEnabled = true;
    }

    public void OnPartDisabled() {
        objectIsEnabled = false;

        foreach (Rigidbody rb in allRBs) {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        if (dissolveTween != null && dissolveTween.IsActive()) {
            dissolveTween.Kill();
        }
    }

    private void RecursiveStoreInitialChildLayer(Transform parentTransform) {
        originalLayerByChildObject.Add(parentTransform, parentTransform.gameObject.layer);

        foreach (Transform child in parentTransform) {
            RecursiveStoreInitialChildLayer(child);
        }
    }

    private void RecursiveRestoreInitialChildLayer(Transform parentTransform) {
        if (originalLayerByChildObject.ContainsKey(parentTransform)) {
            parentTransform.gameObject.layer = originalLayerByChildObject[parentTransform];
        }
        else {
            Debug.LogError($"{name}: Did not have stored initial layer for child object '{parentTransform.gameObject.name}'");
		}

        foreach (Transform child in parentTransform) {
            RecursiveRestoreInitialChildLayer(child);
        }
    }

    public void SetChildrenToLayer(Transform parentTransform, int layer) {
        foreach (Transform child in parentTransform) {
            child.gameObject.layer = layer;
            SetChildrenToLayer(child, layer);
        }
    }

    public void OnObjectDissolve(bool objectSubmittedDissolve = false) {
        hasTriggeredDissolve = true;

        if (partHandler != null) {
            partHandler = null;
        }

        generalSoundEffectPlayer.PlayObjectDissolveSound(transform);

        gameObject.layer = LayerMask.NameToLayer("DissolvedObject");
        SetChildrenToLayer(transform, LayerMask.NameToLayer("DissolvedObject"));

        if (dissolveShader != null) {
            foreach (Rigidbody rb in allRBs) {
                rb.useGravity = false;
                rb.mass = 1.0f;
            }
            randomTorqueDir = Random.insideUnitSphere;

            for (int i = 0; i < allObjectRenderers.Length; i++) {
                Renderer objectRenderer = allObjectRenderers[i];
                objectRenderer.materials = allDissolveMaterialsPerRenderer[i];
            }

            Color greenColor = new Color(0, Mathf.Pow(2, 6), 0);
            foreach (Material rendererMaterial in allDissolveMaterials) {
                rendererMaterial.SetFloat("_DissolveRatio", 0.0f);
                if (objectSubmittedDissolve) {
                    rendererMaterial.SetColor("_GlowColor", greenColor);
                }
            }
            dissolveTween = DOTween.To(
                () => allDissolveMaterials[0].GetFloat("_DissolveRatio"),
                x => {
                    foreach (Material rendererMaterial in allDissolveMaterials) {
                        rendererMaterial.SetFloat("_DissolveRatio", x);
                    }
                },
                1.0f,
                dissolveTime
            ).SetEase(Ease.OutQuad)
            .OnComplete(() => OnObjectDestroy());
        }
        else {
            OnObjectDestroy();
        }
    }

    private void OnObjectDestroy() {
        partHandler.OnPartDisabled();
    }

    private bool LayerIsInLayerMask(int layer, LayerMask layerMask) {
        return layerMask == (layerMask | (1 << layer));
    }

    private IEnumerator DelayBeforeHitSoundCo() {
        yield return new WaitForSeconds(0.25f);
        delayBeforeHitSoundCoOn = false;
    }

    public void SetCanPlayAudioAfterRelease(bool canPlay) {
        canPlayAudioAfterRelease = canPlay;
    }

    public void PlayObjectBangSound() {
        generalSoundEffectPlayer.PlayObjectBangSound(transform, materialType);
    }

    private void OnCollisionEnter(Collision collision) {
        if (!objectIsEnabled) {
            return;
		}

		if (!hasTriggeredDissolve
            && gameObject.layer != LayerMask.NameToLayer("LeftHandPart")
            && gameObject.layer != LayerMask.NameToLayer("RightHandPart")
            && LayerIsInLayerMask(collision.gameObject.layer, dissolveOnContactLayerMask)) {
            OnObjectDissolve();
        }

        // If can play audio and hit something not connected to self
        if (!delayBeforeHitSoundCoOn && collision.gameObject.layer != gameObject.layer) {
            if (gameObject.layer == LayerMask.NameToLayer("LeftHandPart")
            || gameObject.layer == LayerMask.NameToLayer("RightHandPart")) {

                if (delayBeforeHitSoundCo != null) {
                    StopCoroutine(delayBeforeHitSoundCo);
				}
                delayBeforeHitSoundCoOn = true;
                delayBeforeHitSoundCo = StartCoroutine(DelayBeforeHitSoundCo());

                generalSoundEffectPlayer.PlayObjectBangSound(transform, materialType);
            }
        }
        // If it was released, let it play a hit sound for the first thing it hits
        if (canPlayAudioAfterRelease && !delayBeforeHitSoundCoOn) {
            if (gameObject.layer == LayerMask.NameToLayer("Grabbable")) {
                generalSoundEffectPlayer.PlayObjectBangSound(transform, materialType);

                canPlayAudioAfterRelease = false;
            }
		}
	}
}
