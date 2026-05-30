using System;
using System.Collections.Generic;

namespace Tanki
{
    public enum GameState
    {
        Briefing,
        Running,
        Paused,
        Victory,
        Defeat
    }

    public class GameEngine
    {
        private struct FortifiedCell
        {
            public int X;
            public int Y;
            public CellType Type;
            public int Health;
        }

        public const int ScreenWidth = 800;
        public const int ScreenHeight = 600;
        public const int TileSize = 16;

        private readonly Random _rnd = new Random();
        private readonly List<FortifiedCell> _fortifiedCells = new List<FortifiedCell>();
        private readonly MissionDefinition[] _missions = MissionDefinition.CreateCampaign();

        private int _frameCounter;
        private int _spawnCooldown;
        private int _bonusCooldown;
        private int _enemyFreezeTimer;
        private int _fortifyTimer;
        private int _topHqX;
        private int _topHqY;
        private int _bottomHqX;
        private int _bottomHqY;
        private int _announcementTimer;
        private int _stageTransitionTimer;
        private int _stageEnemiesSpawned;
        private int _stageEnemiesDestroyed;
        private int _stageElitesSpawned;
        private int _stageElitesDestroyed;
        private int[] _spawnLaneCenters = Array.Empty<int>();
        private bool _missionUiDirty;
        private string _announcementText = string.Empty;
        private string _missionFailureReason = string.Empty;
        private string[] _missionGoalLines = Array.Empty<string>();

        public MapCell[,] Map { get; private set; }
        public Tank Player1 { get; private set; }
        public List<Tank> Bots { get; private set; }
        public List<Bullet> Bullets { get; private set; }
        public List<Bonus> Bonuses { get; private set; }
        public List<Coin> Coins { get; private set; }
        public List<Explosion> Explosions { get; private set; }
        public GameState State { get; private set; }
        public MissionDefinition CurrentMission { get; private set; }
        public int CurrentMissionIndex { get; private set; }
        public int CurrentStageIndex { get; private set; }
        public bool CampaignCleared { get; private set; }
        public int MapRevision { get; private set; }
        public int Player1Score { get; private set; }
        public int Player1Lives { get; private set; }
        public int TopBaseHealth { get; private set; }
        public int BottomBaseHealth { get; private set; }
        public int TotalEnemies { get; private set; }
        public int EnemiesDestroyed { get; private set; }
        public int EliteEnemiesDestroyed { get; private set; }
        public int BricksDestroyed { get; private set; }
        public int CoinsCollected { get; private set; }
        public int FrameCounter => _frameCounter;
        public bool EnemyFrozen => _enemyFreezeTimer > 0;
        public int EnemyFreezeTimer => _enemyFreezeTimer;
        public int FortifyTimer => _fortifyTimer;
        public int BasesAlive => (TopBaseHealth > 0 ? 1 : 0) + (BottomBaseHealth > 0 ? 1 : 0);
        public int MapWidth => Map.GetLength(0);
        public int MapHeight => Map.GetLength(1);
        public int StageCount => CurrentMission.Stages.Length;
        public MissionStageDefinition CurrentStage => CurrentMission.Stages[Math.Min(CurrentStageIndex, StageCount - 1)];
        public bool IsStageTransition => _stageTransitionTimer > 0;
        public int StageEnemiesRemaining => Math.Max(0, CurrentStage.EnemiesToSpawn - _stageEnemiesDestroyed);
        public int StageTransitionTimer => _stageTransitionTimer;
        public string AnnouncementText => _announcementTimer > 0 ? _announcementText : string.Empty;
        public string MissionFailureReason => _missionFailureReason;
        public string[] MissionBriefingLines => CurrentMission.BriefingLines;
        public string MissionHintLine => CurrentMission.HintLine;
        public bool P1Up;
        public bool P1Down;
        public bool P1Left;
        public bool P1Right;
        public bool P1Fire;

        public string[] MissionGoalLines
        {
            get
            {
                EnsureMissionUi();
                return _missionGoalLines;
            }
        }

        public GameEngine()
        {
            StartGame(0);
        }

        public void StartGame()
        {
            StartGame(CurrentMissionIndex);
        }

