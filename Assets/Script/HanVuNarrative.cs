namespace TuTienCore
{
    internal enum HanVuConversation { None, Opening, BeforeMedicine, AfterMedicine, Woodcutter }

    internal readonly struct HanVuLine
    {
        public readonly string Speaker, Text;

        public HanVuLine(string speaker, string text)
        {
            Speaker = speaker;
            Text = text;
        }
    }

    internal static class HanVuNarrative
    {
        internal const float TrailExplorationFraction = .4f;
        internal const string TrailKnot = "Một sợi dây gai thắt nút đôi nằm trên cành thông thấp.";
        internal const string TrailNote = "Mấy cành khô xếp cạnh lối dốc. Dưới viên đá có mảnh giấy ghi Ma Tùng Sâm.";

        private static string Elder(OriginStoryState story) =>
            story.guardian.gender == GenderType.Nam ? "Ông" : "Bà";

        private static string Friend(OriginStoryState story) =>
            story.companion.gender == GenderType.Nam ? "Cậu ấy" : "Cô ấy";

        private static string Guardian(OriginStoryState story) => Elder(story) + " " + story.guardian.name;

        internal static HanVuLine[] Lines(EntityDataSO hero, HanVuConversation conversation)
        {
            var story = hero.originStory;
            string elder = Elder(story).ToLowerInvariant();
            string guardian = Guardian(story);
            string friend = story.companion.name;
            switch (conversation)
            {
                case HanVuConversation.Opening:
                    return new[]
                    {
                        new HanVuLine(guardian, "Khụ… Con khoác thêm áo đi. Sáng nay sương phủ tới tận bậc cửa."),
                        new HanVuLine(hero.entityName, "Con ấm rồi. " + Elder(story) + " lại thức vì ho cả đêm phải không?"),
                        new HanVuLine(guardian, "Chợp mắt được một lúc. Gói thuốc cũ chỉ còn một nhúm, để " + elder + " pha loãng ra."),
                        new HanVuLine(hero.entityName, "Thầy lang dặn lá bạc mọc ở bìa Cô Hồn Lâm. Khi sương tan, con có thể đi hái."),
                        new HanVuLine(guardian, friend + " sáng nay mang giỏ lên núi. Hai đứa từ bé vẫn chia nhau gùi củi ở đấy."),
                        new HanVuLine(hero.entityName, "Nếu gặp " + friend + ", con sẽ nhắn về ăn cơm. " + Elder(story) + " cứ nghỉ, để con lo bếp.")
                    };
                case HanVuConversation.BeforeMedicine:
                    return new[]
                    {
                        new HanVuLine(hero.entityName, "Con mang lá bạc về rồi. " + Elder(story) + " ngồi tựa lưng cho dễ thở nhé."),
                        new HanVuLine(guardian, "Sáng nay " + friend + " mang giỏ lên núi tìm thêm vị thuốc. " + elder +
                            " vẫn để phần cơm cho " + Friend(story).ToLowerInvariant() + "."),
                        new HanVuLine(hero.entityName, "Con sắc thuốc trước. Để con kê cái gối cao lên một chút.")
                    };
                case HanVuConversation.AfterMedicine:
                    return new[]
                    {
                        new HanVuLine(guardian, "Cổ họng dịu đi rồi, chỉ còn hơi tức ngực. " + friend + " đã về qua ngõ chưa?"),
                        new HanVuLine(hero.entityName, "Con chưa gặp. Lúc qua rừng lần tới, con sẽ hỏi xem " + Friend(story).ToLowerInvariant() + " ghé lối nào.")
                    };
                case HanVuConversation.Woodcutter:
                    return new[]
                    {
                        new HanVuLine("Người đốn củi", "Có, tôi thấy " + friend + " xách giỏ lá. " + Friend(story) +
                            " thong thả hỏi lối rẽ phía bắc, chỗ bóng thông phủ kín đường."),
                        new HanVuLine(hero.entityName, "Cảm ơn bác. Khi qua lối rẽ đó, cháu sẽ để ý.")
                    };
                default:
                    return System.Array.Empty<HanVuLine>();
            }
        }

        internal static bool TellsOfTrip(HanVuConversation conversation, int line) =>
            (conversation == HanVuConversation.Opening && line == 4) ||
            (conversation == HanVuConversation.BeforeMedicine && line == 1);

        internal static bool RecordTrip(EntityDataSO hero)
        {
            if (!StoryOrigin.IsSafe(hero) || hero.originStory.heardOpeningTrip) return false;
            hero.originStory.heardOpeningTrip = true;
            hero.isDirty = true;
            return true;
        }

        internal static bool AtWoodcutter(EntityDataSO hero)
        {
            var origin = StoryOrigin.Definition(hero);
            return origin != null && origin.Forest == hero.worldMapId;
        }

        internal static bool TalkToWoodcutter(EntityDataSO hero)
        {
            if (!AtWoodcutter(hero) || hero.originStory.heardWoodcutter) return false;
            hero.originStory.heardWoodcutter = true;
            hero.isDirty = true;
            return true;
        }

        internal static bool ObserveKnot(EntityDataSO hero)
        {
            if (!AtWoodcutter(hero) || hero.originStory.sawTrailKnot) return false;
            hero.originStory.sawTrailKnot = true;
            hero.isDirty = true;
            return true;
        }

        internal static bool ObserveNote(EntityDataSO hero)
        {
            if (!AtWoodcutter(hero) || hero.originStory.sawTrailNote) return false;
            hero.originStory.sawTrailNote = true;
            hero.isDirty = true;
            return true;
        }

        internal static void AddJournal(EntityDataSO hero, System.Collections.Generic.List<string> lines)
        {
            var story = hero.originStory;
            string friend = story.companion.name;
            if (story.heardOpeningTrip)
                lines.Add("Lời kể · " + Guardian(story) + " nói " + friend + " đã mang giỏ lên núi sáng nay.");
            if (story.heardWoodcutter)
                lines.Add("Lời kể · Người đốn củi thấy " + friend + " mang giỏ và thong thả hỏi lối rẽ phía bắc.");
            if (story.sawTrailKnot)
            {
                lines.Add("Ghi nhận · " + TrailKnot);
                lines.Add("Suy đoán · Có thể ai đó đánh dấu lối về. Dân đi rừng cũng hay dùng nút này.");
            }
            if (story.sawTrailNote && story.stage < OriginStage.SearchElite)
                lines.Add("Ghi nhận · " + TrailNote);
        }
    }
}
