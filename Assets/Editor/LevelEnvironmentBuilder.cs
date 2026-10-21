using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Rebuild the generated scenery without changing imported Asset Store files.</summary>
public static class LevelEnvironmentBuilder
{
    private const string Root = "Assets/Art/Environments";
    private static readonly string[] Colors = {
        "5F7E8C 6AAF61 2E8750 7B563C 83D2DC 3EB5D4 F4F8E8 FFDE8A",
        "7994AF E7F3FF 769FA8 6D879E A2D9EF 65BAD7 F3FAFF C4EEFF",
        "3D8BAB A7E4F0 70C9E2 436D99 83E9FA 40ADD4 F0FBFF A7F8FF",
        "362849 746293 BA81E5 503867 DF9AFF 7B50C2 BBA5E0 F0B4FF",
        "9977A0 8EC5A2 F3A5C5 925570 F9D3DD 76C9E1 FFF1F5 FFD6A8",
        "B66C4C EEC38C 4D9570 8D6550 FFC36B 64C7BB F8DCA8 FFDD88",
        "3D3545 6E474A CD6A4C 3E2D39 FF863F FF4B23 8F7A7D FFB442",
        "55456F 75658E A986E5 463955 CA9FFE 6A6EDB CABDE3 82E8FF",
        "9D8762 C4B27D 689F87 79634C E9BC57 6ABBB9 F1E1B9 FFE8A0",
        "687994 D7E7F4 799AB1 58647C A7D8EE 76AECB EDF7FF FFE5B4",
        "273C50 3C6260 477965 324357 8AAFD2 3F718E 92ABC0 B7F3C8",
        "446C92 BEDBEF 57D6AE 3F7392 7CF2D1 43AACF ECFAFF A9FFD3",
        "3E7F8E 8DD5CC EE9FAB 496F89 BFA0F1 349AB8 B5EDE4 A6F3F9",
        "9AA7BD E7EAF0 A8C4D8 A1A9B4 E6C575 8CC5E7 FBFAF3 FFF1C2",
        "263047 414B69 68DDE8 343551 EC78DE 564DAD A5B5DD 97F9F4",
        "8F7362 B8AA60 D77C49 795140 E8B550 6795A2 F2D7A8 FFE0A0",
        "3B4A61 71839B 749AB7 465370 9EB9D3 456985 C0D3DF DAEFFF",
        "687080 9FA7B4 8494A7 596478 B2BBD0 7B88A5 DDE4F0 BBD8FC",
        "B7A17E E4D0A7 D3B780 B69B6C F7D475 A0CDD9 FFF4DB FFE9A0",
        "849DB6 C4E5E7 F39CBE 7BAFCB FFD878 8DCCF4 EEF8FF C6B2F2"
    };
    private static readonly string[] Skies = {
        "227AC2 B4E3EB 6398B2", "467EB4 C9E3F1 9ABACD", "216B9C A6DDEB 578DA6", "241D43 766394 3D315B",
        "9498D4 F6D6E3 B6A6CA", "A75C79 F4BC90 B97F6A", "382B41 B76652 613F48", "14192F 5D547D 302943",
        "5F9EA5 E5D3A2 A7AE8F", "4D658B C5D4EC 8798B3", "111D35 526A8B 2D4055", "102443 577E9D 2D5278",
        "145779 71C4C9 2A7B93", "6294CC E7EBF4 A5C3D6", "11182E 424E77 252D50", "697E9F E4C49C A58A78",
        "232E49 8292AB 4F607B", "090F22 475270 252D47", "9286B7 F6D7AC BDB0B9", "648DCC E6D9EE A7C6DF"
    };

