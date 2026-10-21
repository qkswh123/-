using System.Collections.Generic;
using UnityEngine;

/// <summary>Builds a left/right climbing route and recycles steps below the player.</summary>
[DisallowMultipleComponent]
public sealed class IceStaircase : MonoBehaviour
{
    [System.Serializable]
    private sealed class Step
    {
        public Transform block;
        public int index;
    }

    [Header("Block and progress")]
    [SerializeField] private GameObject stepPrefab;
    [SerializeField] private Camera gameCamera;
    [SerializeField] private GameObject[] blockVariants;
    [SerializeField] private bool shuffleBlockAppearance;
    [SerializeField] private bool randomizeOnPlay;
    [Tooltip("Assign the penguin here later. Until then, the camera previews the staircase.")]
    public Transform progressTarget;

    [Header("Stair shape")]
    [Min(0.1f)] public float horizontalStep = 2.05f;
    [Min(0.1f)] public float verticalStep = 1.05f;
    [Range(1, 4)] public int maximumLane = 2;
    public int seed = 2741;
    [Min(8)] public int initialStepCount = 30;
    [Min(10f)] public float generateAhead = 23f;
    [Min(5f)] public float keepBelow = 9f;

    [Header("Timed obstacles")]
    [SerializeField] private GameObject hazardPrefab;
    [SerializeField] private int hazardLevel = 1;
    [SerializeField] private int goalStep = 50;

    [Header("Step coins")]
    [SerializeField] private GameObject coinPrefab;

    [Header("Camera preview (before adding a player)")]
    [Tooltip("Move the camera upward during Play to demonstrate endless generation. Automatically ignored when a progress target is assigned.")]
    public bool previewAutoClimb = true;
    [Min(0f)] public float previewSpeed = 1.1f;

    [SerializeField, HideInInspector] private List<Step> steps = new List<Step>();
    [SerializeField, HideInInspector] private int nextIndex;
    [SerializeField, HideInInspector] private int lastLane;
    [SerializeField, HideInInspector] private int direction = 1;
    [SerializeField, HideInInspector] private int remainingRun;
    [SerializeField, HideInInspector] private float previewCameraStartY;

    public int ActiveStepCount => steps.Count;
    public int HighestStepIndex => nextIndex - 1;
    public int LowestStepIndex => steps.Count > 0 ? steps[0].index : 0;

    public void ConfigureBlockVariations(GameObject[] variants, bool randomize, bool randomizeRoute = false)
    {
        blockVariants = variants;
        shuffleBlockAppearance = randomize;
        randomizeOnPlay = randomizeRoute;
        foreach (var step in steps) ApplyAppearance(step);
    }

    private void ApplyAppearance(Step step)
    {
        if (!shuffleBlockAppearance || blockVariants == null || blockVariants.Length == 0) return;
        var prefab = blockVariants[(int)(RouteHash(step.index ^ 0x51ed) % (uint)blockVariants.Length)];
        if (prefab == null) return;
        var sourceMesh = prefab.GetComponent<MeshFilter>();
        var sourceRenderer = prefab.GetComponent<MeshRenderer>();
        if (sourceMesh == null || sourceRenderer == null) return;
        step.block.GetComponent<MeshFilter>().sharedMesh = sourceMesh.sharedMesh;
        step.block.GetComponent<MeshRenderer>().sharedMaterials = sourceRenderer.sharedMaterials;
        var collider = step.block.GetComponent<MeshCollider>();
        if (collider != null) collider.sharedMesh = sourceMesh.sharedMesh;
    }

    public void ConfigureObstacles(GameObject prefab, int level, int goal)
    {
        hazardPrefab = prefab;
        hazardLevel = level;
        goalStep = goal;
        foreach (var step in steps) UpdateHazard(step);
        foreach (var step in steps) UpdateCoin(step);
    }

    public void ConfigureCoins(GameObject prefab)
    {
        coinPrefab = prefab;
        foreach (var step in steps) UpdateCoin(step);
    }

    private void UpdateCoin(Step step)
    {
        if (coinPrefab == null || step.block == null) return;
        var child = step.block.Find("Step Coin");
        if (child == null)
        {
            child = Instantiate(coinPrefab, step.block).transform;
            child.name = "Step Coin";
        }
        child.GetComponent<StepCoin>().Configure(step.index, step.index > 0 && step.index <= goalStep);
    }

    public StepCoin GetCoin(int index)
    {
        foreach (var step in steps)
            if (step.index == index && step.block != null)
                return step.block.GetComponentInChildren<StepCoin>();
        return null;
    }

    public bool TryCollectCoin(int index) => GetCoin(index)?.TryCollect() == true;

