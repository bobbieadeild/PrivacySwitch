# Public snapshot notes

This publication copy removes original workstation hardware/OS details, local diagnostic observations and personal test history. It retains implementation, tests, recovery architecture, build instructions and operational limits. No runtime recovery journal, raw process report, account data or work directory is included. Existing C:\Users\x source paths are fictional exclusion-test fixtures. Program Files and Windows paths describe platform APIs and installation locations, not a particular person's account.

Current implementation: Windows microphone hardware-supported endpoint mute and camera devnode disable with original-state recovery; exact session Copilot-key chord blocking; reviewed one-time force-close of verified Copilot executable/package targets and scoped WebView descendants. The installed mscopilot.exe and mscopilot_proxy.exe variants are supported. Browser/Office hosts and unrelated WebViews remain excluded. No continuous relaunch prevention. Ctrl+Alt+Q is unused.

The corrected Copilot close received a successful manual test report; this does not establish universal compatibility. Four Windows simulation suites pass. Driver readback is not end-to-end recording proof. Linux/macOS companion validation remains pending. Earlier implementation descriptions in the historical documents may be superseded: use current code and these notes for current status.

Build.ps1 compiles all five C# sources with the included ICO and runs all four simulation suites. START-HERE.md describes fresh-computer setup; HANDOVER.md records architecture; LIVE-TEST.md describes intentional functional tests. No previous machine data or chat is required. The executable is unsigned.

No license has been selected. Choose a license before describing this as licensed open source. The project makes no external artwork/font dependency. MANIFEST.sha256 covers all public payload files except itself.
