using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Tanki
{
    public class Renderer
    {
        private enum TextAlign
        {
            Left,
            Center,
            Right
        }

        private readonly int _width;
        private readonly int _height;
        private readonly WriteableBitmap _bitmap;
        private readonly int[] _pixels;
        private readonly int[] _backgroundPixels;
        private readonly Dictionary<char, string[]> _font = new Dictionary<char, string[]>();

        public WriteableBitmap Bitmap => _bitmap;

        public Renderer(int width, int height)
        {
            _width = width;
            _height = height;
            _bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgr32, null);
            _pixels = new int[width * height];
            _backgroundPixels = new int[width * height];

            InitFont();
            BuildBackground();
        }

        public void Render(GameEngine game)
        {
            Buffer.BlockCopy(_backgroundPixels, 0, _pixels, 0, _backgroundPixels.Length * sizeof(int));

            DrawMap(game);
            DrawBasesAura(game);
            DrawCoins(game);
            DrawBonuses(game);
            DrawBullets(game);
            DrawTanks(game);
            DrawExplosions(game);
            DrawBushes(game);
            DrawHud(game);
            DrawMissionPanel(game);
            DrawAnnouncement(game);
            DrawOverlay(game);

            _bitmap.WritePixels(new Int32Rect(0, 0, _width, _height), _pixels, _width * 4, 0);
        }

        private void BuildBackground()
        {
            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    int checker = ((x / 32) + (y / 32)) % 2;
                    int stripe = ((x * 13) + (y * 7)) & 15;
                    byte r = (byte)(18 + checker * 4 + stripe / 6);
                    byte g = (byte)(22 + checker * 5 + stripe / 5);
                    byte b = (byte)(26 + checker * 6 + stripe / 4);
                    _backgroundPixels[(y * _width) + x] = Pack(r, g, b);
                }
            }
        }

        private void DrawMap(GameEngine game)
        {
            int rows = game.Map.GetLength(1);
            int cols = game.Map.GetLength(0);
            int tile = GameEngine.TileSize;

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    MapCell cell = game.Map[x, y];
                    int px = x * tile;
                    int py = y * tile;

                    if (cell.Type == CellType.Empty)
                    {
                        DrawGroundDetail(px, py, x, y);
                        continue;
                    }

                    if (cell.Type == CellType.Bush)
                        continue;

                    switch (cell.Type)
                    {
                        case CellType.Brick:
                            DrawBrickTile(px, py, cell.Health);
                            break;

                        case CellType.Concrete:
                            DrawConcreteTile(px, py);
                            break;

                        case CellType.Water:
                            DrawWaterTile(px, py, game.FrameCounter);
                            break;

                        case CellType.Ice:
                            DrawIceTile(px, py, game.FrameCounter);
                            break;

                        case CellType.HeadquartersTop:
                            DrawHeadquarters(px, py, true, game.TopBaseHealth);
                            break;

                        case CellType.HeadquartersBottom:
                            DrawHeadquarters(px, py, false, game.BottomBaseHealth);
                            break;
                    }
                }
            }
        }

        private void DrawGroundDetail(int px, int py, int tileX, int tileY)
        {
            if (((tileX * 7) + (tileY * 11)) % 9 == 0)
                FillRect(px + 2, py + 2, 4, 4, Color.FromRgb(28, 34, 40));

            if (((tileX * 5) + (tileY * 3)) % 11 == 0)
                FillRect(px + 10, py + 9, 3, 3, Color.FromRgb(32, 38, 44));
        }

        private void DrawBrickTile(int x, int y, int health)
        {
            FillRect(x, y, 16, 16, Color.FromRgb(124, 58, 32));
            FillRect(x, y, 16, 2, Color.FromRgb(165, 98, 58));
            FillRect(x, y + 14, 16, 2, Color.FromRgb(90, 36, 18));

            for (int row = 0; row < 4; row++)
            {
                int mortarY = y + (row * 4);
                FillRect(x, mortarY, 16, 1, Color.FromRgb(84, 38, 22));
            }

            FillRect(x + 7, y + 1, 1, 14, Color.FromRgb(90, 36, 18));
            FillRect(x + 3, y + 5, 1, 6, Color.FromRgb(90, 36, 18));
            FillRect(x + 11, y + 9, 1, 6, Color.FromRgb(90, 36, 18));

            if (health <= 0)
                DrawRectBorder(x + 2, y + 2, 12, 12, Color.FromRgb(255, 180, 120), 1);
        }

        private void DrawConcreteTile(int x, int y)
        {
            FillRect(x, y, 16, 16, Color.FromRgb(96, 103, 114));
            FillRect(x, y, 16, 2, Color.FromRgb(144, 154, 168));
            FillRect(x, y + 14, 16, 2, Color.FromRgb(64, 68, 78));
            FillRect(x, y, 2, 16, Color.FromRgb(132, 140, 152));
            FillRect(x + 14, y, 2, 16, Color.FromRgb(62, 68, 76));
            FillRect(x + 4, y + 4, 8, 8, Color.FromRgb(116, 123, 136));
            FillCircle(x + 4, y + 4, 1, Color.FromRgb(58, 64, 74));
            FillCircle(x + 11, y + 4, 1, Color.FromRgb(58, 64, 74));
            FillCircle(x + 4, y + 11, 1, Color.FromRgb(58, 64, 74));
            FillCircle(x + 11, y + 11, 1, Color.FromRgb(58, 64, 74));
        }

        private void DrawWaterTile(int x, int y, int frame)
        {
            FillRect(x, y, 16, 16, Color.FromRgb(18, 62, 130));
            FillRect(x, y + 10, 16, 6, Color.FromRgb(12, 42, 92));

            for (int row = 0; row < 4; row++)
            {
                int offset = (frame / 3 + row * 3) % 12;
                FillRect(x + offset, y + 2 + (row * 3), 4, 1, Color.FromRgb(132, 214, 255), 170);
                FillRect(x + ((offset + 6) % 12), y + 3 + (row * 3), 3, 1, Color.FromRgb(55, 155, 230), 150);
            }
        }

        private void DrawIceTile(int x, int y, int frame)
        {
            FillRect(x, y, 16, 16, Color.FromRgb(186, 228, 255));
            FillRect(x, y + 10, 16, 6, Color.FromRgb(154, 206, 245));
            DrawLine(x + 1, y + 4, x + 14, y + 1, Color.FromRgb(255, 255, 255));
            DrawLine(x + 2, y + 9, x + 14, y + 5, Color.FromRgb(220, 247, 255));
            DrawLine(x + 1, y + 15 - (frame / 20 % 2), x + 15, y + 10 - (frame / 20 % 2), Color.FromRgb(210, 243, 255));
        }

        private void DrawHeadquarters(int x, int y, bool top, int health)
        {
            Color baseColor = top ? Color.FromRgb(220, 192, 64) : Color.FromRgb(76, 230, 170);
            Color trimColor = top ? Color.FromRgb(255, 232, 128) : Color.FromRgb(166, 255, 222);

            FillRect(x, y, 16, 16, baseColor);
            DrawRectBorder(x, y, 16, 16, trimColor, 2);
            FillRect(x + 5, y + 5, 6, 6, Color.FromRgb(24, 28, 34));
            FillRect(x + 7, y + 3, 2, 10, trimColor);
            FillRect(x + 3, y + 7, 10, 2, trimColor);

            if (health <= 1)
                DrawRectBorder(x + 1, y + 1, 14, 14, Color.FromRgb(255, 80, 80), 1);
        }

        private void DrawBasesAura(GameEngine game)
        {
            int pulse = 80 + (int)(35 * Math.Sin(game.FrameCounter / 8.0));
            if (game.TopBaseHealth > 0)
                FillCircle((game.MapWidth / 2) * GameEngine.TileSize, (2 * GameEngine.TileSize), 24, Color.FromRgb(255, 210, 80), (byte)pulse);

            if (game.BottomBaseHealth > 0)
                FillCircle((game.MapWidth / 2) * GameEngine.TileSize, (game.MapHeight - 2) * GameEngine.TileSize, 24, Color.FromRgb(72, 255, 186), (byte)pulse);
        }

        private void DrawBushes(GameEngine game)
        {
            int rows = game.Map.GetLength(1);
            int cols = game.Map.GetLength(0);

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    if (game.Map[x, y].Type != CellType.Bush)
                        continue;

                    int px = x * GameEngine.TileSize;
                    int py = y * GameEngine.TileSize;
                    FillCircle(px + 5, py + 6, 4, Color.FromRgb(45, 126, 52), 220);
                    FillCircle(px + 10, py + 6, 4, Color.FromRgb(60, 152, 66), 220);
                    FillCircle(px + 7, py + 11, 4, Color.FromRgb(36, 100, 43), 210);
                    FillRect(px + 3, py + 8, 9, 4, Color.FromRgb(25, 74, 34), 150);
                }
            }
        }

        private void DrawBullets(GameEngine game)
        {
            for (int i = 0; i < game.Bullets.Count; i++)
            {
                Bullet bullet = game.Bullets[i];
                Color core = bullet.OwnerTeam == 2 ? Color.FromRgb(255, 110, 110) : Color.FromRgb(255, 235, 120);
                FillCircle((int)bullet.X + 2, (int)bullet.Y + 2, 4, core, 70);
                FillRect((int)bullet.X, (int)bullet.Y, 6, 6, core);

                if (bullet.Direction == Direction.Up || bullet.Direction == Direction.Down)
                    FillRect((int)bullet.X + 2, (int)bullet.Y - 4, 2, 12, Color.FromRgb(255, 255, 255), 120);
                else
                    FillRect((int)bullet.X - 4, (int)bullet.Y + 2, 12, 2, Color.FromRgb(255, 255, 255), 120);
            }
        }

        private void DrawTanks(GameEngine game)
        {
            DrawTank(game.Player1, Color.FromRgb(52, 194, 126), Color.FromRgb(135, 252, 198), Color.FromRgb(12, 60, 39), game.FrameCounter);

            for (int i = 0; i < game.Bots.Count; i++)
            {
                Tank bot = game.Bots[i];
                Color hull = bot.IsElite ? Color.FromRgb(244, 112, 70) : Color.FromRgb(196, 82, 60);
                Color trim = bot.IsElite ? Color.FromRgb(255, 228, 134) : Color.FromRgb(255, 148, 108);
                DrawTank(bot, hull, trim, Color.FromRgb(86, 26, 18), game.FrameCounter);
            }
        }

        private void DrawTank(Tank tank, Color hullColor, Color trimColor, Color darkColor, int frame)
        {
            int x = (int)tank.X;
            int y = (int)tank.Y;

            if (tank.IsDead)
            {
                FillRect(x + 4, y + 10, 24, 10, Color.FromRgb(55, 55, 58), 220);
                DrawRectBorder(x + 6, y + 8, 20, 14, Color.FromRgb(92, 92, 98), 1);
                DrawLine(x + 8, y + 11, x + 24, y + 21, Color.FromRgb(122, 78, 54));
                return;
            }

            int pulse = tank.JustLeveledUp ? 90 + (int)(50 * Math.Sin(frame / 4.0)) : 0;

            FillRect(x + 3, y + 28, 26, 4, Color.FromRgb(0, 0, 0), 90);
            FillRect(x + 2, y + 5, 5, 22, darkColor);
            FillRect(x + 25, y + 5, 5, 22, darkColor);
            FillRect(x + 4, y + 7, 2, 18, Color.FromRgb(188, 188, 188), 70);
            FillRect(x + 26, y + 7, 2, 18, Color.FromRgb(188, 188, 188), 70);

            FillRect(x + 7, y + 6, 18, 20, hullColor);
            FillRect(x + 10, y + 3, 12, 26, hullColor);
            DrawRectBorder(x + 7, y + 6, 18, 20, trimColor, 2);
            DrawRectBorder(x + 10, y + 3, 12, 26, trimColor, 1);
            FillRect(x + 11, y + 9, 10, 10, Color.FromRgb(18, 26, 36));
            DrawRectBorder(x + 11, y + 9, 10, 10, trimColor, 1);

            FillCircle(x + 16, y + 16, 6, trimColor);
            FillCircle(x + 16, y + 16, 4, hullColor);

            DrawCannon(tank.Direction, x, y, trimColor, darkColor);
            DrawTankChevron(tank.Direction, x, y, trimColor);
            DrawHealthBar(tank, x, y);
            DrawLevelPips(tank, x, y, trimColor);

            if (tank.IsElite)
                DrawEliteMark(x + 16, y - 11, frame);

            if (tank.ShieldTimer > 0)
            {
                int shieldPulse = 60 + (int)(40 * Math.Sin(frame / 5.0));
                DrawCircleRing(x + 16, y + 16, 19, Color.FromRgb(110, 255, 255), (byte)shieldPulse);
                DrawCircleRing(x + 16, y + 16, 15, Color.FromRgb(205, 255, 255), (byte)(shieldPulse - 10));
            }

            if (pulse > 0)
                DrawCircleRing(x + 16, y + 16, 21, Color.FromRgb(255, 238, 128), (byte)pulse);
        }

        private void DrawEliteMark(int centerX, int y, int frame)
        {
            int bob = (frame / 12) % 2;
            FillRect(centerX - 8, y - bob, 16, 5, Color.FromRgb(34, 18, 10), 210);
            FillRect(centerX - 6, y + 1 - bob, 3, 2, Color.FromRgb(255, 220, 118));
            FillRect(centerX - 1, y - 1 - bob, 3, 4, Color.FromRgb(255, 220, 118));
            FillRect(centerX + 4, y + 1 - bob, 3, 2, Color.FromRgb(255, 220, 118));
        }

        private void DrawCannon(Direction direction, int x, int y, Color trimColor, Color darkColor)
        {
            if (direction == Direction.Up)
            {
                FillRect(x + 14, y - 4, 4, 16, darkColor);
                FillRect(x + 15, y - 6, 2, 4, trimColor);
            }
            else if (direction == Direction.Down)
            {
                FillRect(x + 14, y + 20, 4, 16, darkColor);
                FillRect(x + 15, y + 30, 2, 4, trimColor);
            }
            else if (direction == Direction.Left)
            {
                FillRect(x - 4, y + 14, 16, 4, darkColor);
                FillRect(x - 6, y + 15, 4, 2, trimColor);
            }
            else
            {
                FillRect(x + 20, y + 14, 16, 4, darkColor);
                FillRect(x + 30, y + 15, 4, 2, trimColor);
            }
        }

        private void DrawTankChevron(Direction direction, int x, int y, Color color)
        {
            if (direction == Direction.Up)
            {
                DrawLine(x + 11, y + 13, x + 16, y + 8, color);
                DrawLine(x + 21, y + 13, x + 16, y + 8, color);
            }
            else if (direction == Direction.Down)
            {
                DrawLine(x + 11, y + 19, x + 16, y + 24, color);
                DrawLine(x + 21, y + 19, x + 16, y + 24, color);
            }
            else if (direction == Direction.Left)
            {
                DrawLine(x + 13, y + 11, x + 8, y + 16, color);
                DrawLine(x + 13, y + 21, x + 8, y + 16, color);
            }
            else
            {
                DrawLine(x + 19, y + 11, x + 24, y + 16, color);
                DrawLine(x + 19, y + 21, x + 24, y + 16, color);
            }
        }

        private void DrawHealthBar(Tank tank, int x, int y)
        {
            FillRect(x, y - 8, 32, 4, Color.FromRgb(46, 18, 20), 220);
            int hpWidth = Math.Max(0, (int)(32 * (tank.Health / (double)Math.Max(1, tank.MaxHealth))));
            FillRect(x, y - 8, hpWidth, 4, tank.TeamId == 2 ? Color.FromRgb(255, 112, 84) : Color.FromRgb(96, 255, 140), 240);
        }

        private void DrawLevelPips(Tank tank, int x, int y, Color color)
        {
            for (int i = 0; i < tank.Level; i++)
                FillRect(x + 6 + (i * 5), y + 31, 3, 2, color);
        }

        private void DrawCoins(GameEngine game)
        {
            for (int i = 0; i < game.Coins.Count; i++)
            {
                Coin coin = game.Coins[i];
                int x = (int)coin.X;
                int y = (int)coin.Y;
                int bob = coin.AnimFrame % 2;
                FillCircle(x + 6, y + 6 - bob, 6, Color.FromRgb(244, 198, 58));
                DrawCircleRing(x + 6, y + 6 - bob, 5, Color.FromRgb(255, 242, 160), 255);
                DrawText(coin.Value.ToString(), x + 6, y + 3 - bob, Color.FromRgb(72, 48, 12), 1, TextAlign.Center);

                if ((coin.SparkleTimer / 10) % 3 == 0)
                {
                    FillRect(x + 2, y + 1, 2, 2, Color.FromRgb(255, 255, 255));
                    FillRect(x + 10, y + 9, 2, 2, Color.FromRgb(255, 255, 255));
                }
            }
        }

        private void DrawBonuses(GameEngine game)
        {
            for (int i = 0; i < game.Bonuses.Count; i++)
            {
                Bonus bonus = game.Bonuses[i];
                if (!bonus.Visible)
                    continue;

                int x = (int)bonus.X;
                int y = (int)bonus.Y;
                Color box = GetBonusColor(bonus.Type);
                FillRect(x, y, 20, 20, Color.FromRgb(18, 24, 32), 220);
                DrawRectBorder(x, y, 20, 20, box, 2);
                FillRect(x + 3, y + 3, 14, 14, box, 90);
                DrawText(GetBonusLabel(bonus.Type), x + 10, y + 6, Color.FromRgb(255, 255, 255), 1, TextAlign.Center);
            }
        }

        private static Color GetBonusColor(BonusType type)
        {
            switch (type)
            {
                case BonusType.Star:
                    return Color.FromRgb(255, 208, 88);
                case BonusType.Shield:
                    return Color.FromRgb(98, 220, 255);
                case BonusType.Grenade:
                    return Color.FromRgb(255, 120, 92);
                case BonusType.Life:
                    return Color.FromRgb(120, 250, 160);
                case BonusType.Freeze:
                    return Color.FromRgb(175, 230, 255);
                default:
                    return Color.FromRgb(190, 186, 255);
            }
        }

        private static string GetBonusLabel(BonusType type)
        {
            switch (type)
            {
                case BonusType.Star:
                    return "UP";
                case BonusType.Shield:
                    return "SH";
                case BonusType.Grenade:
                    return "BO";
                case BonusType.Life:
                    return "1+";
                case BonusType.Freeze:
                    return "FR";
                default:
                    return "FT";
            }
        }

        private void DrawExplosions(GameEngine game)
        {
            for (int i = 0; i < game.Explosions.Count; i++)
            {
                Explosion explosion = game.Explosions[i];
                int x = (int)explosion.X;
                int y = (int)explosion.Y;
                int radius = explosion.Big ? 14 + explosion.Timer : 6 + explosion.Timer;
                byte alpha = (byte)Math.Max(40, 240 - (explosion.Timer * 12));

                FillCircle(x, y, radius, Color.FromRgb(255, 146, 62), alpha);
                FillCircle(x, y, Math.Max(2, radius - 4), Color.FromRgb(255, 232, 128), (byte)Math.Min(255, alpha + 15));
                DrawCircleRing(x, y, radius + 2, Color.FromRgb(255, 90, 36), (byte)Math.Max(30, alpha - 30));
            }
        }

        private void DrawHud(GameEngine game)
        {
            FillRect(12, 12, 776, 72, Color.FromRgb(8, 12, 18), 210);
            DrawRectBorder(12, 12, 776, 72, Color.FromRgb(48, 84, 116), 2);
            FillRect(24, 24, 316, 48, Color.FromRgb(26, 44, 34), 170);
            FillRect(360, 24, 180, 48, Color.FromRgb(24, 34, 48), 170);
            FillRect(560, 24, 216, 48, Color.FromRgb(38, 28, 24), 170);

            DrawText("COMMANDER", 34, 30, Color.FromRgb(112, 255, 174), 2, TextAlign.Left);
            DrawText("SCORE " + game.Player1Score, 34, 48, Color.FromRgb(228, 255, 236), 1, TextAlign.Left);
            DrawText("LIVES " + game.Player1Lives + " LVL " + game.Player1.Level, 34, 60, Color.FromRgb(184, 244, 204), 1, TextAlign.Left);

            DrawText("STAGE " + (game.CurrentStageIndex + 1), 370, 30, Color.FromRgb(188, 226, 255), 2, TextAlign.Left);
            DrawText("LEFT " + game.StageEnemiesRemaining, 370, 48, Color.FromRgb(222, 240, 255), 1, TextAlign.Left);
            DrawText("ACTIVE " + game.Bots.Count, 370, 60, Color.FromRgb(182, 214, 244), 1, TextAlign.Left);

            DrawText("MISSION " + game.CurrentMission.Number, 570, 28, Color.FromRgb(255, 190, 160), 2, TextAlign.Left);
            DrawText("FIELD " + game.EnemiesDestroyed + " OF " + game.TotalEnemies, 570, 48, Color.FromRgb(255, 230, 214), 1, TextAlign.Left);
            DrawText("HQ " + game.TopBaseHealth + " " + game.BottomBaseHealth, 570, 60, Color.FromRgb(255, 210, 170), 1, TextAlign.Left);

            if (game.EnemyFrozen)
                DrawText("FREEZE " + (game.EnemyFreezeTimer / 60), 708, 28, Color.FromRgb(198, 242, 255), 1, TextAlign.Right);

            if (game.FortifyTimer > 0)
                DrawText("FORT " + (game.FortifyTimer / 60), 708, 40, Color.FromRgb(220, 216, 255), 1, TextAlign.Right);

            DrawPlayerStatusBar(34, 18, game.Player1, Color.FromRgb(92, 255, 152));
        }

        private void DrawPlayerStatusBar(int x, int y, Tank tank, Color color)
        {
            FillRect(x + 56, y + 44, 98, 6, Color.FromRgb(42, 12, 14), 180);
            int width = (int)(98 * (tank.Health / (double)Math.Max(1, tank.MaxHealth)));
            FillRect(x + 56, y + 44, width, 6, color, 240);
            DrawRectBorder(x + 56, y + 44, 98, 6, Color.FromRgb(255, 255, 255), 1);
        }

        private void DrawMissionPanel(GameEngine game)
        {
            FillRect(16, 452, 352, 134, Color.FromRgb(10, 15, 22), 216);
            DrawRectBorder(16, 452, 352, 134, Color.FromRgb(76, 110, 146), 2);
            FillRect(28, 464, 328, 22, Color.FromRgb(24, 34, 48), 180);

            DrawText("GOALS", 30, 485, Color.FromRgb(212, 236, 255), 2, TextAlign.Left);
            DrawText("M " + game.CurrentMission.Number, 346, 485, Color.FromRgb(255, 216, 152), 1, TextAlign.Right);
            DrawText(game.CurrentMission.CodeName, 30, 506, Color.FromRgb(255, 216, 152), 1, TextAlign.Left);

            string[] lines = game.MissionGoalLines;
            int y = 522;
            for (int i = 1; i < lines.Length && i < 6; i++)
            {
                DrawText(lines[i], 30, y, Color.FromRgb(222, 234, 244), 1, TextAlign.Left);
                y += 14;
            }

            DrawText(game.CurrentMission.Title, 30, 570, Color.FromRgb(152, 220, 255), 1, TextAlign.Left);
        }

        private void DrawAnnouncement(GameEngine game)
        {
            if (string.IsNullOrEmpty(game.AnnouncementText))
                return;

            FillRect(250, 96, 300, 28, Color.FromRgb(8, 12, 16), 220);
            DrawRectBorder(250, 96, 300, 28, Color.FromRgb(255, 196, 124), 2);
            DrawText(game.AnnouncementText, 400, 104, Color.FromRgb(255, 236, 188), 2, TextAlign.Center);
        }

        private void DrawOverlay(GameEngine game)
        {
            if (game.State == GameState.Running)
                return;

            FillRect(0, 0, _width, _height, Color.FromRgb(0, 0, 0), 168);

            if (game.State == GameState.Briefing)
            {
                FillRect(110, 90, 580, 420, Color.FromRgb(10, 18, 28), 236);
                DrawRectBorder(110, 90, 580, 420, Color.FromRgb(88, 136, 182), 3);
                FillRect(132, 114, 536, 46, Color.FromRgb(22, 36, 52), 200);

                DrawText("MISSION " + game.CurrentMission.Number, 144, 124, Color.FromRgb(255, 226, 164), 2, TextAlign.Left);
                DrawText(game.CurrentMission.CodeName, 144, 144, Color.FromRgb(220, 238, 255), 2, TextAlign.Left);
                DrawText(game.CurrentMission.Title, 400, 176, Color.FromRgb(160, 226, 255), 2, TextAlign.Center);

                DrawText("BRIEFING", 144, 214, Color.FromRgb(255, 222, 170), 2, TextAlign.Left);
                DrawTextLines(game.MissionBriefingLines, 144, 238, Color.FromRgb(230, 238, 244), 1, 18);

                DrawText("GOALS", 144, 320, Color.FromRgb(255, 222, 170), 2, TextAlign.Left);
                DrawTextLines(game.MissionGoalLines, 144, 344, Color.FromRgb(214, 226, 236), 1, 18);

                DrawText(game.MissionHintLine, 400, 436, Color.FromRgb(166, 232, 192), 1, TextAlign.Center);
                DrawText("ENTER TO DEPLOY", 400, 468, Color.FromRgb(255, 240, 198), 2, TextAlign.Center);
                DrawText("P PAUSE  R RESTART", 400, 492, Color.FromRgb(186, 210, 232), 1, TextAlign.Center);
                return;
            }

            FillRect(170, 170, 460, 220, Color.FromRgb(12, 18, 26), 230);
            DrawRectBorder(170, 170, 460, 220, Color.FromRgb(92, 144, 188), 3);

            if (game.State == GameState.Paused)
            {
                DrawText("PAUSED", 400, 208, Color.FromRgb(220, 238, 255), 4, TextAlign.Center);
                DrawText(game.CurrentMission.CodeName, 400, 270, Color.FromRgb(180, 220, 255), 2, TextAlign.Center);
                DrawText("P TO CONTINUE", 400, 308, Color.FromRgb(182, 216, 255), 2, TextAlign.Center);
                DrawText("R TO RESTART", 400, 338, Color.FromRgb(182, 216, 255), 2, TextAlign.Center);
            }
            else if (game.State == GameState.Victory)
            {
                DrawText(game.CampaignCleared ? "CAMPAIGN CLEAR" : "MISSION CLEAR", 400, 206, Color.FromRgb(255, 232, 150), 4, TextAlign.Center);
                DrawText(game.CurrentMission.CodeName, 400, 264, Color.FromRgb(255, 244, 198), 2, TextAlign.Center);
                DrawText(game.CurrentMission.Title, 400, 294, Color.FromRgb(198, 234, 255), 1, TextAlign.Center);
                DrawText(game.CampaignCleared ? "ENTER FOR NEW RUN" : "ENTER FOR NEXT MISSION", 400, 334, Color.FromRgb(255, 244, 198), 2, TextAlign.Center);
                DrawText("R TO REPLAY THIS MISSION", 400, 362, Color.FromRgb(186, 210, 232), 1, TextAlign.Center);
            }
            else
            {
                DrawText("MISSION FAIL", 400, 206, Color.FromRgb(255, 148, 136), 4, TextAlign.Center);
                DrawText(game.CurrentMission.CodeName, 400, 264, Color.FromRgb(255, 220, 214), 2, TextAlign.Center);
                DrawText(game.MissionFailureReason, 400, 298, Color.FromRgb(255, 196, 160), 2, TextAlign.Center);
                DrawText("ENTER FOR BRIEFING", 400, 338, Color.FromRgb(255, 230, 198), 2, TextAlign.Center);
                DrawText("R TO RESTART FAST", 400, 366, Color.FromRgb(186, 210, 232), 1, TextAlign.Center);
            }
        }

        private void DrawTextLines(string[] lines, int x, int y, Color color, int scale, int lineHeight)
        {
            for (int i = 0; i < lines.Length; i++)
                DrawText(lines[i], x, y + (i * lineHeight), color, scale, TextAlign.Left);
        }

        private void FillRect(int x, int y, int width, int height, Color color, byte alpha = 255)
        {
            int startY = Math.Max(0, y);
            int endY = Math.Min(_height, y + height);
            int startX = Math.Max(0, x);
            int endX = Math.Min(_width, x + width);

            for (int py = startY; py < endY; py++)
            {
                int rowIndex = py * _width;
                for (int px = startX; px < endX; px++)
                {
                    if (alpha == 255)
                        _pixels[rowIndex + px] = Pack(color.R, color.G, color.B);
                    else
                        BlendPixel(rowIndex + px, color, alpha);
                }
            }
        }

        private void DrawRectBorder(int x, int y, int width, int height, Color color, int thickness)
        {
            FillRect(x, y, width, thickness, color);
            FillRect(x, y + height - thickness, width, thickness, color);
            FillRect(x, y, thickness, height, color);
            FillRect(x + width - thickness, y, thickness, height, color);
        }

        private void FillCircle(int centerX, int centerY, int radius, Color color, byte alpha = 255)
        {
            int radiusSquared = radius * radius;
            int minX = Math.Max(0, centerX - radius);
            int maxX = Math.Min(_width - 1, centerX + radius);
            int minY = Math.Max(0, centerY - radius);
            int maxY = Math.Min(_height - 1, centerY + radius);

            for (int y = minY; y <= maxY; y++)
            {
                int dy = y - centerY;
                int rowIndex = y * _width;

                for (int x = minX; x <= maxX; x++)
                {
                    int dx = x - centerX;
                    if ((dx * dx) + (dy * dy) > radiusSquared)
                        continue;

                    if (alpha == 255)
                        _pixels[rowIndex + x] = Pack(color.R, color.G, color.B);
                    else
                        BlendPixel(rowIndex + x, color, alpha);
                }
            }
        }

        private void DrawCircleRing(int centerX, int centerY, int radius, Color color, byte alpha)
        {
            int outer = radius * radius;
            int inner = (radius - 2) * (radius - 2);
            int minX = Math.Max(0, centerX - radius);
            int maxX = Math.Min(_width - 1, centerX + radius);
            int minY = Math.Max(0, centerY - radius);
            int maxY = Math.Min(_height - 1, centerY + radius);

            for (int y = minY; y <= maxY; y++)
            {
                int dy = y - centerY;
                int rowIndex = y * _width;
                for (int x = minX; x <= maxX; x++)
                {
                    int dx = x - centerX;
                    int distance = (dx * dx) + (dy * dy);
                    if (distance > outer || distance < inner)
                        continue;

                    BlendPixel(rowIndex + x, color, alpha);
                }
            }
        }

        private void DrawLine(int x0, int y0, int x1, int y1, Color color)
        {
            int dx = Math.Abs(x1 - x0);
            int sx = x0 < x1 ? 1 : -1;
            int dy = -Math.Abs(y1 - y0);
            int sy = y0 < y1 ? 1 : -1;
            int error = dx + dy;

            while (true)
            {
                if (x0 >= 0 && x0 < _width && y0 >= 0 && y0 < _height)
                    _pixels[(y0 * _width) + x0] = Pack(color.R, color.G, color.B);

                if (x0 == x1 && y0 == y1)
                    break;

                int e2 = error * 2;
                if (e2 >= dy)
                {
                    error += dy;
                    x0 += sx;
                }

                if (e2 <= dx)
                {
                    error += dx;
                    y0 += sy;
                }
            }
        }

        private void DrawText(string text, int x, int y, Color color, int scale, TextAlign align)
        {
            if (string.IsNullOrEmpty(text))
                return;

            text = text.ToUpperInvariant();
            int width = MeasureText(text, scale);

            if (align == TextAlign.Center)
                x -= width / 2;
            else if (align == TextAlign.Right)
                x -= width;

            int cursorX = x;
            for (int i = 0; i < text.Length; i++)
            {
                DrawGlyph(text[i], cursorX, y, color, scale);
                cursorX += 6 * scale;
            }
        }

        private int MeasureText(string text, int scale)
        {
            return Math.Max(0, (text.Length * 6 - 1) * scale);
        }

        private void DrawGlyph(char character, int x, int y, Color color, int scale)
        {
            string[] glyph;
            if (!_font.TryGetValue(character, out glyph))
                glyph = _font['?'];

            for (int row = 0; row < glyph.Length; row++)
            {
                string line = glyph[row];
                for (int column = 0; column < line.Length; column++)
                {
                    if (line[column] != 'X')
                        continue;

                    FillRect(x + (column * scale), y + (row * scale), scale, scale, color);
                }
            }
        }

        private void BlendPixel(int index, Color color, byte alpha)
        {
            int dst = _pixels[index];
            int dr = (dst >> 16) & 0xFF;
            int dg = (dst >> 8) & 0xFF;
            int db = dst & 0xFF;
            int inv = 255 - alpha;

            int r = ((color.R * alpha) + (dr * inv)) / 255;
            int g = ((color.G * alpha) + (dg * inv)) / 255;
            int b = ((color.B * alpha) + (db * inv)) / 255;

            _pixels[index] = Pack((byte)r, (byte)g, (byte)b);
        }

        private static int Pack(byte r, byte g, byte b)
        {
            return (255 << 24) | (r << 16) | (g << 8) | b;
        }

        private void InitFont()
        {
            _font[' '] = new[] { ".....", ".....", ".....", ".....", ".....", ".....", "....." };
            _font['?'] = new[] { ".XXX.", "X...X", "...X.", "..X..", "..X..", ".....", "..X.." };
            _font['!'] = new[] { "..X..", "..X..", "..X..", "..X..", "..X..", ".....", "..X.." };
            _font['+'] = new[] { ".....", "..X..", "..X..", "XXXXX", "..X..", "..X..", "....." };
            _font['-'] = new[] { ".....", ".....", ".....", "XXXXX", ".....", ".....", "....." };
            _font[':'] = new[] { ".....", "..X..", ".....", ".....", "..X..", ".....", "....." };
            _font['.'] = new[] { ".....", ".....", ".....", ".....", ".....", ".....", "..X.." };
            _font['/'] = new[] { "....X", "...X.", "...X.", "..X..", ".X...", ".X...", "X...." };
            _font['0'] = new[] { ".XXX.", "X...X", "X..XX", "X.X.X", "XX..X", "X...X", ".XXX." };
            _font['1'] = new[] { "..X..", ".XX..", "..X..", "..X..", "..X..", "..X..", ".XXX." };
            _font['2'] = new[] { ".XXX.", "X...X", "....X", "...X.", "..X..", ".X...", "XXXXX" };
            _font['3'] = new[] { "XXXX.", "....X", "...X.", "..XX.", "....X", "X...X", ".XXX." };
            _font['4'] = new[] { "...X.", "..XX.", ".X.X.", "X..X.", "XXXXX", "...X.", "...X." };
            _font['5'] = new[] { "XXXXX", "X....", "XXXX.", "....X", "....X", "X...X", ".XXX." };
            _font['6'] = new[] { ".XXX.", "X...X", "X....", "XXXX.", "X...X", "X...X", ".XXX." };
            _font['7'] = new[] { "XXXXX", "....X", "...X.", "..X..", "..X..", "..X..", "..X.." };
            _font['8'] = new[] { ".XXX.", "X...X", "X...X", ".XXX.", "X...X", "X...X", ".XXX." };
            _font['9'] = new[] { ".XXX.", "X...X", "X...X", ".XXXX", "....X", "X...X", ".XXX." };
            _font['A'] = new[] { ".XXX.", "X...X", "X...X", "XXXXX", "X...X", "X...X", "X...X" };
            _font['B'] = new[] { "XXXX.", "X...X", "X...X", "XXXX.", "X...X", "X...X", "XXXX." };
            _font['C'] = new[] { ".XXXX", "X....", "X....", "X....", "X....", "X....", ".XXXX" };
            _font['D'] = new[] { "XXXX.", "X...X", "X...X", "X...X", "X...X", "X...X", "XXXX." };
            _font['E'] = new[] { "XXXXX", "X....", "X....", "XXXX.", "X....", "X....", "XXXXX" };
            _font['F'] = new[] { "XXXXX", "X....", "X....", "XXXX.", "X....", "X....", "X...." };
            _font['G'] = new[] { ".XXXX", "X....", "X....", "X.XXX", "X...X", "X...X", ".XXX." };
            _font['H'] = new[] { "X...X", "X...X", "X...X", "XXXXX", "X...X", "X...X", "X...X" };
            _font['I'] = new[] { "XXXXX", "..X..", "..X..", "..X..", "..X..", "..X..", "XXXXX" };
            _font['J'] = new[] { "..XXX", "...X.", "...X.", "...X.", "...X.", "X..X.", ".XX.." };
            _font['K'] = new[] { "X...X", "X..X.", "X.X..", "XX...", "X.X..", "X..X.", "X...X" };
            _font['L'] = new[] { "X....", "X....", "X....", "X....", "X....", "X....", "XXXXX" };
            _font['M'] = new[] { "X...X", "XX.XX", "X.X.X", "X.X.X", "X...X", "X...X", "X...X" };
            _font['N'] = new[] { "X...X", "XX..X", "XX..X", "X.X.X", "X..XX", "X..XX", "X...X" };
            _font['O'] = new[] { ".XXX.", "X...X", "X...X", "X...X", "X...X", "X...X", ".XXX." };
            _font['P'] = new[] { "XXXX.", "X...X", "X...X", "XXXX.", "X....", "X....", "X...." };
            _font['Q'] = new[] { ".XXX.", "X...X", "X...X", "X...X", "X.X.X", "X..X.", ".XX.X" };
            _font['R'] = new[] { "XXXX.", "X...X", "X...X", "XXXX.", "X.X..", "X..X.", "X...X" };
            _font['S'] = new[] { ".XXXX", "X....", "X....", ".XXX.", "....X", "....X", "XXXX." };
            _font['T'] = new[] { "XXXXX", "..X..", "..X..", "..X..", "..X..", "..X..", "..X.." };
            _font['U'] = new[] { "X...X", "X...X", "X...X", "X...X", "X...X", "X...X", ".XXX." };
            _font['V'] = new[] { "X...X", "X...X", "X...X", "X...X", ".X.X.", ".X.X.", "..X.." };
            _font['W'] = new[] { "X...X", "X...X", "X...X", "X.X.X", "X.X.X", "XX.XX", "X...X" };
            _font['X'] = new[] { "X...X", "X...X", ".X.X.", "..X..", ".X.X.", "X...X", "X...X" };
            _font['Y'] = new[] { "X...X", "X...X", ".X.X.", "..X..", "..X..", "..X..", "..X.." };
            _font['Z'] = new[] { "XXXXX", "....X", "...X.", "..X..", ".X...", "X....", "XXXXX" };
        }
    }
}
