using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public sealed class ClimbLevel : MonoBehaviour
{
    public ClimbGameSettings settings;
    [Range(1, 20)] public int levelNumber = 1;
    public string environmentName;
    [SerializeField] private AnimalClimber climber;
    [SerializeField] private IceStaircase staircase;
    [SerializeField] private Camera gameCamera;
    [Header("HUD presentation")]
    [Range(0.75f, 1.2f)] public float hudScale = 0.9f;
    private Texture2D panelTexture;
    private bool choosingLevel;
    private bool completed;
    private bool loading;
    private bool failed;
    private string failureReason;
    private float restartAt;
    private Font uiFont;
    private int points;
    private float lastCoinAt = -10f;
    public AnimalClimber Climber => climber;
    public int Goal => settings.GoalForLevel(levelNumber);
    public bool Completed => completed;
    public bool HasFailed => failed;
    public int Points => points;
    public const int PointsPerCoin = 10;

    public void Configure(ClimbGameSettings configuration, int number, IceStaircase stairs, Camera camera)
    {
        settings = configuration;
        levelNumber = Mathf.Clamp(number, 1, 20);
        staircase = stairs;
        gameCamera = camera;
        if (climber == null)
        {
            var actor = new GameObject("Animal Climber");
            actor.transform.SetParent(transform, false);
            climber = actor.AddComponent<AnimalClimber>();
        }
        var animal = settings.ResolveAnimal(levelNumber, out _);
        if (animal != null) climber.Configure(staircase, gameCamera, animal.prefab, settings.HopTimeForLevel(levelNumber));
    }

    private void Start()
    {
        Configure(settings, levelNumber, staircase, gameCamera);
        climber.StepReached += OnStepReached;
        climber.Failed += OnClimberFailed;
    }

    private void OnClimberFailed(string reason)
    {
        if (failed || completed || loading) return;
        failed = true;
        choosingLevel = false;
        failureReason = reason;
        restartAt = Time.unscaledTime + 2f;
        climber.InputEnabled = false;
    }

    private void OnStepReached(int step)
    {
        if (!failed && !completed && staircase.TryCollectCoin(step))
        {
            points += PointsPerCoin;
            lastCoinAt = Time.unscaledTime;
        }
        if (step < Goal) return;
        completed = true;
        climber.InputEnabled = false;
    }

    private void Update()
    {
        if (loading) return;
        if (failed && Time.unscaledTime >= restartAt) { LoadLevel(levelNumber); return; }
        if (Keyboard.current != null)
        {
            if (!failed && Keyboard.current.tabKey.wasPressedThisFrame) choosingLevel = !choosingLevel;
            if (Keyboard.current.rKey.wasPressedThisFrame) LoadLevel(levelNumber);
            if (completed && Keyboard.current.nKey.wasPressedThisFrame) LoadLevel(levelNumber == 20 ? 1 : levelNumber + 1);
        }
        climber.InputEnabled = !choosingLevel && !completed && !loading && !failed;
    }

    public void LoadLevel(int number)
    {
        if (loading) return;
        string scene = ClimbGameSettings.SceneName(number);
        if (!Application.CanStreamedLevelBeLoaded(scene))
        {
            Debug.LogError("Level is missing from Build Settings: " + scene);
            return;
        }
        loading = true;
        climber.InputEnabled = false;
        SceneManager.LoadSceneAsync(scene);
    }

    private void OnGUI()
    {
        if (!Application.isPlaying || settings == null || climber == null) return;
        if (uiFont == null)
        {
            var installed = Font.GetOSInstalledFontNames();
            foreach (var candidate in new[] { "Apple SD Gothic Neo", "AppleGothic", "Malgun Gothic", "Noto Sans CJK KR", "Arial" })
            {
                if (System.Array.IndexOf(installed, candidate) < 0) continue;
                uiFont = Font.CreateDynamicFontFromOSFont(candidate, 18);
                break;
            }
        }
        var oldMatrix = GUI.matrix;
        var oldFont = GUI.skin.font;
        GUI.skin.font = uiFont;
        float scale = Mathf.Min(Screen.width / 960f, Screen.height / 540f);
        GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 960f * scale) * 0.5f,
            (Screen.height - 540f * scale) * 0.5f, 0), Quaternion.identity, Vector3.one * scale);
        var title = new GUIStyle(GUI.skin.label) { fontSize = 21, fontStyle = FontStyle.Bold };
        var label = new GUIStyle(GUI.skin.label) { fontSize = 16 };
        var button = new GUIStyle(GUI.skin.button) { fontSize = 16 };
        DrawHud(scale);
        GUI.Box(new Rect(180, 496, 600, 32), "");
        GUI.Label(new Rect(193, 500, 580, 27), "장애물  ·  하늘색: 안전  /  노랑: 곧 켜짐  /  빨강: 위험", label);
        if (failed)
        {
            var previousColor = GUI.color;
            GUI.color = new Color(0.35f, 0.03f, 0.08f, 0.18f);
            GUI.DrawTexture(new Rect(0, 0, 960, 540), Texture2D.whiteTexture);
            GUI.color = previousColor;
            DrawBlock(new Rect(280, 174, 410, 194), new Color(0.08f, 0.18f, 0.25f, 0.96f));
            GUI.Label(new Rect(304, 190, 365, 34), "앗! 다시 도전해요", title);
            GUI.Label(new Rect(304, 231, 365, 28), failureReason, label);
            GUI.Label(new Rect(304, 263, 365, 28), Mathf.Max(1, Mathf.CeilToInt(restartAt - Time.unscaledTime)) + "초 후 현재 레벨을 다시 시작합니다", label);
            if (BlockButton(new Rect(325, 307, 320, 43), "지금 다시 시작  (R)", button)) LoadLevel(levelNumber);
        }
        else if (choosingLevel)
        {
            climber.InputEnabled = false;
            DrawBlock(new Rect(230, 125, 500, 305), new Color(0.08f, 0.18f, 0.25f, 0.96f));
            GUI.Label(new Rect(252, 139, 420, 32), "레벨 선택", title);
            for (int i = 1; i <= 20; i++)
            {
                int column = (i - 1) % 5, row = (i - 1) / 5;
                if (BlockButton(new Rect(252 + column * 94, 183 + row * 55, 82, 43), i.ToString(), button)) LoadLevel(i);
            }
            if (BlockButton(new Rect(605, 382, 100, 32), "닫기", button)) choosingLevel = false;
        }
        else if (completed)
        {
            DrawBlock(new Rect(285, 190, 390, 155), new Color(0.08f, 0.18f, 0.25f, 0.96f));
            GUI.Label(new Rect(307, 208, 350, 35), levelNumber == 20 ? "모든 레벨 완료!" : "레벨 " + levelNumber + " 완료!", title);
            if (BlockButton(new Rect(325, 267, 310, 48), levelNumber == 20 ? "레벨 1부터 다시 시작" : "다음 레벨  (N)", button))
                LoadLevel(levelNumber == 20 ? 1 : levelNumber + 1);
        }
        else
        {
            int hazardDistance = 1;
            StepHazard nextHazard = null;
            for (; hazardDistance <= 3; hazardDistance++)
            {
                nextHazard = staircase.GetHazard(climber.CurrentStep + hazardDistance);
                if (nextHazard != null) break;
            }
            if (nextHazard != null)
            {
                GUI.Box(new Rect(260, 443, 440, 38), "");
                GUI.Label(new Rect(275, 449, 420, 29), hazardDistance > 1
                    ? hazardDistance + "칸 뒤에 장애물! 하늘색일 때 이동하세요"
                    : nextHazard.CurrentPhase == StepHazard.Phase.Dangerous
                        ? "다음 계단은 위험해요. 하늘색이 되면 점프!"
                        : "착지할 때 장애물이 하늘색인지 확인하세요", label);
            }
        }
        GUI.matrix = oldMatrix;
        GUI.skin.font = oldFont;
    }

    // Screen-space blocks retain depth and highlights without obscuring the climbing route.
    private void DrawHud(float screenScale)
    {
        var matrix = GUI.matrix;
        float left = -(Screen.width / screenScale - 960f) * 0.5f + 14f;
        float top = -(Screen.height / screenScale - 540f) * 0.5f + 14f;
        float right = 960f - left;
        float bottom = 540f - top;
        float size = Mathf.Clamp(hudScale, 0.75f, 1.2f);
        var ink = new Color(0.9f, 0.97f, 1f);
        var detail = new Color(0.66f, 0.82f, 0.88f);
        var animal = settings.ResolveAnimal(levelNumber, out _);

        GUI.matrix = matrix * Matrix4x4.TRS(new Vector3(left, top, 0), Quaternion.identity, Vector3.one * size);
        DrawBlock(new Rect(0, 0, 272, 72), new Color(0.06f, 0.18f, 0.25f, 0.88f));
        GUI.Label(new Rect(12, 7, 248, 24), "레벨 " + levelNumber + " / 20  ·  " + environmentName, HudText(16, ink, true));
        GUI.Label(new Rect(12, 31, 248, 21), animal?.displayName + "  ·  " + climber.CurrentStep + " / " + Goal, HudText(13, detail));
        Fill(new Rect(12, 58, 248, 4), new Color(0.02f, 0.08f, 0.12f, 0.8f));
        Fill(new Rect(12, 58, 248 * Mathf.Clamp01((float)climber.CurrentStep / Goal), 4), new Color(0.35f, 0.88f, 0.94f));

        GUI.matrix = matrix * Matrix4x4.TRS(new Vector3(right - 214 * size, top, 0), Quaternion.identity, Vector3.one * size);
        var buttonText = HudText(14, ink, true);
        if (BlockButton(new Rect(0, 0, 102, 34), "레벨 선택", buttonText))
        {
            choosingLevel = !choosingLevel;
            climber.InputEnabled = !choosingLevel && !completed && !loading && !failed;
        }
        if (BlockButton(new Rect(112, 0, 102, 34), "다시 시작", buttonText)) LoadLevel(levelNumber);

        GUI.matrix = matrix * Matrix4x4.TRS(new Vector3(left, bottom - 61 * size, 0), Quaternion.identity, Vector3.one * size);
        DrawBlock(new Rect(0, 0, 174, 55), new Color(0.06f, 0.18f, 0.25f, 0.88f));
        GUI.Label(new Rect(12, 4, 150, 20), "코인 포인트", HudText(12, detail));
        GUI.Label(new Rect(12, 22, 108, 29), points.ToString(), HudText(23, new Color(1f, 0.86f, 0.36f), true));
        float rewardAge = Time.unscaledTime - lastCoinAt;
        if (rewardAge < 0.7f)
            GUI.Label(new Rect(119, 25 - rewardAge * 6, 48, 24), "+10", HudText(16, new Color(1f, 0.9f, 0.48f, 1 - rewardAge / 0.7f), true));
        GUI.matrix = matrix;
    }

    private GUIStyle HudText(int size, Color color, bool bold = false)
    {
        var style = new GUIStyle(GUI.skin.label) {
            fontSize = size, fontStyle = bold ? FontStyle.Bold : FontStyle.Normal,
            alignment = TextAnchor.MiddleLeft, clipping = TextClipping.Clip,
            padding = new RectOffset(0, 0, 0, 0)
        };
        style.normal.textColor = color;
        return style;
    }

    private void Fill(Rect rect, Color color)
    {
        var previous = GUI.color;
        GUI.color *= color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
    }

    private void Rounded(Rect rect, Color color)
    {
        if (panelTexture == null)
        {
            const int resolution = 24;
            panelTexture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color[resolution * resolution];
            for (int y = 0; y < resolution; y++)
                for (int x = 0; x < resolution; x++)
                {
                    float dx = Mathf.Max(4.5f - x, x - 18.5f, 0);
                    float dy = Mathf.Max(4.5f - y, y - 18.5f, 0);
                    pixels[y * resolution + x] = new Color(1, 1, 1, Mathf.Clamp01(5 - Mathf.Sqrt(dx * dx + dy * dy)));
                }
            panelTexture.SetPixels(pixels);
            panelTexture.Apply();
        }
        var previous = GUI.color;
        GUI.color *= color;
        GUI.DrawTexture(rect, panelTexture, ScaleMode.StretchToFill);
        GUI.color = previous;
    }

    private void DrawBlock(Rect rect, Color face)
    {
        Rounded(new Rect(rect.x + 2, rect.y + 6, rect.width, rect.height), new Color(0.01f, 0.04f, 0.07f, 0.4f));
        Rounded(new Rect(rect.x, rect.y + 4, rect.width, rect.height), new Color(0.04f, 0.12f, 0.18f, face.a));
        Rounded(rect, new Color(0.53f, 0.78f, 0.85f, face.a));
        Rounded(new Rect(rect.x + 1, rect.y + 1, rect.width - 2, rect.height - 2), face);
        Fill(new Rect(rect.x + 8, rect.y + 2, rect.width - 16, 1), new Color(0.85f, 0.98f, 1f, 0.3f));
    }

    private bool BlockButton(Rect rect, string text, GUIStyle textStyle)
    {
        bool hover = rect.Contains(Event.current.mousePosition) && GUI.enabled;
        bool pressed = hover && Mouse.current != null && Mouse.current.leftButton.isPressed;
        var face = hover ? new Color(0.18f, 0.43f, 0.52f, 0.98f) : new Color(0.1f, 0.28f, 0.36f, 0.96f);
        var visual = rect;
        if (pressed) visual.y += 2;
        DrawBlock(visual, face);
        var centered = new GUIStyle(textStyle) { alignment = TextAnchor.MiddleCenter };
        centered.normal.textColor = new Color(0.9f, 0.97f, 1f);
        GUI.Label(visual, text, centered);
        return GUI.Button(rect, GUIContent.none, GUIStyle.none);
    }

    private void OnDestroy()
    {
        if (climber != null) { climber.StepReached -= OnStepReached; climber.Failed -= OnClimberFailed; }
        if (uiFont != null) Destroy(uiFont);
        if (panelTexture != null) Destroy(panelTexture);
    }
}
