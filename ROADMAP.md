# Future profiles — concept only

The user proposed versioned, one-button privacy/hardening profiles after the microphone/camera app, including telemetry, offline isolation and AI hardware/integration inventory. These are future ideas, explicitly not requested for implementation now. Current deliverables remain microphone/camera controls and platform-specific capability foundations; this document adds no executable controls.

The central future goal is choosing when optional AI integrations may operate or access personal content—typing, screens/images and metadata—with profiles ranging from open everyday use to stricter privacy and a minimal-computer experience. This is a request for control over actual capabilities/data flows, not simply disabling chips. It is not evidence that any particular integration is keylogging or transmitting content.

Potential reviewed controls include supported AI feature/service enablement, content indexing, screen-capture and other content permissions, cloud connections, optional diagnostic settings, and a local AI allowlist where desired. Optional accelerator restriction and offline interface controls are separate layers. Ordinary software can collect information without an NPU, and local AI need not transmit anything. No profile can promise removal of all AI or restoration of an older operating system's behavior.

## Suggested sequence and prerequisites

| Stage | Proposed scope | Prerequisites and honest limits |
|---|---|---|
| Current r2 | Windows hardware-supported microphone mute, camera disable, recovery; Linux session-mute source; macOS detection/guidance | Driver readback is not effective cutoff. Windows live regression test and Linux/macOS real-machine validation remain required |
| Next validation release | Stable identity/capability checks, journal migration, protected state, existing/new-stream and hotplug tests, clear per-platform enforcement | Use disposable/secondary test hardware; preserve speaker output and previously disabled/muted states |
| Baseline regular-use profile | Capability/audit dashboard and supported recommendations | Keep Firewall, Defender/appropriate security protection and OS updates enabled; do not turn them off as a privacy measure |
| Future telemetry profile | Reviewed supported software/service/app controls with edition/version applicability | Explain what each control reduces, measure where feasible, save precise originals, report unsupported cases. No zero-telemetry claim |
| Future offline profile | Reviewed local-interface isolation and restore | Inventory all applicable local paths: Wi-Fi, Ethernet, cellular, Bluetooth networking, USB adapters, VPN/virtual interfaces and new interfaces. Wi-Fi-only disable is not offline. Remote access and voice calls may stop |
| Future AI review profile | Incomplete hardware/software integration inventory and supported app-feature controls | NPU/AI accelerator presence is not evidence of telemetry. Distinguish app integration, CPU/GPU/NPU computation, data access, networking and firmware |
| Vendor-specific advanced review | Optional supported accelerator/firmware considerations | No generic driver unload or one-click firmware guarantee. Manual vendor BIOS/UEFI actions, reboot and expert testing may be required |

The recommended first work is validating the current Windows capture fix and choosing a Linux/macOS test environment, before expanding controls. That ordering avoids building profiles on an unverified enforcement mechanism. It is an engineering recommendation, not a guarantee of strong security.

## Common profile requirements

Each proposed profile should expose a concrete preview: target, original value, intended value, permission needed, interruption/reboot implications, enforcement level and known bypasses. Version/capability detection must gate actions rather than blindly writing unsupported settings. Save originals and durable mutation intent before each action; refuse conflicting snapshots, retain partial failures and offer precise rollback for controls with a validated reversible path. Avoid overwriting external changes silently.

Profiles should remain independent: microphone/camera restore should not reenable networking or weaken security implicitly. Controls whose rollback cannot be made reliable should be omitted from the reversible one-button set or presented as explicit manual steps. A profile collection is not a true sandbox without an actual isolation architecture.

## Offline and AI boundaries

An app can manage permitted local interfaces; it cannot guarantee hostile-administrator-proof network isolation. Other devices, hotplug, firmware radios and alternate physical paths require separate consideration. Physical disconnection and independent verification may be appropriate for stronger threat models. Do not label a profile “Snowden-grade” or guarantee surveillance resistance; strong threat models require separate expert validation.

Disabling an accelerator does not disable AI on CPU/GPU or remove every integration. AI telemetry is a software/network behavior, not a property inferred from an NPU chip. Hardware/firmware inventory will be incomplete and vendor-dependent. Firmware controls may require manual BIOS changes and reboots, may affect boot/security/functionality, and may lack reliable automated restore. No firmware flashing, SIP/security bypass or blanket shared-driver unloading is proposed.

None of these future controls are implemented by this roadmap. Their inclusion requires a separate scoped request, supported mechanism, test environment and reviewable reversibility plan.

## Additional future concept: secure erase / device disposal

The user proposed whole-drive/computer sanitization with repeated confirmations. The current GUI adds only an informational **Planned / future version** card. It opens bounded help, not a selectable erase operation. No disk access, wiping, formatting, sanitize commands or destructive execution is implemented.

Secure erase is irreversible and must be separate from reversible privacy profiles. A future design would need backup verification, ownership and exact device/scope review, staged warnings and explicit final confirmation. Erasing an active system drive typically requires a separate recovery or vendor-supported workflow. Multiple dialogs alone do not validate correct device selection or successful sanitization.

“Infinite wipes” is not a valid sanitization plan. HDD and SSD methods differ; SSD wear-leveling means repeated logical overwrites cannot guarantee all physical data is erased. Do not propose indefinite passes or unnecessary SSD wear. Any future implementation should consult [NIST SP 800-88 Revision 2](https://csrc.nist.gov/pubs/sp/800/88/r2/final), the then-current standards and the specific vendor's supported sanitize/secure erase/cryptographic erase capabilities, prerequisites and verification. No universal complete-erasure claim or promise of removing cloud copies/backups is appropriate.

