# 🎛️ Fader

**Fader** is a modern, lightweight Windows application designed for automatic low-latency audio ducking. It automatically lowers the volume of background apps (like Spotify or media players) whenever audio plays in a trigger application (like Google Chrome, YouTube, or Discord), and smoothly fades your background music back in the instant you pause or finish watching.

![Windows 11](https://img.shields.io/badge/OS-Windows%2010%20%2F%2011-0078D4?logo=windows)
![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)
![WinUI 3](https://img.shields.io/badge/UI-WinUI%203-0078D4)

---

## ✨ Features

- ⚡ **Instant Low-Latency Restore:** Direct WASAPI peak volume polling (100ms interval) means background music returns instantly when a video ends—no waiting on browser session delays.
- 🎚️ **Automatic Audio Ducking:** Lower background music automatically when videos, streams, or voice calls start.
- 🎨 **Modern Windows 11 Design:** Sleek dark UI with Mica translucent backdrop, custom typography (*Miracle Days* font), and custom slider logo.
- 📌 **System Tray & Startup Integration:** Runs quietly in the notification area and supports optional launch on Windows startup.
- ⚙️ **Fully Customizable:** Adjust ducking percentage, fade animation duration, restore delay, and playback sensitivity directly in Settings.

---

## 🚀 Quick Start & Installation

### Prerequisites
- **Windows 10 (version 19041+)** or **Windows 11**
- [.NET 10 SDK](https://dotnet.microsoft.com/download) (or Windows App SDK runtime)

### Installation & Running

1. **Clone the Repository:**
   ```bash
   git clone https://github.com/UssamaRei/fader-app.git
   cd fader-app
   ```

2. **Run the Application:**
   ```bash
   dotnet run --project src/Fader/Fader.csproj -p:Platform=x64
   ```

3. *(Optional)* **Build Executable for Direct Launch:**
   ```bash
   dotnet build src/Fader/Fader.csproj -c Release -p:Platform=x64
   ```
   You can find `Fader.exe` ready to run in `src/Fader/bin/x64/Release/net10.0-windows10.0.22621.0/`.

---

## 📖 How to Use Fader

1. **Launch Fader:** Open the app. It will automatically detect all active Windows audio sessions.
2. **Assign App Roles:**
   - **Trigger Apps:** Set applications that produce audio you want to listen to (e.g., *Google Chrome*, *YouTube*, *Discord*).
   - **Background Apps:** Set applications that should lower their volume when a trigger app plays (e.g., *Spotify*, *Music Player*).
3. **Customize Settings:** Click the **Settings** gear icon in the top header to configure:
   - **Duck Volume:** Target volume percentage for background apps when ducked (default: `20%`).
   - **Fade Duration:** Duration of the smooth volume transition (default: `350ms`).
   - **Restore Delay:** Time to wait after trigger sound stops before restoring music (default: `800ms`).
   - **Launch on Startup:** Automatically launch Fader when you log into Windows.

---

## 🛠️ Tech Stack

- **Framework:** WinUI 3 (Windows App SDK)
- **Audio Core:** Windows Audio Session API (WASAPI) via [NAudio](https://github.com/naudio/NAudio)
- **Architecture:** MVVM Pattern with `CommunityToolkit.Mvvm`
- **Tray Support:** `H.NotifyIcon.WinUI`

---

## 📄 License

Personal & Educational Use.
