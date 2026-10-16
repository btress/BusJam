using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BusJam.EditorTools
{
    public static class LevelGeneratorTool
    {
        public const string LevelDir = "Assets/BusJam/Levels";

        struct Cfg { public int w, h, bays, buses, colors, minBlocked, swaps, maxLen; }

        // 5 level tang dan: bai rong hon, nhieu mau xen ke hon, level 5 chi con 2 ben.
        static readonly Cfg[] Configs =
        {
            new Cfg{ w=4, h=4, bays=3, buses=4,  colors=2, minBlocked=1, swaps=0,  maxLen=2 },
            new Cfg{ w=5, h=5, bays=3, buses=6,  colors=3, minBlocked=2, swaps=2,  maxLen=3 },
            new Cfg{ w=5, h=6, bays=3, buses=8,  colors=4, minBlocked=3, swaps=5,  maxLen=3 },
            new Cfg{ w=6, h=6, bays=3, buses=10, colors=5, minBlocked=5, swaps=10, maxLen=3 },
            new Cfg{ w=7, h=7, bays=2, buses=13, colors=6, minBlocked=7, swaps=18, maxLen=3 },
        };

        [MenuItem("BusJam/Generate 5 Levels")]
        public static void Menu() { GenerateAll(); }

        public static List<LevelDataSO> GenerateAll()
        {
            Directory.CreateDirectory(LevelDir);
            var result = new List<LevelDataSO>();
            for (int i = 0; i < Configs.Length; i++)
            {
                EditorUtility.DisplayProgressBar("BusJam", $"Generating level {i + 1}/5 (kiem chung bang solver)...", i / 5f);
                var lvl = Generate(i, Configs[i]);
                string path = $"{LevelDir}/Level_{i + 1:00}.asset";
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.CreateAsset(lvl, path);
                result.Add(lvl);
            }
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            Debug.Log("BusJam: da tao 5 level trong " + LevelDir);
            return result;
        }

        static LevelDataSO Generate(int idx, Cfg c)
        {
            for (int attempt = 0; attempt < 3000; attempt++)
            {
                var rng = new System.Random(1000 + idx * 7919 + attempt);
                if (!TryPlace(rng, c, out var buses)) continue;

                // gan mau: moi mau xuat hien it nhat 1 lan
                var colors = new List<int>();
                for (int i = 0; i < c.buses; i++) colors.Add(i < c.colors ? i : rng.Next(c.colors));
                Shuffle(rng, colors);
                for (int i = 0; i < buses.Count; i++) { var b = buses[i]; b.colorID = (GameColor)colors[i]; buses[i] = b; }

                var lvl = ScriptableObject.CreateInstance<LevelDataSO>();
                lvl.width = c.w; lvl.height = c.h; lvl.bayCount = c.bays; lvl.buses = buses;

                var blk = LevelSolver.Blockers(lvl);
                int blocked = 0; foreach (var m in blk) if (m != 0) blocked++;
                if (blocked < c.minBlocked) continue;

                // thu tu phong xe hop le (khong chu trinh) -> hang doi nen "giai duoc"
                var order = RandomLaunchOrder(rng, blk, buses.Count);
                if (order == null) continue;
                var baseQueue = new List<GameColor>();
                foreach (int b in order) for (int k = 0; k < BusController.Capacity; k++) baseQueue.Add(buses[b].colorID);

                // xao tron dan: giam so lan swap neu khong tim duoc cau hinh giai duoc
                for (int swaps = c.swaps; swaps >= 0; swaps--)
                    for (int t = 0; t < (swaps == 0 ? 1 : 150); t++)
                    {
                        var q = new List<GameColor>(baseQueue);
                        for (int s = 0; s < swaps; s++) { int a = rng.Next(q.Count), d = rng.Next(q.Count); (q[a], q[d]) = (q[d], q[a]); }
                        lvl.passengerOrder = q;
                        if (LevelSolver.IsSolvable(lvl) && lvl.Validate(out _)) return lvl;
                    }
            }
            throw new Exception($"Khong tao duoc level {idx + 1}");
        }

        static bool TryPlace(System.Random r, Cfg c, out List<BusData> buses)
        {
            buses = new List<BusData>();
            var occ = new bool[c.w, c.h];
            for (int i = 0; i < c.buses; i++)
            {
                bool ok = false;
                for (int t = 0; t < 300 && !ok; t++)
                {
                    int len = r.Next(2, c.maxLen + 1);
                    var dir = (Direction)r.Next(4);
                    var tail = new Vector2Int(r.Next(c.w), r.Next(c.h));
                    var step = dir.ToVec();
                    var head = tail + step * (len - 1);
                    if (head.x < 0 || head.y < 0 || head.x >= c.w || head.y >= c.h) continue;
                    bool free = true;
                    for (int k = 0; k < len; k++) { var p = tail + step * k; if (occ[p.x, p.y]) { free = false; break; } }
                    if (!free) continue;
                    for (int k = 0; k < len; k++) { var p = tail + step * k; occ[p.x, p.y] = true; }
                    buses.Add(new BusData { tail = tail, direction = dir, length = len, colorID = GameColor.Red });
                    ok = true;
                }
                if (!ok) return false;
            }
            return true;
        }

        static List<int> RandomLaunchOrder(System.Random r, ulong[] blk, int n)
        {
            ulong remaining = (1UL << n) - 1UL;
            var order = new List<int>();
            while (remaining != 0)
            {
                var cand = new List<int>();
                for (int i = 0; i < n; i++) if ((remaining & (1UL << i)) != 0 && (blk[i] & remaining) == 0) cand.Add(i);
                if (cand.Count == 0) return null;   // chu trinh chan nhau
                int pick = cand[r.Next(cand.Count)];
                order.Add(pick); remaining &= ~(1UL << pick);
            }
            return order;
        }

        static void Shuffle<T>(System.Random r, IList<T> l)
        {
            for (int i = l.Count - 1; i > 0; i--) { int j = r.Next(i + 1); (l[i], l[j]) = (l[j], l[i]); }
        }
    }
}
