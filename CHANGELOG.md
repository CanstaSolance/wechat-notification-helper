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
