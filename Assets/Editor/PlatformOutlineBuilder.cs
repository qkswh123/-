using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

/// <summary>Adds only visual edges; it never changes platform colliders or scenery.</summary>
public static class PlatformOutlineBuilder
{
    private const string Folder = "Assets/Art/LevelBlocks/Outlines";

    [MenuItem("Penguin/Apply Clear Platform Edges")]
    public static void ApplyAll()
    {
        if(Application.isPlaying)throw new System.InvalidOperationException("Stop Play before saving platform outlines.");
        if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets/Art/LevelBlocks","Outlines");
        var shader=Shader.Find("Penguin/Platform Outline");
        if(shader==null || ShaderUtil.ShaderHasError(shader))throw new System.Exception("Outline shader is not ready.");
        var dark=OutlineMaterial("DarkEdge",shader,new Color(0.64f,0.67f,0.7f),0.035f,1999);
        var light=OutlineMaterial("LightSeparation",shader,new Color(0.94f,0.98f,1f),0.055f,1998);
        string rimPath=Folder+"/TopRim.mat";
        var rim=AssetDatabase.LoadAssetAtPath<Material>(rimPath);
        if(rim==null){rim=new Material(Shader.Find("Universal Render Pipeline/Unlit"));AssetDatabase.CreateAsset(rim,rimPath);}
        rim.SetColor("_BaseColor",new Color(0.68f,0.71f,0.74f));rim.renderQueue=2001;EditorUtility.SetDirty(rim);
        foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Prefabs/LevelBlocks"}))
        {
            string path=AssetDatabase.GUIDToAssetPath(guid);
            var root=PrefabUtility.LoadPrefabContents(path);
            var source=root.GetComponent<MeshFilter>().sharedMesh;
            string meshPath=Folder+"/"+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source))+".asset";
            var smooth=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if(smooth==null)
            {
                smooth=Object.Instantiate(source);smooth.name=source.name+" Edge Normals";
                var vertices=smooth.vertices;var normals=smooth.normals;var averages=new Dictionary<Vector3,Vector3>();
                for(int i=0;i<vertices.Length;i++){averages.TryGetValue(vertices[i],out var total);averages[vertices[i]]=total+normals[i];}
                for(int i=0;i<vertices.Length;i++)normals[i]=averages[vertices[i]].normalized;
                smooth.normals=normals;AssetDatabase.CreateAsset(smooth,meshPath);
            }
            AddShell(root.transform,"Platform Light Edge",smooth,light);
            AddShell(root.transform,"Platform Dark Edge",smooth,dark);
            var old=root.transform.Find("Platform Top Border");if(old!=null)Object.DestroyImmediate(old.gameObject);
            var edge=new GameObject("Platform Top Border");edge.transform.SetParent(root.transform,false);
            var line=edge.AddComponent<LineRenderer>();line.useWorldSpace=false;line.loop=true;line.widthMultiplier=0.018f;
            line.numCornerVertices=3;line.numCapVertices=2;line.sharedMaterial=rim;line.shadowCastingMode=ShadowCastingMode.Off;line.receiveShadows=false;
            var triangles=source.GetTriangles(0);var positions=source.vertices;var border=new List<Vector3>();
            for(int i=0;i<triangles.Length;i+=3)border.Add(positions[triangles[i+2]]+Vector3.up*0.008f);
            line.positionCount=border.Count;line.SetPositions(border.ToArray());
            PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);
        }
        for(int n=1;n<=20;n++)
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/Level"+n+".unity",OpenSceneMode.Single);
            var stairs=Object.FindAnyObjectByType<IceStaircase>();var level=Object.FindAnyObjectByType<ClimbLevel>();
            stairs.Rebuild(stairs.seed);level.Configure(level.settings,n,stairs,Camera.main);
            EditorUtility.SetDirty(stairs);EditorUtility.SetDirty(level);EditorUtility.SetDirty(level.Climber);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        AssetDatabase.SaveAssets();EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity",OpenSceneMode.Single);
    }

    private static Material OutlineMaterial(string name,Shader shader,Color color,float width,int queue)
    {
        string path=Folder+"/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material==null){material=new Material(shader);AssetDatabase.CreateAsset(material,path);}
        material.SetColor("_OutlineColor",color);material.SetFloat("_OutlineWidth",width);material.renderQueue=queue;
        EditorUtility.SetDirty(material);return material;
    }

    private static void AddShell(Transform parent,string name,Mesh mesh,Material material)
    {
        var old=parent.Find(name);if(old!=null)Object.DestroyImmediate(old.gameObject);
        var shell=new GameObject(name);shell.transform.SetParent(parent,false);shell.AddComponent<MeshFilter>().sharedMesh=mesh;
        var renderer=shell.AddComponent<MeshRenderer>();renderer.sharedMaterials=new[]{material,material,material};
        renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.lightProbeUsage=LightProbeUsage.Off;
    }
}
