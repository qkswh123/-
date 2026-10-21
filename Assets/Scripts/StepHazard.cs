using UnityEngine;

/// <summary>A timed obstacle: cyan is safe, yellow warns, red is dangerous.</summary>
public sealed class StepHazard : MonoBehaviour
{
    public enum Phase { Safe, Warning, Dangerous }
    [SerializeField] private Transform spinner;
    [SerializeField] private Renderer[] pieces;
    [SerializeField] private LineRenderer lightHalo;
    [SerializeField] private LineRenderer darkHalo;
    [SerializeField] private int stepIndex;
    [SerializeField] private float offset;
    [SerializeField] private float safeSeconds = 3f;
    [SerializeField] private float warningSeconds = 1.2f;
    [SerializeField] private float dangerousSeconds = 1.2f;
    private MaterialPropertyBlock properties;
    private float activatedAt;
    private float phaseShownAt;
    private Phase shownPhase;
    private IceStaircase staircase;
    private Camera previewCamera;
    private bool previewStarted;
    public int StepIndex => stepIndex;
    public Phase CurrentPhase => shownPhase;
    public bool IsMarkerVisible
    {
        get
        {
            if (!isActiveAndEnabled || spinner == null) return false;
            if (previewCamera == null) previewCamera = Camera.main;
            if (previewCamera == null) return false;
            var view = previewCamera.WorldToViewportPoint(spinner.position);
            return view.z > 0 && view.x > 0 && view.x < 1 && view.y > 0 && view.y < 1;
        }
    }
    // Collision follows what was actually rendered, with a short visual reaction margin.
    public bool IsDangerous => isActiveAndEnabled && shownPhase == Phase.Dangerous
        && Time.time - phaseShownAt >= 0.15f;

    private void OnEnable()
    {
        activatedAt = Time.time;
        phaseShownAt = Time.time;
        shownPhase = Phase.Safe;
        staircase = GetComponentInParent<IceStaircase>();
        previewCamera = Camera.main;
        previewStarted = staircase == null;
        warningSeconds = Mathf.Max(1.2f, warningSeconds);
        UpdateVisuals();
    }

    public void SetVisuals(Transform body, Renderer[] renderers) { spinner = body; pieces = renderers; }
    public void SetHalos(LineRenderer light, LineRenderer dark) { lightHalo = light; darkHalo = dark; }
    public void Configure(int index, int level)
    {
        bool reassigned = stepIndex != index;
        stepIndex = index;
        float difficulty = (Mathf.Clamp(level, 1, 20) - 1) / 19f;
        safeSeconds = Mathf.Lerp(3f, 2.2f, difficulty);
        warningSeconds = 1.2f;
        dangerousSeconds = Mathf.Lerp(1.2f, 1.6f, difficulty);
        // Stagger cycles without ever starting a newly activated obstacle in danger.
        offset = Mathf.Repeat(index * 0.173f, 0.6f) * safeSeconds;
        if (reassigned)
        {
            activatedAt = Time.time;
            shownPhase = Phase.Safe;
            phaseShownAt = Time.time;
            previewStarted = staircase == null;
        }
        UpdateVisuals();
    }
    public Phase PhaseAtTime(float time)
    {
        if (!previewStarted) return Phase.Safe;
        float elapsed = Mathf.Max(0, time - activatedAt);
        if (elapsed < 1.5f) return Phase.Safe;
        float phase = Mathf.Repeat(elapsed - 1.5f + offset, safeSeconds + warningSeconds + dangerousSeconds);
        if (phase < safeSeconds) return Phase.Safe;
        return phase < safeSeconds + warningSeconds ? Phase.Warning : Phase.Dangerous;
    }
    private bool CanPreview()
    {
        if (staircase == null) return true;
        if (previewCamera == null) previewCamera = Camera.main;
        if (previewCamera == null || staircase.progressTarget == null) return false;
        float ahead = transform.position.y - staircase.progressTarget.position.y;
        if (ahead > staircase.verticalStep * 6f || ahead < -staircase.verticalStep) return false;
        var view = previewCamera.WorldToViewportPoint(transform.position + Vector3.up * 0.55f);
        return view.z > 0 && view.x > 0.04f && view.x < 0.96f && view.y > 0.08f && view.y < 0.82f;
    }

