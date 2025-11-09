using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.EventSystems;

[ExecuteAlways]
public class RotationShow : MonoBehaviour
{
    [Range(-1f, 1f)]
    public float rotationSpeed = 0f;

#if UNITY_EDITOR
    void OnEnable()
    {
        EditorApplication.update += EditorUpdate;
    }

    void OnDisable()
    {
        EditorApplication.update -= EditorUpdate;
    }

    void EditorUpdate()
    {
        if (Application.isPlaying) return;
        transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y + rotationSpeed, transform.rotation.eulerAngles.z);
    }
#endif

    void Update()
    {
        if (!Application.isPlaying) return;
        transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y + rotationSpeed, transform.rotation.eulerAngles.z);
    }
}
