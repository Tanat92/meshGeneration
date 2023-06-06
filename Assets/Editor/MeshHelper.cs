using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static MeshGeneration;

[CustomEditor(typeof(MeshGeneration))]
public class MeshHelper : Editor
{
    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("Generation"))
        {
            MeshGeneration meshGeneration = (MeshGeneration)target;
            meshGeneration.Generation();
        }
        base.OnInspectorGUI();
        if (GUI.changed)
        {
            serializedObject.ApplyModifiedProperties();
            MeshGeneration meshGeneration = (MeshGeneration)target;
            meshGeneration.Generation();
        }
    }
}
