using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeneralSoundEffectPlayer : MonoBehaviour {
    public AudioClip[] dissolveAudioClips;

    public AudioClip[] metalBangAudioClips;
    public AudioClip[] metalAltBangAudioClips;
    public AudioClip[] clothBangAudioClips;
    public AudioClip[] glassBangAudioClips;
    public AudioClip[] plasticBangAudioClips;
    public AudioClip[] woodBangAudioClips;

    public AudioClip[] whipAudioClips;
    public AudioClip[] connectAudioClips;

    public AudioClip successAudioClip;
    public AudioClip cashAudioClip;
    public AudioClip failureAudioClip;
    public AudioClip bellAudioClip;

    private Transform cameraTransform;
    void Start() {
        cameraTransform = Camera.main.transform;
    }

    void Update() {
        
    }

    public void PlaySuccessSound() {
        GeneralAudioPool.Instance.PlaySound(successAudioClip, 0.5f, Random.Range(0.9f, 1.1f));
        GeneralAudioPool.Instance.PlaySound(cashAudioClip, 0.5f, Random.Range(0.9f, 1.1f));
    }

    public void PlayFailureSound() {
        GeneralAudioPool.Instance.PlaySound(failureAudioClip, 0.5f, Random.Range(0.8f, 1.0f));
    }

    public void PlayConnectSound() {
        GeneralAudioPool.Instance.PlaySound(connectAudioClips[Random.Range(0, connectAudioClips.Length)], 0.45f, Random.Range(0.9f, 1.1f));
    }

    public void PlayBellSound() {
        GeneralAudioPool.Instance.PlaySound(bellAudioClip, 0.55f, Random.Range(0.9f, 1.1f));
    }

    public void PlayWhipSound() {
        GeneralAudioPool.Instance.PlaySound(whipAudioClips[Random.Range(0, whipAudioClips.Length)], 0.35f, Random.Range(0.9f, 1.15f));
    }

    public void PlayObjectDissolveSound(Transform physicsObject) {
        GeneralAudioPool.Instance.PlayDistanceBasedSound(dissolveAudioClips[Random.Range(0, dissolveAudioClips.Length)], 0.3f,
            Random.Range(0.8f, 1.2f), physicsObject, cameraTransform);
	}

    public void PlayObjectBangSound(Transform physicsObject, string soundType) {
        AudioClip selectedAudioClip = null;
        switch (soundType) {
            case "metal":
                selectedAudioClip = metalBangAudioClips[Random.Range(0, metalBangAudioClips.Length)];
                break;
            case "metal_alt":
                selectedAudioClip = metalAltBangAudioClips[Random.Range(0, metalAltBangAudioClips.Length)];
                break;
            case "cloth":
                selectedAudioClip = clothBangAudioClips[Random.Range(0, clothBangAudioClips.Length)];
                break;
            case "glass":
                selectedAudioClip = glassBangAudioClips[Random.Range(0, glassBangAudioClips.Length)];
                break;
            case "plastic":
                selectedAudioClip = plasticBangAudioClips[Random.Range(0, plasticBangAudioClips.Length)];
                break;
            case "wood":
                selectedAudioClip = woodBangAudioClips[Random.Range(0, woodBangAudioClips.Length)];
                break;
            default:
                Debug.LogWarning($"WARNING - Did not recognize material type {soundType} for audio noise");
                return;
        }

        GeneralAudioPool.Instance.PlayDistanceBasedSound(selectedAudioClip, 0.25f,
            Random.Range(0.8f, 1.2f), physicsObject, cameraTransform);
    }
}
