#if UNITY_EDITOR
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Validate coin rewards through actual safe, failed, recycled and final landings.</summary>
public sealed class ClimbCoinVerification : MonoBehaviour
{
    private string path;
    private bool finished;
    private float deadline;
    private void Fail(string reason) { finished=true;File.WriteAllText(path,"FAIL: "+reason);Debug.LogError(reason);StopAllCoroutines();Destroy(gameObject); }
    private IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject);
        path=Path.GetFullPath(Path.Combine(Application.dataPath,"../Previews/CoinValidation.txt"));
        deadline=Time.realtimeSinceStartup+120;
        File.WriteAllText(path,"Running coin checks...");
        for(int number=1;number<=20;number++)
        {
            yield return SceneManager.LoadSceneAsync("Level"+number);yield return null;
            var level=FindAnyObjectByType<ClimbLevel>();var stairs=FindAnyObjectByType<IceStaircase>();var actor=level.Climber;
            actor.AcceptKeyboardInput=false;
            if(level.Points!=0||stairs.GetCoin(0).IsAvailable||!stairs.GetCoin(1).IsAvailable){Fail("Initial coins/points L"+number);yield break;}
            stairs.TryGetNextStep(0,out _,out int direction);actor.TryClimb(direction);
            while(actor.IsHopping)yield return null;
            if(level.Points!=10||stairs.GetCoin(1).IsAvailable||stairs.TryCollectCoin(1)){Fail("Landing or duplicate credit L"+number);yield break;}
            File.WriteAllText(path,"Verified coin landing in "+number+"/20 levels.");
        }
        yield return SceneManager.LoadSceneAsync("Level1");yield return null;
        var first=FindAnyObjectByType<ClimbLevel>();var route=FindAnyObjectByType<IceStaircase>();var player=first.Climber;player.AcceptKeyboardInput=false;
        route.TryGetNextStep(0,out _,out int correct);player.TryClimb(-correct);
        if(!player.IsFalling||first.Points!=0||!route.GetCoin(1).IsAvailable){Fail("Wrong jump awarded coin");yield break;}
        while(player!=null)yield return null;yield return null;
        first=FindAnyObjectByType<ClimbLevel>();route=FindAnyObjectByType<IceStaircase>();player=first.Climber;player.AcceptKeyboardInput=false;
        if(first.Points!=0){Fail("Retry score not zero");yield break;}
        while(player.CurrentStep<11){route.TryGetNextStep(player.CurrentStep,out _,out correct);player.TryClimb(correct);while(player.IsHopping)yield return null;}
        var hazard=route.GetHazard(12);float hop=first.settings.HopTimeForLevel(1);
        while(!hazard.IsDangerous||hazard.PhaseAtTime(Time.time+hop+0.1f)!=StepHazard.Phase.Dangerous)yield return null;
        route.TryGetNextStep(11,out _,out correct);player.TryClimb(correct);while(player.IsHopping)yield return null;
        if(!player.IsFalling||first.Points!=110||!route.GetCoin(12).IsAvailable){Fail("Hazard hit awarded coin");yield break;}
        while(player!=null)yield return null;yield return null;
        first=FindAnyObjectByType<ClimbLevel>();route=FindAnyObjectByType<IceStaircase>();player=first.Climber;player.AcceptKeyboardInput=false;
        while(player.CurrentStep<first.Goal)
        {
            int next=player.CurrentStep+1;hazard=route.GetHazard(next);
            while(hazard!=null&&(hazard.CurrentPhase!=StepHazard.Phase.Safe||hazard.PhaseAtTime(Time.time+hop+0.12f)!=StepHazard.Phase.Safe))yield return null;
            route.TryGetNextStep(player.CurrentStep,out _,out correct);player.TryClimb(correct);while(player.IsHopping)yield return null;
            if(player.IsFalling||first.Points!=player.CurrentStep*10){Fail("Pooled coin credit mismatch at "+next);yield break;}
            if(next==5){yield return new WaitForSeconds(0.1f);ScreenCapture.CaptureScreenshot(Path.GetFullPath(Path.Combine(Application.dataPath,"../Previews/CoinsAndPoints.png")));}
        }
        if(!first.Completed||first.Points!=500||route.GetCoin(51)?.IsAvailable==true){Fail("Goal coin mismatch");yield break;}
        yield return SceneManager.LoadSceneAsync("Level20");yield return null;
        if(FindAnyObjectByType<ClimbLevel>().Points!=0){Fail("New level score not zero");yield break;}
        yield return SceneManager.LoadSceneAsync("Level1");
        File.WriteAllText(path,"PASS: 3D coins on all 20 levels; 10 points per successful landing; duplicate collection blocked; no credit on wrong jumps or hazardous landings; retry/new level reset; pooled coins respawn correctly; Level1 final coin counted for 500 points.");
        finished=true;Destroy(gameObject);
    }
    private void Update(){if(!finished&&deadline>0&&Time.realtimeSinceStartup>deadline)Fail("Coin test exceeded 120 seconds");}
}
#endif
