using System.Collections.Generic;
using UnityEngine;

namespace BusJam
{
    public enum CellState { Empty, Occupied }

    /// <summary>Bai do xe tren mat phang XZ: ma tran 2D Occupied/Empty + quet vat can toi loi thoat.</summary>
    public class GridManager : MonoBehaviour
    {
        public const int MaxSize = 9;
        public const float TopZ = -0.85f;   // mep tren bai do, ben xe nam tren dai xam cua anh nen
        public float Cell = 1f;

        public int Width { get; private set; }
        public int Height { get; private set; }
        public Vector3 Origin { get; private set; }
        public float Left => Origin.x;
        public float Right => Origin.x + Width * Cell;
        public float Bottom => Origin.z;
        public float Top => Origin.z + Height * Cell;

        public readonly List<BusController> Buses = new List<BusController>();
        public int BusCount => Buses.Count;

        readonly CellState[,] _state = new CellState[MaxSize, MaxSize];
        readonly BusController[,] _occupant = new BusController[MaxSize, MaxSize];
        Renderer[,] _tiles;
        Renderer _panel;

        void Awake()
        {
            _panel = Visuals.Prim(transform, "LotPanel", Vector3.zero, Vector3.one, new Color(0.60f, 0.66f, 0.82f));
            _tiles = new Renderer[MaxSize, MaxSize];
            for (int x = 0; x < MaxSize; x++)
                for (int y = 0; y < MaxSize; y++)
                {
                    var t = Visuals.Prim(transform, $"Tile_{x}_{y}", Vector3.zero, new Vector3(0.96f, 0.1f, 0.96f),
                        ((x + y) & 1) == 0 ? new Color(0.80f, 0.84f, 0.94f) : new Color(0.74f, 0.79f, 0.91f));
                    t.gameObject.SetActive(false);
                    _tiles[x, y] = t;
                }
        }

        public void Init(int w, int h)
        {
            Clear();
            Width = w; Height = h;
            Origin = new Vector3(-w * Cell * 0.5f, 0f, TopZ - h * Cell);
            for (int x = 0; x < MaxSize; x++)
                for (int y = 0; y < MaxSize; y++)
                {
                    bool on = x < w && y < h;
                    _tiles[x, y].gameObject.SetActive(on);
                    if (on) _tiles[x, y].transform.position = CellCenter(new Vector2Int(x, y)) + Vector3.down * 0.05f;
                }
            _panel.transform.position = new Vector3(0, -0.16f, Origin.z + h * Cell * 0.5f);
            _panel.transform.localScale = new Vector3(w * Cell + 0.3f, 0.2f, h * Cell + 0.3f);
        }

        public void Clear()
        {
            for (int x = 0; x < MaxSize; x++)
                for (int y = 0; y < MaxSize; y++) { _state[x, y] = CellState.Empty; _occupant[x, y] = null; }
            Buses.Clear();
        }

        public Vector3 CellCenter(Vector2Int c) => Origin + new Vector3((c.x + 0.5f) * Cell, 0f, (c.y + 0.5f) * Cell);
        public bool InBounds(Vector2Int c) => c.x >= 0 && c.y >= 0 && c.x < Width && c.y < Height;
        public bool IsEmpty(Vector2Int c) => _state[c.x, c.y] == CellState.Empty;

        public void Place(BusController b)
        {
            for (int i = 0; i < b.Length; i++)
            {
                var c = b.Tail + b.Dir.ToVec() * i;
                _state[c.x, c.y] = CellState.Occupied;
                _occupant[c.x, c.y] = b;
            }
            Buses.Add(b);
        }

        public void Remove(BusController b)
        {
            for (int i = 0; i < b.Length; i++)
            {
                var c = b.Tail + b.Dir.ToVec() * i;
                if (InBounds(c) && _occupant[c.x, c.y] == b) { _state[c.x, c.y] = CellState.Empty; _occupant[c.x, c.y] = null; }
            }
            Buses.Remove(b);
        }

        /// <summary>Quet tung o tu dau xe toi mep luoi theo huong dau xe. true = duong thong.</summary>
        public bool CheckExit(BusController bus, out BusController blocker, out int freeCells)
        {
            blocker = null; freeCells = 0;
            var step = bus.Dir.ToVec();
            var c = bus.Head + step;
            while (InBounds(c))
            {
                if (_state[c.x, c.y] == CellState.Occupied && _occupant[c.x, c.y] != bus)
                { blocker = _occupant[c.x, c.y]; return false; }
                freeCells++;
                c += step;
            }
            return true;
        }

        public bool AnyMovable()
        {
            for (int i = 0; i < Buses.Count; i++)
                if (CheckExit(Buses[i], out _, out _)) return true;
            return false;
        }
    }
}
