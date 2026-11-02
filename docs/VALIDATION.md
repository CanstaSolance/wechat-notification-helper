# 0.3.0-rc.2 — compact product UI / shortcut entry

The 920×680 shared WinForms control center replaces the sidebar with segmented navigation, a static explicitly labeled notification illustration, compact setting rows and edit-only save actions. No new framework, resident host or WeChat event/input source.

Shortcut input is focused normal window key handling only. The shipped parser still accepts Ctrl+Alt+A–Z and rejects Shift. Recording commits on letter release; Escape, Tab, lost focus, window deactivation and a 12-second timeout cancel without persistence. An A–Z selector provides a no-typing fallback when another global shortcut intercepts input. Recording does not verify WeChat's actual shortcut. Changed bindings and same-key re-confirmation after an application update use the existing confirmation gate. Ordinary setting saves skip binding writes.

Focused checks cover normalization against the actual parser for all letters, rejection of Shift/function/save accelerators, Escape/focus cancellation preserving an old value, release commit, unchanged-binding save policy, same-key update re-confirmation and changed-path re-confirmation. Isolated shared-form handler fixtures cover consent/save/restore/error and fallback selection resetting confirmation.

Real OS shortcut capture with WeChat's global registration was NOT tested: this task does not connect to or operate WeChat. The fallback selector is exercised against the real shared control. No new notification delivery, installer or native input acceptance is claimed. Captures are actual shared-form renders with an isolated memory host, visibly labeled. Scaling captures simulate layout/fonts; physical monitor transitions/high contrast remain unverified.

Older validation below is historical and does not establish this binary's live acceptance.

---
# 0.3.0-rc.1 control-center validation — 2026-11-02

This release changes the control center and public documentation. It does not extend WeChat event coverage or claim new incoming-message/notification-click acceptance. The installed rc.1 was not replaced.

## Focused local UI checks

- Production WinForms UI compiled with warnings treated as errors.
- An isolated executable links the exact production SettingsForm, controls and display model. Its clearly labeled in-memory host does not read user configuration, connect to WeChat, register notifications, send shortcuts or alter startup. Captures are the actual running shared form, cropped to its client area; only the three public 100% images are exported.
- The three navigation pages were rendered and visually inspected at baseline layout. Keyboard Tab/Enter opened settings in the real preview window; Alt+S exercised the first-consent inline error and successful in-memory save, including locking the accepted consent display.
- The shared controls/handlers were also exercised directly in the isolated process: first-consent gate, memory save, unsaved-edit restore, and failed-save inline feedback all passed. This is not an acceptance of production disk/registry writes or Windows notification delivery.
- Layout/font scaling simulations at 150% (1420×960 viewport) and 200% (1888×1000) were rendered. Settings layouts were visually inspected: content can scroll and save/restore remain visible. The simulation found and corrected an implicit outer-row height that could leave the footer beyond the viewport. All pages remain parented for layout/DPI propagation.
- These are application scaling simulations, not changes to Windows display settings or actual 144/192-DPI monitor tests. Physical DPI transitions, multi-monitor behavior and live high-contrast switching remain unverified. System high-contrast colors and native control/keyboard semantics are implemented.
- During later computer-use checks, a window capture returned black and input state became uncertain. Further OS input was stopped. Restore and error behavior are reported from the explicit shared-form handler checks, not recategorized as mouse/keyboard acceptance.
- UI preview processes are transient and removed after checks. No additional monitoring host was installed. Closing the real control center still uses the existing host's single-instance/background lifetime; this unchanged lifecycle has predecessor acceptance, not new exact-binary deployment acceptance.

The only new isolated release-policy checks cover honest status mapping: an enabled installation without a live target remains Waiting, lack of consent cannot advertise monitoring, and a missing ready source cannot advertise monitoring. The pre-existing guards remain part of the normal build checks.

## Scope deliberately not retested

No WeChat account/window/chat operation, live message, original tray-click repair, shortcut binding, installation upgrade, complete uninstall/rollback, system display change or 2/6-hour resource run was performed. Incident protection and the existing input transaction are retained. No all-machine, perfect notification or permanent update-compatibility claim is made.

The historical evidence below belongs to older binaries and is retained for context.

---
# Validation scope

This is release candidate 0.2.0-rc.2. Compilation and isolated checks do not establish all-machine behavior. The local human acceptance described below belongs to predecessor rc.1, not the new rc.2 DLL.

## rc.2 minimal protection fix

The frozen rc.1 assembly was checked without launching the app or reading/modifying live incident files: in-memory `{}` and a record containing only `reason` both returned unblocked. Its non-nullable constructor boolean was populated as `false` when omitted. This is a real fail-open boundary for a legacy record, not a current-machine monitoring outage.

rc.2 makes the legacy flag nullable, blocks an absent/null flag, and shares a small stream parser between production and isolated checks. Wrong types, invalid syntax and null/non-object roots return no trusted record and are blocked. A valid explicit `false` still permits the existing compatibility path; the known active window-only legacy reason and the independent new-product block retain their earlier semantics. No live incident file is edited.

