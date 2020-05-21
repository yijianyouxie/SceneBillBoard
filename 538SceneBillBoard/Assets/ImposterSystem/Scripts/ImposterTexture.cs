using UnityEngine;
using System.Collections;

namespace ImposterSystem
{
	[System.Serializable]
	public class ImposterTexture
	{

		public int size{ get; private set;}
		public RenderTexture rt{ get; private set;}
		public float memory{ get; private set; }

		public float lastUsedTime { get; private set; }
		internal ImposterBase subscriber;

		public ImposterTexture(int size, int depth = 16){
			this.size = size;
			rt = new RenderTexture (size, size, depth);
			memory = size * size * 6f / (32f * 32f);
			isUsed = false;
		}

		bool _isUsed;
		public bool isUsed{
			get{ 
				return _isUsed;
			}
			set{
				_isUsed = value;
				if (!_isUsed)
					subscriber = null;
				lastUsedTime = Time.time;
			}
		}

		public void DiscardContent () {
            rt.Release();
			Helper.Destroy (rt);
		}

	}
}
