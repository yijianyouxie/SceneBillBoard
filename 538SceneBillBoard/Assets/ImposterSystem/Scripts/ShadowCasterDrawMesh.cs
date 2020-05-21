using UnityEngine;
using System.Collections;
using System;

namespace ImposterSystem
{
    internal class ShadowCasterDrawMesh : ImposterDrawMesh
    {
        internal override void ForcedAwake(ImposterController bc, CameraDetector camera)
        {
            base.ForcedAwake(bc, camera);
            gameObject.name = "shadow caster";
        }

        protected override void SetupImposterCamera()
        {
            // Setup atlas config
            if (needToChangeAtlas())
            {
                lastUpdateConfig.textureResolution = newTextureResolution;
                AtlasHandler newAtlas = _imposterHandler.FindShadowAtlas(this);
                if (newAtlas.isFull)
                    Debug.LogError("Error!!! Atlas is FULL!");
                PrepareForNewAtlas(newAtlas);
            }

            if (!atlas)
                Debug.Log("Error curAtlasHandler == null !!!");
            material = atlas.imposterMat;

            // locates imposterbard camera at right position and rotation
            Transform _imposterCameraTransform = _imposterCamera.transform;
            Vector3 locBillPos = this.position;

            _imposterCameraTransform.rotation = Quaternion.LookRotation(_imposterHandler.lightDirection);
            _imposterCameraTransform.position = locBillPos;
            float imposterQuadSize = this.maxSize / 2;
            this.quadSize = imposterQuadSize * 2;
            _imposterCamera.orthographic = true;
            _imposterCamera.orthographicSize = imposterQuadSize;
            _imposterCamera.farClipPlane = this.maxSize;
            _imposterCamera.nearClipPlane = -this.maxSize;
            _imposterCamera.ResetProjectionMatrix();
        }

        internal override void UpdateImposter()
        {
            isInQueue = false;
            _imposterCamera = _imposterHandler.GetImposterCamera();
            newTextureResolution = _imposterHandler.shadowTextureResolution.GetHashCode();
            imposterController.UpdatePosition();

            SetupImposterCamera();

            _imposterCamera.targetTexture = imposterTexture.rt;
            _imposterCamera.rect = GetRenderRect();

            // save settings
            float oldShadowsDistance = QualitySettings.shadowDistance;
            bool oldInversCulling = GL.invertCulling;
            //apply new settings
            QualitySettings.shadowDistance = 0;
            GL.invertCulling = false;

            imposterController.PrepareToRender(imposterLODIndex, ImpostersHandler.layerForRender);
            RenderImposterCamera();
            imposterController.AfterRender();

            // restore old settigns
            GL.invertCulling = oldInversCulling;
            QualitySettings.shadowDistance = oldShadowsDistance;


            lastUpdateConfig.cameraDirection = nowDirection;
            lastUpdateConfig.objectForwardDirection = imposterController._transform.forward;
            lastUpdateConfig.time = Time.time;
            lastUpdateConfig.screenSize = screenSize;
            lastUpdateConfig.distance = distance;
            lastUpdateConfig.lightDirection = ImpostersHandler.Instance.lightDirection;
            lastUpdateConfig.textureResolution = newTextureResolution;

            this.isActive = true;
            this.isGenerated = true;

            if (impostersMesh != null)
                impostersMesh.UpdateNormals(placeInMesh, -lastUpdateConfig.lightDirection);
        }

    }
}
