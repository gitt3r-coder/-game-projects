using System;

namespace Tanki
{
    public class Tank
    {
        public const int Size = 32;

        public string Name { get; }
        public double X { get; set; }
        public double Y { get; set; }
        public double SpawnX { get; set; }
        public double SpawnY { get; set; }
        public Direction Direction { get; set; }
        public int Level { get; set; }
        public double Speed { get; set; }
        public double BulletSpeed { get; set; }
        public int FireCooldown { get; set; }
        public int FireCooldownMax { get; set; }
        public int Health { get; set; }
        public int MaxHealth { get; set; }
        public int TeamId { get; set; }
        public bool IsElite { get; set; }
        public int ShieldTimer { get; set; }
        public int RespawnTimer { get; set; }
        public int AiDecisionTimer { get; set; }
        public bool JustLeveledUp { get; set; }
        public int LevelUpEffectTimer { get; set; }
        public bool IsDead => Health <= 0;
        public bool RespawnReady => IsDead && RespawnTimer <= 0;

        public Tank(double x, double y, int teamId, string name = null)
        {
            Name = string.IsNullOrWhiteSpace(name) ? $"T{teamId}" : name;
            X = x;
            Y = y;
            SpawnX = x;
            SpawnY = y;
            TeamId = teamId;
            Direction = Direction.Up;
            Level = 1;
            ApplyStats(true);
        }

        public void ApplyStats(bool refillHealth = false)
        {
            Speed = 1.8 + (Level * 0.22);
            BulletSpeed = 5.0 + (Level * 0.55);
            FireCooldownMax = Math.Max(8, 24 - (Level * 2));
            MaxHealth = 2 + (Level / 2);

            if (Health <= 0 || refillHealth || JustLeveledUp)
                Health = MaxHealth;
        }

        public void Upgrade()
        {
            if (Level >= 5)
                return;

            Level++;
            JustLeveledUp = true;
            LevelUpEffectTimer = 90;
            ApplyStats(true);
        }

        public bool TakeDamage(int damage)
        {
            if (ShieldTimer > 0 || IsDead)
                return false;

            Health -= damage;
            if (Health > 0)
                return false;

            Health = 0;
            RespawnTimer = 120;
            return true;
        }

        public void Update()
        {
            if (FireCooldown > 0)
                FireCooldown--;

            if (ShieldTimer > 0)
                ShieldTimer--;

            if (LevelUpEffectTimer > 0)
            {
                LevelUpEffectTimer--;
                if (LevelUpEffectTimer == 0)
                    JustLeveledUp = false;
            }

            if (IsDead && RespawnTimer > 0)
                RespawnTimer--;

            if (AiDecisionTimer > 0)
                AiDecisionTimer--;
        }

        public void Respawn()
        {
            X = SpawnX;
            Y = SpawnY;
            Health = MaxHealth;
            FireCooldown = 15;
            ShieldTimer = 150;
            RespawnTimer = 0;
        }
    }
}
