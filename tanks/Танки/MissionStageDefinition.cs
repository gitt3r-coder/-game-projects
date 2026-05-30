namespace Tanki
{
    public sealed class MissionStageDefinition
    {
        public string Title { get; set; }
        public string Objective { get; set; }
        public int EnemiesToSpawn { get; set; }
        public int EliteEnemies { get; set; }
        public int MaxActiveEnemies { get; set; }
        public int SpawnCooldownMin { get; set; }
        public int SpawnCooldownMax { get; set; }
        public int EnemyLevelMin { get; set; }
        public int EnemyLevelMax { get; set; }
        public int MaxEnemyBullets { get; set; }
        public int StartDelayFrames { get; set; }
    }
}
