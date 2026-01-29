using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraDisplay : MonoBehaviour
{
    [SerializeField] bool _showFrustum = false;
    [SerializeField] bool _showPosition = false;

    Camera _camera;

    void Start()
    {
        _camera = GetComponent<Camera>();
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        if (_showPosition) DrawPosition();
        if (_showFrustum) DrawFrustum();
    }

    void DrawPosition()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 5f);
        Gizmos.DrawWireSphere(transform.position, 50f);
    }

    void DrawFrustum()
    {
        Gizmos.color = Color.yellow;
        Gizmos.matrix = _camera.transform.localToWorldMatrix;
        Gizmos.DrawFrustum(Vector3.zero, _camera.fieldOfView, _camera.farClipPlane, _camera.nearClipPlane, _camera.aspect);
    }
}
