# v0.3.0 · r2 UI polish (build ui-polish.20261103.2)

- Replaced the control-center view with WPF on the existing single WinForms host. The compact status, settings and help pages now share a single title/header, consistent 1px borders and short interruptible motion. Notification identity, policy, user state and shortcut guards retain their prior implementation.
- Language changes update tagged static labels and save asynchronously with serialized persistence and failure rollback. Page transitions are immediate; system reduced motion and high contrast suppress decorative motion.
- The previous v0.3.0 build ui-fix.20261103.1 and historical v0.3.0-rc.2 tag remain distinct.

# v0.3.0 — repaint and bilingual UI revision

- Repairs native background/rounded-corner artifacts and stale page sizing after restore.
- Adds saved Chinese/English choice in the same application, including notification wording and English online help/privacy.
- Removes repeat binding/protection reads from visible UI refresh.
- Keeps optional Ctrl+Alt+A–Z activation, consent, signed-image/current-user gates, incident protection and single host unchanged.
- UI display v0.3.0 is separate from build revision ui-fix.20261103.1.
- Update the existing release; preserve the old tag/assets as historical rollback. GitHub automatic Source code archive of v0.3.0-rc.2 is NOT this revision. Use the new source.zip and manifest SourceFiles hashes.
# 0.3.0-rc.2 — Compact control center

Top navigation, notification illustration, compact setting rows, edit-only save area and focused shortcut recording with A–Z fallback. Corrects Shift support wording. Existing bindings survive ordinary saves; same-key reconfirmation after updates uses existing gates. No native input-path expansion.
# 0.3.0-rc.1 — 2026-11-02

- Rebuilt the real WinForms control center with Overview, Notification settings, and Privacy/help; clearer state, inline feedback, keyboard actions and scrollable content with persistent save controls.
- Added a display-only host boundary and isolated UI preview. No new framework, resident process, tray icon or notification/input pathway.
- Clarified user-confirmed shortcut setup, pause/exit, experimental version support and notification limitations.
- Added Chinese-first product documentation, English guide, versioned privacy notice, privacy-conscious issue template and real shared-form preview captures.
- Retained notification engine, process/shortcut guards, single-instance lifecycle, installer/rollback/uninstaller and incident policy from rc.2.
- UI/layout checks are distinct from predecessor notification acceptance. No new real-message, all-machine or long-duration claim; installed rc.1 is unchanged.
# 0.2.0-rc.2

- Fix legacy incident parsing: missing/null `blocked`, wrong field types, invalid JSON and non-object/null roots now fail closed rather than treating an absent boolean as `false`.
- Preserve explicit `blocked: false` compatibility and the separately scoped known legacy window incident; no notification, input, activation or installation behavior is otherwise changed.
- Add in-memory regression cases using the same parser as production. Version labels/metadata updated.
- Not installed on the development machine and not live-click accepted. rc.1 human acceptance remains predecessor evidence only.

# 0.2.0-rc.1

- Independent release candidate of the working public-attention notification path.
- No tray icon is created. Single background host with on-demand Chinese settings and bounded current-user/session IPC.
- Explicit first-use consent, optional current-user startup, portable/current-user installation and owned-file uninstall.
- Dynamic official installation discovery and Windows/Tencent signer validation; local icon extraction with an original helper-icon fallback.
- User-confirmed shortcut binding; no assumed global shortcut, no invasive window recovery, no zero-process WeChat launch.
- Actual configuration-handle path check protects against packaged-parent AppData virtualization.
- Staged manual package update, one prior package for rollback, clean source whitelist and checksums.

Release candidate: no claim of perfect notifications, cross-machine acceptance, arbitrary WeChat version compatibility, or complete cold-COM/window-state validation. Refer to docs/VALIDATION.md.
