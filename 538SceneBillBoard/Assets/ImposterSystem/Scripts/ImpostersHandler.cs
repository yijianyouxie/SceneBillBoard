using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if UNITY_5_5_OR_NEWER
using UnityEngine.Profiling;
#endif

namespace ImposterSystem
{

    public class ImpostersHandler : Singleton<ImpostersHandler>
    {

        #region UnityEvents 
        void Reset()
        {
            InitViewController();
            InitImpostersHandler();
        }

        bool _isAwaked = false;
        void Awake()
        {
            if (_isAwaked)
                return;
            _isAwaked = true;
            _instance = this;
            InitViewController();
            InitImpostersHandler();
        }

        void OnEnable()
        {
            OnEnableViewControl();
            OnEnableImposterHandler();
        }

        void OnDisable()
        {
            OnDisableViewControl();
            OnDisableImposterHandler();
        }

        void LateUpdate()
        {
            UpdateViewControl();
            UpdateImposterHandler();
        }
#if BILL_SYS_TRIAL_VERSION
        private void OnGUI()
        {
            int width = (int)(Screen.width * 0.3f);
            int height = (int)(Screen.height * 0.1f);
            Color background = GUI.backgroundColor;
            Color content = GUI.contentColor;
            bool guienabled = GUI.enabled;
            GUI.backgroundColor = Color.clear;
            GUI.contentColor = Color.white;
            GUI.enabled = false;
            GUI.Button(new Rect(0, Screen.height - height, width, height), "RealtimeImposterSystem TRIAL VERSION");
            GUI.backgroundColor = background;
            GUI.contentColor = content;
            GUI.enabled = guienabled;
        }
#endif
        #endregion

        #region ImpostersHandler

        #region public vars
        public bool disableImpostersUpdating = false;
        [HideInInspector]
        internal List<ImposterBase> queueOfImposters;
        [SerializeField] internal ImposterRenderType _impostersRenderType = ImposterRenderType.drawMesh;
        public InvisibleImposterAction _invisibleImposterAction = InvisibleImposterAction.none;

        [SerializeField]
        QueueType queueType = QueueType.simple;
        public new Light light;

        [Range(1, 200)]
        public int maxUpdatesPerFrame = 20;
        [SerializeField]
        AtlasResolution atlasResolution = AtlasResolution._2048x2048;
        public Shader shaderForRender;
        [Header("Fading (DrawMesh only)")]
        [SerializeField]
        public bool useFading = false;
        [SerializeField] public Texture2D _fadeNoiseTexture;
        [Range(0.05f, 1)]
        [SerializeField]
        public float fadeTime = 0.2f;
        [SerializeField] public bool dontUpdateWhenFading = false;

        [Header("Imposter Shadow Casting (Test)")]
        [SerializeField]
        public bool shadowCastingEnabled;
        [Range(0.1f, 30f)]
        [SerializeField]
        public float lightDirectionDelta = 3;
        [SerializeField] public TextureResolution shadowTextureResolution = TextureResolution._64x64;
        [SerializeField] private AtlasResolution _shadowAtlasResolution = AtlasResolution._512x512;

        [Header("Additional settings")]
        [Range(1f, 2)]
        [SerializeField]
        public float preloadFactor = 1.2f;
        [Range(0f, 45f)]
        [SerializeField]
        public float minAngleToStopLookAtCamera = 30f;

        //Shaders
        Shader _meshRendererShader;
        Shader _drawMeshShader;
        Shader _imposterShadowCasterShader;

        public int updatedByFrameImpostersCount { get; private set; }
        internal Vector3 lightDirection { get; private set; }
        #endregion


        #region private vars
        Camera _imposterCamera;
        Transform _myTrans;
        List<AtlasHandler> _atlases = new List<AtlasHandler>();
        List<AtlasHandler> _shadowAtlases = new List<AtlasHandler>();
        // def quality settings
        float shadDistance;
        ShadowProjection shadProj;
        #endregion

