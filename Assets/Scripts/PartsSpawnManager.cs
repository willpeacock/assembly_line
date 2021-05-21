using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameObjectListStorage : SerializableDictionary.Storage<List<GameObject>> { }

[System.Serializable]
public class PrefabToInstancesDictionary : SerializableDictionary<GameObject, List<GameObject>, GameObjectListStorage> { }

[System.Serializable]
public class GameObjectGameObjectDictionary : SerializableDictionary<GameObject, GameObject> { }

[System.Serializable]
public class GameObjectTransformDictionary : SerializableDictionary<GameObject, Transform> { }

public class PartsSpawnManager : MonoBehaviour {
    public GameObject[] allPartPrefabs;
    [HideInInspector]
    public PrefabToInstancesDictionary prefabToPartPool = new PrefabToInstancesDictionary();
    [HideInInspector]
    public PrefabToInstancesDictionary prefabToAvailablePartPool = new PrefabToInstancesDictionary();
    [HideInInspector]
    public GameObjectGameObjectDictionary partInstanceToPrefab = new GameObjectGameObjectDictionary();
    [HideInInspector]
    public GameObjectTransformDictionary prefabToPartPoolParent = new GameObjectTransformDictionary();
    [HideInInspector]
    public List<GameObject> uniquePartPrefabs = new List<GameObject>();
    public Transform partPoolsParent;

    public Transform spawnRegionFirst;
    public Transform spawnRegionSecond;

    private BuildJobManager jobManager;

    private float spawnTimeMultiplier = 0.8f;

    private const int INITIAL_POOL_SIZE_PER_PART = 10;

	private void Start() {
        if (uniquePartPrefabs == null || uniquePartPrefabs.Count <= 0) {
            GetUniquePartPrefabs();
        }

        jobManager = FindObjectOfType<BuildJobManager>();
    }

	private void GetUniquePartPrefabs() {
        uniquePartPrefabs = new List<GameObject>();

        foreach (GameObject partPrefab in allPartPrefabs) {
            if (!uniquePartPrefabs.Contains(partPrefab)) {
                uniquePartPrefabs.Add(partPrefab);
            }
		}
    }

    public void GenerateAllInitialPartPools() {
        Debug.Log("GenerateAllInitialPartPools()");

        if (uniquePartPrefabs == null || uniquePartPrefabs.Count <= 0) {
            GetUniquePartPrefabs();
        }

        // Destroy any existing part pools
        if (partPoolsParent.childCount > 0) {
            for (int i = 0; i < partPoolsParent.childCount; i++) {
                DestroyImmediate(partPoolsParent.transform.GetChild(0).gameObject);
			}
		}

        GameObject allPartPoolsObject = new GameObject();
        allPartPoolsObject.name = "all_part_pools";
        Transform allPartPoolsTransform = allPartPoolsObject.transform;
        allPartPoolsTransform.SetParent(partPoolsParent);
        allPartPoolsTransform.localPosition = Vector3.zero;
        allPartPoolsTransform.localRotation = Quaternion.identity;
        allPartPoolsTransform.localScale = Vector3.one;

        prefabToPartPool = new PrefabToInstancesDictionary();
        prefabToAvailablePartPool = new PrefabToInstancesDictionary();
        prefabToPartPoolParent = new GameObjectTransformDictionary();
        for (int i = 0; i < uniquePartPrefabs.Count; i++) {
            GameObject partPrefab = uniquePartPrefabs[i];

            prefabToPartPool.Add(partPrefab, new List<GameObject>());
            prefabToAvailablePartPool.Add(partPrefab, new List<GameObject>());

            // Generate a parent transform for each pool type
            GameObject newPartPoolParent = new GameObject($"{partPrefab.name}_pool");
            Transform newPartPoolParentTransform = newPartPoolParent.transform;
            newPartPoolParentTransform.SetParent(allPartPoolsTransform);
            float initialXOffset = -(uniquePartPrefabs.Count / 2) + i;
            newPartPoolParentTransform.localPosition = new Vector3(initialXOffset, -30.0f, 0.0f);
            newPartPoolParentTransform.localRotation = Quaternion.identity;
            newPartPoolParentTransform.localScale = Vector3.one;

            prefabToPartPoolParent.Add(partPrefab, newPartPoolParentTransform);
        }

        partInstanceToPrefab = new GameObjectGameObjectDictionary();
        // Take into account default spawn rates when determining how many to spawn
        for (int j = 0; j < allPartPrefabs.Length; j++) {
            GameObject partPrefab = allPartPrefabs[j];

            for (int i = 0; i < INITIAL_POOL_SIZE_PER_PART; i++) {
                GenerateNewPrefabInstance(partPrefab);
            }
        }

        // Make duplicates of all lists to keep track of what instances are available for use
        foreach (GameObject partPrefab in prefabToPartPool.Keys) {
            prefabToAvailablePartPool[partPrefab] = new List<GameObject>(prefabToPartPool[partPrefab]);
        }
    }

