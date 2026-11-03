# 微信提醒助手 · 三步开始

版本 v0.3.0，独立、非官方工具。需要 Windows x64 与 .NET 10 Desktop Runtime x64。可在同一个程序的“设置 → Language / 语言”中选择中文或 English，选择后立即保存；已有用户默认保留中文。

1. 全部解压发行包。缺少运行环境时，从 https://dotnet.microsoft.com/en-us/download/dotnet/10.0 安装 Desktop Runtime 10（x64）。
2. 双击 Install.cmd，进入“设置”，阅读并同意后保存。首次使用不带开发者的快捷键设置。
3. 到“提醒”发送测试通知，并检查 Windows 通知中心。测试只确认通知通道，不证明每条微信消息都会提醒。

它接收通用关注提醒，连续提醒可能合并，部分消息可能没有提示；通知不展示发件人和正文。微信在前台、Windows 勿扰或通知关闭时可能不弹出。

可选点击打开：在微信“设置 → 快捷键”核对实际“显示/隐藏窗口”组合键，亲自确认后保存。没有这项或不确定时留空。微信必须已运行；不会代启动、登录或进入某个聊天。微信更新后需重新核对。

关闭窗口后，已启用的助手继续在后台。再次打开 WeChatNotify.exe 或开始菜单“微信提醒助手”回到同一实例。暂停在“提醒”；退出和卸载在“帮助 → 管理助手”。旧通知可能在退出后仍留在 Windows 通知中心，点击时可能短暂启动处理过程。

更新：到 https://github.com/CanstaSolance/wechat-notification-helper/releases 下载新包，解压后运行 Install.cmd。工具不会自动下载更新；安装器先验证、暂存，再切换，保留一个上一安装。Rollback.cmd 可切回保留的版本（若存在）。旧的 0.2.0-rc.2 仍可从 Releases 下载。

卸载仅删除本工具自己的已知注册、设置与经校验的安装文件。不删除微信或聊天。安全保护和未知/修改过的文件会保留，卸载可能因此停止。便携程序目录需自行清理。

安装：%LOCALAPPDATA%\Programs\WeChatNotify
设置：%LOCALAPPDATA%\WeChatNotify

既有通知行为只在一台 Windows 11 x64、微信 4.1.15.56 上验证。其他官方签名版本实验性支持。本次界面更新不代表新的真实来信验收。

详细隐私：[随包 PRIVACY.md](PRIVACY.md)
完整图文帮助：https://github.com/CanstaSolance/wechat-notification-helper/blob/main/README.md
问题反馈：https://github.com/CanstaSolance/wechat-notification-helper/issues

请不要上传聊天截图、联系人信息、原始配置、SID 或个人路径。技术状态不是完全匿名的。
