# Microphone & Camera Switch — current portable handover

Snapshot: **mic-camera-hardware-mute-2026-10-07-r2-ui3**, 7 October 2026, UTC. This identifier is documentation metadata, not an embedded assembly version or Git tag.

## Current status and live-test correction

The agent made no real device mutations during development or this fix. The user did perform real apply/restore actions with the earlier app. Revised microphone mute/camera cutoff has not been live-tested. No protected/effective-cutoff success state is claimed from driver readback.

## Final agreed scope and history

The product remains a small microphone/camera-only switch, original-state restoration, audit, administrator action helper and recovery. Microphones use hardware-supported mute; cameras use device disable. Hardware mute is a driver-reported control, not a claim that microphone hardware is physically disconnected or its driver unloaded. This is the current feasible correction; a universal hardware device disable has not been established.

Initially broad privacy policies, location sensors, telemetry reduction and Copilot implications were explored. A reviewed Defender/Firewall hardening profile was also explored. Security restore was first separated, then explicitly rejected by the operator. The user ultimately said microphone and camera only, nothing else; all broader controls and their helper were removed. The delivered app has no telemetry, location/sensor, app privacy policy, Defender, Firewall, Wi-Fi/networking or offline-mode changes. A network-disable/hardening mode was only a future idea. No earlier prototype executables/helpers are included.

## Package inventory

- `PrivacySwitch.exe`: unsigned portable Windows Forms executable.
- `PrivacySwitch.cs`: native device, journal, compatibility and simulation test code.
- `Window.cs`: complete current GUI, in-app hover/focus hints and owned click help, roadmap cards, elevated dispatcher and safe simulated renderer.
- `GUI-preview.png`: actual rendered GUI with simulated device statuses.
- `GUI-QA.md`: visual inspection results and limitations.
- `CoreAudio.cs`: complete documented Core Audio COM interop and capture hardware mute logic.
- `CopilotKey.cs`: session chord state machine, dedicated native hook pump, cleanup and simulations.
- `CopilotApp.cs`: native process/package-path/descendant detector, reviewed force-close and exclusion checks.
- `COPILOT-DETECTION.md`: failed-detector investigation, revised ancestry checks and read-only report usage.
- `COPILOT-KEY.md`: exact scope, supplied-script interpretation, lifecycle, tests and limits.
- `README.md`: usage/build instructions and limits.
- `LIVE-TEST.md`: regression diagnosis and live verification procedure.
- `HANDOVER.md`: this project context and architecture.
- `portable_switch.py`: common Python backend contract, OS selection, Linux session mute/recovery, macOS detection/guidance and Windows launcher; embedded simulations.
- `PLATFORMS.md`: companion usage, boundaries and platform validation requirements.
- `ROADMAP.md`: planned future profile concept; no new controls.
- `MANIFEST.sha256`: hashes for the sixteen files above; excludes itself and ZIP.

No installer, SDK project, NuGet packages, external runtime download, service, scheduled task or autostart is used. No Git history, old prototype files, real-machine recovery files, captured audio or credentials are included. Self-tests are embedded in the current source/executable.

## Architecture and detection

`Device`, `Entry`, `Journal` are data models. `Native` uses SetupAPI and Configuration Manager for inventory and camera disable/enable. `CoreAudio` uses `IMMDeviceEnumerator`, `IMMDeviceCollection`, `IMMDevice` and `IAudioEndpointVolume` for capture-only enumeration, hardware support and mute. `Engine` owns persistence and apply/restore. `Window` shows the toggle and readable audit. `Compatibility` reports Windows family/build/edition and process bitness. `Tests` replaces native-facing delegates with simulations.

The main window normally runs without elevation. After review, it starts the same executable using `runas` and `--apply` or `--restore`. Windows requests administrator permission. The GUI waits synchronously and can be unresponsive during the helper action. The helper checks administrator membership and uses global mutex `Global\PrivacySwitchDeviceAction` with `WaitOne(0)` to reject concurrent app helpers. Other admin tools remain outside this lock. An abandoned mutex or access error can fail the helper; automatic repair is not implemented.

