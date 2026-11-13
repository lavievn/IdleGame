#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.IO;
using UnityEngine;
using TuTienCore;

internal static class DevelopmentCultivation015
{
    const string SourceRealm = "realm:luyen-khi";
    const string TargetRealm = "realm:truc-co";
    const string ResourceName = "DevelopmentCultivation015";

    [Serializable]
    internal sealed class Balance
    {
        public int version;
        public double[] luyenKhiThresholds, trucCoThresholds;
        public CultivationQiZones qiZones;
        public double trucCoAbsorption, trucCoRefinement, foundationLoss, lightningWarningSeconds;
        public HeavenlyGateConfiguration gate;
    }

#if UNITY_5_3_OR_NEWER
    internal static string Load(out CultivationPlaytestProfile profile,
        out HeavenlyGateConfiguration gate, out HeavenlyGatePenaltyPolicy penalty)
    {
        var asset = Resources.Load<TextAsset>(ResourceName);
        if (asset == null)
            throw new InvalidOperationException("Missing DevelopmentCultivation015 balance.");

        var baseline = Resources.Load<TextAsset>("CultivationProductionV3");
        var capacityAsset = Resources.Load<TextAsset>("CultivationCapacityV1");
        if (!CultivationProductionConfiguration.TryLoadVersion(3, out var production, out _,
                out var status, out var detail))
            throw new InvalidOperationException($"Development cultivation baseline: {status} {detail}");

        // A changed balance opens a separate playtest timeline. Existing receipts and thresholds
        // are always interpreted under the balance with which they were written.
        string fingerprint = TribulationSaveTransaction.Hash(Canonical(asset.text) +
            Canonical(baseline.text) + Canonical(capacityAsset.text));
        var balance = JsonUtility.FromJson<Balance>(asset.text);
        if (balance == null || balance.version < 1 || balance.gate == null)
            throw new InvalidOperationException("Invalid DevelopmentCultivation015 balance.");

        var definition = JsonUtility.FromJson<CultivationProductionDefinition>(baseline.text).cultivation;
        var capacity = JsonUtility.FromJson<CultivationCapacityDefinition>(capacityAsset.text);
        profile = CreateProfile(fingerprint, balance, definition, capacity, production);

        penalty = new HeavenlyGatePenaltyPolicy("development:gate-penalty-015", balance.version, balance.foundationLoss);
        gate = balance.gate;
        gate.schemaVersion = 1;
        gate.version = balance.version;
        gate.id = "development:heavenly-gate-015";
        gate.title = "Thiên Quan";
        gate.cultivationConfiguration = profile.Id;
        gate.sourceRealm = SourceRealm;
        gate.targetRealm = TargetRealm;
        gate.penaltyPolicyId = penalty.Id;
        gate.phase = "SealCombat";
        gate.receiptLifetimeSteps = 2;
        gate.warningSeconds = balance.lightningWarningSeconds;
        if (!gate.IsValid)
            throw new InvalidOperationException("Invalid Development gate rules.");
        return fingerprint;
    }
#endif

    internal static CultivationPlaytestProfile CreateProfile(string fingerprint, Balance balance,
        CultivationPlaytestDefinition definition, CultivationCapacityDefinition capacity, CultivationPlaytestProfile production)
    {
        definition.id = "balance:development-015:" + fingerprint;
        definition.version = balance.version;
        definition.title = "DEV 0.1.5 · Tu luyện";
        definition.qiZones = balance.qiZones;
        definition.realms[0].thresholds = balance.luyenKhiThresholds;
        definition.realms[0].successor = TargetRealm;
        definition.realms = new[] { definition.realms[0], new CultivationPlaytestRealm {
            id = TargetRealm, name = "Trúc Cơ", tier = 1, thresholds = balance.trucCoThresholds, successor = ""
        }};
        var sources = new System.Collections.Generic.List<CultivationQiSource>();
        foreach (var node in WorldMapCatalog.All)
            sources.Add(new CultivationQiSource { nodeId = node.Id, density = production.Density(node.Id) });
        definition.qiSources = sources.ToArray();

        var luyenKhi = capacity.realms[0];
        capacity.realms = new[] { luyenKhi, new CultivationRealmCapacityDefinition {
            realmId = TargetRealm, baseAbsorption = balance.trucCoAbsorption,
            baseRefinement = balance.trucCoRefinement,
            absorptionStageFactors = luyenKhi.absorptionStageFactors,
            refinementStageFactors = luyenKhi.refinementStageFactors
        }};
        CultivationPlaytestProfile profile;
        string detail;
        if (!CultivationPlaytestProfile.TryCreate(definition, production.HasNode, out profile,
                out detail, false, capacity))
            throw new InvalidOperationException("Invalid Development cultivation rules: " + detail);
        return profile;
    }

    internal static string PlayerSaveDirectory(string fingerprint)
    {
        string path = Path.Combine(@"D:\IdleGame\Playtest\Development015", fingerprint, "Saves");
        for (var part = new DirectoryInfo(path); part != null; part = part.Parent)
            if (part.Exists && (part.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Development save path must not traverse a junction: " + part.FullName);
        return path;
    }

    static string Canonical(string text) => text.Replace("\r\n", "\n").TrimStart('\uFEFF').TrimEnd() + "\n";
}
#endif
