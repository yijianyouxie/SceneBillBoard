using UnityEngine;
using System.Collections;

namespace ImposterSystem{
	
	public enum ImposterRenderType{
		meshRenderer,
		drawMesh
	}
		
	public enum QueueType{
		none,
		simple,
        sortedByScreenSize
	}

	public enum InvisibleImposterAction{
		none,
		releaseImposter
	}

	public enum AtlasResolution{
		_512x512 = 512,
		_1024x1024 = 1024,
		_2048x2048 = 2048,
        _4096x4096 = 4096
	}


    public enum TextureResolution
    {
	    _1024x_1024 = 1024,
        _512x512 = 512,
        _256x256 = 256,
        _128x128 = 128,
        _64x64 = 64,
        _32x32 = 32
    }

    public enum ImposterOrientation{
		Viewport,
		Direction
	}
}
