[CmdletBinding()]
param([switch]$Rollback)
$ErrorActionPreference='Stop'
$install=Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'Programs\WeChatNotify'
$previous=$install+'.previous'
$stage=$install+'.stage'
function Assert-NativeDirectory([string]$directory) {
    Add-Type -TypeDefinition @'
using System;using System.Text;using System.Runtime.InteropServices;using Microsoft.Win32.SafeHandles;
public static class InstallContext { [DllImport("kernel32.dll",CharSet=CharSet.Unicode)] public static extern uint GetFinalPathNameByHandle(SafeFileHandle h,StringBuilder p,uint n,uint f); }
'@
    [IO.Directory]::CreateDirectory($directory)|Out-Null
    $probe=Join-Path $directory 'wechatnotify-context.tmp'
    $file=[IO.FileStream]::new($probe,[IO.FileMode]::CreateNew,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None,1,[IO.FileOptions]::DeleteOnClose)
    try { $name=[Text.StringBuilder]::new(32768); $length=[InstallContext]::GetFinalPathNameByHandle($file.SafeFileHandle,$name,32768,0)
        if($length -eq 0 -or $name.ToString() -ine ('\\?\'+$probe)){throw '当前程序正在使用隔离目录。请从资源管理器运行 Install.cmd。'}
    } finally {$file.Dispose()}
}
function Assert-Safe([string]$path) {
    $full=[IO.Path]::GetFullPath($path)
    if($full -ine $install -and $full -ine $previous -and $full -ine $stage){throw '目标不是本工具的固定安装目录。'}
    for($dir=$full;$dir;$dir=[IO.Path]::GetDirectoryName($dir)) {
        if((Test-Path -LiteralPath $dir) -and ((Get-Item -Force -LiteralPath $dir).Attributes -band [IO.FileAttributes]::ReparsePoint)){throw '安装目录不允许重定向。'}
    }
}
function Read-Package([string]$directory) {
    $manifestPath=Join-Path $directory 'package-files.json'
    $manifestFile=Get-Item -Force -LiteralPath $manifestPath
    if($manifestFile.PSIsContainer -or $manifestFile.Length -gt 65536 -or ($manifestFile.Attributes -band [IO.FileAttributes]::ReparsePoint)){throw '发行包清单不安全。'}
    $manifest=Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8|ConvertFrom-Json
    if($manifest.Product -ne 'WeChatNotify' -or $manifest.Schema -ne 1){throw '发行包清单不匹配。'}
    if(@($manifest.Files).Count -lt 1 -or @($manifest.Files).Count -gt 128){throw '发行文件数量无效。'}
    $seen=@{}
    foreach($file in $manifest.Files) {
        if($file.Name -notmatch '^[A-Za-z0-9][A-Za-z0-9_.-]*$' -or $file.Name -eq 'package-files.json' -or $seen.ContainsKey($file.Name)){throw '发行包包含不安全或重复文件名。'}
        $seen[$file.Name]=$true
        $path=Join-Path $directory $file.Name
        $item=Get-Item -Force -LiteralPath $path
        if($item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint)){throw '发行包文件不允许目录或重定向。'}
        if((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $file.Sha256){throw ('文件校验失败：'+$file.Name)}
    }
    if(-not $seen.ContainsKey('WeChatNotify.exe')){throw '发行包缺少程序。'}
    return $manifest
}
function Remove-OwnedPackage([string]$directory) {
    Assert-Safe $directory
    if(-not(Test-Path -LiteralPath $directory)){return}
    $manifest=Read-Package $directory
    $allowed=@($manifest.Files.Name)+@('package-files.json')
    foreach($item in Get-ChildItem -LiteralPath $directory -Force) {if($item.PSIsContainer -or $item.Name -notin $allowed){throw '旧目录包含非发行包文件，已保留，请先移出后重试。'}}
    foreach($name in $allowed){Remove-Item -LiteralPath (Join-Path $directory $name)}
    Remove-Item -LiteralPath $directory
}
function Stop-Own {
    $exe=Join-Path $install 'WeChatNotify.exe'
    if(-not(Test-Path -LiteralPath $exe)){return}
    $request=Start-Process -FilePath $exe -ArgumentList '--quit' -WindowStyle Hidden -PassThru
    if(-not $request.WaitForExit(5000)){throw '退出请求未完成，安装已停止。'}
    foreach($process in Get-Process -Name WeChatNotify -ErrorAction SilentlyContinue) {
        if($process.Path -ieq $exe -and -not $process.WaitForExit(5000)){throw '助手没有正常退出，请从设置退出后重试。'}
    }
}
try {
    if(-not [Environment]::Is64BitOperatingSystem){throw '此发行包仅支持 x64 Windows。'}
    $dotnet=Join-Path $env:ProgramFiles 'dotnet\dotnet.exe'
    if(-not(Test-Path -LiteralPath $dotnet) -or -not((& $dotnet --list-runtimes) -match '^Microsoft.WindowsDesktop.App 10\.')){throw '请先安装微软 .NET 10 Desktop Runtime（x64），然后重新运行。下载说明见 README.zh-CN.md。'}
    Assert-NativeDirectory ([IO.Path]::GetDirectoryName($install))
    foreach($dir in @($install,$previous,$stage)){Assert-Safe $dir}
    if($Rollback) {
        $null=Read-Package $previous
        Stop-Own
        if(Test-Path -LiteralPath $stage){Remove-OwnedPackage $stage}
        if(Test-Path -LiteralPath $install){Move-Item -LiteralPath $install -Destination $stage}
        try {Move-Item -LiteralPath $previous -Destination $install;Move-Item -LiteralPath $stage -Destination $previous}
        catch {if(-not(Test-Path -LiteralPath $install) -and (Test-Path -LiteralPath $stage)){Move-Item -LiteralPath $stage -Destination $install};throw}
    } else {
        $manifest=Read-Package $PSScriptRoot
        if(Test-Path -LiteralPath $stage){Remove-OwnedPackage $stage}
        [IO.Directory]::CreateDirectory($stage)|Out-Null
        foreach($file in $manifest.Files){Copy-Item -LiteralPath (Join-Path $PSScriptRoot $file.Name) -Destination (Join-Path $stage $file.Name)}
        Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'package-files.json') -Destination (Join-Path $stage 'package-files.json')
        $null=Read-Package $stage
        if(Test-Path -LiteralPath $install){$null=Read-Package $install}
        Stop-Own
        if(Test-Path -LiteralPath $previous){Remove-OwnedPackage $previous}
        $hadOld=Test-Path -LiteralPath $install
        if($hadOld){Move-Item -LiteralPath $install -Destination $previous}
        try{Move-Item -LiteralPath $stage -Destination $install}
        catch{if($hadOld -and -not(Test-Path -LiteralPath $install)){Move-Item -LiteralPath $previous -Destination $install};throw}
    }
    Start-Process -FilePath (Join-Path $install 'WeChatNotify.exe') -ArgumentList '--settings'
    Write-Host '已安装。请在设置中了解行为并启用；默认不创建托盘图标，也不会自动开启登录自启。'
} catch {Write-Host ('安装未完成：'+$_.Exception.Message) -ForegroundColor Red;exit 1}
