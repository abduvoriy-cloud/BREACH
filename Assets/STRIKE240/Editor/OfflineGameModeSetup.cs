using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Strike240.Editor
{
    /// <summary>Adds the offline match manager and HUD to the currently open DUST SECTOR scene.</summary>
    internal static class OfflineGameModeSetup
    {
        private const string MapRootName = "DUST SECTOR | Industrial Basin";
        private const string MatchRootName = "BREACH Offline Match";

        [MenuItem("BREACH/Offline Game/Setup DUST SECTOR Round Mode")]
        private static void SetupDustSectorRoundMode()
        {
            Scene scene = SceneManager.GetActiveScene();
            GameObject mapRoot = FindRoot(scene, MapRootName);
            if (mapRoot == null)
            {
                EditorUtility.DisplayDialog("BREACH Offline Game",
                    "Open Assets/STRIKE240/Scenes/DustSector.unity before setting up the round mode.", "OK");
                return;
            }

            FirstPersonController playerController = FindInScene<FirstPersonController>(scene);
            PlayerHealth playerHealth = playerController != null
                ? playerController.GetComponent<PlayerHealth>()
                : FindInScene<PlayerHealth>(scene);
            RifleWeapon playerWeapon = FindInScene<RifleWeapon>(scene);
            BotNavigationSurface surface = mapRoot.GetComponent<BotNavigationSurface>();
            Transform attackersSpawn = FindNamedTransform(scene, "Team A Spawn");
            Transform defendersSpawn = FindNamedTransform(scene, "Team B Spawn");
            BotAgent[] bots = FindAllInScene<BotAgent>(scene);

            if (playerController == null || playerHealth == null || playerWeapon == null || surface == null ||
                surface.NavMeshData == null || attackersSpawn == null || defendersSpawn == null || bots.Length == 0)
            {
                EditorUtility.DisplayDialog("BREACH Offline Game",
                    "The scene needs the player controller, PlayerHealth, rifle, baked NavMesh, both spawn markers, and at least one BotAgent.", "OK");
                return;
            }

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Setup BREACH Offline Round Mode");

            GameObject matchRoot = FindRoot(scene, MatchRootName);
            if (matchRoot == null)
            {
                matchRoot = new GameObject(MatchRootName);
                Undo.RegisterCreatedObjectUndo(matchRoot, "Create BREACH Offline Match");
                SceneManager.MoveGameObjectToScene(matchRoot, scene);
            }

            OfflineRoundManager manager = matchRoot.GetComponent<OfflineRoundManager>();
            if (manager == null) manager = Undo.AddComponent<OfflineRoundManager>(matchRoot);

            OfflineGameHUD hud = matchRoot.GetComponent<OfflineGameHUD>();
            if (hud == null) hud = Undo.AddComponent<OfflineGameHUD>(matchRoot);

            TeamMember playerMember = EnsureTeamMember(playerHealth.gameObject);
            playerMember.SetTeam(manager.PlayerTeam);
            EditorUtility.SetDirty(playerMember);

            foreach (BotAgent bot in bots)
            {
                TeamMember member = EnsureTeamMember(bot.gameObject);
                member.SetTeam(manager.BotTeam);
                EditorUtility.SetDirty(member);
            }

            manager.BindSceneReferences(playerHealth, playerController, playerWeapon,
                attackersSpawn, defendersSpawn, surface, bots);
            hud.Bind(manager);
            EditorUtility.SetDirty(manager);
            EditorUtility.SetDirty(hud);
            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(undoGroup);

            Selection.activeGameObject = matchRoot;
            Debug.Log($"[BREACH Match] Round mode wired for {scene.name}: player={manager.PlayerTeam}, bots={manager.BotTeam}, bots={bots.Length}. Save the scene to keep the setup.", matchRoot);
        }

        private static TeamMember EnsureTeamMember(GameObject actor)
        {
            TeamMember member = actor.GetComponent<TeamMember>();
            return member != null ? member : Undo.AddComponent<TeamMember>(actor);
        }

        private static GameObject FindRoot(Scene scene, string objectName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == objectName) return root;
            return null;
        }

        private static Transform FindNamedTransform(Scene scene, string objectName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                    if (child.name == objectName) return child;
            return null;
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            foreach (T component in Resources.FindObjectsOfTypeAll<T>())
                if (component != null && component.gameObject.scene == scene) return component;
            return null;
        }

        private static T[] FindAllInScene<T>(Scene scene) where T : Component
        {
            var results = new List<T>();
            foreach (T component in Resources.FindObjectsOfTypeAll<T>())
                if (component != null && component.gameObject.scene == scene) results.Add(component);
            return results.ToArray();
        }
    }
}
