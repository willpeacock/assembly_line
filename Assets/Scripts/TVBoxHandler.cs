using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[System.Serializable]
public class StringFloatDictionary : SerializableDictionary<string, float> { }

public class TVBoxHandler : MonoBehaviour {
    public StringFloatDictionary jobPartPricingByType = new StringFloatDictionary();
    public List<GameObject> tvSeparatedPartsPrefabs;
    public List<GameObject> tvCompletedObjectPrefabs;
    public List<GameObject> tvHeaderPrefabs;
    public Transform tvBoxTransform;
    public Transform anchorTransform;
    public LineRenderer ropeLineRenderer;
    public GameObject[] screenObjects;
    public Transform pairedTVCameraSetup;

    private TMP_Text timeRemainingTextMesh;
    private TMP_Text mainEarningsTextMesh;
    private TMP_Text finalEarningsTextMesh;
    private Transform leftIndividualPartsTransform;
    private Transform rightIndividualPartsTransform;
    private Transform completedObjectTransform;
    private Transform headerTransform;
    private GameObject jobCompletedTextObject;
    private GameObject jobFailedTextObject;
    private Rigidbody tvRB;
    private SpringJoint springJoint;

    private List<string> jobPartTypes;

    private BuildJobManager buildJobManager;

    private Coroutine jobActiveCo;

    private bool jobIsActive = false;

    private float currentTimer;
    private float startingTimer;
    private float startingEarnings;
    private float currentEarnings;

    private const float rotateTVObjectsSpeed = 10.0f;

    private float jobTimeMultiplier = 5.0f;

    private const float baseJobTime = 20.0f;
    void Start() {
        jobPartTypes = new List<string>(jobPartPricingByType.Keys);

        buildJobManager = FindObjectOfType<BuildJobManager>();

        Vector3[] newRopePositions = new Vector3[] {
            tvBoxTransform.position,
            anchorTransform.position
        };

        leftIndividualPartsTransform = pairedTVCameraSetup.transform.GetChild(0);
        rightIndividualPartsTransform = pairedTVCameraSetup.transform.GetChild(1);
        completedObjectTransform = pairedTVCameraSetup.transform.GetChild(2);
        headerTransform = pairedTVCameraSetup.transform.GetChild(3);

        timeRemainingTextMesh = pairedTVCameraSetup.transform.GetChild(4).GetComponent<TMP_Text>();
        timeRemainingTextMesh.text = $"{Mathf.CeilToInt(0)}";

        jobCompletedTextObject = pairedTVCameraSetup.transform.GetChild(5).gameObject;
        finalEarningsTextMesh = jobCompletedTextObject.transform.GetChild(0).GetComponent<TMP_Text>();
        jobFailedTextObject = pairedTVCameraSetup.transform.GetChild(6).gameObject;

        mainEarningsTextMesh = pairedTVCameraSetup.transform.GetChild(7).GetComponent<TMP_Text>();

        pairedTVCameraSetup.gameObject.SetActive(false);

        ropeLineRenderer.positionCount = 2;
        ropeLineRenderer.SetPositions(newRopePositions);

        tvRB = tvBoxTransform.GetComponent<Rigidbody>();
        springJoint = tvBoxTransform.GetComponent<SpringJoint>();

        tvRB.mass = Random.Range(9.0f, 12.0f);
        //tvRB.drag = Random.Range(0.1f, 0.2f);
        //tvRB.angularDrag = Random.Range(0.1f, 0.2f);
        springJoint.spring = Random.Range(100.0f, 200.0f);

        // Start with screen off
        screenObjects[0].SetActive(false);
        screenObjects[1].SetActive(true);
    }

    void Update() {
        Vector3[] newRopePositions = new Vector3[] {
            tvBoxTransform.position,
            anchorTransform.position
        };

        ropeLineRenderer.positionCount = 2;
        ropeLineRenderer.SetPositions(newRopePositions);
    }

    public bool CheckIfJobIsActive() {
        return jobIsActive;
    }

    public void OnGameOver() {
        StopAllCoroutines();
	}

