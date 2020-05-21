using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.AnimatedValues;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Events;
using UnityEditor;

namespace ImposterSystem
{
    [CustomEditor(typeof(ImposterController))]
    internal class ImposterControllerEditor : Editor
    {
        private class LODAction
        {
            public delegate void Callback();

            private readonly float m_Percentage;

            private readonly List<LODGroupGUI.LODInfo> m_LODs;

            private readonly Vector2 m_ClickedPosition;

            private readonly SerializedObject m_ObjectRef;

            private readonly SerializedProperty m_LODsProperty;

            private readonly ImposterControllerEditor.LODAction.Callback m_Callback;

            public LODAction(List<LODGroupGUI.LODInfo> lods, float percentage, Vector2 clickedPosition,
                SerializedProperty propLODs, ImposterControllerEditor.LODAction.Callback callback)
            {
                this.m_LODs = lods;
                this.m_Percentage = percentage;
                this.m_ClickedPosition = clickedPosition;
                this.m_LODsProperty = propLODs;
                this.m_ObjectRef = propLODs.serializedObject;
                this.m_Callback = callback;
            }

            public void InsertLOD()
            {
                if (this.m_LODsProperty.isArray)
                {
                    int num = -1;
                    foreach (LODGroupGUI.LODInfo current in this.m_LODs)
                    {
                        if (this.m_Percentage > current.RawScreenPercent)
                        {
                            num = current.LODLevel;
                            break;
                        }
                    }

                    if (num < 0)
                    {
                        this.m_LODsProperty.InsertArrayElementAtIndex(this.m_LODs.Count);
                        num = this.m_LODs.Count;
                    }
                    else
                    {
                        this.m_LODsProperty.InsertArrayElementAtIndex(num);
                    }

                    SerializedProperty serializedProperty =
                        this.m_ObjectRef.FindProperty(string.Format("m_LODs.Array.data[{0}].renderers", num));
                    serializedProperty.arraySize = 0;
                    SerializedProperty arrayElementAtIndex = this.m_LODsProperty.GetArrayElementAtIndex(num);
                    arrayElementAtIndex.FindPropertyRelative("screenRelativeTransitionHeight").floatValue =
                        this.m_Percentage;
                    if (this.m_Callback != null)
                    {
                        this.m_Callback();
                    }

                    this.m_ObjectRef.ApplyModifiedProperties();
                }
            }

            public void DeleteLOD()
            {
                if (this.m_LODs.Count > 0)
                {
                    foreach (LODGroupGUI.LODInfo current in this.m_LODs)
                    {
                        int arraySize = this.m_ObjectRef
                            .FindProperty(string.Format("m_LODs.Array.data[{0}].renderers", current.LODLevel))
                            .arraySize;
                        if (current.m_RangePosition.Contains(this.m_ClickedPosition) &&
                            (arraySize == 0 || EditorUtility.DisplayDialog("Delete LOD",
                                 "Are you sure you wish to delete this LOD?", "Yes", "No")))
                        {
                            SerializedProperty serializedProperty =
                                this.m_ObjectRef.FindProperty(string.Format("m_LODs.Array.data[{0}]",
                                    current.LODLevel));
                            serializedProperty.DeleteCommand();
                            this.m_ObjectRef.ApplyModifiedProperties();
                            if (this.m_Callback != null)
                            {
                                this.m_Callback();
                            }

                            break;
                        }
                    }
                }
            }
        }

        private class LODLightmapScale
        {
            public readonly float m_Scale;

            public readonly List<SerializedProperty> m_Renderers;

            public LODLightmapScale(float scale, List<SerializedProperty> renderers)
            {
                this.m_Scale = scale;
                this.m_Renderers = renderers;
            }
        }

        private int m_SelectedLODSlider = -1;

        private int m_SelectedLOD = -1;

        private int m_NumberOfLODs;

        private ImposterController m_ImposterController;

        private bool m_IsPrefab;

        //		private SerializedProperty m_FadeMode;

        private SerializedProperty m_AnimateCrossFading;

        private SerializedProperty m_LODs;

        private AnimBool m_ShowAnimateCrossFading = new AnimBool();

        private AnimBool m_ShowFadeTransitionWidth = new AnimBool();

        private Vector3 m_LastCameraPos = Vector3.zero;

        private const string kLODDataPath = "m_LODs.Array.data[{0}]";

        private const string kPixelHeightDataPath = "m_LODs.Array.data[{0}].screenRelativeTransitionHeight";

        private const string kRenderRootPath = "m_LODs.Array.data[{0}].renderers";

        private const string kFadeTransitionWidthDataPath = "m_LODs.Array.data[{0}].fadeTransitionWidth";

        private readonly int m_LODSliderId = "LODSliderIDHash".GetHashCode();

        private readonly int m_CameraSliderId = "LODCameraIDHash".GetHashCode();

        private PreviewRenderUtility m_PreviewUtility;

        private static readonly GUIContent[] kSLightIcons = new GUIContent[2];

        SerializedProperty _isStatic;
        SerializedProperty alwaysLookAtCamera;
        SerializedProperty updateBehavior;
        SerializedProperty errorCameraAngle;
        SerializedProperty useErrorLightAngle;
        SerializedProperty errorLightAngle;
        SerializedProperty errorDistance;
        SerializedProperty ZOffset;
        SerializedProperty useUpdateByTime;
        SerializedProperty timeInterval;

        private int activeLOD
        {
            get { return this.m_SelectedLOD; }
        }

        private void OnScenePrefabsUpdated(GameObject instance)
        {
            ImposterController bc = null;
            if ((bc = instance.GetComponent<ImposterController>()) != null)
                foreach (OriginalGOController t in bc.m_LODs[0].renderers)
                    t.Show();
        }

