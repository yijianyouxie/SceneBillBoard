using UnityEngine;
using System.Collections;

namespace ImposterSystem{

	public class FoVCameraControl : MonoBehaviour {

		Camera _camera;
		[SerializeField] 
		[Range(10, 90)]
		float speed = 60;
		void Start () {
			_camera = GetComponent<Camera>();
            speed = _camera.fieldOfView;
		}
		
		void Update () {
			speed -= Input.GetAxis("Mouse ScrollWheel") * 10;
			speed = Mathf.Clamp(speed, 45, 60);
            _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, speed, speed * Time.deltaTime);

		}
	}
}