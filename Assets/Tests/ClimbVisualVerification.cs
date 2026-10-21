#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Capture real idle, mid-hop and landed frames for visual inspection.</summary>
public sealed class ClimbVisualVerification : MonoBehaviour
{
    private readonly StringBuilder report = new StringBuilder();
    private string folder;
    private IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject);
        folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../Previews/CharacterAudit"));
        Directory.CreateDirectory(folder);
        for (int number = 1; number <= 20; number++)
        {
            yield return SceneManager.LoadSceneAsync("Level" + number);
            yield return null;
            var level = FindAnyObjectByType<ClimbLevel>();
            var actor = level.Climber;
            actor.AcceptKeyboardInput = false;
            yield return new WaitForSeconds(0.5f);
            Capture(actor, number, "Idle");
            var stairs = FindAnyObjectByType<IceStaircase>();
            stairs.TryGetNextStep(0, out _, out int direction);
            actor.TryClimb(direction);
            yield return new WaitForSeconds(level.settings.HopTimeForLevel(number) * 0.5f);
            Capture(actor, number, "Jump");
            while (actor.IsHopping) yield return null;
            yield return new WaitForSeconds(0.4f);
            Capture(actor, number, "Landed");
            if (actor.CurrentStep != 1 || actor.IsFalling) { File.WriteAllText(Path.Combine(folder,"Result.txt"),"FAIL landing L"+number); Destroy(gameObject); yield break; }
            File.WriteAllText(Path.Combine(folder,"Result.txt"), report.ToString());
        }
        report.AppendLine("PASS: all 20 idle, mid-hop and landed poses captured; all landed correctly.");
        File.WriteAllText(Path.Combine(folder,"Result.txt"), report.ToString());
        yield return SceneManager.LoadSceneAsync("Level20");
        Destroy(gameObject);
    }
    private void Capture(AnimalClimber actor, int number, string pose)
    {
        var camera = Camera.main;
        var previous = camera.targetTexture;
        var active = RenderTexture.active;
        var target = RenderTexture.GetTemporary(960,540,24);
        camera.targetTexture = target;
        camera.Render();
        RenderTexture.active = target;
        var point = camera.WorldToViewportPoint(actor.transform.position + Vector3.up * 0.55f);
        int x = Mathf.Clamp(Mathf.RoundToInt(point.x*960)-80,0,800);
        int y = Mathf.Clamp(Mathf.RoundToInt(point.y*540)-65,0,410);
        var texture = new Texture2D(160,130,TextureFormat.RGB24,false);
        texture.ReadPixels(new Rect(x,y,160,130),0,0);
        texture.Apply();
        File.WriteAllBytes(Path.Combine(folder,number.ToString("D2")+"_"+pose+".png"),texture.EncodeToPNG());
        Destroy(texture);
        camera.targetTexture = previous;
        RenderTexture.active = active;
        RenderTexture.ReleaseTemporary(target);
        report.AppendLine("L" + number + " " + pose + " frame captured; root=" + actor.transform.position.ToString("F3"));
    }
}
#endif