        private void OnEnable()
        {
            PrefabUtility.prefabInstanceUpdated = OnScenePrefabsUpdated;

            GameObject imposterPreviewGO;
            string previewGOName = "ImposterPreviewGO";
            if ((imposterPreviewGO = GameObject.Find(previewGOName)) != null)
            {
                DestroyImmediate(imposterPreviewGO);
            }

            //			this.m_FadeMode = base.serializedObject.FindProperty("m_FadeMode");
            //			this.m_AnimateCrossFading = base.serializedObject.FindProperty("m_AnimateCrossFading");
            if (serializedObject == null)
                return;
            this.m_LODs = serializedObject.FindProperty("m_LODs");

            if (m_LODs == null)
            {
                Debug.LogError("SHIT!");
            }

            _isStatic = serializedObject.FindProperty("_isStatic");
            //			_imposterOrientation = serializedObject.FindProperty ("_imposterOrientation");
            alwaysLookAtCamera = serializedObject.FindProperty("alwaysLookAtCamera");
            updateBehavior = serializedObject.FindProperty("updateBehavior");
            errorCameraAngle = serializedObject.FindProperty("errorCameraAngle");
            useErrorLightAngle = serializedObject.FindProperty("useErrorLightAngle");
            errorLightAngle = serializedObject.FindProperty("errorLightAngle");
            errorDistance = serializedObject.FindProperty("errorDistance");
            ZOffset = serializedObject.FindProperty("ZOffset");
            useUpdateByTime = serializedObject.FindProperty("useUpdateByTime");
            timeInterval = serializedObject.FindProperty("timeInterval");
            //			center = serializedObject.FindProperty ("center");
            //			size = serializedObject.FindProperty ("size");
            //			quadSize = serializedObject.FindProperty ("quadSize");
            //			this.m_ShowAnimateCrossFading.value = (this.m_FadeMode.intValue != 0);
            this.m_ShowAnimateCrossFading.valueChanged.AddListener(new UnityAction(base.Repaint));
            this.m_ShowFadeTransitionWidth.value = false;
            this.m_ShowFadeTransitionWidth.valueChanged.AddListener(new UnityAction(base.Repaint));
            EditorApplication.update = (EditorApplication.CallbackFunction) Delegate.Combine(EditorApplication.update,
                new EditorApplication.CallbackFunction(this.Update));
            this.m_ImposterController = (ImposterController) base.target;
            PrefabType prefabType = PrefabUtility.GetPrefabType(this.m_ImposterController.gameObject);
            this.m_IsPrefab = (prefabType == PrefabType.Prefab || prefabType == PrefabType.ModelPrefab);
            base.Repaint();
        }

        private void OnDisable()
        {
            PrefabUtility.prefabInstanceUpdated = null;
            EditorApplication.update = (EditorApplication.CallbackFunction) Delegate.Remove(EditorApplication.update,
                new EditorApplication.CallbackFunction(this.Update));
            this.m_ShowAnimateCrossFading.valueChanged.RemoveListener(new UnityAction(base.Repaint));
            this.m_ShowFadeTransitionWidth.valueChanged.RemoveListener(new UnityAction(base.Repaint));
            ShowOriginalGO(0);
            GameObject imposterPreviewGO;
            string previewGOName = "ImposterPreviewGO";
            if ((imposterPreviewGO = GameObject.Find(previewGOName)) != null)
            {
                DestroyImmediate(imposterPreviewGO);
            }
        }

        private static Rect CalculateScreenRect(IEnumerable<Vector3> points)
        {
            List<Vector2> list = (from p in points
                select HandleUtility.WorldToGUIPoint(p)).ToList<Vector2>();
            Vector2 vector = new Vector2(3.40282347E+38f, 3.40282347E+38f);
            Vector2 vector2 = new Vector2(-3.40282347E+38f, -3.40282347E+38f);
            foreach (Vector2 current in list)
            {
                vector.x = ((current.x >= vector.x) ? vector.x : current.x);
                vector2.x = ((current.x <= vector2.x) ? vector2.x : current.x);
                vector.y = ((current.y >= vector.y) ? vector.y : current.y);
                vector2.y = ((current.y <= vector2.y) ? vector2.y : current.y);
            }

            return new Rect(vector.x, vector.y, vector2.x - vector.x, vector2.y - vector.y);
        }

        /// <summary>
        /// Used to draw lod bounds on scene camera
        /// </summary>
        public void OnSceneGUI()
        {
            if (Selection.objects.Length != 1)
                return;
            if (!Application.isPlaying && SceneView.currentDrawingSceneView.camera != null)
                RenderForSceneCamera(SceneView.currentDrawingSceneView.camera);
            //			if (Event.current.type == EventType.Repaint && !(Camera.current == null) && !(SceneView.lastActiveSceneView != SceneView.currentDrawingSceneView))
            //			{
            //				Camera camera = SceneView.lastActiveSceneView.camera;
            //				Vector3 vector = LODUtility.CalculateWorldReferencePoint(this.m_LODGroup);
            //				if (Vector3.Dot(camera.transform.forward, (camera.transform.position - vector).normalized) <= 0f)
            //				{
            //					LODVisualizationInformation lODVisualizationInformation = LODUtility.CalculateVisualizationData(camera, this.m_LODGroup, -1);
            //					float worldSpaceSize = lODVisualizationInformation.worldSpaceSize;
            //					Handles.color = ((lODVisualizationInformation.activeLODLevel == -1) ? LODGroupGUI.kCulledLODColor : LODGroupGUI.kLODColors[lODVisualizationInformation.activeLODLevel]);
            //					Handles.SelectionFrame(0, vector, camera.transform.rotation, worldSpaceSize / 2f);
            //					Vector3 b = camera.transform.right * worldSpaceSize / 2f;
            //					Vector3 b2 = camera.transform.up * worldSpaceSize / 2f;
            //					Rect position = LODGroupEditor.CalculateScreenRect(new Vector3[]
            //						{
            //							vector - b + b2,
            //							vector - b - b2,
            //							vector + b + b2,
            //							vector + b - b2
            //						});
            //					float num = position.x + position.width / 2f;
            //					position = new Rect(num - 100f, position.yMax, 200f, 45f);
            //					if (position.yMax > (float)(Screen.height - 45))
            //					{
            //						position.y = (float)(Screen.height - 45 - 40);
            //					}
            //					Handles.BeginGUI();
            //					GUI.Label(position, GUIContent.none, EditorStyles.notificationBackground);
            //					EditorGUI.DoDropShadowLabel(position, GUIContent.Temp((lODVisualizationInformation.activeLODLevel < 0) ? "Culled" : ("LOD " + lODVisualizationInformation.activeLODLevel)), LODGroupGUI.Styles.m_LODLevelNotifyText, 0.3f);
            //					Handles.EndGUI();
            //				}
            //			}
        }

