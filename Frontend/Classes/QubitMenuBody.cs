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

            public static void Attach(Transform art)
  {
       if (material == null)
    {
 material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.SetColor("_BaseColor", new Color(.025f,.02f,.035f,1));
  }
       var front = art.Find("Front");
 front.localPosition = new Vector3(0,0,-HalfDepth-.0003f);
            var back = art.Find("Back");
  back.localPosition = new Vector3(0,0,HalfDepth+.0003f);
       Place(back,"Header",76,0,544,295);
 Place(back,"Body",176,75,444,526);
            Place(back,"UpperTab",107,307,118,108);
  Place(back,"LowerTab",107,421,118,108);
       Place(back,"OuterTab",39,334,54,54);
 Place(back,"SmallTab",60,398,40,40);
            ((RectTransform)back.Find("Logo")).anchoredPosition = new Vector2(287,-192);
  ((RectTransform)back.Find("Version")).anchoredPosition = new Vector2(99,-267);
       Add(art,"Header",0,0,544,295,40);
 Add(art,"Body",0,75,444,526,40);
            Add(art,"NextTab",395,307,118,108,36);
  Add(art,"PreviousTab",395,421,118,108,36);
       Add(art,"OuterTab",527,334,54,54,18);
 Add(art,"PageTab",520,398,40,40,12);
    }

        public static void LayoutRows(Transform front, int count)
        {
            var first = (RectTransform)front.Find("Row0");
            var second = (RectTransform)front.Find("Row1");
            var last = (RectTransform)front.Find("Row5");
            float top = first.anchoredPosition.y + first.rect.yMax;
            float bottom = last.anchoredPosition.y + last.rect.yMin;
            float gap = Mathf.Max(0, first.anchoredPosition.y + first.rect.yMin
                - second.anchoredPosition.y - second.rect.yMax);
            float height = (top - bottom - gap * (count - 1)) / count;
            for (int i = count; ; i++)
            {
                var unused = front.Find("Row" + i);
                if (unused == null) break;
                unused.gameObject.SetActive(false);
            }
            var rows = new RectTransform[count];
            for (int i = 0; i < count; i++)
            {
                rows[i] = (RectTransform)front.Find("Row" + i);
                if (rows[i] == null)
                {
                    rows[i] = Object.Instantiate(first, front, false);
                    rows[i].name = "Row" + i;
                }
            }
            for (int i = 0; i < count; i++)
            {
                var row = rows[i];
                row.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
                var position = row.anchoredPosition;
                position.y = top - i * (height + gap) - (1 - row.pivot.y) * height;
                row.anchoredPosition = position;

                var label = (RectTransform)row.Find("Label");
                label.anchorMin = new Vector2(label.anchorMin.x, 0);
                label.anchorMax = new Vector2(label.anchorMax.x, 1);
                label.sizeDelta = new Vector2(label.sizeDelta.x, 0);
                label.anchoredPosition = new Vector2(label.anchoredPosition.x, 0);
            }
        }

        public static Transform AttachDetails(Transform art, Transform panel)
        {
            
            const float size = .8f;
            const float width = 420 * size, height = 601 * size;
            var body = Add(art, "InfoPanel", 645, 0, width, height, 16 * size);
            panel.localScale = new Vector3(.001f * size, .001f * size, .001f);
            panel.localPosition = new Vector3((645 + width * .5f - 310) * .001f,
                (310 - height * .5f) * .001f, -HalfDepth - .0003f);
            return body;
        }

 private static void Place(Transform back,string name,float x,float y,float w,float h)
       {
  var rect = (RectTransform)back.Find(name);
            rect.anchoredPosition = new Vector2(x,-y);
 rect.sizeDelta = new Vector2(w,h);
       }
  private static Transform Add(Transform art,string name,float x,float y,float w,float h,float radius)
            {
 var size = new Vector3(w,h,radius);
       if (!meshes.TryGetValue(size,out var mesh))
  {
            mesh = Build(w*.001f,h*.001f,radius*.001f);
 meshes.Add(size,mesh);
       }
  var body = new GameObject("Qubit Body " + name,typeof(MeshFilter),typeof(MeshRenderer));
            body.transform.SetParent(art,false);
 body.transform.localPosition = new Vector3((x+w*.5f-310)*.001f,(310-y-h*.5f)*.001f,0);
       body.GetComponent<MeshFilter>().sharedMesh = mesh;
  body.GetComponent<MeshRenderer>().sharedMaterial = material;
            body.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
 body.GetComponent<MeshRenderer>().receiveShadows = false;
 return body.transform;
       }

  private static Mesh Build(float width,float height,float radius)
       {
 const int n = 36;
            var vertices = new Vector3[n*2+2];
  var triangles = new List<int>();
       var centers = new[] { new Vector2(width*.5f-radius,height*.5f-radius),new Vector2(-width*.5f+radius,height*.5f-radius),new Vector2(-width*.5f+radius,-height*.5f+radius),new Vector2(width*.5f-radius,-height*.5f+radius) };
 for(int i=0;i<n;i++)
            {
  float angle = (i/9*90+(i%9)*90f/8)*Mathf.Deg2Rad;
       Vector2 point = centers[i/9]+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius;
 vertices[i] = new Vector3(point.x,point.y,-HalfDepth);
            vertices[i+n] = new Vector3(point.x,point.y,HalfDepth);
  int next = (i+1)%n;
       triangles.AddRange(new[] { n*2,next,i,n*2+1,i+n,next+n,i,next,next+n,i,next+n,i+n });
 }
            vertices[n*2] = new Vector3(0,0,-HalfDepth);
  vertices[n*2+1] = new Vector3(0,0,HalfDepth);
            var mesh = new Mesh
            {
                name = "Qubit Rounded Body",
                vertices = vertices,
                triangles = triangles.ToArray()
            };
            mesh.RecalculateNormals();mesh.RecalculateBounds();
       return mesh;
 }

 public static readonly Quaternion QubitFaceRotation = Quaternion.Euler(180f, 90f, 90f);
            public static AssetBundle qubitBundle;
   public static GameObject qubitPrefab;
