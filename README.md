# Microphone & Camera Switch

## Functions

- **Mute microphones / disable cameras:** review targets, confirm, and approve administrator permission. Uses driver-reported hardware microphone mute and disables Camera-class devices. Voice/video input may stop; live cutoff must be tested. A successful device-node status alone is not proof of capture cutoff.
- **Restore microphones / cameras:** restores saved original microphone mute and camera enabled states. Microphones already muted stay muted; cameras already disabled stay disabled.
- **Refresh audit:** shows current Windows device status, action results, errors and pending recovery.

Only microphones and cameras are controlled. There are no location/sensor, Windows privacy registry, telemetry, Defender, firewall, hardening, Wi-Fi, networking or offline-mode controls. The broader future idea is outside this app.

## Recovery and verification

The main window runs without elevation. Only a requested action launches this same executable as an administrator helper. Original states and mutation intent are saved before changes. Microphone hardware support and mute readback are checked using Core Audio. Software-only mute is reported as unsupported. Camera Configuration Manager status is checked after disable/enable, using persistent-disable flags. Neither check is proof of effective recording cutoff. Failures remain visible and pending recovery is retained. A lock prevents concurrent action helpers.

Recovery is stored in `%ProgramData%\PrivacySwitch\recovery.xml`, with atomic replacement and a previous `.bak`. Completed restoration creates an audit XML. Relaunch after closing, crashing or rebooting to restore. Do not delete recovery files while restoration is pending. Corrupt or out-of-scope recovery is preserved and blocked for manual review, including journals from the earlier broader prototype. Microphone/camera-only device journals from the earlier version remain restorable; original journal files are never overwritten by Apply when recovery exists.

## Limits

Microphones are enumerated using Core Audio capture direction, not solely a device-node ID prefix. Active endpoints must report hardware mute capability (bit 2). The app uses the documented IAudioEndpointVolume mute control and saves its original value. It does not disable microphone devnodes in new actions, because that method did not stop the operator's live voice input. Shared audio controllers and speaker endpoints stay untouched. This is hardware-supported mute as reported by the driver, not physical disconnection; other applications or administrators can unmute it. Drivers can misreport support. Microphone mute persistence across reboot is driver-dependent.

Camera-class devices remain supported; legacy Image-class devices are listed for review because that class can include scanners. Device status and microphone mute readback are configuration checks, not proof of stopped capture. No protected/successful cutoff state is claimed. Confirm with a live voice/video test after applying; if fresh input continues, regard restriction as failed and restore. Newly attached devices are not continuously blocked. Unsupported endpoints, missing devices and driver failures can cause partial results.
Original-state restoration assumes no competing administrator changes. Windows provides no transaction between device state and the journal: an administrator changing a device between saved intent and mutation cannot always be distinguished. Avoid concurrent Device Manager changes.

## Validation and rebuild

```powershell
& C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /target:winexe /out:PrivacySwitch.exe /win32icon:PrivacySwitch.ico /reference:System.Windows.Forms.dll /reference:System.Drawing.dll PrivacySwitch.cs CoreAudio.cs Window.cs CopilotKey.cs CopilotApp.cs
$p = Start-Process .\PrivacySwitch.exe --test -Wait -PassThru
$p.ExitCode # 0 = passed; simulated operations only, files under work/
```