Exit codes: 0 no indicated incomplete entry; 1 exception; 2 not administrator; 3 unknown action; 4 partial application or pending restore. Driver readback success never means proven live cutoff.

## Target selection and control

Camera targets must be present Camera-class devices with Configuration Manager problem code 0. Already disabled/problematic cameras are omitted and never enabled by restore. Legacy Image-class devices are review-only because that class also contains scanners.

Microphone actions use `EnumAudioEndpoints(eCapture, DEVICE_STATEMASK_ALL)` rather than just matching a PnP prefix. Only ACTIVE endpoints are considered. `QueryHardwareSupport` must include mute bit 2. Software-only mute is rejected because it may not affect exclusive-mode capture. Unsupported capture devices get an explicit journal result and partial helper status; no mute write is attempted. Current capture labels show state, hardware mask and current mute readback. Shared audio controllers and render/speaker endpoints are never changed.

New actions do not disable microphone devnodes. Hardware mute is applied using documented `IAudioEndpointVolume.SetMute(true)` and verified with `GetMute`. Already-muted endpoints are recorded as done without pending mutation, preserving original mute. Other software can unmute; hardware capability can be misreported by a driver. Microphone persistence across reboot is driver-dependent.

Cameras use `CM_Disable_DevNode` with `CM_DISABLE_PERSIST` (8), correcting the earlier flag-0 reboot limitation. `CM_Enable_DevNode` restores. `CM_Locate_DevNode` resolves IDs and `CM_Get_DevNode_Status` verifies devnode problem codes. Camera status is labeled “devnode disabled; live camera cutoff UNTESTED.”

SetupAPI inventory uses `SetupDiGetClassDevs` flags 6 (present/all classes), `SetupDiEnumDeviceInfo`, instance ID and registry properties (class, manufacturer, friendly name/description, driver key); it releases the handle. These registry APIs only read metadata. No privacy/settings registry values are written. A read failure on an unrelated enumerated device can currently abort inventory.

## Original state, journal and recovery

Runtime files remain in `%ProgramData%\PrivacySwitch`, outside the app folder. No machine-specific state is included in the ZIP. The primary journal is `recovery.xml`; mutation intent is persisted before changes. Apply refuses to overwrite any existing active recovery.

Entries store ID/name/type/class, `OriginalMute`, `Pending`, `Done`, and readable `Result`; Journal adds UTC creation time. Type `device` records cameras that were originally enabled. Type `hardware-mute` records active capture mute originals. Unsupported microphones are informational incomplete entries with no pending mutation. Camera originals are implicitly healthy/enabled by selection. Hardware microphone originals explicitly include mute true or false.

Save serializes to `recovery.xml.tmp`, flushes via `Flush(true)`, then initial `File.Move` or subsequent `File.Replace` with `recovery.xml.bak`. The backup is the previous journal version. It is not silently auto-restored because a stale snapshot could omit changes. Filesystem replacement is not a system-wide transaction or a guarantee against every storage failure.

Apply snapshots supported targets before mutation, saves, and rechecks state immediately before each change. Pending intent is saved before mute or disable. Readback failure or mutation exceptions remain pending for restore; entries can succeed independently, so partial operation is explicit. Current camera-only microphone/camera legacy `device` journals remain readable/restorable using the older enable path; broader policy/sensor/unknown journals are blocked and preserved. Legacy audit wording is not accepted as proof of effective restriction.

Restore reverses pending entries. Hardware-mute entries restore their explicit original bit and verify it. Legacy/current device entries enable only problem-22 devices; already-problem-0 devices are accepted without change, other/absent states fail. Successful entries clear pending. Once no mutation is pending, the journal moves to `audit-<GUID>.xml`. Informational unsupported entries can therefore be archived without a device change.

## Residual risks and validation boundaries

