using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeneralSoundEffectPlayer : MonoBehaviour {
    public AudioClip[] dissolveAudioClips;
    public AudioClip[] metalBangAudioClips;
    public AudioClip successAudioClip;
    public AudioClip failureAudioClip;

    private Transform cameraTransform;
    void Start() {
        cameraTransform = Camera.main.transform;
    }

    void Update() {
        
    }

    public void PlaySuccessSound() {
        GeneralAudioPool.Instance.PlaySound(successAudioClip, 0.3f, Random.Range(0.9f, 1.1f));
	}

    public void PlayFailureSound() {
        GeneralAudioPool.Instance.PlaySound(failureAudioClip, 0.3f, Random.Range(0.8f, 1.0f));
    }

    public void PlayObjectDissolveSound(Transform physicsObject) {
        GeneralAudioPool.Instance.PlayDistanceBasedSound(dissolveAudioClips[Random.Range(0, dissolveAudioClips.Length)], 0.3f,
            Random.Range(0.8f, 1.2f), physicsObject, cameraTransform);
	}

    public void PlayMetalBangSound(Transform physicsObject) {
        GeneralAudioPool.Instance.PlayDistanceBasedSound(metalBangAudioClips[Random.Range(0, metalBangAudioClips.Length)], 0.3f,
            Random.Range(0.8f, 1.2f), physicsObject, cameraTransform);
    }
}