        public int currentImpostersCount { get { return queueOfImposters.Count; } }

        #region PRIVATE methods

        void InitImpostersHandler()
        {
            _imposterCamera = GetImposterCamera();
            queueOfImposters = new List<ImposterBase>();
            ImposterCameraSettings(_imposterCamera);
            _meshRendererShader = Shader.Find("ImposterSystem/Imposter/MeshRendererRenderType");
            _drawMeshShader = Shader.Find("ImposterSystem/Imposter/DrawMeshRenderTypeWithFading");
            _imposterShadowCasterShader = Shader.Find("ImposterSystem/ShadowCaster");
            DiscardAtlases();
        }

        void OnEnableImposterHandler()
        {
            SetRenderablesEnabled(true);
        }

        void OnDisableImposterHandler()
        {
            SetRenderablesEnabled(false);
            DiscardAtlases();
            DestroyUnusedTextures();
        }

        void UpdateImposterHandler()
        {
            Profiler.BeginSample("ImposterSystem");
            if (useFading)
            {
                Shader.SetGlobalFloat("_ImposterSystem_FadeTime", fadeTime);
                if (_fadeNoiseTexture == null)
                    Debug.LogError("Noise texture is NULL!!! You must specified the noise texture for correct imposter fading. Example noise texture located at ImposterSystem/Textures/ImposterSystem_Noise.psd \n");
                Shader.SetGlobalTexture("_ImposterSystem_Noise", _fadeNoiseTexture);
                Shader.SetGlobalFloat("_ImposterSystem_NoiseResolution", _fadeNoiseTexture.width);
            }
            if (light)
            {
                lightDirection = light.transform.forward;
                Shader.SetGlobalVector("_ImposterSystem_LightDirection", -lightDirection);
            }
            Shader.SetGlobalFloat("_ImposterSystem_MinAngleToStopLookAtCamera", minAngleToStopLookAtCamera);
            updatedByFrameImpostersCount = 0;

            Profiler.BeginSample("Cleaning up empty textures");
            DestroyEmptyAtlases();
            DestroyUnusedTextures(_unusedTexturesLifetime);
            Profiler.EndSample();

            Profiler.BeginSample("Update imposters textures");
            for (int i = 0; updatedByFrameImpostersCount < maxUpdatesPerFrame && queueOfImposters.Count > 0; i++)
            {
                int imposterIndex = 0;
                if (queueType == QueueType.sortedByScreenSize)
                {
                    float max = float.MinValue;
                    for (int j = 0; j < queueOfImposters.Count; j++)
                    {
                        if (queueOfImposters[j].screenSize > max)
                        {
                            imposterIndex = j;
                            max = queueOfImposters[j].screenSize;
                        }
                    }
                }
                if (queueOfImposters[imposterIndex].isInQueue && queueOfImposters[imposterIndex].isVisible)
                {
                    queueOfImposters[imposterIndex].UpdateImposter();
                    updatedByFrameImpostersCount++;
                }
                queueOfImposters.RemoveAt(imposterIndex);
            }
            Profiler.EndSample();
            Profiler.EndSample();
        }

        private void DiscardAtlases()
        {
            for (int i = 0; i < _atlases.Count; i++)
            {
                if (_atlases[i] != null)
                    _atlases[i].DiscardContent();
            }
            _atlases.Clear();
            for (int i = 0; i < _shadowAtlases.Count; i++)
            {
                if (_shadowAtlases[i] != null)
                    _shadowAtlases[i].DiscardContent();
            }
            _shadowAtlases.Clear();
        }

        private void DestroyEmptyAtlases()
        {
            for (int i = 0; i < _atlases.Count; i++)
            {
                if (_atlases[i].isEmpty)
                {
                    _atlases[i].DiscardContent();
                    _atlases.RemoveAt(i);
                    i--;
                    continue;
                }
            }
        }

