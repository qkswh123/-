using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Real mesh scenery, with three recyclable height bands for endless climbing.</summary>
public sealed class LevelEnvironment : MonoBehaviour
{
    public static readonly string[] ThemeNames = {
        "초원 협곡", "눈꽃 소나무 숲", "푸른 빙하 계곡", "자수정 동굴", "벚꽃 하늘섬",
        "노을 사막", "용암 화산", "운석 지대", "황금빛 고대 유적", "겨울 성채",
        "달빛 숲", "오로라 빙원", "산호 바다", "구름 위 신전", "네온 수정 도시",
        "가을 단풍 계곡", "폭풍 절벽", "달의 산맥", "황금 구름 왕국", "무지개 정상"
    };

    [Range(1, 20)] public int levelNumber = 1;
    [HideInInspector] public bool subduedBackdrop;
    [SerializeField] private Camera gameCamera;
    [SerializeField] private Mesh[] shapes;
    [SerializeField] private Material[] palette;
    [SerializeField] private Material skyMaterial;
    [SerializeField] private Transform[] sceneryBands;
    [SerializeField] private Transform sky;
    [SerializeField] private float initialCameraY;
    [SerializeField] private GameObject snowPinePrefab;
    [SerializeField] private GameObject winterTowerPrefab;
    [SerializeField] private Transform[] spinningProps;
    private readonly List<Transform> spins = new List<Transform>();
    private System.Random random;

    public string ThemeName => ThemeNames[Mathf.Clamp(levelNumber - 1, 0, 19)];

    // Shapes: cube, cylinder, cone, crystal, low-poly sphere, ring, arch, mountain.
    public void Configure(int level, Camera camera, Mesh[] meshes, Material[] materials,
        Material skyMat, GameObject pine, GameObject tower)
    {
        levelNumber = level;
        gameCamera = camera;
        shapes = meshes;
        palette = materials;
        skyMaterial = skyMat;
        snowPinePrefab = pine;
        winterTowerPrefab = tower;
        initialCameraY = camera.transform.position.y;
        random = new System.Random(level * 7919);
        var dome = Make("3D Sky Dome", 4, 6, camera.transform.position, Vector3.one * 190, transform);
        dome.GetComponent<Renderer>().sharedMaterial = skyMaterial;
        sky = dome.transform;
        // Use a smooth built-in sphere mesh for the sky instead of the low-poly scenery mesh.
        var temp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        dome.GetComponent<MeshFilter>().sharedMesh = temp.GetComponent<MeshFilter>().sharedMesh;
        if (Application.isPlaying) Destroy(temp); else DestroyImmediate(temp);
        sceneryBands = new Transform[3];
        for (int band = 0; band < 3; band++)
        {
            var group = new GameObject("Scenery Band " + (band + 1));
            group.transform.SetParent(transform, false);
            sceneryBands[band] = group.transform;
            BuildBand(group.transform, band);
            group.transform.localPosition = Vector3.up * ((band - 1) * 36f);
        }
        spinningProps = spins.ToArray();
    }

    private float Range(float min, float max) => Mathf.Lerp(min, max, (float)random.NextDouble());

