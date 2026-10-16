using System;
using System.Collections.Generic;
using UnityEngine;

namespace TuTienCore
{
    // .54 phase-1: authored data, temporary topology. NOT the final illustrated hex polygons.
    // This class owns ONLY the new-world path; worldMapVersion=0 retains legacy maps.
    public sealed class WorldMapNode
    {
        public string Id, Continent, Name, TerrainLabel, HumanEnemy;
        public TerrainType Terrain;
        public readonly List<string> Animals = new List<string>(7);
        public readonly List<string> Links = new List<string>();
        public bool IsBoss, IsStart;
        public int SuggestedLevel;
        // Approximate atlas pin location, NOT an authored hex polygon coordinate.
        public Vector2 PreviewPosition;
    }

    public sealed class WorldMapNation
    {
        public string Id, Name;
        // Normalized label anchor on the approved world-map illustration.
        public Vector2 LabelPosition;
    }

    public static class WorldMapCatalog
    {
        private static readonly Dictionary<string, WorldMapNode> nodes =
            new Dictionary<string, WorldMapNode>(StringComparer.Ordinal);
        private static readonly List<WorldMapNode> all = new List<WorldMapNode>();
        private static readonly List<WorldMapNation> nations = new List<WorldMapNation>();
        private static readonly string[] startIds = { "TV-20", "TL-16", "XD-11", "TM-12", "HT-17" };
        private static bool initialized;

        public static IList<WorldMapNode> All { get { EnsureLoaded(); return all.AsReadOnly(); } }
        public static IList<WorldMapNation> Nations { get { EnsureLoaded(); return nations.AsReadOnly(); } }
        public static bool Enabled(EntityDataSO hero) => hero != null && hero.worldMapVersion >= 1;
        public static bool TryGet(string id, out WorldMapNode node)
        {
            EnsureLoaded();
            return nodes.TryGetValue(id ?? "", out node);
        }

        private static TerrainType BaseTerrain(string label)
        {
            switch (label)
            {
                case "Cao nguyên": return TerrainType.CaoNguyen;
                case "Đồi núi": return TerrainType.DoiNui;
                case "Đồi núi / Cao nguyên": return TerrainType.DoiNui;
                case "Đầm lầy": return TerrainType.DamLay;
                case "Rừng rậm": return TerrainType.RungRam;
                case "Sa mạc": return TerrainType.SaMac;
                case "Hồ nước": return TerrainType.HoNuoc;
                case "Biển": return TerrainType.Bien;
                // Thảo nguyên is a biotope variant of the current Đồng bằng enum.
                default: return TerrainType.DongBang;
            }
        }

        private static RegionTheme MusicTheme(TerrainType terrain)
        {
            switch (terrain)
            {
                case TerrainType.CaoNguyen: return RegionTheme.CaoSon;
                case TerrainType.DoiNui: return RegionTheme.CaoSon;
                case TerrainType.DamLay: return RegionTheme.UTrach;
                case TerrainType.RungRam: return RegionTheme.SonLam;
                case TerrainType.SaMac: return RegionTheme.HoangMac;
                case TerrainType.Bien: return RegionTheme.HaiVuc;
                default: return RegionTheme.BinhNguyen;
            }
        }

