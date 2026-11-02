using System;
using Strike240;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Strike240.Editor
{
    /// <summary>Builds the separate DUST SECTOR competitive layout from the working demo scene.</summary>
    public static class DustSectorMapSetup
    {
        private const string DemoScenePath = "Assets/STRIKE240/Scenes/PlayerControllerDemo.unity";
        private const string DustSectorScenePath = "Assets/STRIKE240/Scenes/DustSector.unity";
        private const string MaterialFolder = "Assets/STRIKE240/Materials/DustSector";
        private const string MeshFolder = "Assets/STRIKE240/Environment/Meshes";

        [MenuItem("BREACH/Create DUST SECTOR Map")]
        public static void GenerateFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(DustSectorScenePath) != null)
            {
                EditorUtility.DisplayDialog("DUST SECTOR", "DustSector.unity already exists. It was left unchanged to protect the existing map scene.", "OK");
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(DemoScenePath) == null)
            {
                EditorUtility.DisplayDialog("DUST SECTOR", "PlayerControllerDemo.unity is required as the gameplay source scene.", "OK");
                return;
            }

            UnityEngine.SceneManagement.Scene source = EditorSceneManager.OpenScene(DemoScenePath, OpenSceneMode.Single);
            GenerateScene(source);
        }

        // Entry point for Unity batch-mode generation and validation.
        public static void GenerateForValidation()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(DustSectorScenePath) != null)
                throw new InvalidOperationException("DustSector.unity already exists; refusing to replace it during validation.");
            UnityEngine.SceneManagement.Scene source = EditorSceneManager.OpenScene(DemoScenePath, OpenSceneMode.Single);
            GenerateScene(source);
        }

        private static void GenerateScene(UnityEngine.SceneManagement.Scene scene)
        {
            GameObject player = GameObject.Find("Player");
            Camera playerCamera = Camera.main;
            if (player == null || playerCamera == null || playerCamera.GetComponentInParent<FirstPersonController>() == null)
                throw new InvalidOperationException("The demo source scene must contain its player controller and MainCamera.");

            EnsureFolder("Assets/STRIKE240/Materials", "DustSector");
            EnsureFolder("Assets/STRIKE240/Environment", "Meshes");
            RemoveCopiedPracticeRange();
            RemoveCopiedEnvironment();

            MapMaterials materials = CreateMaterials();
            Transform mapRoot = new GameObject("DUST SECTOR | Industrial Basin").transform;
            BuildGroundAndBoundary(mapRoot, materials);
            BuildLaneDividers(mapRoot, materials);
            BuildSiteA(mapRoot, materials);
            BuildSiteB(mapRoot, materials);
            BuildMid(mapRoot, materials);
            BuildCoveredPassages(mapRoot, materials);
            BuildIndustrialLandmarks(mapRoot, materials);
            BuildRouteCover(mapRoot, materials);
            CreateSpawnMarkers(mapRoot, player);
            ConfigureLighting();

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, DustSectorScenePath))
                throw new InvalidOperationException("Unity could not save the generated DUST SECTOR scene.");

            Selection.activeGameObject = player;
            Debug.Log("DUST SECTOR created at " + DustSectorScenePath + ". PlayerControllerDemo remains unchanged.");
        }

        private static void RemoveCopiedPracticeRange()
        {
            foreach (string objectName in new[]
            {
                "Practice Target Left", "Practice Target Center", "Practice Target Right", "Practice Dummy",
                "Practice Target Left Stand", "Practice Target Center Stand", "Practice Target Right Stand"
            })
            {
                GameObject existing = GameObject.Find(objectName);
                if (existing != null) UnityEngine.Object.DestroyImmediate(existing);
            }
        }

        private static void RemoveCopiedEnvironment()
        {
            foreach (string objectName in new[]
            {
                "BREACH Tactical Environment", "STRIKE 240 Tactical Environment",
                "Practice Floor", "North Boundary", "South Boundary", "East Boundary", "West Boundary",
                "Low Cover A", "Low Cover B", "Center Barrier"
            })
            {
                GameObject existing = GameObject.Find(objectName);
                if (existing != null) UnityEngine.Object.DestroyImmediate(existing);
            }
        }

        private static MapMaterials CreateMaterials()
        {
            var materials = new MapMaterials
            {
                sand = GetOrCreateMaterial("Dust_Sandstone", new Color(0.42f, 0.36f, 0.27f), 0f, 0.18f),
                concrete = GetOrCreateMaterial("Dust_Weathered_Concrete", new Color(0.39f, 0.39f, 0.35f), 0.02f, 0.22f),
                paleConcrete = GetOrCreateMaterial("Dust_Pale_Concrete", new Color(0.52f, 0.49f, 0.41f), 0f, 0.20f),
                oxide = GetOrCreateMaterial("Dust_Oxide_Steel", new Color(0.27f, 0.16f, 0.11f), 0.48f, 0.34f),
                darkSteel = GetOrCreateMaterial("Dust_Charcoal_Steel", new Color(0.095f, 0.12f, 0.12f), 0.68f, 0.42f),
                ochre = GetOrCreateMaterial("Dust_Safety_Ochre", new Color(0.54f, 0.34f, 0.12f), 0.06f, 0.26f),
                timber = GetOrCreateMaterial("Dust_Crate_Timber", new Color(0.28f, 0.19f, 0.12f), 0f, 0.25f),
                sitePaint = GetOrCreateMaterial("Dust_Site_Marking", new Color(0.67f, 0.45f, 0.17f), 0.02f, 0.2f)
            };
            ApplySurfaceMaps(materials.sand, "Concrete_Weathered");
            ApplySurfaceMaps(materials.concrete, "Concrete_Weathered");
            ApplySurfaceMaps(materials.paleConcrete, "Concrete_Charcoal");
            ApplySurfaceMaps(materials.oxide, "Steel_Painted");
            ApplySurfaceMaps(materials.darkSteel, "Steel_Dark");
            ApplySurfaceMaps(materials.timber, "Timber_Weathered");
            return materials;
        }

        private static void BuildGroundAndBoundary(Transform parent, MapMaterials m)
        {
            CreateBlock(parent, "Basin floor | 104 x 80 m", new Vector3(0f, -0.25f, 0f), new Vector3(104f, 0.5f, 80f), 0.08f, m.sand, true);

            CreateBlock(parent, "North retaining wall", new Vector3(0f, 2.8f, 40f), new Vector3(104f, 5.6f, 0.7f), 0.12f, m.concrete, true);
            CreateBlock(parent, "South retaining wall", new Vector3(0f, 2.8f, -40f), new Vector3(104f, 5.6f, 0.7f), 0.12f, m.concrete, true);
            CreateBlock(parent, "West retaining wall", new Vector3(-52f, 2.8f, 0f), new Vector3(0.7f, 5.6f, 80f), 0.12f, m.concrete, true);
            CreateBlock(parent, "East retaining wall", new Vector3(52f, 2.8f, 0f), new Vector3(0.7f, 5.6f, 80f), 0.12f, m.concrete, true);

            for (int x = -42; x <= 42; x += 12)
            {
                CreateBlock(parent, "Perimeter | north coping", new Vector3(x, 5.62f, 40f), new Vector3(5.7f, 0.22f, 0.9f), 0.055f, m.darkSteel, false);
                CreateBlock(parent, "Perimeter | south coping", new Vector3(x, 5.62f, -40f), new Vector3(5.7f, 0.22f, 0.9f), 0.055f, m.darkSteel, false);
            }

            CreateBlock(parent, "North lane | dust line", new Vector3(0f, 0.014f, 31.5f), new Vector3(84f, 0.028f, 0.08f), 0.018f, m.ochre, false);
            CreateBlock(parent, "South lane | dust line", new Vector3(0f, 0.014f, -31.5f), new Vector3(84f, 0.028f, 0.08f), 0.018f, m.ochre, false);
        }

        private static void BuildLaneDividers(Transform parent, MapMaterials m)
        {
            // Three 6 m connector gates between the parallel routes prevent the lanes from becoming sealed corridors.
            BuildBrokenDivider(parent, "North to Mid | blast wall", 15.5f, m.concrete);
            BuildBrokenDivider(parent, "Mid to South | blast wall", -15.5f, m.concrete);

            // Short offset walls create choices at each connector and break spawn-to-spawn sightlines.
            CreateBlock(parent, "West connector | offset panel A", new Vector3(-28f, 1.35f, 7.2f), new Vector3(0.45f, 2.7f, 5.8f), 0.07f, m.paleConcrete, true);
            CreateBlock(parent, "West connector | offset panel B", new Vector3(-32f, 1.35f, -7.6f), new Vector3(0.45f, 2.7f, 5.3f), 0.07f, m.concrete, true);
            CreateBlock(parent, "East connector | offset panel A", new Vector3(28f, 1.35f, -7.2f), new Vector3(0.45f, 2.7f, 5.8f), 0.07f, m.paleConcrete, true);
            CreateBlock(parent, "East connector | offset panel B", new Vector3(32f, 1.35f, 7.6f), new Vector3(0.45f, 2.7f, 5.3f), 0.07f, m.concrete, true);
        }

        private static void BuildBrokenDivider(Transform parent, string name, float z, Material material)
        {
            CreateBlock(parent, name + " | west outer", new Vector3(-42f, 1.45f, z), new Vector3(14f, 2.9f, 0.55f), 0.09f, material, true);
            CreateBlock(parent, name + " | west inner", new Vector3(-15f, 1.45f, z), new Vector3(24f, 2.9f, 0.55f), 0.09f, material, true);
            CreateBlock(parent, name + " | east inner", new Vector3(15f, 1.45f, z), new Vector3(24f, 2.9f, 0.55f), 0.09f, material, true);
            CreateBlock(parent, name + " | east outer", new Vector3(42f, 1.45f, z), new Vector3(14f, 2.9f, 0.55f), 0.09f, material, true);
        }

        private static void BuildSiteA(Transform parent, MapMaterials m)
        {
            Vector3 center = new Vector3(-22f, 0f, 25f);
            CreateAreaMarker(parent, "Bombsite A | Layout Only", center);
            CreateSiteMarkings(parent, "Bombsite A | layout only", center, m.sitePaint, m.ochre);
            CreateBlock(parent, "A site | west cover", center + new Vector3(-6.1f, 0.62f, 1f), new Vector3(0.65f, 1.24f, 4.8f), 0.12f, m.concrete, true);
            CreateBlock(parent, "A site | north cover", center + new Vector3(-1.2f, 0.58f, 5.2f), new Vector3(5.2f, 1.16f, 0.65f), 0.12f, m.concrete, true);
            CreateCrateStack(parent, "A site | supply stack", center + new Vector3(3.8f, 0f, 1.4f), m.timber);
            CreatePipeRun(parent, "A site | condenser pipe", center + new Vector3(0f, 2.2f, -2f), center + new Vector3(4.4f, 2.2f, -2f), 0.19f, m.oxide);
            CreateSign(parent, "A site | wayfinding sign", "SITE A  /  CONDENSER YARD", center + new Vector3(0f, 3.3f, 5.8f), new Vector3(0f, 180f, 0f));
        }

        private static void BuildSiteB(Transform parent, MapMaterials m)
        {
            Vector3 center = new Vector3(22f, 0f, -25f);
            CreateAreaMarker(parent, "Bombsite B | Layout Only", center);
            CreateSiteMarkings(parent, "Bombsite B | layout only", center, m.sitePaint, m.ochre);
            CreateBlock(parent, "B site | east cover", center + new Vector3(6.1f, 0.62f, -1f), new Vector3(0.65f, 1.24f, 4.8f), 0.12f, m.concrete, true);
            CreateBlock(parent, "B site | south cover", center + new Vector3(1.2f, 0.58f, -5.2f), new Vector3(5.2f, 1.16f, 0.65f), 0.12f, m.concrete, true);
            CreateCrateStack(parent, "B site | ore pallets", center + new Vector3(-3.8f, 0f, -1.4f), m.timber);
            CreatePipeRun(parent, "B site | transfer line", center + new Vector3(-4.3f, 2.0f, 2.5f), center + new Vector3(0.2f, 2.0f, 2.5f), 0.16f, m.oxide);
            CreateSign(parent, "B site | wayfinding sign", "SITE B  /  ORE TRANSFER", center + new Vector3(0f, 3.3f, -5.8f), Vector3.zero);
        }

        private static void CreateSiteMarkings(Transform parent, string name, Vector3 center, Material paint, Material accent)
        {
            CreateBlock(parent, name + " | north stripe", center + new Vector3(0f, 0.018f, 6.2f), new Vector3(14.2f, 0.035f, 0.12f), 0.025f, paint, false);
            CreateBlock(parent, name + " | south stripe", center + new Vector3(0f, 0.018f, -6.2f), new Vector3(14.2f, 0.035f, 0.12f), 0.025f, paint, false);
            CreateBlock(parent, name + " | west stripe", center + new Vector3(-7.05f, 0.018f, 0f), new Vector3(0.12f, 0.035f, 12.4f), 0.025f, paint, false);
            CreateBlock(parent, name + " | east stripe", center + new Vector3(7.05f, 0.018f, 0f), new Vector3(0.12f, 0.035f, 12.4f), 0.025f, paint, false);
            CreateBlock(parent, name + " | site marker", center + new Vector3(0f, 0.025f, 0f), new Vector3(1.1f, 0.05f, 1.1f), 0.18f, accent, false);
        }

        private static void CreateAreaMarker(Transform parent, string name, Vector3 position)
        {
            var marker = new GameObject(name);
            marker.transform.SetParent(parent, false);
            marker.transform.position = position;
        }

        private static void BuildMid(Transform parent, MapMaterials m)
        {
            CreateAreaMarker(parent, "Mid | Central Exchange", Vector3.zero);
            CreateBlock(parent, "Mid | exchange floor inset", new Vector3(0f, 0.012f, 0f), new Vector3(19f, 0.024f, 16f), 0.09f, m.paleConcrete, false);
            CreateBlock(parent, "Mid | freight block west", new Vector3(-7f, 0.95f, -1.5f), new Vector3(2.4f, 1.9f, 5.2f), 0.16f, m.oxide, true);
            CreateBlock(parent, "Mid | freight block east", new Vector3(7f, 0.95f, 1.5f), new Vector3(2.4f, 1.9f, 5.2f), 0.16f, m.darkSteel, true);
            CreateBlock(parent, "Mid | low central cover", new Vector3(0f, 0.62f, 0f), new Vector3(4.8f, 1.24f, 1.0f), 0.16f, m.concrete, true);
            CreatePipeRun(parent, "Mid | overhead transfer pipe", new Vector3(-2.7f, 4.2f, 7.2f), new Vector3(2.7f, 4.2f, 7.2f), 0.18f, m.oxide);
            CreateSign(parent, "Mid | sector sign", "DUST SECTOR 07  /  MID EXCHANGE", new Vector3(0f, 3.8f, 8.8f), new Vector3(0f, 180f, 0f));
        }

        private static void BuildCoveredPassages(Transform parent, MapMaterials m)
        {
            CreateCanopyPassage(parent, "North | covered freight arcade", -1f, 25f, 22f, 9f, m);
            CreateCanopyPassage(parent, "South | covered pump gallery", 1f, -25f, 18f, 9f, m);
        }

        private static void CreateCanopyPassage(Transform parent, string name, float centerX, float centerZ, float length, float width, MapMaterials m)
        {
            float halfLength = length * 0.5f;
            float halfWidth = width * 0.5f;
            CreateBlock(parent, name + " | roof", new Vector3(centerX, 4.4f, centerZ), new Vector3(length, 0.32f, width), 0.09f, m.darkSteel, true);

            for (int xStep = -1; xStep <= 1; xStep++)
            {
                float x = centerX + xStep * halfLength * 0.88f;
                for (int side = -1; side <= 1; side += 2)
                    CreateBlock(parent, name + " | support", new Vector3(x, 2.1f, centerZ + side * (halfWidth - 0.25f)), new Vector3(0.38f, 4.2f, 0.38f), 0.08f, m.oxide, true);
            }

            for (int beam = -1; beam <= 1; beam++)
                CreateBlock(parent, name + " | roof rib", new Vector3(centerX + beam * halfLength * 0.38f, 4.15f, centerZ), new Vector3(0.14f, 0.22f, width - 0.2f), 0.04f, m.oxide, false);

            CreateBlock(parent, name + " | low west guide", new Vector3(centerX, 0.62f, centerZ - halfWidth + 0.2f), new Vector3(length - 1f, 1.24f, 0.36f), 0.09f, m.concrete, true);
            CreateBlock(parent, name + " | low east guide", new Vector3(centerX, 0.62f, centerZ + halfWidth - 0.2f), new Vector3(length - 1f, 1.24f, 0.36f), 0.09f, m.concrete, true);
        }

        private static void BuildIndustrialLandmarks(Transform parent, MapMaterials m)
        {
            // The sheds and towers sit behind the three main lanes, leaving both cross-passages open.
            CreateBlock(parent, "North apron | sifter house shell", new Vector3(37f, 2.7f, 25f), new Vector3(10f, 5.4f, 9f), 0.22f, m.concrete, true);
            CreateBlock(parent, "North apron | sifter roof cap", new Vector3(37f, 5.48f, 25f), new Vector3(10.7f, 0.24f, 9.7f), 0.12f, m.darkSteel, false);
            CreateBlock(parent, "South apron | pump house shell", new Vector3(-35f, 2.7f, -25f), new Vector3(12f, 5.4f, 9f), 0.22f, m.paleConcrete, true);
            CreateBlock(parent, "South apron | pump house roof cap", new Vector3(-35f, 5.48f, -25f), new Vector3(12.7f, 0.24f, 9.7f), 0.12f, m.oxide, false);

            CreateCylinder(parent, "North apron | water cistern", new Vector3(-40f, 3f, 29f), new Vector3(3.4f, 3f, 3.4f), m.darkSteel, true);
            CreateCylinder(parent, "South apron | settling tank", new Vector3(40f, 2.5f, -29f), new Vector3(3.8f, 2.5f, 3.8f), m.oxide, true);
            CreatePipeRun(parent, "North apron | elevated conduit", new Vector3(-35f, 5.7f, 30f), new Vector3(-21f, 5.7f, 30f), 0.22f, m.oxide);
            CreatePipeRun(parent, "South apron | elevated conduit", new Vector3(21f, 5.7f, -30f), new Vector3(35f, 5.7f, -30f), 0.22f, m.darkSteel);

            CreateCrateStack(parent, "North route | cargo pallets", new Vector3(12f, 0f, 33f), m.timber);
            CreateCrateStack(parent, "South route | cargo pallets", new Vector3(-12f, 0f, -33f), m.timber);
        }

        private static void BuildRouteCover(Transform parent, MapMaterials m)
        {
            CreateBlock(parent, "North route | offset blast shield", new Vector3(11f, 1.15f, 19f), new Vector3(5.4f, 2.3f, 0.62f), 0.14f, m.concrete, true);
            CreateBlock(parent, "North route | short crate barrier", new Vector3(-38f, 0.75f, 21f), new Vector3(4.2f, 1.5f, 1.2f), 0.16f, m.oxide, true);
            CreateBlock(parent, "South route | offset blast shield", new Vector3(-11f, 1.15f, -19f), new Vector3(5.4f, 2.3f, 0.62f), 0.14f, m.concrete, true);
            CreateBlock(parent, "South route | short crate barrier", new Vector3(38f, 0.75f, -21f), new Vector3(4.2f, 1.5f, 1.2f), 0.16f, m.oxide, true);

            CreateBlock(parent, "Mid west | concrete cover", new Vector3(-17f, 0.72f, -5f), new Vector3(3.8f, 1.44f, 1f), 0.16f, m.paleConcrete, true);
            CreateBlock(parent, "Mid east | concrete cover", new Vector3(17f, 0.72f, 5f), new Vector3(3.8f, 1.44f, 1f), 0.16f, m.concrete, true);
            CreateBlock(parent, "Mid north | service crate", new Vector3(2f, 0.85f, 10f), new Vector3(2.6f, 1.7f, 1.2f), 0.16f, m.oxide, true);
            CreateBlock(parent, "Mid south | service crate", new Vector3(-2f, 0.85f, -10f), new Vector3(2.6f, 1.7f, 1.2f), 0.16f, m.darkSteel, true);

            for (int side = -1; side <= 1; side += 2)
            {
                for (int post = -1; post <= 1; post += 2)
                {
                    Vector3 center = new Vector3(side * 46f, 1.5f, post * 5f);
                    CreateBlock(parent, "Spawn apron | short cover", center, new Vector3(2.5f, 3f, 0.55f), 0.13f, m.concrete, true);
                }
            }
        }

        private static void CreateSpawnMarkers(Transform parent, GameObject player)
        {
            Vector3 teamA = new Vector3(-46f, 0.05f, 0f);
            Vector3 teamB = new Vector3(46f, 0.05f, 0f);
            player.transform.SetPositionAndRotation(teamA, Quaternion.Euler(0f, 90f, 0f));

            CreateSpawnMarker(parent, "Team A Spawn", teamA, "TEAM A  /  WEST ENTRY", 90f);
            CreateSpawnMarker(parent, "Team B Spawn", teamB, "TEAM B  /  EAST ENTRY", -90f);
            CreateSign(parent, "DUST SECTOR | map identity", "DUST SECTOR 07  /  INDUSTRIAL BASIN", new Vector3(0f, 5.8f, 39.45f), Vector3.zero);
        }

        private static void CreateSpawnMarker(Transform parent, string name, Vector3 position, string label, float yaw)
        {
            var marker = new GameObject(name);
            marker.transform.SetParent(parent, false);
            marker.transform.position = position;
            marker.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            CreateBlock(parent, name + " | apron stripe", position + new Vector3(0f, -0.012f, 0f), new Vector3(6.8f, 0.025f, 0.14f), 0.025f,
                LoadMaterial("Dust_Safety_Ochre"), false);
            CreateSign(parent, name + " | sign", label, position + new Vector3(0f, 2.6f, yaw > 0f ? -5f : 5f), new Vector3(0f, yaw, 0f));
        }

        private static void CreateSign(Transform parent, string objectName, string text, Vector3 position, Vector3 rotation)
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (font == null) return;

            var sign = new GameObject(objectName);
            sign.transform.SetParent(parent, false);
            sign.transform.SetPositionAndRotation(position, Quaternion.Euler(rotation));
            TextMesh textMesh = sign.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.font = font;
            textMesh.fontSize = 32;
            textMesh.characterSize = 0.12f;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = new Color(0.88f, 0.82f, 0.66f);
            textMesh.richText = false;
        }

        private static void CreateCrateStack(Transform parent, string name, Vector3 floorPosition, Material material)
        {
            CreateBlock(parent, name + " | lower crate", floorPosition + new Vector3(0f, 0.62f, 0f), new Vector3(2.1f, 1.24f, 1.6f), 0.13f, material, true);
            CreateBlock(parent, name + " | upper crate", floorPosition + new Vector3(0.15f, 1.85f, -0.12f), new Vector3(1.7f, 1.2f, 1.45f), 0.12f, material, true);
            CreateBlock(parent, name + " | steel tie", floorPosition + new Vector3(-1.08f, 0.63f, 0f), new Vector3(0.08f, 1.28f, 1.65f), 0.025f, LoadMaterial("Dust_Charcoal_Steel"), false);
        }

        private static void CreatePipeRun(Transform parent, string name, Vector3 start, Vector3 end, float radius, Material material)
        {
            Vector3 delta = end - start;
            GameObject pipe = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pipe.name = name;
            pipe.transform.SetParent(parent, false);
            pipe.transform.position = (start + end) * 0.5f;
            pipe.transform.rotation = Quaternion.FromToRotation(Vector3.up, delta.normalized);
            pipe.transform.localScale = new Vector3(radius * 2f, delta.magnitude * 0.5f, radius * 2f);
            pipe.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(pipe.GetComponent<Collider>());
            MarkStatic(pipe);
        }

        private static void CreateCylinder(Transform parent, string name, Vector3 center, Vector3 scale, Material material, bool collider)
        {
            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.name = name;
            cylinder.transform.SetParent(parent, false);
            cylinder.transform.position = center;
            cylinder.transform.localScale = scale;
            cylinder.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider) UnityEngine.Object.DestroyImmediate(cylinder.GetComponent<Collider>());
            MarkStatic(cylinder);
        }

        private static GameObject CreateBlock(Transform parent, string name, Vector3 center, Vector3 size, float bevel, Material material, bool collider)
        {
            var block = new GameObject(name);
            block.transform.SetParent(parent, false);
            block.transform.position = center;
            float maxBevel = Mathf.Min(size.x, Mathf.Min(size.y, size.z)) * 0.24f;
            Mesh mesh = TacticalEnvironmentUpgrade.GetRoundedBoxMesh(size, Mathf.Clamp(bevel, 0.002f, maxBevel), 3);
            block.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = block.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
            if (collider)
            {
                BoxCollider box = block.AddComponent<BoxCollider>();
                box.size = size;
            }
            MarkStatic(block);
            return block;
        }

        private static void ConfigureLighting()
        {
            GameObject sunObject = GameObject.Find("Sun Light");
            if (sunObject == null) sunObject = new GameObject("Sun Light");
            Light sun = sunObject.GetComponent<Light>();
            if (sun == null) sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.85f, 0.66f);
            sun.intensity = 1.1f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.78f;
            sun.transform.rotation = Quaternion.Euler(38f, -32f, 0f);

            Material skybox = AssetDatabase.LoadAssetAtPath<Material>("Assets/STRIKE240/Materials/Environment/Skybox_Tactical.mat");
            if (skybox != null) RenderSettings.skybox = skybox;
            RenderSettings.sun = sun;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.42f, 0.47f, 0.50f);
            RenderSettings.ambientEquatorColor = new Color(0.35f, 0.34f, 0.30f);
            RenderSettings.ambientGroundColor = new Color(0.22f, 0.20f, 0.16f);
            RenderSettings.ambientIntensity = 0.9f;
            RenderSettings.reflectionIntensity = 0.45f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.56f, 0.50f, 0.40f);
            RenderSettings.fogStartDistance = 55f;
            RenderSettings.fogEndDistance = 125f;
        }

        private static Material LoadMaterial(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/" + name + ".mat");
        }

        private static void ApplySurfaceMaps(Material material, string sourceName)
        {
            string sourceRoot = "Assets/STRIKE240/Materials/Environment/" + sourceName;
            Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(sourceRoot + "_Albedo.asset");
            Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(sourceRoot + "_Normal.asset");
            if (albedo != null && material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", albedo);
                material.SetTextureScale("_BaseMap", new Vector2(1.25f, 1.25f));
            }
            if (normal != null && material.HasProperty("_BumpMap"))
            {
                material.SetTexture("_BumpMap", normal);
                material.SetFloat("_BumpScale", 0.65f);
                material.EnableKeyword("_NORMALMAP");
            }
        }

        private static Material GetOrCreateMaterial(string name, Color color, float metallic, float smoothness)
        {
            string path = MaterialFolder + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                material = new Material(shader) { name = name, enableInstancing = true };
                AssetDatabase.CreateAsset(material, path);
            }

            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void EnsureFolder(string parent, string name)
        {
            string path = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, name);
        }

        private static void MarkStatic(GameObject obj)
        {
            GameObjectUtility.SetStaticEditorFlags(obj,
                StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
        }

        private sealed class MapMaterials
        {
            public Material sand;
            public Material concrete;
            public Material paleConcrete;
            public Material oxide;
            public Material darkSteel;
            public Material ochre;
            public Material timber;
            public Material sitePaint;
        }
    }
}
