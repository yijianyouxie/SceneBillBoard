using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ImposterSystem
{

    internal static class EditorTools
    {

        [MenuItem("Tools/ImposterSystem/Create ImpostersHandler")]
        public static void CreateImposterHeandler()
        {


            if (LayerMask.NameToLayer("imposterRender") == -1)
            {
                if (EditorUtility.DisplayDialog("Imposter System layer setup",
                    "Imposter System requires special layer to work with. It's called 'imposterRender'.\nWould you like to automatically setup new layer?",
                    "Yes",
                    "No, manually"
                    ))
                {
                    LayerSetUp.CreateLayer("imposterRender");
                }
            }

            if (LayerMask.NameToLayer("imposterRender") == -1)
            {
                Debug.LogError("IMPORTANT!!! READ THIS!!! " +
                        "Imposter System requires special layer to work with. " +
                        "It's called <b>'imposterRender'</b>. " +
                        "Specify it in Edit->Project Settings->Tags and Layers.");
                return;
            }

            if (GameObject.FindObjectOfType<ImpostersHandler>())
            {
                if (EditorUtility.DisplayDialog(
                    "Create ImpostersHandler",
                    "Instance of ImpostersHandler alredy exist in scene.\nWould you like to replace it? All settings will be lost!",
                    "Yes",
                    "No"
                    ))
                {
                    // replace old instance
                    GameObject.DestroyImmediate(GameObject.FindObjectOfType<ImpostersHandler>().gameObject);
                }
                else
                {
                    Debug.Log("HERE IS instance of 'ImpostersHandler'. Click on this message to focus on it.", GameObject.FindObjectOfType<ImpostersHandler>());
                    return;
                }
            }
            GameObject go = new GameObject();
            go.name = "ImpostersHandler";
            go.transform.position = Vector3.zero;
            go.transform.SetAsFirstSibling();
            go.AddComponent<ImpostersHandler>();

#if UNITY_5_6_OR_NEWER
            ImpostersHandler.Instance.GetImposterCamera().allowHDR = false;
#endif
        }

        [MenuItem("Tools/ImposterSystem/Setup ImposterController(s)")]
        public static void SetUpImposterController()
        {
            RemoveImposterController();
            Transform[] _selected = Selection.transforms;
            if (_selected.Length == 0)
            {
                Debug.LogError("No gameObject selected! Please, select gameObject to setup imposter");
                return;
            }
            bool hasLodGroup = false;
            foreach (Transform item in _selected)
            {
                if (item.GetComponent<LODGroup>())
                {
                    hasLodGroup = true;
                    break;
                }
            }
            bool importFromLODGroup = hasLodGroup ? EditorUtility.DisplayDialog("Setup ImposterController", "Are you wanna replace LODGroup with ImposterController?", "Yes", "No") : false;
            bool setAllRenderersToLODs = importFromLODGroup ? false : EditorUtility.DisplayDialog("Setup ImposterController", "Are you wanna set all child renderers to Imposter LODs?", "Yes", "No");
            Renderer[] _renders;
            foreach (Transform trans in _selected)
            {
                if (importFromLODGroup && trans.gameObject.GetComponent<LODGroup>())
                {
                    ImposterController curBillControl = trans.gameObject.AddComponent<ImposterController>();
                    LODGroup lodGroup = trans.gameObject.GetComponent<LODGroup>();
                    LOD[] lods = lodGroup.GetLODs();
                    ImposterLOD[] imposterLods = new ImposterLOD[lods.Length];
                    for (int i = 0; i < imposterLods.Length; i++)
                    {
                        LOD curLod = lods[i];
                        ImposterLOD curBillLod = imposterLods[i];
                        curBillLod.screenRelativeTransitionHeight = curLod.screenRelativeTransitionHeight;
                        curBillLod.renderers = new OriginalGOController[curLod.renderers.Length];
                        for (int j = 0; j < curBillLod.renderers.Length; j++)
                        {
                            if (!curLod.renderers[j].GetComponent<OriginalGOController>())
                            {
                                curLod.renderers[j].gameObject.AddComponent<OriginalGOController>();
                            }
                            curBillLod.renderers[j] = curLod.renderers[j].GetComponent<OriginalGOController>();
                        }
                        imposterLods[i] = new ImposterLOD(curBillLod.screenRelativeTransitionHeight, curBillLod.renderers, false, false);
                    }
                    imposterLods[imposterLods.Length - 1].isImposter = true;
                    curBillControl.m_LODs = imposterLods;
                    GameObject.DestroyImmediate(lodGroup);
                    curBillControl.RecalculateBounds();
                }
                else
                {
                    Transform root = trans;
                    if (trans.GetComponent<Renderer>())
                    {
                        GameObject pustishka = new GameObject();
                        pustishka.name = trans.name;
                        pustishka.transform.position = trans.position;
                        pustishka.transform.parent = trans.parent;
                        trans.parent = pustishka.transform;
                        trans.localPosition = Vector3.zero;
                        root = pustishka.transform;
                    }

                    if ((_renders = trans.GetComponentsInChildren<Renderer>()).Length > 0)
                    {
                        ImposterController bc = root.gameObject.AddComponent<ImposterController>();
                        if (setAllRenderersToLODs)
                        {
                            foreach (var r in _renders)
                            {
                                r.gameObject.AddComponent<OriginalGOController>();
                            }
                            OriginalGOController[] ogos = trans.GetComponentsInChildren<OriginalGOController>();
                            bc.m_LODs = new ImposterLOD[2];
                            bc.m_LODs[0] = new ImposterLOD(0.2f, ogos, false, false);
                            bc.m_LODs[1] = new ImposterLOD(0.01f, ogos, true, false);
                        }
                    }
                }
            }
        }

        [MenuItem("Tools/ImposterSystem/Remove ImposterController(s)")]
        internal static void RemoveImposterController()
        {
            Transform[] _selected = Selection.transforms;
            if (_selected.Length == 0)
            {
                Debug.LogError("No gameObject selected! Please, select gameObject to remove imposter");
                return;
            }
            ImposterController[] _bcs;
            ImposterBase[] _bs;
            OriginalGOController[] _ogos;
            foreach (Transform trans in _selected)
            {
                _bcs = trans.GetComponentsInChildren<ImposterController>(true);
                _bs = trans.GetComponentsInChildren<ImposterBase>(true);
                _ogos = trans.GetComponentsInChildren<OriginalGOController>(true);
                int i = 0;
                for (i = 0; i < _bs.Length; i++)
                {
                    GameObject.DestroyImmediate(_bs[i].gameObject);
                }
                for (i = 0; i < _ogos.Length; i++)
                {
                    if (_ogos[i].GetComponent<Renderer>())
                        _ogos[i].GetComponent<Renderer>().enabled = true;
                    GameObject.DestroyImmediate(_ogos[i]);
                }
                for (i = 0; i < _bcs.Length; i++)
                {
                    GameObject.DestroyImmediate(_bcs[i]);
                }
            }
        }

        [MenuItem("Tools/ImposterSystem/Clear scene")]
        public static void DestroyAllBillSysComponentsInTheCurrentScene()
        {
            if (EditorUtility.DisplayDialog("Destroy all ImposterSystem Components", "Are you sure you want to destroy all ImposterSystem Components in the current scene?", "Yes", "No"))
            {
                GameObject[] trans = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
                List<MonoBehaviour> todelComponents = new List<MonoBehaviour>();
                List<MonoBehaviour> todelGameObjects = new List<MonoBehaviour>();
                foreach (var t in trans)
                {
                    if (t == null)
                        continue;
                    todelComponents.AddRange(t.GetComponentsInChildren<ImposterController>(true));
                    todelComponents.AddRange(t.GetComponentsInChildren<OriginalGOController>(true));
                    todelComponents.AddRange(t.GetComponentsInChildren<CameraDetector>(true));
                    todelGameObjects.AddRange(t.GetComponentsInChildren<ImpostersHandler>(true));
                }
                foreach (var t in todelComponents)
                {
                    if (t != null)
                    {
                        GameObject.DestroyImmediate(t);
                    }
                }
                foreach (var t in todelGameObjects)
                {
                    if (t != null)
                    {
                        GameObject.DestroyImmediate(t.gameObject);
                    }
                }
            }
        }

    }
}
