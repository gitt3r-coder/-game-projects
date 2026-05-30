using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;
using System.Xml;
using Formatting = Newtonsoft.Json.Formatting;

namespace BlackBookAppWPF
{
    public class PlayerProfile
    {
        public int Gold { get; set; } = 500;
        public List<Card> Collection { get; set; } = new List<Card>();
        public DateTime LastSpin { get; set; } = DateTime.MinValue;
        public List<string> UnlockedImages { get; set; } = new List<string>();
        public int BossesDefeated { get; set; }
        public int HighestMissionCompleted { get; set; }

        public void Save()
        {
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText("profile.json", json);
        }

        public static PlayerProfile Load()
        {
            if (!File.Exists("profile.json")) return new PlayerProfile();
            try
            {
                string json = File.ReadAllText("profile.json");
                return JsonConvert.DeserializeObject<PlayerProfile>(json) ?? new PlayerProfile();
            }
            catch
            {
                return new PlayerProfile();
            }
        }
    }
}
