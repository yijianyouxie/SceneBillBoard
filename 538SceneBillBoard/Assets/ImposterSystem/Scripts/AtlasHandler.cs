using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
#if UNITY_5_5_OR_NEWER
using UnityEngine.Profiling;
#endif


namespace ImposterSystem
{

    [System.Serializable]
    internal class AtlasHandler : MonoBehaviour
    {

        public ImposterTexture _atlas;
        [SerializeField] Shader shader;
        [SerializeField] UnityEngine.Rendering.ShadowCastingMode _shadowCastingMode;
        public int format;
        public bool isFull
        {
            get { return _nowTexCount == _maxTexCount; }
        }
        public bool isEmpty { get { return _nowTexCount == 0; } }
        [SerializeField] int _resolution = 2048;
        [SerializeField] int _size;
        [SerializeField] int _maxTexCount;
        [SerializeField] int _nowTexCount;
        [SerializeField] bool[] _isEmptyPlace;
        [SerializeField] Dictionary<CameraDetector, CombinedImpostersMesh> _meshForCamera;
#if UNITY_EDITOR
        [SerializeField] List<CombinedImpostersMesh> _forDebug;
#endif

#region localVars
        int i;
        int place;
        float x;
        float y;
        Vector2 offset;
#endregion

        public void ForcedAwake(int t, int atlasRes, Shader some, UnityEngine.Rendering.ShadowCastingMode shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off)
        {
            _shadowCastingMode = shadowCastingMode;
            format = t;
            //			Debug.Log ("Create new atlas: "+format);
            _resolution = atlasRes;
            _atlas = ImpostersHandler.Instance.GetImposterTexture(atlasRes, 16);
            //			_atlas.antiAliasing = 0;
            _size = _resolution / format;
            _maxTexCount = _size * _size;
            _nowTexCount = 0;
            _isEmptyPlace = new bool[_maxTexCount];
            shader = some;
            for (int i = 0; i < _maxTexCount; i++)
                _isEmptyPlace[i] = true;
            _meshForCamera = new Dictionary<CameraDetector, CombinedImpostersMesh>();
        }

        public int GetEmptyPlace()
        {
            for (i = 0; i < _maxTexCount; i++)
                if (_isEmptyPlace[i])
                    return i;
            return -1;
        }

        private void AddCamera(CameraDetector camera)
        {
            CombinedImpostersMesh newMesh = new CombinedImpostersMesh(_maxTexCount);
            _meshForCamera.Add(camera, newMesh);
#if UNITY_EDITOR
            if (_forDebug == null)
                _forDebug = new List<CombinedImpostersMesh>();
            _forDebug.Add(newMesh);
#endif

        }

        internal bool AddImposterToAtlas(BaseImposterAtlasSetup imposter)
        {
            place = GetEmptyPlace();
            if (placeIsOutOfRange(place))
            {
                return false;
            }
            if (!_isEmptyPlace[place])
            {
                Debug.LogError("cant be !isEmpty bec getEmptyPlace befor");
                return false;
            }
            _isEmptyPlace[place] = false;
            _nowTexCount++;
            imposter.atlas = this;
            imposter.placeInAtlas = place;
            //TODO separete method for this 
            if (imposter is ImposterDrawMesh)
            {
                CombinedImpostersMesh cbm;
                if (!_meshForCamera.TryGetValue(imposter.myCamera, out cbm))
                {
                    AddCamera(imposter.myCamera);
                    cbm = _meshForCamera[imposter.myCamera];
                }
                cbm.AddImposter(imposter as ImposterDrawMesh);
            }
            imposter.UVs = GetUVsAtPlace(place);
            return true;
        }

        internal void RemoveImposterFromAtlas(BaseImposterAtlasSetup imposter, bool asOld = false)
        {
            int place = !asOld ? imposter.placeInAtlas : imposter.placeInPrevAtlas;
            if (placeIsOutOfRange(place))
            {
                return;
            }
            _isEmptyPlace[place] = true;
            if (!asOld)
            {
                imposter.atlas = null;
                imposter.placeInAtlas = -1;
            }
            else
            {
                imposter.prevAtlas = null;
                imposter.placeInPrevAtlas = -1;
            }
            _nowTexCount--;
            if (imposter is ImposterDrawMesh)
            {
                CombinedImpostersMesh cbm;
                if (_meshForCamera.TryGetValue(imposter.myCamera, out cbm))
                {
                    int placeInMesh = !asOld ? (imposter as ImposterDrawMesh).placeInMesh : (imposter as ImposterDrawMesh).placeInPrevMesh;
                    cbm.RemoveImposter(placeInMesh);
                }
                else
                {
                    Debug.LogError("Cant find imposter!!!");
                }
            }
        }

        bool placeIsOutOfRange(int place, bool debugError = true)
        {
            if (place < 0 || place > _maxTexCount)
            {
                if (debugError)
                {
                    Debug.LogError("Place is out of range!!!   " + place + " maxPlace:" + _maxTexCount + " texRes:" + format);
                }
                return true;
            }
            return false;
        }

        public Vector4 GetUVsAtPlace(int place)
        {
            if (placeIsOutOfRange(place, false))
            {
                return Vector4.zero;
            }
            x = place / _size;
            y = place % _size;
            return new Vector4(x, y, (x + 1f), (y + 1f)) / _size;
        }

        public Rect GetRectAtPlace(int place)
        {
            if (placeIsOutOfRange(place))
            {
                return new Rect(0, 0, 0, 0);
            }
            x = place / _size;
            y = place % _size;
            return new Rect((float)x / _size, (float)y / _size, 1f / _size, 1f / _size);
        }

        Material _imposterMat;
        public Material imposterMat
        {
            get
            {
                if (_imposterMat == null)
                {
                    if (shader)
                    {
                        _imposterMat = new Material(shader);
                    }
                    _imposterMat.mainTexture = _atlas.rt;
                }
                return _imposterMat;
            }
            set
            {
                _imposterMat = value;
                _imposterMat.mainTexture = _atlas.rt;
                _imposterMats = new Material[] { _imposterMat };
            }
        }

        [SerializeField]
        Material[] _imposterMats;
        public Material[] imposterMats
        {
            get
            {
                if (_imposterMats == null || _imposterMats.Length == 0)
                    _imposterMats = new Material[] { imposterMat };
                return _imposterMats;
            }
        }
        bool _isAlreadyDestroyed = false;
        public void DiscardContent()
        {
            if (_isAlreadyDestroyed)
                return;
            ImpostersHandler.Instance.GetBackTexture(_atlas);
            Helper.Destroy(_imposterMat);
            Helper.Destroy(this.gameObject);
            foreach (var cm in _meshForCamera)
            {
                cm.Value.Destroy();
            }
            _meshForCamera.Clear();
            _isEmptyPlace = null;
            _isAlreadyDestroyed = true;
        }



        /// <summary>
        /// Draw all imposters that using this atlas
        /// </summary>
        public void DrawAll(CameraDetector camera, Camera specificCamera = null)
        {
            Profiler.BeginSample("DrawMesh");
            //Debug.Log("Draw all for camera "+camera.name+" atlas id " + GetInstanceID().ToString());
            if (_meshForCamera.ContainsKey(camera))
            {
                var cam = camera.thisCamera;
                if (specificCamera != null)
                    cam = specificCamera;
                Graphics.DrawMesh(_meshForCamera[camera].GetMesh(), Vector3.zero, Quaternion.Euler(0, 0, 0), imposterMat, 0, cam, 0, null, _shadowCastingMode); //, 0, cam
            }
            Profiler.EndSample();
        }
    }
}
