using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace ImposterSystem
{
    internal static class LayerSetUp
    {

        public static int CreateLayer(string nameForNewLayer)
        {
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

            SerializedProperty layers = tagManager.FindProperty("layers");
            if (layers == null || !layers.isArray)
            {
                Debug.LogWarning("Can't set up the layers.  It's possible the format of the layers and tags data has changed in this version of Unity.");
                Debug.LogWarning("Layers is null: " + (layers == null));
                Debug.LogError("Cant set up layer. Please manually set up layer with name " + nameForNewLayer);
                return -1;
            }
            int i = 8;
            for (i = 8; i < layers.arraySize; i++)
            {
                //Debug.Log ("Layer "+i.ToString()+" : "+layers.GetArrayElementAtIndex(i).stringValue.ToString());
                if (layers.GetArrayElementAtIndex(i).stringValue.ToString() == "")
                {
                    break;
                }
                if (layers.GetArrayElementAtIndex(i).stringValue.ToString() == nameForNewLayer)
                {
                    Debug.Log("Already exist layer " + i.ToString() + " :" + nameForNewLayer.ToString());
                    return i;
                }
            }

            SerializedProperty layerSP = layers.GetArrayElementAtIndex(i);
            //Debug.Log("Setting up layers.  Layer " + [layer number] + " is now called " + [new layer name]);
            layerSP.stringValue = nameForNewLayer;

            tagManager.ApplyModifiedProperties();
            return i;
        }
    }
}
