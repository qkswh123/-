using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>One beveled platform silhouette and color palette for every level.</summary>
public static class ClimbBlockBuilder
{
    public static readonly string[] Names = {
        "둥근 잔디 사각형", "눈꽃 육각형", "빙하 마름모", "수정 오각형", "벚꽃 꽃잎",
        "모래 방패", "용암 바위", "운석 오각석", "황금 팔각형", "성채 톱니",
        "밤하늘 별", "얼음 눈송이", "산호 물결", "구름 발판", "네온 절삭판",
        "단풍잎", "번개 바위", "달빛 원반", "황금 왕관", "분홍 하트"
    };
    private static readonly string[] Colors = {
        "B8E99B 4E9C65 E1F8C7", "EEF8FF 89BDD6 FFFFFF", "A2F0FF 439FCB D8FAFF", "DFB2FF 8757B4 F0DAFF", "FFBDD8 D77EAA FFE9F3",
        "FFE0A1 BD8A50 FFF0CE", "FFAD85 804638 FFD7A3", "BCA9F3 63528D E1D8FF", "FFE09A B88E3C FFF2C8", "D8E9FF 718CB7 F3F7FF",
        "A8B7EF 4E638E D6E0FF", "B8FFE6 5DAFA6 E7FFF7", "FFC5BF B66E9C FFE4DC", "F6F4FF AABBD4 FFFFFF", "95F3EE 4B638E F3ADFF",
        "FFCC83 B97340 FFE9B3", "BED2EE 536B8E E4F1FF", "D5DCEF 818CA9 F4F6FF", "FFE6AA C4A15A FFF7D6", "FFB2D1 AF84CD FFEAF5"
    };

