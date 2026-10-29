$ErrorActionPreference='Stop'
foreach($name in @('Install.ps1','Uninstall.ps1')) {
 $path=Join-Path $PSScriptRoot $name
 $errors=$null
 [Management.Automation.Language.Parser]::ParseFile($path,[ref]$null,[ref]$errors)|Out-Null
 if($errors){throw ($errors.Message -join '; ')}
 $bytes=[IO.File]::ReadAllBytes($path)
 if($bytes[0] -ne 239 -or $bytes[1] -ne 187 -or $bytes[2] -ne 191){throw 'Expected UTF8 BOM'}
 $decoded=Get-Content -LiteralPath $path -Raw
 if($name -eq 'Install.ps1' -and -not $decoded.Contains('安装未完成')){throw 'Chinese decoding failed'}
}
[pscustomobject]@{PowerShell=$PSVersionTable.PSVersion.ToString();BOM=$true;ChineseDecoded=$true;Parsed=$true}|ConvertTo-Json