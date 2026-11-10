# TASK: Optional story, free exploration and first choice-driven encounters

**Decision date:** 2026-11-10. **Authority:** user supersedes mandatory Hàn Vụ story travel gates. This is the next implementation handoff, not a claim code is already changed. Branch `feature/story-origin-thanhkhe-foundation`, PR #13 (draft). Keep animation PR #12 separate. Read `AGENTS.md`, existing origin report and code.

## Product rule — world first, optional story
The protagonist can explore/travel/farm in any location **normally available under world-map rules**, whether or not the player has engaged with the story. Story and opportunities are optional. Player has at least three valid responses to a discovered situation: participate, decline, or do nothing. `Ignore` is NOT equivalent to `Decline` and must not fabricate a choice.

Never use origin stage, companion rescue, herbal quest or NPC dialogue as a hard travel condition. Existing world map physical adjacency and appropriately grounded world progression gates still apply; do not silently unlock all 133 nodes or bypass boss restrictions. The safe village must be enterable/exitable according to adjacency, not dependent on quest progress. Because current `WorldMapCatalog.TravelBlockReason` uses `StoryOrigin.TravelOverride()`, check every path including return routes. The old override currently denies unlisted origin travel while `stage != Complete`, with 'Hãy theo chỉ dẫn...' message. Remove mandatory story locking while preserving intended map topology and map-clear mechanics (differentiate world gates from story gates).

## Minimal concrete implementation, in order
1. **Fix Hàn Vụ travel**: freely choose adjacent nodes subject to *world* conditions. Safe village may not require five combat waves. Do not silently auto-teleport, overwrite map unlocks, cancel current quest or reset story stage. Quest prompts are suggestions only. Ensure player can leave Hàn Vụ without talking to anyone or gathering herbs, go to other reachable maps and return.
2. **Optional participation state**: retain origin ID and NPC identity even if story ignored. Keep existing `Gather/Return/SearchForest/SearchElite/Complete` compatibility but separate actual quest participation/opt-out choice from passive location/wave observations as needed. No forced quest progression purely because player farms 5 wolf waves without opting into the story. If player intentionally starts the herbal objective, qualifying waves count and continue to checkpoint deterministically. Avoid adding a binary global 'story off' switch or overwriting save with new seed. NPC conversation can offer start/learn more/decline/close; close should be neutral.
3. **Event choice model**: define small extension of existing origin story/event data with stable event ID, conditions, offered options, optional no-response and deterministic effects. Start with 2-3 small replay-safe events, e.g. injured traveling cultivator (help, refuse, ignore); suspicious abandoned herb basket (inspect, leave, ignore); local merchant's rumor (ask, dismiss, ignore). Use existing resources/rewards only, or informational journal entries; no new item economy/currency or permanent buffs without balance. Don't invent lore canon that reveals the eventual mystery. If dialogue closes, don't mark it as rejection. No click-required modal that pauses/stalls idle combat.
4. **Replayable journal**: confirmed observed facts separated from NPC testimony, hero inference and explicit player choices. If player never saw an event, do not add it as discovered. Do not imply the companion was abducted or killed. NPC name/gender dynamic, stable IDs.
5. **Travel/quest/save integrity**: no duplicate grants, no rerolls on load, no 'auto complete on unrelated wave', no resetting progress when leaving/returning or defeating an ordinary wolf repeatedly. Strongly separate *map wave progress* from *quest progress*; maintain origin's existing five-wave deterministic herbs once opted in.
6. **UX**: existing map-label hint is subtle and dismissible; no permanent flashing when quest ignored. Player may disable story prompts without forfeiting quest progress; add a low-impact return-to-story affordance in journal only if existing UI supports it. Placeholder UI only, no asset/animation work.

## Tests and evidence
- New character starts Hàn Vụ and can depart *without* touching the quest; explore a normally accessible adjacent map, farm, return, save/load, continue.
- A neighboring map blocked for genuine world-map reasons stays blocked, with clear reason unrelated to the origin.
- Ignoring an encounter does not commit 'declined', does not grant rewards or advance the quest.
- Explicit refusal remains persisted and may have later consequences but does not soft-lock basic progression; reopening depending on repeat policy is consistent.
- Accepting/starting the herbal quest then earning five valid wave completions yields exactly five herbs; unrelated map waves cannot contribute; journal and UI are accurate after replay, death, retry and load.
- Existing Hàn Vụ seeded character/guardian/companion identities remain stable; backward compatibility with saves from prior PR #13 implementation; previous non-origin saves still unaffected.
- Elite story clue requires its true trigger, once only; leaving and returning doesn't fake completion.
- Idle combat, wave spawning, inventory, forge and cultivation retain prior behavior.
- Unity compile + targeted MainGame smoke + Development017 regression. Run Windows Player if available; clearly distinguish gameplay test from known `UnityPlayer.dll` teardown crash. Default-timing interactive playtest is NOT RUN unless truly performed.

## Deliverables
- Small focused commits directly to this feature branch, no merge or force-push; update draft PR #13 summary.
- Report `Docs/automation-reports/HAN_VU_OPTIONAL_STORY_REPORT.md` including exact files changed, test matrix, known remaining limitations and proposed follow-up for additional origins/random encounters.
- Do not refactor unrelated systems or replace art; prioritize fixing freedom of travel and correct non-participation semantics before broad event content.

## Design note
Some encounters may expire based on **explicit in-game simulation state** later, not wall-clock time or passive background execution implicitly. Do not add offline NPC event simulation until timing and save semantics are defined. Progress must remain achievable without reading any story.