Official references: [device disable](https://learn.microsoft.com/en-us/windows/win32/api/cfgmgr32/nf-cfgmgr32-cm_disable_devnode), [device enable](https://learn.microsoft.com/en-us/windows/win32/api/cfgmgr32/nf-cfgmgr32-cm_enable_devnode), [device status](https://learn.microsoft.com/en-us/windows/win32/api/cfgmgr32/nf-cfgmgr32-cm_get_devnode_status).

## Optional platform foundation

The separate PlatformSources ZIP contains portable_switch.py and PLATFORMS.md. Linux capture-source session mute/recovery is implemented as unvalidated source; cameras remain unsupported. macOS provides unvalidated detection/permission guidance only. This Windows executable does not run universally. HANDOVER.md and ROADMAP.md distinguish completed work, required live/platform validation and future-only telemetry/network/AI profile concepts.

## Windows GUI reminder cards (r2-ui3)

## Help-strip correction (r2-ui3)

Hover/focus help is a short hint in a fixed 340 × 68 panel inside the app, clamped to its client bounds. It cannot become a separate desktop popup. Full click/Enter help opens a fixed, owned dialog (nominal 560 × 390 client area, constrained for the current working area) with word-wrapped, vertically scrollable read-only details. Escape/Close dismiss it. Hints are hidden on pointer/focus leave, before/after full help, and on app deactivate, resize/minimize and closing. They are disposed with the form; the owned dialog is disposed when the owner is disposed. Future cards remain help-only and clearly labelled Planned / future version.

Run the layout/lifecycle checks after building:

```powershell
$p = Start-Process .\PrivacySwitch.exe --test-ui -WindowStyle Hidden -Wait -PassThru
$p.ExitCode # expected 0; simulated Windows UI and storage only
```

## Planned secure erase reminder

The GUI now includes a seventh Planned / future version card, **Secure erase / disposal**. It opens informational owned help only through the same help handler as the other roadmap cards. No disk access, wipe/sanitize/format command or destructive action is present. Its help says irreversible, outside reversible profiles, with future backup/ownership/scope review and explicit confirmation prerequisites. Infinite logical overwrites are not a valid universal sanitization strategy, especially with SSD wear-leveling. The roadmap references NIST SP 800-88 Rev. 2 and vendor-supported capabilities/verification. Active system-drive workflows, cloud copies and backups require separate consideration.

The simulated render was reinspected with all seven cards visible. The UI check confirms the secure-erase help exists and explicitly states INFORMATION ONLY and irreversible. Source review confirms that all roadmap clicks only invoke owned help. Device regressions still pass; no real device settings or disk contents were changed by this UI work beyond ordinary project artifact files.

## Additional current profile: Block Copilot key (r2-ui3)

The user authorized a separate reversible session shortcut-control profile based on their supplied script. This is an explicit addition to the earlier microphone/camera-only scope; no broader AI disablement is implemented. **Block Copilot key** suppresses exact Left Shift + Left Windows + F23 while the app's hook thread runs. Extra modifiers and other launch paths remain available. **Stop key blocking** removes only this profile and never exits the app or undoes microphone/camera recovery. Closing the app removes the hook. No startup/shortcut edits, key-history storage, logging or transmission occur. See COPILOT-KEY.md.

Real hooks stayed inactive during development. Simulated --test-key checks passed for modifiers/order/repeat/paired release, missed-state recovery, extra-shortcut exclusions and unhook cleanup; device/UI regressions passed. The simulated preview was inspected with the new session-only profile showing Off. Real keyboard interception remains untested; installed status is OS-reported and not proof against protected desktops, alternative key mappings or silent timeout removal. Native full-length tooltips remain absent; all question-mark help uses the bounded in-app hints and owned full-details dialog.

Earlier scope descriptions in this document refer to the previous microphone/camera-only stage. The current executable additionally supports this explicitly requested transient key chord block, and still makes no telemetry, network, location, Defender/Firewall, firmware or secure-erase changes.

Ctrl+Alt+Q support has been removed at the operator's request. That shortcut passes through normally. The key-block profile is controlled only by its GUI toggle and app/session lifetime; it cannot quit the app through a keyboard escape chord.

## Reviewed force-close addition

Enabling the key profile now offers **Block key + close Copilot app**. A confirmation lists identified installed Microsoft.Copilot app processes in the current session before force-closing them. Unsaved app state may be lost and cannot be restored. Stop key blocking removes only the hook; it does not relaunch the closed app or alter microphone/camera recovery.

CopilotApp.cs queries the exact package family Microsoft.Copilot_8wekyb3d8bbwe and restricts termination to main process names Copilot or Microsoft.Copilot. It excludes shared browser/WebView/RuntimeBroker/Explorer processes. Before termination, the process handle's package identity and creation time must still match the reviewed snapshot, preventing PID-reuse targeting. It verifies exit or reports failure/access denied. Uninspectable processes and unknown app variants remain outside coverage. No app uninstallation, policy edit, continuous relaunch blocker or startup changes are installed.

This closes matching running standalone app processes once. It does NOT disable all Copilot integrations, Office/browser/web access, AI or telemetry. The app can be launched again through another route. The local Get-AppxPackage query was denied, so the implementation uses process-handle package identity instead. Development performed no real process termination or live keyboard hook installation. Compilation and --test-app identity/exclusion checks passed alongside key, device and UI regressions; actual termination remains untested and requires a deliberate reviewed GUI action.

## Detector/subprocess correction

The earlier main-name-only force-close detector has been superseded. COPILOT-DETECTION.md describes native Toolhelp enumeration, verified Microsoft.Copilot package-image paths, scoped WebView2 descendants, ancestry/creation-time checks and follow-up status. Shared browser/Office hosts remain excluded. The new Inspect Copilot button opens read-only copyable diagnostics. If no verified target is found or detection fails, key blocking can still run and explicitly reports that no process was closed. Confirmation/results are bounded owned dialogs. All detection/key/device/UI simulations passed; the operator's actual desktop Copilot process was not visible in this development environment, so real termination remains unvalidated. No real process was terminated or live hook installed during this correction.

## Correction for the reported mscopilot installation

A simulated replay of the supplied process layout selects all 11 Copilot processes and excludes the unrelated Client.CBS and Client.WebExperience WebView trees. Device, keyboard, detector and UI simulation checks pass. Actual desktop termination has not been exercised in the development environment. Close the previous PrivacySwitch instance and run the rebuilt executable before testing; an already running instance retains the old detector.

## Application icon
PrivacySwitch.ico is embedded by Build.ps1 and can also be selected as a Windows folder/shortcut icon. PrivacySwitch-icon.svg is editable design source; PrivacySwitch-icon.png is the preview. See ICON.md. All four simulation suites passed after embedding the icon.



For the current publication status and privacy cleanup, read PUBLIC-SNAPSHOT.md. This directory is the repository root; upload its contents once. Put executable/ZIP downloads in GitHub Releases. No license has been selected.

