# VIRUS9 Third-Party Asset Import Plan

Target: Windows PC course demo. Import only assets that are needed for the first playable route.

## Required Free-First Assets

- Character/NPC prototype:
  - Mini Modular Character Free Demo
  - Fallback: Human Character Dummy by Kevin Iglesias
- Humanoid animation prototype:
  - Human Basic Motions FREE by Kevin Iglesias
  - Add Kevin Iglesias free melee/spellcasting packs if available in the user's Unity account.
- Guardian prototype:
  - RPG Monster Duo PBR Polyart by Dungeon Mason
- UI/VFX/SFX polish:
  - Dark Theme UI
  - Free Quick Effects Vol. 1
  - Stylized Slash VFX
  - Whoosh Sound Effects Pack Lite Edition
  - Free Laser Weapons

## Import Rules

- Import packages under `Assets/ThirdParty/<PublisherOrPackName>/`.
- Keep original package folders intact unless Unity imports a fixed structure.
- Do not overwrite existing `Assets/Art`, `Assets/Scripts`, `Assets/Resources`, or scene files during import.
- After each import, run `Tools/Virus 9/Apply Playable Scene Migration`.
- Record exact package name, publisher, version, URL, and license in this file before final submission.

## Wiring Targets

- NPC prefabs should replace temporary shadow/ally visuals but keep `ShadowNPC` or `PrototypeShadowActor` on the scene root.
- Guardian prefabs should replace `GUARDIAN_Force` and `GUARDIAN_Memory` visuals but keep `GuardianController`, `GuardianAttackController`, `CombatantHealth`, `NavMeshAgent`, and `EnemyJumpController`.
- Humanoid animations should retarget through the existing `PlayerHumanoid.controller` or a duplicated guardian controller.
- VFX/SFX should connect to `PlayerAttackController`, `GuardianAttackController`, and final gate events after the combat route is playable.