    private static Color Hex(string value) { ColorUtility.TryParseHtmlString("#" + value, out var color); return color; }
    private static void Folder(string parent, string name)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + name)) AssetDatabase.CreateFolder(parent, name);
    }

    [MenuItem("Penguin/Rebuild 20 Level Environments")]
    public static void BuildAll()
    {
        if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play before rebuilding scenery.");
        Folder("Assets/Art", "Environments"); Folder(Root, "Meshes"); Folder(Root, "Materials"); Folder(Root, "Prefabs");
        Mesh[] meshes = CreateMeshes();
        var pine = ConvertPrefab("Assets/AurynSky/WinterArena/Prefabs/Trees/WinterTreePineTall.prefab", "SnowPine", 5.5f);
        var tower = ConvertPrefab("Assets/AurynSky/WinterArena/Prefabs/BLUE Prefabs/WinterTower_v2_Blue.prefab", "WinterTower", 7f);
        for (int level = 1; level <= 20; level++)
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Level" + level + ".unity", OpenSceneMode.Single);
            var old = Object.FindAnyObjectByType<LevelEnvironment>();
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var photo = GameObject.Find("Level1 Background");
            if (photo != null) Object.DestroyImmediate(photo);
            Folder(Root + "/Materials", "Level" + level);
            string folder = Root + "/Materials/Level" + level;
            string[] colors = Colors[level - 1].Split(' ');
            var palette = new Material[8];
            for (int i = 0; i < palette.Length; i++)
            {
                string path = folder + "/Palette" + i + ".mat";
                palette[i] = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (palette[i] == null)
                {
                    palette[i] = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    AssetDatabase.CreateAsset(palette[i], path);
                }
                palette[i].SetColor("_BaseColor", Hex(colors[i]));
                palette[i].SetFloat("_Smoothness", i == 5 ? 0.65f : 0.14f);
                if (i == 7 || ((level == 4 || level == 12 || level == 15) && i == 4))
                {
                    palette[i].EnableKeyword("_EMISSION");
                    palette[i].SetColor("_EmissionColor", Hex(colors[i]) * 0.7f);
                }
                EditorUtility.SetDirty(palette[i]);
            }
            string skyPath = folder + "/Sky.mat";
            var sky = AssetDatabase.LoadAssetAtPath<Material>(skyPath);
            if (sky == null) { sky = new Material(Shader.Find("Penguin/Level Sky")); AssetDatabase.CreateAsset(sky, skyPath); }
            string[] skyColors = Skies[level - 1].Split(' ');
            sky.SetColor("_Top", Hex(skyColors[0])); sky.SetColor("_Horizon", Hex(skyColors[1])); sky.SetColor("_Bottom", Hex(skyColors[2]));
            bool night = level == 8 || level == 11 || level == 12 || level == 15 || level == 18;
            sky.SetFloat("_Stars", night ? 1f : 0f);
            sky.SetColor("_SunColor", night ? new Color(0.5f, 0.7f, 1) : new Color(1, 0.87f, 0.65f));
            EditorUtility.SetDirty(sky);
            var camera = Camera.main;
            var root = new GameObject("3D Environment - " + LevelEnvironment.ThemeNames[level - 1]);
            var environment = root.AddComponent<LevelEnvironment>();
            environment.Configure(level, camera, meshes, palette, sky, pine, tower);
            var fog = Object.FindAnyObjectByType<Level1Fog>();
            if (fog != null)
            {
                fog.density = night ? 0.18f : 0.27f;
                fog.fogColor = Color.Lerp(Hex(skyColors[1]), Hex(colors[6]), 0.35f);
                EditorUtility.SetDirty(fog);
            }
            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (light.type == LightType.Directional)
                {
                    light.color = night ? new Color(0.7f, 0.8f, 1) : (level == 6 || level == 7 || level == 16 ? new Color(1, 0.82f, 0.65f) : new Color(1, 0.97f, 0.9f));
                    light.intensity = night ? 1.1f : 1.5f;
                    light.transform.rotation = Quaternion.Euler(40, -35, 0);
                    EditorUtility.SetDirty(light);
                }
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Color.Lerp(Hex(skyColors[1]), Color.white, night ? 0.15f : 0.4f);
            RenderSettings.fog = false;
            var controller = Object.FindAnyObjectByType<ClimbLevel>();
            if (controller != null) { controller.environmentName = environment.ThemeName; EditorUtility.SetDirty(controller); }
            EditorUtility.SetDirty(environment);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Debug.Log("Built 3D scenery: Level " + level + " / " + environment.ThemeName);
        }
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity", OpenSceneMode.Single);
    }

    private static GameObject ConvertPrefab(string sourcePath, string name, float height)
    {
        string path = Root + "/Prefabs/" + name + ".prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
        if (source == null) return null;
        var root = new GameObject(name);
        var model = (GameObject)PrefabUtility.InstantiatePrefab(source);
        model.transform.SetParent(root.transform, false); model.transform.localPosition = Vector3.zero;
        var renderers = model.GetComponentsInChildren<Renderer>();
        var bounds = renderers[0].bounds;
        foreach (var renderer in renderers)
        {
            bounds.Encapsulate(renderer.bounds);
            var materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                var sourceMat = materials[i];
                string materialPath = Root + "/Materials/" + AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(sourceMat)) + "_URP.mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (mat == null)
                {
                    mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    mat.SetTexture("_BaseMap", sourceMat.mainTexture); mat.SetColor("_BaseColor", Color.white); mat.SetFloat("_Smoothness", 0.15f);
                    AssetDatabase.CreateAsset(mat, materialPath);
                }
                materials[i] = mat;
            }
            renderer.sharedMaterials = materials;
        }
        model.transform.localScale *= height / Mathf.Max(0.01f, bounds.size.y);
        bounds = renderers[0].bounds; foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        model.transform.position -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        foreach (var collider in root.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(collider);
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path); Object.DestroyImmediate(root); return prefab;
    }

    private static Mesh[] CreateMeshes()
    {
        var list = new Mesh[8];
        string[] names = { "Cube", "Cylinder", "Cone", "Crystal", "FacetedSphere", "Ring", "Arch", "Mountain" };
        for (int index = 0; index < list.Length; index++)
        {
            string path = Root + "/Meshes/" + names[index] + ".asset";
            list[index] = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (list[index] != null) continue;
            var vertices = new List<Vector3>(); var triangles = new List<int>();
            void Triangle(Vector3 a, Vector3 b, Vector3 c)
            {
                int start = vertices.Count; vertices.Add(a); vertices.Add(b); vertices.Add(c);
                triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2);
            }
            if (index == 0)
            {
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                list[index] = Object.Instantiate(cube.GetComponent<MeshFilter>().sharedMesh);
                Object.DestroyImmediate(cube);
            }
            else if (index == 4)
            {
                Vector3[] v = { Vector3.up, Vector3.down, Vector3.right, Vector3.left, Vector3.forward, Vector3.back };
                int[] t = { 0,4,2,0,3,4,0,5,3,0,2,5,1,2,4,1,4,3,1,3,5,1,5,2 };
                void Subdivide(Vector3 a, Vector3 b, Vector3 c, int depth)
                {
                    if (depth == 0) { Triangle(a,b,c); return; }
                    var ab=(a+b).normalized; var bc=(b+c).normalized; var ca=(c+a).normalized;
                    Subdivide(a,ab,ca,depth-1);Subdivide(ab,b,bc,depth-1);Subdivide(ca,bc,c,depth-1);Subdivide(ab,bc,ca,depth-1);
                }
                for(int i=0;i<t.Length;i+=3) Subdivide(v[t[i]],v[t[i+1]],v[t[i+2]],2);
            }
            else if (index == 5 || index == 6)
            {
                int segments=40, cross=6; float arc=index==5?Mathf.PI*2:Mathf.PI;
                Vector3 Point(int s,int c)
                {
                    float a=s/(float)segments*arc, b=c/(float)cross*Mathf.PI*2;
                    return new Vector3(Mathf.Cos(a)*(1+Mathf.Cos(b)*0.045f),Mathf.Sin(a)*(1+Mathf.Cos(b)*0.045f),Mathf.Sin(b)*0.045f);
                }
                for(int s=0;s<segments;s++)for(int c=0;c<cross;c++)
                { var a=Point(s,c);var b=Point(s+1,c);var d=Point(s,c+1);var e=Point(s+1,c+1);Triangle(a,b,d);Triangle(b,e,d); }
            }
            else
            {
                int sides=index==1?12:7;
                for(int i=0;i<sides;i++)
                {
                    float a=i/(float)sides*Mathf.PI*2,b=(i+1)/(float)sides*Mathf.PI*2;
                    float r1=index==7?0.8f+0.2f*Mathf.Sin(i*2.7f):1f;
                    float r2=index==7?0.8f+0.2f*Mathf.Sin((i+1)*2.7f):1f;
                    var p=new Vector3(Mathf.Cos(a)*r1,0,Mathf.Sin(a)*r1);
                    var q=new Vector3(Mathf.Cos(b)*r2,0,Mathf.Sin(b)*r2);
                    if(index==1)
                    {
                        Triangle(p,p+Vector3.up,q);Triangle(q,p+Vector3.up,q+Vector3.up);
                        Triangle(Vector3.zero,p,q);Triangle(Vector3.up,q+Vector3.up,p+Vector3.up);
                    }
                    else if(index==3)
                    {
                        p.y=0.4f;q.y=0.4f;Triangle(p,Vector3.up,q);Triangle(Vector3.zero,p,q);
                    }
                    else { Triangle(p,Vector3.up+new Vector3(index==7?0.12f:0,0,0),q);Triangle(Vector3.zero,p,q); }
                }
            }
            if(list[index]==null)
            {
                list[index]=new Mesh();list[index].SetVertices(vertices);list[index].SetTriangles(triangles,0);list[index].RecalculateNormals();list[index].RecalculateBounds();
            }
            list[index].name=names[index];AssetDatabase.CreateAsset(list[index],path);
        }
        return list;
    }
}
