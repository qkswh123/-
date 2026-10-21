using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;

public static class CoursePresentationBuilder
{
    private static void Folder(string parent,string name)
    {
        if(!AssetDatabase.IsValidFolder(parent+"/"+name))AssetDatabase.CreateFolder(parent,name);
    }

    [MenuItem("Penguin/Apply Readable Backgrounds and Level Themes")]
    public static void BuildAll()
    {
        if(Application.isPlaying)throw new System.InvalidOperationException("Stop Play before saving course presentation.");
        Folder("Assets/Art/LevelBlocks","IceFloe");
        Folder("Assets/Art/Environments","BackdropMaterials");
        var ice=CreateIceFloe();
        var variants=new List<GameObject>{ice};
        for(int n=1;n<=20;n++)variants.Add(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/LevelBlocks/Level"+n.ToString("D2")+"_Block.prefab"));
        var shader=Shader.Find("Penguin/Backdrop Depth");
        if(shader==null || ShaderUtil.ShaderHasError(shader))throw new System.Exception("Backdrop shader is not ready.");
        for(int level=1;level<=20;level++)
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/Level"+level+".unity",OpenSceneMode.Single);
            var stairs=Object.FindAnyObjectByType<IceStaircase>();
            var controller=Object.FindAnyObjectByType<ClimbLevel>();
            stairs.ConfigureBlockVariations(new GameObject[0],false,level>1);
            stairs.Configure(level==1?ice:variants[level],Camera.main);
            stairs.Rebuild(stairs.seed);
            controller.Configure(controller.settings,level,stairs,Camera.main);
            var env=Object.FindAnyObjectByType<LevelEnvironment>();
            var sky=env.transform.Find("3D Sky Dome").GetComponent<Renderer>().sharedMaterial;
            Color horizon=level==1?new Color(0.58f,0.69f,0.78f):Soften(sky.GetColor("_Horizon"),0.35f);
            horizon=Color.Lerp(horizon,new Color(0.52f,0.6f,0.69f),level==1?0f:0.3f);
            sky.SetColor("_Horizon",horizon);
            sky.SetColor("_Top",level==1?new Color(0.29f,0.46f,0.62f):Soften(sky.GetColor("_Top"),0.5f));
            sky.SetColor("_Bottom",Color.Lerp(horizon,new Color(0.4f,0.49f,0.59f),0.35f));
            EditorUtility.SetDirty(sky);
            if(!env.subduedBackdrop)
            {
                int island=0,mountain=0;
                foreach(var transform in env.GetComponentsInChildren<Transform>(true))
                {
                    if(transform.name=="Floating cliff island")
                    {
                        island++;
                        var p=transform.localPosition;p.x*=1.65f;p.z+=24f;transform.localPosition=p;
                        transform.localScale*=0.85f;
                        if(island%3==0)transform.gameObject.SetActive(false);
                    }
                    if(transform.name=="Distant mountain")
                    {
                        mountain++;
                        if(mountain%2==0)transform.gameObject.SetActive(false);
                        var size=transform.localScale;size.y*=0.7f;transform.localScale=size;
                    }
                    if(transform.name=="Snow peak")transform.gameObject.SetActive(false);
                    if(transform.name.Contains("Winding water"))transform.gameObject.SetActive(false);
                    if(transform.name=="Sculpted cloud")
                    {
                        var size=transform.localScale;size.x*=0.8f;size.y*=2;transform.localScale=size;
                        transform.localPosition+=Vector3.forward*25;
                    }
                    if(transform.name.Contains("sky arch")||transform.name.StartsWith("Rainbow arch")||transform.name=="Aurora ribbon")
                    {
                        transform.localPosition+=Vector3.forward*25;transform.localScale*=0.85f;
                    }
                }
                env.subduedBackdrop=true;
            }
            Folder("Assets/Art/Environments/BackdropMaterials","Level"+level);
            var replacements=new Dictionary<Material,Material>();
            foreach(var renderer in env.GetComponentsInChildren<Renderer>(true))
            {
                if(renderer.name=="3D Sky Dome")continue;
                var materials=renderer.sharedMaterials;
                for(int i=0;i<materials.Length;i++)
                {
                    var source=materials[i];if(source==null)continue;
                    if(source.shader==shader){source.SetColor("_HazeColor",horizon);EditorUtility.SetDirty(source);continue;}
                    if(!replacements.TryGetValue(source,out var replacement))
                    {
                        string sourcePath=AssetDatabase.GetAssetPath(source);
                        string path="Assets/Art/Environments/BackdropMaterials/Level"+level+"/"+AssetDatabase.AssetPathToGUID(sourcePath)+".mat";
                        replacement=AssetDatabase.LoadAssetAtPath<Material>(path);
                        if(replacement==null){replacement=new Material(shader);AssetDatabase.CreateAsset(replacement,path);}
                        replacement.SetTexture("_BaseMap",source.HasProperty("_BaseMap")?source.GetTexture("_BaseMap"):source.mainTexture);
                        Color tint=source.HasProperty("_BaseColor")?source.GetColor("_BaseColor"):Color.white;
                        if(level==1)
                        {
                            if(sourcePath.Contains("Palette1"))tint=new Color(0.7f,0.8f,0.86f);
                            if(sourcePath.Contains("Palette2"))tint=new Color(0.6f,0.72f,0.8f);
                            if(sourcePath.Contains("Palette0"))tint=new Color(0.4f,0.53f,0.63f);
                        }
                        replacement.SetColor("_BaseColor",tint);replacement.SetColor("_HazeColor",horizon);
                        replacement.SetFloat("_Saturation",0.18f);replacement.SetFloat("_HazeStart",20);replacement.SetFloat("_HazeEnd",135);
                        EditorUtility.SetDirty(replacement);replacements.Add(source,replacement);
                    }
                    materials[i]=replacement;
                }
                renderer.sharedMaterials=materials;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            }
            var fog=Object.FindAnyObjectByType<Level1Fog>();fog.density=0.1f;fog.fogColor=horizon;
            if(level==1)controller.environmentName="빙하 협곡";
            EditorUtility.SetDirty(fog);EditorUtility.SetDirty(env);EditorUtility.SetDirty(stairs);EditorUtility.SetDirty(controller);EditorUtility.SetDirty(controller.Climber);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            Debug.Log("Course presentation updated: Level "+level+(level==1?" ice floes":" unified themed blocks"));
        }
        AssetDatabase.SaveAssets();EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity",OpenSceneMode.Single);
    }

