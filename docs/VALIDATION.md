# Validation scope

This is a release candidate. Compilation, isolated checks and one local human acceptance do not establish all-machine behavior.

## Evidence carried from the accepted implementation

On one Windows 11 x64 machine with official WeChat 4.1.15.56, the preceding implementation received a real notification click, opened the existing logged-in WeChat through the user's confirmed shortcut, and the user explicitly observed no tray overflow flash. Its native input/foreground path is retained. Its private acceptance logs, local bindings, extracted icons and user identifiers are not included in this repository.

A rapid hide-and-immediate-restore attempt previously failed and is not recategorized as passing. Shortcut collisions, user changes, suspended/locked desktops and application updates can still prevent activation. The code declines unsafe conditions and pauses repeated failures.

## Candidate checks

- Clean framework-dependent publish using locked cached dependencies; no SDK/runtime bundle or developer settings in package.
- Isolated policy checks: first-use consent, bounded IPC/payloads, scoped incident protection, rejected foreign image/chords, partial-input cleanup, and system suppression.
- First-use settings opened from a directory containing Chinese characters and spaces through the native Explorer context. No consent or shortcut was prefilled; source was not started. A second settings request reused the same resident. Closing unconsented settings terminated the candidate.
- A hidden-launch settings visibility defect found during local validation was corrected using only the helper's own settings window.
- Windows PowerShell 5.1 parses the installer/uninstaller and correctly decodes their UTF-8 BOM Chinese text. Native current-user installation and a staged package upgrade completed. A hidden-AppData ancestor inspection defect found in native installation was fixed; failed attempts remain local review evidence.
- A real native host persisted and reread its own user-confirmed binding. Normal shutdown/restart reinitialized the notification identity successfully after fixing an initial-name/Start-menu-name mismatch; COM target and shortcut ownership checks were not relaxed.
- Actual public source resume bound five existing process leases, then pause deregistered the source; no notification was submitted during this short transition. This does not establish receipt of a new incoming message in the new build.
- The uninstall preflight function was exercised in Windows PowerShell 5.1 on an isolated owned fixture: modified contents, an unsafe name and an unknown file all caused refusal while retaining the original. Full actual uninstall and rollback remain pending local acceptance.
- The installed official WeChat 4.1.15.56 was recognized by read-only signature checks; known legacy incident protection remained unchanged and did not imply new shortcut consent.

## Candidate live acceptance and local migration

The exact candidate binary was tested through one explicitly marked Windows notification on the same Windows 11 x64 / WeChat 4.1.15.56 machine. The user confirmed that the original WeChat opened normally with no tray overflow flash. The matching hot-host callback and activation result confirmed the existing process was foreground, no new WeChat process was started, and the overflow menu was never opened. The component measured 159 ms after callback processing began, including a 124 ms shortcut transaction; this is not a measured full physical-click latency. Raw receipts and local identifiers are private and are not exported.

The same tested binary then replaced the local old helper's observer role after exact owned-registration/configuration backup. The old helper was normally stopped and its own login startup, notification identity and Start-menu entry retired; its program and incident block were retained for exact rollback. The new host reread the equivalent pre-authorized local startup setting, resumed the public source and bound the five original process leases. Original WeChat creation epochs and the old incident block hash were unchanged. This local user's prior startup consent is not a default for new installations.

No source/native activation code was changed after that acceptance. Documentation and clean exports were finalized separately. These statements describe bounded local checks, not long-term stress acceptance.

## Not claimed passed

Cross-machine installations, Windows 10, ARM64/x86, all DPI/multi-monitor cases, full startup/update/rollback/uninstall failure matrix, cold COM in every state, missing runtime/enterprise policy variations, all shortcut collisions, exact per-message deduplication, and 2/6-hour resource stability.

The candidate does not launch WeChat with zero processes. Future signed versions use experimental public attention events only and require new shortcut confirmation. No compatibility promise covers private WeChat internals.

## Manual release acceptance

On a separate Windows test user/machine: install .NET Desktop Runtime if needed; extract and install the package; confirm native user state and explicit opt-in; save an actually configured shortcut; close settings and verify there is no helper tray icon; pause/resume/open settings using the existing host; inspect normal source behavior without reading message content; click one explicitly marked test notification and verify original process epochs/no tray flash; quit normally; test startup ownership, package update/one-version rollback and complete owned-file uninstall. Preserve unexpected/modified files on failure. Do not send test messages or operate login on behalf of users.

Memory measurements must identify process roles, sample duration, CPU delta, working set/private bytes and limitations. A short sample is not a leak test; no forced garbage collection or working-set trimming is used to improve numbers.
