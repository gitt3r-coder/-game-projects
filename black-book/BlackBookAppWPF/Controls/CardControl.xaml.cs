using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;

namespace BlackBookAppWPF.Controls
{
    public partial class CardControl : UserControl
    {
        private Card card;
        private readonly Storyboard glowAnimation;

        public Card Card
        {
            get => card;
            set
            {
                card = value;
                UpdateDisplay();
            }
        }

        public bool IsSelected { get; private set; }
        public event EventHandler<Card> OnCardClicked;
        public event EventHandler<Card> OnCardDoubleClicked;

        public CardControl()
        {
            InitializeComponent();

            MouseLeftButtonDown += CardControl_MouseLeftButtonDown;
            MouseDoubleClick += CardControl_MouseDoubleClick;
            MouseEnter += CardControl_MouseEnter;
            MouseLeave += CardControl_MouseLeave;

            glowAnimation = Resources["GlowEffect"] as Storyboard;
        }

        private void UpdateDisplay()
        {
            if (card == null) return;

            CardName.Text = $"{card.Icon} {card.Name}";
            CardCost.Text = $"💧 {card.Cost}";
            CardPower.Text = $"⚔️ {card.Power}";
            CardDescription.Text = card.Description;
            CardType.Text = card.TypeName;
            CardFaction.Text = card.Faction;
            CardEffect.Text = card.EffectText;
            CardQuote.Text = card.Quote;

            string stars = new string('★', card.Rarity);
            string emptyStars = new string('☆', 5 - card.Rarity);
            CardStars.Text = stars + emptyStars;

            CardBorder.BorderBrush = card.GetRarityColor();
            CardBorder.BorderThickness = new Thickness(2);
            CardBorder.Background = CreateCardChromeBrush();
            CreateCardImage();
        }

