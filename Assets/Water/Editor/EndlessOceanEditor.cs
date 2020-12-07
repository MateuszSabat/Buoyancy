using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PhysWater
{
    [CustomEditor(typeof(EndlessOcean))]
    public class EndlessOceanEditor : Editor
    {
        EndlessOcean ocean;

        bool materialToggle;
        bool chunkToggle;
        bool waveToggle;

        SerializedProperty shadingMode;
      //  SerializedProperty renderMode;
        SerializedProperty mainColor;
        SerializedProperty metallic;
        SerializedProperty smoothness;
        SerializedProperty alphaSwallow;
        SerializedProperty alphaDeep;
        SerializedProperty maxDepthAlpha;
        SerializedProperty observer;
        SerializedProperty size;
        SerializedProperty chunkPrefab;
        SerializedProperty chunkSize;
        SerializedProperty vertexDistance;
        SerializedProperty amplitude;
        SerializedProperty inverseLength;
        SerializedProperty frequency;
        SerializedProperty noiseStrength;
        SerializedProperty noiseFrequency;


        private void OnEnable()
        {
            ocean = (EndlessOcean)target;
            materialToggle = false;
            chunkToggle = false;
            waveToggle = false;

            shadingMode = serializedObject.FindProperty("shadingMode");
            observer = serializedObject.FindProperty("observer");
            size = serializedObject.FindProperty("size");
            chunkPrefab = serializedObject.FindProperty("chunkPrefab");
            chunkSize = serializedObject.FindProperty("chunkSize");
            vertexDistance = serializedObject.FindProperty("vertexDistance");
            amplitude = serializedObject.FindProperty("amplitude");
            inverseLength = serializedObject.FindProperty("inverseLength");
            frequency = serializedObject.FindProperty("frequency");
            noiseStrength =  serializedObject.FindProperty("noiseStrength");
            noiseFrequency =  serializedObject.FindProperty("noiseFrequency");
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("Main");
            EditorGUILayout.PropertyField(shadingMode);
            EditorGUILayout.PropertyField(observer);

            chunkToggle = EditorGUILayout.BeginToggleGroup("Chunk", chunkToggle);
            if (chunkToggle)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(size);
                EditorGUILayout.PropertyField(chunkPrefab, new GUIContent("Chunk Prefab"));
                EditorGUILayout.PropertyField(chunkSize);
                EditorGUILayout.PropertyField(vertexDistance);
                EditorGUI.indentLevel--;
                EditorGUILayout.Space(5f);
            }
            EditorGUILayout.EndToggleGroup();

            waveToggle = EditorGUILayout.BeginToggleGroup(new GUIContent("Wave"), waveToggle);
            if (waveToggle)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(amplitude);
                EditorGUILayout.PropertyField(frequency);
                EditorGUILayout.PropertyField(inverseLength);EditorGUILayout.Space(5f);
                EditorGUILayout.LabelField(new GUIContent("Noise"), EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(noiseStrength, new GUIContent("Strength"));
                EditorGUILayout.PropertyField(noiseFrequency, new GUIContent("Frequency"));
                EditorGUI.indentLevel -= 2;
            }
            EditorGUILayout.EndToggleGroup();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