    private static void Folder(string parent, string name)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + name)) AssetDatabase.CreateFolder(parent, name);
    }

    [MenuItem("Penguin/Rebuild 20 Level Blocks")]
    public static void RebuildAll()
    {
        if (Application.isPlaying) throw new System.InvalidOperationException("Stop Play before changing blocks.");
        Folder("Assets/Art", "LevelBlocks"); Folder("Assets/Art/LevelBlocks", "Meshes"); Folder("Assets/Art/LevelBlocks", "Materials");
        Folder("Assets/Prefabs", "LevelBlocks");
        for (int level = 1; level <= 20; level++)
        {
            string meshPath = "Assets/Art/LevelBlocks/Meshes/Block" + level.ToString("D2") + ".asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (mesh == null) { mesh = BuildMesh(level); AssetDatabase.CreateAsset(mesh, meshPath); }
            var materials = new Material[3];
            string[] colors = Colors[level - 1].Split(' ');
            for (int i = 0; i < 3; i++)
            {
                string path = "Assets/Art/LevelBlocks/Materials/Block" + level.ToString("D2") + "_" + i + ".mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, path); }
                ColorUtility.TryParseHtmlString("#" + colors[i], out var color);
                material.SetColor("_BaseColor", color);
                material.SetFloat("_Smoothness", level == 3 || level == 12 || level == 18 ? 0.55f : 0.2f);
                if (level == 15 && i == 2) { material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", color * 0.5f); }
                EditorUtility.SetDirty(material); materials[i] = material;
            }
            string prefabPath = "Assets/Prefabs/LevelBlocks/Level" + level.ToString("D2") + "_Block.prefab";
            var root = new GameObject("Level " + level + " - " + Names[level - 1]);
            root.AddComponent<MeshFilter>().sharedMesh = mesh;
            root.AddComponent<MeshRenderer>().sharedMaterials = materials;
            root.AddComponent<MeshCollider>().sharedMesh = mesh;
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);

            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Level" + level + ".unity", OpenSceneMode.Single);
            var stairs = Object.FindAnyObjectByType<IceStaircase>();
            var controller = Object.FindAnyObjectByType<ClimbLevel>();
            stairs.Configure(prefab, Camera.main);
            stairs.Rebuild(stairs.seed);
            controller.Configure(controller.settings, level, stairs, Camera.main);
            EditorUtility.SetDirty(stairs); EditorUtility.SetDirty(controller); EditorUtility.SetDirty(controller.Climber);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Debug.Log("Updated Level " + level + " block: " + Names[level - 1]);
        }
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity", OpenSceneMode.Single);
    }

    private static List<Vector2> Outline(int level)
    {
        var points = new List<Vector2>();
        int sides = level == 2 ? 6 : level == 3 ? 4 : level == 4 || level == 8 ? 5 : level == 9 ? 8 : level == 7 || level == 17 ? 12 : 48;
        if (level == 10) sides = 32;
        if (level == 11) sides = 14;
        if (level == 12) sides = 24;
        if (level == 15) sides = 8;
        if (level == 19)
        {
            // Crown silhouette, including three broad tips and a solid center landing.
            points.AddRange(new[] {new Vector2(-1,-0.75f),new Vector2(1,-0.75f),new Vector2(1,0.9f),new Vector2(0.48f,0.42f),new Vector2(0,1),new Vector2(-0.48f,0.42f),new Vector2(-1,0.9f)});
        }
        else if (level == 6)
            points.AddRange(new[] { new Vector2(0,-1),new Vector2(0.8f,-0.5f),new Vector2(1,0.55f),new Vector2(0.6f,1),new Vector2(-0.6f,1),new Vector2(-1,0.55f),new Vector2(-0.8f,-0.5f) });
        else
            for (int i = 0; i < sides; i++)
            {
                float angle = i / (float)sides * Mathf.PI * 2;
                float x = Mathf.Cos(angle), z = Mathf.Sin(angle), radius = 1;
                if (level == 1) { x = Mathf.Sign(x) * Mathf.Pow(Mathf.Abs(x), 0.5f); z = Mathf.Sign(z) * Mathf.Pow(Mathf.Abs(z), 0.5f); }
                if (level == 4) angle += 0.19f;
                if (level == 5) radius = 0.86f + 0.14f * Mathf.Cos(5 * angle);
                if (level == 7) radius = 0.86f + 0.13f * Mathf.Sin(i * 2.37f);
                if (level == 8) angle += 0.38f;
                if (level == 10) radius = i % 4 == 1 || i % 4 == 2 ? 0.82f : 1f;
                if (level == 11) radius = i % 2 == 0 ? 1 : 0.63f;
                if (level == 12) radius = i % 4 == 0 ? 1f : i % 4 == 2 ? 0.67f : 0.77f;
                if (level == 13) radius = 0.87f + 0.13f * Mathf.Cos(4 * angle + 0.3f);
                if (level == 14) radius = 0.9f + 0.1f * Mathf.Sin(7 * angle);
                if (level == 15) { x = Mathf.Sign(x) * Mathf.Pow(Mathf.Abs(x), 0.28f); z = Mathf.Sign(z) * Mathf.Pow(Mathf.Abs(z), 0.28f); }
                if (level == 16) radius = 0.8f + 0.17f * Mathf.Cos(3 * angle) + 0.06f * Mathf.Sin(7 * angle);
                if (level == 17) radius = 0.85f + 0.14f * Mathf.Sin(i * 4.17f);
                if (level == 20)
                {
                    x = 16 * Mathf.Pow(Mathf.Sin(angle), 3);
                    z = 13 * Mathf.Cos(angle) - 5 * Mathf.Cos(2*angle) - 2 * Mathf.Cos(3*angle) - Mathf.Cos(4*angle);
                    points.Add(new Vector2(x, z)); continue;
                }
                if (level == 4 || level == 8) { x = Mathf.Cos(angle); z = Mathf.Sin(angle); }
                points.Add(new Vector2(x, z) * radius);
            }
        // All silhouettes retain a 1.65 x 1.65 footprint and a centered, flat landing.
        var min = points[0]; var max = points[0];
        foreach (var p in points) { min = Vector2.Min(min,p); max = Vector2.Max(max,p); }
        var center = (min + max) * 0.5f;
        for (int i = 0; i < points.Count; i++) points[i] = new Vector2((points[i].x-center.x)/(max.x-min.x)*1.65f,(points[i].y-center.y)/(max.y-min.y)*1.65f);
        float area = 0;
        for (int i=0;i<points.Count;i++) {var a=points[i];var b=points[(i+1)%points.Count];area+=a.x*b.y-b.x*a.y;}
        if (area < 0) points.Reverse();
        return points;
    }

    private static Mesh BuildMesh(int level)
    {
        var outline = Outline(level);
        var vertices = new List<Vector3>();
        var triangles = new[] { new List<int>(), new List<int>(), new List<int>() };
        void Triangle(Vector3 a, Vector3 b, Vector3 c, int material)
        {
            int start=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);
            triangles[material].Add(start);triangles[material].Add(start+1);triangles[material].Add(start+2);
        }
        Vector3 Point(int i,float scale,float y) {var p=outline[i%outline.Count]*scale;return new Vector3(p.x,y,p.y);}
        float[] scales={0.94f,1f,0.96f,0.86f};float[] heights={0,-0.08f,-0.48f,-0.58f};
        for(int i=0;i<outline.Count;i++)
        {
            Triangle(Vector3.zero,Point(i+1,scales[0],0),Point(i,scales[0],0),0);
            for(int ring=0;ring<3;ring++)
            {
                var a=Point(i,scales[ring],heights[ring]);var b=Point(i+1,scales[ring],heights[ring]);
                var c=Point(i,scales[ring+1],heights[ring+1]);var d=Point(i+1,scales[ring+1],heights[ring+1]);
                Triangle(a,b,c,ring==0?2:1);Triangle(b,d,c,ring==0?2:1);
            }
            Triangle(Vector3.down*0.58f,Point(i,scales[3],-0.58f),Point(i+1,scales[3],-0.58f),1);
        }
        var mesh=new Mesh { name="Level "+level+" - "+Names[level-1] };
        mesh.SetVertices(vertices);mesh.subMeshCount=3;
        for(int i=0;i<3;i++)mesh.SetTriangles(triangles[i],i);
        mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
    }
}
