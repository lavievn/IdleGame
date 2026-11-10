# Hàn Vụ origin — story and identity implementation contract (2026-11-10)

Status: approved implementation scope; **story/runtime code NOT yet implemented**. Branch based on `development_0.1.7`; keep PR #12 animation independent. Read `AGENTS.md`, current Unity/C# rules, story design notes, Save rules, gameplay code, tests. Do not treat old design notes as runtime authority.

## 1. Randomized identity, one-time generation
- Main playable hero: randomized gender (current enum `Nam`/`Nu`), randomized 2–3-part Chinese/Hán-Việt name, **always `RaceType.NhanToc`**, multiple `linh căn` allowed per existing 1–4-root rule. Do not modify spirit root odds or combat math.
- Guardian/elder role `hanvu.guardian` (previous draft name Lục Bá): male **or** female, randomized name from appropriate gender pool. Human. Use an appropriate title and pronouns by gender; do not hardcode “Lục Bá” for a female. Guardian stays a genuinely ordinary person unless later plot explicitly decides otherwise.
- Companion role `hanvu.companion` (draft A Ninh): randomized gender **opposite** the main hero’s gender, randomized name from matching pool. The **role/ID** is stable regardless of display name. Do not infer gender from generated name alone.
- Generate identity only on **new-game creation**; persist names, explicit genders, race and origin across Save/Load, retry, death, map changes. Preserve prior saved hero names on migration, and don't assume previously stored NPC data exists. Be careful with SaveManager I/O tests and backward compatibility.
- `NameDatabase` current update on this branch: 20 surnames, 20 middle syllables, 20 male and 20 female given syllables. Structured 2–3-part names, surname first. Validate pronunciation/odd combinations in content review later; names are not canonical Chinese character mappings. Separate stable identifiers from display names.
- Do NOT change all races globally or `SynergyMath.GenerateRandomRace()` if it is also used for NPCs/monsters. Scope Human constraint specifically to main hero and first guardian.

## 2. First origin, multiple origins later
- First playable origin ID `hanvu`; future origin IDs must be supportable, not hardcoded to the global game.
- A safe, **zero-monster** starting map (reuse existing suitable map temporarily). Story/NPC dialogue/UI tutorial only; still allow appropriate manual travel. Current map title can temporarily act as world-map access/hint; don't redesign UI.
- Story sequence: narrator worries about guardian's worsening cough; travel manually to a forest map; complete 5 waves of **wolf-sprite normal beasts**; quest herbs guaranteed enough by the completion of wave 5 (quest progress independent of existing random production loot); map-title/world-map hint pulses to prompt manual return; on safe map, prepare medicine and receive guardian follow-up; companion has gone to seek another cure; travel through 1–2 existing maps; defeat final-wave elite; receive an ambiguous clue about missing companion.
- At this stage, the starting maps' monsters **all use existing wolf image**, **melee basic attacks only**, no ranged combat, learned martial skills or special attacks. Elite may differ in stats but no special skill. Scope to starting content, don't overwrite generic Monster templates/world behavior.
- Hero starts Human but may have multiple roots; do not tie eligibility to a fixed `linh căn`.
- Intro flow and tutorial must be non-blocking for the idle core: narrator short messages, prompt for inventory/map/journal when needed, never auto-travel or stall battles pending manual dialog. Support replayable journal. Stable quest IDs, flag grants exactly-once.
- Write **journal facts** separately from **in-world testimony/hero speculation**. The player may infer abduction, but missing companion's fate is not confirmed at slice end. Avoid inventing full final mystery yet.

## 3. Future systems, do not overbuild slice
- Author data-driven progression for quest stages, world events, revelations and random encounters only where required by the first vertical slice. No generic scripting interpreter or parallel quest frameworks prematurely.
- Design follow-on narrative targets: Chapter I ends with cultivation approaching **Kim Đan viên mãn**, with Nguyên Anh challenge only, not player guaranteed Nguyên Anh. Level 50–60 is a **target range**, not automatic realm conversion. Do not let legacy RankDatabase map total level directly to lore realm.
- Story’s long-term tragedy: a decision to save one beloved individual harms many others; actual truth held back, gradually revealed and potentially misread. Different origins must see different evidence, not necessarily share identical opening NPCs.
- Future NPC random events should be deterministic/persisted when significant, not rerolled on load. Use stable event and actor IDs.

## 4. Delivery workflow for Codex
1. Audit GameManager, StartMenu, EntityDataSO, `NameDatabase`, SaveManager, WorldMap/MonsterSpawner/MonsterController and current map/wave flow; clearly list current truth versus proposed changes.
2. Implement an isolated, smallest viable **one-origin vertical slice** on this branch, using existing APIs and safe placeholder visuals. Do not touch current art, prefab, scenes, GUIDs or camera unnecessarily.
3. Build tests for 2–3-part names (already updated in `Tests~/IdentityMapRegression.cs`), hero always Human, companion opposite gender, guardian any gender and Human, roots 1–4 preserved, Save/Load identity stability, no-spawn safe map, quest herb deterministic 5-wave grant, no double grants, elite reward only once, no auto travel, basic-melee wolf-only opening content.
4. Run available tests; any Unity/Windows verification unavailable must be marked NOT RUN. Document exact output, commit IDs, commands, legacy test issues and compatibility risks.
5. Keep changes isolated from draft animation PR #12, avoid force push/merge; create a draft PR when implementation is ready, request manual Unity validation.

## Important unresolved design details
- Hero new-game gender and race selection/roll path must be audited, not guessed.
- Is `completedWavesInMap` currently auto-advancing `mapNumber` or unlocking map? Preserve existing semantics until explicitly scoped changes are tested. Manual travel remains authoritative.
- Save schema migration and origin-selection UI need a backward-compatible approach.
- “Lục Bá”, “A Ninh” are role-based draft placeholders only, not permanent randomized display names.
