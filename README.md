# PlayPair

PlayPair is a simple way to watch videos or listen to music together with friends, perfectly in sync. 

Whether you are watching a movie on VLC or Plex, or listening to a playlist on Spotify or YouTube, PlayPair runs quietly in the background on Windows and instantly shares your playback controls (play, pause, and seek) between two or more computers. When you hit pause, it pauses for them too. When they skip ahead, your player skips to the exact same second.

---

## What PlayPair Does

* **Instant Control Sharing**: Play, pause, or skip forward/backward in your media player, and everyone else's player responds immediately. It uses SignalR (WebSockets) behind the scenes to sync controls in real time.
* **Zero Setup or Account Needed**: You don't need to sign up, create an account, or share personal info. Just generate a quick 6-character room code, share it with your friend, and start watching.
* **Background Syncing**: The app minimizes to your system tray and stays out of your screen's way, so you can focus on your video or music.
* **Automatic Media Detection**: You don't have to configure individual apps. PlayPair automatically detects what you're playing (Spotify, Plex, VLC, or YouTube in Chrome/Brave) using the standard Windows Media Session API.

---

## How to Get Started

PlayPair is portable, which means you don't need to install anything. Just download and run it.

1. **Download the App**: Grab the ZIP folder here: [PlayPair-Portable.zip](file:///d:/playpair/PlayPair-Portable.zip)
2. **Extract**: Unzip the folder to any folder on your computer.
3. **Run**: Double-click `PlayPair.exe`. It will open and connect automatically to our free shared server.

---

## How to Sync with Friends

### 1. If you are starting the session (The Host):
1. Open the PlayPair status window.
2. Click **New Room**. This generates a simple 6-character code (like `LH93LL`).
3. Click the copy icon to copy the code.
4. Send the code to whoever is watching or listening with you.

### 2. If you are joining a session (The Guest):
1. Open your PlayPair status window.
2. Paste the 6-character code your friend sent you into the text box.
3. Click **Join** to connect your controls.

---

## Running in the System Tray

Once connected, you can hide the interface so it doesn't block your screen:
* **Minimize**: Click the close button in the app footer to hide the window. PlayPair will keep syncing in the background.
* **Bring Back**: Double-click the PlayPair icon in your Windows system tray.
* **Right-click Actions**: Right-click the tray icon to quickly:
  - Create a new room
  - Join an existing room
  - Copy your current room code
  - Reconnect if your connection drops
  - Exit the app completely

---

## Running Your Own Server

By default, PlayPair uses our public sharing server (`https://playpair-server.onrender.com`). If you prefer to host your own private server:

1. Open the `config.txt` file in your PlayPair folder.
2. Replace our default server URL with your own server URL (for example, `https://my-private-server.onrender.com`).
3. Save the file and restart `PlayPair.exe`.

### Cloud Deployment (Render Blueprint)
If you want to host your own server for free on Render:
1. Push this codebase to your own GitHub repository.
2. Go to [Render Blueprints](https://dashboard.render.com/blueprints).
3. Connect your repository. Render will automatically read the `render.yaml` file and set up a Docker-based web service.
4. Approve the setup. Once it is deployed, you can verify it works by visiting `https://your-app.onrender.com/health` (it should display `{ "status": "ok" }`).

---

## Developing Locally

If you want to view, build, or modify the code yourself:

```bash
# Restore project dependencies
dotnet restore PlayPair.sln

# Build the app in Release mode
dotnet build PlayPair.sln --configuration Release --no-restore

# Run the automated test suites
dotnet test PlayPair.sln --configuration Release --no-build
```
