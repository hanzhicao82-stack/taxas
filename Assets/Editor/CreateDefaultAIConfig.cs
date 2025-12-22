using UnityEditor;
using UnityEngine;

public static class CreateDefaultAIConfig
{
    [MenuItem("Tools/Poker/Create Default AIConfig")]
    public static void Create()
    {
        const string folder = "Assets/Configs";
        const string path = folder + "/DefaultAIConfig.asset";

        if (!AssetDatabase.IsValidFolder(folder))
        {
            AssetDatabase.CreateFolder("Assets", "Configs");
        }

        // If the asset already exists, select it and return
        var existing = AssetDatabase.LoadAssetAtPath<AIConfig>(path);
        if (existing != null)
        {
            Selection.activeObject = existing;
            Debug.Log("Default AIConfig already exists and was selected.");
            return;
        }

        var cfg = ScriptableObject.CreateInstance<AIConfig>();
        cfg.raiseProbability = 0.12f;
        cfg.betProbability = 0.06f;
        cfg.raiseSizeBase = 0.5f;
        cfg.raiseSizeAggressionScale = 1.0f;
        cfg.minRaiseFraction = 0.5f;
        cfg.simIterations = 200;

        AssetDatabase.CreateAsset(cfg, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = cfg;
        Debug.Log("Created DefaultAIConfig at " + path);
    }
}
