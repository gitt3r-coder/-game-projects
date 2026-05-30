using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Windows.Forms;

namespace NEON_MEMERY
{
    public partial class Form1 : Form
    {
        private const int SlotCount = 5;
        private readonly List<MemoryFragment> fragments;
        private readonly List<CardHit> cardHits = new List<CardHit>();
        private readonly List<SlotHit> slotHits = new List<SlotHit>();
        private readonly List<ButtonHit> buttonHits = new List<ButtonHit>();
        private readonly string[] timeline = new string[SlotCount];
        private readonly HashSet<string> unlocked = new HashSet<string>();
        private readonly Timer animationTimer;
        private readonly string savePath;

        private readonly Font titleFont = new Font("Segoe UI", 24f, FontStyle.Bold);
        private readonly Font logoFont = new Font("Segoe UI", 15f, FontStyle.Bold);
        private readonly Font headingFont = new Font("Segoe UI", 11f, FontStyle.Bold);
        private readonly Font bodyFont = new Font("Segoe UI", 9.3f, FontStyle.Regular);
        private readonly Font smallFont = new Font("Segoe UI", 8.2f, FontStyle.Regular);
        private readonly Font monoFont = new Font("Consolas", 8.6f, FontStyle.Regular);
        private readonly StringFormat paragraphFormat = new StringFormat
        {
            Trimming = StringTrimming.EllipsisWord,
            FormatFlags = StringFormatFlags.LineLimit
        };

        private float pulse;
        private string selectedId;
        private string interpretation = "empathy";
        private string statusLine = "АРХИВ ОЖИДАЕТ МОНТАЖА";
        private int autosaveFlash;

        public Form1()
        {
            InitializeComponent();
            Text = "NEON//MEMORY";
            ClientSize = new Size(1240, 760);
            MinimumSize = new Size(1060, 680);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(6, 8, 18);
            DoubleBuffered = true;
            KeyPreview = true;

            fragments = CreateFragments();
            savePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "NeonMemory",
                "save.json");

            LoadGame();
            animationTimer = new Timer();
            animationTimer.Interval = 16;
            animationTimer.Tick += AnimationTimer_Tick;
            animationTimer.Start();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            cardHits.Clear();
            slotHits.Clear();
            buttonHits.Clear();

            Rectangle client = ClientRectangle;
            DrawBackdrop(g, client);
            DrawHeader(g, client);

            RectangleF leftPanel = new RectangleF(24, 96, 318, client.Height - 124);
            RectangleF centerPanel = new RectangleF(366, 96, client.Width - 724, client.Height - 124);
            RectangleF rightPanel = new RectangleF(client.Width - 334, 96, 310, client.Height - 124);

            DrawGlassPanel(g, leftPanel, Color.FromArgb(56, 42, 235, 255), 0.46f);
            DrawGlassPanel(g, centerPanel, Color.FromArgb(52, 144, 84, 255), 0.40f);
            DrawGlassPanel(g, rightPanel, Color.FromArgb(54, 255, 57, 181), 0.42f);

            DrawFragmentDeck(g, leftPanel);
            DrawTimeline(g, centerPanel);
            DrawWorldPanel(g, rightPanel);

