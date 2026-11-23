# BREACH

Tactical FPS prototype in Unity 6 and C#. Offline player-versus-bots on a generated industrial map.

This is a learning project. The rifle, range, and map are built from Unity primitives, not finished game art.

## Open it

1. Install Unity `6000.6.7f1` with the Universal Render Pipeline.
2. Clone this repository and open the project folder in Unity Hub.
3. Wait for scripts to compile.
4. Open `Assets/STRIKE240/Scenes/DustSector.unity`.
5. Press Play and click the Game view to capture the mouse.

`PlayerControllerDemo.unity` is the firing range. `SampleScene.unity` is the untouched Unity template.

## Controls

| Input | Action |
| --- | --- |
| W A S D | Move |
| Mouse | Aim |
| Left mouse | Fire |
| B | Automatic or semi-automatic |
| R | Reload |
| Space | Jump |
| Left Shift | Sprint |
| Escape | Release the mouse |

The rifle starts with 30 rounds and 90 in reserve.

## What is in the prototype

- First-person movement, look, sprint, and jump.
- Hitscan rifle with recoil, reload, ammo, and a crosshair HUD.
- Practice targets.
- NavMesh bots with idle, patrol, investigate, chase, engage, and search.
- Offline attackers-versus-defenders rounds, timer, and score on Dust Sector.

Bomb planting and multiplayer are not implemented. Bombsite markers are layout only. The view model is named `AK Placeholder View Model` on purpose.

## Project layout

| Path | Contents |
| --- | --- |
| `Assets/STRIKE240/Scripts` | Player, weapon, AI, combat, and round code |
| `Assets/STRIKE240/Editor` | Menu commands that rebuild the range, rifle, and map |
| `Assets/STRIKE240/Scenes` | Dust Sector and the firing range |
| `Assets/STRIKE240/README.md` | Longer notes for the editor tools |

Editor commands live under the **BREACH** menu after the scripts compile.

## License

[MIT](LICENSE)
