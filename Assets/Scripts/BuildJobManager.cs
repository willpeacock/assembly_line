using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

public class BuildJobManager : MonoBehaviour {
    public List<string> jobPartTypes;
    public TVBoxHandler[] tvBoxHandlers;
    public GameObject earningsHeaderObject;
    public GameObject gameOverHeaderObject;
    public Transform[] strikeTransforms;
    public TMP_Text totalEarningsText;
    public ConveyorBeltHandler[] conveyorBeltHandlers;
    public GameObject[] mainTVComponentObjects;
    public PartsSpawnManager spawnManager;
    public XRNode rightController;
    public XRNode leftController;
    public AudioSource mainMusicAudioSource;

    public List<string> activeJobPartTypes;
    private List<TVBoxHandler> activeTVBoxHandlers;

    private List<TVBoxHandler> availableTVBoxHandlers;

    private Vector2 delayBeforeNextJobRange = new Vector2(15.0f, 20.0f);

    private Coroutine mainJobLaunchingCo;

    private float totalEarnings = 0.0f;
    private int numOfStrikes = 0;
    private bool gameIsActive = false;
    private bool gameHasLaunched = false;
    private bool isReloadingScene = false;

    private float currentConveyorSpeed = 0.0f;
    private float currentTimeMultiplier = 0.0f;
    private float currentSpawnTimeMultiplier = 0.0f;

    private InputDevice rightControllerDevice;
    private InputDevice leftControllerDevice;

    private GeneralSoundEffectPlayer soundEffectPlayer;

