# Platform companion — implemented source, validation pending

`portable_switch.py` is a Python 3.10+ standard-library companion with a common Backend contract (`preview`, reviewed `apply`, `restore`), platform selection, CLI, optional Tkinter GUI, journal storage and embedded simulations. It is source, not a Linux/macOS executable or a universally verified cutoff tool.

| Platform | Implemented behavior | Enforcement and validation |
|---|---|---|
| Windows | Companion reports platform and launches bundled native GUI | Native Windows driver audit, hardware microphone mute and camera disable; live cutoff untested |
| Linux | JSON pactl capture-source inventory; reviewed source mute; exact recorded mute restore; /dev/video inventory only | Session mute only. Cameras unsupported. Code simulated on Windows, no Linux runtime/hardware validation |
| macOS | system_profiler audio/camera inventory; app-permission guidance | Global apply/restore unavailable; no mutations. No macOS runtime validation |

Linux supports a running PulseAudio server, including a PipeWire PulseAudio compatibility server when `pactl` with JSON source support is available. Pure wpctl-only or direct ALSA configurations are reported unsupported. The companion does not install dependencies or change audio services. Run it as the existing desktop user, not root: an administrator/root account may connect to a different session/server.

## Usage

```text
python3 portable_switch.py                         # read-only JSON capability report
python3 portable_switch.py --gui                   # readable report and reviewed action
python3 portable_switch.py --self-test             # simulated controls; no device mutations
```

Linux-only CLI actions, after reading the preview and accepting session-only limits:

```text
python3 portable_switch.py --apply-reviewed-session-mute
python3 portable_switch.py --restore-reviewed
```

Apply deliberately returns exit 4 because enforcement is partial: session-source mute can read back correctly while cameras remain available and direct hardware/other-user/native graph paths bypass mute. Restore returns exit 4 when entries remain pending; errors return 1. CLI action flags are explicit authorization; GUI presents a warning. Preview does not record audio or open a camera. Apply refuses if sources changed after the reviewed GUI snapshot or recovery already exists.

Linux source selection excludes monitor-of-sink loopback sources and names ending `.monitor`. It never changes sinks/speaker outputs. Other capture sources may be virtual inputs; descriptions and driver metadata are visible. Source names and selected device properties are compared on mutation/restore. This is not a cryptographic physical-device identity; sparse metadata or name reuse can defeat matching. New devices are not continuously blocked. Existing/new streams require actual recording tests; a mute flag is only configuration readback. App/server behavior can unmute sources, and server restart persistence is not guaranteed.

Linux recovery: `$XDG_STATE_HOME/privacy-switch/linux-session-recovery.json`, otherwise `~/.local/state/privacy-switch/`. Originals and pending intent are saved before mutation using an fsynced temporary file, atomic replacement and directory sync. A Unix flock serializes companion actions and releases on crash. Failed or missing/changed-source restore retains pending entries. Completed recovery becomes `audit-<timestamp>.json`. Original already-muted sources are never unmuted. No Windows ProgramData or macOS recovery is written by this backend. Directory/file modes request 0700/0600; existing directory ownership/ACL/symlink hardening needs review before treating this as a hardened privileged tool. The companion is deliberately a user-session tool.

macOS guidance: System Settings → Privacy & Security → Microphone / Camera. Per-app permissions differ from a global physical cutoff. The app does not edit the TCC database, reset permissions, unload drivers, bypass SIP, or mutate firmware. `system_profiler` availability/data shape and Tkinter availability require testing on target systems.

Python's Windows backend is a launcher adapter, not a duplicated control backend. The native C# app keeps its own recovery model and meaningful Windows audit. All packages need rebuilding/verification on their target systems before claiming compatibility. No same-binary claim is made.

## Validation and next input

The embedded simulations passed on Windows: session mute/restore, preservation of already-muted sources, monitor exclusion, preview mismatch refusal, existing journal refusal, missing-source restore retention, and macOS/Windows companion mutation refusal. Unix flock, actual pactl commands, system_profiler, GUI behavior and hardware cutoff are unvalidated on the respective operating systems.

Actual platform validation needs the first target OS/distribution/version, audio stack (Linux), processor type (macOS), microphone/camera models and a test machine where an intentional voice/video interruption is acceptable. These details have not been supplied; Windows live correction remains independently deliverable.

References: [PulseAudio official CLI documentation](https://wiki.freedesktop.org/www/Software/PulseAudio/Documentation/User/CLI/), [PulseAudio pactl manual](https://www.freedesktop.org/software/pulseaudio/pactl.html), [WirePlumber tools](https://pipewire.pages.freedesktop.org/wireplumber/tools/wpctl.html), [Apple privacy permissions](https://support.apple.com/en-gb/guide/mac-help/mchl211c911f/27/mac/27), [Apple system information](https://support.apple.com/guide/system-information/system-information-syspr35536/mac).

