using System.Collections.Generic;
using TMPro;
using UnityEngine;

public partial class UIManager
{
    RectTransform skillPanel, skillClose, skillAvailablePrev, skillAvailableNext, skillAdd, skillGrant;
    RectTransform skillQueuePrev, skillQueueNext, skillUp, skillDown, skillRemove;
    TextMeshProUGUI skillTitle, skillAvailableText, skillQueueText;
    readonly List<WeaponSkill> skillAvailable = new List<WeaponSkill>();
    EntityDataSO skillOwner;
    int availableSelection, queueSelection;
    bool wasPausedBeforeSkills;

    bool SkillPanelOpen => skillPanel != null && skillPanel.gameObject.activeSelf;
    internal bool CombatResourcesMenuOpen => systemMenu != null && systemMenu.activeInHierarchy;

    internal void OpenWeaponSkills()
    {
        var game = GameForUI();
        if (game == null || !game.IsGameplayHUDVisible || game.HasTribulationLiability || game.HeroData == null)
            return;
        if (SkillPanelOpen) return;
        if (skillPanel == null) CreateSkillPanel();
        CloseDamageInfo();
        CloseSystemMenu();
        CloseWorldMapMenu();
        wasPausedBeforeSkills = IsPaused;
        skillOwner = game.HeroData;
        availableSelection = queueSelection = 0;
        skillPanel.gameObject.SetActive(true);
        skillPanel.SetAsLastSibling();
        SetPaused(true);
        RefreshWeaponSkills();
    }

    void CloseWeaponSkills(bool restorePause)
    {
        if (!SkillPanelOpen) return;
        skillPanel.gameObject.SetActive(false);
        if (restorePause) SetPaused(wasPausedBeforeSkills);
        skillOwner = null;
    }

    void CreateSkillPanel()
    {
        skillPanel = Surface(hudCanvas, "WeaponSkillsPanel", SurfaceKind.Frame);
        skillTitle = MakeLabel(skillPanel, "VÕ HỌC · TỰ THI TRIỂN", Vector2.zero, Vector2.one);
        skillAvailableText = MakeLabel(skillPanel, "", Vector2.zero, Vector2.one);
        skillQueueText = MakeLabel(skillPanel, "", Vector2.zero, Vector2.one);
        skillAvailableText.fontSize = skillQueueText.fontSize = 12;
        skillClose = SkillButton("SkillClose", "Đóng", () => CloseWeaponSkills(true));
        skillAvailablePrev = SkillButton("SkillAvailablePrev", "‹", () => SelectAvailable(-1));
        skillAvailableNext = SkillButton("SkillAvailableNext", "›", () => SelectAvailable(1));
        skillAdd = SkillButton("SkillAdd", "Thêm vào hàng", AddSelectedSkill);
        skillGrant = SkillButton("SkillGrantDev", "Nhận 3 vũ khí mẫu DEV", GrantSkillWeapons);
        skillQueuePrev = SkillButton("SkillQueuePrev", "‹ Trước", () => SelectQueue(-1));
        skillQueueNext = SkillButton("SkillQueueNext", "Sau ›", () => SelectQueue(1));
        skillUp = SkillButton("SkillUp", "Lên", () => MoveSelectedSkill(-1));
        skillDown = SkillButton("SkillDown", "Xuống", () => MoveSelectedSkill(1));
        skillRemove = SkillButton("SkillRemove", "Bỏ", RemoveSelectedSkill);
    }

    RectTransform SkillButton(string name, string label, System.Action action) =>
        SmallButton(skillPanel, name, label, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one, () => action());

    static void SkillPlace(RectTransform rect, float x, float y, float width, float height)
    {
        var topLeft = new Vector2(0, 1);
        PlaceUI(rect, topLeft, topLeft, new Vector2(x, -y), new Vector2(width, height));
    }

