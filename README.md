# Spotify Mini Overlay

A tiny, transparent, always-on-top Windows desktop controller for Spotify. Keep Spotify minimized and use three floating buttons:

- **Previous track**
- **Play / pause**
- **Next track**

No Spotify login, developer account, API key, browser extension, Python runtime, or third-party package is required. It uses the built-in Windows media keys.

## Run it on Windows 10 or 11

1. Download and extract the project ZIP.
2. Double-click **`Start Overlay.cmd`**.
3. On the first run, Windows compiles a small native **`SpotifyMiniOverlay.exe`** using the .NET Framework compiler included with standard Windows installations.
4. From then on, launch **`SpotifyMiniOverlay.exe`** directly, or keep using **`Start Overlay.cmd`**.
5. Start a song in Spotify and minimize Spotify.

If the build script cannot find the Windows .NET Framework compiler, it displays an explanation. You can also download the prebuilt Windows executable from the GitHub Actions build artifact after the project is published.

## Controls

| Action | How |
| --- | --- |
| Previous song | Click the left arrow |
| Play or pause | Click the round center button |
| Next song | Click the right arrow |
| Move anywhere | Drag the dotted handle on the far left |
| Alternative move | Hold Shift and drag a control |
| Reset position | Right-click the overlay and choose **Reset position** |
| Exit | Right-click and choose **Exit overlay**, or use the tray icon |

The last position is saved in `%LOCALAPPDATA%\SpotifyMiniOverlay\position.txt`. If a monitor is disconnected, the overlay returns to the primary monitor.

## Important behavior

- Windows sends media-key commands to the media session currently handling playback. This usually means Spotify when it is playing, but another media app can receive the commands if it is active.
- The center icon estimates playing/paused state from clicks in **this overlay**. If you pause or resume directly in Spotify or with your keyboard, its icon may temporarily be out of sync. Clicking still sends the correct toggle command.
- The overlay remains over normal desktop apps. Windows secure screens and some exclusive fullscreen applications can appear above it.
- Windows only: this is a desktop companion overlay, not a Spotify modification or browser extension.

## Build manually

Run **`Build Windows App.cmd`**. The build produces `SpotifyMiniOverlay.exe` next to the source file.

The app is written in C# with .NET Framework Windows Forms and calls the Windows media-key API. No network access, credentials, installer, or background service is used.

## GitHub automated build

The `.github/workflows/build-windows.yml` workflow builds the executable on a Windows GitHub Actions runner and uploads it as a downloadable build artifact. This workflow compiles the source but does not claim to test playback on a user's computer.
