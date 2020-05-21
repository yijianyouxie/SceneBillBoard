using UnityEngine;
using System.Collections;
using UnityEditor;

namespace ImposterSystem
{
    [CustomEditor(typeof(CameraDetector))]
    public class CameraDetectorEditor : Editor
    {
        SerializedProperty ignoreImposterSystem;
        SerializedProperty ignoreQueue;
        SerializedProperty _isVrMainCamera;
        SerializedProperty _leftEyeCamera;
        SerializedProperty _rightEyeCamera;

        private void OnEnable()
        {
            ignoreImposterSystem = serializedObject.FindProperty("ignoreImposterSystem");
            ignoreQueue = serializedObject.FindProperty("ignoreQueue");
            _isVrMainCamera = serializedObject.FindProperty("_isVrMainCamera");
            _leftEyeCamera = serializedObject.FindProperty("_leftEyeCamera");
            _rightEyeCamera = serializedObject.FindProperty("_rightEyeCamera");
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(ignoreImposterSystem, new GUIContent("Ignore ImposterSystem"));
            EditorGUILayout.PropertyField(ignoreQueue, new GUIContent("Ignore Queue"));
            //EditorGUILayout.PropertyField(_updateType, new GUIContent("Update on"), new GUILayoutOption[0]);
            EditorGUILayout.PropertyField(_isVrMainCamera, new GUIContent("Is VR main camera"));
            if (_isVrMainCamera.boolValue)
            {
                EditorGUILayout.HelpBox("Select left and right cameras WITHOUT attaching CameraDetector component to them", MessageType.Info);
                EditorGUILayout.PropertyField(_leftEyeCamera, new GUIContent("Left eye camera"));
                EditorGUILayout.PropertyField(_rightEyeCamera, new GUIContent("Right eye camera"));
            }
            base.serializedObject.ApplyModifiedProperties();
        }
    }
}
