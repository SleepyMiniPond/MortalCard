@echo off
chcp 65001 >nul
setlocal
cd /d "%~dp0"
where node >nul 2>nul
if errorlevel 1 (
  echo 找不到 Node.js。請安裝 Node.js 後重新啟動閱讀器。
  pause
  exit /b 1
)
node "Tools\ProjectBoard\start.mjs"
if errorlevel 1 pause
endlocal
