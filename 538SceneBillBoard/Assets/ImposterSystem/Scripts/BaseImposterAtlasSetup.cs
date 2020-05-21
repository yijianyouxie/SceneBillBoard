using UnityEngine;
using System.Collections;
namespace ImposterSystem
{
    internal abstract class BaseImposterAtlasSetup : ImposterBase
    {

        internal bool isChangingAtlas;

        internal AtlasHandler atlas;
        internal int placeInAtlas = -1;

        internal AtlasHandler prevAtlas;
        internal int placeInPrevAtlas = -1;

        protected override ImposterTexture imposterTexture
        {
            get
            {
                if (atlas == null)
                    Debug.LogError("Error");
                return atlas._atlas;
            }
        }
        
        protected virtual bool needToChangeAtlas()
        {
            if (lastUpdateConfig.textureResolution != newTextureResolution)
                return true;
            if (atlas == null)
                return true;
            return false;
        }
        
        protected override void SetupImposterCamera()
        {
            if (needToChangeAtlas())
            {
                lastUpdateConfig.textureResolution = newTextureResolution;
                AtlasHandler newAtlas = _imposterHandler.FindAtlas(this);
                if (newAtlas.isFull)
                    Debug.LogError("Error!!! Atlas is FULL!");
                PrepareForNewAtlas(newAtlas);
            }

            if (!atlas)
                Debug.Log("Error curAtlasHandler == null !!!");
            material = atlas.imposterMat;
            base.SetupImposterCamera();
        }

        protected override Rect GetRenderRect()
        {
            return atlas.GetRectAtPlace(placeInAtlas);
        }
                
        internal override void Release()
        {
            base.Release();
            if (atlas != null)
            {
                if (placeInAtlas != -1)
                    atlas.RemoveImposterFromAtlas(this);
                atlas = null;
            }
            if (prevAtlas != null)
            {
                if (placeInPrevAtlas != -1)
                    prevAtlas.RemoveImposterFromAtlas(this, true);
                prevAtlas = null;
            }
            isChangingAtlas = false;
        }

        protected virtual void PrepareForNewAtlas(AtlasHandler newAtlas)
        {
            if (newAtlas == null)
            {
                Debug.Log("Error new atlas == NULL!!!", this);
                return;
            }
            if (atlas != null)
            {
                atlas.RemoveImposterFromAtlas(this);
            }
            isGenerated = false;
            isChangingAtlas = true;
            newAtlas.AddImposterToAtlas(this);
        }

        protected virtual void ApplyNewAtlas()
        {
            isChangingAtlas = false;
        }
        
        const float mult = 100000f;
        internal virtual Color GetVertexColor
        {
            get
            {
                Vector4 res = new Vector4(
                    (_position.x + mult / 2f) / mult,
                    (_position.y + mult / 2f) / mult,
                    (_position.z + mult / 2f) / mult,
                    0
                );
                return res;
            }
        }        
    }
}