    private void UpdateHazard(Step step)
    {
        if (hazardPrefab == null || step.block == null) return;
        var child = step.block.Find("Step Hazard");
        if (child == null)
        {
            child = Instantiate(hazardPrefab, step.block).transform;
            child.name = "Step Hazard";
        }
        int interval = Mathf.Max(8, 12 - (Mathf.Clamp(hazardLevel, 1, 20) - 1) / 4);
        bool eligible = step.index >= 8 && step.index < goalStep && step.index % interval == 0;
        // Configure the recycled index while hidden, before it can be drawn or checked.
        if (!eligible) child.gameObject.SetActive(false);
        child.GetComponent<StepHazard>().Configure(step.index, hazardLevel);
        if (eligible) child.gameObject.SetActive(true);
    }

    public StepHazard GetHazard(int index)
    {
        foreach (var step in steps)
        {
            if (step.index != index) continue;
            var hazard = step.block.GetComponentInChildren<StepHazard>();
            return hazard != null && hazard.gameObject.activeInHierarchy ? hazard : null;
        }
        return null;
    }

    public void Configure(GameObject prefab, Camera camera)
    {
        stepPrefab = prefab;
        gameCamera = camera;
        previewCameraStartY = camera != null ? camera.transform.position.y : 0f;
        MaintainForHeight(0f);
    }

    public void Rebuild(int routeSeed)
    {
        foreach (var step in steps)
        {
            if (step.block == null) continue;
            if (Application.isPlaying) { step.block.gameObject.SetActive(false); Destroy(step.block.gameObject); }
            else DestroyImmediate(step.block.gameObject);
        }
        steps.Clear();
        nextIndex = 0;
        lastLane = 0;
        direction = 1;
        remainingRun = 0;
        seed = routeSeed;
        MaintainForHeight(0f);
    }

    private void Start()
    {
        if (gameCamera == null) gameCamera = Camera.main;
        if (randomizeOnPlay) Rebuild(System.Environment.TickCount ^ seed);
        MaintainForHeight(0f);
        // Saved scene steps already exist, so initialize their runtime collection state too.
        foreach (var step in steps) UpdateCoin(step);
    }

    private void Update()
    {
        if (gameCamera == null) return;
        if (progressTarget == null && previewAutoClimb)
            gameCamera.transform.position += Vector3.up * (previewSpeed * Time.deltaTime);

        float height = progressTarget != null
            ? progressTarget.position.y - transform.position.y
            : gameCamera.transform.position.y - previewCameraStartY;
        MaintainForHeight(height);
    }

    /// <summary>Keep a finite window of steps around the current climbing height.</summary>
    public void MaintainForHeight(float height)
    {
        if (stepPrefab == null || !float.IsFinite(height)) return;
        float rise = Mathf.Max(0.1f, verticalStep);
        float cutoff = height - Mathf.Max(5f, keepBelow);
        float highestNeeded = Mathf.Max(0f, height) + Mathf.Max(10f, generateAhead);

        while (steps.Count < Mathf.Max(8, initialStepCount) || nextIndex * rise <= highestNeeded)
        {
            Step step;
            if (steps.Count >= Mathf.Max(8, initialStepCount)
                && steps[0].block.localPosition.y < cutoff)
            {
                // Reuse the oldest block, including its renderer and collider.
                step = steps[0];
                steps.RemoveAt(0);
            }
            else
            {
                var block = Instantiate(stepPrefab, transform);
                step = new Step { block = block.transform };
            }

            if (nextIndex > 0)
            {
                uint choice = RouteHash(nextIndex);
                if (remainingRun <= 0)
                {
                    direction = (choice & 1u) == 0 ? -1 : 1;
                    remainingRun = 2 + (int)((choice >> 1) % 3);
                }
                if (Mathf.Abs(lastLane + direction) > Mathf.Max(1, maximumLane))
                    direction = -direction;
                lastLane += direction;
                remainingRun--;
            }

            step.index = nextIndex;
            step.block.name = "Ice Step " + nextIndex.ToString("D4");
            step.block.localPosition = new Vector3(lastLane * horizontalStep, nextIndex * rise, 0f);
            step.block.localRotation = Quaternion.identity;
            ApplyAppearance(step);
            UpdateHazard(step);
            UpdateCoin(step);
            steps.Add(step);
            nextIndex++;
        }
    }

    // Does not change Unity's global random state; route stays repeatable between runs.
    private uint RouteHash(int index)
    {
        unchecked
        {
            uint value = (uint)(index ^ seed);
            value ^= value >> 16;
            value *= 0x7feb352du;
            value ^= value >> 15;
            value *= 0x846ca68bu;
            return value ^ (value >> 16);
        }
    }

    /// <summary>For the future penguin controller: surface position of an active step.</summary>
    public bool TryGetStepTop(int index, out Vector3 position)
    {
        foreach (var step in steps)
        {
            if (step.index != index) continue;
            position = step.block.position;
            return true;
        }
        position = default;
        return false;
    }

    public bool TryGetNextStep(int currentIndex, out Vector3 position, out int inputDirection)
    {
        if (TryGetStepTop(currentIndex, out var current)
            && TryGetStepTop(currentIndex + 1, out position))
        {
            inputDirection = position.x > current.x ? 1 : -1;
            return true;
        }
        position = default;
        inputDirection = 0;
        return false;
    }
}
