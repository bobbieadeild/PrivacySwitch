# Copilot process detector correction

The revised detector uses native Toolhelp process enumeration, limited-query process handles, package-family identity and full executable paths. It supports the existing exact family/main-name match and executable images under the protected Program Files/WindowsApps directory for a precisely named Microsoft.Copilot version/architecture/Microsoft-publisher package folder. It does not trust a Copilot-looking filename or a similarly named folder elsewhere.

WebView2 children and grandchildren are included only when their parent chain leads to a verified Copilot root, in the same session, with creation times compatible with each parent. Unrelated WebView2 processes, browser msedge, Explorer, RuntimeBroker and application frame hosts are not targeted. Other unrecognized integrations and executable variants remain unsupported rather than being guessed.

Before closing, the app takes another native snapshot and rechecks each reviewed PID, creation time, root identity/creation time and ancestry. It opens termination handles only for those matches and checks creation/image identity again. Handles refer to the verified process object rather than blindly terminating a PID that could have been reused. Exit results are reported, and a follow-up snapshot reports verified targets still present/relaunched. New unreviewed processes are not automatically killed. No complete Copilot shutdown or relaunch-prevention guarantee is made.

**Inspect Copilot (read-only)** opens a bounded, wrapped, copyable diagnostic dialog showing verified targets and Copilot/WebView candidates, parent IDs, package family and executable path. It records no typed content, runs no hook and terminates nothing. The report is not automatically saved or transmitted. The enable/force-close confirmation and results now also use bounded owned dialogs. If inspection fails or finds no verified target, key blocking can still be enabled with explicit no-termination status.

Tests passed for trusted package path, wrong publisher/untrusted-location rejection, root/child/grandchild inclusion, unrelated WebView and browser exclusions, stale-parent-time rejection, cross-session rejection, and all prior key/device/UI checks. Simulated preview was reinspected. Actual desktop process detection and termination remain unvalidated here; no real process was terminated during this correction.

To validate, close the old app, extract and launch the updated one, and use **Inspect Copilot** while Copilot is running. If verified targets are listed, the main action reviews them before force-close. Unsaved app state can be lost. If detection still misses it, copy the diagnostic report or attach a screenshot directly; process names alone are insufficient to safely identify shared subprocesses.

## Correction for the reported mscopilot installation

A simulated replay of the supplied process layout selects all 11 Copilot processes and excludes the unrelated Client.CBS and Client.WebExperience WebView trees. Device, keyboard, detector and UI simulation checks pass. Actual desktop termination has not been exercised in the development environment. Close the previous PrivacySwitch instance and run the rebuilt executable before testing; an already running instance retains the old detector.

