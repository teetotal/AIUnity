using UnityEngine;
using System;
using ENGINE;
public class Util {
    public static Vector3 StringToVector3(string target) {
        string[] arr = target.Split(',');
        if(arr.Length == 1) {
            return GameObject.Find(arr[0]).transform.position;
        } else if(arr.Length == 3) {
            return new Vector3(float.Parse(arr[0]), float.Parse(arr[1]), float.Parse(arr[2]));
        }
        throw new Exception("Invalid destination. " + target);
    }
    public static GameObject CreateObjectFromPrefab(string prefabPath, Vector3 position, Vector3 rotation) {
        GameObject prefab = Resources.Load<GameObject>(prefabPath);
        if(prefab == null) 
            throw new Exception("Invalid prefab." + prefabPath);
        Quaternion rot = Quaternion.Euler(rotation.x, rotation.y, rotation.z);
        return UnityEngine.Object.Instantiate(prefab, position, rot);
    }
    public static GameObject CreateChildObjectFromPrefab(string prefabPath, string parentId, bool isRandomRotation = true) {
        GameObject prefab = Resources.Load<GameObject>(prefabPath);
        if(prefab == null) 
            throw new Exception("Invalid prefab." + prefabPath);
        Quaternion rot = Quaternion.identity;
        GameObject obj = UnityEngine.Object.Instantiate(prefab, Vector3.zero, rot);
        obj.transform.SetParent(GameObject.Find(parentId).transform);
        obj.transform.localPosition = Vector3.zero;
        if(isRandomRotation) {
            obj.transform.rotation = Quaternion.Euler(0.0f, UnityEngine.Random.Range(0.0f, 360.0f), 0.0f);
        }
        return obj;
    }
    public static GameObject CreateChildObjectFromPrefabUI(string prefabPath, GameObject parent) {
        GameObject prefab = Resources.Load<GameObject>(prefabPath);
        if(prefab == null) 
            throw new Exception("Invalid prefab." + prefabPath);
        GameObject obj = UnityEngine.Object.Instantiate(prefab);
        obj.transform.SetParent(parent.transform);
        return obj;
    }
}