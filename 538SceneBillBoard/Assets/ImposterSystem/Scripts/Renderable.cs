using UnityEngine;
using System.Collections;

namespace ImposterSystem
{
    public abstract class Renderable : MonoBehaviour
    {

        protected bool _isVisible;
        internal virtual bool isVisible
        {
            get { return _isVisible; }
            set { _isVisible = value; }
        }

        [SerializeField] protected bool _isStatic = true;
        internal virtual bool isStatic
        {
            get { return _isStatic; }
            set { _isStatic = value; }
        }
        [HideInInspector]
        [SerializeField] internal Transform _transform;

        internal int indexInViewContol;

        protected Bounds _bounds;
        public Bounds bounds { get { return _bounds; } }
        //[HideInInspector]
        [SerializeField] protected Vector3 _center;
        public Vector3 center { get { return _center; } }
        //[HideInInspector]
        [SerializeField] protected Vector3 _size;
        public Vector3 size { get { return _size; } }
        //[HideInInspector]
        [SerializeField] protected float _quadSize;
        public float quadSize { get { return _quadSize; } }

        // direction from camera to center of this object
        internal Vector3 nowDirection;
        /// <summary>
        /// distance from camera to center of this object, same as nowDirection.magnitude
        /// </summary>
        internal float nowDistance;
        // screen size of this object
        internal float screenSize;

        #region abstract Methods

        internal abstract void WillRendered();
        internal abstract void ShadowWillRendered();
        internal abstract void RemoveCamera(CameraDetector camera);
        internal abstract int renderPass { get; }

        #endregion


        #region Unity Events
        protected virtual void Reset()
        {
            _transform = transform;
            RecalculateBounds();
        }

        protected virtual void OnEnable()
        {
            _transform = transform;
            _bounds.center = _transform.position + center;
            _bounds.size = size;
            if (ImpostersHandler.Instance)
                indexInViewContol = ImpostersHandler.Instance.AddRenderable(this);
        }

        protected virtual void OnDisable()
        {
            if (ImpostersHandler.Instance)
                ImpostersHandler.Instance.RemoveRenderable(this);
        }
        #endregion


        public virtual void RecalculateBounds()
        {
            //Debug.Log("Renderable.RecalculateBounds()");

            Bounds bound = GetComponent<Renderer>().bounds;
            
            _bounds = bound;
            _center = bound.center - transform.position;
            _size = bound.size;
            _quadSize = Helper.MaxV3(size);
        }

        public virtual void UpdatePosition()
        {
            if (_isStatic)
                return;
            _bounds.center = _transform.position + center;
        }

    }
}