        private static void EnsureLoaded()
        {
            if (initialized) return;
            initialized = true;
            TextAsset asset = Resources.Load<TextAsset>("WorldMapCatalog");
            if (asset == null)
            {
                Debug.LogError("[WorldMap] Missing Resources/WorldMapCatalog.txt; new-world mode unavailable.");
                return;
            }
            string[] lines = asset.text.Split('\n');
            foreach (string raw in lines)
            {
                string line = raw.TrimEnd('\r');
                if (string.IsNullOrEmpty(line) || line[0] == '#') continue;
                string[] f = line.Split('\t');
                if (f.Length != 8) { Debug.LogError("[WorldMap] Bad catalog row: " + line); continue; }
                if (nodes.ContainsKey(f[0])) { Debug.LogError("[WorldMap] Duplicate " + f[0]); continue; }
                WorldMapNode n = new WorldMapNode
                {
                    Id = f[0], Continent = f[1], Name = f[2], TerrainLabel = f[3],
                    Terrain = BaseTerrain(f[3]), HumanEnemy = f[5],
                    IsBoss = f[6] == "B", IsStart = f[7] == "S"
                };
                foreach (string animal in f[4].Split(','))
                {
                    string a = animal.Trim();
                    if (!string.IsNullOrEmpty(a) && !n.Animals.Contains(a)) n.Animals.Add(a);
                }
                if (n.Animals.Count != 7) { Debug.LogError("[WorldMap] Requires 7 species: " + n.Id); continue; }
                nodes.Add(n.Id, n);
                all.Add(n);
            }
            if (all.Count != 133) Debug.LogError("[WorldMap] Expected 133 nodes, got " + all.Count);
            // Optional authored national labels. Without nation boundaries and
            // coordinates the overlay intentionally displays NO invented countries.
            TextAsset countryFile = Resources.Load<TextAsset>("WorldMapCountries");
            if (countryFile != null)
            {
                foreach (string raw in countryFile.text.Split('\n'))
                {
                    string line = raw.TrimEnd('\r').Trim();
                    if (line.Length == 0 || line.StartsWith("#")) continue;
                    string[] c = line.Split('\t');
                    float x, y;
                    if (c.Length != 4 || !float.TryParse(c[2],
                            System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out x) ||
                        !float.TryParse(c[3],
                            System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out y))
                    {
                        Debug.LogWarning("[WorldMap] Invalid country label: " + line); continue;
                    }
                    if (x < 0 || x > 1 || y < 0 || y > 1) continue;
                    nations.Add(new WorldMapNation { Id=c[0], Name=c[1],
                        LabelPosition=new Vector2(x,y) });
                }
            }
            BuildTemporaryLinks();
        }

        private static void Link(WorldMapNode a, WorldMapNode b)
        {
            if (a == null || b == null || a == b) return;
            if (!a.Links.Contains(b.Id)) a.Links.Add(b.Id);
            if (!b.Links.Contains(a.Id)) b.Links.Add(a.Id);
        }

