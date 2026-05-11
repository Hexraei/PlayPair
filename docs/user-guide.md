# PlayPair User Guide

## What is PlayPair?

PlayPair is a Windows desktop application that allows two users to **watch media together in sync**. One person acts as the host, and another joins as a guest. When the host plays, pauses, or seeks a video, the guest's player automatically follows along in real-time.

### Key Features

- **Real-time synchronization**: Play/Pause/Seek commands are instantly relayed between host and guest
- **Windows Media Session integration**: Works with any media player that supports Windows Media Session (e.g., Spotify, Movies & TV, web browsers)
- **Tray background app**: Runs in the Windows system tray for minimal UI intrusion
- **Low latency**: Event-driven sync with sub-second relay time
- **Host-authoritative model**: The host's player is the source of truth

---

## Getting Started

### Prerequisites

- Windows 10 or later
- .NET Runtime 8.0+
- Internet connection for connecting to the relay server

### Installation

1. Download the PlayPair installer from the latest release
2. Run the installer (MSIX or Inno Setup)
3. The app will register itself as a Windows background task and appear in the system tray

### Launching the App

- On first install, PlayPair starts automatically in the background
- Look for the PlayPair icon in your Windows system tray (bottom-right corner)
- Click the icon to bring up the overlay menu

---

## Usage Workflow

### Step 1: Host Creates a Room

1. Click the PlayPair tray icon
2. Select **"Create Room"** from the overlay menu
3. A room code is generated and displayed (e.g., `ABC123`)
4. Share the room code with your guest via chat, email, or call

### Step 2: Guest Joins the Room

1. Click the PlayPair tray icon on the guest machine
2. Select **"Join Room"**
3. Enter the room code provided by the host
4. Click **"Connect"**
5. Confirm that you are connected — the overlay will show "Connected to [room code]"

### Step 3: Start Playing Media

1. **Host**: Open any media player (Spotify, Windows Movies & TV, browser, etc.)
2. **Host**: Start playing media (song, podcast, movie)
3. **Guest**: Open the same media in your player and seek to approximately the same position (or wait for auto-sync)
4. **Host**: The host's play/pause/seek commands will be sent to the guest in real-time

### Step 4: Enjoy Synchronized Playback

- When the host **plays**, the guest's player automatically plays
- When the host **pauses**, the guest's player automatically pauses
- When the host **seeks** (jumps to a different time), the guest's player automatically seeks to the same position
- The sync works across different media players and platforms (e.g., host on Spotify, guest on YouTube)

---

## Supported Media Players

PlayPair integrates with any Windows media player that supports the **Windows Media Session API**, including:

- Spotify
- Apple Music
- YouTube (in browser)
- Windows Movies & TV
- VLC Media Player
- Plex
- Web browsers (via media controls)
- Most third-party media players

---

## Disconnecting and Leaving

### Guest Leaves

1. Click the PlayPair tray icon
2. Select **"Leave Room"**
3. The connection is closed; the host will continue playing

### Host Leaves

1. Click the PlayPair tray icon
2. Select **"Leave Room"**
3. The room is closed and the guest is disconnected
4. If the guest was promoting to host, they become the new host and can invite others

---

## Troubleshooting

### Connection Failed

**Problem**: "Failed to connect to relay server"

**Solutions**:
- Check your internet connection
- Verify the room code is correct
- Ensure the relay server is running and accessible
- Check Windows firewall rules for PlayPair

### No Synchronization

**Problem**: Guest's player doesn't react to host's commands

**Solutions**:
- Verify both players support Windows Media Session API
- Restart both PlayPair clients
- Check that the connection status shows "Connected"
- Ensure both players are running

### Stuck or Out of Sync

**Problem**: Players are misaligned or playback is stuck

**Solutions**:
- Manually seek both players to the same position
- Click **"Request Resync"** from the overlay menu to force a full state reconciliation
- Disconnect and reconnect to the room

### High Latency / Delays

**Problem**: Sync feels slow or laggy

**Solutions**:
- Check your internet connection quality
- Reduce network congestion (close other apps using bandwidth)
- Restart the relay server if hosted locally
- Check server logs for error patterns (see Deployment Guide)

---

## Privacy & Data

PlayPair collects minimal data:

- **Room codes and session events** are ephemeral and cleared when the room closes
- **Media player state** (play/pause/position) is only transmitted between host and guest
- **No personal media content** is stored or logged
- **No tracking or telemetry** is enabled by default

---

## Advanced Features

### Reconnection & Recovery

If the connection is lost:

1. PlayPair automatically attempts to reconnect every 3 seconds for up to 30 seconds
2. On reconnection, the current player state is synced from the server
3. Duplicate commands are filtered to prevent double-plays

### Idempotency

- Commands are tagged with IDs to detect and ignore duplicates
- This ensures that network retries don't cause unexpected behavior (e.g., pausing twice in a row)

### Host Authority Model

- The host's player state is the source of truth
- Guest commands are rejected to prevent out-of-order playback
- Only the host can issue PLAY/PAUSE/SEEK commands

---

## Reporting Issues

If you encounter a bug or have feedback:

1. Check the PlayPair logs (see Troubleshooting)
2. Note the room code, player names, and exact steps to reproduce
3. Submit a bug report via GitHub Issues on the PlayPair repository

---

## Support & Documentation

- **Technical Architecture**: See `docs/architecture.md`
- **Deployment & Operations**: See `docs/release-and-ops.md`
- **Release Notes**: See `docs/release-notes-v0.1.0.md`

---

**PlayPair v0.1.0** — Watch together, stay in sync.