        private void RenderForSceneCamera(Camera camera)
        {
            m_ImposterController.RecalculateBounds();
            m_ImposterController.UpdatePosition();
            float screenSize = ImposterLODUtility.RelativeScreenSize(camera, m_ImposterController);
            int _currentLODIndex = -1;
            for (int i = 0; i < m_ImposterController.m_LODs.Length; i++)
            {
                if (screenSize > m_ImposterController.m_LODs[i].screenRelativeTransitionHeight)
                {
                    _currentLODIndex = i;
                    break;
                }
            }

            GameObject imposterPreviewGO;
            string previewGOName = "ImposterPreviewGO";
            if ((imposterPreviewGO = GameObject.Find(previewGOName)) != null)
            {
                imposterPreviewGO.GetComponent<MeshRenderer>().enabled = false;
            }

            if (_currentLODIndex == -1)
            {
                HideOriginalGO();
                return;
            }

            ImposterLOD lod = m_ImposterController.m_LODs[_currentLODIndex];
            if (!lod.isImposter)
            {
                ShowOriginalGO(_currentLODIndex);
                return;
            }

            if ((imposterPreviewGO = GameObject.Find(previewGOName)) == null)
            {
                //				Debug.Log ("Create new imposter preview.");
                imposterPreviewGO = new GameObject();
                imposterPreviewGO.name = previewGOName;
                imposterPreviewGO.AddComponent<MeshFilter>();
                imposterPreviewGO.AddComponent<MeshRenderer>();
            }

            imposterPreviewGO.hideFlags = HideFlags.HideAndDontSave;
            imposterPreviewGO.GetComponent<MeshFilter>().sharedMesh = Helper.NewPlane(Vector3.zero,
                Vector3.one * m_ImposterController.quadSize / 2, new Vector4(0, 0, 1, 1));
            MeshRenderer mr = imposterPreviewGO.GetComponent<MeshRenderer>();
            mr.enabled = true;
            if (mr.sharedMaterial == null)
            {
                mr.sharedMaterial = new Material(Shader.Find("Unlit/Transparent"));
            }

            Material mat = mr.sharedMaterial;
            Camera renderCamera = ImpostersHandler.Instance.GetImposterCamera();
            ImposterLODUtility.PrepareCameraForRender(renderCamera, m_ImposterController, camera);
            int texRes = ImposterLODUtility.GetImposterTextureResolution(screenSize, (int) lod.minImposterResolution,
                (int) lod.maxImposterResolution);
            RenderTexture rt = new RenderTexture(texRes, texRes, 16);
            if (mat.mainTexture != null)
            {
                Helper.Destroy(mat.mainTexture);
            }

            mat.mainTexture = rt;
            renderCamera.targetTexture = rt;

            PrepareToRender(_currentLODIndex, ImpostersHandler.layerForRender);

            renderCamera.Render();

            m_ImposterController.AfterRender();
            HideOriginalGO();

            Vector3 direction = camera.transform.position - m_ImposterController.bounds.center;
            imposterPreviewGO.transform.position = m_ImposterController.bounds.center +
                                                   direction.normalized * m_ImposterController.ZOffset *
                                                   m_ImposterController.quadSize;
            imposterPreviewGO.transform.rotation = Quaternion.LookRotation(-direction);
        }

        public void Update()
        {
            if (!(SceneView.lastActiveSceneView == null) && !(SceneView.lastActiveSceneView.camera == null))
            {
                if (SceneView.lastActiveSceneView.camera.transform.position != this.m_LastCameraPos)
                {
                    this.m_LastCameraPos = SceneView.lastActiveSceneView.camera.transform.position;
                    //					m_ImposterController.RenderForSceneCamera (SceneView.lastActiveSceneView.camera);
                    base.Repaint();
                }
            }
        }

        private ModelImporter GetImporter()
        {
            return AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(PrefabUtility.GetPrefabParent(base.target))) as
                ModelImporter;
        }

        private bool IsLODUsingCrossFadeWidth(int lod)
        {
            return false;
            //			bool result;
            //			if (this.m_FadeMode.intValue == 0 || this.m_AnimateCrossFading.boolValue)
            //			{
            //				result = false;
            //			}
            //			else if (this.m_FadeMode.intValue == 1)
            //			{
            //				result = true;
            //			}
            //			else if (this.m_NumberOfLODs > 0 && this.m_SelectedLOD == this.m_NumberOfLODs - 1)
            //			{
            //				result = true;
            //			}
            //			else
            //			{
            //				if (this.m_NumberOfLODs > 1 && this.m_SelectedLOD == this.m_NumberOfLODs - 2)
            //				{
            //					SerializedProperty serializedProperty = base.serializedObject.FindProperty(string.Format("m_LODs.Array.data[{0}].renderers", this.m_NumberOfLODs - 1));
            //					if (serializedProperty.arraySize == 1 && serializedProperty.GetArrayElementAtIndex(0).objectReferenceValue is ImposterRenderer)
            //					{
            //						result = true;
            //						return result;
            //					}
            //				}
            //				result = false;
            //			}
            //			return result;
        }

