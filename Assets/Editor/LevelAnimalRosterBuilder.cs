using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Keep the first six characters and give every available later level its own model.</summary>
public static class LevelAnimalRosterBuilder
{
    [MenuItem("Penguin/Assign Level 20 Egg")]
    public static void AssignEgg()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Stop Play first.");
        var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Scanta/RagingEgg/Prefab/Idle.prefab");
        if (source == null) throw new InvalidOperationException("Import Low Poly 3D Animated Egg first.");
        var root = new GameObject("Climbing_Egg");
        var model = (GameObject)PrefabUtility.InstantiatePrefab(source);
        model.transform.SetParent(root.transform, false);
        string controllerPath = "Assets/Art/Animals/CuteAnimations/Egg.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var machine = controller.layers[0].stateMachine;
            var idle = machine.AddState("Idle_A");
            idle.motion = AssetDatabase.LoadAllAssetsAtPath("Assets/Scanta/RagingEgg/Model/Idle.fbx").OfType<AnimationClip>().First(c => c.name == "Idle");
            machine.defaultState = idle;
            var jump = machine.AddState("Jump");
            jump.motion = AssetDatabase.LoadAllAssetsAtPath("Assets/Scanta/RagingEgg/Model/Jump.fbx").OfType<AnimationClip>().First(c => c.name == "Jump");
        }
        var animator = model.GetComponentInChildren<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Animals/Egg.mat");
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Scanta/RagingEgg/Texture/Egg_character_Diffuse.png"));
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Smoothness", 0.2f);
            AssetDatabase.CreateAsset(material, "Assets/Art/Animals/Egg.mat");
        }
        foreach (var renderer in model.GetComponentsInChildren<Renderer>())
            renderer.sharedMaterials = Enumerable.Repeat(material, renderer.sharedMaterials.Length).ToArray();
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/Animals/Climbing_Egg.prefab");
        UnityEngine.Object.DestroyImmediate(root);
        var settings = AssetDatabase.LoadAssetAtPath<ClimbGameSettings>("Assets/GameData/ClimbGameSettings.asset");
        if (settings.animals.Length < 20) Array.Resize(ref settings.animals, 20);
        settings.animals[19] = new ClimbGameSettings.AnimalSlot { displayName = "에그", prefab = prefab };
        EditorUtility.SetDirty(settings);
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Level20.unity");
        UnityEngine.Object.FindAnyObjectByType<ClimbLevel>().Configure(settings, 20, UnityEngine.Object.FindAnyObjectByType<IceStaircase>(), Camera.main);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    private static void Folder(string parent, string name)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + name)) AssetDatabase.CreateFolder(parent, name);
    }

    private static GameObject Dog(string id)
    {
        string original = "Assets/Bublisher/3D Stylized Animated Dogs Kit/Prefabs/" + id + ".prefab";
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(original);
        if (source == null) throw new InvalidOperationException("Missing dog " + original);
        var root = new GameObject("Climbing_" + id);
        var model = (GameObject)PrefabUtility.InstantiatePrefab(source);
        model.transform.SetParent(root.transform, false);
        var animator = model.GetComponentInChildren<Animator>();
        var clips = animator.runtimeAnimatorController.animationClips;
        string controllerPath = "Assets/Art/Animals/DogAnimations/" + id + ".controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var machine = controller.layers[0].stateMachine;
            var idle = machine.AddState("Idle_A");
            idle.motion = clips.First(c => c.name.EndsWith("_Breathing"));
            machine.defaultState = idle;
            var jump = machine.AddState("Jump");
            jump.motion = clips.First(c => c.name.Contains("_Running"));
        }
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        foreach (var renderer in model.GetComponentsInChildren<Renderer>())
        {
            var materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                var sourceMaterial = materials[i];
                string materialPath = "Assets/Art/Animals/DogMaterials/" + sourceMaterial.name + ".mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null)
                {
                    material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    material.SetTexture("_BaseMap", sourceMaterial.mainTexture);
                    material.SetColor("_BaseColor", sourceMaterial.color);
                    material.SetFloat("_Smoothness", 0.2f);
                    AssetDatabase.CreateAsset(material, materialPath);
                }
                materials[i] = material;
            }
            renderer.sharedMaterials = materials;
        }
        string prefabPath = "Assets/Prefabs/Animals/Climbing_" + id + ".prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        UnityEngine.Object.DestroyImmediate(root);
        return prefab;
    }

    [MenuItem("Penguin/Assign Available Unique Animals")]
    public static void Apply()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Stop Play first.");
        Folder("Assets/Art/Animals", "DogAnimations");
        Folder("Assets/Art/Animals", "DogMaterials");
        var settings = AssetDatabase.LoadAssetAtPath<ClimbGameSettings>("Assets/GameData/ClimbGameSettings.asset");
        string backupPath = "Assets/GameData/Backups/ClimbGameSettings_BeforeUnique.asset";
        if (AssetDatabase.LoadAssetAtPath<ClimbGameSettings>(backupPath) == null)
            AssetDatabase.CreateAsset(UnityEngine.Object.Instantiate(settings), backupPath);
        var slots = new ClimbGameSettings.AnimalSlot[20];
        Array.Copy(settings.animals, slots, 6);
        string[] ids = { "corgi", "Pudu", "pug", "Sparrow", "chihuahua", "Gecko", "cur", "Muskrat", "germanshepherd", "Colobus", "Squid", "Herring", "Taipan" };
        string[] names = { "웰시 코기", "꼬마 푸두", "아기 퍼그", "꼬마 참새", "치와와", "도마뱀", "믹스 강아지", "사향쥐", "셰퍼드", "콜로부스 원숭이", "오징어", "청어", "타이판 뱀" };
        for (int i = 0; i < ids.Length; i++)
        {
            bool dog = char.IsLower(ids[i][0]);
            var prefab = dog ? Dog(ids[i]) : AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Animals/Climbing_" + ids[i] + ".prefab");
            if (prefab == null) throw new InvalidOperationException("Missing character " + ids[i]);
            slots[i + 6] = new ClimbGameSettings.AnimalSlot { displayName = names[i], prefab = prefab };
        }
        slots[19] = settings.animals.Length > 19 && settings.animals[19]?.prefab != null && !settings.animals[19].isTemporary
            ? settings.animals[19]
            : new ClimbGameSettings.AnimalSlot { displayName = "꼬마 고양이 (임시)", prefab = slots[1].prefab, isTemporary = true };
        settings.animals = slots;
        EditorUtility.SetDirty(settings);
        for (int number = 7; number <= 20; number++)
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Level" + number + ".unity");
            var level = UnityEngine.Object.FindAnyObjectByType<ClimbLevel>();
            level.Configure(settings, number, UnityEngine.Object.FindAnyObjectByType<IceStaircase>(), Camera.main);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Level " + number + ": " + slots[number - 1].displayName);
        }
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene("Assets/Scenes/Level7.unity");
    }
}
