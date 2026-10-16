using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BusJam
{
    /// <summary>UI tao bang code: HUD (level, so khach con lai, Restart) + popup Win/Lose.</summary>
    public class UIManager : MonoBehaviour
    {
        Font _font;
        Text _level, _info, _title, _btnText;
        GameObject _popup;
        Action _cb;

        public void Build(Action onRestart)
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var cgo = new GameObject("UI Canvas");
            cgo.transform.SetParent(transform, false);
            var canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var sc = cgo.AddComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1080, 1920);
            sc.matchWidthOrHeight = 0.5f;
            cgo.AddComponent<GraphicRaycaster>();
            if (FindObjectOfType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            var root = cgo.transform;
            _level = MakeLabel(root, "Level", "Level 1", 60, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(40, -90), new Vector2(560, 90));
            _info = MakeLabel(root, "Info", "", 40, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(40, -170), new Vector2(600, 70));
            _level.color = _info.color = new Color(0.16f, 0.18f, 0.30f);   // chu toi tren nen sang
            MakeButton(root, "Restart", "Restart", new Vector2(1, 1), new Vector2(-40, -90), new Vector2(260, 100), new Color(0.3f, 0.35f, 0.5f), 40, onRestart);

            _popup = new GameObject("Popup", typeof(RectTransform), typeof(Image));
            _popup.transform.SetParent(root, false);
            Stretch(_popup.GetComponent<RectTransform>());
            _popup.GetComponent<Image>().color = new Color(0, 0, 0, 0.65f);
            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(_popup.transform, false);
            var pr = panel.GetComponent<RectTransform>();
            pr.anchorMin = pr.anchorMax = pr.pivot = new Vector2(0.5f, 0.5f);
            pr.sizeDelta = new Vector2(760, 520);
            panel.GetComponent<Image>().color = new Color(0.15f, 0.17f, 0.25f);
            _title = MakeLabel(panel.transform, "Title", "", 80, TextAnchor.MiddleCenter, new Vector2(0.5f, 1), new Vector2(0, -150), new Vector2(700, 140));
            var b = MakeButton(panel.transform, "Btn", "Next", new Vector2(0.5f, 0), new Vector2(0, 130), new Vector2(480, 140), new Color(0.25f, 0.7f, 0.35f), 56, () => { Hide(); _cb?.Invoke(); });
            _btnText = b.GetComponentInChildren<Text>();
            _popup.SetActive(false);
        }

        public void SetLevel(int n) => _level.text = "Level " + n;
        public void SetInfo(string s) => _info.text = s;
        public void Hide() => _popup.SetActive(false);

        public void ShowPopup(string title, string button, Color btnColor, Action cb)
        {
            _title.text = title; _btnText.text = button; _cb = cb;
            _btnText.transform.parent.GetComponent<Image>().color = btnColor;
            _popup.SetActive(true);
        }

        static void Stretch(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }

        Text MakeLabel(Transform p, string n, string s, int size, TextAnchor a, Vector2 anchor, Vector2 pos, Vector2 sz)
        {
            var go = new GameObject(n, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(p, false);
            var r = go.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = r.pivot = anchor; r.anchoredPosition = pos; r.sizeDelta = sz;
            var t = go.GetComponent<Text>();
            t.font = _font; t.text = s; t.fontSize = size; t.alignment = a; t.color = Color.white; t.fontStyle = FontStyle.Bold;
            t.raycastTarget = false;
            return t;
        }

        Button MakeButton(Transform p, string n, string label, Vector2 anchor, Vector2 pos, Vector2 sz, Color c, int fontSize, Action onClick)
        {
            var go = new GameObject(n, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(p, false);
            var r = go.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = r.pivot = anchor; r.anchoredPosition = pos; r.sizeDelta = sz;
            var img = go.GetComponent<Image>(); img.color = c;
            var btn = go.GetComponent<Button>(); btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick?.Invoke());
            var t = MakeLabel(go.transform, "Text", label, fontSize, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, sz);
            Stretch(t.rectTransform);
            return btn;
        }
    }
}
