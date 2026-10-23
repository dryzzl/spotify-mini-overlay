using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SpotifyMiniOverlay
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new OverlayForm());
        }
    }

    internal sealed class OverlayForm : Form
    {
        private const int VK_MEDIA_NEXT_TRACK = 0xB0;
        private const int VK_MEDIA_PREV_TRACK = 0xB1;
        private const int VK_MEDIA_PLAY_PAUSE = 0xB3;
        private const uint KEYEVENTF_KEYUP = 0x0002;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern void keybd_event(byte key, byte scan, uint flags, UIntPtr extraInfo);

        private static readonly Color TransparentColor = Color.FromArgb(2, 3, 5);
        private static readonly Color RestColor = Color.FromArgb(190, 195, 201);
        private static readonly Color HoverColor = Color.White;

        private readonly Rectangle gripBounds = new Rectangle(3, 15, 19, 35);
        private readonly Rectangle previousBounds = new Rectangle(28, 12, 43, 43);
        private readonly Rectangle toggleBounds = new Rectangle(78, 10, 46, 46);
        private readonly Rectangle nextBounds = new Rectangle(132, 12, 43, 43);
        private readonly ContextMenuStrip menu;
        private readonly NotifyIcon tray;
        private readonly string positionFile;

        private bool locallyPlaying = true;
        private bool dragging;
        private bool moved;
        private Point dragOffset;
        private int hovered = -1;
        private int pressed = -1;

        public OverlayForm()
        {
            ClientSize = new Size(185, 66);
            FormBorderStyle = FormBorderStyle.None;
            BackColor = TransparentColor;
            TransparencyKey = TransparentColor;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer, true);

            positionFile = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SpotifyMiniOverlay", "position.txt");
            Location = LoadPosition();

            menu = new ContextMenuStrip();
            menu.Items.Add("Move: drag the dotted handle", null, delegate { });
            menu.Items.Add("Move from a button: Shift + drag", null, delegate { });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Reset position", null, delegate { ResetPosition(); });
            menu.Items.Add("Exit overlay", null, delegate { Close(); });
            ContextMenuStrip = menu;

            tray = new NotifyIcon();
            tray.Icon = SystemIcons.Application;
            tray.Text = "Spotify Mini Overlay";
            tray.Visible = true;
            ContextMenuStrip trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("Show overlay", null, delegate { ShowOverlay(); });
            trayMenu.Items.Add("Reset position", null, delegate { ResetPosition(); });
            trayMenu.Items.Add("Exit", null, delegate { Close(); });
            tray.ContextMenuStrip = trayMenu;
            tray.DoubleClick += delegate { ShowOverlay(); };

            ToolTip tip = new ToolTip();
            tip.SetToolTip(this, "Drag the dotted grip or Shift+drag a button. Right-click for options.");
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.Clear(TransparentColor);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (SolidBrush gripBackground = new SolidBrush(Color.FromArgb(38, 41, 45)))
            using (SolidBrush dotBrush = new SolidBrush(Color.FromArgb(150, 155, 163)))
            {
                FillRoundRectangle(g, gripBackground, gripBounds, 9);
                for (int y = 25; y <= 43; y += 9)
                {
                    g.FillEllipse(dotBrush, 8, y, 3, 3);
                    g.FillEllipse(dotBrush, 14, y, 3, 3);
                }
            }

            DrawSkip(g, previousBounds, false, hovered == 0 || pressed == 0);
            DrawToggle(g, hovered == 1 || pressed == 1);
            DrawSkip(g, nextBounds, true, hovered == 2 || pressed == 2);
        }

        private void DrawSkip(Graphics g, Rectangle bounds, bool next, bool active)
        {
            Color color = active ? HoverColor : RestColor;
            int centerX = bounds.Left + bounds.Width / 2;
            int centerY = bounds.Top + bounds.Height / 2;

            using (SolidBrush brush = new SolidBrush(color))
            {
                Point[] triangle;
                if (next)
                {
                    triangle = new Point[] {
                        new Point(centerX - 8, centerY - 10),
                        new Point(centerX + 7, centerY),
                        new Point(centerX - 8, centerY + 10)
                    };
                    g.FillPolygon(brush, triangle);
                    g.FillRectangle(brush, centerX + 8, centerY - 10, 3, 20);
                }
                else
                {
                    triangle = new Point[] {
                        new Point(centerX + 8, centerY - 10),
                        new Point(centerX - 7, centerY),
                        new Point(centerX + 8, centerY + 10)
                    };
                    g.FillPolygon(brush, triangle);
                    g.FillRectangle(brush, centerX - 11, centerY - 10, 3, 20);
                }
            }
        }

        private void DrawToggle(Graphics g, bool active)
        {
            Rectangle circle = new Rectangle(
                toggleBounds.Left + 3, toggleBounds.Top + 3, 40, 40);

            using (SolidBrush white = new SolidBrush(active
                ? Color.FromArgb(245, 245, 245) : Color.White))
            using (SolidBrush dark = new SolidBrush(Color.FromArgb(19, 21, 24)))
            {
                g.FillEllipse(white, circle);
                if (locallyPlaying)
                {
                    FillRoundRectangle(g, dark,
                        new Rectangle(circle.Left + 13, circle.Top + 11, 5, 18), 2);
                    FillRoundRectangle(g, dark,
                        new Rectangle(circle.Left + 22, circle.Top + 11, 5, 18), 2);
                }
                else
                {
                    g.FillPolygon(dark, new Point[] {
                        new Point(circle.Left + 15, circle.Top + 10),
                        new Point(circle.Left + 15, circle.Top + 30),
                        new Point(circle.Left + 29, circle.Top + 20)
                    });
                }
            }
        }

        private static void FillRoundRectangle(
            Graphics g, Brush brush, Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            using (GraphicsPath p = new GraphicsPath())
            {
                p.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
                p.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
                p.AddArc(bounds.Right - diameter, bounds.Bottom - diameter,
                    diameter, diameter, 0, 90);
                p.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
                p.CloseFigure();
                g.FillPath(brush, p);
            }
        }

        private int HitButton(Point p)
        {
            if (previousBounds.Contains(p)) return 0;
            if (toggleBounds.Contains(p)) return 1;
            if (nextBounds.Contains(p)) return 2;
            return -1;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;

            int target = HitButton(e.Location);
            if ((ModifierKeys & Keys.Shift) == Keys.Shift || target == -1)
            {
                dragging = true;
                moved = false;
                dragOffset = e.Location;
                Capture = true;
            }
            else
            {
                pressed = target;
                Capture = true;
            }
            Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (dragging)
            {
                Point newLocation = new Point(
                    Cursor.Position.X - dragOffset.X,
                    Cursor.Position.Y - dragOffset.Y);
                if (Location != newLocation)
                {
                    moved = true;
                    Location = newLocation;
                }
                return;
            }
            int nextHover = HitButton(e.Location);
            if (nextHover != hovered)
            {
                hovered = nextHover;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (!dragging && pressed == -1)
            {
                hovered = -1;
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button != MouseButtons.Left) return;

            if (dragging)
            {
                dragging = false;
                Capture = false;
                if (moved) SavePosition();
            }
            else if (pressed != -1)
            {
                int selected = pressed;
                pressed = -1;
                Capture = false;
                if (selected == HitButton(e.Location))
                {
                    if (selected == 0) SendMediaKey(VK_MEDIA_PREV_TRACK);
                    if (selected == 1)
                    {
                        SendMediaKey(VK_MEDIA_PLAY_PAUSE);
                        locallyPlaying = !locallyPlaying;
                    }
                    if (selected == 2) SendMediaKey(VK_MEDIA_NEXT_TRACK);
                }
            }
            Invalidate();
        }

        private static void SendMediaKey(int virtualKey)
        {
            keybd_event((byte)virtualKey, 0, 0, UIntPtr.Zero);
            keybd_event((byte)virtualKey, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }

        private Point DefaultPosition()
        {
            Rectangle work = Screen.PrimaryScreen.WorkingArea;
            return new Point(work.Right - Width - 35, work.Top + 35);
        }

        private Point LoadPosition()
        {
            try
            {
                string[] lines = File.ReadAllLines(positionFile);
                if (lines.Length >= 2)
                {
                    int x;
                    int y;
                    if (int.TryParse(lines[0], NumberStyles.Integer,
                            CultureInfo.InvariantCulture, out x) &&
                        int.TryParse(lines[1], NumberStyles.Integer,
                            CultureInfo.InvariantCulture, out y))
                    {
                        Rectangle saved = new Rectangle(x, y, Width, Height);
                        foreach (Screen screen in Screen.AllScreens)
                        {
                            if (screen.WorkingArea.IntersectsWith(saved))
                                return saved.Location;
                        }
                    }
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            return DefaultPosition();
        }

        private void SavePosition()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(positionFile));
                File.WriteAllLines(positionFile, new string[] {
                    Location.X.ToString(CultureInfo.InvariantCulture),
                    Location.Y.ToString(CultureInfo.InvariantCulture)
                });
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        private void ResetPosition()
        {
            Location = DefaultPosition();
            SavePosition();
            ShowOverlay();
        }

        private void ShowOverlay()
        {
            Show();
            TopMost = true;
            BringToFront();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            SavePosition();
            tray.Visible = false;
            tray.Dispose();
            menu.Dispose();
            base.OnFormClosed(e);
        }
    }
}
