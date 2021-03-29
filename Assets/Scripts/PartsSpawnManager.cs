using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PartsSpawnManager : MonoBehaviour {
    public GameObject[] allPartPrefabs;
    public Transform spawnRegionFirst;
    public Transform spawnRegionSecond;

    private Transform allSpawnedPartsParent;

    private float spawnTimeMultiplier = 0.8f;

    public void BeginSpawning() {
        allSpawnedPartsParent = transform.GetChild(0);
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

    private IEnumerator MainSpawnCo() {
        while (isActiveAndEnabled) {
            yield return new WaitForSeconds(0.3f * spawnTimeMultiplier);
            Transform firstRegionPart = Instantiate(allPartPrefabs[Random.Range(0, allPartPrefabs.Length)],
                spawnRegionFirst.position,
                Quaternion.Euler(Random.Range(0.0f, 360.0f), Random.Range(0.0f, 360.0f), Random.Range(0.0f, 360.0f))).transform;
            firstRegionPart.parent = allSpawnedPartsParent;
            yield return new WaitForSeconds(0.1f * spawnTimeMultiplier);
            Transform secondRegionPart = Instantiate(allPartPrefabs[Random.Range(0, allPartPrefabs.Length)],
                spawnRegionSecond.position,
                Quaternion.Euler(Random.Range(0.0f, 360.0f), Random.Range(0.0f, 360.0f), Random.Range(0.0f, 360.0f))).transform;
            secondRegionPart.parent = allSpawnedPartsParent;
        }
	}
}