        public void StartGame(int missionIndex)
        {
            CurrentMissionIndex = Math.Max(0, Math.Min(_missions.Length - 1, missionIndex));
            CurrentMission = _missions[CurrentMissionIndex];
            CampaignCleared = false;

            _frameCounter = 0;
            _spawnCooldown = 0;
            _bonusCooldown = CurrentMission.BonusCooldownMin;
            _enemyFreezeTimer = 0;
            _fortifyTimer = 0;
            _announcementTimer = 0;
            _stageTransitionTimer = 0;
            _stageEnemiesSpawned = 0;
            _stageEnemiesDestroyed = 0;
            _stageElitesSpawned = 0;
            _stageElitesDestroyed = 0;
            _announcementText = string.Empty;
            _missionFailureReason = string.Empty;
            _fortifiedCells.Clear();
            ResetInput();

            State = GameState.Briefing;
            MapRevision = 1;
            CurrentStageIndex = 0;
            Player1Score = 0;
            Player1Lives = CurrentMission.StartingLives;
            TopBaseHealth = 3;
            BottomBaseHealth = 3;
            TotalEnemies = CurrentMission.TotalEnemies;
            EnemiesDestroyed = 0;
            EliteEnemiesDestroyed = 0;
            BricksDestroyed = 0;
            CoinsCollected = 0;

            Map = MapGenerator.Generate(
                ScreenWidth / TileSize,
                ScreenHeight / TileSize,
                _rnd,
                CurrentMission.Theme,
                out _topHqX,
                out _topHqY,
                out _bottomHqX,
                out _bottomHqY);

            Player1 = new Tank(_bottomHqX * TileSize, (_bottomHqY - 3) * TileSize, 0, "P1");
            Player1.Direction = Direction.Up;
            Player1.ShieldTimer = 180;

            Bullets = new List<Bullet>(96);
            Bots = new List<Tank>(16);
            Bonuses = new List<Bonus>(4);
            Coins = new List<Coin>(16);
            Explosions = new List<Explosion>(32);

            int mapTilesWide = ScreenWidth / TileSize;
            _spawnLaneCenters = new[]
            {
                (6 + 1) * TileSize,
                (mapTilesWide / 2) * TileSize,
                (mapTilesWide - 8) * TileSize
            };

            MarkMissionUiDirty();
        }

        public void BeginMission()
        {
            if (State != GameState.Briefing)
                return;

            State = GameState.Running;
            PrepareStage(0, true);
            SetAnnouncement("MISSION LIVE", 100);
        }

        public void TogglePause()
        {
            if (State == GameState.Running)
                State = GameState.Paused;
            else if (State == GameState.Paused)
                State = GameState.Running;
        }

        public void AdvanceMission()
        {
            if (State != GameState.Victory)
                return;

            if (CurrentMissionIndex >= _missions.Length - 1)
                StartGame(0);
            else
                StartGame(CurrentMissionIndex + 1);
        }

        public void Update()
        {
            if (State != GameState.Running)
                return;

            _frameCounter++;
            if (_announcementTimer > 0)
                _announcementTimer--;

            if (_enemyFreezeTimer > 0)
                _enemyFreezeTimer--;

            if (_fortifyTimer > 0)
            {
                _fortifyTimer--;
                if (_fortifyTimer == 0)
                    RestoreFortifications();
            }

            UpdatePlayer();

            for (int i = 0; i < Bots.Count; i++)
                UpdateBotAI(Bots[i]);

            UpdateBullets();
            UpdateBonuses();
            UpdateCoins();
            UpdateExplosions();
            TrySpawnBonus();
            UpdateStageFlow();
            CheckEndConditions();
        }

        private void ResetInput()
        {
            P1Up = false;
            P1Down = false;
            P1Left = false;
            P1Right = false;
            P1Fire = false;
        }

        private void PrepareStage(int stageIndex, bool immediate)
        {
            CurrentStageIndex = stageIndex;
            _stageEnemiesSpawned = 0;
            _stageEnemiesDestroyed = 0;
            _stageElitesSpawned = 0;
            _stageElitesDestroyed = 0;
            _spawnCooldown = immediate ? CurrentStage.StartDelayFrames : Math.Max(60, CurrentStage.StartDelayFrames);
            _stageTransitionTimer = 0;
            MarkMissionUiDirty();
            SetAnnouncement("STAGE " + (CurrentStageIndex + 1), 90);
        }

        private void UpdatePlayer()
        {
            Player1.Update();
            if (Player1.IsDead)
            {
                TryRespawnPlayer();
                return;
            }

            double speed = GetTankMoveSpeed(Player1);
            double dx = 0;
            double dy = 0;

            if (P1Up)
            {
                dy = -speed;
                Player1.Direction = Direction.Up;
            }
            else if (P1Down)
            {
                dy = speed;
                Player1.Direction = Direction.Down;
            }
            else if (P1Left)
            {
                dx = -speed;
                Player1.Direction = Direction.Left;
            }
            else if (P1Right)
            {
                dx = speed;
                Player1.Direction = Direction.Right;
            }

            TryMoveTank(Player1, dx, dy);

            if (P1Fire && Player1.FireCooldown <= 0)
                FireBullet(Player1);

            HandleBonusPickup(Player1);
            HandleCoinPickup(Player1);
        }

        private void TryRespawnPlayer()
        {
            if (!Player1.RespawnReady || BottomBaseHealth <= 0 || Player1Lives <= 0)
                return;

            Player1Lives--;
            Player1.Respawn();
            FindSafeRespawn(Player1);
            SetAnnouncement("BACK IN", 70);
            MarkMissionUiDirty();
        }