        private Brush CreateCardChromeBrush()
        {
            Color baseColor = GetFactionColor(card.Faction);
            Color dark = Blend(baseColor, Colors.Black, 0.72);
            Color mid = Blend(baseColor, Colors.Black, 0.38);

            LinearGradientBrush brush = new LinearGradientBrush();
            brush.StartPoint = new Point(0, 0);
            brush.EndPoint = new Point(1, 1);
            brush.GradientStops.Add(new GradientStop(dark, 0));
            brush.GradientStops.Add(new GradientStop(mid, 0.48));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(18, 18, 26), 1));
            return brush;
        }

        private void CreateCardImage()
        {
            try
            {
                DrawingVisual visual = new DrawingVisual();
                using (DrawingContext dc = visual.RenderOpen())
                {
                    DrawArtBackground(dc);
                    DrawArtMotif(dc);
                    DrawRarityFrame(dc);
                    DrawCardSigil(dc);
                }

                RenderTargetBitmap bitmap = new RenderTargetBitmap(384, 320, 144, 144, PixelFormats.Pbgra32);
                bitmap.Render(visual);
                CardImage.Source = bitmap;
            }
            catch
            {
                CardImage.Source = null;
            }
        }

        private void DrawArtBackground(DrawingContext dc)
        {
            Color faction = GetFactionColor(card.Faction);
            Color top = Blend(faction, Colors.White, 0.16);
            Color bottom = Blend(faction, Colors.Black, 0.72);

            LinearGradientBrush sky = new LinearGradientBrush();
            sky.StartPoint = new Point(0, 0);
            sky.EndPoint = new Point(0, 1);
            sky.GradientStops.Add(new GradientStop(top, 0));
            sky.GradientStops.Add(new GradientStop(Blend(faction, Colors.Black, 0.35), 0.52));
            sky.GradientStops.Add(new GradientStop(bottom, 1));
            dc.DrawRectangle(sky, null, new Rect(0, 0, 384, 320));

            RadialGradientBrush aura = new RadialGradientBrush();
            aura.Center = new Point(0.52, 0.36);
            aura.GradientOrigin = new Point(0.48, 0.34);
            aura.RadiusX = 0.72;
            aura.RadiusY = 0.62;
            aura.GradientStops.Add(new GradientStop(Color.FromArgb(130, 255, 255, 255), 0));
            aura.GradientStops.Add(new GradientStop(Color.FromArgb(0, 255, 255, 255), 1));
            dc.DrawEllipse(aura, null, new Point(192, 118), 150, 95);

            Pen mistPen = new Pen(new SolidColorBrush(Color.FromArgb(52, 255, 255, 255)), 2);
            for (int i = 0; i < 7; i++)
            {
                double y = 52 + i * 30;
                dc.DrawGeometry(null, mistPen, Wave(8, y, 368, y + 14, 16 + i * 4));
            }
        }

        private void DrawArtMotif(DrawingContext dc)
        {
            string key = card.ArtKey ?? string.Empty;

            if (key.Contains("forest") || key.Contains("grove"))
                DrawForest(dc);
            else if (key.Contains("water") || key.Contains("river") || key.Contains("swamp"))
                DrawWater(dc, key.Contains("swamp"));
            else if (key.Contains("fire") || key.Contains("ember") || key.Contains("sun") || key.Contains("fern"))
                DrawFlame(dc);
            else if (key.Contains("bird") || key.Contains("raven") || key.Contains("oracle"))
                DrawWings(dc);
            else if (key.Contains("bone") || key.Contains("bell"))
                DrawBoneRelic(dc);
            else if (key.Contains("storm") || key.Contains("ice"))
                DrawSkyPower(dc, key.Contains("ice"));
            else if (key.Contains("gate") || key.Contains("circle") || key.Contains("binds") || key.Contains("trap"))
                DrawWard(dc);
            else if (key.Contains("serpent") || key.Contains("basilisk") || key.Contains("fang"))
                DrawBeast(dc);
            else if (key.Contains("eclipse") || key.Contains("black") || key.Contains("dark"))
                DrawEclipse(dc);
            else
                DrawWard(dc);
        }

        private void DrawForest(DrawingContext dc)
        {
            Brush ground = new SolidColorBrush(Color.FromArgb(210, 20, 45, 34));
            dc.DrawRectangle(ground, null, new Rect(0, 212, 384, 108));

            for (int i = 0; i < 9; i++)
            {
                double x = 24 + i * 42;
                double h = 95 + (i % 3) * 22;
                Pen trunk = new Pen(new SolidColorBrush(Color.FromArgb(230, 38, 27, 22)), 8);
                dc.DrawLine(trunk, new Point(x, 230), new Point(x, 230 - h));
                Brush leaves = new SolidColorBrush(Color.FromArgb(210, 35, (byte)(90 + i * 8), 54));
                dc.DrawEllipse(leaves, null, new Point(x, 130), 34, 58);
            }
        }

        private void DrawWater(DrawingContext dc, bool swamp)
        {
            Color water = swamp ? Color.FromRgb(46, 92, 68) : Color.FromRgb(46, 116, 160);
            dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(210, water.R, water.G, water.B)), null, new Rect(0, 170, 384, 150));

            Pen wavePen = new Pen(new SolidColorBrush(Color.FromArgb(150, 220, 255, 245)), 4);
            for (int i = 0; i < 8; i++)
                dc.DrawGeometry(null, wavePen, Wave(20, 188 + i * 17, 360, 188 + i * 17, 18));

            Brush moon = new SolidColorBrush(Color.FromArgb(210, 235, 245, 255));
            dc.DrawEllipse(moon, null, new Point(285, 74), 36, 36);
            dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(150, 255, 255, 255)), null, new Point(180, 180), 78, 22);
        }

        private void DrawFlame(DrawingContext dc)
        {
            DrawPetal(dc, new Point(192, 180), 48, 122, Color.FromArgb(230, 255, 67, 34), -18);
            DrawPetal(dc, new Point(192, 176), 42, 144, Color.FromArgb(225, 255, 180, 54), 16);
            DrawPetal(dc, new Point(192, 188), 24, 86, Color.FromArgb(245, 255, 244, 152), 0);

            Pen spark = new Pen(new SolidColorBrush(Color.FromArgb(160, 255, 236, 160)), 3);
            for (int i = 0; i < 14; i++)
            {
                double x = 68 + i * 19;
                double y = 58 + (i * 37) % 120;
                dc.DrawLine(spark, new Point(x, y), new Point(x + 8, y - 14));
            }
        }

        private void DrawWings(DrawingContext dc)
        {
            Brush wing = new SolidColorBrush(Color.FromArgb(214, 230, 238, 255));
            Brush shadow = new SolidColorBrush(Color.FromArgb(130, 40, 35, 70));
            dc.DrawEllipse(shadow, null, new Point(140, 174), 82, 36);
            dc.DrawEllipse(shadow, null, new Point(244, 174), 82, 36);

            for (int i = 0; i < 6; i++)
            {
                DrawPetal(dc, new Point(155 - i * 9, 178 + i * 2), 22, 94 - i * 6, Color.FromArgb(215, 210, 226, 255), -38 - i * 5);
                DrawPetal(dc, new Point(229 + i * 9, 178 + i * 2), 22, 94 - i * 6, Color.FromArgb(215, 210, 226, 255), 38 + i * 5);
            }

            dc.DrawEllipse(wing, null, new Point(192, 146), 34, 46);
            dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(240, 255, 224, 104)), null, new Point(192, 108), 26, 26);
        }

        private void DrawBoneRelic(DrawingContext dc)
        {
            Pen bonePen = new Pen(new SolidColorBrush(Color.FromArgb(230, 232, 226, 202)), 16);
            dc.DrawLine(bonePen, new Point(130, 222), new Point(254, 90));
            dc.DrawLine(bonePen, new Point(254, 222), new Point(130, 90));
            dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(230, 38, 34, 32)), new Pen(Brushes.Gold, 5), new Point(192, 156), 54, 54);
            DrawText(dc, "☠", 156, 117, 58, Brushes.Gold);
        }

        private void DrawSkyPower(DrawingContext dc, bool ice)
        {
            Color color = ice ? Color.FromRgb(160, 235, 255) : Color.FromRgb(255, 222, 94);
            Pen bolt = new Pen(new SolidColorBrush(Color.FromArgb(245, color.R, color.G, color.B)), 12);
            StreamGeometry g = new StreamGeometry();
            using (StreamGeometryContext ctx = g.Open())
            {
                ctx.BeginFigure(new Point(225, 28), false, false);
                ctx.LineTo(new Point(154, 138), true, false);
                ctx.LineTo(new Point(205, 138), true, false);
                ctx.LineTo(new Point(142, 286), true, false);
                ctx.LineTo(new Point(276, 112), true, false);
                ctx.LineTo(new Point(214, 112), true, false);
            }
            dc.DrawGeometry(null, bolt, g);
            dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(80, color.R, color.G, color.B)), null, new Point(192, 156), 132, 132);
        }

        private void DrawWard(DrawingContext dc)
        {
            Pen ward = new Pen(new SolidColorBrush(Color.FromArgb(235, 210, 238, 255)), 7);
            dc.DrawEllipse(null, ward, new Point(192, 160), 92, 92);
            dc.DrawEllipse(null, new Pen(new SolidColorBrush(Color.FromArgb(170, 255, 220, 120)), 3), new Point(192, 160), 62, 62);
            for (int i = 0; i < 8; i++)
            {
                double angle = i * Math.PI / 4;
                Point p1 = new Point(192 + Math.Cos(angle) * 42, 160 + Math.Sin(angle) * 42);
                Point p2 = new Point(192 + Math.Cos(angle) * 92, 160 + Math.Sin(angle) * 92);
                dc.DrawLine(ward, p1, p2);
            }
        }

        private void DrawBeast(DrawingContext dc)
        {
            Brush body = new SolidColorBrush(Color.FromArgb(230, 44, 32, 42));
            Brush eye = new SolidColorBrush(Color.FromArgb(250, 255, 70, 70));
            dc.DrawEllipse(body, null, new Point(192, 176), 86, 62);
            dc.DrawEllipse(body, null, new Point(146, 126), 42, 42);
            dc.DrawEllipse(body, null, new Point(238, 126), 42, 42);
            dc.DrawEllipse(eye, null, new Point(146, 124), 7, 7);
            dc.DrawEllipse(eye, null, new Point(238, 124), 7, 7);
            Pen claw = new Pen(new SolidColorBrush(Color.FromArgb(230, 245, 245, 220)), 5);
            for (int i = 0; i < 5; i++)
                dc.DrawLine(claw, new Point(110 + i * 40, 238), new Point(124 + i * 40, 270));
        }

        private void DrawEclipse(DrawingContext dc)
        {
            dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(235, 245, 210, 92)), null, new Point(192, 140), 82, 82);
            dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(245, 14, 12, 20)), null, new Point(214, 136), 78, 78);
            Pen ray = new Pen(new SolidColorBrush(Color.FromArgb(120, 255, 210, 80)), 3);
            for (int i = 0; i < 16; i++)
            {
                double angle = i * Math.PI / 8;
                dc.DrawLine(ray, new Point(192 + Math.Cos(angle) * 92, 140 + Math.Sin(angle) * 92),
                    new Point(192 + Math.Cos(angle) * 132, 140 + Math.Sin(angle) * 132));
            }
        }

        private void DrawRarityFrame(DrawingContext dc)
        {
            Pen frame = new Pen(card.GetRarityColor(), 8);
            dc.DrawRoundedRectangle(null, frame, new Rect(6, 6, 372, 308), 18, 18);
            if (card.Rarity >= 4)
            {
                dc.DrawRoundedRectangle(null, new Pen(new SolidColorBrush(Color.FromArgb(90, 255, 255, 255)), 3),
                    new Rect(20, 20, 344, 280), 14, 14);
            }
        }

        private void DrawCardSigil(DrawingContext dc)
        {
            Brush chip = new SolidColorBrush(Color.FromArgb(190, 0, 0, 0));
            dc.DrawRoundedRectangle(chip, null, new Rect(16, 250, 352, 50), 14, 14);
            DrawText(dc, card.RarityName.ToUpperInvariant(), 28, 260, 18, new SolidColorBrush(Color.FromRgb(240, 236, 210)));
            DrawText(dc, card.EffectText, 220, 260, 18, new SolidColorBrush(Color.FromRgb(185, 250, 240)));
        }

        private Geometry Wave(double x1, double y1, double x2, double y2, double amp)
        {
            StreamGeometry g = new StreamGeometry();
            using (StreamGeometryContext ctx = g.Open())
            {
                ctx.BeginFigure(new Point(x1, y1), false, false);
                double width = x2 - x1;
                for (int i = 0; i < 5; i++)
                {
                    double sx = x1 + width * i / 5;
                    ctx.BezierTo(new Point(sx + width / 15, y1 - amp), new Point(sx + width / 7, y1 + amp),
                        new Point(sx + width / 5, y2), true, false);
                }
            }
            return g;
        }

        private void DrawPetal(DrawingContext dc, Point center, double width, double height, Color color, double angle)
        {
            StreamGeometry petal = new StreamGeometry();
            using (StreamGeometryContext ctx = petal.Open())
            {
                ctx.BeginFigure(new Point(center.X, center.Y - height / 2), true, true);
                ctx.BezierTo(new Point(center.X + width, center.Y - height / 4),
                    new Point(center.X + width, center.Y + height / 4),
                    new Point(center.X, center.Y + height / 2), true, false);
                ctx.BezierTo(new Point(center.X - width, center.Y + height / 4),
                    new Point(center.X - width, center.Y - height / 4),
                    new Point(center.X, center.Y - height / 2), true, false);
            }

            TransformGroup tg = new TransformGroup();
            tg.Children.Add(new RotateTransform(angle, center.X, center.Y));
            petal.Transform = tg;
            dc.DrawGeometry(new SolidColorBrush(color), null, petal);
        }

        private void DrawText(DrawingContext dc, string text, double x, double y, double size, Brush brush)
        {
            FormattedText formatted = new FormattedText(
                text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI"),
                size,
                brush,
                1);
            dc.DrawText(formatted, new Point(x, y));
        }

        private Color GetFactionColor(string faction)
        {
            switch (faction)
            {
                case "Пламя": return Color.FromRgb(216, 72, 54);
                case "Река": return Color.FromRgb(50, 135, 170);
                case "Болото": return Color.FromRgb(72, 128, 82);
                case "Лес": return Color.FromRgb(55, 145, 82);
                case "Север": return Color.FromRgb(110, 180, 220);
                case "Небо": return Color.FromRgb(120, 134, 215);
                case "Судьба": return Color.FromRgb(190, 142, 68);
                case "Тьма": return Color.FromRgb(88, 66, 126);
                case "Кость": return Color.FromRgb(150, 138, 112);
                case "Сталь": return Color.FromRgb(125, 145, 160);
                case "Гроза": return Color.FromRgb(104, 118, 210);
                case "Камень": return Color.FromRgb(128, 120, 110);
                case "Поле": return Color.FromRgb(190, 150, 60);
                default: return Color.FromRgb(120, 90, 140);
            }
        }

        private Color Blend(Color a, Color b, double amount)
        {
            byte r = (byte)(a.R + (b.R - a.R) * amount);
            byte g = (byte)(a.G + (b.G - a.G) * amount);
            byte bl = (byte)(a.B + (b.B - a.B) * amount);
            return Color.FromRgb(r, g, bl);
        }

        private void CardControl_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            PlayFlipAnimation();
            OnCardClicked?.Invoke(this, card);
        }

        private void CardControl_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OnCardDoubleClicked?.Invoke(this, card);
        }

        private void CardControl_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            PlayGlowAnimation();
        }

        private void CardControl_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            StopGlowAnimation();
        }

        private void PlayGlowAnimation()
        {
            if (glowAnimation != null && CardBorder != null)
                glowAnimation.Begin(CardBorder);
        }

        private void StopGlowAnimation()
        {
            if (glowAnimation != null && CardBorder != null)
                glowAnimation.Stop(CardBorder);

            if (CardBorder != null)
                CardBorder.RenderTransform = new ScaleTransform(1, 1);
        }

        private void PlayFlipAnimation()
        {
            if (CardBorder == null)
                return;

            ScaleTransform scale = CardBorder.RenderTransform as ScaleTransform;
            if (scale == null)
            {
                scale = new ScaleTransform(1, 1);
                CardBorder.RenderTransform = scale;
            }

            DoubleAnimation pulse = new DoubleAnimation(1, 0.96, TimeSpan.FromMilliseconds(90))
            {
                AutoReverse = true
            };
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, pulse);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, pulse);
        }

        public void SetSelectState(bool isSelected)
        {
            IsSelected = isSelected;

            if (CardBorder == null) return;

            if (isSelected)
            {
                CardBorder.BorderThickness = new Thickness(4);
                CardBorder.BorderBrush = Brushes.White;
                CardBorder.Effect = new DropShadowEffect { Color = Colors.Gold, BlurRadius = 22, Opacity = 0.82 };
            }
            else
            {
                CardBorder.BorderThickness = new Thickness(2);
                CardBorder.BorderBrush = card?.GetRarityColor() ?? Brushes.Gray;
                CardBorder.Effect = null;
            }
        }
    }
}
