using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class StepCoinBuilder
{
    [MenuItem("Penguin/Apply Step Coins to All Levels")]
    public static void ApplyAll()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Stop Play first.");
        if (!AssetDatabase.IsValidFolder("Assets/Art/Coins")) AssetDatabase.CreateFolder("Assets/Art", "Coins");
        var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PolyKebap/Stylized Coin Pack/Prefabs/Gold Coins/Coin_Gold_Circle.prefab");
        if (source == null) throw new InvalidOperationException("Gold coin source missing.");
        var root = new GameObject("Climbing Step Coin");
        var model = (GameObject)PrefabUtility.InstantiatePrefab(source);
        model.name = "Gold Coin Visual";
        model.transform.SetParent(root.transform, false);
        model.transform.localScale = new Vector3(1.5f, 1.5f, 6f);
        model.transform.localPosition = new Vector3(0, 0.65f, -0.48f);
        foreach (var collider in model.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(collider);
        foreach (var renderer in model.GetComponentsInChildren<Renderer>())
        {
            var materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                var original = materials[i];
                string path = "Assets/Art/Coins/" + original.name + ".mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    AssetDatabase.CreateAsset(material, path);
                }
                material.shader = Shader.Find("Universal Render Pipeline/Lit");
                var color = i == 0 ? new Color(1f,0.81f,0.16f) : new Color(0.9f,0.57f,0.07f);
                material.SetColor("_BaseColor", color);
                material.SetFloat("_Metallic", 0.25f);
                material.SetFloat("_Smoothness", 0.45f);
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 0.18f);
                EditorUtility.SetDirty(material);
                materials[i] = material;
            }
            renderer.sharedMaterials = materials;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
        var coin = root.AddComponent<StepCoin>();
        coin.SetVisual(model.transform);
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/StepCoin.prefab");
        UnityEngine.Object.DestroyImmediate(root);
        for (int number = 1; number <= 20; number++)
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Level" + number + ".unity");
            var stairs = UnityEngine.Object.FindAnyObjectByType<IceStaircase>();
            stairs.ConfigureCoins(prefab);
            EditorUtility.SetDirty(stairs);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");
    }
}
