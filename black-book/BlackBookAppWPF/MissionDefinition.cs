namespace BlackBookAppWPF
{
    public class MissionDefinition
    {
        public int Number { get; set; }
        public string Title { get; set; }
        public string EnemyName { get; set; }
        public string Objective { get; set; }
        public string Intro { get; set; }
        public string Glyph { get; set; }
        public int BossLevel { get; set; }
        public int BossHealth { get; set; }
        public double DamageMultiplier { get; set; }
        public double ShieldMultiplier { get; set; }
        public int RewardGold { get; set; }
        public int RewardRarity { get; set; }
        public int RecommendedDeckPower { get; set; }
    }
}
