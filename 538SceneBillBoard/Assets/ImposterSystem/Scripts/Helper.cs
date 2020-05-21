using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace ImposterSystem{

    public class Helper {

        private static Helper _instance;
        public static Helper Instance {
            get {
                if (_instance != null)
                    return _instance;
                _instance = new Helper();
                return _instance;
            }
        }

        Helper() {
            _instance = this;
            init();
            //			Debug.Log ("Create BillSys_Helper exemplar.");
        }

        private void init() {
            uvs = new List<Vector2>();
            uvs.AddRange(new Vector2[4]);
        }

        //public static Vector3 InverseTransformPoint(Vector3 pos, Quaternion toRot, Vector3 toPos){
        //	return Quaternion.Inverse(toRot)*(toPos - pos);
        //}

        //public static Vector3 TransformPoint(Vector3 localPos, Vector3 pos, Quaternion rot){
        //	return pos + rot * localPos;
        //}

        //public static float Max3(float a, float b, float c){
        //	return Mathf.Max( Mathf.Max(a, b), c);
        //}

        //public static float MaxMagnitude3(Vector3 a){
        //	return Max3(new Vector2(a.x, a.y).magnitude, new Vector2(a.x, a.z).magnitude, new Vector2(a.y, a.z).magnitude);
        //}

        //public static Vector3 Vec3Scaled(Vector3 a, Vector3 b){
        //	return new Vector3 (a.x * b.x, a.y * b.y, a.x * b.z);
        //}

        //public static float Vec3Mult(Vector3 a, Vector3 b){
        //	return a.x * b.x + a.y * b.y + a.z * b.z;
        //}

        public static float MaxV3(Vector3 v) {
            return Mathf.Max(new Vector2(v.x, v.y).magnitude, Mathf.Max(new Vector2(v.x, v.z).magnitude, new Vector2(v.y, v.z).magnitude));
        }

        //This returns the angle in radians
        public static float AngleInRad(Vector3 vec1, Vector3 vec2) {
            return Mathf.Atan2(vec2.y - vec1.y, vec2.x - vec1.x);
        }

        //This returns the angle in degrees
        static float radToDeg = Mathf.Rad2Deg;
        static float sqrtMagnitude;
        static float some;
        public static float AngleInDeg(Vector3 vec1, Vector3 vec2) {

            some = (vec1.x * vec2.x + vec1.y * vec2.y + vec1.z * vec2.z) / Mathf.Sqrt(vec1.sqrMagnitude * vec2.sqrMagnitude);
            if (1 - some < 0.0001f)
                return 0.0f;
            some = Mathf.Acos(some) * radToDeg;
            if (some < 0)
                some *= -1;
            return some;
        }

        public static void SimplifyMeshRenderer(MeshRenderer t) {
            t.receiveShadows = false;
#if UNITY_5_4_OR_NEWER
            t.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
#else
            t.useLightProbes = false;
#endif
            t.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            t.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        }

        public static void Destroy(Object obj) {
            if (Application.isPlaying)
                Object.Destroy(obj);
            else
                Object.DestroyImmediate(obj);
        }

        static Vector3[] planeVerts = new Vector3[4];
        static int[] planeTriangle = new int[6] { 0, 1, 2, 2, 3, 0 };
        static Vector2[] planeUVs = new Vector2[4];
        static string imposterMeshName = "ImposterMesh";

        public static Mesh NewPlane(Vector3 pos, Vector3 scale, Vector4 uv_s) {
            //		Debug.Log ("Create new plane : "+scale.ToString());
            Mesh newMesh = new Mesh();
            planeVerts[0] = new Vector3(-scale.x, -scale.y, 0) + pos;
            planeVerts[1] = new Vector3(-scale.x, scale.y, 0) + pos;
            planeVerts[2] = new Vector3(scale.x, scale.y, 0) + pos;
            planeVerts[3] = new Vector3(scale.x, -scale.y, 0) + pos;
            planeUVs[0] = new Vector2(uv_s.x, uv_s.y);
            planeUVs[1] = new Vector2(uv_s.x, uv_s.w);
            planeUVs[2] = new Vector2(uv_s.z, uv_s.w);
            planeUVs[3] = new Vector2(uv_s.z, uv_s.y);

            newMesh.vertices = planeVerts;
            newMesh.triangles = planeTriangle;
            newMesh.uv = planeUVs;
            newMesh.RecalculateNormals();
            newMesh.name = imposterMeshName;
            newMesh.bounds = new Bounds(Vector3.zero, scale);
            return newMesh;
        }

        List<Vector2> uvs;
        public void UpdatePlane_UV(Mesh mesh, Vector4 uv_s) {
            if (uvs.Count != 4) {
                Debug.LogError("Error?!?   UpdatePlane_UV: uvs count = " + uvs.Count);
            }
            uvs[0] = new Vector2(uv_s.x, uv_s.y);
            uvs[1] = new Vector2(uv_s.x, uv_s.w);
            uvs[2] = new Vector2(uv_s.z, uv_s.w);
            uvs[3] = new Vector2(uv_s.z, uv_s.y);
            mesh.SetUVs(0, uvs);
        }

        public static void UpdatePlane_Verts(Mesh mesh, Vector3 scale)
        {
            planeVerts[0] = new Vector3(-scale.x, -scale.y, 0);
            planeVerts[1] = new Vector3(-scale.x, scale.y, 0);
            planeVerts[2] = new Vector3(scale.x, scale.y, 0);
            planeVerts[3] = new Vector3(scale.x, -scale.y, 0);
            mesh.vertices = planeVerts;
            mesh.RecalculateBounds();
        }

        public static Vector4 CameraSpacePlane(Camera cam, Vector3 pos, Vector3 normal, float sideSign)
        {
            Vector3 offsetPos = pos + normal * 0.07f;
            Matrix4x4 m = cam.worldToCameraMatrix;
            Vector3 cpos = m.MultiplyPoint(offsetPos);
            Vector3 cnormal = m.MultiplyVector(normal).normalized * sideSign;
            return new Vector4(cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos, cnormal));
        }

    }
}