- Driver-reported hardware mute is not a physical/firmware guarantee; admins, apps, drivers and compromised systems can undo controls. It is a stronger documented microphone-only mechanism than the failed software endpoint devnode approach, but revised actual cutoff still needs live validation.
- Active voice/video input may stop on a deliberate user action. The assistant must not silently perform that live test. Fresh speech continuing after apply means failure regardless of readback.
- Capture endpoints can include non-microphone sources. New/reconnected devices are not continuously blocked; active stream paths, exclusive mode, driver behavior and automatic app unmute need real tests.
- Camera status does not prove an existing camera stream stopped. Windows Hello cameras can be targeted if Camera-class. No camera functional test has been performed by the agent.
- Exact original-state restore assumes no concurrent admin/application changes. External mute may be overwritten when restoring an originally unmuted endpoint. Saved intent and actual mutation are not transactional; device identity reuse/driver replacement also matter.
- Journal authenticity, XML shape/null handling and ProgramData ACLs have not undergone a security audit. The directory inherits permissions; no explicit protected ACL, signing or privileged IPC validation is implemented. Legacy restore classification uses saved class/ID, not live class validation. Core Audio mute itself rechecks capture membership and hardware mute capability before writing.
- Capability/readback calls fail closed per mutation, but snapshot capability errors can abort the whole apply before writes. Read-only audit may stop on an endpoint error. Unsupported endpoints are reported, not hidden.
- Executable is unsigned; the current GUI has been rendered/visually inspected using simulated statuses; interactive pointer/focus behavior, high-DPI/accessibility, elevation cancellation, live functional cutoff and cross-platform GUI validation remain untested. Build has no embedded release version. Source is compact and would benefit from ordinary multi-file formatting.

## Build and simulation reproduction

Windows 11 is the target environment. .NET SDK was not available in the original SDK listing; the installed .NET Framework compiler is used. Extract to a trusted writable folder, then in PowerShell:

```powershell
& C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /target:winexe /out:PrivacySwitch.exe /win32icon:PrivacySwitch.ico /reference:System.Windows.Forms.dll /reference:System.Drawing.dll PrivacySwitch.cs CoreAudio.cs Window.cs CopilotKey.cs CopilotApp.cs
$p = Start-Process .\PrivacySwitch.exe --test -Wait -PassThru
$p.ExitCode # expected 0
```

Self-tests substitute Engine inventory/status/change/capture/hardware/mute delegates and relocate storage to `work/PrivacySwitch-tests-<GUID>`. They do not invoke native device mutations or change real recovery. Failure attempts to write `%TEMP%\PrivacySwitch-test-error.txt`.

Regression tests cover speaker/scanner/network/sensor exclusions; camera and capture selection; correct-only camera targeting; camera disabled originals; hardware mute apply/restore; preserved original mute; unsupported software mute; false mute readback rejection; atomic backup; crash-like pending mute recovery; failed restore retention; and out-of-scope journal rejection. Compilation and simulations passed with exit 0. Read-only actual inventory/Core Audio checks succeeded, but no effective live recording measurement was made. `LIVE-TEST.md` describes the deliberate user test for existing and newly opened capture sessions.

## Cross-platform foundation — source implemented, target validation pending

The Python companion implements an abstract Backend contract (preview, reviewed apply, restore), platform selection, common capability-report GUI/CLI and Linux versioned session-mute journal. LinuxBackend invokes JSON pactl source inventory and source mute, excludes speaker monitor sources, checks source names/properties, saves originals/pending intent with fsync/atomic replacement and Unix flock, restores originals, and retains failure state. MacBackend reads system_profiler audio/camera inventory and provides permission guidance; apply/restore explicitly refuse. WindowsBackend is a launcher adapter to the native Windows app, not a second Windows control implementation. Existing C# Engine delegates remain simulation/extension seams. Platform/driver capabilities should decide what is supported; an OS name alone cannot imply safe hardware cutoff. Results should distinguish configuration, effective access and unverified/unsupported behavior. Shared speaker controllers must remain excluded.

