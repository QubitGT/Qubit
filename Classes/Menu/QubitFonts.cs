using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Seralyth.Menu;
using UnityEngine;
using UnityEngine.UI;

namespace Seralyth.Classes.Menu
{
 public static class QubitFonts
    {
  private static readonly string[] bundled = { "Minecraft", "Starborn", "KOMIKAX_" };
       private static readonly Dictionary<string,Font> cache = new Dictionary<string,Font>();
 private static List<string> options;
            private static AssetBundle bundle;
  private static ButtonInfo setting;
       private static string selected = "Minecraft";

 public static void Register()
            {
  if(options != null)return;
       options = bundled.Concat(new[] { "System Default" }).Concat(
 Font.GetOSInstalledFontNames().Where(name => !string.IsNullOrWhiteSpace(name)).Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name,StringComparer.OrdinalIgnoreCase).Select(name => "Windows: " + name)).ToList();
  setting = new ButtonInfo {
       buttonText = "Qubit Font", overlapText = "Font: Minecraft", isTogglable = false,
 incremental = true, isSetting = true, value = "Minecraft", legal = true,
            toolTip = "Changes the menu and keyboard font. Includes imported fonts and installed Windows fonts."
  };
       setting.cycleValue = forward => Cycle(forward);
 setting.method = () => Cycle(true);
            setting.onValueChanged = Restore;
  Buttons.AddButton(Buttons.GetCategory("Menu Settings"),setting,1);
       Restore();
 }

            private static void Cycle(bool forward)
  {
       int index = Math.Max(0,options.IndexOf(selected));
 setting.value = options[(index+(forward?1:options.Count-1))%options.Count];
            Restore();
  Preferences.SaveButton(setting);
       }
 private static void Restore()
            {
  string name = setting.GetValue<string>();
       selected = options.Contains(name) ? name : "Minecraft";
 setting.value = selected;
            setting.overlapText = "Font: " + selected;
  if(Main.menu != null)Apply(Main.menu);
       if(Main.VRKeyboard != null)Apply(Main.VRKeyboard);
 }

 public static void Apply(GameObject root)
            {
  Font font = Resolve();
       foreach(Text text in root.GetComponentsInChildren<Text>(true))
 {
            text.font = font;
  text.SetAllDirty();
       }
 }
 private static Font Resolve()
            {
  if(cache.TryGetValue(selected,out var found))return found;
       Font font;
 if(selected == "System Default")font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            else if(selected.StartsWith("Windows: ",StringComparison.Ordinal))font = Font.CreateDynamicFontFromOSFont(selected.Substring(9),28);
  else
       {
 if(bundle == null)
            {
  using(Stream stream = typeof(QubitFonts).Assembly.GetManifestResourceStream("Seralyth.Resources.qubitfonts"))
       using(var memory = new MemoryStream())
 {
            stream.CopyTo(memory);
  bundle = AssetBundle.LoadFromMemory(memory.ToArray());
       }
 }
            font = bundle.LoadAsset<Font>(selected);
  }
       cache.Add(selected,font);
 return font;
            }
    }
}