        public override void OnInspectorGUI()
        {
            bool enabled = GUI.enabled;
            base.serializedObject.Update();

            _isStatic.boolValue = EditorGUILayout.Toggle("Is static", _isStatic.boolValue);
            //			_imposterOrientation.enumValueIndex = (int)(ImposterOrientation)EditorGUILayout.EnumPopup ("Orientation: ", (ImposterOrientation)_imposterOrientation.enumValueIndex);
            EditorGUILayout.PropertyField(alwaysLookAtCamera, new GUIContent("Always Look At Camera"));
            updateBehavior.enumValueIndex = (int) (ImposterOrientation) EditorGUILayout.EnumPopup("Update Behavior",
                (ImposterController.UpdateBehavior) updateBehavior.enumValueIndex);
            errorCameraAngle.floatValue =
                EditorGUILayout.Slider("Error camera angle", errorCameraAngle.floatValue, 0.1f, 180);
            errorDistance.floatValue = EditorGUILayout.Slider("Error distance", errorDistance.floatValue, 0.001f, 2);
            ZOffset.floatValue = EditorGUILayout.Slider("Z Offset", ZOffset.floatValue, 0, 2);

            EditorGUI.BeginDisabledGroup(ImpostersHandler.Instance.light == null);
            EditorGUILayout.PropertyField(useErrorLightAngle, new GUIContent("Use error light angle"));
            if (useErrorLightAngle.boolValue)
                errorLightAngle.floatValue =
                    EditorGUILayout.Slider("Error light angle", errorLightAngle.floatValue, 0.1f, 180);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.PropertyField(useUpdateByTime, new GUIContent("Use update by time"));
            if (useUpdateByTime.boolValue)
                timeInterval.floatValue = EditorGUILayout.Slider("Time interval", timeInterval.floatValue, 0.01f, 180f);

            //EditorGUILayout.PropertyField(_useOwnMaterial, new GUIContent("Use own material "));
            //if (_useOwnMaterial.boolValue)
            //{
            //    EditorGUILayout.HelpBox("This imposter will not batching!", MessageType.Info);
            //    EditorGUILayout.PropertyField(_shaderForImposter, new GUIContent("Shader for imposter"));
            //}
            int count = 0;
            foreach (Transform t in Selection.transforms)
            {
                if (t.GetComponent<ImposterController>())
                    count++;
                if (count > 1)
                    return;
            }

            //			EditorGUILayout.PropertyField(this.m_FadeMode, new GUILayoutOption[0]);
            //			this.m_ShowAnimateCrossFading.target = (this.m_FadeMode.intValue != 0);

            this.m_NumberOfLODs = this.m_LODs.arraySize;
            if (this.m_SelectedLOD >= this.m_NumberOfLODs)
            {
                this.m_SelectedLOD = this.m_NumberOfLODs - 1;
            }

            if (this.m_NumberOfLODs > 0 && this.activeLOD >= 0)
            {
                SerializedProperty serializedProperty =
                    base.serializedObject.FindProperty(
                        string.Format("m_LODs.Array.data[{0}].renderers", this.activeLOD));
                for (int k = serializedProperty.arraySize - 1; k >= 0; k--)
                {
                    SerializedProperty serializedProperty2 = serializedProperty.GetArrayElementAtIndex(k);
                    OriginalGOController x = serializedProperty2.objectReferenceValue as OriginalGOController;
                    if (x == null || x.Renderer == null)
                    {
                        serializedProperty.DeleteArrayElementAtIndex(k);
                    }
                }
            }

            GUILayout.Space(18f);
            Rect rect = GUILayoutUtility.GetRect(0f, 30f, new GUILayoutOption[]
            {
                GUILayout.ExpandWidth(true)
            });
            List<LODGroupGUI.LODInfo> list = LODGroupGUI.CreateLODInfos(this.m_NumberOfLODs, rect,
                (int i) => string.Format("LOD {0}", i),
                (int i) => base.serializedObject
                    .FindProperty(string.Format("m_LODs.Array.data[{0}].screenRelativeTransitionHeight", i))
                    .floatValue);
            this.DrawLODLevelSlider(rect, list);
            GUILayout.Space(16f);
            //			if (QualitySettings.lodBias != 1f)
            //			{
            //				EditorGUILayout.HelpBox(string.Format("Active LOD bias is {0:0.0#}. Distances are adjusted accordingly.", QualitySettings.lodBias), MessageType.Warning);
            //			}
            if (this.m_NumberOfLODs > 0 && this.activeLOD >= 0 && this.activeLOD < this.m_NumberOfLODs)
            {
                this.m_ShowFadeTransitionWidth.target = this.IsLODUsingCrossFadeWidth(this.activeLOD);
                if (EditorGUILayout.BeginFadeGroup(this.m_ShowFadeTransitionWidth.faded))
                {
                    EditorGUILayout.PropertyField(
                        base.serializedObject.FindProperty(string.Format("m_LODs.Array.data[{0}].fadeTransitionWidth",
                            this.activeLOD)), new GUILayoutOption[0]);
                }

                EditorGUILayout.EndFadeGroup();
                EditorGUILayout.PropertyField(
                    base.serializedObject.FindProperty(string.Format("m_LODs.Array.data[{0}].isImposter",
                        this.activeLOD)), new GUIContent("Render as imposter"), new GUILayoutOption[0]);
                if (base.serializedObject
                    .FindProperty(string.Format("m_LODs.Array.data[{0}].isImposter", this.activeLOD)).boolValue)
                {
                    EditorGUILayout.PropertyField(
                        base.serializedObject.FindProperty(string.Format("m_LODs.Array.data[{0}].renderShadows",
                            this.activeLOD)), new GUIContent("Render shadows"), new GUILayoutOption[0]);
                    EditorGUILayout.PropertyField(
                        base.serializedObject.FindProperty(string.Format("m_LODs.Array.data[{0}].maxImposterResolution",
                            this.activeLOD)), new GUIContent("Max texture resolution"), new GUILayoutOption[0]);
                    EditorGUILayout.PropertyField(
                        base.serializedObject.FindProperty(string.Format("m_LODs.Array.data[{0}].minImposterResolution",
                            this.activeLOD)), new GUIContent("Min texture resolution"), new GUILayoutOption[0]);
                }

                this.DrawRenderersInfo(EditorGUIUtility.currentViewWidth);
            }

            GUILayout.Space(8f);
            GUILayout.BeginHorizontal(new GUILayoutOption[0]);
            GUILayout.FlexibleSpace();
            //			bool flag = LODUtility.NeedUpdateLODGroupBoundingBox(this.m_LODGroup);
            //			using (new EditorGUI.DisabledScope(!flag))
            //			{
            //				if (GUILayout.Button((!flag) ? LODGroupGUI.Styles.m_RecalculateBoundsDisabled : LODGroupGUI.Styles.m_RecalculateBounds, new GUILayoutOption[]
            //					{
            //						GUILayout.ExpandWidth(false)
            //					}))
            //				{
            //					Undo.RecordObject(this.m_LODGroup, "Recalculate LODGroup Bounds");
            //					this.m_LODGroup.RecalculateBounds();
            //				}
            //			}
            //			if (GUILayout.Button(LODGroupGUI.Styles.m_LightmapScale, new GUILayoutOption[]
            //				{
            //					GUILayout.ExpandWidth(false)
            //				}))
            //			{
            //				this.SendPercentagesToLightmapScale();
            //			}
            GUILayout.EndHorizontal();
            GUILayout.Space(5f);
            ModelImporter modelImporter = (PrefabUtility.GetPrefabType(base.target) != PrefabType.ModelPrefabInstance)
                ? null
                : this.GetImporter();
            if (modelImporter != null)
            {
                SerializedObject serializedObject = new SerializedObject(modelImporter);
                SerializedProperty serializedProperty3 = serializedObject.FindProperty("m_LODScreenPercentages");
                bool flag2 = serializedProperty3.isArray && serializedProperty3.arraySize == list.Count;
                bool enabled2 = GUI.enabled;
                if (!flag2)
                {
                    GUI.enabled = false;
                }

                if (modelImporter != null &&
                    GUILayout.Button(
                        (!flag2)
                            ? LODGroupGUI.Styles.m_UploadToImporterDisabled
                            : LODGroupGUI.Styles.m_UploadToImporter, new GUILayoutOption[0]))
                {
                    for (int j = 0; j < serializedProperty3.arraySize; j++)
                    {
                        serializedProperty3.GetArrayElementAtIndex(j).floatValue = list[j].RawScreenPercent;
                    }

                    serializedObject.ApplyModifiedProperties();
                    AssetDatabase.ImportAsset(modelImporter.assetPath);
                }

                GUI.enabled = enabled2;
            }

            base.serializedObject.ApplyModifiedProperties();
            GUI.enabled = enabled;
        }

        private void DrawRenderersInfo(float availableWidth)
        {
            int avalibleCollumCount = Mathf.FloorToInt(availableWidth / 60f);
            Rect rect = GUILayoutUtility.GetRect(LODGroupGUI.Styles.m_RendersTitle,
                LODGroupGUI.Styles.m_LODSliderTextSelected);
            if (Event.current.type == EventType.Repaint)
            {
                EditorStyles.label.Draw(rect, LODGroupGUI.Styles.m_RendersTitle, false, false, false, false);
            }

            SerializedProperty serializedProperty =
                base.serializedObject.FindProperty(string.Format("m_LODs.Array.data[{0}].renderers", this.activeLOD));
            int num2 = serializedProperty.arraySize + 1;
            int num3 = Mathf.CeilToInt((float) num2 / (float) avalibleCollumCount);
            Rect rect2 = GUILayoutUtility.GetRect(avalibleCollumCount * 60, (float) (num3 * 60) + 1,
                new GUILayoutOption[] //(float)(num3 * 60)
                {
                    GUILayout.ExpandHeight(true)
                });
            Rect rect3 = rect2;
            GUI.Box(rect2, GUIContent.none);
            rect3.width -= 6f;
            rect3.x += 3f;
            float num4 = rect3.width / (float) avalibleCollumCount;
            List<Rect> list = new List<Rect>();
            for (int i = 0; i < num3; i++)
            {
                int num5 = 0;
                while (num5 < avalibleCollumCount && i * avalibleCollumCount + num5 < serializedProperty.arraySize)
                {
                    Rect rect4 = new Rect(2f + rect3.x + (float) num5 * num4, 2f + rect3.y + (float) (i * 60),
                        num4 - 4f, 56f);
                    list.Add(rect4);
                    this.DrawRendererButton(rect4, i * avalibleCollumCount + num5);
                    num5++;
                }
            }

            if (!this.m_IsPrefab)
            {
                int num6 = (num2 - 1) % avalibleCollumCount;
                int num7 = num3 - 1;
                this.HandleAddRenderer(
                    new Rect(2f + rect3.x + (float) num6 * num4, 2f + rect3.y + (float) (num7 * 60), num4 - 4f, 56f),
                    list, rect2);
            }
        }