        private void FindSafeRespawn(Tank tank)
        {
            double[] offsets = { 0, -36, 36, -72, 72 };
            double spawnY = (_bottomHqY - 3) * TileSize;

            for (int i = 0; i < offsets.Length; i++)
            {
                double spawnX = tank.SpawnX + offsets[i];
                if (CanOccupy(tank, spawnX, spawnY))
                {
                    tank.X = spawnX;
                    tank.Y = spawnY;
                    return;
                }
            }

            tank.X = tank.SpawnX;
            tank.Y = spawnY;
        }

        private void UpdateStageFlow()
        {
            if (_stageTransitionTimer > 0)
            {
                _stageTransitionTimer--;
                if (_stageTransitionTimer == 0)
                {
                    int nextStage = CurrentStageIndex + 1;
                    if (nextStage >= StageCount)
                        WinMission();
                    else
                        PrepareStage(nextStage, false);
                }

                return;
            }

            if (_stageEnemiesSpawned >= CurrentStage.EnemiesToSpawn && Bots.Count == 0)
            {
                if (CurrentStageIndex >= StageCount - 1)
                {
                    if (CoreObjectivesCompleted())
                        WinMission();
                    else if (string.IsNullOrEmpty(AnnouncementText))
                        SetAnnouncement("FINISH OBJECTIVE", 80);
                }
                else
                {
                    _stageTransitionTimer = 150;
                    SetAnnouncement("STAGE CLEAR", 100);
                }

                MarkMissionUiDirty();
                return;
            }

            TrySpawnEnemies();
        }

        private void UpdateBotAI(Tank bot)
        {
            bot.Update();
            if (bot.IsDead || _enemyFreezeTimer > 0)
                return;

            Tank target = Player1.IsDead ? null : Player1;
            double targetX = target != null ? target.X : (_bottomHqX * TileSize);
            double targetY = target != null ? target.Y : (_bottomHqY * TileSize);

            if (bot.AiDecisionTimer <= 0)
            {
                bot.Direction = PickDirectionTowards(bot, targetX, targetY);
                if (_rnd.Next(100) < (bot.IsElite ? 8 : 14))
                    bot.Direction = (Direction)_rnd.Next(4);

                bot.AiDecisionTimer = bot.IsElite ? _rnd.Next(10, 18) : _rnd.Next(16, 28);
            }

            double beforeX = bot.X;
            double beforeY = bot.Y;
            double speed = GetTankMoveSpeed(bot) * (bot.IsElite ? 0.95 : 0.85);

            TryMoveTank(
                bot,
                bot.Direction == Direction.Left ? -speed : bot.Direction == Direction.Right ? speed : 0,
                bot.Direction == Direction.Up ? -speed : bot.Direction == Direction.Down ? speed : 0);

            if (Math.Abs(bot.X - beforeX) < 0.01 && Math.Abs(bot.Y - beforeY) < 0.01)
            {
                bot.Direction = (Direction)_rnd.Next(4);
                bot.AiDecisionTimer = _rnd.Next(8, 16);
            }

            if (bot.FireCooldown > 0)
                return;

            if (GetActiveEnemyBullets() >= CurrentStage.MaxEnemyBullets)
                return;

            bool cleanShot =
                HasClearShotToTank(bot, Player1) ||
                HasClearShotToBase(bot, _bottomHqX, _bottomHqY) ||
                (TopBaseHealth > 0 && HasClearShotToBase(bot, _topHqX, _topHqY));

            if (!cleanShot)
                return;

            int fireChance = bot.IsElite ? 4 : 2;
            if (_rnd.Next(100) < fireChance)
                FireBullet(bot);
        }

        private int GetActiveEnemyBullets()
        {
            int count = 0;
            for (int i = 0; i < Bullets.Count; i++)
            {
                if (Bullets[i].OwnerTeam == 2)
                    count++;
            }

            return count;
        }

        private bool HasClearShotToTank(Tank shooter, Tank target)
        {
            if (target == null || target.IsDead)
                return false;

            double centerX = shooter.X + (Tank.Size / 2.0);
            double centerY = shooter.Y + (Tank.Size / 2.0);
            double targetCenterX = target.X + (Tank.Size / 2.0);
            double targetCenterY = target.Y + (Tank.Size / 2.0);

            if (shooter.Direction == Direction.Up && targetCenterY < centerY && Math.Abs(targetCenterX - centerX) < 14)
                return IsVerticalLineClear(centerX, targetCenterY, centerY);
            if (shooter.Direction == Direction.Down && targetCenterY > centerY && Math.Abs(targetCenterX - centerX) < 14)
                return IsVerticalLineClear(centerX, centerY, targetCenterY);
            if (shooter.Direction == Direction.Left && targetCenterX < centerX && Math.Abs(targetCenterY - centerY) < 14)
                return IsHorizontalLineClear(targetCenterX, centerX, centerY);
            if (shooter.Direction == Direction.Right && targetCenterX > centerX && Math.Abs(targetCenterY - centerY) < 14)
                return IsHorizontalLineClear(centerX, targetCenterX, centerY);

            return false;
        }

