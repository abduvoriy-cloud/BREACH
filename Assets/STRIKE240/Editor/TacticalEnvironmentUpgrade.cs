using System;
using System.Collections.Generic;
using Strike240;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Strike240.Editor
{
    /// <summary>Replaces the starter arena shapes with a compact tactical training yard.</summary>
    public static class TacticalEnvironmentUpgrade
    {
        private const string ScenePath = "Assets/STRIKE240/Scenes/PlayerControllerDemo.unity";
        private const string EnvironmentRootName = "BREACH Tactical Environment";
        private const string LegacyEnvironmentRootName = "STRIKE 240 Tactical Environment";
        private const string EnvironmentAssetRoot = "Assets/STRIKE240/Environment";
        private const string MaterialFolder = "Assets/STRIKE240/Materials/Environment";
        private const string MeshFolder = "Assets/STRIKE240/Environment/Meshes";

        [MenuItem("BREACH/Upgrade Tactical Environment")]
        public static void UpgradeFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                EditorUtility.DisplayDialog("BREACH", "Create the Player Controller Demo scene first.", "OK");
                return;
            }

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            UpgradeScene(scene);
        }

        // Also used for an isolated Unity batch-mode validation run.
        public static void UpgradeForValidation()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            UpgradeScene(scene);
        }

        // Runs both visual setup passes without replacing the player or weapon-system objects.
        public static void UpgradeMilestone3ForValidation()
        {
            UpgradeForValidation();
            WeaponPrototypeSetup.UpgradeRifleVisualsForValidation();
        }

        private static void UpgradeScene(UnityEngine.SceneManagement.Scene scene)
        {
            if (Camera.main == null || GameObject.Find("Player") == null)
                throw new InvalidOperationException("The PlayerControllerDemo scene must contain its MainCamera and Player.");

            EnsureFolder("Assets/STRIKE240", "Environment");
            EnsureFolder(EnvironmentAssetRoot, "Meshes");
            EnsureFolder("Assets/STRIKE240/Materials", "Environment");

            GameObject currentEnvironment = GameObject.Find(EnvironmentRootName);
            if (currentEnvironment != null) UnityEngine.Object.DestroyImmediate(currentEnvironment);
            GameObject legacyEnvironment = GameObject.Find(LegacyEnvironmentRootName);
            if (legacyEnvironment != null) UnityEngine.Object.DestroyImmediate(legacyEnvironment);
            RemoveStarterArenaObjects();

            var environment = new GameObject(EnvironmentRootName);
            EnvironmentMaterials materials = CreateEnvironmentMaterials();

            CreateGround(environment.transform, materials);
            CreatePerimeter(environment.transform, materials);
            CreateTrainingBayStructure(environment.transform, materials);
            CreateCover(environment.transform, materials);
            CreateShippingContainer(environment.transform, materials);
            CreateWoodCrate(environment.transform, materials);
            CreateBarrels(environment.transform, materials);
            CreateGroundDetails(environment.transform, materials);
            CreateWallDetails(environment.transform, materials);
            ConfigureLighting(materials);

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = environment;
            Debug.Log("BREACH tactical environment upgraded. Player, weapon, HUD, targets, and input settings were preserved.");
        }

        private static void RemoveStarterArenaObjects()
        {
            string[] names =
            {
                "Practice Floor", "North Boundary", "South Boundary", "East Boundary", "West Boundary",
                "Low Cover A", "Low Cover B", "Center Barrier"
            };

            foreach (string objectName in names)
            {
                GameObject oldObject = GameObject.Find(objectName);
                if (oldObject != null) UnityEngine.Object.DestroyImmediate(oldObject);
            }
        }

        private static void CreateGround(Transform parent, EnvironmentMaterials materials)
        {
            CreateRoundedBox(parent, "Ground • Concrete slab", Vector3.zero, new Vector3(31.2f, 0.5f, 31.2f), 0.06f, materials.concrete, true, 4);

            // Floor expansion joints and a restrained painted firing lane.
            for (int coordinate = -10; coordinate <= 10; coordinate += 5)
            {
                CreateRoundedBox(parent, "Expansion joint • X", new Vector3(coordinate, 0.006f, 0f), new Vector3(0.018f, 0.012f, 30.4f), 0.004f, materials.joint, false, 1);
                if (coordinate != 0)
                    CreateRoundedBox(parent, "Expansion joint • Z", new Vector3(0f, 0.006f, coordinate), new Vector3(30.4f, 0.012f, 0.018f), 0.004f, materials.joint, false, 1);
            }

            CreateRoundedBox(parent, "Lane marking • Left", new Vector3(-1.05f, 0.009f, 0.8f), new Vector3(0.045f, 0.018f, 9f), 0.01f, materials.paint, false, 1);
            CreateRoundedBox(parent, "Lane marking • Right", new Vector3(1.05f, 0.009f, 0.8f), new Vector3(0.045f, 0.018f, 9f), 0.01f, materials.paint, false, 1);
            CreateRoundedBox(parent, "Firing line", new Vector3(0f, 0.012f, -1.5f), new Vector3(2.2f, 0.018f, 0.055f), 0.01f, materials.paint, false, 1);
            CreateDrainGrate(parent, materials, new Vector3(0f, 0.012f, -5.2f));
        }

        private static void CreatePerimeter(Transform parent, EnvironmentMaterials materials)
        {
            CreateRoundedBox(parent, "North • reinforced concrete wall", new Vector3(0f, 1.6f, 15f), new Vector3(31f, 3.2f, 0.6f), 0.12f, materials.concrete, true, 5);
            CreateRoundedBox(parent, "South • reinforced concrete wall", new Vector3(0f, 1.6f, -15f), new Vector3(31f, 3.2f, 0.6f), 0.12f, materials.concrete, true, 5);
            CreateRoundedBox(parent, "East • reinforced concrete wall", new Vector3(15f, 1.6f, 0f), new Vector3(0.6f, 3.2f, 31f), 0.12f, materials.concrete, true, 5);
            CreateRoundedBox(parent, "West • reinforced concrete wall", new Vector3(-15f, 1.6f, 0f), new Vector3(0.6f, 3.2f, 31f), 0.12f, materials.concrete, true, 5);

            // Coping gives the wall tops a clear architectural profile.
            CreateRoundedBox(parent, "North • wall coping", new Vector3(0f, 3.18f, 15f), new Vector3(31.2f, 0.18f, 0.78f), 0.07f, materials.darkConcrete, false, 3);
            CreateRoundedBox(parent, "South • wall coping", new Vector3(0f, 3.18f, -15f), new Vector3(31.2f, 0.18f, 0.78f), 0.07f, materials.darkConcrete, false, 3);
            CreateRoundedBox(parent, "East • wall coping", new Vector3(15f, 3.18f, 0f), new Vector3(0.78f, 0.18f, 31.2f), 0.07f, materials.darkConcrete, false, 3);
            CreateRoundedBox(parent, "West • wall coping", new Vector3(-15f, 3.18f, 0f), new Vector3(0.78f, 0.18f, 31.2f), 0.07f, materials.darkConcrete, false, 3);

            // Recessed-looking wall bands, spaced buttresses, and low ventilation grilles.
            CreateRoundedBox(parent, "North • recessed wall band", new Vector3(0f, 2.72f, 14.685f), new Vector3(29.8f, 0.12f, 0.035f), 0.015f, materials.darkConcrete, false, 1);
            CreateRoundedBox(parent, "South • recessed wall band", new Vector3(0f, 2.72f, -14.685f), new Vector3(29.8f, 0.12f, 0.035f), 0.015f, materials.darkConcrete, false, 1);
            CreateRoundedBox(parent, "East • recessed wall band", new Vector3(14.685f, 2.72f, 0f), new Vector3(0.035f, 0.12f, 29.8f), 0.015f, materials.darkConcrete, false, 1);
            CreateRoundedBox(parent, "West • recessed wall band", new Vector3(-14.685f, 2.72f, 0f), new Vector3(0.035f, 0.12f, 29.8f), 0.015f, materials.darkConcrete, false, 1);

            for (int z = -12; z <= 12; z += 6)
            {
                CreateRoundedBox(parent, "East • wall buttress", new Vector3(14.55f, 1.55f, z), new Vector3(0.42f, 3.05f, 0.52f), 0.09f, materials.darkConcrete, true, 3);
                CreateRoundedBox(parent, "West • wall buttress", new Vector3(-14.55f, 1.55f, z), new Vector3(0.42f, 3.05f, 0.52f), 0.09f, materials.darkConcrete, true, 3);
            }
        }

        private static void CreateTrainingBayStructure(Transform parent, EnvironmentMaterials materials)
        {
            // Infill closes the gap between the existing perimeter walls and the canopy.
            CreateRoundedBox(parent, "North • upper painted wall", new Vector3(0f, 3.53f, 15f), new Vector3(30.2f, 0.66f, 0.52f), 0.045f, materials.paintedWall, true, 3);
            CreateRoundedBox(parent, "South • upper painted wall", new Vector3(0f, 3.53f, -15f), new Vector3(30.2f, 0.66f, 0.52f), 0.045f, materials.paintedWall, true, 3);
            CreateRoundedBox(parent, "East • upper painted wall", new Vector3(15f, 3.53f, 0f), new Vector3(0.52f, 0.66f, 30.2f), 0.045f, materials.paintedWall, true, 3);
            CreateRoundedBox(parent, "West • upper painted wall", new Vector3(-15f, 3.53f, 0f), new Vector3(0.52f, 0.66f, 30.2f), 0.045f, materials.paintedWall, true, 3);

            // A low roof with exposed steel ribs makes the range read as an indoor facility.
            CreateRoundedBox(parent, "Training bay • ceiling", new Vector3(0f, 3.98f, 0f), new Vector3(30.4f, 0.28f, 30.4f), 0.08f, materials.ceiling, true, 4);
            for (int z = -12; z <= 12; z += 6)
                CreateRoundedBox(parent, "Ceiling • steel cross beam", new Vector3(0f, 3.79f, z), new Vector3(30.1f, 0.13f, 0.16f), 0.045f, materials.darkMetal, false, 2);

            // Recessed-looking fixture lenses use emission; four shadowless point lights provide the actual fill.
            for (int xIndex = -1; xIndex <= 1; xIndex++)
            {
                for (int zIndex = -1; zIndex <= 1; zIndex += 2)
                {
                    float x = xIndex * 8f;
                    float z = zIndex * 7f;
                    CreateRoundedBox(parent, "Ceiling • recessed light lens", new Vector3(x, 3.70f, z), new Vector3(1.55f, 0.035f, 0.34f), 0.045f, materials.fixture, false, 2);
                    if (xIndex != 0)
                        CreateTrainingLight(parent, new Vector3(x, 3.68f, z), new Color(0.78f, 0.84f, 0.88f));
                }
            }

            CreateServiceDoor(parent, materials);
        }

        private static void CreateServiceDoor(Transform parent, EnvironmentMaterials materials)
        {
            // Surface-mounted, non-interactive service door detail on the inner south wall.
            const float x = 9f;
            const float z = -14.64f;
            CreateRoundedBox(parent, "South wall • service door frame", new Vector3(x, 1.38f, z), new Vector3(2.65f, 2.82f, 0.15f), 0.08f, materials.darkMetal, false, 3);
            CreateRoundedBox(parent, "South wall • service door panel", new Vector3(x, 1.34f, z + 0.09f), new Vector3(2.32f, 2.58f, 0.09f), 0.06f, materials.paintedWall, false, 3);
            CreateRoundedBox(parent, "Service door • kick plate", new Vector3(x, 0.22f, z + 0.035f), new Vector3(2.16f, 0.28f, 0.035f), 0.018f, materials.darkMetal, false, 2);
            for (int rib = -1; rib <= 1; rib++)
                CreateRoundedBox(parent, "Service door • reinforcing rib", new Vector3(x + rib * 0.68f, 1.4f, z + 0.035f), new Vector3(0.045f, 1.72f, 0.028f), 0.012f, materials.darkMetal, false, 1);
            CreateRoundedBox(parent, "Service door • handle plate", new Vector3(x + 0.82f, 1.25f, z + 0.025f), new Vector3(0.18f, 0.38f, 0.035f), 0.025f, materials.darkMetal, false, 2);
            CreateRoundedBox(parent, "Service door • handle", new Vector3(x + 0.82f, 1.28f, z - 0.005f), new Vector3(0.12f, 0.035f, 0.045f), 0.015f, materials.metal, false, 2);
            CreateRoundedBox(parent, "Service door • access label", new Vector3(x - 0.62f, 2.28f, z + 0.025f), new Vector3(0.54f, 0.18f, 0.025f), 0.02f, materials.paint, false, 2);
        }

        private static void CreateTrainingLight(Transform parent, Vector3 position, Color color)
        {
            var lightObject = new GameObject("Ceiling • diffuse training light");
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.position = position;
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = 0.9f;
            light.range = 11f;
            light.shadows = LightShadows.None;
            light.renderMode = LightRenderMode.Auto;
        }

        private static void CreateCover(Transform parent, EnvironmentMaterials materials)
        {
            CreateRoundedBox(parent, "Concrete barricade • west", new Vector3(-5f, 0.57f, 0f), new Vector3(3.3f, 1.14f, 0.95f), 0.19f, materials.concrete, true, 5);
            CreateRoundedBox(parent, "Concrete barricade • east", new Vector3(5f, 0.57f, 4f), new Vector3(3.3f, 1.14f, 0.95f), 0.19f, materials.concrete, true, 5);
            CreateRoundedBox(parent, "Concrete firing barrier", new Vector3(0f, 0.91f, 8f), new Vector3(8f, 1.82f, 0.72f), 0.16f, materials.darkConcrete, true, 5);

            CreateRoundedBox(parent, "West barricade • safety inset", new Vector3(-5f, 0.83f, -0.482f), new Vector3(2.45f, 0.07f, 0.022f), 0.02f, materials.paint, false, 2);
            CreateRoundedBox(parent, "East barricade • safety inset", new Vector3(5f, 0.83f, 3.518f), new Vector3(2.45f, 0.07f, 0.022f), 0.02f, materials.paint, false, 2);

            // Repeated wood slats make the firing barrier read as layered construction.
            for (int plank = 0; plank < 5; plank++)
            {
                float y = 0.38f + plank * 0.24f;
                CreateRoundedBox(parent, "Barrier • timber facing", new Vector3(0f, y, 7.62f), new Vector3(7.55f, 0.18f, 0.09f), 0.035f, materials.wood, false, 2);
            }
            CreateRoundedBox(parent, "Barrier • end cap left", new Vector3(-3.85f, 0.92f, 8f), new Vector3(0.26f, 1.84f, 0.82f), 0.08f, materials.darkMetal, false, 3);
            CreateRoundedBox(parent, "Barrier • end cap right", new Vector3(3.85f, 0.92f, 8f), new Vector3(0.26f, 1.84f, 0.82f), 0.08f, materials.darkMetal, false, 3);
        }

        private static void CreateShippingContainer(Transform parent, EnvironmentMaterials materials)
        {
            const float x = 10f;
            const float z = 4f;
            CreateRoundedBox(parent, "Container • weathered steel shell", new Vector3(x, 1.32f, z), new Vector3(4.8f, 2.64f, 6.2f), 0.11f, materials.metal, true, 5);

            for (int rib = 0; rib < 22; rib++)
            {
                float ribZ = z - 2.85f + rib * 0.27f;
                CreateRoundedBox(parent, "Container • side corrugation", new Vector3(x - 2.425f, 1.32f, ribZ), new Vector3(0.07f, 2.42f, 0.105f), 0.024f, materials.ribbedMetal, false, 2);
            }

            for (int corner = -1; corner <= 1; corner += 2)
            {
                for (int end = -1; end <= 1; end += 2)
                {
                    CreateRoundedBox(parent, "Container • corner casting", new Vector3(x + corner * 2.34f, 1.32f, z + end * 3.02f), new Vector3(0.18f, 2.6f, 0.2f), 0.045f, materials.darkMetal, true, 3);
                }
            }

            CreateRoundedBox(parent, "Container • door frame", new Vector3(x, 1.34f, z + 3.14f), new Vector3(2.65f, 2.38f, 0.14f), 0.045f, materials.darkMetal, false, 3);
            CreateRoundedBox(parent, "Container • left door", new Vector3(x - 0.65f, 1.34f, z + 3.055f), new Vector3(1.16f, 2.18f, 0.06f), 0.025f, materials.metal, false, 3);
            CreateRoundedBox(parent, "Container • right door", new Vector3(x + 0.65f, 1.34f, z + 3.055f), new Vector3(1.16f, 2.18f, 0.06f), 0.025f, materials.metal, false, 3);
            for (int seam = -1; seam <= 1; seam++)
                CreateRoundedBox(parent, "Container • door rib", new Vector3(x + seam * 0.62f, 1.34f, z + 3.015f), new Vector3(0.035f, 2.1f, 0.035f), 0.012f, materials.darkMetal, false, 1);

            CreateRoundedBox(parent, "Container • identification plate", new Vector3(x, 2.44f, z - 3.12f), new Vector3(0.92f, 0.28f, 0.035f), 0.035f, materials.paint, false, 2);
            CreateRoundedBox(parent, "Container • hazard plate", new Vector3(x - 1.8f, 0.18f, z - 2.1f), new Vector3(0.52f, 0.07f, 0.045f), 0.015f, materials.paint, false, 1);
        }

        private static void CreateWoodCrate(Transform parent, EnvironmentMaterials materials)
        {
            const float x = -10f;
            const float z = -4f;
            CreateRoundedBox(parent, "Timber crate • core", new Vector3(x, 0.48f, z), new Vector3(1.45f, 0.96f, 1.35f), 0.09f, materials.wood, true, 4);
            for (int board = 0; board < 5; board++)
            {
                float y = 0.12f + board * 0.18f;
                CreateRoundedBox(parent, "Timber crate • front slat", new Vector3(x, y, z - 0.69f), new Vector3(1.31f, 0.14f, 0.045f), 0.025f, materials.wood, false, 2);
                CreateRoundedBox(parent, "Timber crate • side slat", new Vector3(x - 0.74f, y, z), new Vector3(0.045f, 0.14f, 1.21f), 0.025f, materials.wood, false, 2);
            }
            CreateRoundedBox(parent, "Timber crate • corner brace left", new Vector3(x - 0.59f, 0.49f, z - 0.725f), new Vector3(0.11f, 0.98f, 0.06f), 0.025f, materials.darkWood, false, 2);
            CreateRoundedBox(parent, "Timber crate • corner brace right", new Vector3(x + 0.59f, 0.49f, z - 0.725f), new Vector3(0.11f, 0.98f, 0.06f), 0.025f, materials.darkWood, false, 2);
        }

        private static void CreateBarrels(Transform parent, EnvironmentMaterials materials)
        {
            CreateBarrel(parent, materials, new Vector3(8f, 0f, -1.5f), "Fuel drum • A");
            CreateBarrel(parent, materials, new Vector3(8.75f, 0f, -2.1f), "Fuel drum • B");
        }

        private static void CreateBarrel(Transform parent, EnvironmentMaterials materials, Vector3 floorPosition, string barrelName)
        {
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = barrelName;
            body.transform.SetParent(parent, false);
            body.transform.position = floorPosition + Vector3.up * 0.62f;
            body.transform.localScale = new Vector3(0.72f, 0.62f, 0.72f);
            body.GetComponent<Renderer>().sharedMaterial = materials.metal;
            MarkStatic(body);

            for (int ring = 0; ring < 3; ring++)
            {
                GameObject band = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                band.name = barrelName + " • reinforcing ring";
                band.transform.SetParent(parent, false);
                band.transform.position = floorPosition + Vector3.up * (0.28f + ring * 0.33f);
                band.transform.localScale = new Vector3(0.738f, 0.035f, 0.738f);
                band.GetComponent<Renderer>().sharedMaterial = materials.darkMetal;
                UnityEngine.Object.DestroyImmediate(band.GetComponent<Collider>());
                MarkStatic(band);
            }
        }

        private static void CreateGroundDetails(Transform parent, EnvironmentMaterials materials)
        {
            // Small service hatches and drains add scale without adding active systems.
            CreateRoundedBox(parent, "Drain • west", new Vector3(-12f, 0.018f, -3f), new Vector3(0.44f, 0.028f, 1.4f), 0.045f, materials.darkMetal, false, 2);
            CreateRoundedBox(parent, "Drain • east", new Vector3(12f, 0.018f, 7f), new Vector3(0.44f, 0.028f, 1.4f), 0.045f, materials.darkMetal, false, 2);
            CreateRoundedBox(parent, "Service hatch", new Vector3(-12f, 0.018f, 10f), new Vector3(1.1f, 0.028f, 1.1f), 0.07f, materials.darkMetal, false, 3);

            for (int slat = -3; slat <= 3; slat++)
            {
                CreateRoundedBox(parent, "Drain • west slat", new Vector3(-12f + slat * 0.052f, 0.035f, -3f), new Vector3(0.018f, 0.018f, 1.28f), 0.006f, materials.metal, false, 1);
                CreateRoundedBox(parent, "Drain • east slat", new Vector3(12f + slat * 0.052f, 0.035f, 7f), new Vector3(0.018f, 0.018f, 1.28f), 0.006f, materials.metal, false, 1);
            }

            for (int stripe = 0; stripe < 6; stripe++)
            {
                CreateRoundedBox(parent, "Container • floor hazard marking", new Vector3(6.5f + stripe * 0.38f, 0.016f, 0.4f), new Vector3(0.16f, 0.025f, 0.9f), 0.018f, stripe % 2 == 0 ? materials.paint : materials.darkMetal, false, 1);
            }
        }

        private static void CreateDrainGrate(Transform parent, EnvironmentMaterials materials, Vector3 position)
        {
            CreateRoundedBox(parent, "Firing lane • drain frame", position, new Vector3(2.5f, 0.035f, 0.32f), 0.04f, materials.darkMetal, false, 2);
            for (int bar = 0; bar < 13; bar++)
                CreateRoundedBox(parent, "Firing lane • drain slat", position + new Vector3(-1.12f + bar * 0.187f, 0.022f, 0f), new Vector3(0.035f, 0.02f, 0.25f), 0.008f, materials.metal, false, 1);
        }

        private static void CreateWallDetails(Transform parent, EnvironmentMaterials materials)
        {
            // A single service panel and louvered vents give the perimeter human scale.
            CreateRoundedBox(parent, "North wall • service panel", new Vector3(-9f, 1.45f, 14.67f), new Vector3(1.45f, 1.55f, 0.08f), 0.08f, materials.darkMetal, false, 4);
            CreateRoundedBox(parent, "North wall • service panel face", new Vector3(-9f, 1.45f, 14.61f), new Vector3(1.28f, 1.38f, 0.035f), 0.05f, materials.metal, false, 3);
            for (int vent = 0; vent < 7; vent++)
                CreateRoundedBox(parent, "Service panel • vent", new Vector3(-9f, 1.05f + vent * 0.12f, 14.585f), new Vector3(0.85f, 0.025f, 0.02f), 0.01f, materials.darkMetal, false, 1);

            // Wall-mounted luminaires are emissive geometry; the yard stays lit by one sun.
            for (int lightIndex = -1; lightIndex <= 1; lightIndex++)
            {
                float x = lightIndex * 8f;
                CreateRoundedBox(parent, "North wall • light housing", new Vector3(x, 2.9f, 14.62f), new Vector3(0.72f, 0.16f, 0.12f), 0.06f, materials.darkMetal, false, 3);
                CreateRoundedBox(parent, "North wall • light lens", new Vector3(x, 2.88f, 14.55f), new Vector3(0.53f, 0.055f, 0.025f), 0.02f, materials.fixture, false, 2);
            }
        }

        private static void ConfigureLighting(EnvironmentMaterials materials)
        {
            GameObject sunObject = GameObject.Find("Sun Light");
            if (sunObject == null) sunObject = new GameObject("Sun Light");
            Light sun = sunObject.GetComponent<Light>();
            if (sun == null) sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.93f, 0.82f);
            sun.intensity = 0.55f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.82f;
            sun.shadowBias = 0.045f;
            sun.shadowNormalBias = 0.35f;
            // Shadow map resolution is configured by the active URP asset.
            sun.transform.rotation = Quaternion.Euler(43f, -32f, 0f);

            RenderSettings.skybox = materials.skybox;
            RenderSettings.sun = sun;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.34f, 0.38f, 0.40f);
            RenderSettings.ambientIntensity = 0.9f;
            RenderSettings.reflectionIntensity = 0.45f;
            RenderSettings.reflectionBounces = 1;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.49f, 0.56f, 0.61f);
            RenderSettings.fogStartDistance = 30f;
            RenderSettings.fogEndDistance = 65f;
        }

        private static GameObject CreateRoundedBox(Transform parent, string objectName, Vector3 position, Vector3 size, float bevel, Material material, bool collider, int subdivisions)
        {
            Mesh mesh = GetRoundedBoxMesh(size, bevel, subdivisions);
            var obj = new GameObject(objectName);
            obj.transform.SetParent(parent, false);
            obj.transform.position = position;
            obj.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = obj.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
            if (collider) obj.AddComponent<MeshCollider>().sharedMesh = mesh;
            MarkStatic(obj);
            return obj;
        }

        internal static Mesh GetRoundedBoxMesh(Vector3 size, float bevel, int subdivisions)
        {
            string key = string.Format("RoundedBox_{0}_{1}_{2}_{3}_{4}_{5}_{6}",
                Mathf.RoundToInt(size.x * 100f), Mathf.RoundToInt(size.y * 100f), Mathf.RoundToInt(size.z * 100f),
                Mathf.RoundToInt(bevel * 100f), subdivisions, "URP", "v1");
            string assetPath = MeshFolder + "/" + key + ".asset";
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
            if (mesh != null) return mesh;

            mesh = BuildRoundedBoxMesh(key, size, bevel, subdivisions);
            AssetDatabase.CreateAsset(mesh, assetPath);
            return mesh;
        }

        private static Mesh BuildRoundedBoxMesh(string meshName, Vector3 size, float bevel, int subdivisions)
        {
            subdivisions = Mathf.Clamp(subdivisions, 1, 8);
            Vector3 half = size * 0.5f;
            Vector3 inner = new Vector3(Mathf.Max(0f, half.x - bevel), Mathf.Max(0f, half.y - bevel), Mathf.Max(0f, half.z - bevel));
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            Vector3[] faceNormals = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };

            foreach (Vector3 faceNormal in faceNormals)
            {
                Vector3 tangent = Mathf.Abs(faceNormal.y) > 0.5f ? Vector3.right : Vector3.up;
                Vector3 bitangent = Vector3.Cross(faceNormal, tangent).normalized;
                int first = vertices.Count;

                for (int y = 0; y <= subdivisions; y++)
                {
                    float v = Mathf.Lerp(-1f, 1f, y / (float)subdivisions);
                    for (int x = 0; x <= subdivisions; x++)
                    {
                        float u = Mathf.Lerp(-1f, 1f, x / (float)subdivisions);
                        Vector3 surfacePoint = Vector3.Scale(faceNormal, half) + Vector3.Scale(tangent * u, half) + Vector3.Scale(bitangent * v, half);
                        Vector3 corePoint = new Vector3(
                            Mathf.Clamp(surfacePoint.x, -inner.x, inner.x),
                            Mathf.Clamp(surfacePoint.y, -inner.y, inner.y),
                            Mathf.Clamp(surfacePoint.z, -inner.z, inner.z));
                        Vector3 normal = (surfacePoint - corePoint).normalized;
                        if (normal.sqrMagnitude < 0.5f) normal = faceNormal;
                        vertices.Add(corePoint + normal * bevel);
                        normals.Add(normal);

                        float uvU = Mathf.Abs(tangent.x) > 0.5f ? size.x : Mathf.Abs(tangent.y) > 0.5f ? size.y : size.z;
                        float uvV = Mathf.Abs(bitangent.x) > 0.5f ? size.x : Mathf.Abs(bitangent.y) > 0.5f ? size.y : size.z;
                        uvs.Add(new Vector2((u + 1f) * 0.5f * uvU, (v + 1f) * 0.5f * uvV));
                    }
                }

                int row = subdivisions + 1;
                for (int y = 0; y < subdivisions; y++)
                {
                    for (int x = 0; x < subdivisions; x++)
                    {
                        int a = first + y * row + x;
                        int b = a + 1;
                        int c = a + row;
                        int d = c + 1;
                        AddOrientedTriangle(vertices, triangles, a, b, c, faceNormal);
                        AddOrientedTriangle(vertices, triangles, b, d, c, faceNormal);
                    }
                }
            }

            var mesh = new Mesh { name = meshName, indexFormat = vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }

        private static void AddOrientedTriangle(List<Vector3> vertices, List<int> triangles, int a, int b, int c, Vector3 expectedNormal)
        {
            Vector3 actual = Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]);
            if (Vector3.Dot(actual, expectedNormal) >= 0f)
            {
                triangles.Add(a); triangles.Add(b); triangles.Add(c);
            }
            else
            {
                triangles.Add(a); triangles.Add(c); triangles.Add(b);
            }
        }

        private static EnvironmentMaterials CreateEnvironmentMaterials()
        {
            var materials = new EnvironmentMaterials
            {
                concrete = GetSurfaceMaterial("Concrete_Weathered", new Color(0.50f, 0.54f, 0.54f), 0f, 0.18f, 11, 0.20f, SurfaceType.Concrete),
                darkConcrete = GetSurfaceMaterial("Concrete_Charcoal", new Color(0.29f, 0.34f, 0.35f), 0f, 0.2f, 12, 0.18f, SurfaceType.Concrete),
                paintedWall = GetSurfaceMaterial("Painted_Wall", new Color(0.38f, 0.43f, 0.43f), 0.04f, 0.24f, 13, 0.12f, SurfaceType.Concrete),
                ceiling = GetSurfaceMaterial("Ceiling_Concrete", new Color(0.30f, 0.34f, 0.35f), 0.02f, 0.19f, 14, 0.14f, SurfaceType.Concrete),
                metal = GetSurfaceMaterial("Steel_Painted", new Color(0.31f, 0.39f, 0.42f), 0.42f, 0.34f, 21, 0.14f, SurfaceType.Metal),
                ribbedMetal = GetSurfaceMaterial("Steel_Ribbed", new Color(0.22f, 0.30f, 0.33f), 0.5f, 0.4f, 22, 0.12f, SurfaceType.Metal),
                darkMetal = GetSurfaceMaterial("Steel_Dark", new Color(0.09f, 0.13f, 0.14f), 0.62f, 0.38f, 23, 0.12f, SurfaceType.Metal),
                wood = GetSurfaceMaterial("Timber_Weathered", new Color(0.39f, 0.27f, 0.15f), 0f, 0.24f, 31, 0.16f, SurfaceType.Wood),
                darkWood = GetSurfaceMaterial("Timber_Dark", new Color(0.22f, 0.14f, 0.085f), 0f, 0.18f, 32, 0.15f, SurfaceType.Wood),
                paint = GetSurfaceMaterial("Safety_Paint", new Color(0.78f, 0.56f, 0.17f), 0f, 0.28f, 41, 0.08f, SurfaceType.Paint),
                joint = GetSurfaceMaterial("Floor_Joint", new Color(0.12f, 0.16f, 0.17f), 0.12f, 0.25f, 42, 0.08f, SurfaceType.Metal),
                fixture = GetSurfaceMaterial("Fixture_Emission", new Color(0.75f, 0.84f, 0.84f), 0f, 0.45f, 43, 0.02f, SurfaceType.Paint),
                skybox = GetSkybox()
            };
            return materials;
        }

        private static Material GetSurfaceMaterial(string name, Color baseColor, float metallic, float smoothness, int seed, float bumpScale, SurfaceType surfaceType)
        {
            string materialPath = MaterialFolder + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (material == null) material = new Material(shader) { name = name, enableInstancing = true };
            material.SetColor("_BaseColor", baseColor);
            material.SetColor("_Color", baseColor);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_BumpScale", bumpScale);
            material.SetTexture("_BaseMap", GetSurfaceTexture(name + "_Albedo", seed, surfaceType));
            material.SetTextureScale("_BaseMap", new Vector2(0.82f, 0.82f));
            material.SetTexture("_BumpMap", GetNormalTexture(name + "_Normal", seed, surfaceType));
            material.EnableKeyword("_NORMALMAP");

            if (name == "Fixture_Emission")
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", baseColor * 0.35f);
            }

            if (AssetDatabase.LoadAssetAtPath<Material>(materialPath) == null)
                AssetDatabase.CreateAsset(material, materialPath);
            else
                EditorUtility.SetDirty(material);
            return material;
        }

        private static Texture2D GetSurfaceTexture(string name, int seed, SurfaceType type)
        {
            string path = MaterialFolder + "/" + name + ".asset";
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            bool createAsset = texture == null;

            const int resolution = 256;
            if (createAsset) texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, true, false);
            texture.name = name;
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Bilinear;
            texture.anisoLevel = 4;

            for (int y = 0; y < resolution; y++)
            {
                float v = y / (float)resolution;
                for (int x = 0; x < resolution; x++)
                {
                    float u = x / (float)resolution;
                    float broad = TileNoise(u * 8f, v * 8f, 8, seed);
                    float medium = TileNoise(u * 32f, v * 32f, 32, seed + 1);
                    float fine = TileNoise(u * 128f, v * 128f, 128, seed + 2);
                    float variation = broad * 0.06f + medium * 0.035f + fine * 0.018f;
                    if (type == SurfaceType.Wood)
                    {
                        float grain = Mathf.Sin((v * 76f + broad * 0.08f) * Mathf.PI * 2f) * 0.045f;
                        variation += grain;
                    }
                    else if (type == SurfaceType.Metal)
                    {
                        variation += Mathf.Sin((v * 180f + broad * 0.03f) * Mathf.PI * 2f) * 0.012f;
                    }
                    else if (type == SurfaceType.Concrete && medium > 0.62f)
                    {
                        variation -= 0.045f; // subtle porous aggregate flecks
                    }

                    float brightness = 1f + variation;
                    // Keep the map neutral; the URP material supplies the surface tint.
                    texture.SetPixel(x, y, new Color(brightness, brightness, brightness, 1f));
                }
            }
            texture.Apply(true, false);
            if (createAsset) AssetDatabase.CreateAsset(texture, path);
            else EditorUtility.SetDirty(texture);
            return texture;
        }

        private static Texture2D GetNormalTexture(string name, int seed, SurfaceType type)
        {
            string path = MaterialFolder + "/" + name + ".asset";
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            bool createAsset = texture == null;

            const int resolution = 256;
            if (createAsset) texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, true, true);
            texture.name = name;
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Bilinear;
            texture.anisoLevel = 4;
            float amplitude = type == SurfaceType.Concrete ? 2.2f : type == SurfaceType.Wood ? 1.6f : 0.8f;
            for (int y = 0; y < resolution; y++)
            {
                float v = y / (float)resolution;
                for (int x = 0; x < resolution; x++)
                {
                    float u = x / (float)resolution;
                    Vector3 normal = SampleNormal(u, v, 1f / resolution, seed, amplitude);
                    texture.SetPixel(x, y, new Color(normal.x * 0.5f + 0.5f, normal.y * 0.5f + 0.5f, normal.z * 0.5f + 0.5f, 1f));
                }
            }
            texture.Apply(true, false);
            if (createAsset) AssetDatabase.CreateAsset(texture, path);
            else EditorUtility.SetDirty(texture);
            return texture;
        }

        private static Vector3 SampleNormal(float u, float v, float delta, int seed, float amplitude)
        {
            float left = SurfaceHeight(u - delta, v, seed);
            float right = SurfaceHeight(u + delta, v, seed);
            float down = SurfaceHeight(u, v - delta, seed);
            float up = SurfaceHeight(u, v + delta, seed);
            return new Vector3((left - right) * amplitude, (down - up) * amplitude, 1f).normalized;
        }

        private static float SurfaceHeight(float u, float v, int seed)
        {
            u = Mathf.Repeat(u, 1f);
            v = Mathf.Repeat(v, 1f);
            return TileNoise(u * 16f, v * 16f, 16, seed) * 0.52f
                 + TileNoise(u * 64f, v * 64f, 64, seed + 1) * 0.30f
                 + TileNoise(u * 128f, v * 128f, 128, seed + 2) * 0.18f;
        }

        private static float TileNoise(float x, float y, int period, int seed)
        {
            int x0 = Mathf.FloorToInt(x);
            int y0 = Mathf.FloorToInt(y);
            int x1 = (x0 + 1) % period;
            int y1 = (y0 + 1) % period;
            float tx = x - x0;
            float ty = y - y0;
            tx = tx * tx * (3f - 2f * tx);
            ty = ty * ty * (3f - 2f * ty);
            float a = Mathf.Lerp(HashNoise(x0, y0, period, seed), HashNoise(x1, y0, period, seed), tx);
            float b = Mathf.Lerp(HashNoise(x0, y1, period, seed), HashNoise(x1, y1, period, seed), tx);
            return Mathf.Lerp(a, b, ty) * 2f - 1f;
        }

        private static float HashNoise(int x, int y, int period, int seed)
        {
            unchecked
            {
                uint hash = (uint)(x + period * 31 + seed * 7919);
                hash ^= (uint)(y + period * 17) * 0x9E3779B9u;
                hash ^= hash >> 16;
                hash *= 0x7FEB352Du;
                hash ^= hash >> 15;
                hash *= 0x846CA68Bu;
                hash ^= hash >> 16;
                return (hash & 0x00FFFFFFu) / 16777215f;
            }
        }

        private static Material GetSkybox()
        {
            const string path = MaterialFolder + "/Skybox_Tactical.mat";
            Material skybox = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (skybox != null) return skybox;

            Shader shader = Shader.Find("Skybox/Procedural");
            if (shader == null) return null;
            skybox = new Material(shader) { name = "Skybox_Tactical" };
            skybox.SetColor("_SkyTint", new Color(0.52f, 0.62f, 0.69f));
            skybox.SetColor("_GroundColor", new Color(0.23f, 0.28f, 0.30f));
            skybox.SetFloat("_Exposure", 0.8f);
            skybox.SetFloat("_AtmosphereThickness", 1.1f);
            skybox.SetFloat("_SunSize", 0.025f);
            skybox.SetFloat("_SunSizeConvergence", 5f);
            AssetDatabase.CreateAsset(skybox, path);
            return skybox;
        }

        private static void EnsureFolder(string parent, string folderName)
        {
            string path = parent + "/" + folderName;
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, folderName);
        }

        private static void MarkStatic(GameObject obj)
        {
            GameObjectUtility.SetStaticEditorFlags(obj,
                StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
        }

        private enum SurfaceType { Concrete, Metal, Wood, Paint }

        private sealed class EnvironmentMaterials
        {
            public Material concrete;
            public Material darkConcrete;
            public Material paintedWall;
            public Material ceiling;
            public Material metal;
            public Material ribbedMetal;
            public Material darkMetal;
            public Material wood;
            public Material darkWood;
            public Material paint;
            public Material joint;
            public Material fixture;
            public Material skybox;
        }
    }
}
