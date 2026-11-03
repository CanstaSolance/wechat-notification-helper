# Privacy notice — v0.3.0
Updated 2026-11-03. Independent, unofficial Windows helper.

## What it reads
Public process/window metadata (process IDs, creation times, session/current-user ownership, executable paths, foreground/visibility state), installation paths, file version/size/time, official signature and file fingerprint. Windows notification permissions/suppression are checked. An icon may be extracted from your locally verified WeChat installation.

It does not read messages, contacts, chat databases, passwords, login tokens, cookies or clipboard contents. The app does not capture WeChat screenshots or install global input hooks. Public UI screenshots show isolated helper windows, not real chats.

## Optional shortcut
A valid helper notification click may request one attempt using a shortcut you personally confirmed. Existing current-user/session, exact-image fingerprint/version, live-process, desktop, held-key and safety-record gates remain. Only Ctrl+Alt+A–Z is supported, without Shift. Recording uses focused normal key events, never a global hook. Escape, Tab, deactivation or a timeout cancels. Use the letter selector if WeChat intercepts the combination. Neither recording nor selecting sends a test shortcut.

The helper does not start WeChat, sign in or navigate to a particular chat. An update requires renewed confirmation. Repeated failures pause activation; safety records are kept.

## Local data
Settings and limited latest-status files are under %LOCALAPPDATA%\WeChatNotify. They can contain program paths, a Windows SID, confirmed shortcut, file fingerprint/version, process metadata, random action IDs, timestamps and errors. They are not necessarily anonymous. ui-language.json stores only your selected helper language.

Installation is under %LOCALAPPDATA%\Programs\WeChatNotify; one .previous installation is kept for rollback. Current-user notification/COM identity, a Start-menu entry and optional sign-in startup entry are registered. The Windows notification identity stays 微信提醒助手 for compatibility in both UI languages.

## Notifications and networking
Generic notifications contain no sender/body, and do not guarantee every message. Windows can merge/suppress alerts or retain them after exit. Clicking an old alert can briefly start a callback process without restarting monitoring.

No proprietary upload, telemetry or automatic update service is implemented. Online links open GitHub in your browser. GitHub, the browser, Windows and WeChat have their own data/network behavior.

## Exit, uninstall and support
Closing enabled settings leaves the helper running. Use Help > Quit to stop. Before consent, closing exits. Uninstall removes owned registration, known settings (including language), icons and manifest-verified files. Unknown/modified files and safety records remain; uninstallation may stop rather than delete them. It does not delete WeChat/chat data. Notification identity cleanup cannot guarantee immediate removal of every Windows history/cache item.

Do not upload raw settings, personal paths, SID, chat screenshots or contact names to public issues. Share versions and reproduction steps only.

[English guide](README.en.md) · [中文隐私说明](PRIVACY.md)

This revision is tracked separately from the historical v0.3.0-rc.2 tag. Download the new v0.3.0 source ZIP for exact current sources, not GitHub's automatic archive of the old tag.