        private bool HasClearShotToBase(Tank shooter, int hqTileX, int hqTileY)
        {
            double centerX = shooter.X + (Tank.Size / 2.0);
            double centerY = shooter.Y + (Tank.Size / 2.0);
            double targetX = (hqTileX * TileSize) + (TileSize / 2.0);
            double targetY = (hqTileY * TileSize) + (TileSize / 2.0);

            if (shooter.Direction == Direction.Up && targetY < centerY && Math.Abs(targetX - centerX) < 12)
                return IsVerticalLineClear(centerX, targetY, centerY);
            if (shooter.Direction == Direction.Down && targetY > centerY && Math.Abs(targetX - centerX) < 12)
                return IsVerticalLineClear(centerX, centerY, targetY);
            if (shooter.Direction == Direction.Left && targetX < centerX && Math.Abs(targetY - centerY) < 12)
                return IsHorizontalLineClear(targetX, centerX, centerY);
            if (shooter.Direction == Direction.Right && targetX > centerX && Math.Abs(targetY - centerY) < 12)
                return IsHorizontalLineClear(centerX, targetX, centerY);

            return false;
        }

        private bool IsVerticalLineClear(double x, double startY, double endY)
        {
            int tileX = (int)x / TileSize;
            int from = (int)Math.Min(startY, endY) / TileSize;
            int to = (int)Math.Max(startY, endY) / TileSize;

            for (int tileY = from; tileY <= to; tileY++)
            {
                if (!IsInsideMap(tileX, tileY))
                    return false;

                CellType type = Map[tileX, tileY].Type;
                if (type == CellType.Brick || type == CellType.Concrete || type == CellType.Water)
                    return false;
            }

            return true;
        }

        private bool IsHorizontalLineClear(double startX, double endX, double y)
        {
            int tileY = (int)y / TileSize;
            int from = (int)Math.Min(startX, endX) / TileSize;
            int to = (int)Math.Max(startX, endX) / TileSize;

            for (int tileX = from; tileX <= to; tileX++)
            {
                if (!IsInsideMap(tileX, tileY))
                    return false;

                CellType type = Map[tileX, tileY].Type;
                if (type == CellType.Brick || type == CellType.Concrete || type == CellType.Water)
                    return false;
            }

            return true;
        }

        private Direction PickDirectionTowards(Tank bot, double targetX, double targetY)
        {
            double dx = targetX - bot.X;
            double dy = targetY - bot.Y;
            if (Math.Abs(dx) > Math.Abs(dy))
                return dx < 0 ? Direction.Left : Direction.Right;

            return dy < 0 ? Direction.Up : Direction.Down;
        }

        private double GetTankMoveSpeed(Tank tank)
        {
            bool onIce = false;
            bool inBush = false;

            int left = (int)tank.X / TileSize;
            int top = (int)tank.Y / TileSize;
            int right = (int)(tank.X + Tank.Size - 1) / TileSize;
            int bottom = (int)(tank.Y + Tank.Size - 1) / TileSize;

            for (int y = top; y <= bottom; y++)
            {
                for (int x = left; x <= right; x++)
                {
                    if (!IsInsideMap(x, y))
                        continue;

                    CellType type = Map[x, y].Type;
                    if (type == CellType.Ice)
                        onIce = true;
                    else if (type == CellType.Bush)
                        inBush = true;
                }
            }

            double speed = tank.Speed;
            if (onIce)
                speed *= 1.18;
            if (inBush)
                speed *= 0.96;

            return speed;
        }

        private void TryMoveTank(Tank tank, double dx, double dy)
        {
            if (dx != 0 && CanOccupy(tank, tank.X + dx, tank.Y))
                tank.X += dx;

            if (dy != 0 && CanOccupy(tank, tank.X, tank.Y + dy))
                tank.Y += dy;
        }

        private bool CanOccupy(Tank movingTank, double x, double y)
        {
            if (x < 0 || y < 0 || x > ScreenWidth - Tank.Size || y > ScreenHeight - Tank.Size)
                return false;

            int left = ((int)x + 3) / TileSize;
            int top = ((int)y + 3) / TileSize;
            int right = ((int)x + Tank.Size - 4) / TileSize;
            int bottom = ((int)y + Tank.Size - 4) / TileSize;

            for (int ty = top; ty <= bottom; ty++)
            {
                for (int tx = left; tx <= right; tx++)
                {
                    if (!IsInsideMap(tx, ty))
                        return false;

                    if (IsSolidForTank(Map[tx, ty].Type))
                        return false;
                }
            }

            if (WouldOverlapTank(movingTank, Player1, x, y))
                return false;

            for (int i = 0; i < Bots.Count; i++)
            {
                if (WouldOverlapTank(movingTank, Bots[i], x, y))
                    return false;
            }

            return true;
        }

        private static bool IsSolidForTank(CellType type)
        {
            return type == CellType.Brick ||
                   type == CellType.Concrete ||
                   type == CellType.Water ||
                   type == CellType.HeadquartersTop ||
                   type == CellType.HeadquartersBottom;
        }

