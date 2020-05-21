using UnityEngine;
using System.Collections;

namespace ImposterSystem{


	[ExecuteInEditMode]
	public class OriginalGOController : MonoBehaviour {

		[SerializeField] bool simplifyRendererOnRenderToImposter = false;
		bool _rendererEnabled;
		bool _ignoreNextHide = false;
		bool _simplifiedRenderer = false;
		bool receiveShadows;
		UnityEngine.Rendering.ShadowCastingMode shadowCastingMode;
		UnityEngine.Rendering.ReflectionProbeUsage reflectionProbeUsage;
#if UNITY_5_4_OR_NEWER
        UnityEngine.Rendering.LightProbeUsage lightProbeUsage;
#else
        bool useLightProbes;
#endif
        GameObject _go;
        [HideInInspector]
        public Renderer Renderer;
		ImposterController _imposterContrl;
        //[HideInInspector]
        [SerializeField] int _defLayer;

		////////////////////           UNITY EVENTS
		/// 
		void Reset(){
			_defLayer = _go.layer;
			SetUp ();
		}

		void OnEnable(){
			SetUp ();
			if (_imposterContrl == null) {
				Debug.Log ("Error!??!?!?!?!!??!!?!?!?!?");
				return;
			}
		}

		void OnDisable(){
			_imposterContrl._originObjects.Remove (this);
			SetDefaultLayer ();
			if (_imposterContrl == null) {
				return;
			}
		}

		////////////////////////////////////////

		public void SetUp(){
			if ( !(Renderer = GetComponent<Renderer> ())) {
				Debug.LogError ("There are no Renderer attached to this gameObject!!!");
				Helper.Destroy (this);
                return;
			}
			if (!(_imposterContrl = GetComponentInParent<ImposterController> ())) {
				Debug.LogError ("No ImposterController attached to parents of this GO! Dstr this");
				Helper.Destroy (this);
                return;
			}
			_go = gameObject;
			_defLayer = _go.layer;
			_rendererEnabled = Renderer.enabled;
			receiveShadows = Renderer.receiveShadows;
			shadowCastingMode = Renderer.shadowCastingMode;
			reflectionProbeUsage = Renderer.reflectionProbeUsage;
#if UNITY_5_4_OR_NEWER
            lightProbeUsage = Renderer.lightProbeUsage;
#else
            useLightProbes = Renderer.useLightProbes;
#endif
            _simplifiedRenderer = false;
            SetDefaultLayer();
//			if (!_imposterContrl._originObjects.Contains (this))
//				_imposterContrl._originObjects.Add (this);
		}

		public void Show(bool ignoreNextHide = false){
			this._ignoreNextHide = ignoreNextHide;
			if (_rendererEnabled)
				return;
			_rendererEnabled = true;
			Renderer.enabled = true;
			if (_simplifiedRenderer && simplifyRendererOnRenderToImposter) {
				Renderer.receiveShadows = receiveShadows;
				Renderer.shadowCastingMode = shadowCastingMode;
				Renderer.reflectionProbeUsage = reflectionProbeUsage;
#if UNITY_5_4_OR_NEWER
                Renderer.lightProbeUsage = lightProbeUsage;
#else
                Renderer.useLightProbes = useLightProbes;
#endif
                _simplifiedRenderer = false;
			}
		}

		public void Hide(){
            if (!_rendererEnabled || _ignoreNextHide)
            {
                _ignoreNextHide = false;
                return;
			}
			_rendererEnabled = false;
			Renderer.enabled = false;
            SetDefaultLayer ();
		}

		public void PrepareToRender(int layer, bool ignoreNextHide = true){
			_rendererEnabled = true;
			Renderer.enabled = true;
			_ignoreNextHide = ignoreNextHide;
			if (!_simplifiedRenderer && simplifyRendererOnRenderToImposter) {
				Renderer.receiveShadows = false;
				Renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
				Renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
#if UNITY_5_4_OR_NEWER
                Renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
#else
                Renderer.useLightProbes = false;
#endif
                _simplifiedRenderer = true;
			}
			_go.layer = layer;

        }

		public void SetDefaultLayer(){
		    gameObject.layer = _defLayer;
		}

	}

}