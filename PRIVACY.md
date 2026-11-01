# 隐私说明 / Privacy notice

适用版本：**0.3.0-rc.1**  
更新日期：**2026-11-02**  
适用对象：本仓库发行的微信提醒助手。它是独立、非官方工具，与微信、腾讯或微软没有隶属关系。

这份说明根据本版本源码编写。它解释工具自己的行为；不替代 Windows、微信、GitHub 或浏览器的隐私说明。

## 1. 助手读取什么，为什么

**安装与程序身份。** 为避免把操作发给错误程序，助手检查正在运行的 Weixin/WeChat 进程、常见安装目录和 Windows 的程序路径注册信息；读取选定程序的路径、文件版本、大小、修改时间、数字签名证书与文件指纹。签名校验使用 Windows 的本地缓存模式。它读取的是安装程序文件，不是聊天数据库。

**公开的运行状态。** 助手检查进程编号、创建时间、Windows 会话与当前用户归属、程序路径及是否已退出；通过 Windows 公开的关注事件判断是否应提交通用提醒。它也检查前台窗口归属、窗口有效性、可见/最小化/可用状态，以决定抑制提醒或确认打开结果。它不读取窗口聊天文本、联系人列表、消息正文或剪贴板。

**系统通知状态。** 助手检查本工具的通知权限和 Windows 的通知抑制状态。系统勿扰等可能使通知只留在通知中心，或者阻止通知。查询失败时采用保守行为。

**本地图标。** 优先从本机已验证的官方微信程序提取一个图标，保存在助手自己的缓存目录，以便通知显示身份。没有下载腾讯 Logo，也没有把本机提取素材重新分发进安装包；提取失败时使用助手自己的图标。

**安全保护。** 为保留已有事故保护，助手读取自己目录中的保护标记，并可能读取旧版本 WeChatNotifier 目录内的已知保护标记。不修改或自动解除这些标记。

发行版没有桌面截图、微信界面截图、微信可访问性树采集、全局鼠标或键盘记录钩子。仓库中的隔离界面预览只绘制助手自己的窗口，不连接微信，也不随用户程序安装。

## 2. 何时发送组合键

只有在你亲自核对并保存了微信实际使用的“显示/隐藏窗口”快捷键后，**点击本工具自己的通知**才可能触发一次组合键。它不是通过查看聊天来识别消息，也不注册新的全局快捷键。

触发前会检查当前用户与会话、官方程序身份、你确认的文件版本和指纹、已有进程是否仍有效、当前桌面是否允许输入，以及相关修饰键是否正在被你按住。微信已经在前台时，无需再次发送。它只尝试打开已有微信，不会启动新微信、输入账号密码、登录或导航到某个聊天。

一次尝试不会循环重发组合键，也不会展开隐藏托盘或强制修改微信窗口。失败可能导致冷却或暂停该功能，尤其是版本、文件或身份变化。部分输入失败时仅尝试释放自己插入而尚未释放的按键。

组合键属于系统级输入，仍可能受其他软件的快捷键设置、桌面切换或时序影响。因此，请只保存你已核对的真实绑定；不确定时留空，手动打开微信。保护记录不会因你保存设置而被清除。

## 3. 通知里有什么，谁保存它

通知包含通用的“微信有新提醒”等文字，不包含发件人、联系人名称、正文或聊天预览。测试提醒会明确属于测试。通知携带随机的本地操作标识，用于点击处理与技术关联，不是账号或消息标识。

通知交给 Windows 后，可能留在 Windows 通知中心，受系统设置、清除操作及到期规则影响。本版本普通提醒设置约 60 分钟到期，测试提醒约 30 分钟到期；助手的提交记录不保证用户实际看到了弹窗。

**关闭设置窗口不会退出后台助手。** 在管理区退出后，新关注事件不再由该后台实例观察，但旧系统通知可能仍在。点击旧通知可能让 Windows 短暂启动通知点击处理进程；这不等于重新开启常驻提醒。不能把“退出”理解为立即删除所有系统通知或阻止 Windows 再激活任何处理过程。

## 4. 本地保存什么

主要设置/状态目录：**%LOCALAPPDATA%\WeChatNotify**

| 类别 | 内容与用途 |
|---|---|
| preferences.json | 是否同意启用、是否提醒、可选的微信安装路径。 |
| activation-shortcut.json | 你确认的组合键、是否暂停、绑定的文件版本/指纹、Windows 用户 SID 和确认状态。不是微信账号或登录令牌。 |
| status.json 等有限状态文件 | 最近的运行状态、源阶段、进程数量或进程编号、通知提交/点击标识、时间、错误类型/阶段、打开结果和短时耗时。 |
| startup-error.json 等错误状态 | 某些异常可能包含本机安装路径或其他环境细节，不能假定全部匿名。 |
| assets | 助手自己的图标和从本机官方程序提取的图标缓存。 |
| incident-block.json（若存在） | 长期保留的保护标记，不自动过期。 |

这些通常是**固定文件名覆盖保存的最近状态**，不是连续记录聊天的历史数据库。单次状态读取有大小限制，但不代表整个目录被强制配额限制。请不要公开上传原始配置、SID、绝对路径或原始错误文件。

