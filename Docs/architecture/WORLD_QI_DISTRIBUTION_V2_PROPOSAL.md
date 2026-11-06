# World Qi Distribution v2 proposal

06/11/2026 · **IMPLEMENTED / VERIFIED data and native validator; V2 READY — ACTIVATION BLOCKED.** Default production remains `production:cultivation-v09` version1 with `world-qi:atlas-v1` version1. The v2 files live in Tools/data, outside Resources. They are not loaded by New/Load/Travel and are not a release or official long-mai/lore designation.

## Coordinate and reference authority

All133 stable IDs and coordinates are retained from the current1448×1086 indexed map. Source pixels are top-left; normalized coordinates are `(x/1448,1-y/1086)` with bottom-left origin. Existing UI RectTransform/click-polygon registration is unchanged. No topology, pin, link, wave, travel restriction or monster field is altered.

Provenance remains127 IMAGE_LABEL and6 INFERRED: TL-01/TL-17/TL-19/HT-13/TM-03/TM-13. No user approval of these six has been inferred. Native validation checks133 positions against actual runtime PreviewPosition; v0.9 click-cell verification remains the source audit.

The supplied heatmap `15378.png` is1280×960, SHA256 `35adb6412db808d8a9a44ec629449c5db754d71a76fdf78c1deeb472f636a6bb`. Its verified uniform scaling to the existing4:3 artwork is reused:1290 interior SIFT/RANSAC inliers across four quadrants, median simple-scale residual0.633px. This supports regional alignment, not a numerical color legend or guaranteed accuracy at every node. The tool never extracts Qi from pixels. Volcanic temperature, danger and Qi remain distinct.

## Reproducible field

- Data ID `world-qi:atlas-v2`, configuration version2, generator version2, seed20261106.
- Data SHA256: `436ce1b997c56d0c8f93c0244b9595e56d602335bd198c5f09dcf31e7a8a93eb`.
- Inputs: `Tools/data/WorldQiFieldV2.json`, `WorldQiOverridesV2.json`.
- Runtime-shaped proposal: `Tools/data/WorldQiDistributionV2.json`; complete audit CSV and regional summary beside it.
- Offline exporter: `Tools/generate_world_qi_v2.py`. V1 generator/data/overrides are unchanged.

Private seeded Python RNG creates two-scale sinusoidal background and ten medium Gaussian clusters. Four visual target regions select the nearest actual eligible node, with at least0.15 normalized distance from a start and0.2 between centers. No node is moved. Target centers may shift within0.2 to honor available cells and start buffers; this is not a pixel-perfect reconstruction of every illustrated bright spot.

Hot Gaussians use sigma0.10 and target field peak4.85 before tier quantization; four cold pockets and a smoothstep0.14 start buffer provide low basins/transitions. Explicit provenance-bearing overrides pin all five starts to tier2/1Qi/s. The eight v0.9 tier rates/boundaries are retained. Field samples are clamped to[0.6,5], then quantized. No independent per-node rolls, Unity Random access, runtime image sampling or scheduler is added.

| Proposed cluster | Actual center | Normalized X/Y | Catalog region |
|---|---|---|---|
| Northwest |TV-06|0.153315 /0.861878|Tử Vân Cổ Vực|
| Central lake |TL-18|0.604972 /0.664825|Thương Lam Trung Vực|
| Southwest |TV-40|0.129834 /0.397790|Tử Vân Cổ Vực|
| Southeast |TM-25|0.846685 /0.201657|Thanh Mộc U Cảnh|

These four centers are the only5Qi/s nodes:4/133, approximately3.01%. Two are geographically separate parts of the same catalog region, so the geographic count is four while the catalog-region count is three. No inferred center is used for a hotspot anchor. These remain proposed distribution choices, not authored official cultivation grounds.

| Tier /Qi per second | V1 nodes | V2 nodes |
|---|---:|---:|
|0 /0.6|31|12|
|1 /0.8|10|6|
|2 /1.0|32|22|
|3 /1.3|23|21|
|4 /1.8|8|29|
|5 /2.5|16|15|
|6 /3.5|9|24|
|7 /5.0|4|4|
|Total|133|133|

