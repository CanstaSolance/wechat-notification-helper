# Development and release

## Scope

The product contains public Shell attention observation, held read-only process leases, notification policy/delivery, signed local-image discovery, user-confirmed native shortcut activation, current-user installation/identity, and on-demand settings. There is no UI Automation/MSAA conversation worker, Qt integration, debugger, DLL injection, database reader, network interception or Tencent resource in the source export.

The original generic attention policy is retained, including startup suppression, foreground suppression, per-window deduplication, global cooldown, and system presentation policy. The generic source has no per-message/contact knowledge. The retained native shortcut transaction checks session/user/path/birth/liveness, input desktop, held keys, current foreground, exact user-confirmed signed image hash/version, a cross-process transaction mutex, and partial-input ownership cleanup. It sends no text.

## Build

Install the SDK selected in global.json (10.0.401, latest patch roll-forward). Runtime users need the separately installed x64 .NET 10 Desktop Runtime; build users additionally need the SDK. No self-contained/AOT/trimmed build is claimed. The Windows API projection is deliberately not trimmed to avoid unsupported WinForms/WinRT trimming behavior.

```powershell
pwsh -File scripts/Build.ps1
```

For an already populated NuGet cache use `-Offline`; it never fetches a runtime or SDK. Package versions are locked, including the Windows SDK .NET projection version. `PathMap` avoids embedding developer source paths. Repeatable build inputs are pinned; ZIP timestamps are not promised byte-reproducible. Build compares exported-source hashes to the whitelist and writes package checksums.

The script produces a framework-dependent ZIP, a clean source ZIP, an explicit clean `source` folder, package-files.json and SHA256SUMS.txt. The source export only copies `source-files.json` entries. Never push the old engineering tree or Git history. Do not upload artifacts/private, bin/obj, personal settings, logs, incident files, extracted Tencent icons, credentials or local acceptance captures.

CI on Windows restores locked dependencies, runs isolated checks, checks Windows PowerShell 5.1 script parsing/Chinese decoding, and builds packages. It has no real WeChat and cannot perform live acceptance. Workflow uploads artifacts only; it does not publish a release or obtain credentials.

## State and lifecycle

Normal execution is WinForms without a NotifyIcon or main window. Settings are a temporary normal window on the same host UI thread. Session-scoped mutex and current-user-only named pipe prevent a second monitoring host; IPC is restricted to fixed verbs and an exact activation nonce. Both requests and responses have size/time bounds. COM cold activation has a 12-second lifetime and no monitoring source. If forwarding is uncertain it never retries the shortcut directly.

User state is under the actual `%LOCALAPPDATA%\WeChatNotify`, independent of binary location. A handle-derived final path check detects MSIX/AppData virtualization and the setup/background entry can relaunch through the existing Explorer desktop. It checks again; failures stop. No diagnostic file is written to the binary directory.

First-use consent is explicit and not seeded by the developer's settings. Only a signed local Weixin.exe/WeChat.exe with an accepted Tencent signer can supply process bindings or icons. Signature verification uses Windows Authenticode with cached URL retrieval only. No claim is made that local trust caches are complete or freshly revoked online. Multiple installations require user choice.

New official-image versions may continue experimental public attention observation after re-verification; old shortcut hashes cannot authorize new-image input. No private adapter download or zero-process launch exists. Users update the helper manually from a trusted release; package checksums catch corruption, not a malicious publisher.

## Incident protection

Own incident-block.json blocks public shortcut activation and is preserved on uninstall. An unparseable legacy record or an active legacy block with an unknown reason fails closed. The explicitly classified legacy `STABILITY_INCIDENT_CHAT_NONINTERACTIVE_INPUT_MISSING` incident covers invasive window operations/probes, which are absent here; it does not by itself grant shortcut consent. Public shortcut activation still requires fresh per-user consent and its signed-image binding. No incident file is overwritten, cleared or migrated to bypass it.

## Ownership and rollback

Start menu, COM/AUMID registration, HKCU Run entry and packages are owned by this executable/product. They do not alter WeChat's startup/identity. Installer verifies all package hashes before staging and keeps one previous installation. Uninstaller preflights package ownership, flat safe names, duplicate names, hashes, unknown files and reparse points before deletion; it stops with an explanation on mismatch. No recursive user-folder cleanup is used. Custom/unknown files and incident blocks are preserved. A previous package can be restored with Rollback.cmd.

MIT covers project-owned code. Third-party terms and notices are included separately. Microsoft's published redistributable list explicitly covers the Windows SDK .NET projection and WinRT.Runtime assemblies for WinRT API use: https://learn.microsoft.com/en-us/legal/windows-sdk/redist (see the Microsoft.Windows.SDK.NET.Ref section). Validate the actual package notices again if dependencies change.
