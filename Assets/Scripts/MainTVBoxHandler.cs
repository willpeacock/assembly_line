using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainTVBoxHandler : MonoBehaviour {
    public Transform tvBoxTransform;
    public Transform anchorTransform;
    public LineRenderer ropeLineRenderer;

    private Rigidbody tvRB;
    private SpringJoint springJoint;

	private void Start() {
        tvRB = tvBoxTransform.GetComponent<Rigidbody>();
        springJoint = tvBoxTransform.GetComponent<SpringJoint>();

        tvRB.mass = Random.Range(9.0f, 12.0f);
        //tvRB.drag = Random.Range(0.1f, 0.2f);
        //tvRB.angularDrag = Random.Range(0.1f, 0.2f);
        springJoint.spring = Random.Range(100.0f, 200.0f);
    }

	void Update() {
        Vector3[] newRopePositions = new Vector3[] {
            tvBoxTransform.position,
            anchorTransform.position
        };

        ropeLineRenderer.positionCount = 2;
        ropeLineRenderer.SetPositions(newRopePositions);
    }
}
