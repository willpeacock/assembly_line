using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Rigidbody))]
public class GeneralPhysicsObjectHandler : MonoBehaviour {
    private bool hasTriggeredDissolve = false;
    private bool hasBeenDestroyed = false;

    private Shader dissolveShader = null;

    private ConveyorPartHandler partHandler;
    private Rigidbody[] allRBs;

    private Tween dissolveTween;
    private Vector3 randomTorqueDir = Vector3.up;

    private LayerMask dissolveOnContactLayerMask = 0;

    private GeneralSoundEffectPlayer generalSoundEffectPlayer;

    private Coroutine delayBeforeHitSoundCo;
    private bool delayBeforeHitSoundCoOn = false;

    private const float outOfBoundsCutoffYPos = -5.0f;
    private const string dissolveShaderPath = "Universal Render Pipeline/Autodesk Interactive/Autodesk Dissolve";

    private const float dissolveTime = 4.0f;
    private const float dissolveUpwardsForce = 1.5f;
    private const float dissolveTorque = 5.0f;
    void Start() {
        dissolveOnContactLayerMask = LayerMask.GetMask(new string[] { "FactoryObject", "Floor", "Wall" });

        dissolveShader = Shader.Find(dissolveShaderPath);
        if (dissolveShader == null) {
            Debug.Log($"WARNING - Unable to find shader: '{dissolveShaderPath}'");
		}

        generalSoundEffectPlayer = FindObjectOfType<GeneralSoundEffectPlayer>();

        partHandler = GetComponent<ConveyorPartHandler>();
    }

    void Update() {
        if (!hasTriggeredDissolve && !hasBeenDestroyed
            && gameObject.layer != LayerMask.NameToLayer("LeftHandPart")
            && gameObject.layer != LayerMask.NameToLayer("RightHandPart")
            && transform.position.y < outOfBoundsCutoffYPos) {
            OnObjectDestroy();
        }
    }

	private void FixedUpdate() {
		if (hasTriggeredDissolve && !hasBeenDestroyed) {
            foreach (Rigidbody rb in allRBs) {
                rb.AddForce(Vector3.up * dissolveUpwardsForce);

                rb.AddTorque(randomTorqueDir * dissolveTorque);
            }
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

        allRBs = GetComponentsInChildren<Rigidbody>();

        if (dissolveShader != null) {
            foreach (Rigidbody rb in allRBs) {
                rb.useGravity = false;
                rb.mass = 1.0f;
            }
            randomTorqueDir = Random.insideUnitSphere;

            List<Material> allMaterials = new List<Material>();
            Renderer[] allObjectRenderers = GetComponentsInChildren<Renderer>();
            foreach (Renderer objectRenderer in allObjectRenderers) {
                allMaterials.AddRange(objectRenderer.materials);
            }

            Color greenColor = new Color(0, Mathf.Pow(2, 6), 0);
            foreach (Material rendererMaterial in allMaterials) {
                rendererMaterial.shader = dissolveShader;
                rendererMaterial.SetFloat("_DissolveRatio", 0.0f);
                if (objectSubmittedDissolve) {
                    rendererMaterial.SetColor("_GlowColor", greenColor);
                }
            }
            dissolveTween = DOTween.To(
                () => allMaterials[0].GetFloat("_DissolveRatio"),
                x => {
                    foreach (Material rendererMaterial in allMaterials) {
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
        if (dissolveTween != null && dissolveTween.IsActive()) {
            dissolveTween.Kill();
        }
        hasBeenDestroyed = true;
        Destroy(gameObject);
	}

    private bool LayerIsInLayerMask(int layer, LayerMask layerMask) {
        return layerMask == (layerMask | (1 << layer));
    }

    private IEnumerator DelayBeforeHitSoundCo() {
        yield return new WaitForSeconds(0.25f);
        delayBeforeHitSoundCoOn = false;
	}

    private void OnCollisionEnter(Collision collision) {
		if (!hasTriggeredDissolve && !hasBeenDestroyed
            && gameObject.layer != LayerMask.NameToLayer("LeftHandPart")
            && gameObject.layer != LayerMask.NameToLayer("RightHandPart")
            && LayerIsInLayerMask(collision.gameObject.layer, dissolveOnContactLayerMask)) {
            OnObjectDissolve();
        }

        if (!delayBeforeHitSoundCoOn) {
            if (gameObject.layer == LayerMask.NameToLayer("LeftHandPart")
            || gameObject.layer == LayerMask.NameToLayer("RightHandPart")) {

                if (delayBeforeHitSoundCo != null) {
                    StopCoroutine(delayBeforeHitSoundCo);
				}
                delayBeforeHitSoundCoOn = true;
                delayBeforeHitSoundCo = StartCoroutine(DelayBeforeHitSoundCo());

                //generalSoundEffectPlayer.PlayMetalBangSound(transform);
            }
        }
	}
}
