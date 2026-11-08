# Unified Character Visual Contract — v0.9

One saved primary Hero has one stable default body identity across Sword/Bow/Staff/Unarmed and attack-mode changes. WeaponFamily remains the authority for basic attack mechanics, not character appearance. No new artwork or animation framework is introduced.

## Audited ownership

There is no separate Hero prefab in this checkout; the Hero is a serialized MainGame scene object with a UI `Image`. `CultivationSpriteBridge` owns/reuses a child Image named `Cultivation Sprite Body`, disables the old source Image and preserves HP/popup siblings. No SpriteRenderer or Animator is the current body authority. No scene or prefab edit is needed.

Previously `AttachHero` called `Init(false,ToClass(controller.attackMode))`, selecting Swordsman/Archer/Mage textures. Archer loaded prototype3-frame Normal/Attack/Hit sequences. `HeroController.ApplyAttackMode`, equipment refresh and SpawnHero rebound that selection during equip/unequip/New/Load/Retry. Character details are textual; no separate class portrait was found.

V0.9 binds `DefaultHeroVisualKey="Swordsman"` and uses the existing **`CultivationSprites/Swordsman_Normal.png`** only, which was the default New/Unarmed/Melee appearance. A mode rebind retains the exact existing Image and created Sprite object. Hero Attack/Hit requests retain their current presentation deadlines and root feedback/SFX but do not replace the body bitmap. The bridge still uses the existing900×600 canvas/source foot(300,10), scale and logical root; no bounds/name-driven combat workaround is added.

## Preserved mechanics and assets

CombatManager still owns target acquisition, attack interval, release, projectiles, snapshots, damage and hit timing. Equipment/WeaponBasicAttack still resolve Sword/Unarmed→Melee, Bow→RangedPhysical, Staff→RangedMagic. Staff basic shots use the existing `staffBasic` path and ordinary basic timing; their `magic` flag is false because that flag selects **legacy charged-magic flight**, not the damage category. The magical attack source remains intact. No charged spell is enabled by this refactor.

Monster identity/role/pose loading and fallback tints remain separate and unchanged. Old Hero Archer/Mage/Swordsman Attack/Hit textures and sheets are retained, including assets used by monster fallback and fixtures. No texture, GUID, SpriteRenderer, Animator Controller, projectile art or VFX asset is removed or created. Existing entry/body opacity and root movement/attack feedback remain; no new animations were designed.

Hero visual is derived presentation. No Save field or migration is added for sprite/class mode; old saved attack-mode compatibility remains as before. New/Load/Retry/Travel bind the existing logical Hero controller and its one body. No alternate Hero visual is instantiated for a weapon. Race/gender fallback colors on the original source Image are not weapon/class tint selection.

## Minimal future boundary

Existing calls `HeroController.PlayAttackFeedback → CultivationSpriteBridge.Attack/Hit/ResetPose` are the presentation boundary. Combat can later supply WeaponFamily, attack type, optional Skill ID, direction/target and combat state to a small adapter. Character identity must remain independent. Character animation, weapon animation, VFX and SFX may consume action metadata, but animation completion must not become the damage/cooldown authority without a separate design and test task. V0.9 adds no general animation system, rig, timeline, skills or martial arts production behavior.

## Verification

Mono tests verify exact same Image/Sprite across modes and real equipment handlers, action-state expiry, death/reuse, no extra body, missing legacy sample independence, monster pose separation, existing32 weapon mechanics and sprite priority/camera/feet regressions. Historical Archer sample animation assertions are explicitly replaced by unified-identity tests; actual pose/timer/damage/range assertions are retained.

Native MainGame verifies the same body/texture through Sword→Bow→Staff→Unarmed→Sword, real single-Hero damage, primary-owned Bow/Staff projectiles with the correct snapshot type, and Save/Load/Retry/Travel/New. It checks one named body Image, unchanged logical/combat/camera binding and captures each weapon. These are real Unity visuals; editor method/hit-test automation is not Windows native mouse proof. Exact results and Player limitations belong to the v0.9 report.