public static Transform qubitFront;
       public static Canvas qubitFrontCanvas, qubitBackCanvas;
  public static UnityEngine.UI.Text qubitClock, qubitDate, qubitStats, qubitSearch;
                  public static float qubitStatsTime;

        public static readonly List<(string Action, RectTransform Row)> qubitJoystickTargets = new List<(string, RectTransform)>();
        public static RectTransform qubitJoystickMarker;
        public static bool joystickSelectHeld;
 public static bool qubitLoadFailed;
            public static bool UseQubit => !qubitLoadFailed && !clickGUI && CurrentPrompt == null;
        public const int QubitMaxRows = 8;
        public static int QubitRowCount => Mathf.Clamp(_pageSize, 4, QubitMaxRows);
        public static int QubitPageCapacity => QubitRowCount - (isSearching ? 1 : 0);
                  public static void BindControl(string path, string action, bool visible)
    {
 var rect = (RectTransform)qubitFront.Find(path);
            rect.gameObject.SetActive(visible);
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
            public static GameObject CreateMenu()

   {
qubitFront = null;
        qubitJoystickTargets.Clear();
        qubitJoystickMarker = null;
       qubitClock = qubitDate = qubitStats = qubitSearch = null;
  if (!UseQubit) return CreateLegacyMenu();
                  try
    {
 if (qubitPrefab == null)
            {
   using (Stream stream = typeof(Core).Assembly.GetManifestResourceStream($"{PluginInfo.ClientResourcePath}.qubitmenu"))
{
  using (var bytes = new MemoryStream())
                  {
    stream.CopyTo(bytes);
 qubitBundle = AssetBundle.LoadFromMemory(bytes.ToArray());
            }
   }
       qubitPrefab = qubitBundle.LoadAsset<GameObject>("QubitMenu");
                  }
    menu = new GameObject("Qubit Menu");
 menu.transform.localScale = new Vector3(.1f, .3f, .3825f);
            menuBackground = new GameObject("Menu Background Anchor");
   menuBackground.transform.SetParent(menu.transform, false);
menuBackground.transform.localPosition = new Vector3(.5f, 0, 0);
       var art = UnityEngine.Object.Instantiate(qubitPrefab, menu.transform, false);
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
  qubitFrontCanvas = qubitFront.GetComponent<Canvas>();
                  qubitBackCanvas = art.transform.Find("Back").GetComponent<Canvas>();
    foreach (Graphic graphic in art.GetComponentsInChildren<Graphic>(true))
 graphic.material = null;
       Fonts.Apply(art);
            canvasObj = qubitFront.gameObject;
   title = null; fpsCount = null; keyboardInputObject = null;
qubitClock = qubitFront.Find("Clock").GetComponent<UnityEngine.UI.Text>();
       qubitDate = qubitFront.Find("Date").GetComponent<UnityEngine.UI.Text>();

  qubitStats = qubitFront.Find("Stats").GetComponent<UnityEngine.UI.Text>();
                  qubitClock.rectTransform.anchorMin = qubitClock.rectTransform.anchorMax = new Vector2(0, 1);
    qubitClock.rectTransform.pivot = new Vector2(0, 1);
 qubitClock.rectTransform.anchoredPosition = new Vector2(30, -15);
            qubitClock.rectTransform.sizeDelta = new Vector2(180, 33);
   qubitClock.fontSize = 28;
qubitClock.alignment = TextAnchor.UpperLeft;
       qubitDate.rectTransform.anchorMin = qubitDate.rectTransform.anchorMax = new Vector2(0, 1);
  qubitDate.rectTransform.pivot = new Vector2(0, 1);
                  qubitDate.rectTransform.anchoredPosition = new Vector2(56, -46);
    qubitDate.rectTransform.sizeDelta = new Vector2(135, 18);
 qubitDate.fontSize = 14;
            qubitDate.alignment = TextAnchor.UpperLeft;
   qubitClock.color = qubitDate.color = new Color32(169, 119, 251, 255);
BindControl("Home", "Global Return", !disableReturnButton);
       BindControl("Disconnect", "Disconnect", !disableDisconnectButton);
  BindControl("OuterTab/OuterExit", "Disconnect", !disableDisconnectButton);

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
       var label = row.Find("Label").GetComponent<UnityEngine.UI.Text>();
  label.resizeTextForBestFit = true; label.resizeTextMinSize = 12; label.resizeTextMaxSize = 22;
                  label.horizontalOverflow = HorizontalWrapMode.Wrap;

    if (isSearching && i == 0) { qubitSearch = label; continue; }
 int index = i - (isSearching ? 1 : 0);
            if (index >= visible.Length) { row.gameObject.SetActive(false); continue; }
   var info = visible[index];
label.text = info.overlapText ?? info.buttonText;
       label.color = QubitDetails.ButtonTextColor;
       row.GetComponent<Image>().color = info.enabled ? QubitDetails.EnabledButtonColor : Color.black;
   if (!info.label)
{
       qubitJoystickTargets.Add((info.buttonText, row));
       if (info.incremental && incrementalButtons)
  {
                  AddHitbox(row, info.buttonText, -1, 0, 70);
    AddHitbox(row, info.buttonText, 1, 326, 70);
    AddHitbox(row, info.buttonText, 0, 70, 256, true);

 label.text = "−   " + label.text + "   +";
            }
   else AddHitbox(row, info.buttonText);
}
       }
  qubitFront.Find("Page").GetComponent<UnityEngine.UI.Text>().text = pageNumber.ToString();
                  QubitDetails.Attach(art.transform, qubitFront);
                  if (joystickMenuSearching && !disableSearchButton)
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
 if (menu != null) { menu.SetActive(false); UnityEngine.Object.Destroy(menu); menu = null; }

            qubitLoadFailed = true;
   qubitFront = null;
return CreateLegacyMenu();
       }
  }
    }
}
