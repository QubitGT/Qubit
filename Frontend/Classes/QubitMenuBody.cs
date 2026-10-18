/*
 * Qubit Menu  Frontend/Classes/QubitMenuBody.cs
 * A community driven mod menu for Gorilla Tag with over 1000+ mods
 *
 * Copyright (C) 2026  Qubit Software  /Derivative work of Seralyth Software, Goldentrophy Software
 * https://github.com/QubitGT/Qubit
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using Qubit.Core;
using GorillaLocomotion;
using Photon.Pun;
using Qubit.Frontend.Resources;
using Qubit.Libraries.Controls;
using Qubit.Libraries.Core;
using Qubit.Modules;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR;
using static Qubit.Frontend.Core;
using static Qubit.Frontend.Keyboard;
using static Qubit.Frontend.Prompts;
using static Qubit.Frontend.Settings;
using Object = UnityEngine.Object;

namespace Qubit.Frontend.Classes
{
 public static class QubitMenuBody
    {
        private const float HalfDepth = .0125f;
        private static readonly Dictionary<Vector3, Mesh> meshes = new Dictionary<Vector3, Mesh>();
        private static Material material;
        private static Sprite disconnectIcon;

        private static void LayoutPageButtons(Transform front)
        {
            foreach (string name in new[] { "Previous", "Next" })
            {
                var button = (RectTransform)front.Find(name);
                button.anchoredPosition = new Vector2(name == "Previous" ? 142 : 334, -732);
                button.sizeDelta = new Vector2(64, 48);
                var label = button.GetComponentInChildren<TMP_Text>(true);
                label.rectTransform.anchorMin = Vector2.zero;
                label.rectTransform.anchorMax = Vector2.one;
                label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
                label.alignment = TextAlignmentOptions.Center;
                label.fontSize = 36;
            }
        }

        private static void LayoutHeader(Transform front)
        {
            var titleRect = (RectTransform)front.Find("Home/Title");
            titleRect.anchoredPosition = new Vector2(76, -9);
            front.Find("Category").gameObject.SetActive(false);
            ((RectTransform)front.Find("Clock")).anchoredPosition = new Vector2(370, -20);
            ((RectTransform)front.Find("Date")).anchoredPosition = new Vector2(370, -42);
            ((RectTransform)front.Find("Search")).anchoredPosition = new Vector2(424, -72);
            var disconnect = (RectTransform)front.Find("Disconnect");
            disconnect.anchoredPosition = new Vector2(474, -72);
            foreach (var label in disconnect.GetComponentsInChildren<TMP_Text>(true))
                label.gameObject.SetActive(false);

            if (disconnectIcon == null)
            {
                var texture = Qubit.Utilities.Assets.LoadTextureFromResource($"{PluginInfo.ClientResourcePath}.qubit-exit.png");
                disconnectIcon = Sprite.Create(texture, new Rect(17.02675f, 17.02675f, 85.9465f, 85.9465f),
                    new Vector2(.5f, .5f), 100);
            }
            var icon = new GameObject("Exit Icon", typeof(RectTransform), typeof(Image));
            icon.transform.SetParent(disconnect, false);
            var rect = (RectTransform)icon.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(21, 21);
            var image = icon.GetComponent<Image>();
            image.sprite = disconnectIcon;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.color = new Color32(237, 237, 249, 255);
        }

        public static void Attach(Transform art)
        {
            if (material == null)
            {
                var shader = Shader.Find("GorillaTag/UberShader") ?? Shader.Find("Universal Render Pipeline/Unlit");
                material = new Material(shader);
                material.color = new Color32(25, 29, 48, 255);
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", material.color);
            }
            art.Find("Front").localPosition = new Vector3(0, 0, -HalfDepth - .0003f);
            art.Find("Back").localPosition = new Vector3(0, 0, HalfDepth + .0003f);
            Add(art, "Shell", 0, 0, 540, 800, 36);
        }

        public static void LayoutRows(Transform front, int count)
        {
            const float top = 132, bottom = 714, gap = 10;
            float height = (bottom - top - gap * (count - 1)) / count;
            for (int i = 0; i < 8; i++)
            {
                var row = (RectTransform)front.Find("Row" + i);
                row.gameObject.SetActive(i < count);
                if (i >= count) continue;
                row.sizeDelta = new Vector2(484, height);
                row.anchoredPosition = new Vector2(28, -top - i * (height + gap));
                foreach (string name in new[] { "Label", "Indicator" })
                {
                    var label = (RectTransform)row.Find(name);
                    label.anchorMin = new Vector2(0, 0);
                    label.anchorMax = new Vector2(0, 1);
                    label.sizeDelta = new Vector2(label.sizeDelta.x, 0);
                    label.anchoredPosition = new Vector2(label.anchoredPosition.x, 0);
                }
            }
        }

        public static Transform AttachDetails(Transform art, Transform panel)
        {
            const float size = .8f;
            const float width = 420 * size, height = 601 * size;
            var body = Add(art, "InfoPanel", 568, 0, width, height, 24);
            panel.localScale = new Vector3(.001f * size, .001f * size, .001f);
            panel.localPosition = new Vector3((568 + width * .5f - 270) * .001f,
                (400 - height * .5f) * .001f, -HalfDepth - .0003f);
            return body;
        }

        private static Transform Add(Transform art, string name, float x, float y, float width, float height, float radius)
        {
            var size = new Vector3(width, height, radius);
            if (!meshes.TryGetValue(size, out var mesh))
            {
                mesh = Build(width * .001f, height * .001f, radius * .001f);
                meshes.Add(size, mesh);
            }
            var body = new GameObject("Qubit Body " + name, typeof(MeshFilter), typeof(MeshRenderer));
            body.transform.SetParent(art, false);
            body.transform.localPosition = new Vector3((x + width * .5f - 270) * .001f, (400 - y - height * .5f) * .001f, 0);
            body.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = body.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return body.transform;
        }

        private static Mesh Build(float width, float height, float radius)
        {
            const int n = 68, segments = n / 4;
            var vertices = new Vector3[n * 2 + 2];
            var triangles = new List<int>();
            var centers = new[] {
                new Vector2(width * .5f - radius, height * .5f - radius),
                new Vector2(-width * .5f + radius, height * .5f - radius),
                new Vector2(-width * .5f + radius, -height * .5f + radius),
                new Vector2(width * .5f - radius, -height * .5f + radius)
            };
            for (int i = 0; i < n; i++)
            {
                float angle = (i / segments * 90 + i % segments * 90f / (segments - 1)) * Mathf.Deg2Rad;
                Vector2 point = centers[i / segments] + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                vertices[i] = new Vector3(point.x, point.y, -HalfDepth);
                vertices[i + n] = new Vector3(point.x, point.y, HalfDepth);
                int next = (i + 1) % n;
                triangles.AddRange(new[] { n * 2, next, i, n * 2 + 1, i + n, next + n, i, next, next + n, i, next + n, i + n });
            }
            vertices[n * 2] = new Vector3(0, 0, -HalfDepth);
            vertices[n * 2 + 1] = new Vector3(0, 0, HalfDepth);
            var mesh = new Mesh { name = "Qubit Rounded Shell", vertices = vertices, triangles = triangles.ToArray() };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
 public static readonly Quaternion QubitFaceRotation = Quaternion.Euler(180f, 90f, 90f);

   public static GameObject qubitPrefab;
public static Transform qubitFront;
       public static Canvas qubitFrontCanvas, qubitBackCanvas;
  public static TMP_Text qubitClock, qubitDate, qubitStats, qubitSearch;
                  public static float qubitStatsTime;

        public static readonly List<(string Action, RectTransform Row)> qubitJoystickTargets = new List<(string, RectTransform)>();
        public static RectTransform qubitJoystickMarker;
        public static bool joystickSelectHeld;
 public static bool qubitLoadFailed;
            public static bool UseQubit => !qubitLoadFailed && !clickGUI;
        public const int QubitMaxRows = 8;
        public static int QubitRowCount => Mathf.Clamp(_pageSize, 4, QubitMaxRows);
        public static int QubitPageCapacity => QubitRowCount - (isSearching ? 1 : 0);
        public static readonly string[] QubitHomeOrder = {
            "Settings", "Movement Mods", "Room Mods", "Players", "Advantage Mods",
            "Overpowered Mods", "Visual Mods", "Safety Mods"
        };
        public static string QubitCategoryLabel(Module info)
        {
            if (info.buttonText == "Visual Mods") return "Visuals";
            return (info.overlapText ?? info.buttonText).Replace(" Mods", "");
        }
            public static GameObject CreateMenu()

   {
qubitFront = null;
        qubitJoystickTargets.Clear();
        qubitJoystickMarker = null;
       qubitClock = qubitDate = qubitStats = qubitSearch = null;
  if (!UseQubit) return CreateLegacyMenu();
                  try
    {
 if (qubitPrefab == null) qubitPrefab = QubitAssets.Load<GameObject>("QubitMenu");
    menu = new GameObject("Qubit Menu");
 menu.transform.localScale = new Vector3(.1f, .3f, .3825f);
            menuBackground = new GameObject("Menu Background Anchor");
   menuBackground.transform.SetParent(menu.transform, false);
menuBackground.transform.localPosition = new Vector3(.5f, 0, 0);
       var art = Object.Instantiate(qubitPrefab, menu.transform, false);
  art.name = "Qubit Artwork";
                  foreach (Canvas assetCanvas in art.GetComponentsInChildren<Canvas>(true))
    {
 assetCanvas.renderMode = RenderMode.WorldSpace;
            assetCanvas.worldCamera = null;
   assetCanvas.targetDisplay = 0;
assetCanvas.enabled = true;

       }
  foreach (Transform child in art.GetComponentsInChildren<Transform>(true))
                  child.gameObject.layer = 0;
    art.transform.localPosition = new Vector3(.56f, 0, 0);
 art.transform.localRotation = QubitFaceRotation;
            art.transform.localScale = new Vector3(.6f / .3f, .6f / .3825f, .6f / .1f);
  QubitMenuBody.Attach(art.transform);
   qubitFront = art.transform.Find("Front");
if (qubitFront == null || art.transform.Find("Back") == null)
       throw new InvalidOperationException("Both Qubit faces are required.");
        QubitMenuBody.LayoutRows(qubitFront, QubitRowCount);
        LayoutHeader(qubitFront);
        LayoutPageButtons(qubitFront);
  qubitFrontCanvas = qubitFront.GetComponent<Canvas>();
                  qubitBackCanvas = art.transform.Find("Back").GetComponent<Canvas>();
    foreach (Graphic graphic in art.GetComponentsInChildren<Graphic>(true))
 if (graphic is Image) graphic.material = null;
       Fonts.Apply(art);
            canvasObj = qubitFront.gameObject;
   title = null; fpsCount = null; keyboardInputObject = null;
qubitClock = qubitFront.Find("Clock").GetComponent<TMP_Text>();
       qubitDate = qubitFront.Find("Date").GetComponent<TMP_Text>();

  qubitStats = qubitFront.Find("Stats").GetComponent<TMP_Text>();
if (CurrentPrompt != null) RenderQubitPrompt();
        else {
BindControl("Home", "Global Return", !disableReturnButton);
       BindControl("Disconnect", "Disconnect", !disableDisconnectButton);


                  BindControl("Search", "Search", !disableSearchButton);
    BindControl("Next", "NextPage", !disablePageButtons);
 BindControl("Previous", "PreviousPage", !disablePageButtons);
            var buttons = GetModules();
   int capacity = QubitPageCapacity;
pageNumber = Mathf.Clamp(pageNumber, 0, Math.Max(0, (buttons.Length - 1) / capacity));
       pageOffset = Mathf.Clamp(pageOffset, 0, Math.Max(0, buttons.Length - capacity));
  var visible = buttons.Skip(pageScrolling && !joystickMenu ? pageOffset : pageNumber * capacity).Take(capacity).ToArray();
 joystickSelectedButton = "";
            for (int i = 0; i < QubitRowCount; i++)
   {
var row = (RectTransform)qubitFront.Find("Row" + i);
       var label = row.Find("Label").GetComponent<TMP_Text>();
  label.enableAutoSizing = true; label.fontSizeMin = 12; label.fontSizeMax = 23;
                  label.textWrappingMode = TextWrappingModes.Normal;

    if (isSearching && i == 0) { qubitSearch = label; row.Find("Indicator").gameObject.SetActive(false); continue; }
 int index = i - (isSearching ? 1 : 0);
            if (index >= visible.Length) { row.gameObject.SetActive(false); continue; }
   var info = visible[index];
label.text = Manager.CurrentCategoryName == "Main" ? QubitCategoryLabel(info) : info.overlapText ?? info.buttonText;
       label.color = QubitDetails.ButtonTextColor;
       row.GetComponent<Image>().color = info.enabled ? QubitDetails.EnabledButtonColor : Color.white;
   if (!info.label)
{
       qubitJoystickTargets.Add((info.buttonText, row));
       if (info.incremental && incrementalButtons)
  {
                  AddHitbox(row, info.buttonText, -1, 0, 70);
    AddHitbox(row, info.buttonText, 1, row.rect.width - 70, 70);
    AddHitbox(row, info.buttonText, 0, 70, row.rect.width - 140, true);

 label.text = "−   " + label.text + "   +";
            }
   else AddHitbox(row, info.buttonText);
}
       }
        int totalPages = Math.Max(1, (buttons.Length + capacity - 1) / capacity);
                    qubitFront.Find("Page").gameObject.SetActive(false);
                    var emptyLabel = qubitFront.Find("Empty").GetComponent<TMP_Text>();
        emptyLabel.gameObject.SetActive(buttons.Length == 0);
        emptyLabel.text = isSearching ? "No results\nTry a different search." : "No mods to show";
                    var categoryLabel = qubitFront.Find("Category");
                    categoryLabel.GetComponent<TMP_Text>().text = "";
                    categoryLabel.gameObject.SetActive(false);
                }
                  QubitDetails.Attach(art.transform, qubitFront);
                  if (CurrentPrompt == null && joystickMenuSearching && !disableSearchButton)
                      qubitJoystickTargets.Add(("Search", (RectTransform)qubitFront.Find("Search")));
                  UpdateJoystickSelection();
    menu.transform.localScale *= scaleWithPlayer && XRSettings.isDeviceActive ? References.Player.scale * menuScale : menuScale;
 qubitStatsTime = 0;
   RecenterMenu();
            UpdateDisplay();
return menu;
       }
  catch (Exception error)
                  {
    Logs.LogError("Qubit menu could not load; using the original menu. " + error);
 if (menu != null) { menu.SetActive(false); Object.Destroy(menu); menu = null; }

            qubitLoadFailed = true;
   qubitFront = null;
return CreateLegacyMenu();
       }
  }
        public static void RenderQubitPrompt()
        {
            for (int i = 0; i < QubitMaxRows; i++) qubitFront.Find("Row" + i).gameObject.SetActive(false);
            foreach (string name in new[] { "Search", "Disconnect", "Next", "Previous", "Page", "Empty" })
                qubitFront.Find(name).gameObject.SetActive(false);
            qubitFront.Find("Category").GetComponent<TMP_Text>().text = "PROMPT";
            QubitPromptView.Attach(qubitFront, CurrentPrompt);
            BindControl("Prompt/Accept", "Accept Prompt", true);
            BindControl("Prompt/Decline", "Decline Prompt", CurrentPrompt.DeclineText != null);
            qubitJoystickTargets.Add(("Accept Prompt", (RectTransform)qubitFront.Find("Prompt/Accept")));
            if (CurrentPrompt.DeclineText != null)
                qubitJoystickTargets.Add(("Decline Prompt", (RectTransform)qubitFront.Find("Prompt/Decline")));
            foreach (string name in new[] { "Previous", "Next" })
            {
                var rect = (RectTransform)qubitFront.Find("Prompt/" + name);
                AddHitbox(rect, name == "Next" ? "NextPage" : "PreviousPage");
            }
            joystickButtonSelected = Mathf.Clamp(joystickButtonSelected, 0, qubitJoystickTargets.Count - 1);
        }

                  public static void BindControl(string path, string action, bool visible)
    {
 var rect = (RectTransform)qubitFront.Find(path);
            rect.gameObject.SetActive(visible || path == "Home");
   if (visible) AddHitbox(rect, action);
}
       public static void AddHitbox(RectTransform rect, string action, int increment = 0, float x = 0, float width = -1, bool inspectOnly = false)
  {
                  var hit = new GameObject("Hitbox " + action);
    hit.transform.SetParent(rect, false);
 hit.layer = !UnityInput.GetKey(Key.Q) && !isKeyboardPc ? 2 : 0;
            var box = hit.AddComponent<BoxCollider>();

   float w = width < 0 ? rect.rect.width : width;
box.center = new Vector3(x + w * .5f, -rect.rect.height * .5f, -5);
       box.size = new Vector3(w, rect.rect.height, 8);
  box.isTrigger = true;
                  var handler = hit.AddComponent<ButtonCollider>();
    handler.qubitRoot = menu;
 handler.qubitFront = qubitFront;
            handler.relatedText = action;
   handler.incremental = increment != 0;
handler.positive = increment > 0;
 if (inspectOnly) handler.qubitAction = () => { QubitDetails.Inspect(action); QubitDetails.Preview(action, true); };
       }
        public static void UpdateJoystickSelection()
        {
            joystickSelectedButton = "";
            if (!joystickMenu || qubitJoystickTargets.Count == 0)
            {
                if (qubitJoystickMarker != null) qubitJoystickMarker.gameObject.SetActive(false);
                QubitDetails.ApplyButtonColors(qubitFront);
                return;
            }

            joystickButtonSelected = Mathf.Clamp(joystickButtonSelected, 0, qubitJoystickTargets.Count - 1);
            var target = qubitJoystickTargets[joystickButtonSelected];
            joystickSelectedButton = target.Action;
            if (qubitJoystickMarker == null)
            {
                var marker = new GameObject("Joystick Selection", typeof(RectTransform), typeof(Image));
                qubitJoystickMarker = marker.GetComponent<RectTransform>();
                var graphic = marker.GetComponent<Image>();
                graphic.color = new Color32(220, 180, 255, 255);
                graphic.raycastTarget = false;
            }
            qubitJoystickMarker.SetParent(target.Row, false);
            qubitJoystickMarker.anchorMin = new Vector2(0, 0);
            qubitJoystickMarker.anchorMax = new Vector2(0, 1);
            qubitJoystickMarker.pivot = new Vector2(0, .5f);
            qubitJoystickMarker.anchoredPosition = new Vector2(4, 0);
            qubitJoystickMarker.sizeDelta = new Vector2(8, -8);
            qubitJoystickMarker.gameObject.SetActive(true);
            QubitDetails.ApplyButtonColors(qubitFront);
            QubitDetails.Inspect(joystickSelectedButton);
        }
  public static void UpdateDisplay()
                  {
    if (qubitFront == null || menu == null) return;
 Vector3 viewer = isOnPC && TPC != null ? TPC.transform.position : References.HeadCollider.transform.position;
            bool frontVisible = Vector3.Dot(viewer - qubitFront.position, -qubitFront.forward) >= 0;
   qubitFrontCanvas.enabled = frontVisible;

qubitBackCanvas.enabled = !frontVisible;
       if (qubitSearch != null) qubitSearch.text = keyboardInput + (Time.frameCount / 45 % 2 == 0 ? "|" : " ");
  if (Time.unscaledTime < qubitStatsTime) return;
                  qubitStatsTime = Time.unscaledTime + .25f;
    qubitClock.gameObject.SetActive(!Manager.GetIndex("Hide Qubit Clock").enabled);
 qubitDate.gameObject.SetActive(!Manager.GetIndex("Hide Qubit Date").enabled);
            DateTime now = DateTime.Now;
   qubitClock.text = now.ToString("h:mm tt", CultureInfo.InvariantCulture);
qubitDate.text = now.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);
       bool latency = Manager.GetIndex("Qubit Latency").enabled;
  qubitStats.gameObject.SetActive(latency);
                  qubitStats.text = latency ? (PhotonNetwork.InRoom ? PhotonNetwork.GetPing().ToString() : "--") + " ms" : "";
    }
    }
}
