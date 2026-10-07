# Copilot key profile — session shortcut control

## Behavior

Click **Block Copilot key** to install a session keyboard hook. Click **Stop key blocking** or close the app to stop it. Stopping affects only this profile; it never exits the whole app or restores pending microphone/camera changes. No startup installation, autostart, keyboard remapping registry edit or separate background executable is installed. The hook thread belongs to this running app.

Only the exact **Left Shift + Left Windows + F23** chord is suppressed. Extra Ctrl, Alt, right Shift or right Windows modifiers are excluded to avoid blocking other shortcuts. Bare F23, right-side substitute chords and unrelated keys pass through. The original script also matched the chord with extra modifiers; the app deliberately narrows it to the exact advertised chord. Other Copilot launch methods, app features, AI, telemetry and network access remain available.

While active, the first F23 down makes one block/pass decision for that press; repeats and its eventual up use that decision regardless of modifier release order. An F23 already held when installation starts is passed rather than creating an unmatched suppressed press. Stopping the session mid-press removes the hook immediately; subsequent releases pass normally, so no universal key-pair guarantee is made across uninstallation. Modifier key events themselves pass through and can retain normal Windows/Start-menu behavior.

## Implementation and privacy

`CopilotKey.cs` defines the pure `CopilotChord` state machine, `HookLease` cleanup wrapper, dedicated-thread `CopilotKeySession`, and embedded simulations. A `WH_KEYBOARD_LL` callback reads the current virtual key only to match the specified chord and immediately forwards unrelated events. Left/right modifiers are tracked independently. A timer on the hook thread polls modifier states outside the callback to recover missed modifier transitions; initialization also checks already-held keys. The callback does not query current asynchronous state because Windows has not yet updated the current event's state there.

The dedicated message-pump thread keeps the hook independent of GUI waits for the administrator device helper. The callback does no file/network IO or content logging. Stored state is only modifier booleans, F23 press/latch state and session lifecycle/error state. There is no typed-content history, persistent key log, transmission or journal of keyboard input. Keyboard profile Off is restoration by removing the transient hook; device originals stay in their separate existing recovery journal.

The UI distinguishes Off, OS-reported installation and installation/shutdown failure. Installation is not proof that every later shortcut is intercepted: Windows can silently remove a timed-out low-level hook. Protected desktops, different input mechanisms, different dedicated-key mappings and security/integrity boundaries are not guaranteed. No admin request is made for this profile. It may not block a Copilot key whose firmware/driver generates a different chord.

On callback error, the session requests only its own thread's shutdown, unhooks in cleanup and clears key state. Toggle Off/app closing disposes it. The native return from unhook is checked; timeout/removal errors are reported rather than silently treated as successful restoration. Process termination also ends this process's session; this is not persistent enforcement.

## Validation

Compilation passed. `--test-key` simulations passed for exact chord, down order, both modifier release orders, repeats, bare F23 and already-held unrelated repeat, independently held left/right Ctrl/Alt, extra-modifier exclusion, ordinary keys, initial F23 hold, missed modifier synchronization, clear/reset, install failure, idempotent cleanup and retry after an unhook failure. These tests never call SetWindowsHookEx or install a real hook. Device and UI regressions also passed; the preview shows key blocking Off.

Real keyboard interception, protected/elevated application behavior, OS timeout behavior remain untested. A deliberate user click in the live app is required to validate the dedicated key on this machine. No real keyboard hook or device mutation was activated during development.

Build now includes all four C# files:

```powershell
& C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /target:winexe /out:PrivacySwitch.exe /reference:System.Windows.Forms.dll /reference:System.Drawing.dll PrivacySwitch.cs CoreAudio.cs Window.cs CopilotKey.cs CopilotApp.cs
$p = Start-Process .\PrivacySwitch.exe --test-key -Wait -PassThru
$p.ExitCode # 0 = passed, simulated state/cleanup only
```

Official references: [SetWindowsHookEx](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowshookexw), [LowLevelKeyboardProc, message loop/state timing and timeout limits](https://learn.microsoft.com/en-us/windows/win32/winmsg/lowlevelkeyboardproc), [UnhookWindowsHookEx](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-unhookwindowshookex).

Ctrl+Alt+Q support has been removed at the operator's request. That shortcut passes through normally. The key-block profile is controlled only by its GUI toggle and app/session lifetime; it cannot quit the app through a keyboard escape chord.

## Reviewed force-close addition

Enabling the key profile now offers **Block key + close Copilot app**. A confirmation lists identified installed Microsoft.Copilot app processes in the current session before force-closing them. Unsaved app state may be lost and cannot be restored. Stop key blocking removes only the hook; it does not relaunch the closed app or alter microphone/camera recovery.

CopilotApp.cs queries the exact package family Microsoft.Copilot_8wekyb3d8bbwe and restricts termination to main process names Copilot or Microsoft.Copilot. It excludes shared browser/WebView/RuntimeBroker/Explorer processes. Before termination, the process handle's package identity and creation time must still match the reviewed snapshot, preventing PID-reuse targeting. It verifies exit or reports failure/access denied. Uninspectable processes and unknown app variants remain outside coverage. No app uninstallation, policy edit, continuous relaunch blocker or startup changes are installed.

This closes matching running standalone app processes once. It does NOT disable all Copilot integrations, Office/browser/web access, AI or telemetry. The app can be launched again through another route. The local Get-AppxPackage query was denied, so the implementation uses process-handle package identity instead. Development performed no real process termination or live keyboard hook installation. Compilation and --test-app identity/exclusion checks passed alongside key, device and UI regressions; actual termination remains untested and requires a deliberate reviewed GUI action.

## Detector/subprocess correction

The earlier main-name-only force-close detector has been superseded. COPILOT-DETECTION.md describes native Toolhelp enumeration, verified Microsoft.Copilot package-image paths, scoped WebView2 descendants, ancestry/creation-time checks and follow-up status. Shared browser/Office hosts remain excluded. The new Inspect Copilot button opens read-only copyable diagnostics. If no verified target is found or detection fails, key blocking can still run and explicitly reports that no process was closed. Confirmation/results are bounded owned dialogs. All detection/key/device/UI simulations passed; the operator's actual desktop Copilot process was not visible in this development environment, so real termination remains unvalidated. No real process was terminated or live hook installed during this correction.

## Correction for the reported mscopilot installation

A simulated replay of the supplied process layout selects all 11 Copilot processes and excludes the unrelated Client.CBS and Client.WebExperience WebView trees. Device, keyboard, detector and UI simulation checks pass. Actual desktop termination has not been exercised in the development environment. Close the previous PrivacySwitch instance and run the rebuilt executable before testing; an already running instance retains the old detector.

