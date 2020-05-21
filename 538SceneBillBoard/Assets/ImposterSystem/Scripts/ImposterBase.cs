using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace ImposterSystem
{

    [ExecuteInEditMode]
    public abstract class ImposterBase : MonoBehaviour
    {


        #region private 

        protected float _quadSizeHalf;
        protected float _quadSize;
        protected int newTextureResolution;

        #endregion //private 

        #region protected 

        protected Vector3 _nowDirection = Vector3.one;
        protected Material _material;
        protected Vector4 _uvs;
        protected Quaternion _rotation;
        protected Vector3 _position;
        protected float _zOffset;

        protected Transform _transform;
        protected GameObject _gameObject;
        protected ImpostersHandler _imposterHandler;
        protected Camera _imposterCamera;

        protected bool _ignoreNextHide;
        #endregion //protected

        #region internal
        internal bool isActive;
        internal bool isVisible;
        internal bool isGenerated;
        internal int imposterLODIndex;
        internal bool renderShadows;
        internal float screenSize;
        internal float distance;
        internal bool isInQueue = false;
        internal float maxSize;
        internal LastUpdateConfig lastUpdateConfig = new LastUpdateConfig();
        internal ImposterController imposterController;

        #endregion //internal
        

        [SerializeField] protected CameraDetector _camera;
        /// <summary>
        /// Gets or sets the camera. Imposter will render only for this camera.
        /// </summary>
        /// <value>My camera.</value>
        public CameraDetector myCamera
        {
            get
            {
                return _camera;
            }
            set
            {
                if (value == null)
                {
                    Debug.LogError("Trying to set NULL camera!");
                }
                _camera = value;
            }
        }

        protected abstract void UpdateVertices();

        protected abstract ImposterTexture imposterTexture { get; }

        /// <summary>
        /// Sets the size of the quad. Call UpodateVerts().
        /// Sets isGenereted & isActive to false.
        /// </summary>
        /// <value>The size of the quad.</value>
        /// 
        protected virtual float quadSize
        {
            set
            {
                if (_quadSize == value)
                    return;
                _quadSize = value;
                _quadSizeHalf = _quadSize * 0.5f;
                UpdateVertices();
            }
        }

        /// <summary>
        /// Gets or sets the z offset.
        /// zOffset - distance in units from imposter GO to center of original GO.
        /// </summary>
        /// <value>The z offset.</value>
        internal virtual float zOffset
        {
            get { return _zOffset; }
            set
            {
                if (_zOffset == value)
                    return;
                _zOffset = value;
                UpdateVertices();
            }
        }

        /// <summary>
        /// Gets or sets the position of the imposter GO.
        /// </summary>
        /// <value>The position.</value>
        internal virtual Vector3 position
        {
            set { _position = value; }
            get { return _position; }
        }

        /// <summary>
        /// Sets the rotation of the imposter GO.
        /// </summary>
        /// <value>The rotation.</value>
        internal virtual Quaternion rotation
        {
            get { return _rotation; }
            set { _rotation = value; }
        }

        /// <summary>
        /// Sets the UV of imposter mesh.
        /// </summary>
        /// <value>The UVs.</value>
        internal virtual Vector4 UVs {
            get { return _uvs; }
            set { _uvs = value; }
        }


        /// <summary>
        /// Gets or sets the array of materials. Array contains only one material - atlas material or self material.
        /// </summary>
        /// <value>The materials.</value>
        internal virtual Material material
        {
            set { _material = value; }
            get { return _material; }
        }


        /// <summary>
        /// Direction from camera to center of imposter.
        /// </summary>
        /// <value>The now direction.</value>
        internal virtual Vector3 nowDirection
        {
            get { return _nowDirection; }
            set { _nowDirection = value; }
        }

        internal virtual bool needUpdate { get { return true; } }

        internal virtual void ForcedAwake(ImposterController bc, CameraDetector camera)
        {
            imposterController = bc;
            myCamera = camera;
            maxSize = bc.quadSize;
            _imposterHandler = ImpostersHandler.Instance;
            _gameObject = base.gameObject;
            _gameObject.name = myCamera.name + " imposter";
            _transform = _gameObject.transform;
            _transform.parent = imposterController.transform;
            _transform.position = imposterController.bounds.center;
            _position = imposterController.bounds.center;
        }

        internal virtual void UpdateImposter()
        {
            isInQueue = false;
            _imposterCamera = _imposterHandler.GetImposterCamera();
            int maxTexRes = (int)imposterController.m_LODs[imposterLODIndex].maxImposterResolution;
            newTextureResolution = ImposterLODUtility.GetImposterTextureResolution(screenSize, (int)imposterController.m_LODs[imposterLODIndex].minImposterResolution, maxTexRes);
            imposterController.UpdatePosition();

            SetupImposterCamera();

            _imposterCamera.targetTexture = imposterTexture.rt;
            _imposterCamera.rect = GetRenderRect();

            // save settings
            float oldShadowsDistance = QualitySettings.shadowDistance;
            var oldShadowProjection = QualitySettings.shadowProjection;
            bool oldInversCulling = GL.invertCulling;
            //apply new settings
            if (imposterController.m_LODs[imposterLODIndex].renderShadows)
            {
                QualitySettings.shadowDistance = distance + maxSize;
                QualitySettings.shadowProjection = ShadowProjection.CloseFit;
            }
            else
                QualitySettings.shadowDistance = 0;
            GL.invertCulling = false;

            imposterController.PrepareToRender(imposterLODIndex, ImpostersHandler.layerForRender);
            RenderImposterCamera();
            imposterController.AfterRender();

            // restore old settigns
            GL.invertCulling = oldInversCulling;
            QualitySettings.shadowDistance = oldShadowsDistance;
            QualitySettings.shadowProjection = oldShadowProjection;


            lastUpdateConfig.cameraDirection = nowDirection;
            lastUpdateConfig.objectForwardDirection = imposterController._transform.forward;
            lastUpdateConfig.time = Time.time;
            lastUpdateConfig.screenSize = screenSize;
            lastUpdateConfig.distance = distance;
            lastUpdateConfig.lightDirection = imposterController.useErrorLightAngle ? ImpostersHandler.Instance.lightDirection : Vector3.zero;
            lastUpdateConfig.textureResolution = newTextureResolution;

            this.isActive = true;
            this.isGenerated = true;
        }

        protected virtual void SetupImposterCamera()
        {
            Transform currentCamTrans = _camera.transform;

            Transform _imposterCameraTransform = _imposterCamera.transform;
            Vector3 locBillPos = this.position;
            Vector3 fromCamToCenter = _nowDirection = currentCamTrans.position - locBillPos;
            _imposterCameraTransform.rotation = Quaternion.LookRotation(-fromCamToCenter);
            _imposterCameraTransform.position = currentCamTrans.position;
            float imposterQuadSize = this.maxSize / 2;
            this.quadSize = imposterQuadSize * 2;

            fromCamToCenter = _imposterCameraTransform.position - locBillPos - fromCamToCenter.normalized * _zOffset;
            float angleForCamera = 2 * Mathf.Atan2(imposterQuadSize, fromCamToCenter.magnitude) * Mathf.Rad2Deg;
            _imposterCamera.orthographic = false;
            _imposterCamera.fieldOfView = angleForCamera;
            _imposterCamera.farClipPlane = (currentCamTrans.position - locBillPos).magnitude + this.maxSize;
            _imposterCamera.nearClipPlane = Mathf.Max((currentCamTrans.position - locBillPos).magnitude - this.maxSize, 0.03f);
            _imposterCamera.ResetProjectionMatrix();

            if (_camera.useClipPlane)
            {
                _imposterCamera.ResetProjectionMatrix();
                Vector4 clipPlane = Helper.CameraSpacePlane(_imposterCamera, _camera.clipPlanePoint, _camera.clipPlaneNormal, 1.0f);
                _imposterCamera.projectionMatrix = _imposterCamera.CalculateObliqueMatrix(clipPlane);
            }

        }

        protected virtual void RenderImposterCamera()
        {
            if (_imposterHandler.shaderForRender)
                _imposterCamera.RenderWithShader(_imposterHandler.shaderForRender, "");
            else
            {
                _imposterCamera.Render();
            }
        }

        protected abstract Rect GetRenderRect();

        #region UnityEvents
        bool _isDestroying;
        void OnDestroy()
        {
            _isDestroying = true;
            DestroyImposter();
        }
        
        #endregion


        bool isDiscardingContent = false;

        /// <summary>
        /// Destroys all imposters data and destroys imposter GO.
        /// </summary>
        internal void DestroyImposter()
        {
            if (isDiscardingContent)
                return;
            isDiscardingContent = true;
            Release();
            RemoveMembers();
            _camera = null;
            imposterController = null;
            if (!_isDestroying)
            {
                Helper.Destroy(this._gameObject);
            }
        }

        internal virtual void Release()
        {
            if (isInQueue)
                ImpostersHandler.Instance.RemoveImposterFromQueue(this);
            isInQueue = false;
            isActive = false;
            isGenerated = false;
        }

        /// <summary>
        /// Removes the members from AtlasHendler, ImposterController, ImposterHendler.
        /// </summary>
        protected virtual void RemoveMembers()
        {
            if (imposterController)
                imposterController.RemoveImposter(this);
        }

        /// <summary>
        /// Makes imposter renderable for camera.
        /// </summary>
        internal abstract void Show(bool ignoreNextHide = false);

        /// <summary>
        /// Makes imposter unrenderable for camera.
        /// </summary>
        internal abstract void Hide();

    }
    [Serializable]
    internal struct LastUpdateConfig
    {
        public float time;
        public Vector3 lightDirection;
        public Vector3 cameraDirection;
        public Vector3 objectForwardDirection;
        public float screenSize;
        public float distance;
        public int textureResolution;
    }
}