        AtlasHandler CreateNewAtlasHandler(int texRes, int resolution, Shader shader, UnityEngine.Rendering.ShadowCastingMode shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off)
        {
            GameObject go = new GameObject();
            go.name = "AtlasHandler " + texRes;
            go.transform.parent = transform;
            go.transform.localPosition = Vector3.zero;
            AtlasHandler res = go.AddComponent<AtlasHandler>();
            res.ForcedAwake(texRes, resolution, shader, shadowCastingMode);
            res.imposterMat.SetTexture("_Noise", _fadeNoiseTexture);
            return res;
        }

        static void ImposterCameraSettings(Camera helperCam)
        {
            helperCam.enabled = false;
#if UNITY_5_6_OR_NEWER
            helperCam.allowHDR = false;
#else
            helperCam.hdr = false;
#endif
            helperCam.depthTextureMode = DepthTextureMode.None;
            helperCam.backgroundColor = Color.clear;
            helperCam.clearFlags = CameraClearFlags.Color;
            helperCam.renderingPath = RenderingPath.Forward;
            helperCam.cullingMask = LayerMask.GetMask(LayerMask.LayerToName(layerForRender));
            helperCam.useOcclusionCulling = false;
            helperCam.farClipPlane = 10000;
        }

        private void SetRenderablesEnabled(bool value)
        {
            foreach (var t in GameObject.FindObjectsOfType<Renderable>())
                t.enabled = value;
        }
#endregion // private methods

#region INTERNAL methods

        internal void RemoveImposterFromQueue(ImposterBase imposter)
        {
            imposter.isInQueue = false;
        }

        internal void AddImposterToQueue(ImposterBase imposter)
        {
            if (queueType == QueueType.none)
            {
                imposter.UpdateImposter();
                return;
            }
            if (imposter.isInQueue)
                return;
            imposter.isInQueue = true;
            queueOfImposters.Add(imposter);
        }

        internal AtlasHandler FindAtlas(ImposterBase imposter)
        {
            int i;
            int format = imposter.lastUpdateConfig.textureResolution;
            for (i = 0; i < _atlases.Count; i++)
            {
                if (_atlases[i] == null)
                {
                    _atlases.RemoveAt(i);
                    i--;
                    continue;
                }
                if (!_atlases[i].isFull && _atlases[i].format == format)
                    return _atlases[i];
            }
            Shader shader = _meshRendererShader;
            if (_impostersRenderType == ImposterRenderType.drawMesh)
            {
                shader = _drawMeshShader;
            }
            _atlases.Add(CreateNewAtlasHandler(format, atlasResolution.GetHashCode(), shader));
            return _atlases[_atlases.Count - 1];
        }

        internal AtlasHandler FindShadowAtlas(ShadowCasterDrawMesh shadowCaster)
        {
            int i;
            int format = shadowCaster.lastUpdateConfig.textureResolution;
            for (i = 0; i < _shadowAtlases.Count; i++)
            {
                if (_shadowAtlases[i] == null)
                {
                    _shadowAtlases.RemoveAt(i);
                    i--;
                    continue;
                }
                if (!_shadowAtlases[i].isFull && _shadowAtlases[i].format == format)
                    return _shadowAtlases[i];
            }
            _shadowAtlases.Add(CreateNewAtlasHandler(format, _shadowAtlasResolution.GetHashCode(), _imposterShadowCasterShader, UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly));
            _shadowAtlases[_shadowAtlases.Count - 1].name = "ShadowAtlas " + (_shadowAtlases.Count - 1).ToString();
            return _shadowAtlases[_shadowAtlases.Count - 1];
        }


#endregion // internal methods

#region PUBLIC methods
        public void UpdateAllImpostersImmediately()
        {
            QueueType qt = queueType;
            queueType = QueueType.none;
            foreach (CameraDetector cd in _camers)
            {
                ProcessCamera(cd, true);
            }
            queueType = qt;
        }

