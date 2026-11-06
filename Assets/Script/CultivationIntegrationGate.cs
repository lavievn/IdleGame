public static class CultivationIntegrationGate
{
    // v0.8 balance v1 is independently configured and tested in isolated contexts.
    // Production remains closed: legacy player consent/upgrade commit and rollback UI
    // are not implemented. Never silently initialize or rewrite personal Saves.
    // Blood Essence Burning and Heavenly Trial remain locked independently.
    public const bool ProductionEnabled=false;
    public const string Status="Tu vi chưa cấu hình";
}
