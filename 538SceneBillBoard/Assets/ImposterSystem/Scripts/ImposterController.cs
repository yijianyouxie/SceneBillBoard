using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ImposterSystem
{

    //[ExecuteInEditMode]
    public class ImposterController : Renderable
    {

        [SerializeField] public ImposterLOD[] m_LODs;

        public enum UpdateBehavior {
            Show3DObject_WaitForUpdate,
            ShowLastImposter_WaitForUpdate,
            ImmediatelyUpdate
        }
        

        ////////////////// Update mode
        
        public bool alwaysLookAtCamera = true;
        public UpdateBehavior updateBehavior = UpdateBehavior.ShowLastImposter_WaitForUpdate;
        public float errorCameraAngle = 5f;
        public bool useErrorLightAngle = false;
        public float errorLightAngle = 10f;
        public float errorDistance = 0.15f;
        public float ZOffset = 0.5f;
        public bool useUpdateByTime = false;
        public float timeInterval = 1f;

        //Debug
        [SerializeField] private bool _change;
        [SerializeField] float _angle;


        /// <summary>
        /// Dont use!!!		/// </summary>
        public List<OriginalGOController> _originObjects = new List<OriginalGOController>();

        [SerializeField] private List<ImposterBase> _imposters;
        [SerializeField] private ShadowCasterDrawMesh _shadowCaster;

        int _currentLODIndex = -1;

        internal override bool isStatic
        {
            get { return _isStatic; }
            set { _isStatic = value; }
        }

        internal override int renderPass
        {
            get { return 1; }
        }

        protected override void Reset()
        {
            if (m_LODs == null || m_LODs.Length == 0)
            {
                m_LODs = new ImposterLOD[2];
                m_LODs[0] = new ImposterLOD(0.2f, new OriginalGOController[0], false, false);
                m_LODs[1] = new ImposterLOD(0.01f, new OriginalGOController[0], true, false);
            }
            base.Reset();

            alwaysLookAtCamera = true;
            errorCameraAngle = 3;
            _imposters = new List<ImposterBase>();
        }

        void SetInitialReferences()
        {
            _transform = transform;
            _imposterHandler = ImpostersHandler.Instance;
            RemoveImposters();
            _imposters = new List<ImposterBase>();
            RecalculateBounds();
            for (int i = 0; i < _originObjects.Count; i++)
            {
                _originObjects[i].SetUp();
            }
            _currentLODIndex = -1;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SetInitialReferences();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            RemoveImposters();
            if (_shadowCaster != null)
            {
                _shadowCaster.DestroyImposter();
            }
            _shadowCaster = null;
#if UNITY_EDITOR
            if (Application.isPlaying)
#endif
                if (ImpostersHandler.Instance)
                    ImpostersHandler.Instance.RemoveRenderable(this);
            ShowOriginalGO(0);
        }


        internal override bool isVisible
        {
            get { return _isVisible; }
            set
            {
                if (!value)
                {
                    if (_imposterHandler._invisibleImposterAction == InvisibleImposterAction.releaseImposter && FindCurrentImposter() && _curImposter.isActive)
                    {
                        _curImposter.Release();
                        GoToLOD(-1);
                    }
                }
                _isVisible = value;
            }
        }

        public override void UpdatePosition()
        {
            base.UpdatePosition();
            for (i = 0; i < _imposters.Count; i++)
            {
                _imposters[i].position = bounds.center;
            }
        }

        internal override void WillRendered()
        {
            _curCamera = _imposterHandler.onPreCullCamera;
            if (screenSize > 1)
            {
                screenSize = 1;
            }
            else if (screenSize < 0)
            {
                screenSize = 0;
            }

            _currentLODIndex = GetCurrentLODIndex(screenSize);
            //~1.5

            // if we dont need imposter anymore
            if (_currentLODIndex == -1 || !m_LODs[_currentLODIndex].isImposter || _curCamera.ignoreImposterSystem)
            {
                GoToLOD(_currentLODIndex);
                return;
            }
            //check if imposter exist
            if (!FindCurrentImposter())
            {
                CreateCurrentImposter();
            }

            _curImposter.isVisible = true;
            _curImposter.screenSize = screenSize;
            _curImposter.distance = nowDistance;
            if (_curCamera.overrideZOffset)
                _curImposter.zOffset = _curCamera.zOffset * _curImposter.maxSize;
            else
                _curImposter.zOffset = ZOffset * _curImposter.maxSize;
           

            if (needUpdate())
            {
                _curImposter.imposterLODIndex = _currentLODIndex;
                _curImposter.nowDirection = nowDirection;
                if (_curCamera.ignoreQueue ||
                    updateBehavior == UpdateBehavior.ImmediatelyUpdate ||
                    !_curImposter.isActive)
                {
                    _curImposter.UpdateImposter();
                }
                else
                {
                    if (updateBehavior == UpdateBehavior.Show3DObject_WaitForUpdate)
                    {
                        _curImposter.Release();
                    }
                    _imposterHandler.AddImposterToQueue(_curImposter);
                }
            }
            if (alwaysLookAtCamera)
                _curImposter.nowDirection = nowDirection;
            
            // if imposter ready to be showen
            if (_curImposter.isGenerated)
            {
                GoToLOD(_currentLODIndex);
            }
            else
            {
                ShowOriginalGO(_currentLODIndex);
            }
        }

        internal override void ShadowWillRendered()
        {
            _curCamera = _imposterHandler.onPreCullCamera;
            if (isVisible == false)
            {
                _currentLODIndex = GetCurrentLODIndex(screenSize);
                if (_currentLODIndex == -1 || !m_LODs[_currentLODIndex].isImposter)
                {
                    GoToLOD(0);
                    return;
                }
            }
            else // need that else 
            {
                if (_currentLODIndex == -1 || !m_LODs[_currentLODIndex].isImposter ||  !_curImposter.isGenerated)
                {
                    if (_shadowCaster != null)
                        _shadowCaster.DestroyImposter();
                    return;
                }
            }

            if (_shadowCaster == null)
                CreateShadowCaster();

            _shadowCaster.isVisible = true;
            _shadowCaster.screenSize = screenSize;
            _shadowCaster.distance = nowDistance;
            _shadowCaster.zOffset = ZOffset * _shadowCaster.maxSize;
            if (IsShadowCasterNeedUpdate())
            {
                _shadowCaster.imposterLODIndex = _currentLODIndex;
                _shadowCaster.nowDirection = nowDirection;
                if (_shadowCaster.isActive && !_curCamera.ignoreQueue)
                    _imposterHandler.AddImposterToQueue(_shadowCaster);
                else
                    _shadowCaster.UpdateImposter();
            }
            if (_shadowCaster.isGenerated)
                _shadowCaster.Show();
        }

        [SerializeField] float debugDistanceDelta;

        /// <summary>
        /// Return true, if imposter needs updating.
        /// </summary>
        /// <returns><c>true</c>, if update was needed, <c>false</c> otherwise.</returns>
        private bool needUpdate()
        {
            // if already waiting for update
            if (_curImposter.isInQueue || !_curImposter.needUpdate)
            {
                return false;
            }
            if (!_curImposter.isActive)
                return true;
            if (useUpdateByTime && (Time.time - _curImposter.lastUpdateConfig.time) > timeInterval)
                return true;

            // check if angle from last update bigger then maxAngleTreshhold
            _angle = Helper.AngleInDeg(_curImposter.lastUpdateConfig.cameraDirection, nowDirection);
            if (!isStatic)
                _angle += Helper.AngleInDeg(_curImposter.lastUpdateConfig.objectForwardDirection, _transform.forward);
            if (_angle > errorCameraAngle)
                return true;
            
            if (useErrorLightAngle)
            {
                _angle = Helper.AngleInDeg(_curImposter.lastUpdateConfig.lightDirection, _imposterHandler.lightDirection);
                if (_angle > errorLightAngle)
                    return true;
            }

            // if need to change resolution of imposter texture
            int maxTexRes = (int)m_LODs[_currentLODIndex].maxImposterResolution;
            if (_curCamera.overrideMaxTextureRes)
                maxTexRes = (int)_curCamera.maxTextureResolution;

            if (ImposterLODUtility.GetImposterTextureResolution(screenSize, (int)m_LODs[_currentLODIndex].minImposterResolution, maxTexRes) > _curImposter.lastUpdateConfig.textureResolution)
                return true;

            // if size on screen changed 
            debugDistanceDelta = Mathf.Abs(nowDistance - _curImposter.lastUpdateConfig.distance) / _curImposter.lastUpdateConfig.distance;
            if (debugDistanceDelta > errorDistance)
                return true;

            return false;
        }

        private bool IsShadowCasterNeedUpdate() {
            if (_shadowCaster.isInQueue || !_shadowCaster.needUpdate)
            {
                return false;
            }
            if (!_shadowCaster.isGenerated)
                return true;
            if (Helper.AngleInDeg(_imposterHandler.lightDirection, _shadowCaster.lastUpdateConfig.lightDirection) > _imposterHandler.lightDirectionDelta)
                return true;
            return false;
        }

        bool FindCurrentImposter()
        {
            for (int i = 0; i < _imposters.Count; i++)
            {
                if (_imposters[i].myCamera.Equals(_curCamera))
                {
                    _curImposter = _imposters[i];
                    return true;
                }
            }
            return false;
        }

        void CreateCurrentImposter()
        {
            GameObject newBill = new GameObject();
            if (_imposterHandler._impostersRenderType == ImposterRenderType.drawMesh)
                _curImposter = newBill.AddComponent<ImposterDrawMesh>();
            else
                _curImposter = newBill.AddComponent<ImposterAtlasMeshRenderer>();
            _curImposter.ForcedAwake(this, _curCamera);
            _imposters.Add(_curImposter);
        }

        void CreateShadowCaster()
        {
            GameObject newBill = new GameObject();
            _shadowCaster = newBill.AddComponent<ShadowCasterDrawMesh>();
            _shadowCaster.ForcedAwake(this, _curCamera);
        }

        /// <summary>
        /// Adds already created imposters in the queue to update.
        /// </summary>
        public void UpdateImposter()
        {
            foreach (var b in _imposters)
            {
                if (!b.isInQueue)
                    ImpostersHandler.Instance.AddImposterToQueue(b);
            }
        }

        /// <summary>
        /// Adds already created imposter for specific camera in the queue to update.
        /// </summary>
        /// <param name="camera">Camera.</param>
        public void UpdateImposter(Camera camera)
        {
            //Imposter b;
            foreach (var b in _imposters)
            {
                if (b.myCamera.thisCamera == camera)
                    ImpostersHandler.Instance.AddImposterToQueue(b);
            }
        }

        /// <summary>
        /// Updates the already created imposters immediately.
        /// </summary>
        public void UpdateImposterImmediately()
        {
            // TODO set the nowDirection before update
            foreach (var b in _imposters)
            {
                b.UpdateImposter();
            }
        }

        /// <summary>
        /// Updates the already created for specific camera  imposter  immediately.
        /// </summary>
        /// <param name="camera">Camera.</param>
        public void UpdateImposterImmediately(Camera camera)
        {
            foreach (var b in _imposters)
            {
                if (b.myCamera.thisCamera == camera)
                    b.UpdateImposter();
            }
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            if (_change)
            {
                Gizmos.DrawWireCube(bounds.center, bounds.size);
                Gizmos.color = new Color(1, 0, 0, 0.3f);
                Vector3 direction = transform.position + center - SceneView.currentDrawingSceneView.camera.transform.position;
                Gizmos.DrawMesh(Helper.NewPlane(Vector3.zero, new Vector3(quadSize / 2, quadSize / 2, quadSize / 2), new Vector4(0, 0, 1, 1)), transform.position + center - direction.normalized * ZOffset * quadSize, Quaternion.LookRotation(direction));
                //				Gizmos.DrawSphere (transform.position + center, quadSize/2);
            }
        }
#endif


        internal void RemoveImposter(ImposterBase go)
        {
            _imposters.Remove(go);
        }

        public void RemoveImposters()
        {
            //			Debug.Log ("RemoveImposters");
            _curImposter = null;
            int count = _imposters.Count;
            for (int i = 0; i < count; i++)
            {
                if (_imposters[0] != null)
                    _imposters[0].DestroyImposter();
                else
                    _imposters.RemoveAt(0);
            }
            this._imposters.Clear();
        }

        internal override void RemoveCamera(CameraDetector camera)
        {
            foreach (var b in _imposters)
            {
                if (b.myCamera == camera)
                {
                    b.DestroyImposter();
                    break;
                }
            }
        }

        CameraDetector _curCamera;
        private ImposterBase _curImposter;
        ImpostersHandler _imposterHandler;
        int i, count;

        private void ShowOriginalGO(int imposterLODIndex)
        {
            ImposterLOD lod = m_LODs[imposterLODIndex];
            for (i = 0; i < lod.renderers.Length; i++)
            {
                lod.renderers[i].Show(true);
            }
            HideOriginalGO();
        }

        public void PrepareToRender(int imposterLODIndex, int layer)
        {
            ImposterLOD lod = m_LODs[imposterLODIndex];
            for (i = 0; i < lod.renderers.Length; i++)
            {
                lod.renderers[i].PrepareToRender(layer);
            }
            HideOriginalGO();
        }

        private void HideOriginalGO()
        {
            for (i = 0; i < _originObjects.Count; i++)
                _originObjects[i].Hide();
        }

        void ShowImposter()
        {
            _curImposter.Show(true);
            HideImposters();
        }

        void HideImposters()
        {
            for (i = 0; i < _imposters.Count; i++)
            {
                _imposters[i].Hide();
            }
        }

        /// <summary>
        /// Dont use!!! Sets the default layer for normal objects.
        /// </summary>
        public void AfterRender()
        {
            for (i = 0; i < _originObjects.Count; i++)
            {
                _originObjects[i].SetDefaultLayer();
                _originObjects[i].Hide();
            }
        }

        private int GetCurrentLODIndex(float screenSize)
        {
            for (int i = 0; i < m_LODs.Length; i++)
            {
                if (screenSize > m_LODs[i].screenRelativeTransitionHeight)
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Recalculates the bounds.
        /// </summary>
        public override void RecalculateBounds()
        {
            //Debug.Log("ImposterController.RecalculateBounds()");
            _originObjects = new List<OriginalGOController>();
            for (int j = 0; j < m_LODs.Length; j++)
            {
                ImposterLOD b = m_LODs[j];
                for (int i = 0; i < b.renderers.Length; i++)
                {
                    if (b.renderers[i] == null)
                    {
                        var list = new List<OriginalGOController>();
                        list.AddRange(b.renderers);
                        list.RemoveAt(i);
                        b.renderers = list.ToArray();
                        if (i > 0)
                            i--;
                        continue;
                    }
                    if (!_originObjects.Contains(b.renderers[i]))
                        _originObjects.Add(b.renderers[i]);
                }
            }
            
            //_originObjects.AddRange(GetComponentsInChildren<OriginalGOController>(true));
            Renderer[] rs = new Renderer[_originObjects.Count];
            for (int i = 0; i < _originObjects.Count; i++)
            {
                rs[i] = _originObjects[i].Renderer;
            }

            Bounds bound = new Bounds();

            foreach (Renderer r in rs)
            {
                if (r.GetComponent<ImposterBase>())
                    continue;
                if (bound.extents == Vector3.zero)
                    bound = r.bounds;
                else
                    bound.Encapsulate(r.bounds);
            }
            _bounds = bound;
            _center = bound.center - transform.position;
            _size = bound.size;
            _quadSize = Helper.MaxV3(size);

        }

        internal void GoToLOD(int lodIndex)
        {
            //if (lastLODIndex == lodIndex && !cameraChanged)
            //	return;
            if (lodIndex == -1)
            {
                HideOriginalGO();
                if (FindCurrentImposter())
                {
                    _curImposter.Release();
                }
                HideImposters();
                return;
            }
            ImposterLOD lod = m_LODs[lodIndex];
            if (lod.isImposter && !_curCamera.ignoreImposterSystem)
            {
                HideOriginalGO();
                ShowImposter();
            }
            else
            {
                //TODO check if it can be commented
                if (FindCurrentImposter() && _curImposter.isActive)
                {
                    _curImposter.Release();
                }
                ShowOriginalGO(lodIndex);
                HideImposters();
            }
        }
    }

}