        static int _layerForRender = -1;
        public static int layerForRender
        {
            get
            {
                if (_layerForRender == -1)
                    _layerForRender = LayerMask.NameToLayer("imposterRender");
                if (_layerForRender == -1)
                {
                    Debug.LogError("You must set the 'imposterRender' layer in unity editor before use ImposterSystem in builds!!!");
                }
                return _layerForRender;
            }
        }

        public Camera GetImposterCamera()
        {
            if (_imposterCamera)
                return _imposterCamera;
            GameObject camGO = GameObject.Find("imposterCamera");
            if (camGO)
            {
                if (!camGO.activeInHierarchy)
                    camGO.SetActive(true);
                if (!camGO.GetComponent<Camera>())
                {
                    Helper.Destroy(camGO);
                    return GetImposterCamera();
                }
                else
                    return _imposterCamera = camGO.GetComponent<Camera>();
            }
            else
            {
                camGO = new GameObject();
                camGO.transform.parent = transform;
                camGO.transform.localPosition = Vector3.zero;
                camGO.name = "imposterCamera";
                camGO.AddComponent<Camera>();
                camGO.hideFlags = HideFlags.DontSave;
                _imposterCamera = camGO.GetComponent<Camera>();
                ImposterCameraSettings(_imposterCamera);
                return _imposterCamera;
            }
        }

#endregion // public methods




#endregion



#region ImposterTexturesController
        //[Header("Textures Controller")]
        //[SerializeField]
        float _unusedTexturesLifetime = 5f;
        List<ImposterTexture> _imposterTextures = new List<ImposterTexture>();
        internal ImposterTexture GetImposterTexture(int size, int depth = 16)
        {
            ImposterTexture imposterTex = null;
            for (int i = 0; i < _imposterTextures.Count; i++)
            {
                if (!_imposterTextures[i].isUsed && _imposterTextures[i].size == size && _imposterTextures[i].rt.depth == depth)
                {
                    imposterTex = _imposterTextures[i];
                    break;
                }
            }
            if (imposterTex == null)
            {
                imposterTex = new ImposterTexture(size, depth);
                _imposterTextures.Add(imposterTex);
            }
            imposterTex.isUsed = true;
            return imposterTex;
        }

        internal void GetBackTexture(ImposterTexture bt)
        {
            if (bt == null)
                return;
            bt.isUsed = false;
            bt.rt.Release();
        }

        public float GetMemoryUsage()
        {
            float res = 0;
            for (int i = 0; i < _imposterTextures.Count; i++)
            {
                res += _imposterTextures[i].memory;
            }
            return res;
        }

        public void DestroyUnusedTextures(float unusedTime = 0)
        {
            for (int i = 0; i < _imposterTextures.Count; i++)
            {
                if (!_imposterTextures[i].isUsed && (Time.time - _imposterTextures[i].lastUsedTime > unusedTime))
                {
                    _imposterTextures[i].DiscardContent();
                    _imposterTextures.RemoveAt(i);
                    i--;
                }
            }
        }


#endregion



#region VIEW CONTROL
        //////////////////////////////////////////////////////////////////////////////////////
        /// VIEW CONTROL
        /// 
        internal List<CameraDetector> _camers { get; private set; }

        Dictionary<int, List<Renderable>> _passToRenderables;
        internal CameraDetector onPreCullCamera { get; set; }
        internal Camera specificCamera { get; set; }

        Plane[] cameraPlanes;
        float sizer;

        void InitViewController()
        {
            _passToRenderables = new Dictionary<int, List<Renderable>>();
            _camers = new List<CameraDetector>();
            cameraPlanes = new Plane[6];
        }

        void OnEnableViewControl()
        {
            Camera.onPreCull += OnPreCullForCamera;
        }

        void OnDisableViewControl()
        {
            Camera.onPreCull -= OnPreCullForCamera;
            _passToRenderables.Clear();
        }

