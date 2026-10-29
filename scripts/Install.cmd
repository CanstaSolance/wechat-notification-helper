@echo off
chcp 65001 >nul
if not exist "%~dp0WeChatNotify.exe" (
 echo 请先右键 ZIP，选择“全部解压缩”，再运行 Install.cmd。
 pause
 exit /b 1
)
"%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install.ps1"
if errorlevel 1 pause
