

namespace ImposterSystem
{
	/// <summary>
	/// Imposter LOD
	/// </summary>
	[System.Serializable]
	public struct ImposterLOD
	{
		public float screenRelativeTransitionHeight;

		public float fadeTransitionWidth;

		public bool isImposter;
        public bool renderShadows;

        public TextureResolution minImposterResolution;
        public TextureResolution maxImposterResolution;

        public OriginalGOController[] renderers;
        
		public ImposterLOD(float screenRelativeTransitionHeight, 
            OriginalGOController[] renderers, 
            bool isImposter, bool renderShadows = false, 
            TextureResolution minImposterResolution = TextureResolution._32x32, 
            TextureResolution maxImposterResolution = TextureResolution._512x512)
		{
			this.screenRelativeTransitionHeight = screenRelativeTransitionHeight;
			this.fadeTransitionWidth = 0f;
			this.renderers = renderers;
			this.isImposter = isImposter;
            this.renderShadows = renderShadows;
            this.minImposterResolution = minImposterResolution;
            this.maxImposterResolution = maxImposterResolution;
        }
	}

}