        private bool WouldOverlapTank(Tank movingTank, Tank other, double x, double y)
        {
            if (other == null || other == movingTank || other.IsDead)
                return false;

            return x < other.X + Tank.Size &&
                   x + Tank.Size > other.X &&
                   y < other.Y + Tank.Size &&
                   y + Tank.Size > other.Y;
        }

        private void FireBullet(Tank tank)
        {
            double bulletX = tank.X + (Tank.Size / 2.0) - 3;
            double bulletY = tank.Y + (Tank.Size / 2.0) - 3;

            if (tank.Direction == Direction.Up)
                bulletY = tank.Y - 2;
            else if (tank.Direction == Direction.Down)
                bulletY = tank.Y + Tank.Size - 4;
            else if (tank.Direction == Direction.Left)
                bulletX = tank.X - 2;
            else if (tank.Direction == Direction.Right)
                bulletX = tank.X + Tank.Size - 4;

            int damage = tank.Level >= 4 ? 2 : 1;
            Bullets.Add(new Bullet(bulletX, bulletY, tank.Direction, tank.BulletSpeed, damage, tank.TeamId));
            tank.FireCooldown = tank.FireCooldownMax;
        }

        private void UpdateBullets()
        {
            for (int i = Bullets.Count - 1; i >= 0; i--)
            {
                Bullet bullet = Bullets[i];
                bullet.Update();

                if (bullet.X < -8 || bullet.X > ScreenWidth + 8 || bullet.Y < -8 || bullet.Y > ScreenHeight + 8)
                {
                    Bullets.RemoveAt(i);
                    continue;
                }

                if (HandleBulletVsBullet(i))
                    continue;

                if (CheckMapCollision(bullet))
                {
                    Bullets.RemoveAt(i);
                    continue;
                }

                if (CheckTankHit(bullet, Player1))
                {
                    Bullets.RemoveAt(i);
                    continue;
                }

                bool removed = false;
                for (int botIndex = Bots.Count - 1; botIndex >= 0; botIndex--)
                {
                    if (!CheckTankHit(bullet, Bots[botIndex]))
                        continue;

                    Bullets.RemoveAt(i);
                    removed = true;
                    break;
                }

                if (removed)
                    continue;
            }
        }

