using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Qubit.Menu;
using UnityEngine;
using UnityEngine.UI;

namespace Qubit.Classes.Menu
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
            => Resolve(selected);

 public static IReadOnlyList<string> Options => options;
 public static string Selected => selected;
 public static void Select(string name)
 {
     if (!options.Contains(name)) return;
     setting.value = name;
     Restore();
     Preferences.SaveButton(setting);
 }
 public static Font Resolve(string name)
            {
  if(cache.TryGetValue(name,out var found))return found;
       Font font;
 if(name == "System Default")font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            else if(name.StartsWith("Windows: ",StringComparison.Ordinal))font = Font.CreateDynamicFontFromOSFont(name.Substring(9),28);
  else
       {
 if(bundle == null)
            {
  using(Stream stream = typeof(QubitFonts).Assembly.GetManifestResourceStream("Qubit.Resources.qubitfonts"))
       using(var memory = new MemoryStream())
 {
            stream.CopyTo(memory);
  bundle = AssetBundle.LoadFromMemory(memory.ToArray());
       }
 }
            font = bundle.LoadAsset<Font>(name);
  }
       cache.Add(name,font);
 return font;
            }
    }
}
