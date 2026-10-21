using UnityEngine;

[CreateAssetMenu(menuName = "Penguin/Climb Game Settings")]
public sealed class ClimbGameSettings : ScriptableObject
{
    [System.Serializable]
    public sealed class AnimalSlot
    {
        public string displayName;
        public GameObject prefab;
        public bool isTemporary;
    }

    [Tooltip("One animal slot per level. Empty slots temporarily use an available animal.")]
    public AnimalSlot[] animals = new AnimalSlot[20];
    public const int LevelCount = 20;

    public AnimalSlot ResolveAnimal(int level, out bool temporary)
    {
        int slot = Mathf.Clamp(level, 1, LevelCount) - 1;
        if (animals != null && slot < animals.Length && animals[slot]?.prefab != null)
        {
            temporary = animals[slot].isTemporary;
            return animals[slot];
        }
        temporary = true;
        int available = 0;
        if (animals != null)
            foreach (var animal in animals) if (animal?.prefab != null) available++;
        if (available == 0) return null;
        int fallback = slot % available;
        foreach (var animal in animals)
            if (animal?.prefab != null && fallback-- == 0) return animal;
        return null;
    }

    public int GoalForLevel(int level) => Mathf.Clamp(level, 1, LevelCount) * 50;
    public float HopTimeForLevel(int level) => Mathf.Lerp(0.26f, 0.18f, (Mathf.Clamp(level, 1, LevelCount) - 1) / 19f);
    public static string SceneName(int level) => "Level" + Mathf.Clamp(level, 1, LevelCount);
}