    void RefreshWeaponSkills()
    {
        if (!SkillPanelOpen) return;
        var game = GameForUI();
        if (game == null || game.HeroData != skillOwner || !game.IsGameplayHUDVisible || game.HasTribulationLiability)
        {
            CloseWeaponSkills(false);
            return;
        }
        var progress = skillOwner.combatSkills;
        var weapon = WeaponBasicAttack.Equipped(skillOwner);
        skillAvailable.Clear();
        foreach (var skill in WeaponSkillRegistry.All)
            if (skill.IsAvailable(skillOwner, weapon)) skillAvailable.Add(skill);
        availableSelection = Mathf.Clamp(availableSelection, 0, Mathf.Max(0, skillAvailable.Count - 1));
        queueSelection = Mathf.Clamp(queueSelection, 0, Mathf.Max(0, progress.queue.Count - 1));
        var selected = skillAvailable.Count == 0 ? null : skillAvailable[availableSelection];
        skillAvailableText.text = selected == null ? "KỸ NĂNG KHẢ DỤNG\nTrang bị vũ khí Hoàng có kỹ năng trong Hòm đồ."
            : "KỸ NĂNG KHẢ DỤNG  " + (availableSelection + 1) + "/" + skillAvailable.Count +
              "\n" + SkillLine(selected, weapon, progress) + "\n" +
              (selected.Source == CombatSkillSource.EquipmentGranted ? EquipmentRefinement.DisplayName(weapon) : "Đã học");
        if (progress.queue.Count == 0) skillQueueText.text = "HÀNG THI TRIỂN\nChưa chọn kỹ năng. Hero dùng đòn đánh thường.";
        else
        {
            var lines = new System.Text.StringBuilder("HÀNG THI TRIỂN · ").Append(progress.queue.Count).AppendLine(" kỹ năng");
            int start = Mathf.Max(0, Mathf.Min(queueSelection - 1, progress.queue.Count - 3));
            for (int i = start; i < progress.queue.Count && i < start + 3; i++)
            {
                var entry = WeaponSkillRegistry.Find(progress.queue[i]);
                if (i != start) lines.AppendLine();
                lines.Append(i == queueSelection ? "› " : "   ").Append(i + 1).Append(". ")
                    .Append(entry == null ? progress.queue[i] + " · Không khả dụng" : SkillLine(entry, weapon, progress));
            }
            skillQueueText.text = lines.ToString();
        }
        skillAdd.gameObject.SetActive(selected != null && !progress.queue.Contains(selected.Id));
        skillAvailablePrev.gameObject.SetActive(availableSelection > 0);
        skillAvailableNext.gameObject.SetActive(availableSelection + 1 < skillAvailable.Count);
        skillQueuePrev.gameObject.SetActive(queueSelection > 0);
        skillQueueNext.gameObject.SetActive(queueSelection + 1 < progress.queue.Count);
        skillUp.gameObject.SetActive(queueSelection > 0);
        skillDown.gameObject.SetActive(queueSelection + 1 < progress.queue.Count);
        skillRemove.gameObject.SetActive(progress.queue.Count > 0);
        skillGrant.gameObject.SetActive(GameManager.DevelopmentItemGrantsEnabled);

        float w = Mathf.Clamp(ManagementWindowLayout.CombatPixels(Screen.width) - 16, 300, 500);
        PlaceUI(skillPanel, new Vector2(.5f,.5f), new Vector2(.5f,.5f), Vector2.zero, new Vector2(w, 298));
        skillPanel.localScale = Vector3.one * Mathf.Min(1, (Screen.height - 12f) / 298f);
        SkillPlace(skillTitle.rectTransform, 8, 3, w-100, 30);
        SkillPlace(skillClose, w-84, 5, 76, 26);
        SkillPlace(skillAvailableText.rectTransform, 8, 36, w-16, 54);
        SkillPlace(skillAvailablePrev, 8, 92, 40, 26);
        SkillPlace(skillAvailableNext, 53, 92, 40, 26);
        SkillPlace(skillAdd, w-132, 92, 124, 26);
        SkillPlace(skillQueueText.rectTransform, 8, 124, w-16, 85);
        float buttonWidth = (w-20) / 5;
        SkillPlace(skillQueuePrev, 8, 215, buttonWidth-2, 26);
        SkillPlace(skillQueueNext, 8+buttonWidth, 215, buttonWidth-2, 26);
        SkillPlace(skillUp, 8+2*buttonWidth, 215, buttonWidth-2, 26);
        SkillPlace(skillDown, 8+3*buttonWidth, 215, buttonWidth-2, 26);
        SkillPlace(skillRemove, 8+4*buttonWidth, 215, buttonWidth-2, 26);
        SkillPlace(skillGrant, 8, 254, w-16, 30);
    }

    string SkillLine(WeaponSkill skill, EquipmentInstance weapon, CombatSkillProgress progress)
    {
        string source = skill.Source == CombatSkillSource.EquipmentGranted ? "Trang bị" : "Đã học";
        float remaining = progress.Remaining(skill.Id);
        return skill.Name + " · " + source + " · " + skill.Cost + " Mana · " +
            (remaining > 0 ? remaining.ToString("0.0") + "s còn lại" : skill.Cooldown + "s hồi") +
            (skill.IsAvailable(skillOwner, weapon) ? "" : " · Không khả dụng");
    }

    void SelectAvailable(int delta) { availableSelection += delta; RefreshWeaponSkills(); }
    void SelectQueue(int delta) { queueSelection += delta; RefreshWeaponSkills(); }

    void AddSelectedSkill()
    {
        if (!SkillPanelOpen || availableSelection >= skillAvailable.Count) return;
        if (skillOwner.combatSkills.Add(skillAvailable[availableSelection].Id)) skillOwner.isDirty = true;
        queueSelection = skillOwner.combatSkills.queue.Count - 1;
        RefreshWeaponSkills();
    }

    void RemoveSelectedSkill()
    {
        if (!SkillPanelOpen || queueSelection >= skillOwner.combatSkills.queue.Count) return;
        if (skillOwner.combatSkills.Remove(skillOwner.combatSkills.queue[queueSelection])) skillOwner.isDirty = true;
        RefreshWeaponSkills();
    }

    void MoveSelectedSkill(int delta)
    {
        if (!SkillPanelOpen || !skillOwner.combatSkills.Move(queueSelection, delta)) return;
        skillOwner.isDirty = true;
        queueSelection += delta;
        RefreshWeaponSkills();
    }

    void GrantSkillWeapons()
    {
        GameForUI()?.GrantWeaponSkillSamples();
        RefreshWeaponSkills();
    }
}
