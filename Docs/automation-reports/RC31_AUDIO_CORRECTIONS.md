# RC3.1 — Audio corrections

Morning remains in dry forest and selects one clip in the default test mix. The scheduler rotates every 120 seconds. A physical cap of one requires fade-out → source reuse → fade-in. Morning limit (0–3), rotation and fade are inspector settings. Morning gain is per family and per file. Dry Meadow keeps two regular loops and now plays cricket only during dry Night. The dry and wet Meadow pools remain mutually exclusive. Map assignments and all 191 OGG assets are unchanged.

Coast retains three bed loops, beach/wave/wind events (10–15 seconds) and up to two total Coast weather sources. Rain loops share a bounded gain budget, including while fading. Storm selects the verified Coast storm loop. Global overlays and GateThunder remain separate.

| TEST_PROFILE | Rain | WindRain | Pebble | Thunder | Coast | Pebble voices | Thunder interval |
|---|---:|---:|---:|---:|---:|---:|---|
| Light | .45 | .18 | .45 | .20 | .65 | 1 | disabled |
| Heavy | .80 | .65 | .75 | .45 | .85 | 2 | 65 s ±25% |
| Storm | 1.00 | 1.00 | 1.00 | .90 | 1.00 | 2 | 35 s ±25% |

These test multipliers apply to independent layer gains on top of the existing weather bus, clip and family gain. Each profile has editable fade and clip allowlists. Empty lists retain the verified family pool. HeavyRain below audio intensity .85 uses Heavy. At .85 or higher it uses Storm. The visual system still has only LightRain and HeavyRain. Native visual preview at full HeavyRain uses Storm audio automatically. Explicit audio audition lets the user compare Heavy and Storm without changing visual weather or Save.

Original source ZIP SHA-256 650aa81e8cc3d9d4b6ad668e6338d7001c11a825a3c05d25be1cc2f80037918a. Twelve catalog review clips stay unclassified. No human mix acceptance claimed.
