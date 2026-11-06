# World Qi Distribution V1

06/11/2026 · Development v0.9. Versioned environmental Qi table, not final world economy or geographic survey. No monster/loot/EXP/topology changes.

## Coordinate authority

The current `WorldMapArtwork.png` is1448×1086. `WorldMapPolygonSeeds.txt` provides all133 stable IDs with **top-left pixel coordinates**. `WorldMapCatalog.LoadIndexedSeedPositions` maps them to `PreviewPosition=(x/1448,1-y/1086)`. These are the actual currently used atlas centers; they are not invented from ID order. The UI selects/reveals `WorldMapPolygons.json` regions rather than rendering133 independent pin GameObjects.

`WorldMapPanelUI.Layout` sizes the artwork RectTransform to its exact source aspect, inside the available panel, anchored center/top. Image, fog, revealed polygons and outlines share that subtree. Zoom/pan applies to this subtree. `WorldMapPolygons.NormalizedPoint` converts screen→local RectTransform and then `(local-area.min)/area.size`; normalized origin is bottom-left. No additional crop/letterbox offset exists inside this exact-aspect image rect. Background/UI positioning does not change normalized map coordinates.

Export columns: `nodeId,sourceX,sourceY,normalizedX,normalizedY,coordinateStatus`. Native verification checks133/133 centers against `WorldMapCatalog.PreviewPosition`, their own actual click polygons and the imported sprite dimensions. No missing/duplicate ID. `coordinateStatus` retains `IMAGE_LABEL` for127 and `INFERRED` for6 (`TL-01,TL-17,TL-19,HT-13,TM-03,TM-13`). All source polygons remain historically `reviewed=false`; runtime registration is verified, not reclassified as manually approved geographic labels. No pins, polygons or links are moved.

At initial v0.9 handoff the user's unlabelled original map/illustrative heatmap were not found in the task workspace. The v1 debug output therefore uses the existing indexed artwork. The user supplied the illustrative heatmap after handoff; its registration audit is recorded below. V1 Qi numbers remain procedural, not extracted from image colors.

## Heatmap reference received after handoff

The supplied `15378.png` is1280×960, SHA256 `35adb6412db808d8a9a44ec629449c5db754d71a76fdf78c1deeb472f636a6bb`. Source artwork remains1448×1086, SHA256 `a4cc5889ef5a2431e19865da2caca2b0513b01ac789c58fa3bd7f221f3fc5602`. Both have4:3 aspect. Top-left pixel mapping is `referenceX=sourceX*1280/1448`, `referenceY=sourceY*960/1086`; bottom-left normalized coordinates stay unchanged.

Image-analysis audit excludes the frame/compass and uses SIFT ratio0.7 plus3px affine RANSAC. It finds1295 ratio matches/1290 inliers across all four interior quadrants, with median0.633px residual under simple uniform scaling (0.451px after fitting). Thus uniform registration is supported by interior map features, not merely matching borders. This measures matched-feature residual, not guaranteed accuracy at every node. It does not change the six inherited INFERRED centers or their provenance.

The image is now usable as a spatial visual reference for regional design. Visible warm clusters include the northwestern palace area, southwest settlement, southeast forest settlement and central lake settlement. The northeastern volcanic colors also mix underlying terrain and illustration; they do not alone establish a numerical Qi value. There is no calibrated scalar legend, so no color-to-Qi extraction or per-node numeric fidelity is claimed.

External task evidence contains `heatmap-reference-registration.json` and `heatmap-reference-node-registration.csv` with all133 stable IDs, source/reference coordinates, mapping status, inherited coordinate provenance and separately labelled current v1 tier/rate. Every row explicitly marks `qiExtractedFromImage=false`. The attachment is retained at its original task location; it is not installed as runtime art or sampled at runtime.

This input update leaves `world-qi:atlas-v1` version1, its fingerprint and all existing Save identities unchanged. Its procedural four-hotspot distribution is not presented as matching every warm region in the new image. A distribution designed around these illustrated regions must be exported under a new data/config version and preserve the supported v1 Save path, rather than silently replacing v1 semantics.

## Data and generator

- Runtime asset: `Assets/Resources/WorldQiDistributionV1.json`.
- ID `world-qi:atlas-v1`, configuration version1, generator version1, seed20261106.
- Offline tool: `Tools/generate_world_qi.py`.
- Reviewed overrides: `Tools/data/WorldQiOverridesV1.json`.
- Production configuration references the table ID/version and a SHA256 content fingerprint (line-ending normalized). Table version and production configuration version advance together; unsupported Save configuration versions are rejected instead of silently reinterpreted.

Example export, with the project root as current directory:

```powershell
python Tools/generate_world_qi.py --project-root . --output-dir .utmp/world-qi-export
```

The tool exports data, coordinate/Qi CSVs, summary, generator parameters and an optional PIL debug PNG. It refuses an output directory inside Assets and refuses changing an existing exported table under the same version. Installing a new reviewed config remains explicit. `--seed`, `--version`, `--overrides` are supported; seed/generator/tier changes require a new data/profile version. The override input is read without being rewritten. No generator runs on New/Load/Travel and no runtime image sampling occurs.

Private Python `random.Random(seed)` selects Gaussian parameters and eligible centers; Unity's global Random is never touched. A continuous field combines a low-frequency sinusoid, a finer sinusoid, nine medium Gaussian clusters, two strong hotspots and three cold pockets. Hotspot centers use actual node coordinates, at least0.2 normalized units from starts; current centers are TV-35 and TV-03. Strong Gaussian sigma0.105, amplitude3.8; cold centers TL-14,TL-29,TM-23, sigma0.095/amplitude−2.4. Smoothstep attenuation within0.19 of a start tends continuously to1.0Qi/s. Explicit start overrides then pin all five to tier2. Sample values are clamped to[0.6,5.0] and quantized using boundaries0.7/0.9/1.15/1.55/2.15/3.0/4.75.

| Tier | Qi/s | Label | Current node count |
|---|---:|---|---:|
| 0 | 0.6 | Khô cạn | 31 |
| 1 | 0.8 | Loãng | 10 |
| 2 | 1.0 | Bình thường | 32 |
| 3 | 1.3 | Khá | 23 |
| 4 | 1.8 | Dồi dào | 8 |
| 5 | 2.5 | Đậm đặc | 16 |
| 6 | 3.5 | Linh địa | 9 |
| 7 | 5.0 | Hotspot / Long mạch | 4 |

All133 entries have exact tier/rate agreement, valid coordinates, seed/version, override flag and provenance. Native validation rejects missing, duplicate, unknown, invalid coordinates/rates/start rates/version mismatches. Rates are immutable copies at runtime. The largest sampled near-pair difference at distance<0.085 is1.5Qi/s (5↔3.5 hotspot transition); no start sits at3.5/5. The field is spatially correlated, not an independent per-node roll. Geographic distance is in normalized map space, not surveyed metres. The table is a v0.9 experimental distribution, not approval of every region's economy.

## Absorption adapter and economy

`NodeQiRate` is **absolute environmental Qi/s**. The retained domain pipeline computes `density × absorptionCoefficient × efficiency`. Production keeps coefficient2/efficiency1 and supplies `density=NodeQiRate/2`, so advertised5Qi/s means actual5, not10. Start1Qi/s feeds refinement input cap1Qi/s with efficiency1, preserving thresholds[0,900,2100,3600] and the approximately60-minute target. This intentionally differs from historical v0.8 density1×coefficient2=2Qi/s reserve accumulation; historical profiles are untouched.

Hot5Qi/s still refines at most1Qi/s; excess4Qi/s accumulates before bottleneck and all5Qi/s afterward. Cold0.6Qi/s can constrain refinement to0.6/s without initial reserve. The engine does not infer cultivation from kills/EXP and does not change refinement because of heatmap tier. No technique/root/gear/boss bonus was added.

Travel first validates access, settles fractional time at its **old** source, then changes the node and resolves the new table entry. State/reserve/owner are retained; entry time is paused. All133 production IDs are required before activation; no fallback conceals missing entries. The inherited fallback0.5 density is explicitly reserved for existing legacy `worldMapVersion=0` with no stable node. Nonempty unknown IDs are rejected by the production binding, and invalid world Save IDs are rejected before canonical swap.

Character shows current map Qi/s, absorption/s and refinement input limit/s separately, plus foundation/bar/reserve/realm/stage/bottleneck/Level/EXP/energy and locked blood/trial. It does not advertise Tu vi+5/s or an unsupported ETA.

## Verification and remaining scope

Native full3660-second scheduler simulations cover all five starts,30/60/144FPS, checkpoint replay, deterministic repetition, hot and cold sources. Start Viên mãn≈3600.1s; no automatic breakthrough. MainGame verifies actual Travel attribution, pause/Load/Retry/New and independent EXP. Runtime config reload consumes no Unity Random value. Separate-process evidence and reproducibility hashes are listed in the v0.9 report.

World temperature, danger, monster strength, rare encounters, loot and long-mai events remain independent and unchanged. The four hotspots currently cluster in Tử Vân; future geographic/world-economy review may relocate them only through a new version. Six inferred source centers and all draft polygon provenance remain visible for later author review.