安装目录：**%LOCALAPPDATA%\Programs\WeChatNotify**，升级暂存使用同级 .stage，上一安装使用 .previous，通常只保留一份上一安装。

为了 Windows 通知、通知点击和可选的登录后运行，工具会在**当前 Windows 用户**下注册自己的通知身份/点击处理入口、开始菜单快捷方式和可选启动项。它不更改微信的程序、账号、聊天文件或系统全局焦点/安全设置。

进程内有短时的失败计数、冷却和合并状态。不同 Windows 用户/会话的本地控制通道有隔离与大小/命令限制；这些是保护措施，不应表述为无限制的安全保证。

## 5. 联网、遥测与外部链接

本工具**没有自有上传、遥测、分析或更新服务器**，没有实现把上述设置、日志或聊天发送到开发者的网络接口，也不会自动检查或下载新版本。

你点击 GitHub 下载/反馈链接或微软帮助链接时，默认浏览器访问相应外站；这些网站与浏览器可能根据各自政策处理 IP、Cookie 等数据。你主动提交到 GitHub 的 Issue 可能公开。应用内的“在线隐私说明 / 在线使用帮助”会在浏览器中打开对应发行版本的 GitHub 页面，需要联网。简短要点在应用内可离线阅读，完整说明也作为文本随包提供。

Windows 通知、证书缓存/系统服务、微信和浏览器本身有自己的系统或网络行为，不受本工具完整控制。“没有自有上传”不等于整台电脑离线。

## 6. 如何停止、清理与卸载

- 不同意首次启用：关闭设置窗口即可退出，不启动关注事件来源。
- 临时暂停：在概览暂停提醒，或停用通知点击打开；不删除设置及旧通知。
- 完全退出后台助手：隐私与帮助 → 管理助手 → 退出助手；已交给 Windows 的旧通知仍可能存在。
- 卸载：在同一管理区选择卸载并确认。脚本先检查安装归属、文件清单与指纹，再正常退出、注销本工具自己的通知与启动注册，删除明确列出的本工具设置/状态文件和已知图标。
- 修改过的发行文件、清单外文件、重定向目录或不安全路径会导致保留/停止，不做无条件递归删除。旧的可回退安装也只在通过检查后删除。卸载不删除微信安装、用户账号或聊天。
- **保护标记、旧版本目录中的保护记录及未知文件会保留**，不会自动解除保护。状态目录若仍有保留文件，也会留下。便携程序目录不由安装卸载脚本删除，需你自行备份、清理。
- 退出不会立即清除旧通知；卸载会注销本工具通知身份并尝试相关清理，不能保证 Windows 缓存或历史立即全部消失。可在通知中心自行清除。

升级先验证与暂存，再切换目录，保留一个可回退安装；回退不恢复或删除微信数据，也不清除保护标记。

## 7. 反馈问题时保护隐私

可以提交：Windows 版本、助手版本、微信程序版本（不要账号）、简短复现步骤、预期/实际现象、是否出现无隐私的提示。

不要提交：聊天/联系人截图、消息正文、原始配置或状态目录打包、Windows SID、用户名、个人路径、账号或凭据。若确需技术信息，请先手动检查和脱敏，再决定是否公开。这里没有自动上传诊断或一键复制原始配置功能。

通过 [GitHub Issues](https://github.com/CanstaSolance/wechat-notification-helper/issues) 反馈；提交前自行检查公开范围。版本说明与已验证边界见 [README](README.zh-CN.md) 和 [验证记录](https://github.com/CanstaSolance/wechat-notification-helper/blob/main/docs/VALIDATION.md)。

---

## English summary

Version 0.3.0-rc.1, updated 2026-11-02. This unofficial utility observes public process/window attention metadata, verifies installed program signatures/fingerprints, and checks Windows notification state. It does not read chat text, contacts, databases, credentials, clipboard or login tokens. No screenshot capture or global input-recording hook is shipped in the application.

Settings and bounded latest-state files under **%LOCALAPPDATA%\WeChatNotify** can contain paths, a Windows SID, file fingerprints, process metadata, timestamps and technical errors. They are not necessarily anonymous. Current-user notification/startup registrations and an extracted local icon are also used.

A personally confirmed shortcut can be sent once after a valid helper notification click, subject to exact-image/current-user/live-process and input safety checks. No WeChat launch, login or conversation navigation occurs. Leave it unconfigured if unsure.

Generic notifications have no sender/body. Windows may retain them after exit; an old notification click can briefly start a callback process without resuming monitoring. Uninstall removes known owned settings, registrations and verified package files; unknown/modified files and safety records remain. Uninstallation unregisters the helper identity and attempts related cleanup, but cannot guarantee immediate removal of all Windows notification caches/history. WeChat data is not removed.

There is no proprietary upload, telemetry or update service. Browser links to GitHub/Microsoft, and Windows/WeChat's own network behavior, are separate. Never publish raw configuration, chat screenshots, personal paths or user identifiers in issues. The detailed sections above describe scope, local files, activation and cleanup limits.
