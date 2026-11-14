#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Text;
using TuTienCore;
using UnityEngine;

public partial class UIManager
{
    private bool diagnosticsOpen, combatDiagnostics;
    private Rect diagnosticsRect;
    private Vector2 combatDiagnosticsScroll;

    private bool CanShowDiagnostics(GameManager game) => game != null && game.IsGameplayHUDVisible &&
        !game.HasPendingConfirmation && !SkillPanelOpen && !(worldMapUI?.IsOpen ?? false) &&
        !(musicVolumePanel?.IsOpen ?? false) && !(devUI?.IsOpen ?? false) &&
        !(inventoryUI?.IsOpen ?? false) && !(inventoryUI?.IsForgeOpen ?? false) &&
        (damagePanel == null || !damagePanel.activeSelf) && (systemMenu == null || !systemMenu.activeInHierarchy);

    private bool HitDiagnostics(Vector2 screenPoint) => diagnosticsOpen && CanShowDiagnostics(GameForUI()) &&
        diagnosticsRect.Contains(new Vector2(screenPoint.x, Screen.height - screenPoint.y));

    private void OnGUI()
    {
        var game = GameForUI();
        if (!CanShowDiagnostics(game)) return;
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.F8)
        {
            diagnosticsOpen = !diagnosticsOpen;
            Event.current.Use();
        }
        if (!diagnosticsOpen) return;
        diagnosticsRect = new Rect(12, 44, Mathf.Min(560, Screen.width - 24), Mathf.Min(550, Screen.height - 56));
        GUI.Box(diagnosticsRect, GUIContent.none);
        GUILayout.BeginArea(diagnosticsRect);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Environment Audio")) combatDiagnostics = false;
        if (GUILayout.Button("Combat & Progression")) combatDiagnostics = true;
        if (GUILayout.Button("Close", GUILayout.Width(65))) diagnosticsOpen = false;
        GUILayout.EndHorizontal();
        if (combatDiagnostics) DrawCombatDiagnostics(game);
        else if (game.AudioDebugPlayer != null && !game.HasTribulationLiability) game.AudioDebugPlayer.DrawAudioDebug();
        else GUILayout.Label("Environment audio inactive.");
        GUILayout.EndArea();
    }

    private void DrawCombatDiagnostics(GameManager game)
    {
        var data = game.HeroData;
        if (data == null) return;
        var combat = CombatManager.Instance;
        var actor = combat?.CurrentHeroCombatState;
        var weapon = WeaponBasicAttack.Family(WeaponBasicAttack.Equipped(data));
        var cultivation = game.Cultivation;
        var text = new StringBuilder(768);
        text.AppendLine($"Level {data.currentLevel} · EXP {data.currentExp}/{data.expToNextLevel}");
        if (ExperienceProgress.TryRead(data.experienceJson, out var exp))
            text.AppendLine("EXP Reserve " + ExperienceProgress.Amount(exp));
        if (cultivation != null)
        {
            var foundation = cultivation.Current.Foundation;
            var stage = cultivation.Profile.Catalog.Evaluate(foundation.ActualRealmId, foundation.Amount).MinorStage;
            text.AppendLine($"{foundation.ActualRealmId} · {stage} · Tu vi {foundation.Amount:0.##}");
            text.AppendLine($"Qi reserve {cultivation.Current.Reserve.Amount:0.##} · Supply {cultivation.EnvironmentalQiPerSecond:0.##}/s · Absorbed {cultivation.LastStepFlow.AbsorptionPerSecond:0.##}/s");
        }
        text.AppendLine($"Weapon {weapon} · Base HP {data.baseHealth} · Base ATK {data.baseDamage} · Base attack speed {data.baseAttackSpeed:0.##}");
        text.AppendLine($"HP {actor?.CurrentHP}/{actor?.MaxHP} · Mana {actor?.CurrentMana:0.##}/{actor?.MaxMana:0.##} · ATK {WeaponBasicAttack.Physical(data)} · MATK {WeaponBasicAttack.Magical(data)}");
        text.AppendLine("Target: " + (actor?.Controller?.CurrentTarget?.name ?? "—"));
        text.AppendLine("Modifiers recorded on Hero (conditions apply at evaluation):");
        if (data.combatStatModifiers != null)
            foreach (var mod in data.combatStatModifiers)
            {
                if (mod == null) continue;
                text.AppendLine($"{mod.sourceId} · {mod.stat} · flat {mod.flat:0.##} · pct {mod.percent:P1}" +
                    $" · requires [{string.Join(", ", (IEnumerable<string>)mod.requiredTags ?? Array.Empty<string>())}]" +
                    $" · blocks [{string.Join(", ", (IEnumerable<string>)mod.blockedTags ?? Array.Empty<string>())}]");
            }
        text.AppendLine("Mastery:");
        if (data.mastery != null)
        {
            foreach (var row in data.mastery.weapons) text.AppendLine($"{row.id} · {row.units}");
            foreach (var row in data.mastery.skills) text.AppendLine($"{row.id} · {row.units}");
        }
        text.AppendLine("Latest resolved damage:");
        text.AppendLine(combat?.HitDiagnostic ?? "—");
        text.AppendLine(game.LatestDamageDiagnostic);
        text.AppendLine("Latest loot: " + (combat?.LootDiagnostic ?? "—"));
        combatDiagnosticsScroll = GUILayout.BeginScrollView(combatDiagnosticsScroll);
        GUILayout.Label(text.ToString());
        GUILayout.EndScrollView();
    }
}
#endif
