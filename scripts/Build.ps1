[CmdletBinding()]
param([string]$OutputDirectory,[switch]$Offline)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$version='0.2.0-rc.1'
if(-not $OutputDirectory){$OutputDirectory=Join-Path $root ('artifacts\release-'+$version)}
$out=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath $out){throw '输出目录已存在；请选择新的目录，不覆盖既有发行证据。'}
$whitelist=Get-Content -LiteralPath (Join-Path $root 'source-files.json') -Raw -Encoding UTF8|ConvertFrom-Json
if($whitelist.Schema -ne 1 -or @($whitelist.Files).Count -lt 10){throw '源码白名单无效。'}
$source=Join-Path $out 'source'
$package=Join-Path $out 'package'
[IO.Directory]::CreateDirectory($source)|Out-Null
[IO.Directory]::CreateDirectory($package)|Out-Null
$sourceHashes=@();$seen=@{}
foreach($relative in $whitelist.Files) {
    if($relative -match '(^/|^[A-Za-z]:|\\|(^|/)\.\.(/|$))' -or $seen.ContainsKey($relative)){throw '源码路径不安全或重复。'}
    $seen[$relative]=$true
    $from=Join-Path $root $relative
    $file=Get-Item -LiteralPath $from
    if($file.PSIsContainer -or ($file.Attributes -band [IO.FileAttributes]::ReparsePoint)){throw '源码白名单文件无效。'}
    if($file.Extension -in @('.exe','.dll','.pfx','.pem','.key','.nupkg')){throw '源码白名单含禁止的二进制或密钥类型。'}
    if($relative -notmatch '\.(png|ico|rtf)$') {
        $text=Get-Content -LiteralPath $from -Raw -Encoding UTF8
        if($text -match '(?i)[A-Z]:[\\/]+Users[\\/]+[\p{L}\d_. -]+[\\/]|S-1-5-21-(\d+-){2,}\d+|(?<![A-F0-9])[A-F0-9]{64}(?![A-F0-9])'){throw ('源码疑似包含本机资料：'+$relative)}
    }
    $to=Join-Path $source $relative
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($to))|Out-Null
    Copy-Item -LiteralPath $from -Destination $to
    $hash=(Get-FileHash -LiteralPath $to -Algorithm SHA256).Hash
    if($hash -ne (Get-FileHash -LiteralPath $from -Algorithm SHA256).Hash){throw '源码复制后不一致。'}
    $sourceHashes+=@{Name=$relative;Sha256=$hash}
}
$restore=@('restore',(Join-Path $root 'tests\ReleaseChecks.csproj'),'--locked-mode')
if($Offline){$restore+=@('--source',(Join-Path $env:USERPROFILE '.nuget\packages'),'--ignore-failed-sources')}
& dotnet @restore
if($LASTEXITCODE -ne 0){throw '锁定依赖还原失败。'}
& dotnet run --project (Join-Path $root 'tests\ReleaseChecks.csproj') -c Release --no-restore
if($LASTEXITCODE -ne 0){throw '隔离检查失败。'}
& (Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe') -NoProfile -ExecutionPolicy Bypass -File (Join-Path $root 'scripts\Check-WindowsPowerShell.ps1')
if($LASTEXITCODE -ne 0){throw 'Windows PowerShell 5.1 脚本检查失败。'}
& dotnet publish (Join-Path $root 'src\WeChatNotify.csproj') -c Release --no-restore -o $package
if($LASTEXITCODE -ne 0){throw '发布构建失败。'}
foreach($name in @('Install.cmd','Install.ps1','Rollback.cmd','Uninstall.ps1')) {
    $from=Join-Path (Join-Path $root 'scripts') $name
    $to=Join-Path $package $name
    if($name.EndsWith('.ps1')){[IO.File]::WriteAllText($to,[IO.File]::ReadAllText($from),[Text.UTF8Encoding]::new($true))}
    else{[IO.File]::WriteAllText($to,([IO.File]::ReadAllText($from).Replace("`r`n","`n").Replace("`n","`r`n")),[Text.UTF8Encoding]::new($false))}
}
foreach($name in @('LICENSE','README.zh-CN.md','THIRD_PARTY_NOTICES.md')){Copy-Item -LiteralPath (Join-Path $root $name) -Destination (Join-Path $package $name)}
Copy-Item -LiteralPath (Join-Path $root 'docs\WINDOWS-SDK-LICENSE.rtf') -Destination (Join-Path $package 'WINDOWS-SDK-LICENSE.rtf')
$files=@(Get-ChildItem -LiteralPath $package -File|Sort-Object Name|ForEach-Object{@{Name=$_.Name;Sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash}})
@{Schema=1;Product='WeChatNotify';Version=$version;Architecture='win-x64';FrameworkDependent=$true;Files=$files}|ConvertTo-Json -Depth 5|Set-Content -LiteralPath (Join-Path $package 'package-files.json') -Encoding UTF8
$binaryZip=Join-Path $out ('WeChatNotify-'+$version+'-win-x64.zip')
$sourceZip=Join-Path $out ('WeChatNotify-'+$version+'-source.zip')
Compress-Archive -Path (Join-Path $package '*') -DestinationPath $binaryZip -CompressionLevel Optimal
Compress-Archive -Path (Join-Path $source '*') -DestinationPath $sourceZip -CompressionLevel Optimal
@($binaryZip,$sourceZip)|ForEach-Object{(Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash+'  '+[IO.Path]::GetFileName($_)}|Set-Content -LiteralPath (Join-Path $out 'SHA256SUMS.txt') -Encoding ASCII
@{Version=$version;SourceFiles=$sourceHashes;PackageFiles=$files;Signed=$false;SelfContained=$false}|ConvertTo-Json -Depth 6|Set-Content -LiteralPath (Join-Path $out 'build-manifest.json') -Encoding UTF8
Get-Item -LiteralPath $binaryZip,$sourceZip|Select-Object Name,Length
