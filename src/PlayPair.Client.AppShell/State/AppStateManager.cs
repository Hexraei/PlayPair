using System;

namespace PlayPair.Client.AppShell.State;

public sealed class AppStateManager
{
    private string? _currentRoomCode;
    private string? _clientId;
    private string? _displayName;
    private SourceRole _currentRole;
    private bool _isConnected;

    public event EventHandler? StateChanged;

    public string? CurrentRoomCode
    {
        get => _currentRoomCode;
        private set
        {
            if (_currentRoomCode != value)
            {
                _currentRoomCode = value;
                StateChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public string? ClientId
    {
        get => _clientId;
        private set
        {
            if (_clientId != value)
            {
                _clientId = value;
                StateChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public string? DisplayName
    {
        get => _displayName;
        private set
        {
            if (_displayName != value)
            {
                _displayName = value;
                StateChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public SourceRole CurrentRole
    {
        get => _currentRole;
        private set
        {
            if (_currentRole != value)
            {
                _currentRole = value;
                StateChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public bool IsConnected
    {
        get => _isConnected;
        private set
        {
            if (_isConnected != value)
            {
                _isConnected = value;
                StateChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public void SetRoom(string roomCode, string clientId, SourceRole role, string displayName)
    {
        CurrentRoomCode = roomCode ?? throw new ArgumentNullException(nameof(roomCode));
        ClientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
        CurrentRole = role;
        DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
    }

    public void UpdateConnectionState(bool isConnected)
    {
        IsConnected = isConnected;
    }

    public void ClearRoom()
    {
        CurrentRoomCode = null;
        ClientId = null;
        DisplayName = null;
        CurrentRole = SourceRole.Guest;
        IsConnected = false;
    }

    public override string ToString() =>
        $"Room={CurrentRoomCode}, ClientId={ClientId}, Role={CurrentRole}, Connected={IsConnected}, DisplayName={DisplayName}";
}

public enum SourceRole
{
    Host = 0,
    Guest = 1
}
