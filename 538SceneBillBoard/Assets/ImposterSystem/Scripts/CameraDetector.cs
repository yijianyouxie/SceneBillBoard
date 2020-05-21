using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace ImposterSystem
{

    public class CameraDetector : MonoBehaviour
    {

        private static int _nextID = 0;

        internal int id { get; private set; }
        internal Camera thisCamera { get; private set; }

        internal bool useClipPlane { get; private set; }
        internal Vector3 clipPlanePoint { get; private set; }
        internal Vector3 clipPlaneNormal { get; private set; }

        internal bool overrideZOffset { get; private set; }
        internal float zOffset { get; private set; }

        internal bool overrideMaxTextureRes { get; private set; }
        internal TextureResolution maxTextureResolution { get; private set; }

        internal int startPass = 0;

        internal bool isVrMainCamera { get { return _isVrMainCamera; } }
        internal CameraDetector leftEyeCamera { get; private set; }
        internal CameraDetector rightEyeCamera { get; private set; }
        internal UpdateType updateType { get { return _updateType; } }
        internal bool isVrEyeCamera { get; set; }
        internal bool isLeftVrEyeCamera { get; set; }
        internal CameraDetector mainVrCamera { get; set; }

        public enum UpdateType
        {
            Update,
            LateUpdate,
            OnPreCull
        }

        #region INSPECTOR
        public bool ignoreImposterSystem = false;
        public bool ignoreQueue = false;
        [SerializeField] private UpdateType _updateType = UpdateType.OnPreCull;
        [SerializeField] private bool _isVrMainCamera = false;
        [SerializeField] private Camera _leftEyeCamera;
        [SerializeField] private Camera _rightEyeCamera;
        #endregion //INSPECTOR

        private void Awake()
        {
            id = _nextID++;
        }

        void OnEnable()
        {
            if (!GetComponent<Camera>())
            {
                Debug.LogError("CameraDetector require Camera component on gameObject!!!");
                Helper.Destroy(this);
            }
            if (isVrMainCamera)
            {
                isVrEyeCamera = false;
                isLeftVrEyeCamera = false;
                leftEyeCamera = _leftEyeCamera.gameObject.AddComponent<CameraDetector>();
                rightEyeCamera = _rightEyeCamera.gameObject.AddComponent<CameraDetector>();

                leftEyeCamera.hideFlags = HideFlags.HideInInspector;
                rightEyeCamera.hideFlags = HideFlags.HideInInspector;

                leftEyeCamera.mainVrCamera = this;
                rightEyeCamera.mainVrCamera = this;

                leftEyeCamera.isVrEyeCamera = true;
                rightEyeCamera.isVrEyeCamera = true;

                leftEyeCamera.isLeftVrEyeCamera = true;
                rightEyeCamera.isLeftVrEyeCamera = false;
            }
            thisCamera = GetComponent<Camera>();
            ImpostersHandler.Instance.AddCamera(this);
            ResetClipPlane();
            ResetZOffset();
        }

        void OnDisable()
        {
            if (ImpostersHandler.Instance)
                ImpostersHandler.Instance.RemoveCamera(this);
        }

        private void Update()
        {
            if (_updateType == UpdateType.Update)
                Process();
        }

        private void LateUpdate()
        {
            if (_updateType == UpdateType.LateUpdate)
                Process();
        }

        private void Process()
        {
            ImpostersHandler.Instance.ProcessCamera(this);
        }

        internal void SetClipPlane(Vector3 clipPlanePoint, Vector3 clipPlaneNormal)
        {
            this.useClipPlane = true;
            this.clipPlanePoint = clipPlanePoint;
            this.clipPlaneNormal = clipPlaneNormal;
        }

        internal void ResetClipPlane()
        {
            useClipPlane = false;
            clipPlanePoint = Vector3.zero;
            clipPlaneNormal = Vector3.zero;
        }

        internal void SetZOffset(float zOffset)
        {
            overrideZOffset = true;
            this.zOffset = zOffset;
        }

        internal void ResetZOffset()
        {
            overrideZOffset = false;
            zOffset = 0;
        }

        internal void SetMaxTextureResolution(TextureResolution maxTextureResolution)
        {
            overrideMaxTextureRes = true;
            this.maxTextureResolution = maxTextureResolution;
        }

        //public bool Equals(object o)
        //{
        //    CameraDetector cd = o as CameraDetector;
        //    if (cd == null)
        //        return false;
        //    return cd.id == this.id;
        //}

        public bool Equals(CameraDetector cd)
        {
            return cd.id == id;
        }
    }

}