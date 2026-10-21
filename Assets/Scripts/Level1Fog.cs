using UnityEngine;

/// <summary>Camera-relative 3D fog banks; each bank has independent wind and depth.</summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class Level1Fog : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Transform[] fogBanks;
    [Tooltip("Overall fog thickness. Lower this if the mountains are too hidden.")]
    [Range(0f, 2f)] public float density = 1f;
    public Color fogColor = new Color(0.82f, 0.91f, 0.97f, 1f);
    [Tooltip("Wind animation speed. Zero pauses the fog.")]
    [Range(0f, 3f)] public float windSpeed = 1f;
    [Tooltip("Preview wind in the editor without pressing Play.")]
    public bool animateInEditor = true;

    private MaterialPropertyBlock properties;
    private double lastTime;
    private float flowTime;
    private static readonly float[] Distances = { 0.71f, 0.49f, 0.29f };
    private static readonly float[] Centers = { 0.56f, 0.43f, 0.25f };
    private static readonly float[] Heights = { 0.29f, 0.27f, 0.24f };
    private static readonly float[] Densities = { 0.025f, 0.039f, 0.047f };

    public void Configure(Camera camera, Transform[] banks)
    {
        targetCamera = camera;
        fogBanks = banks;
        UpdateBanks();
    }

    private void OnEnable() => lastTime = CurrentTime();

    private static double CurrentTime()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying) return UnityEditor.EditorApplication.timeSinceStartup;
#endif
        return Time.timeAsDouble;
    }

    private void Update()
    {
        double now = CurrentTime();
        if (Application.isPlaying || animateInEditor)
            flowTime += (float)System.Math.Clamp(now - lastTime, 0.0, 0.1) * windSpeed;
        lastTime = now;
        UpdateBanks();
#if UNITY_EDITOR
        if (!Application.isPlaying && animateInEditor && windSpeed > 0f)
        {
            UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
        }
#endif
    }

    private void UpdateBanks()
    {
        if (targetCamera == null || fogBanks == null) return;
        properties ??= new MaterialPropertyBlock();
        for (int i = 0; i < Mathf.Min(fogBanks.Length, 3); i++)
        {
            var bank = fogBanks[i];
            if (bank == null) continue;
            float distance = targetCamera.farClipPlane * Distances[i];
            float height = targetCamera.orthographic ? 2f * targetCamera.orthographicSize
                : 2f * distance * Mathf.Tan(targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            bank.SetPositionAndRotation(targetCamera.transform.position
                + targetCamera.transform.forward * distance
                + targetCamera.transform.up * ((Centers[i] - 0.5f) * height),
                targetCamera.transform.rotation);
            bank.localScale = new Vector3(height * targetCamera.aspect * 1.65f,
                height * Heights[i], height * 0.24f);
            var renderer = bank.GetComponent<Renderer>();
            if (renderer == null) continue;
            renderer.GetPropertyBlock(properties);
            properties.SetFloat("_FlowTime", flowTime);
            properties.SetFloat("_Density", Densities[i] * density);
            properties.SetColor("_FogColor", fogColor);
            renderer.SetPropertyBlock(properties);
        }
    }
}
