using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Apply permanent high-contrast silhouettes to saved and pooled hazards.</summary>
public static class HazardPresentationBuilder
{
    private static LineRenderer Halo(StepHazard hazard, string name, bool light)
    {
        var child=hazard.transform.Find(name);
        if(child==null){child=new GameObject(name).transform;child.SetParent(hazard.transform,false);}
        var line=child.GetComponent<LineRenderer>();if(line==null)line=child.gameObject.AddComponent<LineRenderer>();
        string path="Assets/Art/Obstacles/"+(light?"HazardLightHalo":"HazardDarkHalo")+".mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));material.SetColor("_BaseColor",light?Color.white:new Color(0.12f,0.16f,0.22f));AssetDatabase.CreateAsset(material,path);}
        line.sharedMaterial=material;line.useWorldSpace=false;line.loop=true;line.positionCount=32;
        line.widthMultiplier=light?0.085f:0.03f;line.numCornerVertices=3;line.numCapVertices=3;
        line.shadowCastingMode=ShadowCastingMode.Off;line.receiveShadows=false;
        var camera=Camera.main;var right=camera!=null?camera.transform.right:Vector3.right;var up=camera!=null?camera.transform.up:Vector3.up;
        for(int i=0;i<32;i++){float a=i*Mathf.PI*2/32;line.SetPosition(i,new Vector3(0.22f,0.72f,-0.4f)+(right*Mathf.Cos(a)+up*Mathf.Sin(a))*0.46f);}
        return line;
    }
    private static void Apply(StepHazard hazard)
    {
        hazard.SetHalos(Halo(hazard,"Permanent Light Halo",true),Halo(hazard,"Permanent Dark Halo",false));
        hazard.Configure(hazard.StepIndex,1);
        EditorUtility.SetDirty(hazard);
    }
    [MenuItem("Penguin/Fix Hazard Presentation All Levels")]
    public static void ApplyAll()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Stop Play first.");
        var root=PrefabUtility.LoadPrefabContents("Assets/Prefabs/TimedStepHazard.prefab");
        try{Apply(root.GetComponent<StepHazard>());PrefabUtility.SaveAsPrefabAsset(root,"Assets/Prefabs/TimedStepHazard.prefab");}
        finally{PrefabUtility.UnloadPrefabContents(root);}
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/TimedStepHazard.prefab");
        for(int n=1;n<=20;n++)
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/Level"+n+".unity");
            var stairs=UnityEngine.Object.FindAnyObjectByType<IceStaircase>();
            foreach(var hazard in stairs.GetComponentsInChildren<StepHazard>(true))Apply(hazard);
            stairs.ConfigureObstacles(prefab,n,n*50);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        AssetDatabase.SaveAssets();EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");
    }
}