            if (autosaveFlash > 0)
            {
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(Math.Min(180, autosaveFlash * 6), 90, 255, 218)))
                {
                    g.DrawString("SAVE SYNC", monoFont, brush, client.Width - 104, 72);
                }
            }
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);

            foreach (ButtonHit hit in buttonHits)
            {
                if (hit.Bounds.Contains(e.Location))
                {
                    HandleButton(hit.Command);
                    return;
                }
            }

            foreach (SlotHit hit in slotHits)
            {
                if (hit.Bounds.Contains(e.Location))
                {
                    if (e.Button == MouseButtons.Right)
                    {
                        timeline[hit.Index] = null;
                        selectedId = null;
                        statusLine = "СВЯЗЬ РАЗОМКНУТА";
                        SaveGame();
                    }
                    else if (!string.IsNullOrEmpty(selectedId))
                    {
                        PlaceSelectedFragment(hit.Index);
                    }
                    else if (!string.IsNullOrEmpty(timeline[hit.Index]))
                    {
                        selectedId = timeline[hit.Index];
                        statusLine = "ФРАГМЕНТ ПОДНЯТ С ТАЙМЛАЙНА";
                    }

                    Invalidate();
                    return;
                }
            }

            foreach (CardHit hit in cardHits)
            {
                if (hit.Bounds.Contains(e.Location))
                {
                    selectedId = hit.FragmentId;
                    statusLine = "ФРАГМЕНТ " + hit.FragmentId + " В ФОКУСЕ";
                    Invalidate();
                    return;
                }
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.KeyCode == Keys.S)
            {
                SaveGame();
                statusLine = "СОХРАНЕНИЕ ЗАКРЕПЛЕНО";
                Invalidate();
            }

            if (e.KeyCode == Keys.Escape)
            {
                selectedId = null;
                Invalidate();
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            SaveGame();
            animationTimer.Stop();
            animationTimer.Dispose();
            titleFont.Dispose();
            logoFont.Dispose();
            headingFont.Dispose();
            bodyFont.Dispose();
            smallFont.Dispose();
            monoFont.Dispose();
            paragraphFormat.Dispose();
            base.OnFormClosed(e);
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            pulse += 0.028f;
            if (pulse > 10000f)
            {
                pulse = 0f;
            }

            if (autosaveFlash > 0)
            {
                autosaveFlash--;
            }

            Invalidate();
        }

        private static List<MemoryFragment> CreateFragments()
        {
            return new List<MemoryFragment>
            {
                new MemoryFragment("M01", "Последний завтрак", "Кухня пахнет озоном. На стекле пальцем выведено: «не верь дню запуска».", "дом", 12, "cyan"),
                new MemoryFragment("M02", "Лифт к облакам", "Орбитальная башня уходит в белый шум. Толпа смотрит вверх, но никто не моргает.", "город", 28, "violet"),
                new MemoryFragment("M03", "Комната зеркал", "Семнадцать копий одного лица спорят о том, кто из них настоящий свидетель.", "личность", 35, "pink"),
                new MemoryFragment("M04", "Протокол Эхо", "Совет просит архив сохранить людей как данные, пока тела еще дышат.", "решение", 41, "green"),
                new MemoryFragment("M05", "Пожар в серверной", "Красный свет гаснет слой за слоем. Кто-то вручную закрывает шлюзы памяти.", "катастрофа", 54, "amber"),
                new MemoryFragment("M06", "Голос матери", "Сообщение повторяется без адресата: «если найдешь меня, не собирай все обратно».", "утрата", 63, "blue"),
                new MemoryFragment("M07", "Пустой город", "На рассвете улицы включаются сами. Реклама зовет людей, которых больше нет.", "финал", 79, "white")
            };
        }

        private void DrawBackdrop(Graphics g, Rectangle client)
        {
            Color top = Blend(Color.FromArgb(5, 8, 18), WorldTint(), 0.12f);
            Color bottom = Color.FromArgb(13, 7, 28);
            using (LinearGradientBrush brush = new LinearGradientBrush(client, top, bottom, LinearGradientMode.Vertical))
            {
                g.FillRectangle(brush, client);
            }

            using (Pen gridPen = new Pen(Color.FromArgb(22, 89, 255, 233), 1f))
            {
                int offset = (int)(pulse * 12f) % 42;
                for (int x = -42 + offset; x < client.Width; x += 42)
                {
                    g.DrawLine(gridPen, x, 88, x + 170, client.Height);
                }

                for (int y = 98; y < client.Height; y += 42)
                {
                    g.DrawLine(gridPen, 0, y, client.Width, y + (int)(Math.Sin(pulse + y) * 10));
                }
            }

            using (Pen scanPen = new Pen(Color.FromArgb(42, 255, 255, 255), 1f))
            {
                int scanY = 96 + (int)((Math.Sin(pulse * 0.8f) + 1f) * 0.5f * (client.Height - 120));
                g.DrawLine(scanPen, 0, scanY, client.Width, scanY);
            }

            for (int i = 0; i < 42; i++)
            {
                float x = (float)((Math.Sin(i * 12.73 + pulse * 0.4) + 1) * 0.5 * client.Width);
                float y = 80 + (float)((Math.Cos(i * 7.19 + pulse * 0.25) + 1) * 0.5 * (client.Height - 100));
                int alpha = 35 + (int)((Math.Sin(pulse + i) + 1) * 45);
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(alpha, 126, 255, 235)))
                {
                    g.FillEllipse(brush, x, y, 2f, 2f);
                }
            }
        }

        private void DrawHeader(Graphics g, Rectangle client)
        {
            RectangleF header = new RectangleF(24, 18, client.Width - 48, 62);
            using (Pen pen = new Pen(Color.FromArgb(92, 75, 245, 255), 1f))
            {
                g.DrawLine(pen, header.Left, header.Bottom, header.Right, header.Bottom);
            }

            DrawGlowText(g, "NEON//MEMORY", titleFont, new PointF(26, 18), Color.FromArgb(118, 255, 236), Color.FromArgb(60, 10, 255, 255));
            using (SolidBrush brush = new SolidBrush(Color.FromArgb(176, 214, 239, 255)))
            {
                g.DrawString("digital archive / post-human reconstruction", monoFont, brush, 31, 58);
                g.DrawString(statusLine, monoFont, brush, client.Width - 330, 31);
            }

            DrawCommandButton(g, new RectangleF(client.Width - 252, 52, 84, 26), "SCAN", "scan");
            DrawCommandButton(g, new RectangleF(client.Width - 160, 52, 68, 26), "SAVE", "save");
            DrawCommandButton(g, new RectangleF(client.Width - 84, 52, 60, 26), "NEW", "new");
        }

        private void DrawFragmentDeck(Graphics g, RectangleF panel)
        {
            DrawSectionTitle(g, "ФРАГМЕНТЫ", panel.X + 18, panel.Y + 18, Color.FromArgb(122, 255, 238));

            int found = unlocked.Count;
            using (SolidBrush brush = new SolidBrush(Color.FromArgb(168, 206, 232, 255)))
            {
                g.DrawString("FOUND " + found + "/" + fragments.Count, monoFont, brush, panel.Right - 94, panel.Y + 22);
            }

            List<MemoryFragment> visible = fragments.Where(f => unlocked.Contains(f.Id)).ToList();
            float y = panel.Y + 58;
            foreach (MemoryFragment fragment in visible)
            {
                bool placed = timeline.Contains(fragment.Id);
                RectangleF card = new RectangleF(panel.X + 18, y, panel.Width - 36, 86);
                DrawMemoryCard(g, card, fragment, selectedId == fragment.Id, placed, true);
                cardHits.Add(new CardHit(fragment.Id, Rectangle.Round(card)));
                y += 96;
            }

            if (visible.Count < fragments.Count)
            {
                RectangleF locked = new RectangleF(panel.X + 18, Math.Min(panel.Bottom - 96, y + 6), panel.Width - 36, 72);
                DrawLockedCard(g, locked);
            }
        }

        private void DrawTimeline(Graphics g, RectangleF panel)
        {
            DrawSectionTitle(g, "ТАЙМЛАЙН", panel.X + 18, panel.Y + 18, Color.FromArgb(190, 161, 255));
            DrawCoherenceMeter(g, panel);

            float top = panel.Y + 98;
            float slotWidth = Math.Min(188f, (panel.Width - 64) / 2f);
            float left = panel.X + 32;
            float right = panel.Right - 32 - slotWidth;
            PointF[] centers = new PointF[SlotCount];

            for (int i = 0; i < SlotCount; i++)
            {
                float x = i % 2 == 0 ? left : right;
                float y = top + i * 98;
                RectangleF slot = new RectangleF(x, y, slotWidth, 76);
                centers[i] = new PointF(slot.X + slot.Width / 2f, slot.Y + slot.Height / 2f);
                DrawTimelineSlot(g, slot, i);
                slotHits.Add(new SlotHit(i, Rectangle.Round(slot)));
            }

            DrawConnections(g, centers);

            using (SolidBrush brush = new SolidBrush(Color.FromArgb(150, 225, 238, 255)))
            {
                string report = BuildArchiveReport();
                RectangleF reportRect = new RectangleF(panel.X + 26, panel.Bottom - 82, panel.Width - 52, 54);
                g.DrawString(report, bodyFont, brush, reportRect, paragraphFormat);
            }
        }

        private void DrawWorldPanel(Graphics g, RectangleF panel)
        {
            DrawSectionTitle(g, "МИР", panel.X + 18, panel.Y + 18, Color.FromArgb(255, 99, 197));

            RectangleF viewport = new RectangleF(panel.X + 18, panel.Y + 58, panel.Width - 36, 176);
            DrawWorldViewport(g, viewport);

            using (SolidBrush brush = new SolidBrush(Color.FromArgb(194, 226, 238, 255)))
            {
                RectangleF textBox = new RectangleF(panel.X + 20, viewport.Bottom + 18, panel.Width - 40, 112);
                g.DrawString(BuildWorldText(), bodyFont, brush, textBox, paragraphFormat);
            }

            float y = viewport.Bottom + 154;
            DrawSectionTitle(g, "ИНТЕРПРЕТАЦИЯ", panel.X + 18, y, Color.FromArgb(122, 255, 238));
            y += 38;
            DrawInterpretationButton(g, new RectangleF(panel.X + 18, y, panel.Width - 36, 40), "СОСТРАДАНИЕ", "empathy");
            DrawInterpretationButton(g, new RectangleF(panel.X + 18, y + 50, panel.Width - 36, 40), "ДОКАЗАТЕЛЬСТВО", "evidence");
            DrawInterpretationButton(g, new RectangleF(panel.X + 18, y + 100, panel.Width - 36, 40), "ЗАБВЕНИЕ", "erasure");

            using (SolidBrush brush = new SolidBrush(Color.FromArgb(130, 210, 226, 255)))
            {
                string branch = "BRANCH: " + GetBranchName().ToUpperInvariant();
                g.DrawString(branch, monoFont, brush, panel.X + 20, panel.Bottom - 36);
            }
        }

        private void DrawGlassPanel(Graphics g, RectangleF rect, Color edge, float opacity)
        {
            using (GraphicsPath path = RoundedRect(rect, 18f))
            using (SolidBrush fill = new SolidBrush(Color.FromArgb((int)(80 * opacity), 18, 28, 48)))
            using (Pen border = new Pen(edge, 1.2f))
            {
                g.FillPath(fill, path);
                g.DrawPath(border, path);
            }

            RectangleF shine = new RectangleF(rect.X + 1, rect.Y + 1, rect.Width - 2, Math.Max(30, rect.Height * 0.18f));
            using (GraphicsPath shinePath = RoundedRect(shine, 17f))
            using (LinearGradientBrush brush = new LinearGradientBrush(shine, Color.FromArgb(38, 255, 255, 255), Color.Transparent, LinearGradientMode.Vertical))
            {
                g.FillPath(brush, shinePath);
            }
        }

        private void DrawMemoryCard(Graphics g, RectangleF rect, MemoryFragment fragment, bool selected, bool placed, bool compact)
        {
            Color accent = FragmentColor(fragment);
            int fillAlpha = selected ? 80 : 46;
            using (GraphicsPath path = RoundedRect(rect, 12f))
            using (SolidBrush fill = new SolidBrush(Color.FromArgb(fillAlpha, 18, 30, 52)))
            using (Pen border = new Pen(Color.FromArgb(selected ? 210 : 118, accent), selected ? 2.2f : 1.1f))
            {
                g.FillPath(fill, path);
                g.DrawPath(border, path);
            }

            float glitch = (float)Math.Sin(pulse * 2.2f + fragment.Minute) * 3f;
            using (Pen pen = new Pen(Color.FromArgb(70, accent), 1f))
            {
                g.DrawLine(pen, rect.Left + 12, rect.Top + 22 + glitch, rect.Right - 14, rect.Top + 20 - glitch);
                g.DrawLine(pen, rect.Left + 12, rect.Bottom - 16, rect.Right - 14, rect.Bottom - 18);
            }

            using (SolidBrush accentBrush = new SolidBrush(Color.FromArgb(230, accent)))
            using (SolidBrush textBrush = new SolidBrush(placed ? Color.FromArgb(150, 207, 221, 233) : Color.FromArgb(224, 235, 250, 255)))
            using (SolidBrush dimBrush = new SolidBrush(Color.FromArgb(148, 191, 213, 229)))
            {
                g.DrawString(fragment.Id + " / " + fragment.Tag.ToUpperInvariant(), monoFont, accentBrush, rect.X + 14, rect.Y + 10);
                g.DrawString(fragment.Title, headingFont, textBrush, rect.X + 14, rect.Y + 29);

                RectangleF textRect = new RectangleF(rect.X + 14, rect.Y + 51, rect.Width - 28, rect.Height - 56);
                string body = compact ? fragment.Summary : fragment.Summary + "\n" + fragment.Tag;
                g.DrawString(body, smallFont, dimBrush, textRect, paragraphFormat);
            }

            if (placed)
            {
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(130, 90, 255, 218)))
                {
                    g.FillEllipse(brush, rect.Right - 24, rect.Top + 12, 9, 9);
                }
            }
        }

        private void DrawLockedCard(Graphics g, RectangleF rect)
        {
            using (GraphicsPath path = RoundedRect(rect, 12f))
            using (HatchBrush hatch = new HatchBrush(HatchStyle.Percent20, Color.FromArgb(60, 121, 255, 233), Color.FromArgb(24, 15, 24, 40)))
            using (Pen border = new Pen(Color.FromArgb(76, 121, 255, 233), 1f))
            {
                g.FillPath(hatch, path);
                g.DrawPath(border, path);
            }

            using (SolidBrush brush = new SolidBrush(Color.FromArgb(136, 213, 235, 255)))
            {
                g.DrawString("LOCKED MEMORY BLOCK", monoFont, brush, rect.X + 14, rect.Y + 17);
                g.DrawString("SIGNAL BELOW THRESHOLD", smallFont, brush, rect.X + 14, rect.Y + 40);
            }
        }

        private void DrawTimelineSlot(Graphics g, RectangleF slot, int index)
        {
            string id = timeline[index];
            MemoryFragment fragment = GetFragment(id);
            Color accent = fragment == null ? Color.FromArgb(84, 135, 175) : FragmentColor(fragment);
            bool active = !string.IsNullOrEmpty(selectedId);
            int alpha = fragment == null ? (active ? 54 : 32) : 68;

            using (GraphicsPath path = RoundedRect(slot, 14f))
            using (SolidBrush fill = new SolidBrush(Color.FromArgb(alpha, 18, 31, 52)))
            using (Pen border = new Pen(Color.FromArgb(fragment == null ? 90 : 180, accent), fragment == null ? 1f : 1.8f))
            {
                g.FillPath(fill, path);
                g.DrawPath(border, path);
            }

            using (SolidBrush accentBrush = new SolidBrush(Color.FromArgb(220, accent)))
            using (SolidBrush textBrush = new SolidBrush(Color.FromArgb(222, 234, 247, 255)))
            using (SolidBrush dimBrush = new SolidBrush(Color.FromArgb(140, 197, 214, 231)))
            {
                g.DrawString("T+" + (index + 1).ToString("00"), monoFont, accentBrush, slot.X + 12, slot.Y + 10);
                if (fragment == null)
                {
                    g.DrawString("пустой узел", headingFont, dimBrush, slot.X + 12, slot.Y + 31);
                }
                else
                {
                    g.DrawString(fragment.Title, headingFont, textBrush, slot.X + 12, slot.Y + 30);
                    g.DrawString(fragment.Minute.ToString("00") + " min / " + fragment.Tag, monoFont, dimBrush, slot.X + 12, slot.Y + 53);
                }
            }
        }

        private void DrawConnections(Graphics g, PointF[] centers)
        {
            for (int i = 0; i < centers.Length - 1; i++)
            {
                bool connected = !string.IsNullOrEmpty(timeline[i]) && !string.IsNullOrEmpty(timeline[i + 1]);
                Color color = connected ? Color.FromArgb(130, 118, 255, 234) : Color.FromArgb(45, 118, 255, 234);
                using (Pen pen = new Pen(color, connected ? 2.2f : 1f))
                {
                    pen.DashStyle = connected ? DashStyle.Solid : DashStyle.Dash;
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    PointF a = centers[i];
                    PointF b = centers[i + 1];
                    float wave = (float)Math.Sin(pulse * 2f + i) * 18f;
                    using (GraphicsPath path = new GraphicsPath())
                    {
                        path.AddBezier(
                            a,
                            new PointF(a.X + (b.X - a.X) * 0.33f, a.Y + wave),
                            new PointF(a.X + (b.X - a.X) * 0.66f, b.Y - wave),
                            b);
                        g.DrawPath(pen, path);
                    }
                }

                if (connected)
                {
                    float t = (float)((Math.Sin(pulse * 1.7f + i) + 1) * 0.5);
                    PointF dot = Lerp(centers[i], centers[i + 1], t);
                    using (SolidBrush brush = new SolidBrush(Color.FromArgb(220, 255, 255, 255)))
                    {
                        g.FillEllipse(brush, dot.X - 3, dot.Y - 3, 6, 6);
                    }
                }
            }
        }

        private void DrawCoherenceMeter(Graphics g, RectangleF panel)
        {
            int coherence = CalculateCoherence();
            RectangleF meter = new RectangleF(panel.Right - 186, panel.Y + 22, 150, 10);
            using (GraphicsPath bgPath = RoundedRect(meter, 5f))
            using (SolidBrush bg = new SolidBrush(Color.FromArgb(44, 255, 255, 255)))
            {
                g.FillPath(bg, bgPath);
            }

            RectangleF fillRect = new RectangleF(meter.X, meter.Y, meter.Width * coherence / 100f, meter.Height);
            if (fillRect.Width > 1)
            {
                using (GraphicsPath fillPath = RoundedRect(fillRect, 5f))
                using (LinearGradientBrush fill = new LinearGradientBrush(fillRect, Color.FromArgb(255, 91, 255, 215), Color.FromArgb(255, 255, 84, 206), LinearGradientMode.Horizontal))
                {
                    g.FillPath(fill, fillPath);
                }
            }

            using (SolidBrush brush = new SolidBrush(Color.FromArgb(160, 219, 237, 255)))
            {
                g.DrawString("COHERENCE " + coherence + "%", monoFont, brush, meter.X - 2, meter.Y + 14);
            }
        }

        private void DrawWorldViewport(Graphics g, RectangleF rect)
        {
            Color tint = WorldTint();
            using (GraphicsPath path = RoundedRect(rect, 14f))
            using (SolidBrush fill = new SolidBrush(Color.FromArgb(76, 8, 13, 26)))
            using (Pen border = new Pen(Color.FromArgb(135, tint), 1.4f))
            {
                g.FillPath(fill, path);
                g.DrawPath(border, path);
            }

            GraphicsState state = g.Save();
            g.SetClip(rect);
            float horizon = rect.Y + rect.Height * 0.62f;
            using (Pen pen = new Pen(Color.FromArgb(80, tint), 1f))
            {
                for (int i = 0; i < 18; i++)
                {
                    float x = rect.X + 14 + i * 16;
                    float height = 28 + (float)(Math.Sin(i * 0.9 + pulse) + 1) * 46;
                    RectangleF tower = new RectangleF(x, horizon - height, 9 + (i % 3) * 5, height);
                    using (SolidBrush brush = new SolidBrush(Color.FromArgb(34 + (i % 4) * 10, tint)))
                    {
                        g.FillRectangle(brush, tower);
                    }

                    g.DrawLine(pen, tower.Left, tower.Top, tower.Right, tower.Top + (float)Math.Sin(pulse + i) * 6);
                }

                for (int y = 0; y < 6; y++)
                {
                    float lineY = horizon + y * 14 + (float)Math.Sin(pulse + y) * 3;
                    g.DrawLine(pen, rect.Left + 8, lineY, rect.Right - 8, lineY);
                }
            }

            int filled = timeline.Count(id => !string.IsNullOrEmpty(id));
            using (SolidBrush brush = new SolidBrush(Color.FromArgb(175, tint)))
            {
                float core = 16 + filled * 4 + (float)Math.Sin(pulse * 2) * 2;
                g.FillEllipse(brush, rect.X + rect.Width / 2f - core / 2f, rect.Y + 36 - core / 2f, core, core);
            }

            g.Restore(state);

            using (SolidBrush brush = new SolidBrush(Color.FromArgb(162, 228, 245, 255)))
            {
                g.DrawString("WORLD SIMULATION", monoFont, brush, rect.X + 14, rect.Y + 12);
                g.DrawString(GetBranchName().ToUpperInvariant(), headingFont, brush, rect.X + 14, rect.Bottom - 34);
            }
        }

        private void DrawInterpretationButton(Graphics g, RectangleF rect, string text, string command)
        {
            bool active = interpretation == command;
            Color color = command == "empathy"
                ? Color.FromArgb(99, 255, 218)
                : command == "evidence" ? Color.FromArgb(181, 143, 255) : Color.FromArgb(255, 91, 166);

            using (GraphicsPath path = RoundedRect(rect, 10f))
            using (SolidBrush fill = new SolidBrush(Color.FromArgb(active ? 86 : 35, 18, 28, 47)))
            using (Pen border = new Pen(Color.FromArgb(active ? 210 : 90, color), active ? 1.8f : 1f))
            {
                g.FillPath(fill, path);
                g.DrawPath(border, path);
            }

            using (SolidBrush brush = new SolidBrush(active ? Color.White : Color.FromArgb(176, 219, 232, 244)))
            {
                g.DrawString(text, headingFont, brush, rect.X + 14, rect.Y + 10);
            }

            buttonHits.Add(new ButtonHit(command, Rectangle.Round(rect)));
        }

        private void DrawCommandButton(Graphics g, RectangleF rect, string text, string command)
        {
            using (GraphicsPath path = RoundedRect(rect, 8f))
            using (SolidBrush fill = new SolidBrush(Color.FromArgb(38, 28, 40, 66)))
            using (Pen border = new Pen(Color.FromArgb(120, 108, 255, 232), 1f))
            {
                g.FillPath(fill, path);
                g.DrawPath(border, path);
            }

            using (SolidBrush brush = new SolidBrush(Color.FromArgb(210, 231, 252, 255)))
            {
                SizeF size = g.MeasureString(text, monoFont);
                g.DrawString(text, monoFont, brush, rect.X + (rect.Width - size.Width) / 2f, rect.Y + 6);
            }

            buttonHits.Add(new ButtonHit(command, Rectangle.Round(rect)));
        }

        private void DrawSectionTitle(Graphics g, string text, float x, float y, Color color)
        {
            using (SolidBrush brush = new SolidBrush(color))
            {
                g.DrawString(text, logoFont, brush, x, y);
            }
        }

        private void DrawGlowText(Graphics g, string text, Font font, PointF point, Color color, Color glow)
        {
            using (SolidBrush glowBrush = new SolidBrush(glow))
            {
                g.DrawString(text, font, glowBrush, point.X - 1, point.Y);
                g.DrawString(text, font, glowBrush, point.X + 1, point.Y);
                g.DrawString(text, font, glowBrush, point.X, point.Y - 1);
                g.DrawString(text, font, glowBrush, point.X, point.Y + 1);
            }

            using (SolidBrush brush = new SolidBrush(color))
            {
                g.DrawString(text, font, brush, point);
            }
        }

        private void HandleButton(string command)
        {
            if (command == "scan")
            {
                UnlockNextFragment();
                return;
            }

            if (command == "save")
            {
                SaveGame();
                statusLine = "СОХРАНЕНИЕ ЗАКРЕПЛЕНО";
                Invalidate();
                return;
            }

            if (command == "new")
            {
                ResetGame();
                return;
            }

            interpretation = command;
            statusLine = "ТРАКТОВКА: " + GetBranchName().ToUpperInvariant();
            SaveGame();
            Invalidate();
        }

        private void UnlockNextFragment()
        {
            MemoryFragment next = fragments.FirstOrDefault(f => !unlocked.Contains(f.Id));
            if (next == null)
            {
                statusLine = "СКАНИРОВАНИЕ ЗАВЕРШЕНО";
                Invalidate();
                return;
            }

            unlocked.Add(next.Id);
            selectedId = next.Id;
            statusLine = "НАЙДЕН ФРАГМЕНТ " + next.Id;
            SaveGame();
            Invalidate();
        }

        private void PlaceSelectedFragment(int index)
        {
            if (string.IsNullOrEmpty(selectedId) || !unlocked.Contains(selectedId))
            {
                return;
            }

            for (int i = 0; i < timeline.Length; i++)
            {
                if (timeline[i] == selectedId)
                {
                    timeline[i] = null;
                }
            }

            timeline[index] = selectedId;
            MemoryFragment fragment = GetFragment(selectedId);
            statusLine = fragment == null ? "УЗЕЛ ОБНОВЛЕН" : fragment.Title.ToUpperInvariant() + " СМОНТИРОВАН";
            selectedId = null;
            SaveGame();
        }

        private void ResetGame()
        {
            for (int i = 0; i < timeline.Length; i++)
            {
                timeline[i] = null;
            }

            unlocked.Clear();
            unlocked.Add("M01");
            unlocked.Add("M02");
            unlocked.Add("M06");
            selectedId = null;
            interpretation = "empathy";
            statusLine = "АРХИВ ПЕРЕЗАГРУЖЕН";
            SaveGame();
            Invalidate();
        }

        private void LoadGame()
        {
            try
            {
                if (File.Exists(savePath))
                {
                    using (FileStream stream = File.OpenRead(savePath))
                    {
                        DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(MemorySave));
                        MemorySave save = serializer.ReadObject(stream) as MemorySave;
                        if (save != null)
                        {
                            Array.Clear(timeline, 0, timeline.Length);
                            for (int i = 0; i < Math.Min(timeline.Length, save.Timeline.Count); i++)
                            {
                                if (fragments.Any(f => f.Id == save.Timeline[i]))
                                {
                                    timeline[i] = save.Timeline[i];
                                }
                            }

                            unlocked.Clear();
                            foreach (string id in save.Unlocked)
                            {
                                if (fragments.Any(f => f.Id == id))
                                {
                                    unlocked.Add(id);
                                }
                            }

                            interpretation = string.IsNullOrEmpty(save.Interpretation) ? "empathy" : save.Interpretation;
                            statusLine = "СОХРАНЕНИЕ ВОССТАНОВЛЕНО";
                        }
                    }
                }
            }
            catch
            {
                statusLine = "СОХРАНЕНИЕ ПОВРЕЖДЕНО";
            }

            if (unlocked.Count == 0)
            {
                unlocked.Add("M01");
                unlocked.Add("M02");
                unlocked.Add("M06");
            }
        }

        private void SaveGame()
        {
            try
            {
                string directory = Path.GetDirectoryName(savePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                MemorySave save = new MemorySave();
                save.Timeline = timeline.Select(id => id ?? string.Empty).ToList();
                save.Unlocked = unlocked.OrderBy(id => id).ToList();
                save.Interpretation = interpretation;

                using (FileStream stream = File.Create(savePath))
                {
                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(MemorySave));
                    serializer.WriteObject(stream, save);
                }

                autosaveFlash = 24;
            }
            catch
            {
                statusLine = "ОШИБКА СОХРАНЕНИЯ";
            }
        }

        private MemoryFragment GetFragment(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            return fragments.FirstOrDefault(f => f.Id == id);
        }

        private int CalculateCoherence()
        {
            List<MemoryFragment> placed = timeline
                .Where(id => !string.IsNullOrEmpty(id))
                .Select(GetFragment)
                .Where(f => f != null)
                .ToList();

            if (placed.Count == 0)
            {
                return 0;
            }

            int score = placed.Count * 12;
            for (int i = 0; i < placed.Count - 1; i++)
            {
                int delta = placed[i + 1].Minute - placed[i].Minute;
                score += delta >= 0 ? 8 : -9;
                if (Math.Abs(delta) <= 18)
                {
                    score += 4;
                }
            }

            if (timeline.Contains("M04") && timeline.Contains("M05") && Array.IndexOf(timeline, "M04") < Array.IndexOf(timeline, "M05"))
            {
                score += 11;
            }

            if (interpretation == "evidence" && timeline.Contains("M03"))
            {
                score += 7;
            }

            if (interpretation == "erasure" && timeline.Contains("M06"))
            {
                score -= 8;
            }

            return Math.Max(0, Math.Min(100, score));
        }

        private string BuildArchiveReport()
        {
            int filled = timeline.Count(id => !string.IsNullOrEmpty(id));
            if (filled == 0)
            {
                return "Монтаж пуст. Архив хранит только шум и неподписанные лица.";
            }

            if (CalculateCoherence() > 72)
            {
                return "События складываются в устойчивую жизнь. Симуляция начинает достраивать город вокруг выбранной версии.";
            }

            if (filled >= 4)
            {
                return "Линия почти собрана, но причинность конфликтует. Мир мерцает между несколькими виновниками.";
            }

            return "Связи появились, но архив не доверяет реконструкции. Нужны новые узлы памяти.";
        }

        private string BuildWorldText()
        {
            int coherence = CalculateCoherence();

            if (interpretation == "empathy")
            {
                return coherence > 68
                    ? "Ты собираешь не отчет, а человека. Город отвечает мягким светом: часть исчезнувших получает имена."
                    : "Архив принимает боль за главный ключ. В пустых квартирах включаются голоса, но лица еще рассыпаются.";
            }

            if (interpretation == "evidence")
            {
                return coherence > 68
                    ? "Версия становится обвинением. На фасадах проступают протоколы совета и маршрут последней эвакуации."
                    : "Мир требует доказательств. Каждый неподтвержденный фрагмент превращает улицы в судебную схему.";
            }

            return coherence > 68
                ? "Ты оставляешь пробелы намеренно. Симуляция очищает город от боли, но вместе с ней исчезает часть правды."
                : "Забвение работает как холодный фильтр. Чем меньше связей, тем спокойнее и безжизненнее становится архив.";
        }

        private string GetBranchName()
        {
            if (interpretation == "evidence")
            {
                return "дело против совета";
            }

            if (interpretation == "erasure")
            {
                return "милосердная пустота";
            }

            return "возвращение имени";
        }

        private Color WorldTint()
        {
            if (interpretation == "evidence")
            {
                return Color.FromArgb(177, 137, 255);
            }

            if (interpretation == "erasure")
            {
                return Color.FromArgb(255, 80, 150);
            }

            return Color.FromArgb(92, 255, 218);
        }

        private Color FragmentColor(MemoryFragment fragment)
        {
            if (fragment == null)
            {
                return Color.FromArgb(116, 210, 232);
            }

            switch (fragment.ColorKey)
            {
                case "violet":
                    return Color.FromArgb(181, 143, 255);
                case "pink":
                    return Color.FromArgb(255, 91, 197);
                case "green":
                    return Color.FromArgb(108, 255, 162);
                case "amber":
                    return Color.FromArgb(255, 191, 87);
                case "blue":
                    return Color.FromArgb(105, 171, 255);
                case "white":
                    return Color.FromArgb(230, 245, 255);
                default:
                    return Color.FromArgb(100, 255, 232);
            }
        }

        private static GraphicsPath RoundedRect(RectangleF rect, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float diameter = radius * 2f;
            RectangleF arc = new RectangleF(rect.Location, new SizeF(diameter, diameter));
            path.AddArc(arc, 180, 90);
            arc.X = rect.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = rect.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = rect.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }

        private static PointF Lerp(PointF a, PointF b, float t)
        {
            return new PointF(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);
        }

        private static Color Blend(Color a, Color b, float amount)
        {
            amount = Math.Max(0f, Math.Min(1f, amount));
            return Color.FromArgb(
                (int)(a.R + (b.R - a.R) * amount),
                (int)(a.G + (b.G - a.G) * amount),
                (int)(a.B + (b.B - a.B) * amount));
        }
    }

    internal sealed class MemoryFragment
    {
        public MemoryFragment(string id, string title, string summary, string tag, int minute, string colorKey)
        {
            Id = id;
            Title = title;
            Summary = summary;
            Tag = tag;
            Minute = minute;
            ColorKey = colorKey;
        }

        public string Id { get; private set; }
        public string Title { get; private set; }
        public string Summary { get; private set; }
        public string Tag { get; private set; }
        public int Minute { get; private set; }
        public string ColorKey { get; private set; }
    }

    [DataContract]
    internal sealed class MemorySave
    {
        public MemorySave()
        {
            Timeline = new List<string>();
            Unlocked = new List<string>();
            Interpretation = "empathy";
        }

        [DataMember]
        public List<string> Timeline { get; set; }

        [DataMember]
        public List<string> Unlocked { get; set; }

        [DataMember]
        public string Interpretation { get; set; }
    }

    internal sealed class CardHit
    {
        public CardHit(string fragmentId, Rectangle bounds)
        {
            FragmentId = fragmentId;
            Bounds = bounds;
        }

        public string FragmentId { get; private set; }
        public Rectangle Bounds { get; private set; }
    }

    internal sealed class SlotHit
    {
        public SlotHit(int index, Rectangle bounds)
        {
            Index = index;
            Bounds = bounds;
        }

        public int Index { get; private set; }
        public Rectangle Bounds { get; private set; }
    }

    internal sealed class ButtonHit
    {
        public ButtonHit(string command, Rectangle bounds)
        {
            Command = command;
            Bounds = bounds;
        }

        public string Command { get; private set; }
        public Rectangle Bounds { get; private set; }
    }
}
