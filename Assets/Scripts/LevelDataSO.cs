using System;
using System.Collections.Generic;
using UnityEngine;

namespace BusJam
{
    /// <summary>Tail = o duoi xe, xe keo dai theo 'direction' den o dau (Head).</summary>
    [Serializable]
    public struct BusData
    {
        public Vector2Int tail;
        public Direction direction;
        [Range(2, 3)] public int length;
        public GameColor colorID;

        public Vector2Int Head => tail + direction.ToVec() * (length - 1);
        public Vector2Int Cell(int i) => tail + direction.ToVec() * i;
    }

    [CreateAssetMenu(menuName = "BusJam/Level Data", fileName = "Level_")]
    public class LevelDataSO : ScriptableObject
    {
        [Header("Parking grid (M x N)")]
        [Range(2, 9)] public int width = 5;
        [Range(2, 9)] public int height = 5;

        [Header("Bus stop")]
        [Range(2, 3)] public int bayCount = 3;

        [Header("Buses & passengers")]
        public List<BusData> buses = new List<BusData>();
        [Tooltip("Thu tu mau hanh khach trong hang doi (phan tu 0 = dung dau hang)")]
        public List<GameColor> passengerOrder = new List<GameColor>();

        /// <summary>Kiem tra tinh hop le: trong luoi, khong chong len nhau, so khach moi mau = 3 x so xe mau do.</summary>
        public bool Validate(out string error)
        {
            error = "";
            if (width < 2 || height < 2 || width > 9 || height > 9) { error = "Grid size invalid"; return false; }
            if (bayCount < 2 || bayCount > 3) { error = "bayCount must be 2..3"; return false; }

            var occ = new bool[width, height];
            var busCount = new int[Palette.ColorCount];
            foreach (var b in buses)
            {
                if (b.length < 2 || b.length > 3) { error = "Bus length must be 2..3"; return false; }
                for (int i = 0; i < b.length; i++)
                {
                    var c = b.Cell(i);
                    if (c.x < 0 || c.y < 0 || c.x >= width || c.y >= height) { error = $"Bus out of grid at {c}"; return false; }
                    if (occ[c.x, c.y]) { error = $"Buses overlap at {c}"; return false; }
                    occ[c.x, c.y] = true;
                }
                busCount[(int)b.colorID]++;
            }
            var paxCount = new int[Palette.ColorCount];
            foreach (var c in passengerOrder) paxCount[(int)c]++;
            for (int i = 0; i < Palette.ColorCount; i++)
                if (paxCount[i] != busCount[i] * BusController.Capacity)
                { error = $"Color {(GameColor)i}: {paxCount[i]} passengers vs {busCount[i]} buses x3"; return false; }
            return true;
        }
    }
}
