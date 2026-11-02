using System.Collections.Generic;
using Strike240;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace Strike240.Editor
{
    /// <summary>Non-destructive navigation bake and configurable offline bot spawner.</summary>
    public sealed class BotTestTools : EditorWindow
    {
        private const string MapRootName = "DUST SECTOR | Industrial Basin";
        private const string NavMeshAssetPath = "Assets/STRIKE240/Navigation/DustSector_BotNavMesh.asset";
        private const string SpawnGroupName = "BREACH Offline Test Bots";

        private int botCount = 3;
        private int maximumBotCount = 5;
        private BotDifficulty difficulty = BotDifficulty.Medium;
        private float forwardOffset = 18f;
        private float lateralSpacing = 3.2f;
        private bool isSpawning;

        [MenuItem("BREACH/Offline Bots/Test Bot Spawner")]
        public static void OpenWindow()
        {
            GetWindow<BotTestTools>("BREACH Offline Bots");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("DUST SECTOR Bot Test Setup", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Bake a NavMesh from the existing DUST SECTOR colliders, then add test bots to the active scene. The map is never regenerated or replaced.",
                MessageType.Info);

            GameObject mapRoot = GameObject.Find(MapRootName);
            BotNavigationSurface surface = mapRoot == null ? null : mapRoot.GetComponent<BotNavigationSurface>();
            if (mapRoot == null)
            {
                EditorGUILayout.HelpBox("Open Assets/STRIKE240/Scenes/DustSector.unity to configure map navigation.", MessageType.Warning);
            }
            else
            {
                string status = surface != null && surface.NavMeshData != null
                    ? $"NavMesh asset assigned for {NavMesh.GetSettingsNameFromID(surface.AgentTypeId)} (Agent Type ID {surface.AgentTypeId}). Save the scene before closing it."
                    : "NavMesh is not baked yet.";
                EditorGUILayout.HelpBox(status, surface != null && surface.NavMeshData != null ? MessageType.None : MessageType.Warning);
            }

            using (new EditorGUI.DisabledScope(mapRoot == null))
            {
                if (GUILayout.Button("Bake / Refresh DUST SECTOR NavMesh"))
                    BakeMapNavigation(mapRoot);
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Bot Spawning", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Spawning replaces the previous test bot group. The maximum defaults to 5.", MessageType.Info);
            maximumBotCount = EditorGUILayout.IntSlider("Maximum bot count", maximumBotCount, 1, 32);
            botCount = Mathf.Clamp(botCount, 1, maximumBotCount);
            botCount = EditorGUILayout.IntSlider("Number of bots", botCount, 1, maximumBotCount);
            difficulty = (BotDifficulty)EditorGUILayout.EnumPopup("Difficulty", difficulty);
            forwardOffset = EditorGUILayout.Slider("Spawn distance from player", forwardOffset, 8f, 30f);
            lateralSpacing = EditorGUILayout.Slider("Lateral spacing", lateralSpacing, 2f, 5f);

            using (new EditorGUI.DisabledScope(isSpawning || mapRoot == null || surface == null || surface.NavMeshData == null))
            {
                if (GUILayout.Button($"Spawn {botCount} {difficulty} bot{(botCount == 1 ? "" : "s")}"))
                    SpawnBots();
            }
        }

        private static void BakeMapNavigation(GameObject mapRoot)
        {
            if (mapRoot == null) return;

            BotNavigationSurface surface = mapRoot.GetComponent<BotNavigationSurface>();
            if (surface == null)
            {
                Undo.AddComponent<BotNavigationSurface>(mapRoot);
                surface = mapRoot.GetComponent<BotNavigationSurface>();
            }

            EnsureNavigationFolder();
            var sources = new List<NavMeshBuildSource>(512);
            var markups = new List<NavMeshBuildMarkup>();
            NavMeshBuilder.CollectSources(mapRoot.transform, ~0, NavMeshCollectGeometry.PhysicsColliders,
                NavMesh.GetAreaFromName("Walkable"), markups, sources);

            int colliderCount = 0;
            foreach (Collider collider in mapRoot.GetComponentsInChildren<Collider>(true))
            {
                if (collider != null && collider.enabled && !collider.isTrigger)
                    colliderCount++;
            }

            if (colliderCount == 0 || sources.Count == 0)
            {
                ReportNavigationError("No enabled, non-trigger map colliders were collected below the DUST SECTOR root.", mapRoot);
                return;
            }

            int agentTypeId = surface.AgentTypeId;
            NavMeshBuildSettings settings = NavMesh.GetSettingsByID(agentTypeId);
            if (settings.agentTypeID != agentTypeId)
            {
                ReportNavigationError($"Agent Type ID {agentTypeId} has no NavMesh build settings. Check Project Settings > AI Navigation.", mapRoot);
                return;
            }

            settings.agentRadius = 0.4f;
            settings.agentHeight = 1.8f;
            settings.agentClimb = 0.35f;
            settings.agentSlope = 45f;
            settings.overrideVoxelSize = true;
            settings.voxelSize = 0.1f;
            settings.minRegionArea = 1f;

            Bounds localBounds = new Bounds(new Vector3(0f, 2f, 0f), new Vector3(108f, 12f, 84f));
            string[] validation = settings.ValidationReport(localBounds);
            if (validation.Length > 0)
            {
                ReportNavigationError("NavMesh build settings are invalid: " + string.Join("; ", validation), mapRoot);
                return;
            }

            NavMeshData candidate = NavMeshBuilder.BuildNavMeshData(settings, sources, localBounds,
                mapRoot.transform.position, mapRoot.transform.rotation);
            if (candidate == null)
            {
                ReportNavigationError($"Unity failed to build NavMesh data from {sources.Count} collider sources.", mapRoot);
                return;
            }

            surface.UnregisterNavMesh();
            NavMeshQueryFilter filter = surface.CreateQueryFilter(NavMesh.AllAreas);
            bool candidateHasWalkableSurface = false;
            Vector3 validationPoint = GetMapValidationPoint(mapRoot);
            NavMeshDataInstance candidateInstance = default;
            try
            {
                candidateInstance = NavMesh.AddNavMeshData(candidate, mapRoot.transform.position,
                    mapRoot.transform.rotation);
                candidateHasWalkableSurface = candidateInstance.valid &&
                    NavMesh.SamplePosition(validationPoint, out _, 4f, filter);
            }
            finally
            {
                if (candidateInstance.valid)
                    NavMesh.RemoveNavMeshData(candidateInstance);
            }

            if (!candidateHasWalkableSurface)
            {
                DestroyImmediate(candidate);
                surface.EnsureRegistered();
                ReportNavigationError($"The bake produced no walkable polygon near Team A Spawn for Agent Type ID {agentTypeId}. Check that the basin floor has an enabled collider and that it is under the DUST SECTOR root.", mapRoot);
                return;
            }

            NavMeshData data = surface.NavMeshData;
            if (data == null) data = AssetDatabase.LoadAssetAtPath<NavMeshData>(NavMeshAssetPath);

            bool existingAssetMatchesAgent = false;
            if (data != null)
            {
                surface.AssignNavMeshData(data, agentTypeId);
                existingAssetMatchesAgent = surface.EnsureRegistered() &&
                    NavMesh.SamplePosition(validationPoint, out _, 4f, filter);
                surface.UnregisterNavMesh();
            }

            if (existingAssetMatchesAgent)
            {
                if (!NavMeshBuilder.UpdateNavMeshData(data, settings, sources, localBounds))
                {
                    data = candidate;
                    AssetDatabase.CreateAsset(data, AssetDatabase.GenerateUniqueAssetPath(NavMeshAssetPath));
                }
                else
                {
                    DestroyImmediate(candidate);
                }
            }
            else
            {
                data = candidate;
                AssetDatabase.CreateAsset(data, AssetDatabase.GenerateUniqueAssetPath(NavMeshAssetPath));
            }

            Undo.RecordObject(surface, "Assign DUST SECTOR NavMesh");
            surface.AssignNavMeshData(data, agentTypeId);
            if (!surface.EnsureRegistered() ||
                !NavMesh.SamplePosition(validationPoint, out _, 4f, surface.CreateQueryFilter(NavMesh.AllAreas)))
            {
                ReportNavigationError("The NavMesh asset was built but could not be registered or sampled after assignment. Do not spawn bots; refresh the bake and check the Console.", mapRoot);
                return;
            }

            EditorUtility.SetDirty(data);
            EditorUtility.SetDirty(surface);
            EditorSceneManager.MarkSceneDirty(mapRoot.scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[BREACH NavMesh] Baked and assigned Agent Type ID {agentTypeId} using {sources.Count} sources from {colliderCount} enabled map colliders. Save DustSector.unity to persist the surface reference.", mapRoot);
        }

        private void SpawnBots()
        {
            if (isSpawning) return;
            isSpawning = true;
            try
            {
                SpawnBotsInternal();
            }
            finally
            {
                isSpawning = false;
                Repaint();
            }
        }

        private void SpawnBotsInternal()
        {
            maximumBotCount = Mathf.Clamp(maximumBotCount, 1, 32);
            botCount = Mathf.Clamp(botCount, 1, maximumBotCount);
            Scene activeScene = SceneManager.GetActiveScene();
            GameObject mapRoot = GameObject.Find(MapRootName);
            if (mapRoot == null || mapRoot.scene != activeScene)
            {
                EditorUtility.DisplayDialog("BREACH Offline Bots", "The active scene must be DUST SECTOR.", "OK");
                return;
            }

            BotNavigationSurface surface = mapRoot.GetComponent<BotNavigationSurface>();
            if (surface == null || surface.NavMeshData == null)
            {
                ReportNavigationError("No NavMesh asset is assigned to DUST SECTOR. Bake the map before spawning bots.", mapRoot);
                return;
            }

            if (!surface.EnsureRegistered())
            {
                ReportNavigationError("The assigned NavMesh asset could not be registered. Refresh the DUST SECTOR bake before spawning bots.", mapRoot);
                return;
            }

            NavMeshQueryFilter filter = surface.CreateQueryFilter(NavMesh.AllAreas);
            GameObject teamASpawn = GameObject.Find("Team A Spawn");
            Vector3 mapCheckPoint = teamASpawn != null ? teamASpawn.transform.position : mapRoot.transform.position;
            if (!NavMesh.SamplePosition(mapCheckPoint, out _, 4f, filter))
            {
                ReportNavigationError($"No valid NavMesh position was found near Team A Spawn for Agent Type ID {surface.AgentTypeId}. Re-bake the map; no bots were created.", mapRoot);
                return;
            }

            FirstPersonController playerController = FindPlayerController(activeScene);
            if (playerController == null)
            {
                EditorUtility.DisplayDialog("BREACH Offline Bots", "No FirstPersonController was found in the active scene.", "OK");
                return;
            }

            if (!NavMesh.SamplePosition(playerController.transform.position, out NavMeshHit playerNavMeshHit,
                    4f, filter))
            {
                ReportNavigationError("The player is not on or near the assigned NavMesh, so bot spawning was cancelled.", playerController.gameObject);
                return;
            }

            var reservedPositions = new List<Vector3>(botCount);
            Vector3 forward = Vector3.ProjectOnPlane(playerController.transform.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            const int columns = 4;
            var spawnPositions = new List<Vector3>(botCount);
            for (int i = 0; i < botCount; i++)
            {
                int column = i % columns;
                int row = i / columns;
                float sideOffset = (column - (columns - 1) * 0.5f) * lateralSpacing;
                Vector3 desired = playerNavMeshHit.position +
                                  forward * (forwardOffset + row * 4f) +
                                  right * sideOffset;

                if (!TryFindSpawnPosition(desired, playerNavMeshHit.position, filter,
                        reservedPositions, out Vector3 spawnPosition))
                {
                    ReportNavigationError($"Could not find a free NavMesh position for bot {i + 1}. Reduce bot count or spacing; no bots were created.", mapRoot);
                    return;
                }

                spawnPositions.Add(spawnPosition);
                reservedPositions.Add(spawnPosition);
            }

            PlayerHealth playerHealth = playerController.GetComponent<PlayerHealth>();
            if (playerHealth == null)
            {
                Undo.AddComponent<PlayerHealth>(playerController.gameObject);
                playerHealth = playerController.GetComponent<PlayerHealth>();
            }

            List<Transform> existingSpawnGroups = FindSpawnGroups(activeScene);
            Transform spawnParent = FindOrCreateSpawnGroup(activeScene, existingSpawnGroups);

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Spawn BREACH test bots");

            // The dedicated container only holds bots created by this tool. Match the generated
            // names as well so unrelated objects someone placed in the container are preserved.
            RemovePreviouslySpawnedBots(existingSpawnGroups);

            for (int i = 0; i < botCount; i++)
            {
                Vector3 position = spawnPositions[i];

                var botObject = new GameObject($"BREACH {difficulty} Bot {i + 1}");
                Undo.RegisterCreatedObjectUndo(botObject, "Create BREACH test bot");
                botObject.SetActive(false);
                Undo.SetTransformParent(botObject.transform, spawnParent, "Parent BREACH test bot");
                botObject.transform.SetPositionAndRotation(position, Quaternion.LookRotation(-forward, Vector3.up));

                CapsuleCollider capsule = Undo.AddComponent<CapsuleCollider>(botObject);
                capsule.height = 1.8f;
                capsule.radius = 0.38f;
                capsule.center = Vector3.up * 0.9f;

                NavMeshAgent agent = Undo.AddComponent<NavMeshAgent>(botObject);
                agent.agentTypeID = surface.AgentTypeId;
                agent.radius = 0.4f;
                agent.height = 1.8f;
                agent.baseOffset = 0f;
                agent.speed = BotDifficultySettings.For(difficulty).movementSpeed;
                agent.enabled = false;

                Undo.AddComponent<BotVision>(botObject);
                BotAgent bot = Undo.AddComponent<BotAgent>(botObject);
                bot.Configure(playerHealth, difficulty, position, 8f, surface);

                AudioSource audioSource = Undo.AddComponent<AudioSource>(botObject);
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 1f;
                audioSource.dopplerLevel = 0f;
                audioSource.volume = 0.35f;
                EditorUtility.SetDirty(bot);
                botObject.SetActive(true);
            }

            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(activeScene);
            Selection.activeGameObject = spawnParent.gameObject;
            Debug.Log($"Spawned {botCount} {difficulty} test bots. Save the active scene to keep the setup.");
        }

        private static FirstPersonController FindPlayerController(Scene activeScene)
        {
            FirstPersonController[] controllers = Resources.FindObjectsOfTypeAll<FirstPersonController>();
            foreach (FirstPersonController controller in controllers)
            {
                if (controller != null && controller.gameObject.scene == activeScene)
                    return controller;
            }
            return null;
        }

        private static Transform FindOrCreateSpawnGroup(Scene activeScene, List<Transform> existingGroups)
        {
            if (existingGroups.Count > 0)
            {
                existingGroups[0].gameObject.SetActive(true);
                return existingGroups[0];
            }

            var group = new GameObject(SpawnGroupName);
            Undo.RegisterCreatedObjectUndo(group, "Create BREACH bot container");
            SceneManager.MoveGameObjectToScene(group, activeScene);
            return group.transform;
        }

        private static List<Transform> FindSpawnGroups(Scene activeScene)
        {
            var groups = new List<Transform>();
            foreach (GameObject root in activeScene.GetRootGameObjects())
            {
                foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
                {
                    if (candidate.name == SpawnGroupName)
                        groups.Add(candidate);
                }
            }
            return groups;
        }

        private static void RemovePreviouslySpawnedBots(List<Transform> spawnGroups)
        {
            foreach (Transform group in spawnGroups)
            {
                if (group == null) continue;
                BotAgent[] bots = group.GetComponentsInChildren<BotAgent>(true);
                foreach (BotAgent bot in bots)
                {
                    if (bot == null || bot.transform.parent != group || !HasSpawnerGeneratedName(bot.name))
                        continue;

                    Undo.DestroyObjectImmediate(bot.gameObject);
                }
            }
        }

        private static bool HasSpawnerGeneratedName(string objectName)
        {
            return objectName.StartsWith("BREACH Easy Bot ") ||
                   objectName.StartsWith("BREACH Medium Bot ") ||
                   objectName.StartsWith("BREACH Hard Bot ");
        }

        private static bool TryFindSpawnPosition(Vector3 desired, Vector3 reachableFrom,
            NavMeshQueryFilter filter, List<Vector3> reservedPositions, out Vector3 position)
        {
            const float sampleDistance = 1.5f;
            const float minimumSpacing = 2f;
            const int directionsPerRing = 8;

            for (int ring = 0; ring <= 3; ring++)
            {
                float radius = ring * 1.5f;
                int directionCount = ring == 0 ? 1 : directionsPerRing;
                for (int directionIndex = 0; directionIndex < directionCount; directionIndex++)
                {
                    float angle = directionIndex * Mathf.PI * 2f / directionsPerRing;
                    Vector3 offset = ring == 0
                        ? Vector3.zero
                        : new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
                    Vector3 candidate = desired + offset;
                    if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, sampleDistance, filter))
                        continue;

                    bool overlapsReserved = false;
                    foreach (Vector3 reserved in reservedPositions)
                    {
                        if ((reserved - hit.position).sqrMagnitude < minimumSpacing * minimumSpacing)
                        {
                            overlapsReserved = true;
                            break;
                        }
                    }
                    if (overlapsReserved) continue;

                    var path = new NavMeshPath();
                    if (!NavMesh.CalculatePath(reachableFrom, hit.position, filter, path) ||
                        path.status != NavMeshPathStatus.PathComplete)
                        continue;

                    position = hit.position;
                    return true;
                }
            }

            position = default;
            return false;
        }

        private static Vector3 GetMapValidationPoint(GameObject mapRoot)
        {
            GameObject teamASpawn = GameObject.Find("Team A Spawn");
            return teamASpawn != null && teamASpawn.scene == mapRoot.scene
                ? teamASpawn.transform.position
                : mapRoot.transform.position;
        }

        private static void ReportNavigationError(string message, Object context)
        {
            string fullMessage = "[BREACH NavMesh] " + message;
            Debug.LogError(fullMessage, context);
            EditorUtility.DisplayDialog("BREACH Navigation Error", message, "OK");
        }

        private static void EnsureNavigationFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/STRIKE240/Navigation"))
                AssetDatabase.CreateFolder("Assets/STRIKE240", "Navigation");
        }
    }

    /// <summary>
    /// Owns NavMesh registrations across Editor Play Mode transitions. Unity can keep the
    /// managed domain alive while reloading scenes, so clean the edit-time handle before
    /// transition and explicitly register the active scene surface on entry.
    /// </summary>
    [InitializeOnLoad]
    internal static class BotNavigationSurfacePlayModeLifecycle
    {
        static BotNavigationSurfacePlayModeLifecycle()
        {
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.ExitingEditMode:
                case PlayModeStateChange.ExitingPlayMode:
                case PlayModeStateChange.EnteredEditMode:
                    UnregisterLoadedSurfaces();
                    break;

                case PlayModeStateChange.EnteredPlayMode:
                    RegisterActiveSceneSurfaces();
                    break;
            }
        }

        private static void UnregisterLoadedSurfaces()
        {
            foreach (BotNavigationSurface surface in Resources.FindObjectsOfTypeAll<BotNavigationSurface>())
            {
                if (IsLoadedSceneObject(surface))
                    surface.UnregisterNavMesh();
            }
        }

        private static void RegisterActiveSceneSurfaces()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            foreach (BotNavigationSurface surface in Resources.FindObjectsOfTypeAll<BotNavigationSurface>())
            {
                if (surface == null || !surface.isActiveAndEnabled ||
                    !IsLoadedSceneObject(surface) || surface.gameObject.scene != activeScene ||
                    surface.NavMeshData == null)
                    continue;

                if (!surface.EnsureRegistered())
                    Debug.LogError($"[BREACH NavMesh] Failed to register the NavMesh asset for Agent Type ID {surface.AgentTypeId} after entering Play Mode.", surface);
            }
        }

        private static bool IsLoadedSceneObject(BotNavigationSurface surface)
        {
            if (surface == null) return false;
            Scene scene = surface.gameObject.scene;
            return scene.IsValid() && scene.isLoaded;
        }
    }
}
