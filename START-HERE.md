# Start here on a new computer

This archive is the self-contained project handover. Extract all files together into a writable folder. No previous chat, Codex installation, account, credentials, recovery files or original workspace is required.

## Current status (7 October 2026)

## Run or rebuild on Windows

The target is Windows 11 with the Windows .NET Framework 4.x runtime. Run the bundled unsigned PrivacySwitch.exe. Microphone/camera actions request administrator elevation; process termination can fail when permissions are insufficient. Driver and Copilot-version differences still require testing on the new computer.

For development, use Windows PowerShell and the .NET Framework C# compiler. Build.ps1 finds the compiler beneath the current Windows directory (64-bit first, then 32-bit), compiles all five C# files and runs all four simulation suites. It uses no NuGet packages. If the compiler is missing, install a suitable .NET Framework development environment before rebuilding. From the extracted folder:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Build.ps1
```

The process-level execution policy option applies to this invocation only. Review the script first. Build.ps1 stops on compilation or test failure. Tests simulate controls and do not install a real keyboard hook or terminate Copilot. Temporary test files may be created. The executable is replaced by the local build; regenerate release hashes after changes.

## Continue development

Read HANDOVER.md for architecture, recovery design, scope and known limitations; README.md for use; COPILOT-DETECTION.md and COPILOT-KEY.md for Copilot behavior; LIVE-TEST.md for microphone/camera verification; ROADMAP.md for future-only concepts. Documentation includes historical stages: current executable and this status take precedence over superseded descriptions.

The optional portable_switch.py needs Python 3.10+, with Tkinter only for its GUI. Linux session mute additionally needs a working user PulseAudio-compatible server and pactl JSON source support. macOS provides detection/guidance only. PLATFORMS.md describes limits and tests. These companion backends remain unvalidated on their target platforms.

## State and publishing

Project files contain no required machine-specific runtime data. Windows creates recovery state under the new computer's ProgramData\PrivacySwitch directory when device actions are used. Never copy an old computer's recovery journal onto another computer to restore its devices. Preserve such state separately if recovering that original computer.

MANIFEST.sha256 covers the payload files, excluding itself and the ZIP. Verify it before modifications with Get-FileHash if desired. This handover contains source, embedded tests, executable, screenshot and design/history documentation, but no Git history or original chat transcript; neither is needed to continue from this snapshot.

For GitHub, extract this handover into the repository root so the source is browsable; the ZIP can also be attached as a release asset. No open-source license has been selected. Add a license of your choosing if you want to grant reuse rights. Repository creation, upload and publishing are not performed by this package. There is no installer, signing certificate or CI pipeline included.

The handover also contains PrivacySwitch.ico (required by Build.ps1), editable PrivacySwitch-icon.svg, PNG preview and ICON.md with folder instructions.



Current status is recorded in PUBLIC-SNAPSHOT.md; personal validation notes were removed from this publication copy.

