using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class StartButtonHandler : MonoBehaviour {
    public Animator buttonAnim;
    public AudioClip buttonPressedClip;
    public AudioClip buttonMoveUpwardsClip;

    private bool hasBeenPressed = false;

    private BuildJobManager buildJobManager;
    private Transform cameraTransform;
    void Start() {
        buildJobManager = FindObjectOfType<BuildJobManager>();
        cameraTransform = Camera.main.transform;
    }

    void Update() {
        
    }

    private IEnumerator OnPressedCo() {
        yield return new WaitForSeconds(1.0f);

        Tween moveButtonUpTween = transform.parent.DOMoveY(8.5f, 1.0f).SetEase(Ease.InCirc);

        GeneralAudioPool.Instance.PlayDistanceBasedSound(buttonMoveUpwardsClip, 0.3f,
                Random.Range(0.9f, 1.1f), transform, cameraTransform);

        while (moveButtonUpTween.IsActive()) {
            yield return null;
		}

        yield return new WaitForSeconds(1.0f);

        buildJobManager.BeginGame();

        transform.parent.gameObject.SetActive(false);
    }

	private void OnTriggerEnter(Collider other) {
        if (hasBeenPressed) {
            return;
		}

		if (other.gameObject.layer == LayerMask.NameToLayer("Player")) {
            buttonAnim.Play("start_button_pressed");
            GeneralAudioPool.Instance.PlayDistanceBasedSound(buttonPressedClip, 0.4f,
                Random.Range(0.9f, 1.1f), transform, cameraTransform);

            StartCoroutine(OnPressedCo());

            hasBeenPressed = true;

        }
	}
}
