#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Load each assigned character, inspect its actual renderers and perform a landing.</summary>
public sealed class ClimbRosterVerification : MonoBehaviour
{
    private static string ResultPath => Path.GetFullPath(Path.Combine(Application.dataPath, "../Previews/RosterValidation.txt"));
    private readonly HashSet<GameObject> unique = new HashSet<GameObject>();
    private bool failed;
    private void Fail(string message) { failed = true; File.WriteAllText(ResultPath, "FAIL: " + message); Debug.LogError(message); Destroy(gameObject); }
    private IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject);
        File.WriteAllText(ResultPath, "Running roster verification...");
        for (int number = 1; number <= 20; number++)
        {
            yield return SceneManager.LoadSceneAsync("Level" + number);
            yield return null;
            var level = FindAnyObjectByType<ClimbLevel>();
            var animal = level.settings.ResolveAnimal(number, out bool temporary);
            if (animal == null || (!temporary && !unique.Add(animal.prefab))) { Fail("Missing or duplicate character L" + number); yield break; }
            if (number < 20 && temporary) { Fail("Unexpected placeholder L" + number); yield break; }
            var actor = level.Climber;
            actor.AcceptKeyboardInput = false;
            var renderers = actor.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) { Fail("Missing renderers L" + number); yield break; }
            foreach (var renderer in renderers)
                foreach (var material in renderer.sharedMaterials)
                    if (material == null || !material.shader.isSupported || material.shader.name.Contains("InternalError"))
                    { Fail("Unsupported material L" + number); yield break; }
            var stairs = FindAnyObjectByType<IceStaircase>();
            stairs.TryGetNextStep(0, out var landing, out int direction);
            if (!actor.TryClimb(direction)) { Fail("Rejected input L" + number); yield break; }
            while (actor.IsHopping) yield return null;
            if (actor.IsFalling || actor.CurrentStep != 1 || Vector3.Distance(actor.transform.position, landing) > 0.002f)
            { Fail("Landing failed L" + number); yield break; }
            if (number == 7 || number == 9 || number == 19 || number == 20)
            {
                yield return new WaitForSeconds(0.2f);
                ScreenCapture.CaptureScreenshot(Path.GetFullPath(Path.Combine(Application.dataPath, "../Previews/Level" + number + "-Character.png")));
                yield return null;
            }
            File.WriteAllText(ResultPath, "Verified character rendering and landing: " + number + "/20; unique final assignments=" + unique.Count);
        }
        if (!failed) File.WriteAllText(ResultPath, "PASS: all 20 scenes loaded; " + unique.Count + " distinct final character prefabs; supported materials; all 20 characters landed correctly.");
        yield return SceneManager.LoadSceneAsync("Level7");
        Destroy(gameObject);
    }
}
#endif