| Catalog region | Nodes | V1 max-tier | V2 max-tier |
|---|---:|---:|---:|
|Tử Vân Cổ Vực|31|4|2|
|Thương Lam Trung Vực|29|0|1|
|Xích Diễm Chi Địa|22|0|0|
|Thanh Mộc U Cảnh|26|0|1|
|Hoàng Thổ Nguyên Dã|25|0|0|

Unweighted node mean rises from1.461654 to1.917293Qi/s, approximately31.2%. This is a data average, not a measured player economy increase: visit time, safety, travel access and refinement capacity matter. Low nodes fall from41 to18 while tier6 rises9→24. The proposal therefore needs a reserve/economy review before activation even though max-tier rarity is unchanged. Cold pockets include the central basin and northeast; the northeast is not automatically high Qi because the artwork is volcanic.

Largest sampled near-pair difference at distance<0.085 is1.7Qi/s, versus1.5 in v1. The native validator checks non-override near pairs stay≤2Qi/s. Smooth continuous fields remain quantized and may skip a tier over a sampled interval; measured1.7 transitions are reported rather than described as identical to v1. Manual anomalies require stable ID/provenance and remain visible in export.

## Export and validation

```powershell
python Tools/generate_world_qi_v2.py --project-root . --output-dir C:/Temp/IdleGameQiV2
```

Output contains the full JSON/CSV, summary, generator parameters and debug overlay. The overlay legend identifies final node tiers, square start overrides and named proposed centers. It changes no source artwork. Numeric reproducibility can run with `--no-render`; PNG generation uses the already available Pillow dependency and is not runtime code.

Two fresh exports are byte-identical to the reviewed data. Version3 override fixtures survive regeneration without changing input files. Unknown IDs, noninteger seed, nonfinite field parameters and changed reviewed seed/data under the same version are rejected. A changed field/seed/tier mapping/override must advance the data/config version. The original v1 production files stay byte-identical. All133 entries retain coordinates, tier/rate, versions, seed, override flag and provenance.

Native `WorldCultivationBalanceV10Verifier.RunNative` validates the v2 table and proposed config, coordinates/starts/regions/spatial bounds, and current v1 Save compatibility. It also runs the separate refinement model simulations. This is Unity native evidence; v2 is not a live MainGame configuration.

## V1→V2 compatibility plan — not implemented

Stable node IDs give an identity mapping for geography. The per-node CSV records both v1 and v2 rates. Foundation, reserve, energy, Level/EXP, roots, points, inventory/equipment/forge and world/wave data must be preserved exactly. No backdated Qi compensation or foundation-to-new-rate rescaling is justified. Existing v1 partial time must settle under its original source before any approved configuration switch; offline time remains excluded.

A supported upgrade would need explicit consent/preflight, a fully validated candidate, immutable verified original backup, guarded transaction/rollback, new config identity/version and committed verification before canonical swap. Existing migration metadata may already protect a legacy→v1 upgrade and must not be overwritten or mistaken for a generic config upgrade receipt. Failure, newer-slot races, old binaries, repeated requests, manual rollback and restart need dedicated fixtures. Supporting v1 reads/writes must remain available alongside an opt-in v2 path.

Current production loader deliberately points only to v1. Native tests demonstrate v1 still loads and rejects a v2 component on its writer rather than retagging it. `CultivationProductionV2.proposal.json` validates only when explicitly supplied by the owned native verifier. The transaction above and dual-configuration resolver have not been implemented/accepted, so **activation remains BLOCKED**. No migration was run on player data.

## Future world economy boundary

Future joins should use stable node ID plus Qi config ID/version. Keep Qi supply, character refinement capacity, monster difficulty, encounter/drop configuration, cultivation-ground/lore identity and travel access as separate authorities. These dimensions may be reviewed together for risk/reward, but this batch changes none of the latter systems. No rare item, boss, encounter, danger increase or long-mai reward is added.
