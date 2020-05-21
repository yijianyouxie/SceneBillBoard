using UnityEngine;
using System.Collections;
using System;

namespace ImposterSystem
{
    internal class ImposterDrawMesh : BaseImposterAtlasSetup
    {
        internal Vector3[] locVertsPos;
        internal Vector4[] uv;

        internal CombinedImpostersMesh impostersMesh;
        internal int placeInMesh;
        internal CombinedImpostersMesh prevImposterMesh;
        internal int placeInPrevMesh;

        internal float changingAtlasProgress { get; private set; }
        internal float changingAtlasEndTime { get; private set; }

        internal override Vector3 position
        {
            set
            {
                if (value == _position)
                    return;
                _position = value;
                if (impostersMesh != null)
                    impostersMesh.UpdatePosition(placeInMesh, GetVertexColor);
            }
        }

        internal override Vector4 UVs
        {
            set
            {
                _uvs = value;
                int valueZ = imposterController.alwaysLookAtCamera ? 1 : 0;
                uv[0] = new Vector4(value.x, value.y, valueZ, 0);
                uv[1] = new Vector4(value.x, value.w, valueZ, 0);
                uv[2] = new Vector4(value.z, value.w, valueZ, 0);
                uv[3] = new Vector4(value.z, value.y, valueZ, 0);
                if (impostersMesh != null)
                    impostersMesh.UpdateUVs(this);
            }
        }


        protected override void UpdateVertices()
        {
            locVertsPos[0] = new Vector3(-_quadSizeHalf, -_quadSizeHalf, _zOffset);
            locVertsPos[1] = new Vector3(-_quadSizeHalf, _quadSizeHalf, _zOffset);
            locVertsPos[2] = new Vector3(_quadSizeHalf, _quadSizeHalf, _zOffset);
            locVertsPos[3] = new Vector3(_quadSizeHalf, -_quadSizeHalf, _zOffset);
            if (impostersMesh != null)
                impostersMesh.UpdateVertices(this);
        }

        internal override void ForcedAwake(ImposterController bc, CameraDetector camera)
        {
            base.ForcedAwake(bc, camera);
            locVertsPos = new Vector3[4];
            uv = new Vector4[4];
            changingAtlasProgress = 1;
        }

        internal override void UpdateImposter()
        {
            base.UpdateImposter();
            impostersMesh.UpdateNormals(placeInMesh, lastUpdateConfig.cameraDirection);
        }

        protected override void SetupImposterCamera()
        {
            base.SetupImposterCamera();
            UVs = _uvs;
        }

        protected override void PrepareForNewAtlas(AtlasHandler newAtlas)
        {
            if (atlas == null || !_imposterHandler.useFading)
            {
                if (atlas != null && placeInAtlas != -1)
                    atlas.RemoveImposterFromAtlas(this);
                atlas = newAtlas;
                newAtlas.AddImposterToAtlas(this);
                return;
            }
            if (newAtlas == null) {
                Debug.LogError("Error");
                return;
            }
            if (prevAtlas != null)
            {
                ApplyNewAtlas();
            }
            isChangingAtlas = true;
            prevAtlas = atlas;
            prevImposterMesh = impostersMesh;
            placeInPrevAtlas = placeInAtlas;
            placeInPrevMesh = placeInMesh;
            atlas = newAtlas;
            newAtlas.AddImposterToAtlas(this);
            Color color = GetVertexColor;
            changingAtlasEndTime = Time.timeSinceLevelLoad + _imposterHandler.fadeTime;
            float time = changingAtlasEndTime / 100000f;
            color.a = time;
            impostersMesh.UpdatePosition(placeInMesh, color);
            color.a = -time;
            try
            {
                prevImposterMesh.UpdatePosition(placeInPrevMesh, color);
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
                Debug.Log(placeInPrevMesh + " " + prevImposterMesh.colors.Count);
            }
            changingAtlasProgress = 0;
            if (atlas == null)
                Debug.LogError("Error");
        }

        protected override void ApplyNewAtlas()
        {
            isChangingAtlas = false;
            if (prevAtlas != null && placeInPrevAtlas != -1)
                prevAtlas.RemoveImposterFromAtlas(this, true);
            prevAtlas = null;
            prevImposterMesh = null;
            placeInPrevAtlas = -1;
            placeInPrevMesh = -1;
        }

        protected override bool needToChangeAtlas()
        {
            return true;
        }

        internal override bool needUpdate
        {
            get
            {
                if (_imposterHandler.dontUpdateWhenFading && isChangingAtlas)
                {
                    return false;
                }
                return true;
            }
        }

        internal override void Hide()
        {
        }

        internal override void Show(bool ignoreNextHide = false)
        {
            if (isChangingAtlas)
            {
                if (changingAtlasEndTime < Time.timeSinceLevelLoad)
                    ApplyNewAtlas();
                else
                {
                    changingAtlasProgress += Time.deltaTime / _imposterHandler.fadeTime;
                }
            }
        }
    }
}
