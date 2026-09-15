# Qubit integration

Release DLL: `bin/Release/netstandard2.1/Seralyth Menu.dll`.
The corrected Windows AssetBundle is embedded as `Seralyth.Resources.qubitmenu`; no external bundle file is needed at runtime.

## Changes

- `Menu/Main.cs` creates Qubit for the normal menu. Six rows use the existing button registry, category actions, favorites, enabled list, search/aliases, legal-build filtering, paging and incremental buttons.
- Search reserves its first row for typed text and displays five results per page. Page counts use the same filtered list as the display. Joystick selection is bounded to the visible items.
- Home uses Global Return, exit icons use Disconnect, the magnifier uses Search, and arrows use the existing page actions. Actions still pass through Seralyth's ButtonCollider/Toggle code.
- Clock/date/latency toggles were added to Menu Settings and are saved by the existing preferences system. The Qubit FPS display is removed. Live purple time and date occupy the upper-left header, matching the reference; latency remains separately toggleable.
- The prefab is rotated inside the original menu root. Front local -Z maps to Seralyth +X; artwork right maps to -Y; artwork up maps to +Z. The original hand/desktop positioning remains in use. All scale factors are positive and compensate the old root's unequal dimensions.
- Only the front has click hitboxes. `Classes/Menu/ButtonCollider.cs` rejects Qubit interactions from behind and from old dropped menu instances.
- The runtime uses the host game's default UI material for both-eye rendering, selecting the visible front/back Canvas every frame. This avoids depending on stereo variants from the non-XR authoring project's custom material. The standalone Unity shader was also updated with explicit stereo variants and initialized vertex output, and the new bundle was embedded.
- Three pre-existing compile failures in `Mods/Fun.cs` were repaired by replacing the removed `SetHoverAllowed(true)` calls with the installed game's `isHoverAllowed` property.

ClickGUI and modal prompts retain their existing renderer. Legacy theme/geometry controls do not reskin the Qubit artwork. Asset-load failure logs an error and falls back to the original menu.

## Verification

- Release build: zero errors; three existing unused-field warnings in AIManager.
- Embedded bundle SHA-256 matched `Resources/qubitmenu` after compilation.
- Unity transform checks passed for hand front/back axes, desktop upright/unmirrored text, right-hand pose, positive scale, undistorted dimensions, and front/back depth.
- Actual two-eye headset rendering and gameplay input still require an in-game check.

Original source copies are in `QubitBackup`. Build used `dotnet build SeralythMenu.csproj -c Release -p:CI=TRUE`; this deliberately did not auto-copy the DLL into the running game's plugins directory. Replace your existing Seralyth DLL with the release DLL, then restart the game so the old shader/bundle is no longer cached. Keep only one installed Seralyth DLL.
Clock/date visibility fix: old saved Qubit Clock and Qubit Date values were false. They are now opt-out settings named Hide Qubit Clock and Hide Qubit Date, defaulting to false, so the header is visible after preference restoration.

Keyboard integration: Resources/qubitkeyboard is embedded. Settings.SpawnKeyboard creates QubitKeyboardView for VR text entry, with the existing two finger pointers. All 55 keys call the existing typing handler. Enter submits search/prompts; Escape closes text entry. Shift/Caps labels and pressed colors use UI Images, not the old mesh ColorChanger. The keyboard uses the host UI material and rejects touches from behind. A kinematic Rigidbody enables trigger callbacks. The menu anchor uses the upright desktop basis above the keyboard. Physical desktop keyboard input is retained. Release build and embedded bundle hashes passed; headset typing needs an in-game check.

Fonts: Settings > Menu Settings > Qubit Font (displayed as Font: name) cycles with the minus/plus key areas. Minecraft is the default, followed by Starborn, KOMIKAX_, System Default, and Windows fonts reported by Unity. The saved value is the font name, not an index, and an unavailable saved name falls back to Minecraft. The selection applies to menu and keyboard Text components immediately and on creation. The three custom fonts are embedded as Seralyth.Resources.qubitfonts; they do not need to be installed in Windows. System fonts come from the local PC. Dynamic-font glyph checks, Release build, and embedded bundle hash passed. Headset appearance still needs an in-game check.
