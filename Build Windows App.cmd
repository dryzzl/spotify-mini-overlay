@echo off
setlocal
cd /d "%~dp0"
set "COMPILER=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%COMPILER%" set "COMPILER=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%COMPILER%" (
  echo Could not find the Microsoft .NET Framework C# compiler.
  echo This project needs the .NET Framework 4.x compiler installed with Windows.
  echo You can also download a GitHub Actions build artifact.
  pause
  exit /b 1
)
"%COMPILER%" /nologo /optimize+ /target:winexe /out:"%~dp0SpotifyMiniOverlay.exe" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll "%~dp0SpotifyMiniOverlay.cs"
if errorlevel 1 (
  echo.
  echo Build failed. Review the compiler error above.
  pause
  exit /b 1
)
echo Built SpotifyMiniOverlay.exe successfully.
exit /b 0
