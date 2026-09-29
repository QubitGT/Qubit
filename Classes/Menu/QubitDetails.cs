using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Qubit.Menu;
using Qubit.Mods;
using Qubit.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Qubit.Classes.Menu
{
    
    public sealed class QubitDetails : MonoBehaviour
    {
        private static AssetBundle bundle;
        private static QubitDetails current;
        private static string selectedMod;
        private static int mode, candidate, descriptionPage;
        private static ButtonInfo dotVisible, lineVisible, dotSize, dotOpacity, lineWidth, lineOpacity, pointerColor, themeEnabled;
        private static readonly Color Purple = new Color32(178, 112, 251, 255);
        private Transform face;
        private Canvas canvas;
        private GameObject pointer;
        private Transform dot;
        private LineRenderer line;
        private Material dotMaterial, lineMaterial;
        private Renderer touchRenderer;
        private bool touchWasVisible;
        private ButtonCollider[] targets;
        private bool wasPressed = true;
        private readonly System.Collections.Generic.Dictionary<Graphic, Color> originalColors = new System.Collections.Generic.Dictionary<Graphic, Color>();
        private readonly System.Collections.Generic.Dictionary<Image, Sprite> originalSprites = new System.Collections.Generic.Dictionary<Image, Sprite>();
        private readonly System.Collections.Generic.Dictionary<Image, Image.Type> originalTypes = new System.Collections.Generic.Dictionary<Image, Image.Type>();
        private float refreshAt;

        public static void Register()
        {
            dotVisible = Setting("Qubit Pointer Dot", "Shows the small dot at the pointer hit position.", true);
            lineVisible = Setting("Qubit Pointer Line", "Shows a faint line from your controller to the menu.", true);
            dotSize = Number("Qubit Dot Size", new[] { 2f, 3f, 4f, 6f, 8f }, 4f, "mm");
            dotOpacity = Number("Qubit Dot Opacity", new[] { 25f, 50f, 75f, 100f }, 100f, "%");
            lineWidth = Number("Qubit Line Width", new[] { .3f, .65f, 1f, 1.5f }, .65f, "mm");
            lineOpacity = Number("Qubit Line Opacity", new[] { 1f, 2f, 4.5f, 8f, 15f, 30f }, 4.5f, "%");
            pointerColor = Number("Qubit Pointer Color", new[] { 0f, 1f, 2f, 3f }, 0f, "(0 purple, 1 white, 2 cyan, 3 pink)");
            themeEnabled = Setting("Qubit Apply Theme Colors", "Applies the chosen menu theme to Qubit. Disable to restore the picture's purple and black colors.", false);
            themeEnabled.enableMethod = themeEnabled.disableMethod = () => { if (Main.menu != null) ApplyTheme(Main.menu); };
        }
        private static ButtonInfo Setting(string name, string tip, bool enabled)
        {
            var b = new ButtonInfo { buttonText = name, toolTip = tip, isSetting = true, legal = true, enabled = enabled };
            Buttons.AddButton(Buttons.GetCategory("Menu Settings"), b);
            return b;
        }
        private static ButtonInfo Number(string name, float[] values, float initial, string unit)
        {
            var b = new ButtonInfo { buttonText = name, toolTip = "Adjusts " + name.ToLowerInvariant() + ".", isSetting = true, legal = true, isTogglable = false, incremental = true, value = initial };
            b.onValueChanged = () => {
                if (!values.Contains(b.GetValue<float>())) b.value = initial;
                b.overlapText = name + ": " + b.GetValue<float>().ToString("0.##") + " " + unit;
            };
            b.cycleValue = forward => {
                int index = Array.IndexOf(values, b.GetValue<float>());
                b.value = values[(index + (forward ? 1 : values.Length - 1)) % values.Length];
                b.onValueChanged(); Preferences.SaveButton(b);
            };
            b.method = () => b.cycleValue(true);
            b.onValueChanged(); Buttons.AddButton(Buttons.GetCategory("Menu Settings"), b);
            return b;
        }
        public static void Attach(Transform art, Transform front)
        {
            try
            {
                if (bundle == null)
                {
                    using (var stream = typeof(QubitDetails).Assembly.GetManifestResourceStream("Qubit.Resources.qubitdetails"))
                    using (var memory = new MemoryStream())
                    { stream.CopyTo(memory); bundle = AssetBundle.LoadFromMemory(memory.ToArray()); }
                }
                var panel = Instantiate(bundle.LoadAsset<GameObject>("QubitDetails"), art, false);
                panel.transform.localRotation = Quaternion.identity;
                QubitMenuBody.AttachDetails(art, panel.transform);
                current = panel.AddComponent<QubitDetails>();
                current.face = front;
                current.canvas = panel.GetComponent<Canvas>();
                current.canvas.renderMode = RenderMode.WorldSpace;
                foreach (Graphic g in panel.GetComponentsInChildren<Graphic>()) g.material = null;
                QubitFonts.Apply(panel);
                current.Bind("Info", () => { mode = 0; current.Refresh(); });
                current.Bind("Fonts", () => current.Open(1));
                current.Bind("Themes", () => current.Open(2));
                current.Bind("Previous", () => current.Browse(-1));
                current.Bind("Next", () => current.Browse(1));
                current.Bind("Apply", () => current.Apply());
                current.Bind("Cancel", () => { current.Open(mode); mode = 0; current.Refresh(); });
                current.pointer = Instantiate(bundle.LoadAsset<GameObject>("QubitPointer"), panel.transform, false);
                current.dot = current.pointer.transform.Find("Dot");
                current.line = current.pointer.GetComponent<LineRenderer>();
                current.lineMaterial = new Material(current.line.sharedMaterial);
                current.line.sharedMaterial = current.lineMaterial;
                current.dotMaterial = new Material(current.dot.GetComponent<Renderer>().sharedMaterial);
                current.dot.GetComponent<Renderer>().sharedMaterial = current.dotMaterial;
                current.targets = Main.menu.GetComponentsInChildren<ButtonCollider>();
                foreach (var graphic in front.GetComponentsInChildren<Graphic>(true)) current.originalColors[graphic] = graphic.color;
                foreach (var image in front.GetComponentsInChildren<Image>(true)) { current.originalSprites[image] = image.sprite; current.originalTypes[image] = image.type; }
                ApplyTheme(Main.menu);
                current.Refresh();
            }
            catch (Exception ex) { Qubit.Managers.LogManager.LogError("Qubit details failed to load: " + ex); }
        }
        private void Bind(string name, Action action)
        {
            var rect = (RectTransform)transform.Find(name);
            var box = rect.gameObject.AddComponent<BoxCollider>();
            box.isTrigger = true; box.center = new Vector3(rect.rect.width / 2, -20, -5);
            box.size = new Vector3(rect.rect.width, 40, 8);
            var handler = rect.gameObject.AddComponent<ButtonCollider>();
            handler.qubitFront = face; handler.qubitRoot = Main.menu;
            handler.relatedText = "Qubit " + name; handler.qubitAction = action;
        }
        public static void Inspect(string name)
        {
            if (current == null || Buttons.GetIndex(name) == null || name == selectedMod) return;
            selectedMod = name; descriptionPage = 0;
            if (mode == 0) current.Refresh();
        }
        public static bool Preview(string name, bool forward)
        {
            if (current == null || Main.menu == null || !current.transform.IsChildOf(Main.menu.transform) || !current.gameObject.activeInHierarchy || Main.CurrentPrompt != null || Main.clickGUI) return false;
            int requested = name == "Qubit Font" || name == "Change Font Type" ? 1 : name == "Change Menu Theme" ? 2 : 0;
            if (requested == 0) return false;
            if (mode != requested) current.Open(requested);
            else current.Browse(forward ? 1 : -1);
            return true;
        }
        private void Open(int tab)
        {
            mode = tab; candidate = tab == 1 ? Math.Max(0, QubitFonts.Options.ToList().IndexOf(QubitFonts.Selected)) : Mathf.Clamp(Main.themeType, 0, Settings.Themes.Count - 1);
            Refresh();
        }
        private void Browse(int delta)
        {
            if (mode == 0) descriptionPage = Math.Max(0, descriptionPage + delta);
            else { int count = mode == 1 ? QubitFonts.Options.Count : Settings.Themes.Count; candidate = (candidate + delta + count) % count; }
            Refresh();
        }
        private void Apply()
        {
            if (mode == 1) QubitFonts.Select(QubitFonts.Options[candidate]);
            if (mode == 2)
            {
                var custom = Buttons.GetIndex("Custom Menu Theme");
                if (custom != null) custom.SetEnabled(false);
                var setting = Buttons.GetIndex("Change Menu Theme");
                setting.value = Settings.Themes[candidate].Name; setting.onValueChanged?.Invoke(); Preferences.SaveButton(setting);
                themeEnabled.SetEnabled(true);
                ApplyTheme(Main.menu);
            }
            Refresh();
        }
        private Text Label(string name) => transform.Find(name).GetComponent<Text>();
        private void Refresh()
        {
            if (canvas == null) return;
            var info = selectedMod == null ? null : Buttons.GetIndex(selectedMod);
            Label("Title").text = mode == 1 ? "Font preview" : mode == 2 ? "Theme preview" : info?.overlapText ?? info?.buttonText ?? "Select a mod";
            Label("Title").resizeTextForBestFit = true; Label("Title").resizeTextMinSize = 13; Label("Title").resizeTextMaxSize = 25;
            Label("Status").text = mode != 0 ? "Preview only / Apply to save" : info == null ? "Point at a row to inspect it" : info.isTogglable ? (info.enabled ? "ENABLED" : "DISABLED") : "ACTION / SETTING";
            string description = info?.toolTip ?? "Point at a mod to read its pop-up description. Use Fonts or Themes to try a style before applying it. Pointer controls are in Menu Settings.";
            description = Regex.Replace(description, "<[^>]+>", "");
            var pages = new System.Collections.Generic.List<string>();
            while (description.Length > 160)
            {
                int end = description.LastIndexOf(' ', 160); if (end < 1) end = 160;
                pages.Add(description.Substring(0, end)); description = description.Substring(end).TrimStart();
            }
            pages.Add(description); descriptionPage = Mathf.Clamp(descriptionPage, 0, pages.Count - 1);
            Label("Description").text = mode == 1 ? QubitFonts.Options[candidate] + "\n\nThe quick brown fox jumps over the lazy dog." : mode == 2 ? Settings.Themes[candidate].Name + "\n\nPreview of the menu background, button and text colors." : pages[descriptionPage];
            Label("Description").resizeTextForBestFit = true; Label("Description").resizeTextMinSize = 12; Label("Description").resizeTextMaxSize = 19;
            Label("Hint").text = mode == 0 ? "Description " + (descriptionPage + 1) + "/" + pages.Count + " / < > to browse" : "Preview only - choose Apply to save";
            transform.Find("Apply").gameObject.SetActive(mode != 0);
            transform.Find("Cancel").gameObject.SetActive(mode != 0);
            var font = QubitFonts.Resolve(mode == 1 ? QubitFonts.Options[candidate] : QubitFonts.Selected);
            Label("PreviewText").font = Label("PreviewSample").font = font;
            Label("PreviewText").text = "Favorite Mods";
            Label("PreviewText").resizeTextForBestFit = Label("PreviewSample").resizeTextForBestFit = true;
            Label("PreviewText").resizeTextMinSize = Label("PreviewSample").resizeTextMinSize = 12;
            Label("PreviewText").resizeTextMaxSize = Label("PreviewSample").resizeTextMaxSize = 21;
            PreviewColors();
        }
        private void PreviewColors()
        {
            var theme = mode == 2 ? Settings.Themes[candidate] : null;
            transform.Find("Preview").GetComponent<Image>().color = theme == null ? Purple : theme.Background().GetCurrentColor();
            transform.Find("PreviewRow").GetComponent<Image>().color = theme == null ? Color.black : theme.ButtonColors()[0].GetCurrentColor();
            Label("PreviewText").color = theme == null ? Purple : theme.TextColors()[1].GetCurrentColor();
            Label("PreviewSample").color = Label("PreviewHeading").color = theme == null ? Color.black : theme.TextColors()[0].GetCurrentColor();
        }
        public static void ApplyTheme(GameObject root)
        {
            var front = root.transform.Find("Qubit Artwork/Front"); if (front == null) return;
            bool apply = themeEnabled != null && themeEnabled.enabled;
            if (current == null) return;
            if (!apply)
            {
                foreach (var pair in current.originalColors) if (pair.Key != null) pair.Key.color = pair.Value;
                foreach (var pair in current.originalSprites) if (pair.Key != null) pair.Key.sprite = pair.Value;
                foreach (var pair in current.originalTypes) if (pair.Key != null) pair.Key.type = pair.Value;
                return;
            }
            foreach (var image in front.GetComponentsInChildren<Image>(true))
            {
                bool row = image.name.StartsWith("Row", StringComparison.Ordinal);
                var action = image.GetComponentInChildren<ButtonCollider>();
                bool enabled = action != null && Buttons.GetIndex(action.relatedText)?.enabled == true;
                if (row) image.color = Main.buttonColors[enabled ? 1 : 0].GetCurrentColor();
                else if (image.name == "Body")
                {
                    image.sprite = current.transform.Find("Frame").GetComponent<Image>().sprite;
                    image.type = Image.Type.Sliced;
                    image.color = Main.backgroundColor.GetCurrentColor();
                }
            }
            foreach (var text in front.GetComponentsInChildren<Text>(true))
            {
                var action = text.transform.parent.GetComponentInChildren<ButtonCollider>();
                bool enabled = action != null && Buttons.GetIndex(action.relatedText)?.enabled == true;
                text.color = Main.textColors[enabled ? 2 : 1].GetCurrentColor();
            }
        }
        private void LateUpdate()
        {
            if (Main.menu == null || !transform.IsChildOf(Main.menu.transform)) { pointer.SetActive(false); return; }
            var touch = Main.reference == null ? null : Main.reference.GetComponent<Renderer>();
            if (touch != touchRenderer)
            {
                RestoreTouchPointer();
                touchRenderer = touch;
                if (touchRenderer != null) touchWasVisible = touchRenderer.enabled;
            }
            if (touchRenderer != null) touchRenderer.enabled = false;
            bool pc = Main.isOnPC && Main.TPC != null;
            var viewer = pc ? Main.TPC.transform.position : GorillaTagger.Instance.headCollider.transform.position;
            canvas.enabled = Vector3.Dot(viewer - face.position, -face.forward) >= 0;
            pointer.SetActive(canvas.enabled);
            if (!canvas.enabled) return;
            if (Time.unscaledTime >= refreshAt) { PreviewColors(); if (themeEnabled.enabled) ApplyTheme(Main.menu); refreshAt = Time.unscaledTime + .1f; }
            if (Main.joystickMenu) Inspect(Main.joystickSelectedButton);
            bool left = Main.reference != null && Main.reference.transform.parent == GorillaTagger.Instance.leftHandTransform;
            var hand = ControllerUtilities.GetTrueHandPosition(left);
            Ray ray = pc && Mouse.current != null ? Main.TPC.ScreenPointToRay(Mouse.current.position.ReadValue()) : new Ray(hand.position, hand.forward);
            bool pressed = pc ? Mouse.current != null && Mouse.current.leftButton.isPressed : (left ? Main.leftTrigger : Main.rightTrigger) > .5f;
            ButtonCollider target = null; RaycastHit closest = default; float distance = 3f;
            foreach (var item in targets)
            {
                if (item == null || !item.gameObject.activeInHierarchy || item.qubitRoot != Main.menu) continue;
                var collider = item.GetComponent<Collider>();
                if (collider != null && collider.Raycast(ray, out var hit, distance)) { distance = hit.distance; closest = hit; target = item; }
            }
            bool hitMenu = target != null && Vector3.Dot(ray.origin - face.position, -face.forward) > 0;
            dot.gameObject.SetActive(hitMenu && dotVisible.enabled);
            line.enabled = hitMenu && lineVisible.enabled;
            if (hitMenu)
            {
                Color color = new[] { Purple, Color.white, Color.cyan, new Color(1, .35f, .75f) }[Mathf.Clamp(pointerColor.GetValue<int>(), 0, 3)];
                dot.position = closest.point - ray.direction * .001f;
                var scale = dot.parent.lossyScale;
                float size = dotSize.GetValue<float>() * .001f;
                dot.localScale = new Vector3(size / Mathf.Abs(scale.x), size / Mathf.Abs(scale.y), size / Mathf.Abs(scale.z));
                color.a = dotOpacity.GetValue<float>() / 100f; dotMaterial.color = color;
                color.a = lineOpacity.GetValue<float>() / 100f; line.startColor = line.endColor = color;
                line.startWidth = line.endWidth = lineWidth.GetValue<float>() * .001f;
                line.SetPosition(0, ray.origin); line.SetPosition(1, closest.point);
                Inspect(target.relatedText);
                if (!pc && pressed && !wasPressed && !Main.joystickMenu) target.Press(true);
            }
            wasPressed = pressed;
        }
        private void OnDestroy()
        {
            if (current == this) current = null;
            if (dotMaterial != null) Object.Destroy(dotMaterial);
            if (lineMaterial != null) Object.Destroy(lineMaterial);
        }
        private void RestoreTouchPointer()
        {
            if (touchRenderer != null) touchRenderer.enabled = touchWasVisible;
            touchRenderer = null;
        }
        private void OnDisable() { RestoreTouchPointer(); wasPressed = true; }
    }
}
