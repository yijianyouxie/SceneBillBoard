using UnityEngine;
using System.Collections;

namespace ImposterSystem{

	public class PlayerController : MonoBehaviour {

		[SerializeField] float speed;
		[SerializeField] float runSpeed;
        [SerializeField] bool allowUpDown = false;
		void Start () {
		
		}

		void Update () {
			float h = Input.GetAxis("Horizontal");
			float v = Input.GetAxis("Vertical");
			Vector3 plusPos = transform.forward * v + transform.right * h;
			if (allowUpDown && Input.GetKey(KeyCode.Space))
				plusPos += transform.up;
			if (allowUpDown && Input.GetKey(KeyCode.LeftControl))
				plusPos -= transform.up;
			if (Input.GetKey(KeyCode.LeftShift))
				plusPos *= runSpeed;
			else
				plusPos *= speed;
			transform.position += plusPos * Time.deltaTime;
		}
	}
}
