using Strike240;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Strike240.Editor
{
    /// <summary>Installs the weapon prototype into the generated controller demo.</summary>
    public static class WeaponPrototypeSetup
    {
        private const string DemoScenePath = "Assets/STRIKE240/Scenes/PlayerControllerDemo.unity";

        [MenuItem("BREACH/Install Weapon Prototype in Demo Scene")]
        public static void InstallFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(DemoScenePath) == null)
            {
                EditorUtility.DisplayDialog("BREACH", "Create the Player Controller Demo scene first, then install the weapon prototype.", "OK");
                return;
            }

            var scene = EditorSceneManager.OpenScene(DemoScenePath, OpenSceneMode.Single);
            InstallIntoOpenDemoScene(scene);
        }

        [MenuItem("BREACH/Add Practice Dummy to Open Scene")]
        public static void AddPracticeDummyToOpenScene()
        {
            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                EditorUtility.DisplayDialog("BREACH", "Open a scene before adding a practice dummy.", "OK");
                return;
            }

            GameObject existingDummy = GameObject.Find("Practice Dummy");
            if (existingDummy != null)
            {
                Selection.activeGameObject = existingDummy;
                EditorUtility.DisplayDialog("BREACH", "This scene already has a Practice Dummy.", "OK");
                return;
            }

            EnsureFolder("Assets/STRIKE240", "Materials");
            Material body = GetOrCreateMaterial("DummyCloth", new Color(0.24f, 0.30f, 0.29f));
            Material details = GetOrCreateMaterial("TargetRed", new Color(0.72f, 0.11f, 0.07f));
            CreatePracticeDummy("Practice Dummy", new Vector3(2f, 0f, 9f), body, details);
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = GameObject.Find("Practice Dummy");
            Debug.Log("BREACH practice dummy added. Save the scene to keep it.");
        }

        [MenuItem("BREACH/Upgrade Rifle Visuals in Open Scene")]
        public static void UpgradeRifleVisualsInOpenScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            GameObject viewModel = GameObject.Find("AK Placeholder View Model");
            GameObject weaponSystem = GameObject.Find("Weapon System");
            if (viewModel == null || weaponSystem == null)
            {
                EditorUtility.DisplayDialog("BREACH", "Open PlayerControllerDemo with the installed weapon prototype before upgrading rifle visuals.", "OK");
                return;
            }

            UpgradeRifleVisuals(viewModel, weaponSystem, EditorSceneManager.GetActiveScene());
        }

        public static void UpgradeRifleVisualsForValidation()
        {
            GameObject viewModel = GameObject.Find("AK Placeholder View Model");
            GameObject weaponSystem = GameObject.Find("Weapon System");
            if (viewModel == null || weaponSystem == null)
                throw new System.InvalidOperationException("The open scene must contain AK Placeholder View Model and Weapon System objects.");

            UpgradeRifleVisuals(viewModel, weaponSystem, EditorSceneManager.GetActiveScene());
        }

        private static void UpgradeRifleVisuals(GameObject viewModel, GameObject weaponSystem, UnityEngine.SceneManagement.Scene scene)
        {

            EnsureFolder("Assets/STRIKE240", "Environment");
            EnsureFolder("Assets/STRIKE240/Environment", "Meshes");
            EnsureFolder("Assets/STRIKE240", "Materials");

            Material metal = ConfigureWeaponMaterial("WeaponMetal", new Color(0.12f, 0.14f, 0.13f), 0.68f, 0.43f);
            Material darkMetal = ConfigureWeaponMaterial("WeaponDarkMetal", new Color(0.045f, 0.055f, 0.052f), 0.48f, 0.35f);
            Material wood = ConfigureWeaponMaterial("WeaponWood", new Color(0.30f, 0.18f, 0.09f), 0f, 0.27f);
            Material glass = ConfigureWeaponMaterial("WeaponOpticGlass", new Color(0.10f, 0.19f, 0.20f), 0.2f, 0.72f);

            ClearViewModelGeometry(viewModel.transform);
            BuildRifleGeometry(viewModel.transform, metal, darkMetal, wood, glass);
            if (weaponSystem.GetComponent<WeaponVisualEffects>() == null)
                weaponSystem.AddComponent<WeaponVisualEffects>();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = viewModel;
            Debug.Log("BREACH rifle view model upgraded. WeaponSystem and RifleWeapon references were preserved.");
        }

        // Entry point for compile and scene setup validation in Unity batch mode.
        public static void InstallForValidation()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(DemoScenePath) == null)
                throw new System.InvalidOperationException("PlayerControllerDemo.unity must exist before weapon setup.");

            var scene = EditorSceneManager.OpenScene(DemoScenePath, OpenSceneMode.Single);
            InstallIntoOpenDemoScene(scene);
        }

        private static void InstallIntoOpenDemoScene(UnityEngine.SceneManagement.Scene scene)
        {
            Camera aimCamera = Camera.main;
            if (aimCamera == null)
            {
                EditorUtility.DisplayDialog("BREACH", "The demo scene needs a MainCamera. Recreate the player controller demo and try again.", "OK");
                return;
            }

            FirstPersonController playerController = aimCamera.GetComponentInParent<FirstPersonController>();
            CharacterController characterController = aimCamera.GetComponentInParent<CharacterController>();
            if (playerController == null || characterController == null)
            {
                EditorUtility.DisplayDialog("BREACH", "The MainCamera must be a child of the Player with FirstPersonController and CharacterController components.", "OK");
                return;
            }

            EnsureFolder("Assets/STRIKE240", "Materials");
            EnsureFolder("Assets/STRIKE240", "Environment");
            EnsureFolder("Assets/STRIKE240/Environment", "Meshes");
            RemoveOldPrototypeObjects();

            Material metal = ConfigureWeaponMaterial("WeaponMetal", new Color(0.12f, 0.14f, 0.13f), 0.68f, 0.43f);
            Material darkMetal = ConfigureWeaponMaterial("WeaponDarkMetal", new Color(0.045f, 0.055f, 0.052f), 0.48f, 0.35f);
            Material wood = ConfigureWeaponMaterial("WeaponWood", new Color(0.30f, 0.18f, 0.09f), 0f, 0.27f);
            Material targetPaint = GetOrCreateMaterial("TargetPaint", new Color(0.76f, 0.79f, 0.74f));
            Material targetRed = GetOrCreateMaterial("TargetRed", new Color(0.72f, 0.11f, 0.07f));
            Material dummyCloth = GetOrCreateMaterial("DummyCloth", new Color(0.24f, 0.30f, 0.29f));

            Transform weaponRoot = new GameObject("AK Placeholder View Model").transform;
            weaponRoot.SetParent(aimCamera.transform, false);
            weaponRoot.localPosition = new Vector3(0.29f, -0.25f, 0.82f);
            weaponRoot.localRotation = Quaternion.Euler(0f, -2f, 0f);
            weaponRoot.localScale = Vector3.one * 0.82f;
            Material glass = ConfigureWeaponMaterial("WeaponOpticGlass", new Color(0.10f, 0.19f, 0.20f), 0.2f, 0.72f);
            BuildRifleGeometry(weaponRoot, metal, darkMetal, wood, glass);

            GameObject weaponSystem = new GameObject("Weapon System");
            weaponSystem.transform.SetParent(aimCamera.transform, false);
            AudioSource audioSource = weaponSystem.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            WeaponAudio audio = weaponSystem.AddComponent<WeaponAudio>();
            RifleWeapon weapon = weaponSystem.AddComponent<RifleWeapon>();
            weaponSystem.AddComponent<WeaponVisualEffects>();
            SerializedObject serializedWeapon = new SerializedObject(weapon);
            serializedWeapon.FindProperty("aimCamera").objectReferenceValue = aimCamera;
            serializedWeapon.FindProperty("playerController").objectReferenceValue = playerController;
            serializedWeapon.FindProperty("characterController").objectReferenceValue = characterController;
            serializedWeapon.FindProperty("weaponAudio").objectReferenceValue = audio;
            serializedWeapon.ApplyModifiedPropertiesWithoutUndo();

            GameObject hudObject = new GameObject("Weapon HUD");
            WeaponHUD hud = hudObject.AddComponent<WeaponHUD>();
            SerializedObject serializedHud = new SerializedObject(hud);
            serializedHud.FindProperty("weapon").objectReferenceValue = weapon;
            serializedHud.ApplyModifiedPropertiesWithoutUndo();

            CreatePracticeTarget("Practice Target Left", new Vector3(-4f, 1.55f, 6.5f), targetPaint, targetRed);
            CreatePracticeTarget("Practice Target Center", new Vector3(0f, 1.55f, 6.5f), targetPaint, targetRed);
            CreatePracticeTarget("Practice Target Right", new Vector3(4f, 1.55f, 6.5f), targetPaint, targetRed);
            CreatePracticeDummy("Practice Dummy", new Vector3(2f, 0f, 9f), dummyCloth, targetRed);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = weaponSystem;
            Debug.Log("BREACH weapon prototype installed. Left mouse fires, B toggles semi/auto, R reloads.");
        }

        private static void RemoveOldPrototypeObjects()
        {
            foreach (string objectName in new[]
            {
                "AK Placeholder View Model", "Weapon System", "Weapon HUD",
                "Practice Target Left", "Practice Target Center", "Practice Target Right",
                "Practice Dummy",
                "Practice Target Left Stand", "Practice Target Center Stand", "Practice Target Right Stand"
            })
            {
                GameObject existing = GameObject.Find(objectName);
                if (existing != null) Object.DestroyImmediate(existing);
            }
        }

        private static void BuildRifleGeometry(Transform root, Material metal, Material darkMetal, Material wood, Material glass)
        {
            AddRoundedWeaponPart(root, "Receiver • lower alloy", new Vector3(0f, 0f, 0.02f), new Vector3(0.19f, 0.15f, 0.38f), 0.035f, 3, metal);
            AddRoundedWeaponPart(root, "Receiver • dust cover", new Vector3(0f, 0.086f, 0.015f), new Vector3(0.17f, 0.034f, 0.34f), 0.014f, 2, darkMetal);
            AddRoundedWeaponPart(root, "Handguard • wood", new Vector3(0f, 0.005f, 0.34f), new Vector3(0.16f, 0.09f, 0.28f), 0.028f, 3, wood);
            AddPart(root, "Barrel • heavy profile", PrimitiveType.Cylinder, new Vector3(0f, 0.01f, 0.68f), new Vector3(0.027f, 0.27f, 0.027f), darkMetal, Quaternion.Euler(90f, 0f, 0f));
            AddPart(root, "Barrel • gas block", PrimitiveType.Cylinder, new Vector3(0f, 0.01f, 0.55f), new Vector3(0.048f, 0.035f, 0.048f), metal, Quaternion.Euler(90f, 0f, 0f));
            AddPart(root, "Muzzle brake", PrimitiveType.Cylinder, new Vector3(0f, 0.01f, 0.97f), new Vector3(0.052f, 0.06f, 0.052f), metal, Quaternion.Euler(90f, 0f, 0f));
            AddPart(root, "Muzzle brake • bore", PrimitiveType.Cylinder, new Vector3(0f, 0.01f, 1.018f), new Vector3(0.025f, 0.012f, 0.025f), darkMetal, Quaternion.Euler(90f, 0f, 0f));
            for (int port = -1; port <= 1; port++)
                AddPart(root, "Muzzle brake • port", PrimitiveType.Cylinder, new Vector3(0f, 0.01f + port * 0.025f, 0.985f), new Vector3(0.055f, 0.007f, 0.055f), darkMetal, Quaternion.Euler(90f, 0f, 0f));

            AddRoundedWeaponPart(root, "Stock • polymer", new Vector3(0f, 0.025f, -0.36f), new Vector3(0.13f, 0.13f, 0.37f), 0.045f, 3, darkMetal);
            AddRoundedWeaponPart(root, "Stock • shoulder pad", new Vector3(0f, 0.025f, -0.55f), new Vector3(0.135f, 0.135f, 0.045f), 0.02f, 2, metal);
            AddPart(root, "Magazine • curved body", PrimitiveType.Capsule, new Vector3(0f, -0.17f, 0.06f), new Vector3(0.09f, 0.17f, 0.105f), darkMetal, Quaternion.Euler(-10f, 0f, 0f));
            AddRoundedWeaponPart(root, "Magazine • floor plate", new Vector3(0f, -0.32f, 0.08f), new Vector3(0.095f, 0.035f, 0.105f), 0.015f, 2, metal);
            AddRoundedWeaponPart(root, "Pistol grip", new Vector3(0f, -0.13f, -0.12f), new Vector3(0.09f, 0.18f, 0.1f), 0.035f, 3, wood, Quaternion.Euler(10f, 0f, 0f));
            for (int groove = 0; groove < 5; groove++)
                AddRoundedWeaponPart(root, "Grip • checkering", new Vector3(0f, -0.09f - groove * 0.024f, -0.066f), new Vector3(0.07f, 0.009f, 0.008f), 0.003f, 1, darkMetal);

            AddRoundedWeaponPart(root, "Handguard • lower heat shield", new Vector3(0f, -0.055f, 0.34f), new Vector3(0.13f, 0.026f, 0.22f), 0.012f, 2, darkMetal);
            for (int vent = -2; vent <= 2; vent++)
            {
                AddPart(root, "Handguard • vent", PrimitiveType.Cylinder, new Vector3(0f, 0.054f, 0.25f + vent * 0.042f), new Vector3(0.014f, 0.014f, 0.014f), darkMetal);
                AddRoundedWeaponPart(root, "Receiver • side rib", new Vector3(0.095f, 0.012f, -0.04f + vent * 0.045f), new Vector3(0.018f, 0.018f, 0.025f), 0.006f, 1, metal);
            }

            AddRoundedWeaponPart(root, "Top rail", new Vector3(0f, 0.115f, 0.12f), new Vector3(0.12f, 0.025f, 0.42f), 0.01f, 2, darkMetal);
            for (int tooth = 0; tooth < 9; tooth++)
                AddRoundedWeaponPart(root, "Top rail • lug", new Vector3(0f, 0.134f, -0.07f + tooth * 0.047f), new Vector3(0.145f, 0.014f, 0.018f), 0.004f, 1, metal);
            AddPart(root, "Optic • mount", PrimitiveType.Cylinder, new Vector3(0f, 0.18f, 0.09f), new Vector3(0.11f, 0.022f, 0.11f), darkMetal);
            AddRoundedWeaponPart(root, "Optic • housing", new Vector3(0f, 0.23f, 0.09f), new Vector3(0.13f, 0.075f, 0.14f), 0.03f, 3, darkMetal);
            AddPart(root, "Optic • front lens", PrimitiveType.Cylinder, new Vector3(0f, 0.23f, 0.165f), new Vector3(0.045f, 0.008f, 0.045f), glass, Quaternion.Euler(90f, 0f, 0f));
            AddPart(root, "Optic • rear lens", PrimitiveType.Cylinder, new Vector3(0f, 0.23f, 0.015f), new Vector3(0.04f, 0.008f, 0.04f), glass, Quaternion.Euler(90f, 0f, 0f));

            AddRoundedWeaponPart(root, "Front sight • base", new Vector3(0f, 0.092f, 0.74f), new Vector3(0.055f, 0.035f, 0.05f), 0.01f, 2, darkMetal);
            AddRoundedWeaponPart(root, "Front sight • post", new Vector3(0f, 0.145f, 0.74f), new Vector3(0.016f, 0.11f, 0.022f), 0.006f, 2, metal);
            AddRoundedWeaponPart(root, "Rear sight • base", new Vector3(0f, 0.107f, 0.19f), new Vector3(0.12f, 0.04f, 0.055f), 0.012f, 2, darkMetal);
            AddRoundedWeaponPart(root, "Rear sight • notch", new Vector3(0f, 0.133f, 0.19f), new Vector3(0.055f, 0.018f, 0.025f), 0.007f, 2, metal);
            AddRoundedWeaponPart(root, "Trigger guard", new Vector3(0f, -0.12f, 0.01f), new Vector3(0.09f, 0.025f, 0.12f), 0.012f, 2, metal);
            AddPart(root, "Trigger", PrimitiveType.Cylinder, new Vector3(0f, -0.12f, 0.025f), new Vector3(0.012f, 0.035f, 0.012f), darkMetal, Quaternion.Euler(10f, 0f, 0f));
            AddPart(root, "Charging handle", PrimitiveType.Cylinder, new Vector3(0.105f, 0.075f, -0.005f), new Vector3(0.025f, 0.05f, 0.025f), metal, Quaternion.Euler(0f, 0f, 90f));
            AddRoundedWeaponPart(root, "Selector lever", new Vector3(0.105f, -0.035f, -0.02f), new Vector3(0.018f, 0.065f, 0.095f), 0.008f, 2, metal, Quaternion.Euler(0f, 0f, -18f));

            for (int side = -1; side <= 1; side += 2)
            {
                for (int fastener = 0; fastener < 3; fastener++)
                    AddPart(root, "Receiver • fastener", PrimitiveType.Cylinder, new Vector3(side * 0.098f, 0.015f, -0.08f + fastener * 0.09f), new Vector3(0.012f, 0.012f, 0.012f), metal, Quaternion.Euler(0f, 0f, 90f));
            }

            var muzzlePoint = new GameObject("Muzzle Point").transform;
            muzzlePoint.SetParent(root, false);
            muzzlePoint.localPosition = new Vector3(0f, 0.01f, 1.04f);
            muzzlePoint.localRotation = Quaternion.identity;
        }

        private static void ClearViewModelGeometry(Transform root)
        {
            for (int childIndex = root.childCount - 1; childIndex >= 0; childIndex--)
                Object.DestroyImmediate(root.GetChild(childIndex).gameObject);
        }

        private static void AddRoundedWeaponPart(Transform parent, string partName, Vector3 position, Vector3 size, float bevel, int subdivisions, Material material, Quaternion rotation = default)
        {
            var part = new GameObject(partName);
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localRotation = rotation == default ? Quaternion.identity : rotation;
            part.AddComponent<MeshFilter>().sharedMesh = TacticalEnvironmentUpgrade.GetRoundedBoxMesh(size, bevel, subdivisions);
            MeshRenderer renderer = part.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private static Material ConfigureWeaponMaterial(string materialName, Color color, float metallic, float smoothness)
        {
            Material material = GetOrCreateMaterial(materialName, color);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject AddPart(Transform parent, string partName, PrimitiveType shape, Vector3 position, Vector3 scale, Material material, Quaternion rotation = default)
        {
            GameObject part = GameObject.CreatePrimitive(shape);
            part.name = partName;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.transform.localRotation = rotation == default ? Quaternion.identity : rotation;
            Collider collider = part.GetComponent<Collider>();
            if (collider != null) Object.DestroyImmediate(collider);
            Renderer renderer = part.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return part;
        }

        private static void CreatePracticeTarget(string targetName, Vector3 position, Material boardMaterial, Material bullseyeMaterial)
        {
            GameObject board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.name = targetName;
            board.transform.position = position;
            board.transform.localScale = new Vector3(0.85f, 1.2f, 0.12f);
            PracticeTarget target = board.AddComponent<PracticeTarget>();
            SerializedObject serializedTarget = new SerializedObject(target);
            serializedTarget.FindProperty("targetRenderer").objectReferenceValue = board.GetComponent<Renderer>();
            serializedTarget.ApplyModifiedPropertiesWithoutUndo();
            board.GetComponent<Renderer>().sharedMaterial = boardMaterial;

            CreateTargetDisc(board.transform, "Outer Ring", new Vector3(0f, 0f, -0.068f), 0.34f, bullseyeMaterial);
            CreateTargetDisc(board.transform, "Inner Ring", new Vector3(0f, 0f, -0.078f), 0.2f, boardMaterial);
            CreateTargetDisc(board.transform, "Center", new Vector3(0f, 0f, -0.088f), 0.085f, bullseyeMaterial);

            GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cube);
            post.name = targetName + " Stand";
            post.transform.position = position + new Vector3(0f, -1.0f, 0.12f);
            post.transform.localScale = new Vector3(0.1f, 0.8f, 0.1f);
            post.GetComponent<Renderer>().sharedMaterial = bullseyeMaterial;
        }

        private static void CreatePracticeDummy(string dummyName, Vector3 position, Material bodyMaterial, Material detailMaterial)
        {
            GameObject dummy = new GameObject(dummyName);
            dummy.transform.position = position;
            PracticeTarget target = dummy.AddComponent<PracticeTarget>();
            SerializedObject serializedTarget = new SerializedObject(target);
            serializedTarget.FindProperty("maximumHealth").floatValue = 100f;
            GameObject torso = CreateDummyPart(dummy.transform, "Torso", PrimitiveType.Capsule, new Vector3(0f, 1.15f, 0f), new Vector3(0.72f, 0.62f, 0.42f), bodyMaterial);
            serializedTarget.FindProperty("targetRenderer").objectReferenceValue = torso.GetComponent<Renderer>();
            serializedTarget.ApplyModifiedPropertiesWithoutUndo();
            CreateDummyPart(dummy.transform, "Head", PrimitiveType.Sphere, new Vector3(0f, 2.05f, 0f), new Vector3(0.42f, 0.42f, 0.42f), detailMaterial);
            CreateDummyPart(dummy.transform, "Left Shoulder", PrimitiveType.Sphere, new Vector3(-0.48f, 1.48f, 0f), new Vector3(0.24f, 0.28f, 0.25f), bodyMaterial);
            CreateDummyPart(dummy.transform, "Right Shoulder", PrimitiveType.Sphere, new Vector3(0.48f, 1.48f, 0f), new Vector3(0.24f, 0.28f, 0.25f), bodyMaterial);
            CreateDummyPart(dummy.transform, "Left Leg", PrimitiveType.Capsule, new Vector3(-0.2f, 0.42f, 0f), new Vector3(0.29f, 0.72f, 0.32f), bodyMaterial);
            CreateDummyPart(dummy.transform, "Right Leg", PrimitiveType.Capsule, new Vector3(0.2f, 0.42f, 0f), new Vector3(0.29f, 0.72f, 0.32f), bodyMaterial);
        }

        private static GameObject CreateDummyPart(Transform parent, string partName, PrimitiveType shape, Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(shape);
            part.name = partName;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            return part;
        }

        private static void CreateTargetDisc(Transform parent, string discName, Vector3 localPosition, float radius, Material material)
        {
            AddPart(parent, discName, PrimitiveType.Cylinder, localPosition, new Vector3(radius, 0.012f, radius), material, Quaternion.Euler(90f, 0f, 0f));
        }

        private static void EnsureFolder(string parent, string name)
        {
            string path = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, name);
        }

        private static Material GetOrCreateMaterial(string materialName, Color color)
        {
            string assetPath = "Assets/STRIKE240/Materials/" + materialName + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (material != null) return material;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            material = new Material(shader) { color = color };
            AssetDatabase.CreateAsset(material, assetPath);
            return material;
        }
    }
}