        private void HandleAddRenderer(Rect position, IEnumerable<Rect> alreadyDrawn, Rect drawArea)
        {
            Event evt = Event.current;
            EventType type = evt.type;
            switch (type)
            {
                case EventType.Repaint:
                    LODGroupGUI.Styles.m_LODStandardButton.Draw(position, GUIContent.none, false, false, false, false);
                    LODGroupGUI.Styles.m_LODRendererAddButton.Draw(
                        new Rect(position.x - 2f, position.y, position.width, position.height), "  Drag&Drop", false,
                        false, false, false);
                    return;
                case EventType.Layout:
                case EventType.Ignore:
                case EventType.Used:
                case EventType.ValidateCommand:
                    return;
                case EventType.DragUpdated:
                case EventType.DragPerform:
                {
                    bool flag = false;
                    if (drawArea.Contains(evt.mousePosition))
                    {
                        if (alreadyDrawn.All((Rect x) => !x.Contains(evt.mousePosition)))
                        {
                            flag = true;
                        }
                    }

                    if (!flag)
                    {
                        return;
                    }

                    if (DragAndDrop.objectReferences.Count<UnityEngine.Object>() > 0)
                    {
                        DragAndDrop.visualMode =
                            ((!this.m_IsPrefab) ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.None);
                        if (evt.type == EventType.DragPerform)
                        {
                            IEnumerable<GameObject> selectedGameObjects = from go in DragAndDrop.objectReferences
                                where go as GameObject != null
                                select go as GameObject;
                            IEnumerable<OriginalGOController> renderers = this.GetRenderers(selectedGameObjects, true);
                            this.AddGameObjectRenderers(renderers, true);
                            DragAndDrop.AcceptDrag();
                            evt.Use();
                            return;
                        }
                    }

                    evt.Use();
                    return;
                }

                case EventType.ExecuteCommand:
                {
                    Debug.LogError("SHEAT !!!");
                    //					string commandName = evt.commandName;
                    //					if (commandName == "ObjectSelectorClosed" && ObjectSelector.get.objectSelectorID == "LODGroupSelector".GetHashCode())
                    //					{
                    //						GameObject gameObject = ObjectSelector.GetCurrentObject() as GameObject;
                    //						if (gameObject != null)
                    //						{
                    //							this.AddGameObjectRenderers(this.GetRenderers(new List<GameObject>
                    //								{
                    //									gameObject
                    //								}, true), true);
                    //						}
                    //						evt.Use();
                    //						GUIUtility.ExitGUI();
                    //					}
                    return;
                }
            }

            return;
        }

        private void DrawRendererButton(Rect position, int rendererIndex)
        {
            SerializedProperty serializedProperty =
                base.serializedObject.FindProperty(string.Format("m_LODs.Array.data[{0}].renderers", this.activeLOD));
            SerializedProperty serializedProperty2 = serializedProperty.GetArrayElementAtIndex(rendererIndex);
            if (serializedProperty2.objectReferenceValue == null)
            {
                serializedProperty.DeleteArrayElementAtIndex(rendererIndex);
                return;
            }

            OriginalGOController originalGOController =
                serializedProperty2.objectReferenceValue as OriginalGOController;
            Renderer renderer = (serializedProperty2.objectReferenceValue as OriginalGOController).Renderer;
            Rect position2 = new Rect(position.xMax - 20f, position.yMax - 20f, 20f, 20f);
            Event current = Event.current;
            EventType type = current.type;
            if (type != EventType.Repaint)
            {
                if (type == EventType.MouseDown)
                {
                    if (!this.m_IsPrefab && position2.Contains(current.mousePosition))
                    {
                        if (serializedProperty.GetArrayElementAtIndex(rendererIndex).objectReferenceValue != null)
                            serializedProperty.GetArrayElementAtIndex(rendererIndex).objectReferenceValue = null;
                        serializedProperty.DeleteArrayElementAtIndex(rendererIndex);

                        base.serializedObject.ApplyModifiedProperties();

                        // if deleted originalGOController is not used anywhere else, than delet this component
                        bool used = false;
                        foreach (ImposterLOD lod in m_ImposterController.m_LODs)
                        {
                            foreach (OriginalGOController c in lod.renderers)
                                if (c == originalGOController)
                                    used = true;
                        }

                        if (!used)
                        {
                            originalGOController.Hide();
                            originalGOController.Show();
                            DestroyImmediate(originalGOController);
                        }

                        this.m_ImposterController.RecalculateBounds();
                        base.serializedObject.ApplyModifiedProperties();
                        current.Use();
                    }
                    else if (position.Contains(current.mousePosition))
                    {
                        EditorGUIUtility.PingObject(renderer);
                        current.Use();
                    }
                }
            }
            else
            {
                if (renderer != null)
                {
                    MeshFilter component = renderer.GetComponent<MeshFilter>();
                    GUIContent content;
                    if (component != null && component.sharedMesh != null)
                    {
                        content = new GUIContent(AssetPreview.GetAssetPreview(component.sharedMesh),
                            renderer.gameObject.name);
                    }
                    else if (renderer is SkinnedMeshRenderer)
                    {
                        content = new GUIContent(
                            AssetPreview.GetAssetPreview((renderer as SkinnedMeshRenderer).sharedMesh),
                            renderer.gameObject.name);
                    }
                    else
                    {
                        content = new GUIContent(ObjectNames.NicifyVariableName(renderer.GetType().Name),
                            renderer.gameObject.name);
                    }

                    LODGroupGUI.Styles.m_LODBlackBox.Draw(position, GUIContent.none, false, false, false, false);
                    LODGroupGUI.Styles.m_LODRendererButton.Draw(
                        new Rect(position.x + 2f, position.y + 2f, position.width - 4f, position.height - 4f), content,
                        false, false, false, false);
                }
                else
                {
                    LODGroupGUI.Styles.m_LODBlackBox.Draw(position, GUIContent.none, false, false, false, false);
                    LODGroupGUI.Styles.m_LODRendererButton.Draw(position, "<Empty>", false, false, false, false);
                }

                if (!this.m_IsPrefab)
                {
                    LODGroupGUI.Styles.m_LODBlackBox.Draw(position2, GUIContent.none, false, false, false, false);
                    LODGroupGUI.Styles.m_LODRendererRemove.Draw(position2, LODGroupGUI.Styles.m_IconRendererMinus,
                        false, false, false, false);
                }
            }
        }

