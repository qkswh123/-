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
        if (uiFont == null) uiFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Arial" }, 18);
        var oldMatrix = GUI.matrix;
        var oldFont = GUI.skin.font;
        GUI.skin.font = uiFont;
        float scale = Mathf.Min(Screen.width / 960f, Screen.height / 540f);
        GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 960f * scale) * 0.5f,
            (Screen.height - 540f * scale) * 0.5f, 0), Quaternion.identity, Vector3.one * scale);
        var title = new GUIStyle(GUI.skin.label) { fontSize = 21, fontStyle = FontStyle.Bold };
        var label = new GUIStyle(GUI.skin.label) { fontSize = 16 };
        var button = new GUIStyle(GUI.skin.button) { fontSize = 16 };
        var animal = settings.ResolveAnimal(levelNumber, out bool temporary);
        GUI.Box(new Rect(14, 12, 425, 98), "");
        GUI.Label(new Rect(26, 19, 405, 29), "레벨 " + levelNumber + " / 20  ·  " + environmentName, title);
        GUI.Label(new Rect(26, 50, 405, 25), animal?.displayName + "  ·  등반 " + climber.CurrentStep + " / " + Goal + "    ← / →", label);
        GUI.Label(new Rect(26, 77, 365, 26), temporary ? "추가 동물 등록 전: 임시 동물 배정" : "다음 계단이 있는 방향으로 눌러주세요", label);
        var pointBackground = GUI.color;
        GUI.color = new Color(0.06f, 0.1f, 0.15f, 0.96f);
        GUI.DrawTexture(new Rect(455, 12, 225, 72), Texture2D.whiteTexture);
        GUI.color = pointBackground;
        GUI.Box(new Rect(455, 12, 225, 72), "");
        var pointStyle = new GUIStyle(title);
        pointStyle.normal.textColor = new Color(1f, 0.86f, 0.3f);
        GUI.Label(new Rect(470, 20, 200, 30), "코인 포인트  " + points, pointStyle);
        var rewardStyle = new GUIStyle(label);
        rewardStyle.normal.textColor = new Color(1f, 0.94f, 0.65f);
        GUI.Label(new Rect(470, 51, 200, 25), Time.unscaledTime - lastCoinAt < 0.7f ? "+10 포인트!" : "코인 1개 = 10포인트", rewardStyle);
        if (GUI.Button(new Rect(700, 18, 112, 36), "레벨 선택", button)) choosingLevel = !choosingLevel;
        if (GUI.Button(new Rect(824, 18, 122, 36), "다시 시작", button)) LoadLevel(levelNumber);
        GUI.Box(new Rect(180, 496, 600, 32), "");
        GUI.Label(new Rect(193, 500, 580, 27), "장애물  ·  하늘색: 안전  /  노랑: 곧 켜짐  /  빨강: 위험", label);
        if (failed)
        {
            var previousColor = GUI.color;
            GUI.color = new Color(0.35f, 0.03f, 0.08f, 0.18f);
            GUI.DrawTexture(new Rect(0, 0, 960, 540), Texture2D.whiteTexture);
            GUI.color = previousColor;
            GUI.Box(new Rect(280, 174, 410, 194), "");
            GUI.Label(new Rect(304, 190, 365, 34), "앗! 다시 도전해요", title);
            GUI.Label(new Rect(304, 231, 365, 28), failureReason, label);
            GUI.Label(new Rect(304, 263, 365, 28), Mathf.Max(1, Mathf.CeilToInt(restartAt - Time.unscaledTime)) + "초 후 현재 레벨을 다시 시작합니다", label);
            if (GUI.Button(new Rect(325, 307, 320, 43), "지금 다시 시작  (R)", button)) LoadLevel(levelNumber);
        }
        else if (choosingLevel)
        {
            climber.InputEnabled = false;
            GUI.Box(new Rect(230, 125, 500, 305), "");
            GUI.Label(new Rect(252, 139, 420, 32), "레벨 선택", title);
            for (int i = 1; i <= 20; i++)
            {
                int column = (i - 1) % 5, row = (i - 1) / 5;
                if (GUI.Button(new Rect(252 + column * 94, 183 + row * 55, 82, 43), i.ToString(), button)) LoadLevel(i);
            }
            if (GUI.Button(new Rect(605, 382, 100, 32), "닫기", button)) choosingLevel = false;
        }
        else if (completed)
        {
            GUI.Box(new Rect(285, 190, 390, 155), "");
            GUI.Label(new Rect(307, 208, 350, 35), levelNumber == 20 ? "모든 레벨 완료!" : "레벨 " + levelNumber + " 완료!", title);
            if (GUI.Button(new Rect(325, 267, 310, 48), levelNumber == 20 ? "레벨 1부터 다시 시작" : "다음 레벨  (N)", button))
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

    private void OnDestroy()
    {
        if (climber != null) { climber.StepReached -= OnStepReached; climber.Failed -= OnClimberFailed; }
        if (uiFont != null) Destroy(uiFont);
    }
}
