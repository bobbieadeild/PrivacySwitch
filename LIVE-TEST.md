# Live-test investigation and verification limits

Date: 7 October 2026. Revision: mic-camera-hardware-mute-2026-10-07-r2.

## Observed evidence

The user pressed the previous app's disable switch and reported that the ongoing Codex voice call still received fresh speech. The previous implementation's success criteria were insufficient. It disabled the microphone's software AudioEndpoint devnode and treated Configuration Manager problem code 22 as proof of restriction. It did not inspect the Core Audio endpoint or an existing recording stream.

## Correction

New microphone actions use documented, driver-reported hardware endpoint mute instead of devnode disable. `EnumAudioEndpoints(eCapture)` limits selection to capture devices. Only endpoints reporting hardware mute capability are changed; software-only mute is unsupported because exclusive-mode capture can bypass it. The original mute bit is saved before mutation and restored later, preserving already-muted endpoints. Speaker/render endpoints and shared controllers remain untouched.

The app checks support and mute readback. It labels results “driver reports hardware-muted; live recording cutoff UNTESTED.” Camera results similarly separate devnode status from effective live cutoff. No “protected” state is claimed. Camera disable uses `CM_DISABLE_PERSIST` (8); the prior version used 0, which does not persist across reboot. Microphone mute persistence is driver-dependent.

This correction provides a microphone-only mechanism supported by the local driver's reported capability. It does not prove physical hardware isolation or stopped recording. Driver errors and external unmute remain possible. Neither undocumented `IPolicyConfig` controls nor a shared-controller workaround are used.

## Validation

## Deliberate user verification

Close the older executable and launch the revised app from the extracted package. Read the current capture audit. Click **Mute microphones / disable cameras** and approve elevation only when ready for voice input to stop. The assistant cannot promise a written reply will reliably appear after that interruption.

After applying, try fresh speech in the existing voice session and separately test a newly opened recording session. Also test a camera session if camera cutoff is required. If fresh input continues, consider the affected restriction failed even if driver readback says muted or disabled. Use **Restore microphones / cameras** to recover the original states and preserve the audit for further diagnosis. Until this test succeeds, do not rely on this build as a verified microphone/camera cutoff tool.

Official references: [hardware versus software endpoint controls](https://learn.microsoft.com/en-us/windows/win32/api/endpointvolume/nn-endpointvolume-iaudioendpointvolume), [SetMute](https://learn.microsoft.com/en-us/windows/win32/api/endpointvolume/nf-endpointvolume-iaudioendpointvolume-setmute), [Core Audio device states](https://learn.microsoft.com/en-us/windows/win32/coreaudio/device-state-xxx-constants), [persistent device disable](https://learn.microsoft.com/en-us/windows/win32/api/cfgmgr32/nf-cfgmgr32-cm_disable_devnode).

