using UnityEngine;

/// <summary>Keeps a camera's background image filled without stretching it.</summary>
[ExecuteAlways]
public sealed class Level1Background : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private float imageAspect = 1.9f;

    public void Configure(Camera camera, float aspect)
    {
        targetCamera = camera;
        imageAspect = aspect;
        FitToCamera();
    }

    private void LateUpdate() => FitToCamera();

    private void FitToCamera()
    {
        if (targetCamera == null || imageAspect <= 0f) return;

        // Stay behind gameplay objects and follow the camera while climbing.
        float distance = targetCamera.farClipPlane * 0.9f;
        float height = targetCamera.orthographic
            ? targetCamera.orthographicSize * 2f
            : 2f * distance * Mathf.Tan(targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float width = height * targetCamera.aspect;

        // Cover the entire screen, cropping the edges instead of distorting the image.
        if (width / height > imageAspect) height = width / imageAspect;
        else width = height * imageAspect;

        transform.SetPositionAndRotation(
            targetCamera.transform.position + targetCamera.transform.forward * distance,
            targetCamera.transform.rotation);
        transform.localScale = new Vector3(width, height, 1f);
    }
}
