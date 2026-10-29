# 0.2.0-rc.1

- Independent release candidate of the working public-attention notification path.
- No tray icon is created. Single background host with on-demand Chinese settings and bounded current-user/session IPC.
- Explicit first-use consent, optional current-user startup, portable/current-user installation and owned-file uninstall.
- Dynamic official installation discovery and Windows/Tencent signer validation; local icon extraction with an original helper-icon fallback.
- User-confirmed shortcut binding; no assumed global shortcut, no invasive window recovery, no zero-process WeChat launch.
- Actual configuration-handle path check protects against packaged-parent AppData virtualization.
- Staged manual package update, one prior package for rollback, clean source whitelist and checksums.

Release candidate: no claim of perfect notifications, cross-machine acceptance, arbitrary WeChat version compatibility, or complete cold-COM/window-state validation. Refer to docs/VALIDATION.md.