        private IEnumerable<OriginalGOController> GetRenderers(IEnumerable<GameObject> selectedGameObjects,
            bool searchChildren)
        {
            IEnumerable<OriginalGOController> result;
            if (EditorUtility.IsPersistent(this.m_ImposterController))
            {
                result = new List<OriginalGOController>();
            }
            else
            {
                IEnumerable<GameObject> childGO = from go in selectedGameObjects
                    where go.transform.IsChildOf(this.m_ImposterController.transform)
                    select go;
                IEnumerable<GameObject> nonChildGO = from go in selectedGameObjects
                    where !go.transform.IsChildOf(this.m_ImposterController.transform)
                    select go;
                List<GameObject> list = new List<GameObject>();
                if (nonChildGO.Count<GameObject>() > 0)
                {
                    if (EditorUtility.DisplayDialog("Reparent GameObjects",
                        "Some objects are not children of the LODGroup GameObject. Do you want to reparent them and add them to the LODGroup?",
                        "Yes, Reparent", "No, Use Only Existing Children"))
                    {
                        foreach (GameObject current in nonChildGO)
                        {
                            if (EditorUtility.IsPersistent(current))
                            {
                                GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(current);
                                if (gameObject != null)
                                {
                                    gameObject.transform.parent = this.m_ImposterController.transform;
                                    gameObject.transform.localPosition = Vector3.zero;
                                    gameObject.transform.localRotation = Quaternion.identity;
                                    list.Add(gameObject);
                                }
                            }
                            else
                            {
                                current.transform.parent = this.m_ImposterController.transform;
                                list.Add(current);
                            }
                        }

                        childGO = childGO.Union(list);
                    }
                }

                List<Renderer> list2 = new List<Renderer>();
                foreach (GameObject current2 in childGO)
                {
                    if (searchChildren)
                    {
                        list2.AddRange(current2.GetComponentsInChildren<Renderer>());
                    }
                    else
                    {
                        list2.Add(current2.GetComponent<Renderer>());
                    }
                }

                //				IEnumerable<Renderer> collection = from go in DragAndDrop.objectReferences
                //						where go as Renderer != null
                //					select go as Renderer;
                List<OriginalGOController> collection = new List<OriginalGOController>();
                foreach (Renderer rend in list2)
                {
                    if (!rend.GetComponent<OriginalGOController>())
                    {
                        rend.gameObject.AddComponent<OriginalGOController>();
                    }

                    collection.Add(rend.GetComponent<OriginalGOController>());
                }

                result = collection;
            }

            return result;
        }

        private void AddGameObjectRenderers(IEnumerable<OriginalGOController> toAdd, bool add)
        {
            SerializedProperty serializedProperty =
                base.serializedObject.FindProperty(string.Format("m_LODs.Array.data[{0}].renderers", this.activeLOD));
            if (!add)
            {
                serializedProperty.ClearArray();
            }

            List<OriginalGOController> list = new List<OriginalGOController>();
            for (int i = 0; i < serializedProperty.arraySize; i++)
            {
                SerializedProperty serializedProperty2 = serializedProperty.GetArrayElementAtIndex(i);
                OriginalGOController renderer = serializedProperty2.objectReferenceValue as OriginalGOController;
                if (!(renderer == null))
                {
                    list.Add(renderer);
                }
            }

            foreach (OriginalGOController current in toAdd)
            {
                if (!list.Contains(current))
                {
                    serializedProperty.arraySize++;
                    serializedProperty.GetArrayElementAtIndex(serializedProperty.arraySize - 1).objectReferenceValue =
                        current;
                    list.Add(current);
                }
            }

            base.serializedObject.ApplyModifiedProperties();
            this.m_ImposterController.RecalculateBounds();
        }

        private void DeletedLOD()
        {
            this.m_SelectedLOD--;
        }

        private static void UpdateCamera(float desiredPercentage, ImposterController imposterController)
        {
            //			Debug.LogError ("SHEAT !!!");
            Vector3 pos = ImposterLODUtility.CalculateWorldReferencePoint(imposterController);
            float relativeScreenHeight = Mathf.Max(desiredPercentage / 2, 1E-06f);
            float num = ImposterLODUtility.CalculateDistance(SceneView.lastActiveSceneView.camera, relativeScreenHeight,
                imposterController);
            if (SceneView.lastActiveSceneView.camera.orthographic)
            {
                num *= Mathf.Sqrt(2f * SceneView.lastActiveSceneView.camera.aspect);
            }

            SceneView.lastActiveSceneView.LookAtDirect(pos, SceneView.lastActiveSceneView.camera.transform.rotation,
                num);
        }

        private void UpdateSelectedLODFromCamera(IEnumerable<LODGroupGUI.LODInfo> lods, float cameraPercent)
        {
            foreach (LODGroupGUI.LODInfo current in lods)
            {
                if (cameraPercent > current.RawScreenPercent)
                {
                    this.m_SelectedLOD = current.LODLevel;
                    break;
                }
            }
        }

