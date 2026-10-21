#if UNITY_EDITOR
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Editor-only integration check of falling, timed hazards and automatic restart.</summary>
public sealed class ClimbFailureVerification : MonoBehaviour
{
    private float deadline;
    private bool finished;
    private static string ResultPath => Path.GetFullPath(Path.Combine(Application.dataPath, "../Previews/FailureValidation.txt"));

    private IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject);
        deadline = Time.realtimeSinceStartup + 75f;
        File.WriteAllText(ResultPath, "Running fall and obstacle checks...");
        yield return SceneManager.LoadSceneAsync("Level1");
        yield return null;
        var level = FindAnyObjectByType<ClimbLevel>();
        var actor = level.Climber;
        actor.AcceptKeyboardInput = false;
        var stairs = FindAnyObjectByType<IceStaircase>();
        // Exercise the real prefab through a full cycle and pooled reactivation.
        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/TimedStepHazard.prefab");
        var probeObject = Instantiate(prefab, new Vector3(1000, 0, 1000), Quaternion.identity);
        var probe = probeObject.GetComponent<StepHazard>();
        probe.Configure(12, 1);
        var body = probeObject.transform.Find("Rotating Ice Mine");
        if (probe.IsDangerous || body.localScale.x < 0.9f || body.localPosition.y < 0.5f)
        { Fail("New obstacle is hidden or immediately dangerous"); yield break; }
        while (probe.CurrentPhase != StepHazard.Phase.Warning) yield return null;
        float warningAt = Time.time;
        while (probe.CurrentPhase == StepHazard.Phase.Warning)
        {
            if (body.localScale.x < 0.9f) { Fail("Obstacle shrank during warning"); yield break; }
            yield return null;
        }
        if (Time.time - warningAt + 2 * Time.deltaTime < 1.2f || probe.IsDangerous)
        { Fail("Insufficient warning or damage before visible-red grace"); yield break; }
        yield return new WaitForSeconds(0.18f);
        if (!probe.IsDangerous) { Fail("Visible red obstacle never became dangerous"); yield break; }
        probeObject.SetActive(false);
        probe.Configure(24, 20);
        probeObject.SetActive(true);
        if (probe.IsDangerous || probe.CurrentPhase != StepHazard.Phase.Safe || body.localScale.x < 0.9f)
        { Fail("Recycled obstacle popped in as dangerous or hidden"); yield break; }
        Destroy(probeObject);
        stairs.TryGetNextStep(0, out _, out int correct);
        actor.TryClimb(-correct);
        if (!actor.IsFalling || !level.HasFailed || actor.InputEnabled) { Fail("Wrong jump did not enter failure state"); yield break; }
        float cameraY = Camera.main.transform.position.y;
        yield return new WaitForSecondsRealtime(0.3f);
        ScreenCapture.CaptureScreenshot(Path.GetFullPath(Path.Combine(Application.dataPath, "../Previews/FallAndRetry.png")));
        yield return new WaitForSecondsRealtime(0.75f);
        if (actor.transform.position.y > -1f || Mathf.Abs(Camera.main.transform.position.y-cameraY)>0.05f)
        { Fail("Fall motion or camera hold is incorrect"); yield break; }
        while (actor != null) yield return null;
        yield return null;
        level = FindAnyObjectByType<ClimbLevel>(); actor = level.Climber;
        if (actor.CurrentStep != 0 || level.HasFailed || !actor.InputEnabled) { Fail("Automatic restart did not reset player"); yield break; }
        actor.AcceptKeyboardInput = false;
        stairs = FindAnyObjectByType<IceStaircase>();
        yield return ClimbTo(actor, stairs, 11);
        if (finished) yield break;
        var hazard = stairs.GetHazard(12);
        if (hazard == null) { Fail("First obstacle missing at step12"); yield break; }
        float duration = level.settings.HopTimeForLevel(1);
        while (!hazard.IsDangerous || hazard.PhaseAtTime(Time.time+duration+0.1f)!=StepHazard.Phase.Dangerous) yield return null;
        ScreenCapture.CaptureScreenshot(Path.GetFullPath(Path.Combine(Application.dataPath, "../Previews/ObstacleDanger.png")));
        stairs.TryGetNextStep(11,out _,out correct); actor.TryClimb(correct);
        while (actor.IsHopping) yield return null;
        if (!actor.IsFalling || !level.HasFailed || actor.CurrentStep != 11) { Fail("Active obstacle did not cause failure"); yield break; }
        while (actor != null) yield return null;
        yield return null;
        level=FindAnyObjectByType<ClimbLevel>();actor=level.Climber;actor.AcceptKeyboardInput=false;stairs=FindAnyObjectByType<IceStaircase>();
        yield return ClimbTo(actor,stairs,11);
        if(finished)yield break;
        hazard=stairs.GetHazard(12);
        while(hazard.CurrentPhase!=StepHazard.Phase.Safe || hazard.PhaseAtTime(Time.time+duration+0.12f)!=StepHazard.Phase.Safe)yield return null;
        stairs.TryGetNextStep(11,out var landing,out correct);actor.TryClimb(correct);
        while(actor.IsHopping)yield return null;
        if(actor.IsFalling || actor.CurrentStep!=12 || Vector3.Distance(actor.transform.position,landing)>0.002f){Fail("Safe obstacle could not be crossed");yield break;}
        yield return SceneManager.LoadSceneAsync("Level1");
        yield return null;
        finished=true;
        File.WriteAllText(ResultPath,"PASS: obstacle stays visible at full size; 1.2-second warning; damage waits for visible red plus reaction grace; recycled obstacles restart safe; wrong-direction fall; automatic same-level restart; active obstacle failure; inactive obstacle crossing; returned to Level1.");
        Destroy(gameObject);
    }

    private IEnumerator ClimbTo(AnimalClimber actor, IceStaircase stairs, int target)
    {
        while(actor.CurrentStep<target)
        {
            stairs.TryGetNextStep(actor.CurrentStep,out _,out int direction);
            if(!actor.TryClimb(direction)){Fail("Safe route input rejected");yield break;}
            while(actor.IsHopping)yield return null;
            if(actor.IsFalling){Fail("Unexpected failure on safe route");yield break;}
        }
    }
    private void Update(){if(!finished && deadline>0 && Time.realtimeSinceStartup>deadline)Fail("Failure check exceeded 75 seconds");}
    private void Fail(string reason){finished=true;File.WriteAllText(ResultPath,"FAIL: "+reason);Debug.LogError(reason);StopAllCoroutines();Destroy(gameObject);}
}
#endif
