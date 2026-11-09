# Development 0.1.8 — Visual, Animation & UI Foundation (Cloud handoff)

Source baseline: `development_0.1.7` @ `fafc02d613a2e6a15c72a8dd48b935d2faa54f0e`.

## Ground truth from 0.1.7
- Eight equipment slots, instance IDs, rarity rules (Common primary stat only; Hoàng/Uncommon may grant a SkillID), Inventory Compare, Forge, loot, and Save/Load are implemented.
- Report: `Docs/automation-reports/DEVELOPMENT_017_REPORT.md`.
- Five groups of automated MainGame/Windows Player smoke tests reportedly passed **on the developer's Windows machine with WARP**. This is report evidence, not a fresh Cloud validation.
- Native mouse and default GPU shutdown behavior remain unverified.

## Goal
Improve the readability and replaceability of existing 2D presentation *without changing combat mechanics, item economics, or Save data*.

## First cloud implementation slice (code changes still pending)
1. Audit current sprite/animation binding for Hero, Monster, item loot, equipped weapon and skill slots. Identify the actual script paths, sprites and anchors. Avoid introducing a second animation framework.
2. Separate visual configuration/asset references from gameplay damage and positional state only where necessary. Preserve animation timing, Combat damage, skill casting/cooldown and projectile identity.
3. Make equipped items visually distinguishable in Inventory/Compare and on the HUD through existing icon slots and fallback sprites. Ensure missing sprite does not hide item details or break interaction.
4. Ensure weapon and effect anchors follow hero stance/facing. Do not change collider, hitbox, BattleArea or camera coordinates to compensate for art.
5. Improve clarity of selection/unavailable/active states using current UI style, without redesigning Inventories or opening new menus.
6. Add targeted non-visual code tests or static checks for selection/asset fallback; attach before/after screenshots only if an actual renderer is available.

## Deliberately out of scope
- Generating production anime/chibi sprites or model animations without reference assets.
- New skill/item content, new rarity tiers, damage/balance, story, Save migrations.
- Claims of Unity Play Mode or Windows build PASS from a Linux/source-only Cloud environment.

## Acceptance criteria for a Codex Cloud task
- All changed lines trace to the above scope and use the existing project patterns.
- Missing image fallback is safe and distinguishable; weapon icons match actual equipped item.
- No gameplay or Save contract changes; compilation warnings are attributed if testing is available.
- All smoke results explicitly name the environment. Any unavailable Unity/Windows checks are marked **NOT RUN**.
- Commit only targeted files on this branch and open a PR; do not merge or force-push.
- User will still perform final sprite/VFX and native input acceptance on Windows.

## Agent guidelines
Read `AGENTS.md`, `.agents/skills/idle-game-project-rules/SKILL.md`, `.agents/skills/karpathy-guidelines/SKILL.md`, `.agents/skills/unity-csharp-scripting/SKILL.md` and `.agents/skills/create-game-assets/SKILL.md` before source changes.

## Cloud status at handoff
This document and the Karpathy skill are committed using GitHub actions. **No Unity source code, runtime assets, build, or player verification has yet been implemented for 0.1.8.** A Codex Cloud execution session still needs to be started from this branch.