Linux needs distribution/session detection and platform-specific permission handling. PipeWire/WirePlumber, PulseAudio and ALSA expose different controls and direct-device paths. Source/session mute is not global physical microphone cutoff. Camera device access and existing open handles require separate handling; shared USB devices or kernel module unloading may affect unrelated functions. A backend must capture precise originals and validate existing/new capture paths on actual hardware before claiming support. [WirePlumber tools](https://pipewire.pages.freedesktop.org/wireplumber/tools/wpctl.html) and [PulseAudio command interface](https://wiki.freedesktop.org/www/Software/PulseAudio/Documentation/User/CLI/) are starting references, not implemented behavior.

macOS has per-app camera/microphone permissions and its own audio/driver capabilities. Permission control is different from reversible global hardware cutoff; blindly unloading shared drivers is not an appropriate universal backend. macOS detection/guidance source is implemented but unvalidated; no macOS binary, device mutation, broad permission reset or driver unload was implemented. [Apple privacy settings](https://support.apple.com/en-gb/guide/mac-help/mchl211c911f/27/mac/27) document its app-specific controls.

Actual cross-platform builds need a chosen first target (Linux distribution/audio stack or macOS version/Apple Intel hardware) and access to a suitable test machine. Those details have not been supplied. Further platform validation remains separate from Windows live-fix validation. See PLATFORMS.md for usage, partial enforcement and untested paths. Python companion simulations passed on Windows, including originals, loopback exclusion, preview/journal guards and missing-source recovery retention; actual pactl, Unix locking, macOS system_profiler and cross-platform GUI were not tested on their target operating systems.

## Next-version considerations, not delivered promises

Priorities to consider technically: controlled live capture tests; preview-to-helper target binding; explicit protected journal ACLs and live identity checks; versioned journal migration; bounded polling and independent per-endpoint errors; async UI; packaging/signing/version metadata; multi-file formatting; and measuring whether hardware mute is effective/persistent on each driver. These are engineering considerations, not invented user priorities. No broader telemetry/security/networking mode is included.

SHA-256 manifest uses flat relative filenames, excludes itself and ZIP, and provides integrity comparison rather than authenticated provenance. Copy the bundle outside any workspace scheduled for deletion first. Nothing was deleted by this export beyond removing superseded helper files from the prior distribution.

Official current Windows references: [IAudioEndpointVolume](https://learn.microsoft.com/en-us/windows/win32/api/endpointvolume/nn-endpointvolume-iaudioendpointvolume), [SetMute](https://learn.microsoft.com/en-us/windows/win32/api/endpointvolume/nf-endpointvolume-iaudioendpointvolume-setmute), [Core Audio states](https://learn.microsoft.com/en-us/windows/win32/coreaudio/device-state-xxx-constants), [CM_Disable_DevNode](https://learn.microsoft.com/en-us/windows/win32/api/cfgmgr32/nf-cfgmgr32-cm_disable_devnode).

## Additional future concept

ROADMAP.md records the operator's explicitly future-only profile direction: baseline regular use, microphone/camera restriction, telemetry, all-local-interface offline control and AI integration/accelerator/firmware review. None of telemetry/network/AI/firmware control is implemented. NPU presence is not evidence of telemetry; accelerator restriction cannot stop CPU/GPU AI. Firmware rollback and universal network isolation cannot be promised. Do not turn off Firewall, Defender or updates as a privacy measure. Profiles are not a sandbox without an isolation architecture.

The user's underlying future goal is deciding when optional AI features may operate/access typing, screen/images and metadata, ranging from everyday use to stricter privacy/minimal-computer profiles. Future controls should follow feature/service behavior, permissions/indexing/screen capture, cloud/data flows, optional diagnostics and a desired local-AI allowlist, not just accelerator identity. No actual keylogging/collection is asserted without evidence; local AI can be offline and ordinary software can collect data without an NPU. Universal AI removal or an old-OS experience cannot be guaranteed. This rationale is conceptual only and changes no current backend.

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