    private GameObject GenerateNewPrefabInstance(GameObject partPrefab) {
        //Debug.Log($"GenerateNewPrefabInstance({partPrefab.name})");

        List<GameObject> partPoolInstances = prefabToPartPool[partPrefab];
        Transform partPoolParent = prefabToPartPoolParent[partPrefab];

        GameObject newPartInstance = Instantiate(partPrefab, partPoolParent);

        newPartInstance.name = $"{partPrefab.name}_instance_{partPoolInstances.Count}";

        Transform newPartInstanceTransform = newPartInstance.transform;
        newPartInstanceTransform.localPosition = Vector3.zero;
        newPartInstanceTransform.localRotation = Quaternion.identity;

        partPoolInstances.Add(newPartInstance);

        partInstanceToPrefab.Add(newPartInstance, partPrefab);

        return newPartInstance;
    }

	public void BeginSpawning() {
        StartCoroutine(MainSpawnCo());
    }

    public void OnGameOver() {
        StopAllCoroutines();
	}

    public void SetSpawnTimeMultiplier(float newMultiplier) {
        spawnTimeMultiplier = newMultiplier;
    }

    public float GetSpawnTimeMultiplier() {
        return spawnTimeMultiplier;
    }

    public GameObject PickPrefabToSpawn() {
        GameObject selectedPrefab = jobManager.AttemptGetJobPartPrefabToSpawn();
        // If nothing selected by job manager, just grab from the general pool
        if (selectedPrefab == null) {
            selectedPrefab = allPartPrefabs[Random.Range(0, allPartPrefabs.Length)];
        }
        return selectedPrefab;
    }

    public void OnPartInstanceDisabled(GameObject partInstance) {
        if (!partInstanceToPrefab.ContainsKey(partInstance)) {
            Debug.LogError($"PartsSpawnManager did not find associated prefab with instance: '{partInstance}'");
            return;
		}

        GameObject sourcePrefab = partInstanceToPrefab[partInstance];

        if (!prefabToAvailablePartPool.ContainsKey(sourcePrefab)) {
            Debug.LogError($"PartsSpawnManager did not have associated available instance pool with prefab: '{sourcePrefab}'");
            return;
        }
        List<GameObject> availablePartPool = prefabToAvailablePartPool[sourcePrefab];

        availablePartPool.Add(partInstance);
    }

    private GameObject SelectInstanceToSpawn(GameObject partPrefab, ref bool generatedNewInstance) {
        if (!prefabToAvailablePartPool.ContainsKey(partPrefab)) {
            Debug.LogError($"PartsSpawnManager did not have associated available instance pool with prefab: '{partPrefab}'");
            return null;
        }
        List<GameObject> availablePartPool = prefabToAvailablePartPool[partPrefab];

        if (availablePartPool.Count <= 0) {
            generatedNewInstance = true;
            GameObject newPartInstance = GenerateNewPrefabInstance(partPrefab);

            return newPartInstance;
        }
        else {
            GameObject selectedPartInstance = availablePartPool[0];
            availablePartPool.RemoveAt(0);

            return selectedPartInstance;
        }
    }

    private void SpawnInPrefabInstanceAtRegion(GameObject selectedPrefab, Transform spawnRegion) {
        bool generatedNewInstance = false;
        GameObject spawnedPartInstance = SelectInstanceToSpawn(selectedPrefab, ref generatedNewInstance);
        Transform spawnedPartInstanceTransform = spawnedPartInstance.transform;

        spawnedPartInstanceTransform.position = spawnRegion.position;
        spawnedPartInstanceTransform.rotation = Quaternion.Euler(
            Random.Range(0.0f, 360.0f), Random.Range(0.0f, 360.0f), Random.Range(0.0f, 360.0f));

        ConveyorPartHandler spawnedPartHandler = spawnedPartInstance.GetComponent<ConveyorPartHandler>();
        if (spawnedPartHandler == null) {
            Debug.LogError($"{spawnedPartInstance.name}: Did not find ConveyorPartHandler in selected instance within PartsSpawnManager");
            return;
		}
        if (generatedNewInstance) {
            spawnedPartHandler.InitializeHandler();
        }
        spawnedPartHandler.OnPartEnabled();
    }

    private IEnumerator MainSpawnCo() {
        while (isActiveAndEnabled) {
            yield return new WaitForSeconds(0.3f * spawnTimeMultiplier);
            SpawnInPrefabInstanceAtRegion(PickPrefabToSpawn(), spawnRegionFirst);
            yield return new WaitForSeconds(0.1f * spawnTimeMultiplier);
            SpawnInPrefabInstanceAtRegion(PickPrefabToSpawn(), spawnRegionSecond);
        }
	}
}
