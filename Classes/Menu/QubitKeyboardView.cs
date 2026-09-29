 using System;
            using System.IO;
   using GorillaLocomotion;
using UnityEngine;
       using UnityEngine.UI;
  using static Qubit.Menu.Main;
                  namespace Qubit.Classes.Menu

    {
 public sealed class QubitKeyboardView : MonoBehaviour
            {
   private static AssetBundle bundle;
private static GameObject prefab;
       private Canvas face;
  private Text input;
                  private GameObject placeholder;
    private Transform keys;
 private string lastText;
            private bool lastShift, lastCaps;
   private string pressed;
private float pressedUntil;
       public static GameObject Create()
  {
                  if (prefab == null)
    {

 using (Stream stream = typeof(QubitKeyboardView).Assembly.GetManifestResourceStream("Qubit.Resources.qubitkeyboard"))
            {
using (var memory = new MemoryStream())
       {
  stream.CopyTo(memory);
                  bundle = AssetBundle.LoadFromMemory(memory.ToArray());
    }
 }
   prefab = bundle.LoadAsset<GameObject>("QubitKeyboard");
       }
  var instance = Instantiate(prefab);
                  instance.name = "Qubit Keyboard";
    var body = instance.AddComponent<Rigidbody>();
 body.isKinematic = true;

            body.useGravity = false;
   instance.transform.position = GorillaTagger.Instance.bodyCollider.transform.position;
instance.transform.rotation = GorillaTagger.Instance.bodyCollider.transform.rotation;
       instance.transform.localScale *= scaleWithPlayer ? GTPlayer.Instance.scale * menuScale : menuScale;
  var view = instance.AddComponent<QubitKeyboardView>();
                  view.face = instance.transform.Find("Canvas").GetComponent<Canvas>();
    view.face.renderMode = RenderMode.WorldSpace;
 view.face.worldCamera = null;
            view.face.transform.localPosition = new Vector3(0, .1f, 1.1f);
   view.face.transform.localRotation = Quaternion.Euler(35f, 0, 0);
view.keys = view.face.transform.Find("Keys");
       view.input = view.face.transform.Find("InputBar/InputText").GetComponent<Text>();
  view.input.supportRichText = false;
                  view.placeholder = view.face.transform.Find("InputBar/Placeholder").gameObject;
    foreach (Transform child in instance.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 0;
 foreach (Graphic graphic in instance.GetComponentsInChildren<Graphic>(true)) graphic.material = null;
       QubitFonts.Apply(instance);
            KeyboardKey.keyLookupDictionary.Clear();

   foreach (Transform key in view.keys)
{
       var handler = key.gameObject.AddComponent<KeyboardKey>();
  handler.key = key.name;
                  handler.qubitKeyboard = view;
    KeyboardKey.keyLookupDictionary[key.name] = handler;
 key.gameObject.layer = 2;
            key.GetComponent<Button>().transition = Selectable.Transition.None;
   key.GetComponent<Image>().color = Color.black;
}
       menuSpawnPosition = instance.transform.Find("MenuSpawnPosition").gameObject;
  menuSpawnPosition.transform.localPosition = new Vector3(0, .68f, 1.1f);
                  menuSpawnPosition.transform.localRotation = Quaternion.identity;
    view.RefreshKeys();
 return instance;
            }
   public bool CanTouch(Collider pointer)

{
       return VRKeyboard == gameObject && inTextInput &&
  Vector3.Dot(pointer.bounds.center - face.transform.position, -face.transform.forward) > 0;
                  }
    public void Flash(string key)
 {
            pressed = key;
   pressedUntil = Time.unscaledTime + .13f;
RefreshKeys();
       }
  public void RefreshKeys()
                  {
 foreach (Transform key in keys)
            {
   bool selected = key.name == "CapsLock" && lockShift || key.name == "Shift" && shift;
bool down = key.name == pressed && Time.unscaledTime < pressedUntil;

       key.GetComponent<Image>().color = down ? new Color(.3f,.12f,.43f) : selected ? new Color(.2f,.08f,.29f) : Color.black;
  if (key.name.Length == 1)
                  {
    string value = key.name;
 const string unshifted = "1234567890-=[]\\;',./";
            const string shifted = "!@#$%^&*()_+{}|:\"<>?";
   int index = unshifted.IndexOf(value, StringComparison.Ordinal);
key.Find("Label").GetComponent<Text>().text = index >= 0 && shift ? shifted[index].ToString() :
       char.IsLetter(value[0]) && !(shift ^ lockShift) ? value.ToLowerInvariant() : value.ToUpperInvariant();
  }
                  }
    lastShift = shift;
 lastCaps = lockShift;
            }
   private void LateUpdate()
{

  face.enabled = Vector3.Dot(GorillaTagger.Instance.headCollider.transform.position - face.transform.position, -face.transform.forward) >= 0;
                  if (lastText != keyboardInput)
    {
 lastText = keyboardInput;
            input.text = lastText ?? "";
   placeholder.SetActive(string.IsNullOrEmpty(lastText));
}
       if (lastShift != shift || lastCaps != lockShift || pressed != null && Time.unscaledTime >= pressedUntil)
  {
                  if (Time.unscaledTime >= pressedUntil) pressed = null;
    RefreshKeys();
 }
            }
   private void OnDestroy()
{
       if (keys == null) return;
  foreach (Transform key in keys)

                  if (KeyboardKey.keyLookupDictionary.TryGetValue(key.name, out var registered) && registered == key.GetComponent<KeyboardKey>())
    KeyboardKey.keyLookupDictionary.Remove(key.name);
 }
            }
   }