    private GameObject Make(string name, int mesh, int material, Vector3 position, Vector3 scale, Transform parent,
        Vector3 rotation = default)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localScale = scale;
        go.transform.localRotation = Quaternion.Euler(rotation);
        go.AddComponent<MeshFilter>().sharedMesh = shapes[mesh];
        var renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = palette[material];
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        return go;
    }

    private void BuildBand(Transform parent, int band)
    {
        int theme = levelNumber;
        bool space = theme == 8 || theme == 18;
        bool cave = theme == 4;
        for (int i = 0; i < 12; i++)
        {
            float x = (i - 5.5f) * 15f;
            float height = Range(24, 55);
            float z = Range(78, 103);
            Make("Distant mountain", 7, 0, new Vector3(x, -21, z), new Vector3(Range(9, 17), height, Range(10, 18)), parent);
            if (theme == 2 || theme == 3 || theme == 12 || theme == 17 || theme == 18)
                Make("Snow peak", 2, 6, new Vector3(x + 0.5f, -21 + height * 0.73f, z), new Vector3(4.3f, height * 0.29f, 4.3f), parent);
        }
        for (int side = -1; side <= 1; side += 2)
        {
            for (int tier = 0; tier < 3; tier++)
            {
                float x = side * Range(8f, 12f);
                float y = tier * 12f - 7f;
                float z = Range(13f, 22f);
                float width = Range(4.2f, 6.5f);
                var island = new GameObject("Floating cliff island");
                island.transform.SetParent(parent, false);
                island.transform.localPosition = new Vector3(x, y, z);
                Make("Faceted rock underside", 7, 0, Vector3.zero, new Vector3(width, Range(6, 12), width * 0.8f), island.transform, new Vector3(0, Range(0, 360), 180));
                Make("Island surface", 1, 1, Vector3.zero, new Vector3(width, 0.45f, width * 0.8f), island.transform);
                Decorate(island.transform, tier, side);
            }
        }
        if (!space)
            for (int i = 0; i < 5; i++) Cloud(parent, new Vector3(Range(-50, 50), Range(19, 30), Range(55, 100)), Range(3, 6));
        if (cave)
            for (int i = 0; i < 11; i++)
                Make("Hanging cave crystal", 3, i % 2 == 0 ? 4 : 0, new Vector3((i - 5) * 9, 36, Range(35, 70)), new Vector3(4, Range(8, 19), 4), parent, new Vector3(0, 0, 180));
        if (theme == 1 || theme == 3 || theme == 6 || theme == 7 || theme == 13 || theme == 16)
        {
            for (int i = 0; i < 7; i++)
                Make(theme == 7 ? "Lava cascade" : "Winding water / ice", 0, 5,
                    new Vector3(Mathf.Sin(i * 1.25f) * 13, -11f + i * 0.09f, 20 + i * 9), new Vector3(7, 0.22f, 12), parent, new Vector3(0, Mathf.Cos(i) * 28, 0));
        }
        if (theme == 8 || theme == 15 || theme == 19 || theme == 20)
        {
            var ring = Make("Monumental sky arch", 6, 4, new Vector3(0, 0, 64), Vector3.one * 20, parent);
            if (theme == 8 || theme == 15) spins.Add(ring.transform);
            if (theme == 20)
                for (int i = 1; i <= 5; i++) Make("Rainbow arch " + i, 6, i % 7, new Vector3(0, 0, 64 + i * 0.2f), Vector3.one * (20 + i * 0.9f), parent);
        }
        if (theme == 12)
            for (int i = 0; i < 5; i++) Make("Aurora ribbon", 6, i % 2 == 0 ? 4 : 2, new Vector3(-20 + i * 9, 18 + i * 2, 68 + i * 4), new Vector3(24, 12, 3), parent, new Vector3(0, i * 12, i * 8));
        if (theme == 17)
            for (int i = 0; i < 3; i++) Make("Lightning shard", 3, 7, new Vector3(-24 + i * 23, 15 + i * 3, 65), new Vector3(0.6f, 13, 0.6f), parent, new Vector3(0, 0, 16 * (i % 2 == 0 ? 1 : -1)));
        if (theme == 18)
            Make("Distant moon", 4, 6, new Vector3(-27, 25, 100), Vector3.one * 12, parent);
    }

    private void Decorate(Transform parent, int tier, int side)
    {
        int theme = levelNumber;
        switch (theme)
        {
            case 1: case 5: case 11: case 16:
                for (int i = 0; i < 3; i++) Tree(parent, new Vector3((i - 1) * 2.2f, 0.45f, Range(-1.5f, 1.5f)), false, Range(0.8f, 1.3f));
                if (theme == 11) for (int i = 0; i < 5; i++) Make("Forest glow mote", 4, 7, new Vector3(Range(-4, 4), Range(1, 5), Range(-2, 2)), Vector3.one * 0.12f, parent);
                break;
            case 2:
                if (snowPinePrefab != null)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        var tree = Instantiate(snowPinePrefab, parent);
                        tree.transform.localPosition = new Vector3((i - 1) * 2.6f, 0.45f, Range(-1.5f, 1.5f));
                        tree.transform.localRotation = Quaternion.Euler(0, Range(0, 360), 0);
                        tree.transform.localScale *= 1.5f;
                    }
                }
                else Tree(parent, Vector3.up * 0.45f, true, 1.6f);
                break;
            case 3: case 4: case 12: case 15:
                for (int i = 0; i < 5; i++) Make("Growing crystal", 3, i % 2 == 0 ? 4 : 2, new Vector3(Range(-3.5f, 3.5f), 0.45f, Range(-2, 2)), new Vector3(Range(0.5f, 1), Range(2, 6), Range(0.5f, 1)), parent, new Vector3(Range(-15, 15), Range(0, 180), Range(-15, 15)));
                break;
            case 6:
                for (int i = 0; i < 3; i++)
                {
                    var p = new Vector3((i - 1) * 2.4f, 0.45f, 0);
                    Make("Cactus trunk", 1, 2, p, new Vector3(0.35f, 3.5f, 0.35f), parent);
                    Make("Cactus arm", 1, 2, p + new Vector3(0.9f, 1.6f, 0), new Vector3(0.24f, 1.8f, 0.24f), parent);
                    Make("Cactus branch", 0, 2, p + new Vector3(0.5f, 1.7f, 0), new Vector3(1.2f, 0.4f, 0.4f), parent);
                }
                break;
            case 7:
                Make("Volcano cone", 7, 0, Vector3.up * 0.4f, new Vector3(3.5f, 6, 3.5f), parent);
                Make("Molten crater", 5, 7, Vector3.up * 5.4f, new Vector3(1.5f, 0.6f, 1.5f), parent, new Vector3(90, 0, 0));
                for (int i = 0; i < 4; i++) Make("Floating ember", 4, 7, new Vector3(Range(-2, 2), Range(6, 10), Range(-2, 2)), Vector3.one * Range(0.1f, 0.3f), parent);
                break;
            case 8:
                for (int i = 0; i < 4; i++)
                {
                    var rock = Make("Orbiting asteroid", 4, i % 2 == 0 ? 0 : 4, new Vector3(Range(-3, 3), Range(2, 7), Range(-2, 2)), Vector3.one * Range(0.7f, 1.7f), parent);
                    spins.Add(rock.transform);
                }
                break;
            case 9: case 14: case 19:
                for (int i = -1; i <= 1; i += 2)
                {
                    Make("Temple column", 1, 6, new Vector3(i * 2.3f, 0.45f, 0), new Vector3(0.6f, 5.5f, 0.6f), parent);
                    Make("Golden capital", 0, 4, new Vector3(i * 2.3f, 5.8f, 0), new Vector3(1.6f, 0.5f, 1.6f), parent);
                }
                Make("Temple lintel", 0, 4, new Vector3(0, 6.4f, 0), new Vector3(6.5f, 0.65f, 1.8f), parent);
                if (theme != 9) Make("Temple pediment", 2, 6, new Vector3(0, 6.8f, 0), new Vector3(4.5f, 2, 2.3f), parent);
                break;
            case 10:
                if (winterTowerPrefab != null)
                {
                    var tower = Instantiate(winterTowerPrefab, parent);
                    tower.transform.localPosition = Vector3.up * 0.45f;
                    tower.transform.localRotation = Quaternion.Euler(0, 180, 0);
                    tower.transform.localScale *= 1.5f;
                }
                else Make("Fortress tower", 0, 6, Vector3.up * 3, new Vector3(3, 6, 3), parent);
                Make("Castle wall", 0, 0, new Vector3(0, 1.5f, 2), new Vector3(9, 2.5f, 1), parent);
                break;
            case 13:
                for (int i = 0; i < 5; i++)
                {
                    var p = new Vector3(Range(-3, 3), 0.4f, Range(-2, 2));
                    Make("Coral stalk", 1, i % 2 == 0 ? 2 : 4, p, new Vector3(0.25f, Range(1.5f, 3), 0.25f), parent, new Vector3(0, 0, Range(-25, 25)));
                    Make("Coral crown", 4, i % 2 == 0 ? 2 : 4, p + Vector3.up * 2.5f, new Vector3(0.9f, 0.6f, 0.8f), parent);
                    Make("Water bubble", 5, 7, p + Vector3.up * Range(4, 7), Vector3.one * 0.35f, parent);
                }
                break;
            case 17:
                Make("Storm spire", 7, 0, Vector3.up * 0.5f, new Vector3(2, 10, 2), parent, new Vector3(0, 0, side * 12));
                Make("Ice lightning crystal", 3, 7, new Vector3(2, 0.5f, 1), new Vector3(0.5f, 4, 0.5f), parent);
                break;
            case 18:
                for (int i = 0; i < 4; i++) Make("Moon crater", 5, 3, new Vector3(Range(-3, 3), 0.52f, Range(-2, 2)), Vector3.one * Range(0.5f, 1.2f), parent, new Vector3(90, 0, 0));
                break;
            case 20:
                for (int i = 0; i < 5; i++) Make("Summit crystal", 3, i + 2, new Vector3((i - 2) * 1.3f, 0.5f, 0), new Vector3(0.6f, 2.5f + i * 0.5f, 0.6f), parent);
                Make("Summit crown", 5, 4, new Vector3(0, 6, 0), new Vector3(2, 2, 2), parent, new Vector3(20, 0, 0));
                break;
        }
    }

    private void Tree(Transform parent, Vector3 position, bool pine, float size)
    {
        Make("Tree trunk", 1, 3, position, new Vector3(0.25f, 3 * size, 0.25f), parent);
        if (pine)
        {
            for (int i = 0; i < 3; i++) Make("Pine canopy", 2, 2, position + Vector3.up * (1.5f + i) * size, new Vector3(1.7f - i * 0.3f, 2.4f, 1.7f - i * 0.3f) * size, parent);
        }
        else
        {
            for (int i = 0; i < 3; i++) Make("Faceted leafy crown", 4, 2, position + new Vector3((i - 1) * 0.9f * size, (3.3f + (i == 1 ? 0.8f : 0)) * size, 0), new Vector3(1.6f, 1.8f, 1.5f) * size, parent);
        }
    }

    private void Cloud(Transform parent, Vector3 position, float size)
    {
        for (int i = 0; i < 3; i++) Make("Sculpted cloud", 4, 6, position + Vector3.right * (i - 1) * size, new Vector3(1.6f, 0.65f, 0.8f) * size, parent);
    }

    private void LateUpdate()
    {
        if (gameCamera == null) return;
        if (sky != null) sky.position = gameCamera.transform.position;
        float height = gameCamera.transform.position.y - initialCameraY;
        if (sceneryBands != null)
            for (int i = 0; i < sceneryBands.Length; i++)
            {
                float original = (i - 1) * 36f;
                float y = original + Mathf.Floor((height - original + 54f) / 108f) * 108f;
                if (sceneryBands[i] != null) sceneryBands[i].localPosition = Vector3.up * y;
            }
        if (spinningProps != null)
            foreach (var prop in spinningProps)
                if (prop != null) prop.Rotate(0, 9f * Time.deltaTime, 4f * Time.deltaTime, Space.Self);
    }
}