        void UpdateViewControl()
        {

        }

        internal int AddRenderable(Renderable t)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                //				Debug.Log("[ViewControl] Dont add imposter at editor mode.");
                return -1;
            }
#endif
            List<Renderable> rends;
            if (!_passToRenderables.TryGetValue(t.renderPass, out rends))
            {
                rends = new List<Renderable>();
                _passToRenderables.Add(t.renderPass, rends);
            }
            rends.Add(t);
            return rends.Count - 1;
        }

        internal void RemoveRenderable(Renderable t)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                return;
#endif
            if (t.indexInViewContol == -1)
            {
                //Debug.LogError("WTF!?");
                return;
            }
            List<Renderable> rends;
            if (!_passToRenderables.TryGetValue(t.renderPass, out rends))
            {
                return;
            }
            if (t.indexInViewContol >= rends.Count || rends[t.indexInViewContol] != t)
            {
                return;
            }
            rends[t.indexInViewContol] = rends[rends.Count - 1];
            rends[t.indexInViewContol].indexInViewContol = t.indexInViewContol;
            rends.RemoveAt(rends.Count - 1);
        }


        internal void AddCamera(CameraDetector camera)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                return;
#endif

            if (_camers.Contains(camera))
                return;
            _camers.Add(camera);
        }

        internal void RemoveCamera(CameraDetector camera)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                return;
#endif
            _camers.Remove(camera);
            for (int pass = 0; pass < 2; pass++)
            {
                List<Renderable> rends;
                if (_passToRenderables.TryGetValue(pass, out rends))
                {
                    for (int i = 0; i < rends.Count; i++)
                    {
                        rends[i].RemoveCamera(camera);
                    }
                }
            }

        }


        internal void OnPreCullForCamera(Camera cam)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                return;
