#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Exercise the actual randomized routes and obstacle cycle in every affected level.</summary>
public sealed class ClimbAllHazardsVerification : MonoBehaviour
{
    private readonly StringBuilder report = new StringBuilder();
    private static string ResultPath => Path.GetFullPath(Path.Combine(Application.dataPath, "../Previews/AllHazardsValidation.txt"));
    private float deadline;
    private bool finished;
    private void Check(bool value, string message) { if (!value) { File.WriteAllText(ResultPath, report + "FAIL: " + message); throw new System.InvalidOperationException(message); } }
    private IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject);
        deadline = Time.realtimeSinceStartup + 240;
        File.WriteAllText(ResultPath, "Running all affected levels...");
        for (int number = 1; number <= 20; number++)
        {
            yield return SceneManager.LoadSceneAsync("Level" + number);
            yield return null;
            var level = FindAnyObjectByType<ClimbLevel>();
            var actor = level.Climber;
            actor.AcceptKeyboardInput = false;
            var stairs = FindAnyObjectByType<IceStaircase>();
            int interval = Mathf.Max(8, 12 - (number - 1) / 4);
            var hazard = stairs.GetHazard(interval);
            Check(hazard != null && !hazard.IsDangerous, "Initial hazard missing or dangerous L" + number);
            Check(hazard.transform.Find("Permanent Light Halo") != null && hazard.transform.Find("Permanent Dark Halo") != null,
                "Missing permanent contrast halo L" + number);
            // Let an offscreen hazard wait longer than a complete cycle.
            Check(hazard.PhaseAtTime(Time.time + 100) == StepHazard.Phase.Safe, "Offscreen hazard already cycling L" + number);
            var camera = Camera.main;
            var view = camera.WorldToViewportPoint(hazard.transform.Find("Rotating Ice Mine").position);
            if (view.z > 0 && view.x > 0 && view.x < 1 && view.y > 0 && view.y < 1)
                Check(hazard.IsMarkerVisible, "Distant visible hazard has no marker L" + number);
            while (actor.CurrentStep < interval - 1)
            {
                stairs.TryGetNextStep(actor.CurrentStep, out _, out int direction);
                Check(actor.TryClimb(direction), "Climb rejected L" + number);
                while (actor.IsHopping) yield return null;
                Check(!actor.IsFalling, "Unexpected fall L" + number);
            }
            var body = hazard.transform.Find("Rotating Ice Mine");
            Check(body.localScale.x >= 0.9f && body.localPosition.y >= 0.5f, "Hidden body L" + number);
            float until = Time.time + 10;
            while (hazard.CurrentPhase != StepHazard.Phase.Warning && Time.time < until) yield return null;
            Check(hazard.CurrentPhase == StepHazard.Phase.Warning, "Preview never activated L" + number);
            float warningAt = Time.time;
            while (hazard.CurrentPhase == StepHazard.Phase.Warning)
            {
                Check(body.localScale.x >= 0.9f, "Body vanished L" + number);
                yield return null;
            }
            // If warning began during approach, observe a complete second cycle.
            if (Time.time - warningAt < 1.15f)
            {
                while (hazard.CurrentPhase != StepHazard.Phase.Warning) yield return null;
                warningAt = Time.time;
                while (hazard.CurrentPhase == StepHazard.Phase.Warning) yield return null;
            }
            Check(Time.time - warningAt + 2 * Time.deltaTime >= 1.2f, "Short warning L" + number);
            Check(!hazard.IsDangerous, "Damage before visual grace L" + number);
            yield return new WaitForSeconds(0.18f);
            Check(hazard.IsDangerous, "Red never dangerous L" + number);
            if (number == 3 || number == 7 || number == 12 || number == 20)
            {
                ScreenCapture.CaptureScreenshot(Path.GetFullPath(Path.Combine(Application.dataPath, "../Previews/Level" + number + "-Hazard.png")));
                yield return null;
            }
            while (hazard.CurrentPhase != StepHazard.Phase.Safe) yield return null;
            stairs.TryGetNextStep(actor.CurrentStep, out _, out int safeDirection);
            Check(actor.TryClimb(safeDirection), "Safe entry rejected L" + number);
            while (actor.IsHopping) yield return null;
            Check(!actor.IsFalling && actor.CurrentStep == interval, "Safe crossing failed L" + number);
            // Force real pool reuse, retaining the same geometry and fresh safe activation.
            actor.enabled = false;
            int recycledIndex = interval * 4;
            actor.transform.position = new Vector3(0, (recycledIndex - 1) * stairs.verticalStep, 0);
            stairs.MaintainForHeight(actor.transform.position.y);
            var recycled = stairs.GetHazard(recycledIndex);
            Check(recycled != null && !recycled.IsDangerous, "Recycled hazard starts dangerous L" + number);
            Check(recycled.transform.Find("Permanent Light Halo") != null, "Recycled halo missing L" + number);
            Check(recycled.transform.Find("Rotating Ice Mine").localScale.x >= 0.9f, "Recycled body hidden L" + number);
            report.AppendLine("PASS L" + number + ": approach, full warning, visible-red grace, safe landing, pooled reuse.");
            File.WriteAllText(ResultPath, report.ToString());
        }
        yield return SceneManager.LoadSceneAsync("Level1");
        finished = true;
        File.WriteAllText(ResultPath, report + "PASS: all 20 levels verified in Play mode, including permanent on-screen markers.");
        Destroy(gameObject);
    }
    private void Update()
    {
        if (!finished && deadline > 0 && Time.realtimeSinceStartup > deadline)
        {
            File.WriteAllText(ResultPath, report + "FAIL: verification timed out.");
            StopAllCoroutines(); Destroy(gameObject);
        }
    }
}
#endif
