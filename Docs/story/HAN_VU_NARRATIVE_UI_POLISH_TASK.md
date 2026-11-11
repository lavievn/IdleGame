# Hàn Vụ — Narrative polish and compact HUD (approved 2026-11-11)

Implementation handoff, not completed runtime. Repository lavievn/IdleGame, feature/story-origin-thanhkhe-foundation, draft PR #13; coordinate with optional-story requirement in Issue #14 and Docs/story/OPTIONAL_STORY_AND_ENCOUNTERS_TASK.md. Read AGENTS.md / project skills. User validated first origin playable and requests presentation/content polish.

## Narrative goal
Replace abrupt objective delivery and unmotivated “go find companion” with warm, natural, emotionally grounded opening. The long mystery is tragic but **opening clues should be understated and ambiguous**, not blood, kidnapping or rescue alarm. Primary protagonist, guardian and childhood companion all have **randomized display names and explicit genders**; generate once, persist, dynamically substitute in prose. Guardian can be female (do not call her “Lục Bá”); guardian is human, protagonist human with permitted multi-roots. The companion is opposite gender to hero, but **never require romance**.

### Sequence of proposed beats (write polished Vietnamese dialogue, not literal hardcoding of samples)
0. **At Hàn Vụ village**, guardian audibly/visually unwell, coughing intermittently; demonstrate care for hero rather than issuing quest. Example exchange for tone only:
   - Guardian: “Khụ... Trời trở lạnh rồi. Con đừng lên núi sớm quá, sương dày lắm.”
   - Hero: “Người lại ho rồi. Thuốc cũ còn không?”
   - Guardian: “Còn ít... Chắc nghỉ vài hôm là đỡ. Con cứ lo việc của con.”
   - Hero: “Lần trước thầy lang bảo lá bạc mọc bên Cô Hồn Lâm. Con đi một chuyến, trước khi trời tối sẽ về.”
   - Guardian: “Đừng vào sâu. [companion name] sáng nay có hỏi đường lên núi; ta dặn nó đi cùng người đốn củi, chẳng biết có nghe không.”
   - Hero: “Vậy con sẽ để ý. Người ở nhà đừng nhóm bếp, đợi con về.”
   Keep exchange short, ~5–7 lines, naturally paced, allow skip/revisit, never block travel/AFK game.
1. **First forest entry**: meet a woodcutter by roadside (**NPC with temporary text/name label and interaction hotspot**, not automatically omniscient line floating without context). Optional conversation first time: companion was seen carrying herb basket, asked directions to shady trail; seemed calm, not distressed. Player may speak, pass by, return later. Record testimony only upon interaction.
2. **After some actual exploration or waves**: discover an ordinary subtle clue (e.g. a note of the name of an unfamiliar herb, neatly stacked branches, a knotted ribbon used to mark trail). Mark **observation vs inference**. No bloody fabric or implication of immediate danger. Story clues need not all be combat drops; avoid making elite kill sole reason narrative exists. Use 2–3 discovery beats triggered by visited location / progress, with nonblocking text and reasonable repetition rules.
3. **Return home and medicine**: guardian improves but not fully, asks where the companion has gone, making hero's interest in finding them personally motivated; player can ignore it and pursue other goals. Do not announce “kidnapped” or “danger”.
4. **Later forest / elite at last wave**: retain existing combat encounter for gameplay; optional observation of a trail-marker or herb-related artifact near the elite's area, not gore or danger cue. The hero wonders why companion came this way, but does not conclude danger. Main story mystery starts with uncertainty, not an alarm.

Suggested subtle journal text:
- Confirmed: “Bên lối rẽ có một sợi dây buộc vào cành thông, thắt nút đôi.”
- Testimony: “Người đốn củi nói đã thấy [companion] hỏi đường lên phía bắc.”
- Inference: “Có thể [companion] đánh dấu lối về. Cũng có thể chỉ là dấu của người đi rừng.”
No events fabricated for skipped dialogue; journal must not claim player met woodcutter unless actually interacted.

### Choice semantics
Maintain free exploration and opt-in story from Issue #14. Narrative dialogue / quest acceptance separate from visiting/farming. Player choices: ask/participate, politely decline, ignore/close. **Ignore is not reject.** Story is optional; no hard story gate on WorldMap travel, no automatic teleport, no persistent flashing prompt for players who dismiss hints. Ensure old origin saves migrate sanely and previous quest stages do not soft-lock. Keep production loot and wave reward authority.

## HUD layout
Code audited in prior branch: `UIManager.StoryOrigin.cs` creates `HanVuStory` center-bottom 330x102 with buttons `HanVuJournal` and `HanVuDismiss`. `HudW3View.cs` constructs `cards[3]`, layouts all three even when absent.

1. Relocate two small buttons **“Nhật ký”** and **“Ẩn gợi ý”** directly under the visible Hero status block, next to the existing system menu cluster. Prefer small consistent buttons/icon+tooltip and existing `Surface`, `SmallButton`, W3 layout theme. Don't leave duplicate buttons in large narration panel. On narrow sizes avoid overlap and preserve click hitboxes. Preserve journal opening, hint hiding, pagination / relaunchability.
2. Keep character status card `0` only if Hero exists. Hide cards `1`, `2` when no valid *active/deployed* Hero at those slots. Do not use placeholder labels or draw empty panels. Handle subsequent addition/removal/respawn and 1/2/3 active Heroes, making layout compact rather than reserving empty space. Avoid disrupting ability HUD, map control or HP and mana.
3. Narrative UI: a compact, readable, optional dialogue presentation (speaker label and short utterance), not a giant 330px quest instructions slab. Can use neutral placeholder backgrounds. Keep one short line / sequence at a time, prior lines available in journal/dialogue replay, navigation if needed.
4. Preserve 600x338, 800x450, 1150+ viewport responsiveness and UI safe spacing; do not add production sprites, fonts or animation artwork. Only adjust targeted HUD/layout as required.

## Test acceptance
- Fresh New Game, opening dialogue is understandable without prior lore; correct randomized names/gender titles; safely skip, replay or leave without receiving quest; Save/Load mid-dialogue doesn't duplicate flags/reward.
- Woodcutter info only after first actual interaction, survives travel/reload; first encounter optional, no blocking.
- Noncombat clue triggers and any wave-progress clue are real, one-time and independent; not falsely labeled as combat loot. Elite clue remains soft, not a forced kidnapping accusation.
- Guardian conversation explains companion's trip **before** medicine turn-in; after medicine, renewed motivation, no contradiction regardless of companion gender or prior dialogue optionality.
- Player can farm freely, enter other legitimately unlocked map, return to story later. Honor Issue #14 scope and prevent quest-state gates.
- HUD: with 1 Hero exactly one status card, with 2 exactly 2, with 3 exactly 3; updates after death/respawn; Journal/Hide under status, system menu aligned, buttons fully interactive at 600, 800, 1150 presets. Check no duplicate UI overlay.
- Legacy Save/Load, equipment/combat/wave/tribulation and PR #12 animation unaffected.
- Run real Unity tests, Windows Player on actual available machine. Do not claim native cursor/full-speed playtest if only handler-based smoke. Explicitly report pre-existing UnityPlayer.dll exit crash separately.

## Delivery
Implement code and data, not docs only. Focused commits on the existing story feature branch; update draft PR #13 and reference Issues #14 and this task; add `Docs/automation-reports/HAN_VU_NARRATIVE_UI_POLISH_REPORT.md` with tests, images/screens if available, files/commits/known limits. No merge, no force-push.