#endif

            CameraDetector camDetector = null;
            bool found = false;
            for (int i = 0; i < _camers.Count; i++)
            {
                if (_camers[i].thisCamera.Equals(cam))
                {
                    found = true;
                    camDetector = _camers[i];
                    break;
                }
            }
            if (!found)
                return;
            if (camDetector.isVrMainCamera)
                return;
            Profiler.BeginSample("ImposterSystem");
            specificCamera = null;
            bool needProcess = camDetector.updateType == CameraDetector.UpdateType.OnPreCull;
            if (camDetector.isVrEyeCamera)
            {
                specificCamera = camDetector.thisCamera;
                if (camDetector.isLeftVrEyeCamera && needProcess)
                {
                    ProcessCamera(camDetector.mainVrCamera);
                }
                camDetector = camDetector.mainVrCamera;
            }
            else
            {
                if (needProcess)
                    ProcessCamera(camDetector);
            }

            //Debug.Log("OnPreCullForCamera "+ camDetector.name);
            Profiler.BeginSample("Calling a DrawMesh");
            if (!camDetector.ignoreImposterSystem)
            {
                for (int i = 0; i < _atlases.Count; i++)
                {
                    _atlases[i].DrawAll(camDetector, specificCamera);
                }
                if (shadowCastingEnabled)
                {
                    for (int i = 0; i < _shadowAtlases.Count; i++)
                    {
                        _shadowAtlases[i].DrawAll(camDetector, null);
                    }
                }
            }
            Profiler.EndSample();
            Profiler.EndSample();
        }

        internal void ProcessCamera(CameraDetector camDetector, bool forceVisability = false)
        {
            Profiler.BeginSample("Process camera");
            if (disableImpostersUpdating)
                return;
            bool isVisible;
            float _multiplier;
            Vector3 _cameraPosition;
            Renderable _curRenderable;
            Vector3 imposterPosition;
            Camera cam = camDetector.thisCamera;
            float shadowDistance = QualitySettings.shadowDistance;
            if (shadowCastingEnabled && light == null)
            {
                shadowCastingEnabled = false;
                Debug.LogError("You need to specify light at ImpostersHandler, if you want imposter shadow casting");
            }
            bool shadowsEnabledOnLight = light != null && light.shadows != LightShadows.None;
            onPreCullCamera = camDetector;
            _multiplier = 2 * Mathf.Tan(cam.fieldOfView / 2 * Mathf.Deg2Rad);
            _cameraPosition = cam.transform.position;
            Vector3 cameraDirection = cam.transform.forward.normalized;
            bool isCameraOrthographic = cam.orthographic;
            Vector3 fromCamToBill = Vector3.zero;
            CalculateFrustumPlanes(cam, cameraPlanes, preloadFactor);
            for (int pass = camDetector.startPass; pass < 2; pass++)
            {
                List<Renderable> rends;
                if (_passToRenderables.TryGetValue(pass, out rends))
                {
                    for (int index = 0; index < rends.Count; index++)
                    {
                        _curRenderable = rends[index];
                        if (!_curRenderable.isStatic)
                            _curRenderable.UpdatePosition();
                        imposterPosition = _curRenderable.bounds.center;
                        fromCamToBill = _cameraPosition - imposterPosition;
                        _curRenderable.nowDistance = fromCamToBill.magnitude;
                        sizer = _curRenderable.quadSize / (_curRenderable.nowDistance * _multiplier);
                        _curRenderable.screenSize = sizer;
                        isVisible = forceVisability || GeometryUtility.TestPlanesAABB(cameraPlanes, _curRenderable.bounds);//  getIsVisible;
                        _curRenderable.isVisible = isVisible;

                        if (isVisible)
                        {
                            if (isCameraOrthographic)
                                _curRenderable.nowDirection = cameraDirection;
                            else
                                _curRenderable.nowDirection = fromCamToBill;
                            _curRenderable.WillRendered();
                        }
                        if ((shadowCastingEnabled || (shadowsEnabledOnLight && !isVisible)) && _curRenderable.nowDistance < shadowDistance)
                            _curRenderable.ShadowWillRendered();
                    }
                }
            }
            Profiler.EndSample();
        }

        private void CalculateFrustumPlanes(Camera cam, Plane[] planes, float fovMult)
        {
            Matrix4x4 projMat = Matrix4x4.Perspective(cam.fieldOfView * fovMult,
                cam.aspect,
                cam.nearClipPlane,
                cam.farClipPlane);
            Matrix4x4 mat = projMat * cam.worldToCameraMatrix;
            // left
            planes[0].normal = new Vector3(mat.m30 + mat.m00, mat.m31 + mat.m01, mat.m32 + mat.m02);
            planes[0].distance = mat.m33 + mat.m03;

            // right
            planes[1].normal = new Vector3(mat.m30 - mat.m00, mat.m31 - mat.m01, mat.m32 - mat.m02);
            planes[1].distance = mat.m33 - mat.m03;

            // bottom
            planes[2].normal = new Vector3(mat.m30 + mat.m10, mat.m31 + mat.m11, mat.m32 + mat.m12);
            planes[2].distance = mat.m33 + mat.m13;

            // top
            planes[3].normal = new Vector3(mat.m30 - mat.m10, mat.m31 - mat.m11, mat.m32 - mat.m12);
            planes[3].distance = mat.m33 - mat.m13;

            // near
            planes[4].normal = new Vector3(mat.m30 + mat.m20, mat.m31 + mat.m21, mat.m32 + mat.m22);
            planes[4].distance = mat.m33 + mat.m23;

            // far
            planes[5].normal = new Vector3(mat.m30 - mat.m20, mat.m31 - mat.m21, mat.m32 - mat.m22);
            planes[5].distance = mat.m33 - mat.m23;

            // normalize
            for (uint i = 0; i < 6; i++)
            {
                float length = planes[i].normal.magnitude;
                planes[i].normal /= length;
                planes[i].distance /= length;
            }
        }


#endregion
    }

}
