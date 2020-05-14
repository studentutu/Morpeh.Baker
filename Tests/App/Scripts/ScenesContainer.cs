using UnityEngine;

[CreateAssetMenu(fileName = "Scenes", menuName = "Test/Scenes", order = 1)]
public class ScenesContainer : ScriptableObject
{
    private static ScenesContainer instance = null;
    public static ScenesContainer Instance
    {
        get
        {
            if (instance == null) Init();
            return instance;
        }
    }

    [SerializeField] private int allScenes = 0;
    public int AllScenes => allScenes;

    private static void Init()
    {
        if (instance == null)
        {
            var allLoadedResourcesObjects = UnityEngine.Resources.LoadAll<ScenesContainer>("Tests");
            // When Instantiated - it clones everything from it!
            instance = UnityEngine.ScriptableObject.Instantiate<ScenesContainer>(allLoadedResourcesObjects[0]);
            // This is what Unity does on the start of the application for each Scriptable Object.
            instance.hideFlags = UnityEngine.HideFlags.NotEditable | UnityEngine.HideFlags.HideAndDontSave;
            for (int i = 0; i < allLoadedResourcesObjects.Length; i++)
            {
                UnityEngine.Resources.UnloadAsset(allLoadedResourcesObjects[i]);
            }
            allLoadedResourcesObjects = null;
        }
    }
}
