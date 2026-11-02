# BREACH — Realistic Tactical Competitive FPS

## Install the prototype

1. Open the Unity 6 project and let scripts finish compiling.
2. Choose **BREACH → Install Weapon Prototype in Demo Scene**.
3. Open `Assets/STRIKE240/Scenes/PlayerControllerDemo.unity` if the editor switched to another scene.
4. Press **Play** and click the Game view if the mouse is unlocked.

To add only the humanoid dummy while preserving the current weapon and scene object references, open the desired scene and choose **BREACH → Add Practice Dummy to Open Scene**, then save the scene. The full installer also places a dummy when it rebuilds the weapon prototype.

The installer updates only the generated `PlayerControllerDemo` scene. It does not change the original `Assets/Scenes/SampleScene.unity` or project settings. It can be run again to rebuild the weapon and practice targets.

## Controls

- **W / A / S / D:** move
- **Mouse:** aim
- **Left mouse button:** fire
- **B:** switch between automatic and semi-automatic fire
- **R:** reload
- **Space:** jump
- **Left Shift:** sprint
- **Escape:** release the mouse; click the Game view to lock it again

The rifle starts with **30 rounds** and **90 reserve rounds**. Empty magazines reload automatically when reserve ammunition remains. Recoil kick, rate of fire, range, magazine size, and reload duration are inspector settings on `RifleWeapon`.

## Script locations

- `Scripts/Player/FirstPersonController.cs` — Milestone 1 movement, look, and recoil recovery hook.
- `Scripts/Weapons/RifleWeapon.cs` — fire modes, raycast firing, ammunition, reload, and recoil impulses.
- `Scripts/Weapons/WeaponAudio.cs` — AudioSource playback and generated placeholder shot/reload clips. Replace the optional clips in the Inspector with final audio later.
- `Scripts/UI/WeaponHUD.cs` — responsive center crosshair, hit confirmation, ammunition display, and fire-mode status.
- `Scripts/Targets/PracticeTarget.cs` — damageable target hit feedback and hit-point tracking.
- `Editor/WeaponPrototypeSetup.cs` — adds the procedural AK-inspired view model, weapon, HUD, range plates, and a simple humanoid practice dummy to the demo scene.

The view model and range plates are made from Unity primitive geometry with project-local URP materials. The ray starts at the camera center, so it follows the crosshair rather than the barrel. The weapon system uses the project's installed Input System package; no input package or project setting changes are required.

Rifle damage is configurable in the `RifleWeapon` Inspector. The range dummy starts with 100 health and exposes its remaining health and hit count in its `PracticeTarget` component. Targets remain in place for repeated aim practice; this prototype does not implement enemy AI or destruction.

## Tactical environment upgrade

Choose **BREACH → Upgrade Tactical Environment** to regenerate only the `BREACH Tactical Environment` root in `PlayerControllerDemo`. The editor tool preserves the player, rifle system, HUD, target plates, practice dummy, `SampleScene`, and project settings. It creates rounded concrete, painted wall, and steel geometry, a low ceiling canopy with structural ribs, an inner-wall service door, six emissive lens panels with four shadowless fill lights, tiled procedural surface and normal maps, wood details, a procedural skybox, and restrained distance fog. It uses one directional shadow light and no post-processing volume.

Choose **BREACH → Upgrade Rifle Visuals in Open Scene** from `PlayerControllerDemo` to rebuild only the generated rifle view-model children and attach the separate `WeaponVisualEffects` component to the existing `Weapon System`. This leaves the `AK Placeholder View Model` root, `Weapon System`, `RifleWeapon`, audio, HUD, and their scene references in place. This command replaces the generated geometry under the view-model root. The procedural rifle adds a softened receiver and stock, wood handguard, optic, rail, sights, magazine details, muzzle brake, and small fasteners. Muzzle flash and impact sparks use pre-created particle systems; the muzzle flash point light has no shadows.

Environment mesh assets are saved in `Assets/STRIKE240/Environment/Meshes`; the generated URP materials and small tiled textures are in `Assets/STRIKE240/Materials/Environment`. Geometry uses shared materials and static batching flags. The scene keeps the existing 1× render scale and 2048 main-light shadow map. The visual effects allocate their small particle pool once per weapon and do not allocate new particles per shot.

## Visual upgrade test

1. Open `Assets/STRIKE240/Scenes/PlayerControllerDemo.unity`.
2. Choose **BREACH → Upgrade Tactical Environment** and allow the editor to finish importing generated assets.
3. Choose **BREACH → Upgrade Rifle Visuals in Open Scene**. The command saves the scene after upgrading the view model and adding effects.
4. Press **Play**. Move around the range and check that the canopy and ceiling fixtures are visible. Fire at the practice plates and dummy to check the muzzle flash, surface sparks, hit feedback, ammo counter, recoil, and shooting sound. Reload to confirm that reload sound and ammunition behavior are unchanged.

The upgraded service door is a visual wall detail and does not open. Lighting is real-time and intentionally simple; no lightmaps, volumetric effects, or post-processing are generated. Geometry and FX remain placeholders for later authored art and final tuning.

## DUST SECTOR competitive map

Choose **BREACH → Create DUST SECTOR Map** to create `Assets/STRIKE240/Scenes/DustSector.unity`. The generator starts from `PlayerControllerDemo` to preserve the tested player, camera, weapon, and HUD references, removes the copied practice yard/targets in the new scene only, and saves the new map as a separate scene. It does not change `PlayerControllerDemo`, `SampleScene`, or the build settings.

The 104 × 80 m layout places Team A at the west entry, Team B at the east entry, Bombsite A in the northwest, Bombsite B in the southeast, and Mid in the center. North freight, mid, and south pump lanes connect through three cross-passages per divider. Covered freight and pump galleries, industrial sheds, tanks, conduit, crate stacks, and offset concrete cover shape the routes. The spawn markers are layout references for this single-player prototype; there are no team, bomb, or multiplayer systems.

To test, open `Assets/STRIKE240/Scenes/DustSector.unity` and press **Play**. The player starts at Team A. Use **WASD**, mouse look, **Space** to jump, and **Left Shift** to sprint. Walk through Mid, both cross-passages, each site, the covered routes, and around the industrial sheds; fire at the static cover to check the existing weapon effects. The editor generator uses rounded shared mesh assets, static environment geometry, primitive cylinder props, and BoxColliders on route-blocking structures.
