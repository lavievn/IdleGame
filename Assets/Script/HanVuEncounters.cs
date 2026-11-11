using System;
using System.Collections.Generic;

namespace TuTienCore
{
    public enum EncounterChoice { Pending, Participated, Declined }

    [Serializable]
    public sealed class OriginEncounterRecord
    {
        public string id;
        public EncounterChoice choice;
    }

    public sealed class OriginEncounter
    {
        public readonly string Id, Map, Title, Observation, Participate, Decline, Result;
        public readonly int RequiredWaves;

        public OriginEncounter(string id, string map, int requiredWaves, string title,
            string observation, string participate, string decline, string result)
        {
            Id = id;
            Map = map;
            RequiredWaves = requiredWaves;
            Title = title;
            Observation = observation;
            Participate = participate;
            Decline = decline;
            Result = result;
        }
    }

    public static class HanVuEncounters
    {
        private static readonly OriginEncounter[] Offers =
        {
            new OriginEncounter("hanvu.merchant_rumor", "TV-20", 0, "Gánh hàng ở đầu thôn",
                "Một người bán hàng dừng gánh bên giếng và bắt chuyện.", "Hỏi thăm", "Xin phép đi",
                "Lời kể · Người bán hàng nghe nói sương trong rừng thường tan muộn ở những lối thấp."),
            new OriginEncounter("hanvu.herb_basket", "TV-16", 1, "Giỏ lá bên đường",
                "Một chiếc giỏ đựng lá khô đặt cạnh tảng đá.", "Xem giỏ", "Để nguyên",
                "Ghi nhận · Trong giỏ có lá đã úa và một sợi lạt gãy. Chưa biết giỏ của ai."),
            new OriginEncounter("hanvu.wounded_traveler", "TV-24", 0, "Khách qua đường",
                "Một người đi đường đang ngồi nghỉ, cổ tay quấn vải.", "Giúp đỡ", "Từ chối",
                "Ghi nhận · Ta giúp người ấy buộc lại cổ tay. Sau khi cảm ơn, người ấy tiếp tục lên đường.")
        };

        private static OriginEncounter Find(string id) => Array.Find(Offers, offer => offer.Id == id);
        private static OriginEncounterRecord Record(OriginStoryState state, string id) =>
            state.encounters?.Find(record => record != null && record.id == id);

        public static OriginEncounter Available(EntityDataSO hero)
        {
            if (StoryOrigin.Definition(hero) == null) return null;
            foreach (var offer in Offers)
            {
                if (offer.Map != hero.worldMapId) continue;
                var record = Record(hero.originStory, offer.Id);
                if (record != null) return record.choice == EncounterChoice.Pending ? offer : null;
                if (hero.worldMapWaveProgress >= offer.RequiredWaves ||
                    WorldMapCatalog.IsCleared(hero, offer.Map)) return offer;
            }
            return null;
        }

        public static bool Observe(EntityDataSO hero, string id)
        {
            if (Available(hero)?.Id != id || string.IsNullOrEmpty(id)) return false;
            var state = hero.originStory;
            if (Record(state, id) != null) return false;
            if (state.encounters == null) state.encounters = new List<OriginEncounterRecord>();
            state.encounters.Add(new OriginEncounterRecord { id = id });
            hero.isDirty = true;
            return true;
        }

        public static bool Choose(EntityDataSO hero, string id, EncounterChoice choice)
        {
            if (choice != EncounterChoice.Participated && choice != EncounterChoice.Declined) return false;
            if (Available(hero)?.Id != id || string.IsNullOrEmpty(id)) return false;
            var record = Record(hero.originStory, id);
            if (record == null || record.choice != EncounterChoice.Pending) return false;
            record.choice = choice;
            hero.isDirty = true;
            return true;
        }

        internal static bool IsValid(OriginStoryState state)
        {
            if (state.encounters == null) return true;
            var seen = new HashSet<string>();
            foreach (var record in state.encounters)
                if (record == null || Find(record.id) == null || !seen.Add(record.id) ||
                    record.choice < EncounterChoice.Pending || record.choice > EncounterChoice.Declined)
                    return false;
            return true;
        }

        internal static void AddJournal(OriginStoryState state, List<string> lines)
        {
            if (state.encounters == null) return;
            foreach (var record in state.encounters)
            {
                var offer = Find(record.id);
                lines.Add("Ghi nhận · " + offer.Observation);
                if (record.choice == EncounterChoice.Pending) continue;
                lines.Add("Lựa chọn · " + (record.choice == EncounterChoice.Participated ? offer.Participate : offer.Decline) +
                    " (" + offer.Title + ").");
                if (record.choice == EncounterChoice.Participated) lines.Add(offer.Result);
            }
        }
    }
}
