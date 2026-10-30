using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace BusJam
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager I { get; private set; }
        const string _keyLevel = "BusJam_Level";

        [Header("Data")]
        public List<LevelDataSO> levels = new List<LevelDataSO>();
        [Header("Background")]
        public Texture2D backgroundTexture;
        [Header("3D models (de trong = hinh khoi placeholder)")]
        public GameObject busModel;
        public GameObject passengerModel;
        [Tooltip("Neu dau xe quay nguoc: dat 180 (hoac 90/270)")] public float busYawOffset = 0f;
        [Tooltip("Neu nhan vat quay mat sai huong: dat 180")] public float passengerYawOffset = 0f;
        public float passengerHeight = 0.9f;
        [Tooltip("true: khach ngoi tren GridPartPrefab (Sitting). false: chay vao xe roi bien mat")] public bool showSeatedPassengers = true;
        [Tooltip("Ti le thu nho cua khach khi ngoi tren xe")] public float seatedScale = 0.5f;
        [Tooltip("Ha khach xuong (ti le chieu cao) neu bi noi len khoi ghe; tang neu bi lun vao ghe thi giam")] public float seatDrop = 0.25f;
        [Tooltip("Xoay huong ngoi: 0/90/180/270")] public float seatYaw = 0f;
        [Tooltip("Lech them vi tri ngoi (toa do local cua xe)")] public Vector3 seatOffset = Vector3.zero;
        [Tooltip("Tu khoa ten material than xe de doi mau, vd: green,body. De trong = tu dong tim material mau don sac")] public string busTintKeywords = "";
        public bool tintBusBody = true;

        [Header("UI Prefabs (Tuy chon neu chua co trong Scene)")]
        public GameObject hudCanvasPrefab;
        public GameObject popupCanvasPrefab;
        public GameObject vfxCanvasPrefab;

        public SettingsManager Settings { get; private set; }
        public HapticManager Haptics { get; private set; }
        public GridManager Grid { get; private set; }
        public BayManager Bays { get; private set; }
        public PassengerQueue Queue { get; private set; }
        public PoolHub Pools { get; private set; }
        public ParticlePool Fx { get; private set; }
        public AudioManager Audio { get; private set; }
        public CameraShake Shake { get; private set; }
        public UIManager UI { get; private set; }

        /// <summary>Co khoa input (_isBusy): bus dang chay tu bai ra ben, hoac dang nap level.</summary>
        public bool IsBusy => _isBusy;
        public bool CanPlay => _state == GameState.Playing;

        enum GameState { Loading, Playing, Won, Lost }
        GameState _state = GameState.Loading;
        bool _isBusy;
        bool _boardRunning, _again;
        int _departing;
        int _levelIndex;
        Coroutine _boardCo;
        Camera _cam;

        T Make<T>(string n) where T : Component
        {
            var g = new GameObject(n);
            g.transform.SetParent(transform, false);
            return g.AddComponent<T>();
        }

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            transform.localScale = Vector3.one;

            BusController.TintKeywords = busTintKeywords;
            BusController.TintBody = tintBusBody;
            PassengerController.StandHeight = passengerHeight;
            PassengerController.ShowSeated = showSeatedPassengers;
            PassengerController.SeatedScale = seatedScale;
            PassengerController.SeatDrop = seatDrop;
            PassengerController.SeatYaw = seatYaw;
            PassengerController.SeatOffset = seatOffset;

            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.Portrait;
            Input.multiTouchEnabled = false;
            DOTween.SetTweensCapacity(500, 100);

            SetupCamera();
            SetupLighting();
            Make<BackgroundController>("Background").Init(_cam, backgroundTexture);

            Settings = FindObjectOfType<SettingsManager>();
            if (Settings == null) Settings = Make<SettingsManager>("Settings");

            Haptics = FindObjectOfType<HapticManager>();
            if (Haptics == null) Haptics = Make<HapticManager>("Haptics");

            Audio = Make<AudioManager>("Audio");
            Shake = Make<CameraShake>("CameraShake");
            Shake.Init(_cam.transform);

            Pools = Make<PoolHub>("Pools");
            Pools.Init(busModel, passengerModel, busYawOffset, passengerYawOffset);

            Fx = Make<ParticlePool>("Fx");
            Fx.Init();

            Grid = Make<GridManager>("Grid");
            Bays = Make<BayManager>("Bays");
            Queue = Make<PassengerQueue>("Queue");

            UI = FindObjectOfType<UIManager>();
            if (UI == null) UI = Make<UIManager>("UI");

            Make<InputHandler>("Input");
        }

        void Start()
        {
            if (levels == null || levels.Count == 0)
            {
                Debug.LogError("GameManager: chua gan LevelDataSO nao. Chay menu BusJam > Setup Everything.");
                return;
            }
            _levelIndex = PlayerPrefs.GetInt(_keyLevel, 0);
            StartCoroutine(LoadLevelRoutine(_levelIndex));
        }

        void SetupCamera()
        {
            _cam = Camera.main;
            if (_cam == null)
            {
                var g = new GameObject("Main Camera") { tag = "MainCamera" };
                _cam = g.AddComponent<Camera>();
                g.AddComponent<AudioListener>();
            }
            _cam.orthographic = true;
            _cam.transform.rotation = Quaternion.Euler(70f, 0f, 0f);       // nhin tu tren xuong, nghieng 70 do
            _cam.transform.position = new Vector3(0f, 0f, 0.45f) - _cam.transform.forward * 40f;
            _cam.nearClipPlane = 0.1f;
            _cam.farClipPlane = 200f;
            float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
            _cam.orthographicSize = Mathf.Max(9.6f, 5.4f / aspect);   // luon thay du be rong 10.8 unit
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = new Color(0.82f, 0.86f, 0.94f);
        }

        void SetupLighting()
        {
            var lights = FindObjectsOfType<Light>();
            Light dirLight = null;
            foreach (var l in lights)
            {
                if (l.type == LightType.Directional) { dirLight = l; break; }
            }

            if (dirLight == null)
            {
                var lightGo = new GameObject("Directional Light");
                dirLight = lightGo.AddComponent<Light>();
                dirLight.type = LightType.Directional;
            }
            dirLight.color = new Color(1f, 0.98f, 0.92f);
            dirLight.intensity = 0.5f;
            dirLight.transform.rotation = Quaternion.Euler(55f, -30f, 0f);

            RenderSettings.sun = dirLight;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.85f, 0.88f, 0.93f);
            RenderSettings.ambientIntensity = 1.2f;
        }

        // ================= Level flow =================
        void CleanupBoard()
        {
            if (_boardCo != null) StopCoroutine(_boardCo);
            _boardCo = null;
            _boardRunning = false;
            _again = false;
            _departing = 0;
            Pools.ReleaseAll();     // thu hoi xe + hanh khach ve Pool (khong Destroy)
            Fx.ReleaseAll();
            Grid.Clear();
            Bays.Clear();
            Queue.Clear();
        }

        IEnumerator LoadLevelRoutine(int absIndex)
        {
            _isBusy = true;
            _state = GameState.Loading;
            UI.Hide();
            CleanupBoard();

            var data = levels[absIndex % levels.Count];
            if (!data.Validate(out var err)) Debug.LogError($"Level '{data.name}' invalid: {err}");

            Grid.Init(data.width, data.height);
            Bays.Init(data.bayCount, Grid.Top);
            for (int i = 0; i < data.buses.Count; i++)
            {
                var b = Pools.GetBus();
                b.Init(data.buses[i], Grid, i * 0.04f);
                Grid.Place(b);
            }
            Queue.Init(data.passengerOrder, Bays.QueueFront);
            Physics.SyncTransforms();

            UI.SetLevel(absIndex + 1);
            UI.SyncCoinsDisplay();
            UpdateHud();
            yield return new WaitForSeconds(0.5f);
            _state = GameState.Playing;
            _isBusy = false;
        }

        void UpdateHud() => UI.SetInfo("Passengers: " + Queue.Remaining);

        public void RestartLevel()
        {
            if (_state == GameState.Loading) return;
            StartCoroutine(LoadLevelRoutine(_levelIndex));
        }

        // ================= Input =================
        public void OnBusTapped(BusController bus)
        {
            if (_isBusy || _state != GameState.Playing || bus.State != BusController.BusState.Parked) return;
            AudioManager.I.Play(Sfx.Click);
            if (Haptics != null) Haptics.LightImpact();

            if (!Grid.CheckExit(bus, out var blocker, out _))
            {
                if (Haptics != null) Haptics.Warning();
                bus.PlayBlocked(blocker);               // nhich 0.2m roi giat nguoc + rung
                return;
            }
            int bay = Bays.Reserve(bus);
            if (bay < 0)                                // ben da day
            {
                if (Haptics != null) Haptics.Warning();
                Bays.PlayFullError();
                AudioManager.I.Play(Sfx.Error);
                Shake.Light();
                return;
            }
            Grid.Remove(bus);
            _isBusy = true;
            bus.LaunchToBay(Grid, Bays.BayPos(bay), Grid.Top + 0.9f, () =>
            {
                _isBusy = false;
                TryStartBoarding();
            });
        }

        // ================= Boarding =================
        void TryStartBoarding()
        {
            if (_state != GameState.Playing) return;
            if (_boardRunning) { _again = true; return; }
            _boardCo = StartCoroutine(BoardingRoutine());
        }

        IEnumerator BoardingRoutine()
        {
            _boardRunning = true;
            while (_state == GameState.Playing)
            {
                var p = Queue.Peek();
                if (p == null) break;
                var bus = Bays.FindBoardable(p.ColorID);
                if (bus == null)
                {
                    if (_again) { _again = false; continue; }
                    break;
                }
                _again = false;
                p = Queue.Dequeue();
                UpdateHud();
                int seat = bus.Boarded;
                bus.Boarded++;
                yield return p.BoardBus(bus, seat).WaitForCompletion();
                if (_state != GameState.Playing) break;

                if (bus.Boarded >= BusController.Capacity)
                {
                    yield return new WaitForSeconds(0.12f);   // dong cua
                    Bays.Free(bus);                            // giai phong vi tri do ngay
                    _departing++;
                    var leaving = bus;
                    leaving.DepartFromBay(Grid.Top + 0.9f, () => OnBusLeft(leaving));
                }
            }
            _boardRunning = false;
            _boardCo = null;
            EvaluateEnd();
        }

        void OnBusLeft(BusController bus)
        {
            _departing--;
            for (int i = bus.Riders.Count - 1; i >= 0; i--) Pools.ReleasePassenger(bus.Riders[i]);
            bus.Riders.Clear();
            Pools.ReleaseBus(bus);
            EvaluateEnd();
        }

        // ================= Win / Lose =================
        bool IsWin() => Queue.Remaining == 0 && Grid.BusCount == 0 && Bays.UsedCount == 0 && _departing == 0;

        bool IsLose()
        {
            var p = Queue.Peek();
            if (p == null) return false;
            if (Bays.FindBoardable(p.ColorID) != null) return false;
            if (Bays.FreeCount == 0) return Bays.AllArrived;   // ben day + khach dau hang khong trung xe nao (deadlock)
            return !Grid.AnyMovable();                         // con cho nhung khong xe nao thoat duoc
        }

        void EvaluateEnd()
        {
            if (_state != GameState.Playing) return;
            if (IsWin()) { Win(); return; }
            if (_isBusy || _boardRunning) return;
            if (IsLose()) Lose();
        }

        void Win()
        {
            _state = GameState.Won;
            _isBusy = true;
            PlayerPrefs.SetInt(_keyLevel, _levelIndex + 1);
            PlayerPrefs.Save();
            AudioManager.I.Play(Sfx.Win);
            if (Haptics != null) Haptics.Success();
            Shake.Strong();
            Fx.Play(FxType.Confetti, new Vector3(-3.5f, 0.5f, -6f));
            Fx.Play(FxType.Confetti, new Vector3(3.5f, 0.5f, -6f));
            Fx.Play(FxType.Confetti, new Vector3(0f, 0.5f, -7f));

            StartCoroutine(WinDelayed());
        }

        IEnumerator WinDelayed()
        {
            yield return new WaitForSeconds(0.8f);
            UI.ShowWin(25, () =>
            {
                _levelIndex++;
                StartCoroutine(LoadLevelRoutine(_levelIndex));
            });
        }

        void Lose()
        {
            _state = GameState.Lost;
            _isBusy = true;
            AudioManager.I.Play(Sfx.Lose);
            if (Haptics != null) Haptics.Warning();
            Shake.Strong();
            StartCoroutine(LoseDelayed());
        }

        IEnumerator LoseDelayed()
        {
            yield return new WaitForSeconds(0.8f);
            UI.ShowLose(() => StartCoroutine(LoadLevelRoutine(_levelIndex)));
        }
    }
}