	public void OnJobStart(string jobPartType, int jobIndex) {
        int jobObjectIndex = jobPartTypes.FindIndex(x => x.Equals(jobPartType));
        if (jobObjectIndex < 0) {
            Debug.LogError($"TVBoxHandler did not identify job part {jobPartType}");
            return;
		}
        GameObject leftSeparatedPartsObject = Instantiate(tvSeparatedPartsPrefabs[jobObjectIndex], leftIndividualPartsTransform);
        leftSeparatedPartsObject.transform.localPosition = Vector3.zero;
        leftSeparatedPartsObject.transform.localRotation = Quaternion.identity;
        GameObject rightSeparatedPartsObject = Instantiate(tvSeparatedPartsPrefabs[jobObjectIndex], rightIndividualPartsTransform);
        rightSeparatedPartsObject.transform.localPosition = Vector3.zero;
        rightSeparatedPartsObject.transform.localRotation = Quaternion.identity;

        GameObject completedObjectObject = Instantiate(tvCompletedObjectPrefabs[jobObjectIndex], completedObjectTransform);
        completedObjectObject.transform.localPosition = Vector3.zero;
        completedObjectObject.transform.localRotation = Quaternion.identity;

        GameObject headerObject = Instantiate(tvHeaderPrefabs[jobObjectIndex], headerTransform);
        headerObject.transform.localPosition = Vector3.zero;
        headerObject.transform.localRotation = Quaternion.identity;

        pairedTVCameraSetup.gameObject.SetActive(true);

        currentEarnings = jobPartPricingByType[jobPartType];
        startingEarnings = currentEarnings;

        currentTimer = Mathf.CeilToInt(jobTimeMultiplier * (baseJobTime + startingEarnings / 4.0f));
        startingTimer = currentTimer;

        timeRemainingTextMesh.text = $"{Mathf.CeilToInt(currentTimer)}";
        mainEarningsTextMesh.text = GetEarningsString();

        // Turn screen on
        screenObjects[0].SetActive(true);
        screenObjects[1].SetActive(false);

        jobIsActive = true;
        jobActiveCo = StartCoroutine(JobActiveCo());
    }

    private string GetEarningsString() {
        string startingString = $"${System.Math.Round(currentEarnings, 2)}";
        // Make sure there are always two decimal places displayed
        if (startingString.Split('.').Length <= 1) {
            startingString += ".00";
        }
        else if (startingString.Split('.')[1].Length <= 1) {
            startingString += "0";
        }
        return startingString;
    }

    private void AnimateTVVisuals() {
        leftIndividualPartsTransform.Rotate(Vector3.up, rotateTVObjectsSpeed * Time.deltaTime);
        rightIndividualPartsTransform.Rotate(Vector3.up, rotateTVObjectsSpeed * Time.deltaTime);
        completedObjectTransform.Rotate(Vector3.up, rotateTVObjectsSpeed * Time.deltaTime);
    }

    private void RemoveGeneralJobVisuals() {
        Destroy(leftIndividualPartsTransform.GetChild(0).gameObject);
        Destroy(rightIndividualPartsTransform.GetChild(0).gameObject);
        Destroy(completedObjectTransform.GetChild(0).gameObject);
        Destroy(headerTransform.GetChild(0).gameObject);
    }

    private void ShutOffScreen() {
        // Turn screen off
        screenObjects[0].SetActive(false);
        screenObjects[1].SetActive(true);
        jobFailedTextObject.SetActive(false);
        jobCompletedTextObject.SetActive(false);
    }

    private void OnJobTimerFinished() {
        ShutOffScreen();
        buildJobManager.OnTVBoxJobBehaviorFinished(this);
    }

    public float OnJobSubmitted() {
        if (jobActiveCo != null) {
            StopCoroutine(jobActiveCo);
		}
        RemoveGeneralJobVisuals();
        mainEarningsTextMesh.text = "";
        finalEarningsTextMesh.text = GetEarningsString();
        jobCompletedTextObject.SetActive(true);

        jobIsActive = false;

        StartCoroutine(OnJobSubmittedCo());

        return currentEarnings;
    }

    public void SetTimeMultiplier(float newTimeMultiplier) {
        jobTimeMultiplier = newTimeMultiplier;
	}

    public float GetTimeMultiplier() {
        return jobTimeMultiplier;
	}

    private IEnumerator OnJobSubmittedCo() {
        yield return new WaitForSeconds(3.0f);

        OnJobTimerFinished();
    }

    private IEnumerator JobActiveCo() {
        while (currentTimer > 0) {
            AnimateTVVisuals();

            timeRemainingTextMesh.text = $"{Mathf.CeilToInt(currentTimer)}";
            mainEarningsTextMesh.text = GetEarningsString();

            currentTimer -= Time.deltaTime;
            currentEarnings = Mathf.Lerp(0.0f, startingEarnings, currentTimer / startingTimer);
            yield return null;
		}
        currentEarnings = 0.0f;
        currentTimer = 0.0f;
        timeRemainingTextMesh.text = "TIME UP";
        mainEarningsTextMesh.text = "$0.00";

        // Grace period
        yield return new WaitForSeconds(0.5f);

        jobIsActive = false;
        RemoveGeneralJobVisuals();
        jobFailedTextObject.SetActive(true);
        buildJobManager.OnJobFailed();
        mainEarningsTextMesh.text = "";

        yield return new WaitForSeconds(3.0f);

        OnJobTimerFinished();
    }
}