        private void DrawLODLevelSlider(Rect sliderPosition, List<LODGroupGUI.LODInfo> lods)
        {
            int controlID = GUIUtility.GetControlID(this.m_LODSliderId, FocusType.Passive);
            int controlID2 = GUIUtility.GetControlID(this.m_CameraSliderId, FocusType.Passive);
            Event current = Event.current;
            EventType typeForControl = current.GetTypeForControl(controlID);
            switch (typeForControl)
            {
                case EventType.MouseDown:
                {
                    if (current.button == 1 && sliderPosition.Contains(current.mousePosition))
                    {
                        float cameraPercent = LODGroupGUI.GetCameraPercent(current.mousePosition, sliderPosition);
                        GenericMenu genericMenu = new GenericMenu();
                        if (lods.Count >= 8)
                        {
                            genericMenu.AddDisabledItem(new GUIContent("Insert Before"));
                        }
                        else
                        {
                            genericMenu.AddItem(new GUIContent("Insert Before"), false,
                                new GenericMenu.MenuFunction(new ImposterControllerEditor.LODAction(lods, cameraPercent,
                                    current.mousePosition, this.m_LODs, null).InsertLOD));
                        }

                        bool flag = true;
                        if (lods.Count > 0 && lods[lods.Count - 1].RawScreenPercent < cameraPercent)
                        {
                            flag = false;
                        }

                        if (flag)
                        {
                            genericMenu.AddDisabledItem(new GUIContent("Delete"));
                        }
                        else
                        {
                            genericMenu.AddItem(new GUIContent("Delete"), false,
                                new GenericMenu.MenuFunction(new ImposterControllerEditor.LODAction(lods, cameraPercent,
                                        current.mousePosition, this.m_LODs, DeletedLOD)
                                    .DeleteLOD)); //new LODGroupEditor.LODAction.Callback(this, DeletedLOD)).DeleteLOD));
                        }

                        genericMenu.ShowAsContext();
                        bool flag2 = false;
                        foreach (LODGroupGUI.LODInfo current2 in lods)
                        {
                            if (current2.m_RangePosition.Contains(current.mousePosition))
                            {
                                this.m_SelectedLOD = current2.LODLevel;
                                flag2 = true;
                                break;
                            }
                        }

                        if (!flag2)
                        {
                            this.m_SelectedLOD = -1;
                        }

                        current.Use();
                        goto IL_6FF;
                    }

                    Rect rect = sliderPosition;
                    rect.x -= 5f;
                    rect.width += 10f;
                    if (rect.Contains(current.mousePosition))
                    {
                        current.Use();
                        GUIUtility.hotControl = controlID;
                        bool flag3 = false;
                        IOrderedEnumerable<LODGroupGUI.LODInfo> collection = from lod in lods
                            where lod.ScreenPercent > 0.5f
                            select lod
                            into x
                            orderby x.LODLevel descending
                            select x;
                        IOrderedEnumerable<LODGroupGUI.LODInfo> collection2 = from lod in lods
                            where lod.ScreenPercent <= 0.5f
                            select lod
                            into x
                            orderby x.LODLevel
                            select x;
                        List<LODGroupGUI.LODInfo> list = new List<LODGroupGUI.LODInfo>();
                        list.AddRange(collection);
                        list.AddRange(collection2);
                        foreach (LODGroupGUI.LODInfo current3 in list)
                        {
                            if (current3.m_ButtonPosition.Contains(current.mousePosition))
                            {
                                this.m_SelectedLODSlider = current3.LODLevel;
                                flag3 = true;
                                this.BeginLODDrag(current3.RawScreenPercent + 0.001f, this.m_ImposterController);
                                break;
                            }
                        }

                        if (!flag3)
                        {
                            foreach (LODGroupGUI.LODInfo current4 in lods)
                            {
                                if (current4.m_RangePosition.Contains(current.mousePosition))
                                {
                                    this.m_SelectedLOD = current4.LODLevel;
                                    break;
                                }
                            }
                        }
                    }

                    goto IL_6FF;
                }

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlID)
                    {
                        GUIUtility.hotControl = 0;
                        this.m_SelectedLODSlider = -1;
                        this.EndLODDrag();
                        current.Use();
                    }

                    goto IL_6FF;
                case EventType.MouseMove:
                case EventType.KeyDown:
                case EventType.KeyUp:
                case EventType.ScrollWheel:
                case EventType.Layout:
                    IL_5B:
                    if (typeForControl != EventType.DragExited)
                    {
                        goto IL_6FF;
                    }

                    current.Use();
                    goto IL_6FF;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == controlID && this.m_SelectedLODSlider >= 0 &&
                        lods[this.m_SelectedLODSlider] != null)
                    {
                        current.Use();
                        float cameraPercent2 = LODGroupGUI.GetCameraPercent(current.mousePosition, sliderPosition);
                        LODGroupGUI.SetSelectedLODLevelPercentage(cameraPercent2 - 0.001f, this.m_SelectedLODSlider,
                            lods);
                        SerializedProperty serializedProperty = base.serializedObject.FindProperty(
                            string.Format("m_LODs.Array.data[{0}].screenRelativeTransitionHeight",
                                lods[this.m_SelectedLODSlider].LODLevel));
                        serializedProperty.floatValue = lods[this.m_SelectedLODSlider].RawScreenPercent;
                        this.UpdateLODDrag(cameraPercent2, this.m_ImposterController);
                    }

                    goto IL_6FF;
                case EventType.Repaint:
                    LODGroupGUI.DrawLODSlider(sliderPosition, lods, this.activeLOD);
                    goto IL_6FF;
                case EventType.DragUpdated:
                case EventType.DragPerform:
                {
                    int num = -2;
                    foreach (LODGroupGUI.LODInfo current5 in lods)
                    {
                        if (current5.m_RangePosition.Contains(current.mousePosition))
                        {
                            num = current5.LODLevel;
                            break;
                        }
                    }

                    if (num == -2)
                    {
                        if (LODGroupGUI
                            .GetCulledBox(sliderPosition, (lods.Count <= 0) ? 1f : lods[lods.Count - 1].ScreenPercent)
                            .Contains(current.mousePosition))
                        {
                            num = -1;
                        }
                    }

                    if (num >= -1)
                    {
                        this.m_SelectedLOD = num;
                        if (DragAndDrop.objectReferences.Count<UnityEngine.Object>() > 0)
                        {
                            DragAndDrop.visualMode =
                                ((!this.m_IsPrefab) ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.None);
                            if (current.type == EventType.DragPerform)
                            {
                                IEnumerable<GameObject> selectedGameObjects = from go in DragAndDrop.objectReferences
                                    where go as GameObject != null
                                    select go as GameObject;
                                IEnumerable<OriginalGOController> renderers =
                                    this.GetRenderers(selectedGameObjects, true);
                                if (num == -1)
                                {
                                    this.m_LODs.arraySize++;
                                    SerializedProperty serializedProperty2 =
                                        base.serializedObject.FindProperty(string.Format(
                                            "m_LODs.Array.data[{0}].screenRelativeTransitionHeight", lods.Count));
                                    if (lods.Count == 0)
                                    {
                                        serializedProperty2.floatValue = 0.5f;
                                    }
                                    else
                                    {
                                        SerializedProperty serializedProperty3 =
                                            base.serializedObject.FindProperty(string.Format(
                                                "m_LODs.Array.data[{0}].screenRelativeTransitionHeight",
                                                lods.Count - 1));
                                        serializedProperty2.floatValue = serializedProperty3.floatValue / 2f;
                                    }

                                    this.m_SelectedLOD = lods.Count;
                                    this.AddGameObjectRenderers(renderers, false);
                                }
                                else
                                {
                                    this.AddGameObjectRenderers(renderers, true);
                                }

                                DragAndDrop.AcceptDrag();
                            }
                        }

                        current.Use();
                        goto IL_6FF;
                    }

                    goto IL_6FF;
                }

