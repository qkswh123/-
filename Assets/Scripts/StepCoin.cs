using UnityEngine;

/// <summary>A pooled step reward. Collected only after a successful landing.</summary>
public sealed class StepCoin : MonoBehaviour
{
    [SerializeField] private Transform visual;
    [SerializeField] private int stepIndex = -1;
    private bool collected;
    private bool eligible;
    private float collectedAt;
    private float horizontalOffset;
    [SerializeField] private Vector3 baseScale = Vector3.one;
    public int StepIndex => stepIndex;
    public bool IsAvailable => eligible && !collected && gameObject.activeInHierarchy;

    public void SetVisual(Transform body) { visual = body; baseScale = body.localScale; }

    public void Configure(int index, bool canCollect)
    {
        if (stepIndex != index) collected = false;
        stepIndex = index;
        eligible = canCollect;
        baseScale.z = Mathf.Max(baseScale.z, baseScale.x * 4f);
        var hazard = transform.parent != null ? transform.parent.GetComponentInChildren<StepHazard>() : null;
        horizontalOffset = hazard != null ? -0.45f : 0f;
        if (visual == null) return;
        visual.gameObject.SetActive(eligible && !collected);
        visual.localScale = baseScale;
        visual.localPosition = new Vector3(horizontalOffset, 0.65f, -0.48f);
    }

    public bool TryCollect()
    {
        if (!IsAvailable) return false;
        collected = true;
        collectedAt = Time.time;
        return true;
    }

    private void Update()
    {
        if (visual == null || !eligible || !visual.gameObject.activeSelf) return;
        visual.localRotation = Quaternion.Euler(0, Time.time * 110f, 0);
        if (!collected)
        {
            visual.localPosition = new Vector3(horizontalOffset, 0.65f + Mathf.Sin(Time.time * 3f + stepIndex) * 0.06f, -0.48f);
            return;
        }
        float t = Mathf.Clamp01((Time.time - collectedAt) / 0.22f);
        visual.localPosition = new Vector3(horizontalOffset, 0.65f + t * 0.55f, -0.48f);
        visual.localScale = baseScale * (1f - t);
        if (t >= 1f) visual.gameObject.SetActive(false);
    }
}
