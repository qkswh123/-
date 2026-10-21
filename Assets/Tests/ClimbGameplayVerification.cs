#if UNITY_EDITOR
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Editor-only smoke test. It is never added to saved game scenes.</summary>
public sealed class ClimbGameplayVerification : MonoBehaviour
{
    private float deadline;
    private bool finished;

    private IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject);
        deadline = Time.realtimeSinceStartup + 90f;
        File.WriteAllText(ResultPath, "Running gameplay checks...");
        for (int number = 1; number <= 20; number++)
        {
            yield return SceneManager.LoadSceneAsync(ClimbGameSettings.SceneName(number));
            yield return null;
            var level = FindAnyObjectByType<ClimbLevel>();
            var stairs = FindAnyObjectByType<IceStaircase>();
            var actor = level.Climber;
            actor.AcceptKeyboardInput = false;
            if (!stairs.TryGetNextStep(0, out var next, out int direction)) { Fail("Missing next step in " + number); yield break; }
            if (!actor.TryClimb(direction)) { Fail("Correct direction rejected in " + number); yield break; }
            while (actor.IsHopping) yield return null;
            if (actor.CurrentStep != 1 || Vector3.Distance(actor.transform.position, next) > 0.002f)
            { Fail("Landing mismatch in " + number); yield break; }
            File.WriteAllText(ResultPath, "Verified animal landing in " + number + "/20 levels.");
        }

        yield return SceneManager.LoadSceneAsync("Level1");
        yield return null;
        var first = FindAnyObjectByType<ClimbLevel>();
        first.Climber.AcceptKeyboardInput = false;
        var route = FindAnyObjectByType<IceStaircase>();
        while (first.Climber.CurrentStep < first.Goal)
        {
            route.TryGetNextStep(first.Climber.CurrentStep, out _, out int direction);
            var hazard = route.GetHazard(first.Climber.CurrentStep + 1);
            while (hazard != null && (hazard.CurrentPhase != StepHazard.Phase.Safe
                || hazard.PhaseAtTime(Time.time + first.settings.HopTimeForLevel(1) + 0.12f) != StepHazard.Phase.Safe))
                yield return null;
            if (!first.Climber.TryClimb(direction)) { Fail("Completion climb rejected"); yield break; }
            while (first.Climber.IsHopping) yield return null;
        }
        if (!first.Completed || first.Climber.InputEnabled) { Fail("Completion did not stop input"); yield break; }
        first.LoadLevel(2);
        while (SceneManager.GetActiveScene().name != "Level2") yield return null;
        yield return null;
        var second = FindAnyObjectByType<ClimbLevel>();
        second.LoadLevel(20);
        while (SceneManager.GetActiveScene().name != "Level20") yield return null;
        yield return null;
        FindAnyObjectByType<ClimbLevel>().LoadLevel(1);
        while (SceneManager.GetActiveScene().name != "Level1") yield return null;
        yield return null;
        finished = true;
        File.WriteAllText(ResultPath, "PASS: all 20 level scenes loaded in Play mode; all 20 animals landed on widened next blocks; Level1 goal completed with safe obstacle timing; input stopped at completion; menu loading to Level2 and Level20 succeeded; returned to Level1.");
        ScreenCapture.CaptureScreenshot(Path.GetFullPath(Path.Combine(Application.dataPath, "../Previews/Level1-Game.png")));
        Destroy(gameObject);
    }

    private static string ResultPath => Path.GetFullPath(Path.Combine(Application.dataPath, "../Previews/GameplayValidation.txt"));

    private void Update()
    {
        if (!finished && deadline > 0 && Time.realtimeSinceStartup > deadline)
            Fail("Gameplay check exceeded 90 seconds");
    }

    private void Fail(string reason)
    {
        finished = true;
        File.WriteAllText(ResultPath, "FAIL: " + reason);
        Debug.LogError(reason);
        StopAllCoroutines();
        Destroy(gameObject);
    }
}
#endif