                default:
                    goto IL_5B;
            }

            IL_6FF:
            if (SceneView.lastActiveSceneView != null && SceneView.lastActiveSceneView.camera != null &&
                !this.m_IsPrefab)
            {
                Camera camera = SceneView.lastActiveSceneView.camera;
                float activeRelativeScreenSize =
                    ImposterLODUtility.RelativeScreenSize(camera, this.m_ImposterController);
                float value = LODGroupGUI.DelinearizeScreenPercentage(activeRelativeScreenSize);
                Vector3
                    b = this.m_ImposterController.bounds
                        .center; //LODUtility.CalculateWorldReferencePoint(this.m_LODGroup);
                Vector3 normalized = (SceneView.lastActiveSceneView.camera.transform.position - b).normalized;
                if (Vector3.Dot(camera.transform.forward, normalized) > 0f)
                {
                    value = 1f;
                }

                Rect rect2 = LODGroupGUI.CalcLODButton(sliderPosition, Mathf.Clamp01(value));
                Rect position = new Rect(rect2.center.x - 15f, rect2.y - 25f, 32f, 32f);
                Rect position2 = new Rect(rect2.center.x - 1f, rect2.y, 2f, rect2.height);
                Rect position3 = new Rect(position.center.x - 5f, position2.yMax, 35f, 20f);
                switch (current.GetTypeForControl(controlID2))
                {
                    case EventType.MouseDown:
                        if (position.Contains(current.mousePosition))
                        {
                            current.Use();
                            float cameraPercent3 = LODGroupGUI.GetCameraPercent(current.mousePosition, sliderPosition);
                            this.UpdateSelectedLODFromCamera(lods, cameraPercent3);
                            GUIUtility.hotControl = controlID2;
                            this.BeginLODDrag(cameraPercent3, this.m_ImposterController);
                        }

                        break;
                    case EventType.MouseUp:
                        if (GUIUtility.hotControl == controlID2)
                        {
                            this.EndLODDrag();
                            GUIUtility.hotControl = 0;
                            current.Use();
                        }

                        break;
                    case EventType.MouseDrag:
                        if (GUIUtility.hotControl == controlID2)
                        {
                            current.Use();
                            float cameraPercent4 = LODGroupGUI.GetCameraPercent(current.mousePosition, sliderPosition);
                            this.UpdateSelectedLODFromCamera(lods, cameraPercent4);
                            this.UpdateLODDrag(cameraPercent4, this.m_ImposterController);
                        }

                        break;
                    case EventType.Repaint:
                    {
                        Color backgroundColor = GUI.backgroundColor;
                        GUI.backgroundColor = new Color(backgroundColor.r, backgroundColor.g, backgroundColor.b, 0.8f);
                        LODGroupGUI.Styles.m_LODCameraLine.Draw(position2, false, false, false, false);
                        GUI.backgroundColor = backgroundColor;
                        GUI.Label(position, LODGroupGUI.Styles.m_CameraIcon, GUIStyle.none);
                        LODGroupGUI.Styles.m_LODSliderText.Draw(position3,
                            string.Format("{0:0}%", Mathf.Clamp01(activeRelativeScreenSize) * 100f), false, false,
                            false, false);
                        break;
                    }
                }
            }
        }

        private void BeginLODDrag(float desiredPercentage, ImposterController imposterController)
        {
            if (!(SceneView.lastActiveSceneView == null) && !(SceneView.lastActiveSceneView.camera == null) &&
                !this.m_IsPrefab)
            {
                ImposterControllerEditor.UpdateCamera(desiredPercentage, imposterController);
#if !UNITY_2019_1_OR_NEWER
                SceneView.lastActiveSceneView.SetSceneViewFiltering(true);
#endif
                HierarchyProperty.FilterSingleSceneObject(imposterController.gameObject.GetInstanceID(), false);
                SceneView.RepaintAll();
            }
        }

        private void UpdateLODDrag(float desiredPercentage, ImposterController imposterController)
        {
            if (!(SceneView.lastActiveSceneView == null) && !(SceneView.lastActiveSceneView.camera == null) &&
                !this.m_IsPrefab)
            {
                ImposterControllerEditor.UpdateCamera(desiredPercentage, imposterController);
                SceneView.RepaintAll();
            }
        }

        private void EndLODDrag()
        {
            if (!(SceneView.lastActiveSceneView == null) && !(SceneView.lastActiveSceneView.camera == null) &&
                !this.m_IsPrefab)
            {
#if !UNITY_2019_1_OR_NEWER
                SceneView.lastActiveSceneView.SetSceneViewFiltering(false);
#endif
                HierarchyProperty.FilterSingleSceneObject(0, true);
            }
        }

        public override bool HasPreviewGUI()
        {
            return base.target != null;
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            //			if (!ShaderUtil.hardwareSupportsRectRenderTexture)
            //			{
            //				if (Event.current.type == EventType.Repaint)
            //				{
            //					EditorGUI.DropShadowLabel(new Rect(r.x, r.y, r.width, 40f), "LOD preview \nnot available");
            //				}
            //			}
            //			else
            //			{
            //				//TODO: PreviewGUI
            ////				Debug.LogError ("SHEAT !!!");
            //				this.InitPreview();
            ////				this.m_PreviewDir = PreviewGUI.Drag2D(this.m_PreviewDir, r);
            //				this.m_PreviewDir.y = Mathf.Clamp(this.m_PreviewDir.y, -89f, 89f);
            //				if (Event.current.type == EventType.Repaint)
            //				{
            //					this.m_PreviewUtility.BeginPreview(r, background);
            //					this.DoRenderPreview();
            //					this.m_PreviewUtility.EndAndDrawPreview(r);
            //				}
            //			}
        }

        private void InitPreview()
        {
            if (this.m_PreviewUtility == null)
            {
                this.m_PreviewUtility = new PreviewRenderUtility();
            }

            if (ImposterControllerEditor.kSLightIcons[0] == null)
            {
                ImposterControllerEditor.kSLightIcons[0] = EditorGUIUtility.IconContent("PreMatLight0");
                ImposterControllerEditor.kSLightIcons[1] = EditorGUIUtility.IconContent("PreMatLight1");
            }
        }

        private void ShowOriginalGO(int imposterLODIndex)
        {
            ImposterLOD lod = m_ImposterController.m_LODs[imposterLODIndex];
            HideOriginalGO();
            for (int i = 0; i < lod.renderers.Length; i++)
            {
                if (lod.renderers[i] != null)
                {
                    lod.renderers[i].Show();
                    lod.renderers[i].Renderer.enabled = true;
                }
            }
        }

        private void HideOriginalGO()
        {
            for (int i = 0; i < m_ImposterController.m_LODs.Length; i++)
            {
                for (int j = 0; j < m_ImposterController.m_LODs[i].renderers.Length; j++)
                {
                    if (m_ImposterController.m_LODs[i].renderers[j] != null)
                    {
                        m_ImposterController.m_LODs[i].renderers[j].Hide();
                    }
                }
            }
        }

        private void PrepareToRender(int imposterLODIndex, int layer)
        {
            HideOriginalGO();
            ImposterLOD lod = m_ImposterController.m_LODs[imposterLODIndex];
            for (int i = 0; i < lod.renderers.Length; i++)
            {
                if (lod.renderers[i] != null)
                    lod.renderers[i].PrepareToRender(layer, false);
            }
        }
    }
}