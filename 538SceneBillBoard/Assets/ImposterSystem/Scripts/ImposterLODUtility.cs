using UnityEngine;
using System.Collections;

namespace ImposterSystem{

	public class ImposterLODUtility {

		public static float RelativeScreenSize(Camera camera, ImposterController imposterController){
			float screenSize;
			if (camera.orthographic) {
				screenSize = imposterController.quadSize / camera.orthographicSize / 2;
			} else {
				Vector3 _viewportPoint = camera.WorldToViewportPoint (CalculateWorldReferencePoint(imposterController));
				float _multiplier = 2 * Mathf.Tan ( camera.fieldOfView/2 * Mathf.Deg2Rad);
				screenSize = imposterController.quadSize / (_viewportPoint.z * _multiplier);
			}

//			Debug.Log ("RelativeScreenSize: "+screenSize);
			return screenSize;
		}

		public static float CalculateDistance(Camera camera, float relativeScreenHeight, ImposterController imposterController){
			float distance;
			if (camera.orthographic) {
				distance = imposterController.quadSize / relativeScreenHeight / 4;
			} else {
				float _multiplier = 2 * Mathf.Tan ( camera.fieldOfView/2 * Mathf.Deg2Rad);
				relativeScreenHeight *= 2;
				distance = imposterController.quadSize / relativeScreenHeight / _multiplier;
			}
//			Debug.Log ("distance: "+distance+" screenSize: "+relativeScreenHeight);
			return distance;
		}

		public static Vector3 CalculateWorldReferencePoint(ImposterController imposterController){
			return imposterController._transform.position;
		}

        public static int GetImposterTextureResolution(float screenSize, int minRes, int maxRes){
			int resolution = (int)(Screen.height * screenSize)*2;
			if (resolution >= maxRes)
				return maxRes;
			if (resolution <= minRes)
				return minRes;
			resolution--;
			resolution |= (resolution >> 1);
			resolution |= (resolution >> 2);
			resolution |= (resolution >> 4);
			resolution |= (resolution >> 8);
			resolution |= (resolution >> 16);
			return resolution+1;
		}

		public static void PrepareCameraForRender(Camera renderCam, ImposterController imposterController, Camera sourceCam){
			Transform _locBillCamTrans = renderCam.transform;
			Vector3 locBillPos = imposterController.bounds.center;
			_locBillCamTrans.position = sourceCam.transform.position;
			Vector3 fromCamToCenter = _locBillCamTrans.position - locBillPos;
			_locBillCamTrans.rotation = Quaternion.LookRotation (-fromCamToCenter);
			float imposterQuadSize = imposterController.quadSize / 2;
			fromCamToCenter = _locBillCamTrans.position - imposterController.bounds.center - fromCamToCenter.normalized * imposterController.ZOffset * imposterController.quadSize;
			float angleForCamera = 2 * Mathf.Atan2 (imposterQuadSize, fromCamToCenter.magnitude) * Mathf.Rad2Deg; 
			renderCam.fieldOfView = angleForCamera;
			renderCam.farClipPlane = (_locBillCamTrans.position - locBillPos).magnitude + imposterController.quadSize;
			renderCam.nearClipPlane = Mathf.Max( (_locBillCamTrans.position - locBillPos).magnitude - imposterController.quadSize, 0.03f);
			renderCam.ResetProjectionMatrix ();
		}
	}
}
