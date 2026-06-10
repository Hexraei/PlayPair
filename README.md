# PlayPair

PlayPair helps you watch videos and listen to music together in sync. It is a Windows app that runs in the background and instantly mirrors your media controls (play, pause, and seek) between two or more computers.

---

## Key Features

* **Instant Sync**: Play, pause, and seek commands are shared instantly using WebSockets (SignalR).
* **Clean Interface**: A simple dark-themed status window that stays out of your way.
* **Runs in the Background**: Minimizes to the system tray so you can keep syncing without cluttering your screen.
* **Easy Connections**: Just copy a 6-character room code and share it with friends.
* **Supports Your Favorite Players**: Automatically detects what you are playing (in Spotify, YouTube via Chrome/Brave, VLC, Plex, etc.) using the Windows Media Session API.

---

## Quick Start Guide

You do not need to install anything. PlayPair is portable and ready to run.

1. **Download the App**: Get the ZIP folder: [PlayPair-Portable.zip](file:///d:/playpair/PlayPair-Portable.zip)
2. **Extract**: Unzip the folder to any location on your computer.
3. **Run**: Double-click `PlayPair.exe` to start the app. It automatically connects to our public relay server.

---

## How to Sync with Friends

### 1. If you are the Host:
1. Open the PlayPair status window.
2. Click **New Room**. This generates a 6-character code (for example, `LH93LL`).
3. Click the copy icon to copy the code.
4. Send the code to your friends.

### 2. If you are a Guest:
1. Open the PlayPair status window.
2. Paste the 6-character code your friend sent into the input box.
3. Click **Join** to sync.

---

## System Tray Controls

PlayPair sits quietly in your Windows system tray:
* **Show Interface**: Double-click the tray icon to open the status window.
* **Hide to Tray**: Click the close button in the app footer to hide the window while keeping synchronization active.
* **Right-click Menu**: Right-click the tray icon to quickly access actions:
  - Create Room
  - Join Room
  - Copy Room Code
  - Reconnect
  - Exit (closes the app completely)

---

## Custom Server Configuration

By default, the app connects to our public server: `https://playpair-server.onrender.com`.

If you want to use a private server:
1. Open the `config.txt` file in your application folder.
2. Change the default URL to your own server URL (for example, `https://my-private-playpair.onrender.com`).
3. Save the file and restart `PlayPair.exe`.

---

## Cloud Deployment (For Hosts & Developers)

If you want to host your own relay server in the cloud for free, we have configured a Render Blueprint (`render.yaml`).

1. Push this codebase to your own GitHub repository.
2. Go to [Render Blueprints](https://dashboard.render.com/blueprints).
3. Connect your repository. Render will automatically configure the Docker-based web service using `render.yaml`.
4. Approve the blueprint. Once deployed, verify that the health check page (for example, `https://your-app.onrender.com/health`) returns `{ "status": "ok" }`.

---

## Local Development Commands

If you want to build or test the code locally:

```bash
# Restore dependencies
dotnet restore PlayPair.sln

# Build the solution in Release mode
dotnet build PlayPair.sln --configuration Release --no-restore

# Run all test suites
dotnet test PlayPair.sln --configuration Release --no-build
```
