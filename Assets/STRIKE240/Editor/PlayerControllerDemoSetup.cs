using Strike240;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Strike240.Editor
{
    /// <summary>Generates a standalone player movement demo scene.</summary>
    public static class PlayerControllerDemoSetup
    {
        [MenuItem("BREACH/Create Player Controller Demo")]
        public static void CreateDemoFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            CreateDemoScene();
        }

        // Entry point used by Unity batch mode when validating the generator.
        public static void CreateDemoForValidation()
        {
            CreateDemoScene();
        }

        private static void CreateDemoScene()
        {
            const string root = "Assets/STRIKE240";
            EnsureFolder(root, "Editor");
            EnsureFolder(root, "Scripts");
            EnsureFolder(root + "/Scripts", "Player");
            EnsureFolder(root, "Materials");
            EnsureFolder(root, "Scenes");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateBlock("Practice Floor", new Vector3(0f, -0.25f, 0f), new Vector3(30f, 0.5f, 30f), new Color(0.18f, 0.22f, 0.20f));
            CreateBlock("North Boundary", new Vector3(0f, 1.5f, 15f), new Vector3(30f, 3f, 0.5f), new Color(0.32f, 0.36f, 0.34f));
            CreateBlock("South Boundary", new Vector3(0f, 1.5f, -15f), new Vector3(30f, 3f, 0.5f), new Color(0.32f, 0.36f, 0.34f));
            CreateBlock("East Boundary", new Vector3(15f, 1.5f, 0f), new Vector3(0.5f, 3f, 30f), new Color(0.32f, 0.36f, 0.34f));
            CreateBlock("West Boundary", new Vector3(-15f, 1.5f, 0f), new Vector3(0.5f, 3f, 30f), new Color(0.32f, 0.36f, 0.34f));
            CreateBlock("Low Cover A", new Vector3(-5f, 0.6f, 0f), new Vector3(3f, 1.2f, 1f), new Color(0.45f, 0.38f, 0.26f));
            CreateBlock("Low Cover B", new Vector3(5f, 0.6f, 4f), new Vector3(3f, 1.2f, 1f), new Color(0.45f, 0.38f, 0.26f));
            CreateBlock("Center Barrier", new Vector3(0f, 1f, 8f), new Vector3(8f, 2f, 0.6f), new Color(0.28f, 0.32f, 0.30f));

            GameObject player = new GameObject("Player");
            player.transform.position = new Vector3(0f, 0.05f, -8f);
            CharacterController character = player.AddComponent<CharacterController>();
            character.height = 1.8f;
            character.radius = 0.35f;
            character.center = new Vector3(0f, 0.9f, 0f);
            character.slopeLimit = 45f;
            character.stepOffset = 0.3f;

            GameObject cameraObject = new GameObject("Player Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(player.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, 1.62f, 0f);
            cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();

            FirstPersonController controller = player.AddComponent<FirstPersonController>();
            SerializedObject serializedController = new SerializedObject(controller);
            serializedController.FindProperty("cameraPivot").objectReferenceValue = cameraObject.transform;
            serializedController.ApplyModifiedPropertiesWithoutUndo();

            GameObject sun = new GameObject("Sun Light");
            Light light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            RenderSettings.ambientLight = new Color(0.45f, 0.48f, 0.5f);

            EditorSceneManager.SaveScene(scene, root + "/Scenes/PlayerControllerDemo.unity");
            Selection.activeGameObject = player;
            Debug.Log("BREACH demo scene created. Play with WASD, mouse, Space, and Left Shift; Esc releases the cursor.");
        }

        private static void EnsureFolder(string parent, string name)
        {
            string path = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, name);
        }

        private static void CreateBlock(string name, Vector3 position, Vector3 scale, Color color)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.position = position;
            block.transform.localScale = scale;

            string materialPath = "Assets/STRIKE240/Materials/" + name.Replace(" ", "") + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                material = new Material(shader) { color = color };
                AssetDatabase.CreateAsset(material, materialPath);
            }
            block.GetComponent<Renderer>().sharedMaterial = material;
        }
    }
}