    private const int maxActiveJobCount = 6;
    void Start() {
        soundEffectPlayer = FindObjectOfType<GeneralSoundEffectPlayer>();

        activeJobPartTypes = new List<string>();
        activeTVBoxHandlers = new List<TVBoxHandler>();

        availableTVBoxHandlers = new List<TVBoxHandler>();
        foreach (TVBoxHandler tvBoxHandler in tvBoxHandlers) {
            availableTVBoxHandlers.Add(tvBoxHandler);
        }

        currentConveyorSpeed = conveyorBeltHandlers[0].GetSpeed();
        foreach (ConveyorBeltHandler beltHandler in conveyorBeltHandlers) {
            beltHandler.SetSpeed(0);
        }

        currentSpawnTimeMultiplier = spawnManager.GetSpawnTimeMultiplier();

        currentTimeMultiplier = tvBoxHandlers[0].GetTimeMultiplier();

        mainTVComponentObjects[0].SetActive(true);
        mainTVComponentObjects[1].SetActive(false);
        mainTVComponentObjects[2].SetActive(false);

        rightControllerDevice = InputDevices.GetDeviceAtXRNode(rightController);
        leftControllerDevice = InputDevices.GetDeviceAtXRNode(leftController);
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.Escape)) {
            Application.Quit();
		}

        if (Application.isEditor) {
            if (activeJobPartTypes.Count < maxActiveJobCount && Input.GetKeyDown("1")) {
                BeginNextJob();
			}
		}

        // Wait for input to launch game
        if (!gameHasLaunched) {
            TryGetInputDevices();
            bool buttonPressed = false;

            if (rightControllerDevice != null && rightControllerDevice.isValid) {
                rightControllerDevice.TryGetFeatureValue(CommonUsages.primaryButton, out buttonPressed);
                if (!buttonPressed) {
                    rightControllerDevice.TryGetFeatureValue(CommonUsages.secondaryButton, out buttonPressed);
                }
            }
            if (!buttonPressed && leftControllerDevice != null && leftControllerDevice.isValid) {
                leftControllerDevice.TryGetFeatureValue(CommonUsages.primaryButton, out buttonPressed);
                if (!buttonPressed) {
                    leftControllerDevice.TryGetFeatureValue(CommonUsages.secondaryButton, out buttonPressed);
                }
            }

            if (buttonPressed) {
                BeginGame();

            }
		}
        // Wait for input on game over
        else if (!gameIsActive && !isReloadingScene) {
            TryGetInputDevices();
            float rightControllerTrigger = 0.0f;
            float leftControllerTrigger = 0.0f;

            if (rightControllerDevice != null && rightControllerDevice.isValid) {
                rightControllerDevice.TryGetFeatureValue(CommonUsages.trigger, out rightControllerTrigger);
            }
            if (leftControllerDevice != null && leftControllerDevice.isValid) {
                leftControllerDevice.TryGetFeatureValue(CommonUsages.trigger, out leftControllerTrigger);
            }

            if (rightControllerTrigger >= 0.4f || leftControllerTrigger >= 0.4f
                || Input.GetKeyDown(KeyCode.Space)) {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                isReloadingScene = true;
            }
		}
    }

    private void TryGetInputDevices() {
        if (rightControllerDevice == null || !rightControllerDevice.isValid) {
            rightControllerDevice = InputDevices.GetDeviceAtXRNode(rightController);
        }
        if (leftControllerDevice == null || !leftControllerDevice.isValid) {
            leftControllerDevice = InputDevices.GetDeviceAtXRNode(leftController);
        }
    }

    public void BeginGame() {
        gameHasLaunched = true;
        gameIsActive = true;

        mainTVComponentObjects[0].SetActive(false);
        mainTVComponentObjects[1].SetActive(true);
        mainTVComponentObjects[2].SetActive(false);

        foreach (ConveyorBeltHandler beltHandler in conveyorBeltHandlers) {
            beltHandler.SetSpeed(currentConveyorSpeed);
        }

        spawnManager.BeginSpawning();

        mainJobLaunchingCo = StartCoroutine(MainJobLaunchingCo());
    }

    private void BeginNextJob() {
        List<string> availableJobPartTypes = new List<string>(jobPartTypes);
        foreach (string jobPartType in jobPartTypes) {
            // If there is at least one active job of this type
            if (activeJobPartTypes.Contains(jobPartType)) {
                // If there are two or more active jobs of this type
                List<string> matchingJobs = activeJobPartTypes.FindAll(x => x.Equals(jobPartType));

                if (matchingJobs.Count >= 2) {
                    availableJobPartTypes.Remove(jobPartType);
                }
            }
		}

        TVBoxHandler nextTVBoxHandler = availableTVBoxHandlers[0];
        availableTVBoxHandlers.RemoveAt(0);

        soundEffectPlayer.PlayBellSound();

        string nextJobPartType = availableJobPartTypes[Random.Range(0, availableJobPartTypes.Count)];
        activeJobPartTypes.Add(nextJobPartType);
        activeTVBoxHandlers.Add(nextTVBoxHandler);

        nextTVBoxHandler.OnJobStart(nextJobPartType, activeJobPartTypes.Count-1);
    }

    public void OnTVBoxJobBehaviorFinished(TVBoxHandler jobTVBoxHandler) {

        int jobIndex = activeTVBoxHandlers.FindIndex(x => x == jobTVBoxHandler);
        availableTVBoxHandlers.Add(jobTVBoxHandler);

        activeJobPartTypes.RemoveAt(jobIndex);
        activeTVBoxHandlers.Remove(jobTVBoxHandler);
    }

    private void OnGameOver() {
        if (mainJobLaunchingCo != null) {
            StopCoroutine(mainJobLaunchingCo);
		}

        mainMusicAudioSource.pitch = 0.7f;

        spawnManager.OnGameOver();

        earningsHeaderObject.SetActive(false);
        gameOverHeaderObject.SetActive(true);

        foreach (TVBoxHandler boxHandler in tvBoxHandlers) {
            boxHandler.OnGameOver();
		}

        foreach (ConveyorBeltHandler beltHandler in conveyorBeltHandlers) {
            beltHandler.OnGameOver();
		}

        gameIsActive = false;
	}

    public void OnJobFailed() {
        if (!gameIsActive) {
            return;
		}

        soundEffectPlayer.PlayFailureSound();

        numOfStrikes++;

        // Add a red strike to the main screen
        strikeTransforms[numOfStrikes - 1].GetChild(0).gameObject.SetActive(false);
        strikeTransforms[numOfStrikes - 1].GetChild(1).gameObject.SetActive(true);

        if (numOfStrikes >= 3) {
            OnGameOver();
        }

    }

    public bool CheckIfValidJobObject(string objectType) {
        if (!activeJobPartTypes.Contains(objectType)) {
            return false;
		}
        // Otherwise, check all occurances of the object type in job list
        for (int i = 0; i < activeJobPartTypes.Count; i++) {
            if (activeJobPartTypes[i].Equals(objectType)) {
                TVBoxHandler jobTVBoxHandler = activeTVBoxHandlers[i];

                if (jobTVBoxHandler.CheckIfJobIsActive()) {
                    return true;
				}
            }
		}

        return false;
	}

    public void OnValidObjectSubmitted(ConveyorPartHandler objectPartHandler) {
        if (!gameIsActive) {
            return;
        }

        string objectType = objectPartHandler.GetPartType();
        if (!activeJobPartTypes.Contains(objectType)) {
            return;
        }
        TVBoxHandler jobTVBoxHandler = null;
        // Otherwise, find first valid occurances of the object type in job list
        for (int i = 0; i < activeJobPartTypes.Count; i++) {
            if (activeJobPartTypes[i].Equals(objectType)) {
                TVBoxHandler currentJobTVBoxHandler = activeTVBoxHandlers[i];

                if (currentJobTVBoxHandler.CheckIfJobIsActive()) {
                    jobTVBoxHandler = currentJobTVBoxHandler;
                    break;
                }
            }
        }
        if (jobTVBoxHandler == null) {
            Debug.LogWarning("WARNING - Object considered valid was not labeled as active"
                 + $"by any TV box handlers, object='{objectPartHandler.gameObject.name}'");
            return;
		}

        objectPartHandler.OnValidObjectSubmitted();

        totalEarnings += jobTVBoxHandler.OnJobSubmitted();

        totalEarningsText.text = GetEarningsString();

        soundEffectPlayer.PlaySuccessSound();

        currentConveyorSpeed *= 1.05f;
        foreach (ConveyorBeltHandler beltHandler in conveyorBeltHandlers) {
            beltHandler.SetSpeed(currentConveyorSpeed);
		}

        currentSpawnTimeMultiplier *= 0.95f;
        spawnManager.SetSpawnTimeMultiplier(currentSpawnTimeMultiplier);

        currentTimeMultiplier *= 0.9f;
        foreach (TVBoxHandler boxHandler in tvBoxHandlers) {
            boxHandler.SetTimeMultiplier(currentTimeMultiplier);
        }

        Debug.Log($"<color=cyan>SUBMITTED JOB for {objectType}</color>");
    }

    private string GetEarningsString() {
        string startingString = $"${System.Math.Round(totalEarnings, 2)}";
        // Make sure there are always two decimal places displayed
        if (startingString.Split('.').Length <= 1) {
            startingString += ".00";
        }
        else if (startingString.Split('.')[1].Length <= 1) {
            startingString += "0";
        }
        return startingString;
    }

    private IEnumerator MainJobLaunchingCo() {
        mainMusicAudioSource.pitch = 2.0f;
        Time.timeScale = 2.0f;

        foreach (ConveyorBeltHandler beltHandler in conveyorBeltHandlers) {
            beltHandler.SetSpeed(2.0f);
        }
        spawnManager.SetSpawnTimeMultiplier(1.0f / 4.0f);

        yield return new WaitForSecondsRealtime(5.0f);

        mainMusicAudioSource.pitch = 1.0f;
        Time.timeScale = 1.0f;

        foreach (ConveyorBeltHandler beltHandler in conveyorBeltHandlers) {
            beltHandler.SetSpeed(currentConveyorSpeed);
        }
        spawnManager.SetSpawnTimeMultiplier(currentSpawnTimeMultiplier);

        mainTVComponentObjects[0].SetActive(false);
        mainTVComponentObjects[1].SetActive(false);
        mainTVComponentObjects[2].SetActive(true);


        yield return new WaitForSeconds(2.0f);

        while (isActiveAndEnabled) {
            if (activeJobPartTypes.Count < maxActiveJobCount) {
                BeginNextJob();
            }
            else {
                /* If all jobs are taken, wait until jobs are available before starting the delay again */
                while (activeJobPartTypes.Count >= maxActiveJobCount) {
                    yield return null;
                }
            }

            yield return new WaitForSeconds(Random.Range(delayBeforeNextJobRange.x, delayBeforeNextJobRange.y));
        }
    }
}
