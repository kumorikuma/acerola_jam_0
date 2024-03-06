using System;
using System.Collections.Generic;
using UnityEngine;

public static class GameObjectExtensions {
    public static void DestroyChildren(this GameObject obj) {
        foreach (Transform child in obj.transform) {
            UnityEngine.Object.Destroy(child.gameObject);
        }
    }

    public static void DestroyChildrenImmediate(this GameObject obj) {
        List<GameObject> destroyList = new();

        foreach (Transform child in obj.transform) {
            destroyList.Add(child.gameObject);
        }

        foreach (GameObject objPendingDestroy in destroyList) {
            UnityEngine.Object.DestroyImmediate(objPendingDestroy);
        }
    }

    public static GameObject FindChildByName(this GameObject parent, string name) {
        // Check if the current GameObject has the target name
        if (parent.name == name) {
            return parent;
        }

        // Iterate through each child and perform a recursive search
        foreach (Transform child in parent.transform) {
            GameObject result = child.gameObject.FindChildByName(name);
            if (result != null) {
                return result;
            }
        }

        // If not found in this branch, return null
        return null;
    }
}
