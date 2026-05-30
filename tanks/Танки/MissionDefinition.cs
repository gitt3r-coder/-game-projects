using System;

namespace Tanki
{
    public enum MapTheme
    {
        Balanced,
        Wetlands,
        FrozenFront,
        Fortress
    }

    public sealed class MissionDefinition
    {
        public int Number { get; set; }
        public string CodeName { get; set; }
        public string Title { get; set; }
        public string[] BriefingLines { get; set; }
        public string HintLine { get; set; }
        public MapTheme Theme { get; set; }
        public int MinimumBasesAlive { get; set; }
        public int StartingLives { get; set; }
        public int CoinTarget { get; set; }
        public int BrickTarget { get; set; }
        public int EliteKillTarget { get; set; }
        public int TimeLimitFrames { get; set; }
        public int BonusCooldownMin { get; set; }
        public int BonusCooldownMax { get; set; }
        public int CoinDropChance { get; set; }
        public int BonusDropChance { get; set; }
        public MissionStageDefinition[] Stages { get; set; }

        public int TotalEnemies
        {
            get
            {
                int total = 0;
                for (int i = 0; i < Stages.Length; i++)
                    total += Stages[i].EnemiesToSpawn;

                return total;
            }
        }

        public static MissionDefinition[] CreateCampaign()
        {
            return new[]
            {
                new MissionDefinition
                {
                    Number = 1,
                    CodeName = "FIRST WATCH",
                    Title = "HOLD THE LINE",
                    BriefingLines = new[]
                    {
                        "SCOUT ARMOR IS MOVING IN",
                        "KEEP THE HQ SAFE",
                        "LEARN THE FIELD AND SURVIVE"
                    },
                    HintLine = "EASY START CLEAR 3 STAGES",
                    Theme = MapTheme.Balanced,
                    MinimumBasesAlive = 1,
                    StartingLives = 4,
                    BonusCooldownMin = 480,
                    BonusCooldownMax = 760,
                    CoinDropChance = 18,
                    BonusDropChance = 16,
                    Stages = new[]
                    {
                        new MissionStageDefinition
                        {
                            Title = "PATROL WAVE",
                            Objective = "DESTROY THE SCOUTS",
                            EnemiesToSpawn = 3,
                            EliteEnemies = 0,
                            MaxActiveEnemies = 2,
                            SpawnCooldownMin = 110,
                            SpawnCooldownMax = 150,
                            EnemyLevelMin = 1,
                            EnemyLevelMax = 1,
                            MaxEnemyBullets = 1,
                            StartDelayFrames = 45
                        },
                        new MissionStageDefinition
                        {
                            Title = "PRESSURE TEST",
                            Objective = "STOP THE NEXT PUSH",
                            EnemiesToSpawn = 3,
                            EliteEnemies = 0,
                            MaxActiveEnemies = 2,
                            SpawnCooldownMin = 90,
                            SpawnCooldownMax = 120,
                            EnemyLevelMin = 1,
                            EnemyLevelMax = 2,
                            MaxEnemyBullets = 2,
                            StartDelayFrames = 120
                        },
                        new MissionStageDefinition
                        {
                            Title = "BREAKTHROUGH",
                            Objective = "STOP THE FINAL RUSH",
                            EnemiesToSpawn = 4,
                            EliteEnemies = 1,
                            MaxActiveEnemies = 3,
                            SpawnCooldownMin = 78,
                            SpawnCooldownMax = 110,
                            EnemyLevelMin = 1,
                            EnemyLevelMax = 2,
                            MaxEnemyBullets = 2,
                            StartDelayFrames = 140
                        }
                    }
                },
                new MissionDefinition
                {
                    Number = 2,
                    CodeName = "RIVER RUN",
                    Title = "SECURE THE SUPPLIES",
                    BriefingLines = new[]
                    {
                        "THE DEPOT IS SCATTERED ACROSS THE MARSH",
                        "COLLECT THE COINS WHILE HOLDING POSITION",
                        "THE WAVES GET FASTER EACH STAGE"
                    },
                    HintLine = "CLEAR STAGES AND COLLECT 10 COINS",
                    Theme = MapTheme.Wetlands,
                    MinimumBasesAlive = 1,
                    StartingLives = 4,
                    CoinTarget = 10,
                    BonusCooldownMin = 360,
                    BonusCooldownMax = 640,
                    CoinDropChance = 42,
                    BonusDropChance = 18,
                    Stages = new[]
                    {
                        new MissionStageDefinition
                        {
                            Title = "MARSH ENTRY",
                            Objective = "CLEAR THE SHALLOW CROSSING",
                            EnemiesToSpawn = 4,
                            EliteEnemies = 0,
                            MaxActiveEnemies = 2,
                            SpawnCooldownMin = 100,
                            SpawnCooldownMax = 135,
                            EnemyLevelMin = 1,
                            EnemyLevelMax = 2,
                            MaxEnemyBullets = 2,
                            StartDelayFrames = 50
                        },
                        new MissionStageDefinition
                        {
                            Title = "SUPPLY PICKUP",
                            Objective = "COLLECT COINS UNDER FIRE",
                            EnemiesToSpawn = 4,
                            EliteEnemies = 1,
                            MaxActiveEnemies = 3,
                            SpawnCooldownMin = 84,
                            SpawnCooldownMax = 116,
                            EnemyLevelMin = 1,
                            EnemyLevelMax = 2,
                            MaxEnemyBullets = 2,
                            StartDelayFrames = 120
                        },
                        new MissionStageDefinition
                        {
                            Title = "RIVER EXIT",
                            Objective = "PUNCH THROUGH THE LAST BLOCKADE",
                            EnemiesToSpawn = 5,
                            EliteEnemies = 1,
                            MaxActiveEnemies = 3,
                            SpawnCooldownMin = 74,
                            SpawnCooldownMax = 104,
                            EnemyLevelMin = 2,
                            EnemyLevelMax = 3,
                            MaxEnemyBullets = 3,
                            StartDelayFrames = 140
                        }
                    }
                },
                new MissionDefinition
                {
                    Number = 3,
                    CodeName = "WHITE FRONT",
                    Title = "CUT DOWN THE ELITES",
                    BriefingLines = new[]
                    {
                        "THE ENEMY BRINGS HEAVY ARMOR ON ICE",
                        "USE COVER AND WAIT FOR CLEAN SHOTS",
                        "THE FINAL STAGE SENDS MULTIPLE ELITES"
                    },
                    HintLine = "DESTROY 4 ELITE TANKS",
                    Theme = MapTheme.FrozenFront,
                    MinimumBasesAlive = 1,
                    StartingLives = 4,
                    EliteKillTarget = 4,
                    BonusCooldownMin = 320,
                    BonusCooldownMax = 580,
                    CoinDropChance = 26,
                    BonusDropChance = 22,
                    Stages = new[]
                    {
                        new MissionStageDefinition
                        {
                            Title = "ICE SCREEN",
                            Objective = "STOP THE ADVANCE GUARD",
                            EnemiesToSpawn = 4,
                            EliteEnemies = 1,
                            MaxActiveEnemies = 2,
                            SpawnCooldownMin = 96,
                            SpawnCooldownMax = 128,
                            EnemyLevelMin = 2,
                            EnemyLevelMax = 3,
                            MaxEnemyBullets = 2,
                            StartDelayFrames = 55
                        },
                        new MissionStageDefinition
                        {
                            Title = "COLD PUSH",
                            Objective = "FOCUS THE HEAVY TARGETS",
                            EnemiesToSpawn = 5,
                            EliteEnemies = 1,
                            MaxActiveEnemies = 3,
                            SpawnCooldownMin = 78,
                            SpawnCooldownMax = 106,
                            EnemyLevelMin = 2,
                            EnemyLevelMax = 3,
                            MaxEnemyBullets = 3,
                            StartDelayFrames = 130
                        },
                        new MissionStageDefinition
                        {
                            Title = "ARMORED SPEAR",
                            Objective = "SURVIVE THE ELITE DOUBLE WAVE",
                            EnemiesToSpawn = 6,
                            EliteEnemies = 2,
                            MaxActiveEnemies = 3,
                            SpawnCooldownMin = 68,
                            SpawnCooldownMax = 94,
                            EnemyLevelMin = 3,
                            EnemyLevelMax = 4,
                            MaxEnemyBullets = 3,
                            StartDelayFrames = 150
                        }
                    }
                },
                new MissionDefinition
                {
                    Number = 4,
                    CodeName = "IRON CITADEL",
                    Title = "BREACH THE FORT",
                    BriefingLines = new[]
                    {
                        "THE FINAL POSITION IS SEALED BY WALLS",
                        "BLAST OPEN THE ROUTE BEFORE THE LAST WAVE",
                        "HARD MODE KEEP YOUR DISTANCE"
                    },
                    HintLine = "DESTROY 18 WALL BLOCKS AND CLEAR ALL STAGES",
                    Theme = MapTheme.Fortress,
                    MinimumBasesAlive = 1,
                    StartingLives = 3,
                    BrickTarget = 18,
                    BonusCooldownMin = 300,
                    BonusCooldownMax = 560,
                    CoinDropChance = 28,
                    BonusDropChance = 24,
                    Stages = new[]
                    {
                        new MissionStageDefinition
                        {
                            Title = "OUTER GATE",
                            Objective = "OPEN THE FIRST WALL RING",
                            EnemiesToSpawn = 4,
                            EliteEnemies = 1,
                            MaxActiveEnemies = 2,
                            SpawnCooldownMin = 92,
                            SpawnCooldownMax = 122,
                            EnemyLevelMin = 2,
                            EnemyLevelMax = 3,
                            MaxEnemyBullets = 2,
                            StartDelayFrames = 60
                        },
                        new MissionStageDefinition
                        {
                            Title = "INNER YARD",
                            Objective = "BREAK THE DEFENSIVE POCKETS",
                            EnemiesToSpawn = 4,
                            EliteEnemies = 1,
                            MaxActiveEnemies = 3,
                            SpawnCooldownMin = 78,
                            SpawnCooldownMax = 104,
                            EnemyLevelMin = 3,
                            EnemyLevelMax = 4,
                            MaxEnemyBullets = 3,
                            StartDelayFrames = 140
                        },
                        new MissionStageDefinition
                        {
                            Title = "CORE CHAMBER",
                            Objective = "HOLD POSITION AND KEEP FIRING",
                            EnemiesToSpawn = 4,
                            EliteEnemies = 1,
                            MaxActiveEnemies = 3,
                            SpawnCooldownMin = 70,
                            SpawnCooldownMax = 96,
                            EnemyLevelMin = 3,
                            EnemyLevelMax = 4,
                            MaxEnemyBullets = 3,
                            StartDelayFrames = 150
                        },
                        new MissionStageDefinition
                        {
                            Title = "FINAL BREACH",
                            Objective = "DEFEAT THE LAST ARMORED GROUP",
                            EnemiesToSpawn = 5,
                            EliteEnemies = 2,
                            MaxActiveEnemies = 4,
                            SpawnCooldownMin = 62,
                            SpawnCooldownMax = 86,
                            EnemyLevelMin = 4,
                            EnemyLevelMax = 5,
                            MaxEnemyBullets = 4,
                            StartDelayFrames = 170
                        }
                    }
                }
            };
        }
    }
}
