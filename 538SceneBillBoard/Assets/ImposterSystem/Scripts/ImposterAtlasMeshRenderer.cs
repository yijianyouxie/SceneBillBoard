using UnityEngine;
using System.Collections;
using System;

namespace ImposterSystem
{
    internal class ImposterAtlasMeshRenderer : BaseImposterAtlasSetup
    {
        MeshFilter _meshFilter;
        bool _meshRendererEnabled;
        MeshRenderer _meshRenderer;
        Transform _meshRendererTransform;

        internal override void ForcedAwake(ImposterController bc, CameraDetector camera)
        {
            base.ForcedAwake(bc, camera);
            GameObject meshRendererGO = new GameObject();
            meshRendererGO.name = "Renderer";
            _meshRendererTransform = meshRendererGO.transform;
            _meshRendererTransform.parent = _transform;
            _meshRendererTransform.localPosition = Vector3.zero;
            _meshFilter = meshRendererGO.AddComponent<MeshFilter>();
            _meshRenderer = meshRendererGO.AddComponent<MeshRenderer>();
            Helper.SimplifyMeshRenderer(_meshRenderer);
            _meshRenderer.enabled = false;
            _meshFilter.sharedMesh = Helper.NewPlane(Vector3.zero, new Vector3(_quadSizeHalf, _quadSizeHalf, _quadSizeHalf), new Vector4(0, 0, 1, 1));
            _meshFilter.sharedMesh.RecalculateBounds();
        }

        internal override float zOffset
        {
            set
            {
                if (_zOffset == value)
                    return;
                _zOffset = value;
                _meshRendererTransform.localPosition = new Vector3(0, 0, -_zOffset);
            }
        }

        internal override Vector3 position
        {
            set
            {
                if (value == _position)
                    return;
                _transform.position = _position = value;
            }
        }
        internal override Quaternion rotation
        {
            set { _transform.rotation = _rotation = value; }
        }

        internal override Vector4 UVs
        {
            get { return _uvs; }
            set
            {
                _uvs = value;
                Helper.Instance.UpdatePlane_UV(_meshFilter.sharedMesh, _uvs);
            }
        }

        internal override Material material
        {
            set
            {
                _material = value;
                _meshRenderer.sharedMaterial = _material;
            }
        }
        internal override Vector3 nowDirection
        {
            set
            {
                _nowDirection = value;
                rotation = Quaternion.LookRotation(-_nowDirection);
            }
        }

        protected override void UpdateVertices()
        {
            Helper.UpdatePlane_Verts(_meshFilter.sharedMesh, new Vector3(_quadSizeHalf, _quadSizeHalf, _quadSizeHalf));
        }

        internal override void Hide()
        {
            if (!_meshRendererEnabled || _ignoreNextHide)
            {
                _ignoreNextHide = false;
                return;
            }
            _meshRenderer.enabled = false;
            _meshRendererEnabled = false;
        }

        internal override void Show(bool ignoreNextHide = false)
        {
            _ignoreNextHide = ignoreNextHide;
            if (_meshRendererEnabled)
                return;
            _meshRenderer.enabled = true;
            _meshRendererEnabled = true;
        }
    }
}
