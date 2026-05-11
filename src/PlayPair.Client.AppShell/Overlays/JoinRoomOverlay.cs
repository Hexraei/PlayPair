using System;
using System.Drawing;
using System.Windows.Forms;

namespace PlayPair.Client.AppShell.Overlays;

public sealed partial class JoinRoomOverlay : Form
{
    private TextBox? _roomCodeTextBox;
    private Button? _joinButton;
    private Button? _closeButton;
    private Label? _errorLabel;
    private Label? _instructionLabel;
    private PictureBox? _spinnerPictureBox;
    private bool _isJoining;

    public string? RoomCode => _roomCodeTextBox?.Text?.Trim().ToUpper();

    public JoinRoomOverlay()
    {
        InitializeComponent();
        SetStyle(ControlStyles.DoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
    }

    private void InitializeComponent()
    {
        this.SuspendLayout();

        // Form properties
        this.ClientSize = new Size(400, 250);
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.ShowIcon = false;
        this.ShowInTaskbar = false;
        this.StartPosition = FormStartPosition.CenterScreen;
        this.Text = "PlayPair - Join Room";
        this.TopMost = true;

        // Title label
        var titleLabel = new Label
        {
            Text = "Join Room",
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            Dock = DockStyle.Top,
            Height = 40,
            Padding = new Padding(10),
            TextAlign = ContentAlignment.MiddleLeft
        };

        // Instruction label
        _instructionLabel = new Label
        {
            Text = "Enter the room code from the host:",
            Font = new Font("Segoe UI", 10f),
            Location = new Point(20, 50),
            Size = new Size(360, 25),
            TextAlign = ContentAlignment.MiddleLeft
        };

        // Room code input
        _roomCodeTextBox = new TextBox
        {
            Location = new Point(20, 85),
            Size = new Size(360, 35),
            Font = new Font("Segoe UI", 14f),
            CharacterCasing = CharacterCasing.Upper,
            MaxLength = 6,
            TextAlign = HorizontalAlignment.Center
        };
        _roomCodeTextBox.TextChanged += RoomCodeTextBox_TextChanged;
        _roomCodeTextBox.KeyDown += RoomCodeTextBox_KeyDown;

        // Error label
        _errorLabel = new Label
        {
            Text = "",
            Font = new Font("Segoe UI", 9f),
            Location = new Point(20, 130),
            Size = new Size(360, 40),
            TextAlign = ContentAlignment.TopLeft,
            ForeColor = Color.Red,
            Visible = false
        };

        // Spinner
        _spinnerPictureBox = new PictureBox
        {
            Size = new Size(25, 25),
            Location = new Point(20, 180),
            SizeMode = PictureBoxSizeMode.CenterImage,
            Visible = false
        };
        _spinnerPictureBox.Paint += (s, e) => DrawSpinner(e.Graphics);

        // Join button
        _joinButton = new Button
        {
            Text = "Join",
            Location = new Point(260, 180),
            Size = new Size(120, 35),
            BackColor = SystemColors.Control,
            Enabled = false
        };
        _joinButton.Click += JoinButton_Click;

        // Close button
        _closeButton = new Button
        {
            Text = "Cancel",
            Location = new Point(20, 220),
            Size = new Size(120, 35),
            BackColor = SystemColors.Control
        };
        _closeButton.Click += CloseButton_Click;

        this.Controls.Add(titleLabel);
        this.Controls.Add(_instructionLabel);
        this.Controls.Add(_roomCodeTextBox);
        this.Controls.Add(_errorLabel);
        this.Controls.Add(_spinnerPictureBox);
        this.Controls.Add(_joinButton);
        this.Controls.Add(_closeButton);

        this.ResumeLayout(false);
    }

    public void SetJoining()
    {
        _isJoining = true;
        if (_roomCodeTextBox != null) _roomCodeTextBox.Enabled = false;
        if (_joinButton != null) _joinButton.Enabled = false;
        if (_closeButton != null) _closeButton.Enabled = false;
        if (_errorLabel != null) _errorLabel.Visible = false;
        if (_spinnerPictureBox != null) _spinnerPictureBox.Visible = true;
    }

    public void SetReady()
    {
        _isJoining = false;
        if (_roomCodeTextBox != null) _roomCodeTextBox.Enabled = true;
        if (_closeButton != null) _closeButton.Enabled = true;
        if (_spinnerPictureBox != null) _spinnerPictureBox.Visible = false;
        if (_errorLabel != null) _errorLabel.Visible = false;
    }

    public void SetError(string errorMessage)
    {
        _isJoining = false;
        if (_roomCodeTextBox != null) _roomCodeTextBox.Enabled = true;
        if (_joinButton != null)
        {
            _joinButton.Enabled = !string.IsNullOrWhiteSpace(_roomCodeTextBox?.Text) && _roomCodeTextBox.Text.Length == 6;
        }
        if (_closeButton != null) _closeButton.Enabled = true;
        if (_spinnerPictureBox != null) _spinnerPictureBox.Visible = false;
        if (_errorLabel != null)
        {
            _errorLabel.Text = errorMessage;
            _errorLabel.Visible = true;
        }
    }

    private void RoomCodeTextBox_TextChanged(object? sender, EventArgs e)
    {
        if (_roomCodeTextBox == null || _joinButton == null)
            return;

        string text = _roomCodeTextBox.Text?.Trim() ?? string.Empty;
        _joinButton.Enabled = !_isJoining && text.Length == 6;
        if (_errorLabel != null)
            _errorLabel.Visible = false;
    }

    private void RoomCodeTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (_joinButton == null)
            return;

        if (e.KeyCode == Keys.Return && _joinButton.Enabled)
        {
            JoinButton_Click(this, EventArgs.Empty);
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.Escape)
        {
            CloseButton_Click(this, EventArgs.Empty);
            e.Handled = true;
        }
    }

    private void JoinButton_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(RoomCode) || RoomCode.Length != 6)
        {
            SetError("Room code must be exactly 6 characters.");
            return;
        }

        this.DialogResult = DialogResult.OK;
        SetJoining();
    }

    private void CloseButton_Click(object? sender, EventArgs e)
    {
        this.DialogResult = DialogResult.Cancel;
        this.Close();
    }

    private void DrawSpinner(Graphics g)
    {
        var rect = new Rectangle(2, 2, 21, 21);
        int angle = (int)(DateTime.Now.Millisecond / 10);

        g.Clear(this.BackColor);
        using var pen = new Pen(SystemColors.Highlight, 2);
        g.DrawArc(pen, rect, angle, 90);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (_spinnerPictureBox != null && _spinnerPictureBox.Visible)
            _spinnerPictureBox.Invalidate();
    }
}
