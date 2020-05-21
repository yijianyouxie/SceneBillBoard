using UnityEngine;
using UnityEngine.UI;
using System.Collections;
namespace ImposterSystem
{
    /// <summary>
    /// Just wrapper to share UI prefab between scenes
    /// </summary>
    public class ExampleUIController : MonoBehaviour
    {
        [SerializeField] Toggle _disableImpostersUpdate;
        [SerializeField] Slider _maxUpdatesPerFrame;

        [SerializeField] Toggle _useFading;
        [SerializeField] Slider _fadeTime;
        [SerializeField] Toggle _dontUpdateWhenFading;

        [SerializeField] Toggle _imposterShadowCasting;
        [SerializeField] Slider _lightDirectionDelta;

        [SerializeField] Slider _preloadFactor;
        [SerializeField] Slider _minAngleToStopLookAt;

        private void Awake()
        {
        }

        private void OnEnable()
        {
            UpdateSettings();
            SetupListeners();
        }

        public void UpdateSettings()
        {
            if (null == _disableImpostersUpdate)
            {
                return;
            }
            ImpostersHandler handler = ImpostersHandler.Instance;
            _disableImpostersUpdate.isOn = handler.disableImpostersUpdating;
            _maxUpdatesPerFrame.value = handler.maxUpdatesPerFrame;

            _useFading.isOn = handler.useFading;
            _fadeTime.value = handler.fadeTime;
            _dontUpdateWhenFading.isOn = handler.dontUpdateWhenFading;

            _imposterShadowCasting.isOn = handler.shadowCastingEnabled;
            _lightDirectionDelta.value = handler.lightDirectionDelta;

            _preloadFactor.value = handler.preloadFactor;
            _minAngleToStopLookAt.value = handler.minAngleToStopLookAtCamera;
        }

        public void SetupListeners()
        {
            if (null == _disableImpostersUpdate)
            {
                return;
            }
            _disableImpostersUpdate.onValueChanged.AddListener(value => ImpostersHandler.Instance.disableImpostersUpdating = value);
            _maxUpdatesPerFrame.onValueChanged.AddListener(value => ImpostersHandler.Instance.maxUpdatesPerFrame = (int)_maxUpdatesPerFrame.value);

            _useFading.onValueChanged.AddListener(value => ImpostersHandler.Instance.useFading = value);
            _fadeTime.onValueChanged.AddListener(value => ImpostersHandler.Instance.fadeTime = _fadeTime.value);
            _dontUpdateWhenFading.onValueChanged.AddListener(value => ImpostersHandler.Instance.dontUpdateWhenFading = value);

            _imposterShadowCasting.onValueChanged.AddListener(value => ImpostersHandler.Instance.shadowCastingEnabled = value);
            _lightDirectionDelta.onValueChanged.AddListener(value => ImpostersHandler.Instance.lightDirectionDelta = _lightDirectionDelta.value);

            _preloadFactor.onValueChanged.AddListener(value => ImpostersHandler.Instance.preloadFactor = _preloadFactor.value);
            _minAngleToStopLookAt.onValueChanged.AddListener(value => ImpostersHandler.Instance.minAngleToStopLookAtCamera = _minAngleToStopLookAt.value);
        }

        public bool ImposterSystemEnabled
        {
            get { return ImpostersHandler.Instance.enabled; }
            set { ImpostersHandler.Instance.enabled = value; }
        }

        public void Spawn(int value)
        {
            FindObjectOfType<ExampleSpawner>().OnSpawn(value);
        }

        public void SetPlayerMovementEnabled(bool value)
        {
            FindObjectOfType<MouseLook>().enabled = value;
            FindObjectOfType<PlayerController>().enabled = value;
        }
    }
}