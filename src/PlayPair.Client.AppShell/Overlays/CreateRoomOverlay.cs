using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PlayPair.Client.AppShell.Overlays;

public sealed partial class CreateRoomOverlay : Form
{
    private Label? _statusLabel;
    private Label? _roomCodeLabel;
    private Button? _copyButton;
    private Button? _closeButton;
    private PictureBox? _spinnerPictureBox;

    public string? RoomCode { get; private set; }

    public CreateRoomOverlay()
    {
        InitializeComponent();
        SetStyle(ControlStyles.DoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
    }

    private void InitializeComponent()
    {
        this.SuspendLayout();

        // Form properties
        this.ClientSize = new Size(400, 200);
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.ShowIcon = false;
        this.ShowInTaskbar = false;
        this.StartPosition = FormStartPosition.CenterScreen;
        this.Text = "PlayPair - Create Room";
        this.TopMost = true;

        // Title label
        var titleLabel = new Label
        {
            Text = "Creating Room...",
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            Dock = DockStyle.Top,
            Height = 40,
            Padding = new Padding(10),
            TextAlign = ContentAlignment.MiddleLeft
        };

        // Spinner
        _spinnerPictureBox = new PictureBox
        {
            Size = new Size(30, 30),
            Location = new Point(10, 50),
            SizeMode = PictureBoxSizeMode.CenterImage
        };
        // Draw a simple spinner circle
        _spinnerPictureBox.Paint += (s, e) => DrawSpinner(e.Graphics);

        // Status label
        _statusLabel = new Label
        {
            Text = "Connecting to server...",
            Font = new Font("Segoe UI", 10f),
            Location = new Point(50, 55),
            Size = new Size(330, 30),
            TextAlign = ContentAlignment.MiddleLeft
        };

        // Room code label
        _roomCodeLabel = new Label
        {
            Text = "Room Code: ---",
            Font = new Font("Segoe UI", 14f, FontStyle.Bold),
            Location = new Point(20, 90),
            Size = new Size(360, 40),
            TextAlign = ContentAlignment.MiddleCenter,
            Visible = false
        };

        // Copy button
        _copyButton = new Button
        {
            Text = "Copy Code",
            Location = new Point(260, 140),
            Size = new Size(120, 35),
            BackColor = SystemColors.Control,
            Visible = false
        };
        _copyButton.Click += CopyButton_Click;

        // Close button
        _closeButton = new Button
        {
            Text = "Close",
            Location = new Point(20, 140),
            Size = new Size(120, 35),
            BackColor = SystemColors.Control
        };
        _closeButton.Click += CloseButton_Click;

        this.Controls.Add(titleLabel);
        this.Controls.Add(_spinnerPictureBox);
        this.Controls.Add(_statusLabel);
        this.Controls.Add(_roomCodeLabel);
        this.Controls.Add(_copyButton);
        this.Controls.Add(_closeButton);

        this.ResumeLayout(false);
    }

    public void SetCreating()
    {
        if (_statusLabel == null || _spinnerPictureBox == null || _roomCodeLabel == null || _copyButton == null)
            return;

        _statusLabel.Text = "Connecting to server...";
        _spinnerPictureBox.Visible = true;
        _roomCodeLabel.Visible = false;
        _copyButton.Visible = false;
    }

    public void SetReady(string roomCode)
    {
        if (_statusLabel == null || _spinnerPictureBox == null || _roomCodeLabel == null || _copyButton == null)
            return;

        RoomCode = roomCode ?? throw new ArgumentNullException(nameof(roomCode));
        _statusLabel.Text = "Ready to connect";
        _roomCodeLabel.Text = roomCode;
        _spinnerPictureBox.Visible = false;
        _roomCodeLabel.Visible = true;
        _copyButton.Visible = true;
    }

    public void SetError(string errorMessage)
    {
        if (_statusLabel == null || _spinnerPictureBox == null)
            return;

        _statusLabel.Text = errorMessage;
        _statusLabel.ForeColor = Color.Red;
        _spinnerPictureBox.Visible = false;
    }

    private void CopyButton_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(RoomCode) || _copyButton == null)
            return;

        Clipboard.SetText(RoomCode);
        _copyButton.Text = "Copied!";
        _copyButton.Enabled = false;

        Task.Delay(2000).ContinueWith(_ =>
        {
            if (InvokeRequired && _copyButton != null)
            {
                Invoke(() =>
                {
                    if (_copyButton != null)
                    {
                        _copyButton.Text = "Copy Code";
                        _copyButton.Enabled = true;
                    }
                });
            }
        });
    }

    private void CloseButton_Click(object? sender, EventArgs e)
    {
        this.DialogResult = DialogResult.Cancel;
        this.Close();
    }

    private void DrawSpinner(Graphics g)
    {
        var rect = new Rectangle(5, 5, 20, 20);
        int angle = (int)(DateTime.Now.Millisecond / 10);

        g.Clear(this.BackColor);
        using var pen = new Pen(SystemColors.Highlight, 2);
        g.DrawArc(pen, rect, angle, 90);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (_spinnerPictureBox != null)
            _spinnerPictureBox.Invalidate();
    }
}
