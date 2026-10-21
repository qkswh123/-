using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class AnimalClimber : MonoBehaviour
{
    [SerializeField] private IceStaircase staircase;
    [SerializeField] private Camera gameCamera;
    [SerializeField] private GameObject visualPrefab;
    [SerializeField] private Transform visual;
    [SerializeField] private Vector3 cameraOffset;
    [SerializeField] private float hopDuration = 0.26f;
    private readonly List<Material> ownedMaterials = new List<Material>();
    private Animator animator;
    private bool hopping;
    private int currentStep;
    private float wrongUntil;
    private bool falling;
    private Vector3 heldCameraPosition;
    private float fallStarted;

    public bool InputEnabled { get; set; } = true;
    [System.NonSerialized] public bool AcceptKeyboardInput = true;
    public bool IsHopping => hopping;
    public bool IsFalling => falling;
    public int CurrentStep => currentStep;
    public bool WrongDirection => Time.unscaledTime < wrongUntil;
    public event System.Action<int> StepReached;
    public event System.Action<string> Failed;

    public void Configure(IceStaircase stairs, Camera camera, GameObject prefab, float duration)
    {
        staircase = stairs;
        gameCamera = camera;
        hopDuration = duration;
        currentStep = 0;
        hopping = false;
        falling = false;
        InputEnabled = true;
        if (staircase.TryGetStepTop(0, out var start)) transform.position = start;
        cameraOffset = gameCamera.transform.position - transform.position;
        staircase.previewAutoClimb = false;
        staircase.progressTarget = transform;

        if (visual == null || visualPrefab != prefab)
        {
            if (visual != null)
            {
                if (Application.isPlaying) Destroy(visual.gameObject);
                else DestroyImmediate(visual.gameObject);
            }
            var model = Instantiate(prefab, transform);
            model.name = "Animal Visual";
            visual = model.transform;
            visualPrefab = prefab;
            visual.localPosition = Vector3.zero;
            visual.localRotation = Quaternion.identity;
            foreach (var lod in model.GetComponentsInChildren<LODGroup>()) lod.ForceLOD(0);
            FitVisualToStep();
            if (Application.isPlaying) ConvertImportedMaterials();
        }
        animator = visual.GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.applyRootMotion = false;
            if (Application.isPlaying) PlayAnimation("Idle_A");
        }
        visual.localRotation = Quaternion.Euler(0f, 180f, 0f);
    }

    private void FitVisualToStep()
    {
        var renderers = visual.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;
        var bounds = renderers[0].bounds;
        foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        float scale = Mathf.Min(1.35f / Mathf.Max(0.01f, bounds.size.y),
            1.28f / Mathf.Max(0.01f, Mathf.Max(bounds.size.x, bounds.size.z)));
        visual.localScale *= scale;
        bounds = renderers[0].bounds;
        foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        visual.position += new Vector3(transform.position.x - bounds.center.x,
            transform.position.y - bounds.min.y + 0.015f, transform.position.z - bounds.center.z);
    }

    private void ConvertImportedMaterials()
    {
        // New animal assets may still use Built-in shaders. Keep their originals untouched.
        var converted = new Dictionary<Material, Material>();
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) return;
        foreach (var renderer in visual.GetComponentsInChildren<Renderer>())
        {
            var materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                var source = materials[i];
                if (source == null || source.shader.name.Contains("Universal")
                    || source.shader.name.Contains("Shader Graphs")) continue;
                if (!converted.TryGetValue(source, out var replacement))
                {
                    replacement = new Material(shader) { name = source.name + " (Climbing URP)" };
                    replacement.SetTexture("_BaseMap", source.mainTexture);
                    replacement.SetColor("_BaseColor", source.HasProperty("_Color") ? source.color : Color.white);
                    replacement.SetFloat("_Smoothness", 0.2f);
                    converted.Add(source, replacement);
                    ownedMaterials.Add(replacement);
                }
                materials[i] = replacement;
            }
            renderer.sharedMaterials = materials;
        }
    }

    private void Update()
    {
        if (!falling && !hopping && staircase != null
            && staircase.TryGetStepTop(currentStep, out var top) && transform.position.y < top.y - 0.6f)
            TriggerFall("블록에서 떨어졌어요!", 1);
        if (!AcceptKeyboardInput || !InputEnabled || hopping || Keyboard.current == null) return;
        bool left = Keyboard.current.leftArrowKey.wasPressedThisFrame;
        bool right = Keyboard.current.rightArrowKey.wasPressedThisFrame;
        if (left == right) return;
        TryClimb(right ? 1 : -1);
    }

    public bool TryClimb(int inputDirection)
    {
        if (!Application.isPlaying || !InputEnabled || hopping || falling || staircase == null) return false;
        staircase.MaintainForHeight(transform.position.y - staircase.transform.position.y);
        if (!staircase.TryGetNextStep(currentStep, out var next, out int expected)) return false;
        if (inputDirection != expected)
        {
            wrongUntil = Time.unscaledTime + 1f;
            TriggerFall("반대 방향으로 점프했어요!", inputDirection);
            return false;
        }
        wrongUntil = 0f;
        StartCoroutine(Hop(next, expected));
        return true;
    }

    private IEnumerator Hop(Vector3 destination, int direction)
    {
        hopping = true;
        var start = transform.position;
        visual.localRotation = Quaternion.Euler(0, direction > 0 ? 130 : -130, 0);
        PlayAnimation("Jump");
        float elapsed = 0f;
        while (elapsed < hopDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.05f, hopDuration));
            transform.position = Vector3.Lerp(start, destination, Mathf.SmoothStep(0, 1, t))
                + Vector3.up * (Mathf.Sin(t * Mathf.PI) * 0.36f);
            yield return null;
        }
        transform.position = destination;
        var hazard = staircase.GetHazard(currentStep + 1);
        if (hazard != null && hazard.IsDangerous)
        {
            TriggerFall("켜진 장애물에 부딪혔어요!", -direction);
            yield break;
        }
        currentStep++;
        hopping = false;
        PlayAnimation("Idle_A");
        StepReached?.Invoke(currentStep);
    }

    public void TriggerFall(string reason, int direction = 1)
    {
        if (falling) return;
        falling = true;
        hopping = false;
        InputEnabled = false;
        heldCameraPosition = gameCamera != null ? gameCamera.transform.position : Vector3.zero;
        fallStarted = Time.unscaledTime;
        StopAllCoroutines();
        Failed?.Invoke(reason);
        StartCoroutine(FallSequence(direction));
    }

    private IEnumerator FallSequence(int direction)
    {
        var start = transform.position;
        var velocity = new Vector3(Mathf.Sign(direction) * 2.3f, 2.8f, -0.5f);
        float elapsed = 0f;
        var renderers = visual.GetComponentsInChildren<Renderer>();
        var flash = new MaterialPropertyBlock();
        while (elapsed < 1.5f)
        {
            elapsed += Time.unscaledDeltaTime;
            transform.position = start + velocity * elapsed + Vector3.down * (5.5f * elapsed * elapsed);
            visual.Rotate(0, 80f * Time.unscaledDeltaTime, -direction * 150f * Time.unscaledDeltaTime, Space.Self);
            foreach (var renderer in renderers)
            {
                renderer.GetPropertyBlock(flash);
                flash.SetColor("_BaseColor", elapsed < 0.15f ? new Color(1, 0.35f, 0.4f) : Color.white);
                renderer.SetPropertyBlock(flash);
            }
            yield return null;
        }
    }

    private void PlayAnimation(string state)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;
        if (animator.HasState(0, Animator.StringToHash(state))) animator.CrossFade(state, 0.07f, 0);
    }

    private void LateUpdate()
    {
        if (gameCamera == null) return;
        if (falling)
        {
            float t = Time.unscaledTime - fallStarted;
            gameCamera.transform.position = heldCameraPosition + new Vector3(Mathf.Sin(t * 75), Mathf.Cos(t * 63), 0) * (0.08f * Mathf.Exp(-t * 8));
            return;
        }
        var target = new Vector3(cameraOffset.x + transform.position.x * 0.2f,
            cameraOffset.y + transform.position.y, cameraOffset.z + transform.position.z);
        gameCamera.transform.position = Vector3.Lerp(gameCamera.transform.position, target,
            1f - Mathf.Exp(-7f * Time.deltaTime));
    }

    private void OnDestroy()
    {
        foreach (var material in ownedMaterials) if (material != null) Destroy(material);
    }
}