The regression cases run only on in-memory JSON through that production parser. The input transaction, notification path and installer are unchanged apart from release version labels. rc.2 has not been installed or sent a test notification; it does not inherit rc.1's exact-binary live acceptance.

## Evidence carried from the accepted implementation

On one Windows 11 x64 machine with official WeChat 4.1.15.56, the preceding implementation received a real notification click, opened the existing logged-in WeChat through the user's confirmed shortcut, and the user explicitly observed no tray overflow flash. Its native input/foreground path is retained. Its private acceptance logs, local bindings, extracted icons and user identifiers are not included in this repository.

A rapid hide-and-immediate-restore attempt previously failed and is not recategorized as passing. Shortcut collisions, user changes, suspended/locked desktops and application updates can still prevent activation. The code declines unsafe conditions and pauses repeated failures.

## rc.1 predecessor checks

- Clean framework-dependent publish using locked cached dependencies; no SDK/runtime bundle or developer settings in package.
- Isolated policy checks: first-use consent, bounded IPC/payloads, scoped incident protection, rejected foreign image/chords, partial-input cleanup, and system suppression.
- First-use settings opened from a directory containing Chinese characters and spaces through the native Explorer context. No consent or shortcut was prefilled; source was not started. A second settings request reused the same resident. Closing unconsented settings terminated the candidate.
- A hidden-launch settings visibility defect found during local validation was corrected using only the helper's own settings window.
- Windows PowerShell 5.1 parses the installer/uninstaller and correctly decodes their UTF-8 BOM Chinese text. Native current-user installation and a staged package upgrade completed. A hidden-AppData ancestor inspection defect found in native installation was fixed; failed attempts remain local review evidence.
- A real native host persisted and reread its own user-confirmed binding. Normal shutdown/restart reinitialized the notification identity successfully after fixing an initial-name/Start-menu-name mismatch; COM target and shortcut ownership checks were not relaxed.
- Actual public source resume bound five existing process leases, then pause deregistered the source; no notification was submitted during this short transition. This does not establish receipt of a new incoming message in the new build.
- The uninstall preflight function was exercised in Windows PowerShell 5.1 on an isolated owned fixture: modified contents, an unsafe name and an unknown file all caused refusal while retaining the original. Full actual uninstall and rollback remain pending local acceptance.
- The installed official WeChat 4.1.15.56 was recognized by read-only signature checks; known legacy incident protection remained unchanged and did not imply new shortcut consent.

## rc.1 predecessor live acceptance and local migration

The exact rc.1 candidate binary was tested through one explicitly marked Windows notification on the same Windows 11 x64 / WeChat 4.1.15.56 machine. The user confirmed that the original WeChat opened normally with no tray overflow flash. The matching hot-host callback and activation result confirmed the existing process was foreground, no new WeChat process was started, and the overflow menu was never opened. The component measured 159 ms after callback processing began, including a 124 ms shortcut transaction; this is not a measured full physical-click latency. Raw receipts and local identifiers are private and are not exported.

The same tested binary then replaced the local old helper's observer role after exact owned-registration/configuration backup. The old helper was normally stopped and its own login startup, notification identity and Start-menu entry retired; its program and incident block were retained for exact rollback. The new host reread the equivalent pre-authorized local startup setting, resumed the public source and bound the five original process leases. Original WeChat creation epochs and the old incident block hash were unchanged. This local user's prior startup consent is not a default for new installations.

rc.1 code/binaries were frozen after that acceptance and the installed rc.1 remains unchanged. rc.2 changes only the protection parsing and version metadata noted above. These statements describe bounded local checks, not long-term stress acceptance.

## Not claimed passed

Cross-machine installations, Windows 10, ARM64/x86, all DPI/multi-monitor cases, full startup/update/rollback/uninstall failure matrix, cold COM in every state, missing runtime/enterprise policy variations, all shortcut collisions, exact per-message deduplication, and 2/6-hour resource stability.

The candidate does not launch WeChat with zero processes. Future signed versions use experimental public attention events only and require new shortcut confirmation. No compatibility promise covers private WeChat internals.

## Manual release acceptance

On a separate Windows test user/machine: install .NET Desktop Runtime if needed; extract and install the package; confirm native user state and explicit opt-in; save an actually configured shortcut; close settings and verify there is no helper tray icon; pause/resume/open settings using the existing host; inspect normal source behavior without reading message content; click one explicitly marked test notification and verify original process epochs/no tray flash; quit normally; test startup ownership, package update/one-version rollback and complete owned-file uninstall. Preserve unexpected/modified files on failure. Do not send test messages or operate login on behalf of users.

Memory measurements must identify process roles, sample duration, CPU delta, working set/private bytes and limitations. A short sample is not a leak test; no forced garbage collection or working-set trimming is used to improve numbers.
