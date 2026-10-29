[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$AppPath,[switch]$Confirmed)
$ErrorActionPreference='Stop'
try {
if(-not $Confirmed){throw '请从助手设置中确认卸载。'}
$exe=[IO.Path]::GetFullPath($AppPath)
$root=[IO.Path]::GetDirectoryName($exe)
$expected=Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'Programs\WeChatNotify'
if([IO.Path]::GetFileName($exe) -ne 'WeChatNotify.exe' -or $root -ine $PSScriptRoot){throw '卸载目标与脚本位置不一致。'}
for($dir=$root;$dir;$dir=[IO.Path]::GetDirectoryName($dir)){if((Get-Item -Force -LiteralPath $dir).Attributes -band [IO.FileAttributes]::ReparsePoint){throw '卸载路径不允许重定向。'}}
function Preflight-Package([string]$directory) {
    if(-not(Test-Path -LiteralPath $directory)){return $null}
    if($directory -ine $expected -and $directory -ine ($expected+'.previous')){throw '不删除便携目录，请手动删除解压文件夹。'}
    if((Get-Item -Force -LiteralPath $directory).Attributes -band [IO.FileAttributes]::ReparsePoint){throw '安装目录不允许重定向。'}
    $manifestPath=Join-Path $directory 'package-files.json'
    $manifestFile=Get-Item -Force -LiteralPath $manifestPath
    if($manifestFile.PSIsContainer -or $manifestFile.Length -gt 65536 -or ($manifestFile.Attributes -band [IO.FileAttributes]::ReparsePoint)){throw '发行清单不安全。'}
    $manifest=Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8|ConvertFrom-Json
    if($manifest.Product -ne 'WeChatNotify' -or $manifest.Schema -ne 1){throw '发行包归属验证失败。'}
    if(@($manifest.Files).Count -lt 1 -or @($manifest.Files).Count -gt 128){throw '发行文件数量无效。'}
    $seen=@{}
    foreach($file in $manifest.Files) {
        if($file.Name -notmatch '^[A-Za-z0-9][A-Za-z0-9_.-]*$' -or $file.Name -eq 'package-files.json' -or $seen.ContainsKey($file.Name)){throw '清单文件名不安全或重复，原文件已保留。'}
        $seen[$file.Name]=$true
        $path=Join-Path $directory $file.Name
        $item=Get-Item -Force -LiteralPath $path
        if($item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint)){throw '文件包含目录或重定向，原文件已保留。'}
        if((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $file.Sha256){throw ('文件已修改，未删除：'+$file.Name)}
    }
    if(-not $seen.ContainsKey('WeChatNotify.exe')){throw '清单没有本工具程序，原文件已保留。'}
    foreach($item in Get-ChildItem -LiteralPath $directory -Force){if($item.PSIsContainer -or ($item.Name -ne 'package-files.json' -and -not $seen.ContainsKey($item.Name))){throw '发现清单外文件，目录已保留。请先备份这些文件，再重试卸载。'}}
    return $manifest
}
# Complete package validation happens before stopping, unregistering or deleting anything.
$previousPath=$expected+'.previous'
if($root -ieq $expected){foreach($directory in @($expected,$previousPath)){$null=Preflight-Package $directory}}
$request=Start-Process -FilePath $exe -ArgumentList '--quit' -WindowStyle Hidden -PassThru
if(-not $request.WaitForExit(5000)){throw '正常退出请求未完成。'}
foreach($p in Get-Process -Name WeChatNotify -ErrorAction SilentlyContinue){if($p.Path -ieq $exe -and -not $p.WaitForExit(5000)){throw '助手仍在运行，未删除文件。'}}
$unregister=Start-Process -FilePath $exe -ArgumentList '--unregister' -WindowStyle Hidden -PassThru
if(-not $unregister.WaitForExit(8000) -or $unregister.ExitCode -ne 0){throw '通知/启动项注销未通过，未删除文件。'}
$state=Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'WeChatNotify'
if(Test-Path -LiteralPath $state) {
    if((Get-Item -Force -LiteralPath $state).Attributes -band [IO.FileAttributes]::ReparsePoint){throw '状态目录不允许重定向。'}
    # Preserve incident blocks even on uninstall; they must never silently expire.
    $names=@('preferences.json','activation-shortcut.json','status.json','identity.json','activation.json','callback.json','cold-callback.json','delivery.json','notification-error.json','startup-error.json','fatal-error.json','uninstall-error.json','ipc-error.json','source-cleanup.json')
    foreach($name in $names){foreach($suffix in @('','.tmp')){$path=Join-Path $state ($name+$suffix);if(Test-Path -LiteralPath $path){Remove-Item -LiteralPath $path}}}
    $assets=Join-Path $state 'assets'
    if((Test-Path -LiteralPath $assets) -and -not((Get-Item -Force -LiteralPath $assets).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
        foreach($file in Get-ChildItem -LiteralPath $assets -File){if($file.Name -eq 'helper.png' -or $file.Name -match '^wechat-[A-F0-9]{16}\.png$'){Remove-Item -LiteralPath $file.FullName}}
        if(@(Get-ChildItem -LiteralPath $assets -Force).Count -eq 0){Remove-Item -LiteralPath $assets}
    }
    if(@(Get-ChildItem -LiteralPath $state -Force).Count -eq 0){Remove-Item -LiteralPath $state}
}
function Remove-Files([string]$directory) {
    if(-not(Test-Path -LiteralPath $directory)){return}
    $manifest=Preflight-Package $directory
    foreach($file in $manifest.Files){$path=Join-Path $directory $file.Name;if(Test-Path -LiteralPath $path){Remove-Item -LiteralPath $path}}
    Remove-Item -LiteralPath (Join-Path $directory 'package-files.json')
    if(@(Get-ChildItem -LiteralPath $directory -Force).Count -eq 0){Remove-Item -LiteralPath $directory}
}
if($root -ieq $expected){Remove-Files ($expected+'.previous');Remove-Files $expected}
} catch {
    Add-Type -AssemblyName System.Windows.Forms
    [Windows.Forms.MessageBox]::Show(('卸载未完成：'+$_.Exception.Message),'微信提醒助手')|Out-Null
    exit 1
}
