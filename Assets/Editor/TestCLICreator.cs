using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class TestCLICreator
{
    [MenuItem("CLI/Create Test Scene")]
    public static void CreateSceneWithCube()
    {
        Debug.Log("[Unity CLI] Creating new scene 'TestCLI' with a Cube...");

        // Create a new scene with default game objects (Camera, Directional Light)
        Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // Create a Cube primitive
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "Cube";
        cube.transform.position = Vector3.zero;

        // Ensure target directory exists
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
        {
            AssetDatabase.CreateFolder("Assets", "Scenes");
        }

        string scenePath = "Assets/Scenes/TestCLI.unity";
        bool saved = EditorSceneManager.SaveScene(newScene, scenePath);
        if (saved)
        {
            Debug.Log($"[Unity CLI] Successfully created and saved scene at: {scenePath}");
        }
        else
        {
            Debug.LogError($"[Unity CLI] Failed to save scene at: {scenePath}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public static void CreateSceneWithCubeBatch()
    {
        try
        {
            CreateSceneWithCube();
            EditorApplication.Exit(0);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Unity CLI] Exception creating scene: {ex}");
            EditorApplication.Exit(1);
        }
    }
}
