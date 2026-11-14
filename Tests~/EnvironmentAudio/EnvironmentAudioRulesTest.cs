using System;
using System.Linq;

internal static class EnvironmentAudioRulesTest
{
    private static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception(name);
        Console.WriteLine("PASS " + name);
    }

    private static void Parse(string name, AudioFamily family, AudioVariant variant) =>
        Check(EnvironmentAudioRules.TryParse(name + ".ogg", out var actualFamily, out var actualVariant) &&
            actualFamily == family && actualVariant == variant, name);

    public static void Main()
    {
        Parse("Coast_01_rain", AudioFamily.Coast, AudioVariant.Rain);
        Parse("Coast_01_storm", AudioFamily.Coast, AudioVariant.Storm);
        Parse("Coast_01_wind", AudioFamily.Coast, AudioVariant.Wind);
        Parse("Meadow_01_rain", AudioFamily.Meadow, AudioVariant.Rain);
        Parse("Meadow_01_cricket", AudioFamily.Meadow, AudioVariant.Cricket);
        Parse("Rain_01", AudioFamily.Rain, AudioVariant.Default);
        Parse("WindRain_01", AudioFamily.WindRain, AudioVariant.Default);
        Parse("Thunder_01", AudioFamily.Thunder, AudioVariant.Default);
        Parse("Coast_heavy_rain", AudioFamily.Coast, AudioVariant.Rain);
        Parse("Coast_medium_rain", AudioFamily.Coast, AudioVariant.Rain);
        Parse("Coast_wave_storm", AudioFamily.Coast, AudioVariant.Storm);
        Check(new[] {"Coast_01_rain_noise", "Coast_near_water", "Coast_wave_rain", "Rain_01_storm", "Meadow_01_", "Lodges_base",
                "Coast_", "ForestRain_01"}.All(name => !EnvironmentAudioRules.TryParse(name, out _, out _)), "Unknown, ambiguous and missing tokens are rejected");

        var random = new Random(210);
        var clips = new[] { "Coast_01", "Coast_02", "Coast_03", "Coast_04", "Coast_01_rain", "Coast_02_storm", "Coast_03_wind",
            "Meadow_01", "Meadow_02", "Meadow_03", "Meadow_04", "Meadow_01_rain", "Meadow_02_light_rain", "Meadow_01_cricket",
            "Morning_01", "Morning_02", "Morning_03", "Morning_04", "Morning_05", "Morning_06", "Primeval_01", "Primeval_02", "Primeval_03",
            "Lodges_01", "Lodges_02", "Night_01", "Night_02", "Night_03", "Night_04", "Night_05", "Rain_01", "Thunder_01",
            "WindRain_01", "Pebble_01", "Pebble_02", "Pebble_03", "Coast_03_wind" };
        var schedule = new EnvironmentAudioSchedule(clips, random);
        int Count(AudioFamily family) => schedule.Desired.Count(x => x.Family == family);
        bool Has(string name) => schedule.Desired.Any(item => item.Name == name);
        void Update(AudioMap map, AudioWeather weather, bool night = false, float time = 0f) =>
            schedule.Update(map, weather, night, time, 1);

        Update(AudioMap.Coast, AudioWeather.Dry);
        Check(Count(AudioFamily.Coast) == 3 && schedule.Events.Count == 0, "Coast chooses three regular loops");
        Update(AudioMap.Coast, AudioWeather.Dry, time:9);
        Check(schedule.Events.Count == 0, "No Coast event before ten seconds");
        Update(AudioMap.Coast, AudioWeather.Dry, time:15);
        Check(schedule.Events.Single().Name == "Coast_03_wind" && !Has("Coast_03_wind"), "Coast event in 10–15 seconds, outside regular slots");
        Update(AudioMap.Coast, AudioWeather.Rain, time:16);
        Check(Has("Coast_01_rain") && Has("Rain_01") && Count(AudioFamily.Coast) == 4 && Count(AudioFamily.Rain) == 1 &&
            Count(AudioFamily.Pebble) == 2 && Count(AudioFamily.WindRain) == 1, "Coast and global wet limits are independent");
        Update(AudioMap.Coast, AudioWeather.Storm, time:17);
        Check(!Has("Coast_01_rain") && Has("Coast_02_storm") && Has("Rain_01"), "Rain to Storm clears stale desired variant");
        for (int i = 0; i < 20; i++) Update(AudioMap.Coast, AudioWeather.Storm, time:17);
        Check(schedule.Desired.Count(item => item.Name == "Coast_02_storm") == 1 && schedule.Events.Count == 0,
            "Repeated weather updates keep one Coast weather loop");
        Update(AudioMap.Coast, AudioWeather.Dry, time:18);
        Check(schedule.Desired.Count == 3, "Storm to Clear removes all wet layers");
        Update(AudioMap.Meadow, AudioWeather.Dry, time:20);
        var meadow = schedule.Desired.Where(item => item.Layer == AudioLayer.Meadow).Select(item => item.Name).ToArray();
        Check(meadow.Length == 2 && Count(AudioFamily.Meadow) == 3 && Has("Meadow_01_cricket"),
            "Meadow chooses two loops and one cricket from separate pools");
        Update(AudioMap.Meadow, AudioWeather.Dry, time:320);
        Check(!meadow.Any(Has) && Count(AudioFamily.Meadow) == 3 && Has("Meadow_01_cricket"),
            "Meadow rotates two loops after five minutes while keeping cricket");
        Update(AudioMap.Meadow, AudioWeather.Rain, time:321);
        Check(Count(AudioFamily.Meadow) == 1 && Count(AudioFamily.Rain) == 1 && !Has("Meadow_01_cricket") &&
            !Has("Coast_01_rain"), "Meadow rain excludes dry Meadow only");
        Update(AudioMap.Meadow, AudioWeather.Storm, time:322);
        Check(Count(AudioFamily.Meadow) == 0 && Has("Rain_01"), "Storm leaves Meadow silent pending eligibility approval");
        Update(AudioMap.Meadow, AudioWeather.Storm, time:365);
        Check(schedule.Events.Count == 1 && schedule.Events[0].Family == AudioFamily.Thunder && !Has("Thunder_01"),
            "Global Thunder is a scheduled one-shot");
        Update(AudioMap.Forest, AudioWeather.Dry, time:400);
        var morning = schedule.Desired.Where(item => item.Family == AudioFamily.Morning).Select(item => item.Name).ToArray();
        Check(morning.Length == 3 && Count(AudioFamily.Primeval) == 3, "Forest schedules both families independently");
        Update(AudioMap.Forest, AudioWeather.Dry, time:519);
        Check(morning.All(Has), "Morning keeps the set until rotation");
        Update(AudioMap.Forest, AudioWeather.Dry, time:520);
        Check(!morning.Any(Has) && Count(AudioFamily.Morning) == 3, "Three Morning loops rotate after 120 seconds");
        Update(AudioMap.Forest, AudioWeather.Dry, true, 522);
        Check(Count(AudioFamily.Morning) == 3 && Count(AudioFamily.Primeval) == 0 &&
            Count(AudioFamily.Night) == 4, "Night overlays Morning; Primeval stops at night");
        Update(AudioMap.Forest, AudioWeather.Storm, true, 523);
        Check(Count(AudioFamily.Morning) == 0 && Count(AudioFamily.Night) == 0 && Count(AudioFamily.Rain) == 1,
            "Night storm keeps global Rain while Morning is silent");
        Update(AudioMap.Forest, AudioWeather.Dry, true, 524);
        Check(Count(AudioFamily.Morning) == 3, "Dry Morning can start at night");
        Update(AudioMap.Forest, AudioWeather.Dry, time:525);
        Check(Count(AudioFamily.Morning) == 3 && Count(AudioFamily.Night) == 0, "Dawn removes only the Night overlay");
        Update(AudioMap.Forest, AudioWeather.Rain, time:526);
        Check(Count(AudioFamily.Morning) == 0 && Count(AudioFamily.Primeval) == 0 && Count(AudioFamily.Rain) == 1,
            "Forest light rain excludes Morning and Primeval");
        Update(AudioMap.Forest, AudioWeather.Storm, time:527);
        Check(Count(AudioFamily.Morning) == 0 && Count(AudioFamily.Primeval) == 0 && Count(AudioFamily.Rain) == 1,
            "Forest heavy rain excludes Morning and Primeval");
        Update(AudioMap.Mountain, AudioWeather.Dry, time:528);
        Check(Count(AudioFamily.Morning) == 0 && Count(AudioFamily.Primeval) == 3, "Dry mountains schedule Primeval only");
        Update(AudioMap.Mountain, AudioWeather.Storm, time:529);
        Check(Count(AudioFamily.Morning) == 0 && Count(AudioFamily.Primeval) == 0 && Count(AudioFamily.Rain) == 1,
            "Mountain storm keeps global Rain");
        Update(AudioMap.Unassigned, AudioWeather.Rain, true, 530);
        Check(Count(AudioFamily.Night) == 0 && Count(AudioFamily.Rain) == 1 && Count(AudioFamily.Pebble) == 2,
            "Wet overlays remain on maps without a local family");
        for (int i = 0; i < 3; ++i)
        {
            schedule.Reset();
            Update(AudioMap.Lodges, AudioWeather.Dry);
            Check(Count(AudioFamily.Lodges) == 1 && schedule.Desired.Select(x => x.Name).Distinct().Count() == schedule.Desired.Count,
                "Reset/reload chooses one Lodges clip without duplicate registrations " + (i + 1));
        }
        schedule.Reset();
        schedule.Update(AudioMap.Coast, AudioWeather.Rain, false, 0f, 0);
        Check(!Has("Coast_01_rain") && Has("Rain_01"), "Unconfigured Coast weather slots stay off");
        schedule.Update(AudioMap.Coast, AudioWeather.Rain, false, 1f, 1);
        Update(AudioMap.Unassigned, AudioWeather.Rain, time:2f);
        Check(Count(AudioFamily.Coast) == 0 && Has("Rain_01"), "Travel leaves Coast while retaining global Rain");
        var empty = new EnvironmentAudioSchedule(Array.Empty<string>(), random);
        empty.Update(AudioMap.Coast, AudioWeather.Rain, false, 0f, 1);
        Check(empty.Desired.Count == 0 && empty.Events.Count == 0, "Empty manifest pool is safe");
        var actual = new EnvironmentAudioSchedule(new[] { "Coast_heavy_rain", "Coast_medium_rain", "Coast_wave_storm" }, random);
        actual.Update(AudioMap.Coast, AudioWeather.Rain, false, 0f, 2);
        Check(actual.Desired.Count == 2 && actual.Desired.All(entry => entry.Variant == AudioVariant.Rain), "Configured two Coast Rain loops");
        actual.Update(AudioMap.Coast, AudioWeather.Storm, false, 1f, 2);
        Check(actual.Desired.Single().Variant == AudioVariant.Storm, "One verified Coast Storm loop");
    }
}