    private static Color Soften(Color color,float saturation)
    {
        float grey=color.r*0.2126f+color.g*0.7152f+color.b*0.0722f;
        return Color.Lerp(new Color(grey,grey,grey),color,saturation);
    }

    private static GameObject CreateIceFloe()
    {
        string path="Assets/Prefabs/LevelBlocks/Level01_IceFloe.prefab";
        var existing=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(existing!=null)return existing;
        string[] colors={"DFF8FF","3D96C8","F9FEFF"};var materials=new Material[3];
        for(int i=0;i<3;i++)
        {
            materials[i]=new Material(Shader.Find("Universal Render Pipeline/Lit"));ColorUtility.TryParseHtmlString("#"+colors[i],out var color);
            materials[i].SetColor("_BaseColor",color);materials[i].SetFloat("_Smoothness",i==1?0.65f:0.38f);
            AssetDatabase.CreateAsset(materials[i],"Assets/Art/LevelBlocks/IceFloe/Surface"+i+".mat");
        }
        var root=new GameObject("Ice Floe - Snow and Blue Ice");
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Art/LevelBlocks/Meshes/Block07.asset");
        root.AddComponent<MeshFilter>().sharedMesh=mesh;root.AddComponent<MeshRenderer>().sharedMaterials=materials;root.AddComponent<MeshCollider>().sharedMesh=mesh;
        var crackMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit"));crackMaterial.SetColor("_BaseColor",new Color(0.45f,0.75f,0.87f));
        AssetDatabase.CreateAsset(crackMaterial,"Assets/Art/LevelBlocks/IceFloe/Cracks.mat");
        var crack=new GameObject("Ice fracture");crack.transform.SetParent(root.transform,false);var line=crack.AddComponent<LineRenderer>();
        line.useWorldSpace=false;line.sharedMaterial=crackMaterial;line.widthMultiplier=0.012f;line.positionCount=4;
        line.SetPositions(new[]{new Vector3(-0.42f,0.007f,0.28f),new Vector3(-0.12f,0.007f,0.08f),new Vector3(0.05f,0.007f,0.14f),new Vector3(0.33f,0.007f,-0.18f)});
        line.shadowCastingMode=ShadowCastingMode.Off;line.receiveShadows=false;
        var prefab=PrefabUtility.SaveAsPrefabAsset(root,path);Object.DestroyImmediate(root);return prefab;
    }
}
