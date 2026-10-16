using System;
using System.Collections.Generic;
using UnityEngine;

namespace BusJam.EditorTools
{
    /// <summary>Bo giai (DFS + memo) mo phong dung luat game de kiem chung level co the thang.</summary>
    public static class LevelSolver
    {
        /// <summary>blockers[i] = bitmask cac xe nam tren duong thoat cua xe i.</summary>
        public static ulong[] Blockers(LevelDataSO l)
        {
            int n = l.buses.Count;
            var occ = new int[l.width, l.height];
            for (int x = 0; x < l.width; x++) for (int y = 0; y < l.height; y++) occ[x, y] = -1;
            for (int i = 0; i < n; i++) for (int k = 0; k < l.buses[i].length; k++) { var c = l.buses[i].Cell(k); occ[c.x, c.y] = i; }

            var res = new ulong[n];
            for (int i = 0; i < n; i++)
            {
                var step = l.buses[i].direction.ToVec();
                var c = l.buses[i].Head + step;
                while (c.x >= 0 && c.y >= 0 && c.x < l.width && c.y < l.height)
                {
                    if (occ[c.x, c.y] >= 0 && occ[c.x, c.y] != i) res[i] |= 1UL << occ[c.x, c.y];
                    c += step;
                }
            }
            return res;
        }

        public static bool IsSolvable(LevelDataSO l, int nodeLimit = 100000) => new Impl().Run(l, nodeLimit);

        class Impl
        {
            int _n, _bays, _nodes, _limit;
            ulong _full;
            ulong[] _blk;
            int[] _col;
            List<GameColor> _order;
            readonly HashSet<string> _seen = new HashSet<string>();

            public bool Run(LevelDataSO l, int limit)
            {
                _n = l.buses.Count; _bays = l.bayCount; _limit = limit;
                _full = (1UL << _n) - 1UL;
                _blk = Blockers(l);
                _col = new int[_n];
                for (int i = 0; i < _n; i++) _col[i] = (int)l.buses[i].colorID;
                _order = l.passengerOrder;
                var bb = new int[_bays]; var bc = new int[_bays];
                for (int i = 0; i < _bays; i++) bb[i] = -1;
                return Dfs(0UL, 0, bb, bc);
            }

            void Auto(int[] bb, int[] bc, ref int q)
            {
                while (q < _order.Count)
                {
                    int best = -1;
                    for (int f = 0; f < _bays; f++)
                        if (bb[f] >= 0 && _col[bb[f]] == (int)_order[q] && bc[f] < 3 && (best < 0 || bc[f] > bc[best])) best = f;
                    if (best < 0) break;
                    bc[best]++; q++;
                    if (bc[best] == 3) { bb[best] = -1; bc[best] = 0; }
                }
            }

            bool Dfs(ulong launched, int q, int[] bb, int[] bc)
            {
                if (q == _order.Count && launched == _full) return true;
                var key = new int[_bays];
                for (int i = 0; i < _bays; i++) key[i] = bb[i] < 0 ? -1 : bb[i] * 4 + bc[i];
                Array.Sort(key);
                if (!_seen.Add(launched + "|" + q + "|" + string.Join(",", key))) return false;
                if (++_nodes > _limit) return false;

                int free = -1;
                for (int f = 0; f < _bays; f++) if (bb[f] < 0) { free = f; break; }
                if (free < 0) return false;

                for (int b = 0; b < _n; b++)
                {
                    if ((launched & (1UL << b)) != 0) continue;
                    if ((_blk[b] & ~launched) != 0) continue;
                    var nbb = (int[])bb.Clone(); var nbc = (int[])bc.Clone();
                    nbb[free] = b; nbc[free] = 0;
                    int nq = q;
                    Auto(nbb, nbc, ref nq);
                    if (Dfs(launched | (1UL << b), nq, nbb, nbc)) return true;
                }
                return false;
            }
        }
    }
}