        private bool HandleBulletVsBullet(int bulletIndex)
        {
            Bullet bullet = Bullets[bulletIndex];

            for (int i = bulletIndex - 1; i >= 0; i--)
            {
                Bullet other = Bullets[i];
                if (other.OwnerTeam == bullet.OwnerTeam)
                    continue;

                if (Math.Abs(other.X - bullet.X) <= 4 && Math.Abs(other.Y - bullet.Y) <= 4)
                {
                    AddExplosion(bullet.X, bullet.Y, false);
                    Bullets.RemoveAt(bulletIndex);
                    Bullets.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        private bool CheckMapCollision(Bullet bullet)
        {
            int tx = (int)(bullet.X + 2) / TileSize;
            int ty = (int)(bullet.Y + 2) / TileSize;
            if (!IsInsideMap(tx, ty))
                return false;

            MapCell cell = Map[tx, ty];
            switch (cell.Type)
            {
                case CellType.Brick:
                    cell.Health -= bullet.Damage;
                    if (cell.Health <= 0)
                    {
                        Map[tx, ty] = new MapCell(CellType.Empty);
                        BricksDestroyed++;
                        MapRevision++;
                        MarkMissionUiDirty();

                        if (CurrentMission.CoinDropChance > 0 && _rnd.Next(100) < CurrentMission.CoinDropChance)
                            Coins.Add(new Coin((tx * TileSize) + 4, (ty * TileSize) + 4, _rnd.Next(1, 4)));

                        if (CurrentMission.BrickTarget > 0 && BricksDestroyed == CurrentMission.BrickTarget)
                            SetAnnouncement("WALL OPEN", 100);
                    }

                    AddExplosion(bullet.X, bullet.Y, false);
                    return true;

                case CellType.Concrete:
                case CellType.Water:
                    AddExplosion(bullet.X, bullet.Y, false);
                    return true;

                case CellType.HeadquartersTop:
                    DamageHeadquarters(true, bullet.OwnerTeam);
                    AddExplosion(bullet.X, bullet.Y, true);
                    return true;

                case CellType.HeadquartersBottom:
                    DamageHeadquarters(false, bullet.OwnerTeam);
                    AddExplosion(bullet.X, bullet.Y, true);
                    return true;
            }

            return false;
        }

        private void DamageHeadquarters(bool topBase, int ownerTeam)
        {
            if (ownerTeam != 2)
                return;

            if (topBase)
            {
                TopBaseHealth = Math.Max(0, TopBaseHealth - 1);
                if (TopBaseHealth == 0)
                {
                    Map[_topHqX, _topHqY] = new MapCell(CellType.Empty);
                    MapRevision++;
                }
            }
            else
            {
                BottomBaseHealth = Math.Max(0, BottomBaseHealth - 1);
                if (BottomBaseHealth == 0)
                {
                    Map[_bottomHqX, _bottomHqY] = new MapCell(CellType.Empty);
                    MapRevision++;
                }
            }

            SetAnnouncement("HQ HIT", 60);
            MarkMissionUiDirty();
        }

        private bool CheckTankHit(Bullet bullet, Tank tank)
        {
            if (tank == null || tank.IsDead || bullet.OwnerTeam == tank.TeamId)
                return false;

            if (bullet.X < tank.X || bullet.X > tank.X + Tank.Size || bullet.Y < tank.Y || bullet.Y > tank.Y + Tank.Size)
                return false;

            bool wasKilled = tank.TakeDamage(bullet.Damage);
            AddExplosion(bullet.X, bullet.Y, false);

            if (!wasKilled)
                return true;

            AddExplosion(tank.X + 12, tank.Y + 12, true);

            if (tank.TeamId == 2)
            {
                AwardScore(120 + (tank.Level * 25) + (tank.IsElite ? 90 : 0));
                SpawnLoot(tank);
                if (tank.IsElite)
                {
                    EliteEnemiesDestroyed++;
                    _stageElitesDestroyed++;
                    SetAnnouncement("ELITE DOWN", 90);
                }

                Bots.Remove(tank);
                EnemiesDestroyed++;
                _stageEnemiesDestroyed++;
                MarkMissionUiDirty();
            }

            return true;
        }

        private void SpawnLoot(Tank tank)
        {
            Coins.Add(new Coin(tank.X + 10, tank.Y + 10, _rnd.Next(2, 5)));
            if (CurrentMission.BonusDropChance > 0 && _rnd.Next(100) < CurrentMission.BonusDropChance)
            {
                Bonuses.Add(new Bonus(
                    tank.X + 8,
                    tank.Y + 8,
                    (BonusType)_rnd.Next(Enum.GetValues(typeof(BonusType)).Length)));
            }
        }

        private void AwardScore(int amount)
        {
            Player1Score += amount;
        }

        private void UpdateBonuses()
        {
            for (int i = Bonuses.Count - 1; i >= 0; i--)
            {
                Bonus bonus = Bonuses[i];
                bonus.Lifetime--;
                if (bonus.Lifetime < 240)
                {
                    bonus.BlinkTimer++;
                    bonus.Visible = (bonus.BlinkTimer / 8) % 2 == 0;
                }

                if (bonus.Lifetime <= 0)
                    Bonuses.RemoveAt(i);
            }
        }

        private void TrySpawnBonus()
        {
            if (_bonusCooldown > 0)
            {
                _bonusCooldown--;
                return;
            }

            if (Bonuses.Count >= 2)
                return;

            for (int attempt = 0; attempt < 24; attempt++)
            {
                int tileX = _rnd.Next(4, MapWidth - 4);
                int tileY = _rnd.Next(5, MapHeight - 5);
                CellType type = Map[tileX, tileY].Type;
                if (type != CellType.Empty && type != CellType.Bush && type != CellType.Ice)
                    continue;

                double x = tileX * TileSize;
                double y = tileY * TileSize;
                if (Intersects(Player1.X, Player1.Y, Tank.Size, Tank.Size, x, y, 20, 20))
                    continue;

                Bonuses.Add(new Bonus(x, y, (BonusType)_rnd.Next(Enum.GetValues(typeof(BonusType)).Length)));
                _bonusCooldown = _rnd.Next(CurrentMission.BonusCooldownMin, CurrentMission.BonusCooldownMax + 1);
                SetAnnouncement("POWER DROP", 70);
                return;
            }

            _bonusCooldown = 180;
        }

        private void HandleBonusPickup(Tank tank)
        {
            if (tank.IsDead)
                return;

            for (int i = Bonuses.Count - 1; i >= 0; i--)
            {
                Bonus bonus = Bonuses[i];
                if (!bonus.Visible)
                    continue;

                if (!Intersects(tank.X, tank.Y, Tank.Size, Tank.Size, bonus.X, bonus.Y, 20, 20))
                    continue;

                ApplyBonus(tank, bonus.Type);
                AwardScore(90);
                AddExplosion(bonus.X + 4, bonus.Y + 4, false);
                Bonuses.RemoveAt(i);
                SetAnnouncement("POWER UP", 75);
            }
        }

        private void ApplyBonus(Tank tank, BonusType bonusType)
        {
            switch (bonusType)
            {
                case BonusType.Star:
                    tank.Upgrade();
                    break;

                case BonusType.Shield:
                    tank.ShieldTimer = Math.Max(tank.ShieldTimer, 300);
                    break;

                case BonusType.Grenade:
                    for (int i = Bots.Count - 1; i >= 0; i--)
                    {
                        AddExplosion(Bots[i].X + 10, Bots[i].Y + 10, true);
                        AwardScore(140 + (Bots[i].Level * 20));
                        if (Bots[i].IsElite)
                        {
                            EliteEnemiesDestroyed++;
                            _stageElitesDestroyed++;
                        }

                        SpawnLoot(Bots[i]);
                        Bots.RemoveAt(i);
                        EnemiesDestroyed++;
                        _stageEnemiesDestroyed++;
                    }
                    MarkMissionUiDirty();
                    break;

                case BonusType.Life:
                    Player1Lives++;
                    MarkMissionUiDirty();
                    break;

                case BonusType.Freeze:
                    _enemyFreezeTimer = 240;
                    break;

                case BonusType.Fortify:
                    ApplyFortifications();
                    break;
            }
        }

        private void ApplyFortifications()
        {
            RestoreFortifications();
            _fortifyTimer = 360;
            _fortifiedCells.Clear();
            FortifyBaseArea(_topHqX, _topHqY);
            FortifyBaseArea(_bottomHqX, _bottomHqY);
            MapRevision++;
            MarkMissionUiDirty();
        }

        private void FortifyBaseArea(int centerTileX, int centerTileY)
        {
            for (int y = centerTileY - 1; y <= centerTileY + 1; y++)
            {
                for (int x = centerTileX - 1; x <= centerTileX + 1; x++)
                {
                    if (!IsInsideMap(x, y))
                        continue;

                    if (x == centerTileX && y == centerTileY)
                        continue;

                    _fortifiedCells.Add(new FortifiedCell
                    {
                        X = x,
                        Y = y,
                        Type = Map[x, y].Type,
                        Health = Map[x, y].Health
                    });

                    Map[x, y] = new MapCell(CellType.Concrete);
                }
            }
        }

        private void RestoreFortifications()
        {
            if (_fortifiedCells.Count == 0)
                return;

            for (int i = 0; i < _fortifiedCells.Count; i++)
            {
                FortifiedCell cell = _fortifiedCells[i];
                Map[cell.X, cell.Y] = new MapCell(cell.Type) { Health = cell.Health };
            }

            _fortifiedCells.Clear();
            MapRevision++;
        }

        private void UpdateCoins()
        {
            for (int i = Coins.Count - 1; i >= 0; i--)
            {
                Coin coin = Coins[i];
                coin.AnimTimer++;
                coin.SparkleTimer++;
                if (coin.AnimTimer >= 10)
                {
                    coin.AnimTimer = 0;
                    coin.AnimFrame = (coin.AnimFrame + 1) % 4;
                }
            }
        }

        private void HandleCoinPickup(Tank tank)
        {
            for (int i = Coins.Count - 1; i >= 0; i--)
            {
                Coin coin = Coins[i];
                if (!Intersects(tank.X, tank.Y, Tank.Size, Tank.Size, coin.X, coin.Y, 14, 14))
                    continue;

                AwardScore(coin.Value * 25);
                CoinsCollected += coin.Value;
                if (tank.Health < tank.MaxHealth && _rnd.Next(100) < 25)
                    tank.Health++;

                if (CurrentMission.CoinTarget > 0 && CoinsCollected >= CurrentMission.CoinTarget)
                    SetAnnouncement("SUPPLIES SECURED", 90);

                Coins.RemoveAt(i);
                MarkMissionUiDirty();
            }
        }

        private void UpdateExplosions()
        {
            for (int i = Explosions.Count - 1; i >= 0; i--)
            {
                Explosions[i].Update();
                if (!Explosions[i].IsAlive)
                    Explosions.RemoveAt(i);
            }
        }

        private void TrySpawnEnemies()
        {
            if (_stageEnemiesSpawned >= CurrentStage.EnemiesToSpawn)
                return;
            if (Bots.Count >= CurrentStage.MaxActiveEnemies)
                return;

            if (_spawnCooldown > 0)
            {
                _spawnCooldown--;
                return;
            }

            SpawnBot();
            _spawnCooldown = _rnd.Next(CurrentStage.SpawnCooldownMin, CurrentStage.SpawnCooldownMax + 1);
        }

        private void SpawnBot()
        {
            int laneIndex = _rnd.Next(_spawnLaneCenters.Length);
            double spawnX = _spawnLaneCenters[laneIndex];
            double spawnY = TileSize;

            bool forceElite = _stageElitesSpawned < CurrentStage.EliteEnemies &&
                              (CurrentStage.EnemiesToSpawn - _stageEnemiesSpawned) <= (CurrentStage.EliteEnemies - _stageElitesSpawned);

            bool isElite = forceElite;
            Tank bot = new Tank(spawnX, spawnY, 2, $"B{EnemiesDestroyed + Bots.Count + 1}");
            bot.Direction = Direction.Down;
            bot.IsElite = isElite;
            bot.Level = _rnd.Next(CurrentStage.EnemyLevelMin, CurrentStage.EnemyLevelMax + 1);
            if (bot.IsElite && bot.Level < 3)
                bot.Level = 3;

            bot.ApplyStats(true);
            if (bot.IsElite)
            {
                bot.MaxHealth += 1;
                bot.Health = bot.MaxHealth;
                bot.ShieldTimer = 70;
            }
            else
            {
                bot.ShieldTimer = 45;
            }

            bot.AiDecisionTimer = _rnd.Next(10, 20);

            if (CanOccupy(bot, spawnX, spawnY))
            {
                Bots.Add(bot);
                _stageEnemiesSpawned++;
                if (bot.IsElite)
                    _stageElitesSpawned++;
                MarkMissionUiDirty();
                return;
            }

            for (int i = 0; i < _spawnLaneCenters.Length; i++)
            {
                spawnX = _spawnLaneCenters[i];
                bot.X = spawnX;
                bot.Y = spawnY;
                if (CanOccupy(bot, spawnX, spawnY))
                {
                    Bots.Add(bot);
                    _stageEnemiesSpawned++;
                    if (bot.IsElite)
                        _stageElitesSpawned++;
                    MarkMissionUiDirty();
                    return;
                }
            }
        }

        private void CheckEndConditions()
        {
            if (State != GameState.Running)
                return;

            if (BasesAlive < CurrentMission.MinimumBasesAlive)
            {
                LoseMission("HQ LINE BROKEN");
                return;
            }

            if (Player1.IsDead && Player1Lives == 0)
            {
                LoseMission("TANK DESTROYED");
                return;
            }
        }

        private void WinMission()
        {
            if (State != GameState.Running)
                return;

            if (CurrentMission.CoinTarget > 0 && CoinsCollected < CurrentMission.CoinTarget)
            {
                LoseMission("NOT ENOUGH SUPPLIES");
                return;
            }

            if (CurrentMission.BrickTarget > 0 && BricksDestroyed < CurrentMission.BrickTarget)
            {
                LoseMission("FORTRESS STILL CLOSED");
                return;
            }

            if (CurrentMission.EliteKillTarget > 0 && EliteEnemiesDestroyed < CurrentMission.EliteKillTarget)
            {
                LoseMission("ELITES STILL ACTIVE");
                return;
            }

            State = GameState.Victory;
            CampaignCleared = CurrentMissionIndex >= _missions.Length - 1;
            ResetInput();
            SetAnnouncement(CampaignCleared ? "CAMPAIGN CLEAR" : "MISSION CLEAR", 160);
        }

        private bool CoreObjectivesCompleted()
        {
            if (CurrentMission.CoinTarget > 0 && CoinsCollected < CurrentMission.CoinTarget)
                return false;
            if (CurrentMission.BrickTarget > 0 && BricksDestroyed < CurrentMission.BrickTarget)
                return false;
            if (CurrentMission.EliteKillTarget > 0 && EliteEnemiesDestroyed < CurrentMission.EliteKillTarget)
                return false;

            return true;
        }

        private void LoseMission(string reason)
        {
            if (State != GameState.Running)
                return;

            State = GameState.Defeat;
            _missionFailureReason = reason;
            ResetInput();
            MarkMissionUiDirty();
        }

        private void SetAnnouncement(string text, int durationFrames)
        {
            _announcementText = text;
            _announcementTimer = durationFrames;
        }

        private void EnsureMissionUi()
        {
            if (!_missionUiDirty)
                return;

            var lines = new List<string>(8)
            {
                "MISSION " + CurrentMission.Number + " " + CurrentMission.CodeName,
                "STAGE " + (CurrentStageIndex + 1) + " OF " + StageCount,
                "WAVE " + _stageEnemiesDestroyed + " OF " + CurrentStage.EnemiesToSpawn,
                CurrentStage.Title,
                CurrentStage.Objective
            };

            if (CurrentMission.CoinTarget > 0)
                lines.Add("COINS " + CoinsCollected + " OF " + CurrentMission.CoinTarget);

            if (CurrentMission.EliteKillTarget > 0)
                lines.Add("ELITE " + EliteEnemiesDestroyed + " OF " + CurrentMission.EliteKillTarget);

            if (CurrentMission.BrickTarget > 0)
                lines.Add("WALL " + BricksDestroyed + " OF " + CurrentMission.BrickTarget);

            lines.Add("HQ " + BasesAlive + " OF " + CurrentMission.MinimumBasesAlive + " NEED");

            _missionGoalLines = lines.ToArray();
            _missionUiDirty = false;
        }

        private void MarkMissionUiDirty()
        {
            _missionUiDirty = true;
        }

        private void AddExplosion(double x, double y, bool big)
        {
            Explosions.Add(new Explosion(x, y, big));
        }

        private bool IsInsideMap(int tx, int ty)
        {
            return tx >= 0 && ty >= 0 && tx < MapWidth && ty < MapHeight;
        }

        private static bool Intersects(double ax, double ay, double aw, double ah, double bx, double by, double bw, double bh)
        {
            return ax < bx + bw && ax + aw > bx && ay < by + bh && ay + ah > by;
        }
    }
}
