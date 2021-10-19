using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HierarchyPin : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
#if UNITY_EDITOR
        UnityEditor.EditorGUIUtility.PingObject(gameObject);
#endif
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
