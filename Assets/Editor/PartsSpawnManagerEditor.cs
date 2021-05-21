using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PartsSpawnManager))]
public class PartsSpawnManagerEditor : Editor {
    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        PartsSpawnManager partsSpawnManager = (PartsSpawnManager)target;
        if (GUILayout.Button("Generate Parts Pool")) {
            partsSpawnManager.GenerateAllInitialPartPools();
        }
    }
}
