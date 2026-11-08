public static class CultivationIntegrationGate
{
    // v0.9 production config +133-node table, native migration/backup/rollback and
    // real MainGame consent/lifecycle/visual acceptance passed before activation.
    // Startup only binds validated config. Legacy slots still require explicit consent.
    // Blood Essence Burning, Heavenly Trial and offline cultivation remain locked.
    public const bool ProductionEnabled=true;
    public const bool WorldQiV2Enabled=true; // Open only after compatibility/clock/MainGame/Player acceptance.
    public const bool HeavenlyTribulationEnabled=false; // Retail trial remains closed.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public const bool DevelopmentCultivationLoopEnabled=true;
#endif
    public const bool CapacityEnabled=true; // New identity; enable only after native/MainGame/Player safety gates.
    public const string Status="Tu vi chưa cấu hình";
}
