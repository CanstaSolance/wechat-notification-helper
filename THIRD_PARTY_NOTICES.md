# Third-party notices

The helper's original code is MIT, copyright 2026 CanstaSolance. This does not relicense third-party components or Tencent's trademarks/resources.

## CommunityToolkit.WinUI.Notifications 7.1.2

The original NuGet license follows (MIT):
# Windows Community Toolkit

Copyright © .NET Foundation and Contributors

All rights reserved.

## MIT License (MIT)

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the “Software”), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NON-INFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

## Upstream notification/conversation policy design attribution

Conversation UIA parsing and monitoring design reference:
zddiao-max/wechat-message-notifier
https://github.com/zddiao-max/wechat-message-notifier/tree/cc1abd64b2146675586f86c156cc1f92c062c6cc
SessionParser.cs, WeChatMonitor.cs, WindowsNotificationCenter.cs.
The original foreground/restore, first-name-match routing, and protocol registration
implementations are not used. This product retains its own notification identity.

MIT License

Copyright (c) 2026 ZDDIAO-MAX

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

## Windows SDK .NET projection and C#/WinRT support

Binary packages include Microsoft.Windows.SDK.NET.dll and WinRT.Runtime.dll from Microsoft.Windows.SDK.NET.Ref 10.0.19041.57. These are application support assemblies, not the .NET Desktop Runtime or a bundled SDK installation.

Microsoft copyright and terms apply separately. The targeting pack's published license URL is https://aka.ms/WinSDKLicenseURL ; the Microsoft license downloaded from that official redirect is included as WINDOWS-SDK-LICENSE.rtf in binary packages and docs/WINDOWS-SDK-LICENSE.rtf in source. Microsoft redistribution documentation: https://go.microsoft.com/fwlink/?LinkId=524842 . Do not claim these binaries are newly licensed under this project's MIT license. C#/WinRT source/license: https://github.com/microsoft/CsWinRT . Its MIT license is included below.

MIT License

Copyright (c) Microsoft Corporation.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

## Tencent

WeChat/Weixin and their logos belong to Tencent. No Tencent executable, DLL, extracted icon or chat/account data is distributed in these packages. A verified local installation may supply its icon at runtime for local decoration only; an original bell icon is the fallback. This is an independent, unofficial product.

## Runtime prerequisites

The .NET 10 Desktop Runtime is installed separately by the user from Microsoft under its own terms. It is not included in this framework-dependent ZIP. Development-only SDK/NuGet caches, credentials and local diagnostics are excluded.