using UnityEngine;

/// Keeps the complete board visible when the Game view changes shape.
[RequireComponent(typeof(Camera))]
public sealed class BoardCamera : MonoBehaviour
{
    void LateUpdate()
    {
        var cam = GetComponent<Camera>();
        cam.orthographicSize = Mathf.Max(5.3f, 8.5f / Mathf.Max(cam.aspect, 0.1f));
    }
}
