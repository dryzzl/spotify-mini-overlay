@echo off
setlocal
if not exist "%~dp0SpotifyMiniOverlay.exe" (
  call "%~dp0Build Windows App.cmd"
  if errorlevel 1 exit /b 1
)
start "" "%~dp0SpotifyMiniOverlay.exe"
exit /b 0