    private void Update()
    {
        // A distant/offscreen obstacle must not enter danger before the player can see it.
        if (!previewStarted && CanPreview())
        {
            previewStarted = true;
            activatedAt = Time.time;
            phaseShownAt = Time.time;
        }
        UpdateVisuals();
    }

    private void OnGUI()
    {
        // Visibility is independent of the six-step activation range: warn as soon as it enters view.
        if (Event.current.type != EventType.Repaint || !IsMarkerVisible) return;
        var point = previewCamera.WorldToScreenPoint(spinner.position + Vector3.up * 0.45f);
        if (point.z <= 0) return;
        float size = Mathf.Clamp(Screen.height / 540f * 18f, 18f, 36f);
        var rect = new Rect(point.x - size * 0.5f, Screen.height - point.y - size, size, size);
        Color previous = GUI.color;
        int previousDepth = GUI.depth;
        GUI.depth = 5;
        GUI.color = new Color(0.12f, 0.15f, 0.2f);
        GUI.DrawTexture(new Rect(rect.x - 2, rect.y - 2, size + 4, size + 4), Texture2D.whiteTexture);
        GUI.color = shownPhase == Phase.Dangerous ? new Color(1f, 0.3f, 0.35f)
            : shownPhase == Phase.Warning ? new Color(1f, 0.8f, 0.2f) : Color.white;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = Color.white;
        var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.RoundToInt(size * 0.9f), fontStyle = FontStyle.Bold };
        style.normal.textColor = new Color(0.1f, 0.13f, 0.18f);
        GUI.Label(rect, "!", style);
        GUI.color = previous;
        GUI.depth = previousDepth;
    }

    private void LateUpdate()
    {
        if (spinner == null || previewCamera == null) return;
        foreach (var halo in new[] { lightHalo, darkHalo })
        {
            if (halo == null) continue;
            var center = spinner.position - previewCamera.transform.forward * (halo == darkHalo ? 0.08f : 0.06f);
            for (int i = 0; i < halo.positionCount; i++)
            {
                float angle = i * Mathf.PI * 2f / halo.positionCount;
                halo.SetPosition(i, transform.InverseTransformPoint(center +
                    (previewCamera.transform.right * Mathf.Cos(angle) + previewCamera.transform.up * Mathf.Sin(angle)) * 0.46f));
            }
        }
    }
    private void UpdateVisuals()
    {
        var phase = PhaseAtTime(Time.time);
        if (phase != shownPhase) phaseShownAt = Time.time;
        shownPhase = phase;
        Color color = phase == Phase.Safe ? new Color(0.2f, 0.9f, 0.92f)
            : phase == Phase.Warning ? new Color(1f, 0.72f, 0.12f) : new Color(1f, 0.14f, 0.23f);
        if (spinner != null)
        {
            // Never hide the safe obstacle or make it suddenly grow on activation.
            spinner.localScale = Vector3.one * 1.15f;
            spinner.localPosition = new Vector3(0.22f, 0.72f, -0.32f);
            spinner.localRotation = Quaternion.Euler(0, Time.time * 85f, 0);
        }
        if (pieces == null) return;
        properties ??= new MaterialPropertyBlock();
        foreach (var piece in pieces)
        {
            if (piece == null) continue;
            piece.GetPropertyBlock(properties);
            properties.SetColor("_BaseColor", color);
            properties.SetColor("_EmissionColor", color * 0.25f);
            piece.SetPropertyBlock(properties);
        }
    }
}