        // The indexed map provides 133 manually located candidate centers.
        // They improve the interim route graph, but are NOT surveyed polygon
        // boundaries. Final adjacency will be read from reviewed shared edges.
        private static bool LoadIndexedSeedPositions()
        {
            TextAsset source = Resources.Load<TextAsset>("WorldMapPolygonSeeds");
            if (source == null) return false;
            int loaded = 0;
            foreach (string raw in source.text.Split('\n'))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line[0] == '#') continue;
                string[] row = line.Split('\t');
                int x, y;
                WorldMapNode n;
                if (row.Length < 3 || !nodes.TryGetValue(row[0],out n) ||
                    !int.TryParse(row[1], out x) || !int.TryParse(row[2], out y))
                    continue;
                n.PreviewPosition = new Vector2(x / 1448f, 1f - y / 1086f);
                loaded++;
            }
            return loaded == all.Count && loaded == 133;
        }

        private static WorldMapNode NearestLink(WorldMapNode origin,
            List<WorldMapNode> candidates)
        {
            WorldMapNode result = null;
            float minimum = float.MaxValue;
            foreach (WorldMapNode n in candidates)
            {
                if (n == origin || n.IsBoss) continue;
                float dist = (n.PreviewPosition-origin.PreviewPosition).sqrMagnitude;
                if (dist < minimum) { result = n; minimum = dist; }
            }
            return result;
        }

        private static void ConnectSpatialNeighbors(
            Dictionary<string, List<WorldMapNode>> continents)
        {
            // Connect four nearest candidates within each region (no boss),
            // then link naturally adjacent large areas through closest pairs.
            foreach (var group in continents.Values)
            foreach (var n in group)
            {
                var near = new List<WorldMapNode>(group);
                near.Remove(n);
                near.Sort((a,b) =>
                    (n.PreviewPosition-a.PreviewPosition).sqrMagnitude.CompareTo(
                     (n.PreviewPosition-b.PreviewPosition).sqrMagnitude));
                for (int i = 0; i < near.Count && i < 4; i++) Link(n,near[i]);
            }

            string[] passages = {
                "TV|TL", "TV|HT", "TL|HT", "TL|XD",
                "TL|TM", "XD|TM", "HT|TM"
            };
            foreach (string passage in passages)
            {
                string[] prefixes = passage.Split('|');
                WorldMapNode bestA=null,bestB=null;
                float minDist=float.MaxValue;
                foreach (var a in all)
                {
                    if (a.IsBoss || !a.Id.StartsWith(prefixes[0]+"-")) continue;
                    foreach (var b in all)
                    {
                        if (b.IsBoss || !b.Id.StartsWith(prefixes[1]+"-")) continue;
                        float dist=(a.PreviewPosition-b.PreviewPosition).sqrMagnitude;
                        if (dist<minDist) {minDist=dist;bestA=a;bestB=b;}
                    }
                }
                Link(bestA,bestB);
            }
        }

        // Not final cartographic adjacency: routes use nearest label-center
        // prototypes until exact traced/shared edges are approved.
        private static void BuildTemporaryLinks()
        {
            var continents = new Dictionary<string, List<WorldMapNode>>();
            foreach (var n in all)
            {
                if (!continents.ContainsKey(n.Continent)) continents[n.Continent] = new List<WorldMapNode>();
                if (!n.IsBoss) continents[n.Continent].Add(n);
            }
            bool indexedPositions = LoadIndexedSeedPositions();
            if (indexedPositions) ConnectSpatialNeighbors(continents);
            else foreach (var group in continents.Values)
            {
                group.Sort((a, b) => String.CompareOrdinal(a.Id, b.Id));
                for (int i = 0; i < group.Count; ++i)
                {
                    if (i > 0) Link(group[i], group[i - 1]);
                    if (i > 1) Link(group[i], group[i - 2]);
                }
            }
            // Legacy fallback passages if no approved indexed seed file.
            if (!indexedPositions)
            {
            // Provisional land/sea passageways, NOT nation borders.
            string[] order = { "TV", "TL", "XD", "TM", "HT" };
            for (int i = 0; i < order.Length - 1; i++)
            {
                WorldMapNode a = null, b = null;
                foreach (var n in all)
                {
                    if (n.IsBoss) continue;
                    if (a == null && n.Id.StartsWith(order[i] + "-")) a = n;
                    if (b == null && n.Id.StartsWith(order[i + 1] + "-")) b = n;
                }
                // Connect edge nodes at the end/beginning, avoiding starting nodes.
                if (a != null && b != null)
                {
                    var left = continents[a.Continent];
                    var right = continents[b.Continent];
                    Link(left[left.Count - 1], right[0]);
                }
            }
            // Provisional normalized atlas markers. Final positions must be imported
            // from the user's hex polygons; no country borders are generated here.
            foreach (var pair in continents)
            {
                var group = new List<WorldMapNode>(pair.Value);
                foreach (var boss in all) if (boss.IsBoss && boss.Continent == pair.Key) group.Add(boss);
                group.Sort((a, b) => String.CompareOrdinal(a.Id, b.Id));
                string prefix = group.Count == 0 ? "" : group[0].Id.Substring(0, 2);
                Vector2 center;
                switch (prefix)
                {
                    case "TV": center = new Vector2(.20f, .66f); break;
                    case "TL": center = new Vector2(.49f, .73f); break;
                    case "XD": center = new Vector2(.80f, .70f); break;
                    case "HT": center = new Vector2(.46f, .27f); break;
                    default: center = new Vector2(.81f, .28f); break;
                }
                int columns = 6;
                for (int i = 0; i < group.Count; i++)
                {
                    int x = i % columns, y = i / columns;
                    float width = .235f, height = .28f;
                    float steps = Mathf.Max(1, (group.Count - 1) / columns);
                    group[i].PreviewPosition = new Vector2(
                        center.x + (x - 2.5f) * (width / columns),
                        center.y + ((steps * .5f) - y) * (height / (steps + 1f)));
                }
            }
            }
            // Boss is a single-way leaf, never directly adjacent to a birth node.
            foreach (var boss in all)
            {
                if (!boss.IsBoss) continue;
                var group = continents[boss.Continent];
                WorldMapNode start = null;
                foreach (var n in group) if (n.IsStart) { start = n; break; }
                WorldMapNode attach=null;
                if (indexedPositions)
                {
                    // Boss should be near its artwork location, but never
                    // directly attached to an eligible birth map.
                    float score=float.MaxValue;
                    foreach (var candidate in group)
                    {
                        if (candidate.IsStart) continue;
                        float candidateScore=
                            (candidate.PreviewPosition-boss.PreviewPosition).sqrMagnitude;
                        if (candidateScore < score) {score=candidateScore;attach=candidate;}
                    }
                }
                else
                {
                    int startIndex = start == null ? group.Count / 2 : group.IndexOf(start);
                    attach = startIndex < group.Count / 2 ? group[group.Count - 1] : group[0];
                    if (attach.IsStart && group.Count > 1) attach = group[1];
                }
                Link(boss, attach);
            }
            // Fixed, repeatable test levels from nearest birth point, never from Hero level.
            var distance = new Dictionary<string, int>();
            var queue = new Queue<WorldMapNode>();
            foreach (var id in startIds)
            {
                WorldMapNode start;
                if (nodes.TryGetValue(id, out start))
                {
                    distance[id] = 0;
                    queue.Enqueue(start);
                }
            }
            while (queue.Count > 0)
            {
                WorldMapNode current = queue.Dequeue();
                foreach (var id in current.Links)
                {
                    if (distance.ContainsKey(id)) continue;
                    distance[id] = distance[current.Id] + 1;
                    queue.Enqueue(nodes[id]);
                }
            }
            foreach (var n in all)
            {
                int depth;
                n.SuggestedLevel = n.IsBoss ? 120 :
                    1 + 3 * (distance.TryGetValue(n.Id, out depth) ? depth : 20);
            }
        }

        private static void ApplyMap(EntityDataSO hero, WorldMapNode node)
        {
            hero.worldMapId = node.Id;
            hero.mapName = node.Name;
            hero.mapTerrain = node.Terrain;
            hero.regionTheme = MusicTheme(node.Terrain);
            hero.regionIndex = -1;
            hero.mapNumber = 1; // Legacy counter is NOT a world-map position.
            hero.difficulty = 0;
            hero.completedWavesInMap = 0;
            hero.worldMapWaveProgress = 0;
            hero.worldMapLoopWave = 1;
            hero.isDirty = true;
        }

        public static bool StartNew(EntityDataSO hero)
        {
            if (hero == null) return false;
            EnsureLoaded();
            if (all.Count == 0) return false;
            // Five confirmed starting nodes, one within each major region.
            var valid = new List<WorldMapNode>();
            foreach (var id in startIds) { WorldMapNode n; if (nodes.TryGetValue(id, out n)) valid.Add(n); }
            if (valid.Count == 0) return false;
            WorldMapNode selected = valid[UnityEngine.Random.Range(0, valid.Count)];
            hero.worldMapVersion = 1;
            hero.worldMapVisited = new List<string> { selected.Id };
            hero.worldMapCleared = new List<string>();
            ApplyMap(hero, selected);
            return true;
        }

        public static void Normalize(EntityDataSO hero)
        {
            if (!Enabled(hero)) return;
            WorldMapNode node;
            if (!TryGet(hero.worldMapId, out node))
            {
                // Do not reroll a nonexistent saved location or corrupt old saves.
                Debug.LogWarning("[WorldMap] Unknown saved node: " + hero.worldMapId);
                return;
            }
            if (hero.worldMapVisited == null) hero.worldMapVisited = new List<string>();
            if (hero.worldMapCleared == null) hero.worldMapCleared = new List<string>();
            if (!hero.worldMapVisited.Contains(node.Id)) { hero.worldMapVisited.Add(node.Id); hero.isDirty = true; }
            int progress = Mathf.Clamp(hero.worldMapWaveProgress, 0, 5);
            if (hero.worldMapWaveProgress != progress) hero.isDirty = true;
            hero.worldMapWaveProgress = progress;
            int wave = Mathf.Clamp(hero.worldMapLoopWave, 1, 5);
            if (hero.worldMapLoopWave != wave) hero.isDirty = true;
            hero.worldMapLoopWave = wave;
            hero.completedWavesInMap = wave - 1;
            if (progress >= 5 && !hero.worldMapCleared.Contains(node.Id))
            {
                hero.worldMapCleared.Add(node.Id);
                hero.isDirty = true;
            }
            hero.mapName = node.Name;
            hero.mapTerrain = node.Terrain;
            hero.regionTheme = MusicTheme(node.Terrain);
            hero.regionIndex = -1;
        }

        public static bool CompleteWave(EntityDataSO hero)
        {
            if (!Enabled(hero)) return false;
            Normalize(hero);
            // Repeat waves 1..5 indefinitely at this node. No automatic
            // transition to a neighboring map, even after the first clear.
            hero.worldMapLoopWave = hero.worldMapLoopWave >= 5
                ? 1 : hero.worldMapLoopWave + 1;
            hero.completedWavesInMap = hero.worldMapLoopWave - 1;
            if (hero.worldMapWaveProgress < 5) hero.worldMapWaveProgress++;
            hero.isDirty = true;
            if (hero.worldMapWaveProgress < 5) return false;
            if (hero.worldMapCleared.Contains(hero.worldMapId)) return false;
            hero.worldMapCleared.Add(hero.worldMapId);
            return true;
        }

        public static void RestartAfterDefeat(EntityDataSO hero)
        {
            if (!Enabled(hero)) return;
            // During phase 1 stay on the current node; never lose past clear history.
            hero.worldMapWaveProgress = IsCleared(hero, hero.worldMapId) ? 5 : 0;
            hero.worldMapLoopWave = 1;
            hero.completedWavesInMap = 0;
            hero.mapVisits++;
            hero.isDirty = true;
            Normalize(hero);
        }

        public static bool IsCleared(EntityDataSO hero, string id)
            => Enabled(hero) && hero.worldMapCleared != null && hero.worldMapCleared.Contains(id);

        public static string TravelBlockReason(EntityDataSO hero, string id)
        {
            if (!Enabled(hero)) return "Nhân vật đang ở chế độ bản đồ cũ.";
            WorldMapNode current, target;
            if (!TryGet(hero.worldMapId, out current) || !TryGet(id, out target))
                return "Không tìm thấy map hợp lệ.";
            if (id == hero.worldMapId) return "Bạn đang ở map này.";
            if (!current.Links.Contains(id)) return "Chỉ được đi tới map kề trực tiếp.";
            if (!IsCleared(hero, current.Id)) return "Cần hoàn thành 5 wave của map hiện tại ít nhất một lần.";
            if (target.IsBoss) return "Map Boss endgame: chưa chốt vật phẩm / cảnh giới mở khóa.";
            return null;
        }

        public static bool TryTravel(EntityDataSO hero, string targetId)
        {
            if (TravelBlockReason(hero, targetId) != null) return false;
            WorldMapNode target;
            if (!TryGet(targetId, out target)) return false;
            ApplyMap(hero, target);
            if (!hero.worldMapVisited.Contains(targetId)) hero.worldMapVisited.Add(targetId);
            return true;
        }
    }
}
