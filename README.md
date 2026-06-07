# PlayPair 🎵

PlayPair is a sleek, lightweight Windows tray application that allows you and your friends to synchronize media playback (play, pause, seek, and track details) in real-time. 

---

## ✨ Key Features

* **Real-time Sync**: High-performance, low-latency SignalR synchronization engine.
* **Premium Dark Card UI**: Collapsible overlay window styled with high-contrast, modern typography and clean states.
* **System Tray Integration**: Stays active in the background. Minimize to the tray by clicking the Close button, and control room states using the right-click context menu.
* **Smart Validation**: Simple 6-character room codes with instant clipboard copying and uppercase validation.
* **Media Tracking**: Automatically detects and displays your active playing media session (e.g. tracks playing in Brave, Chrome, Spotify) to keep rooms aligned.

---

## 🚀 Quick Start Guide

You do not need to install anything! PlayPair is distributed as a zero-configuration portable package.

1. **Download the App**: Get the portable release archive: [PlayPair-Portable.zip](file:///d:/playpair/PlayPair-Portable.zip)
2. **Extract**: Extract the contents of the ZIP folder to a directory of your choice.
3. **Run**: Double-click `PlayPair.exe` to launch the application. It will automatically connect to the default public relay server.

---

## 👥 How to Sync with Friends

### 1. If you are the Host:
1. Open the PlayPair overlay window.
2. Click **New Room**. A unique 6-character room code (e.g. `LH93LL`) will be generated.
3. Click the copy icon (📋) to copy the code to your clipboard.
4. Send the code to your friends!

### 2. If you are the Guest:
1. Open the PlayPair overlay window.
2. Enter the 6-character code your friend sent into the text input box.
3. Click **Join**. You are now synced!

---

## ⚙️ System Tray Controls

PlayPair runs quietly in the Windows system tray:
* **Show UI**: Double-click the tray icon (or select any action) to open the main status card.
* **Close to Tray**: Clicking the **Close** button in the app footer will hide the window to the tray while keeping synchronization active.
* **Quick Menu Actions**: Right-click the tray icon to quickly access actions:
  - Create Room
  - Join Room
  - Copy Room Code
  - Reconnect
  - Exit (select this to shut down the application completely)

---

## 🌐 Custom Server Configuration

By default, the application connects to our public host: `https://playpair-server.onrender.com`.

If you or your friends host a private server, you can point the client to it:
1. Open the `config.txt` file in your application folder.
2. Replace the default URL with your custom server URL (e.g., `https://my-private-playpair.onrender.com`).
3. Save the file and restart `PlayPair.exe`.

---

## 🛠️ Cloud Deployment (For Hosts & Developers)

We have configured a Render Blueprint (`render.yaml`) to allow you to host your own SignalR relay server in the cloud for free.

1. Push this codebase to your own GitHub repository.
2. Navigate to [Render Blueprints](https://dashboard.render.com/blueprints).
3. Connect your repository. Render will automatically parse [render.yaml](file:///d:/playpair/render.yaml) and configure the dockerized Web Service.
4. Approve the blueprint. Once deployed, verify that your server health check (e.g. `https://your-app.onrender.com/health`) returns `{ "status": "ok" }`.

---

## 💻 Local Development Commands

If you want to build or test the code locally:

```bash
# Restore dependencies
dotnet restore PlayPair.sln

# Build the solution in Release mode
dotnet build PlayPair.sln --configuration Release --no-restore

# Run all test suites
dotnet test PlayPair.sln --configuration Release --no-build
```
