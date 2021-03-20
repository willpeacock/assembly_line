using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(MeshRenderer))]
public class ConveyorBeltHandler : MonoBehaviour {
    public float speed = 2.0f;
    public float texScrollSpeedFactor = 0.25f;

    private Rigidbody rb;
    private Material mainRubberBeltMaterial;
    private float currentMaterialOffset = 0.0f;

	private void Start() {
        rb = GetComponent<Rigidbody>();

        mainRubberBeltMaterial = GetComponent<MeshRenderer>().material;
    }

	private void Update() {
        currentMaterialOffset += Time.deltaTime * (speed * texScrollSpeedFactor);

        mainRubberBeltMaterial.mainTextureOffset = new Vector2(currentMaterialOffset, 0);
    }

	void FixedUpdate() {
        Vector3 beltPos = rb.position;
        rb.position -= transform.right * speed * Time.fixedDeltaTime;
        rb.MovePosition(beltPos);
    }

    public float GetSpeed() {
        return speed;
	}

    public void SetSpeed(float newSpeed) {
        speed = newSpeed;
	}

    public void OnGameOver() {
        speed = 0.0f;
	}
}
