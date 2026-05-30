using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using System.Xml;
using Formatting = Newtonsoft.Json.Formatting;

namespace BlackBookAppWPF
{
    public class CardStorage
    {
        private const string Folder = "Decks";

        public CardStorage()
        {
            if (!Directory.Exists(Folder))
                Directory.CreateDirectory(Folder);
        }

        public void Save(Deck deck)
        {
            string json = JsonConvert.SerializeObject(deck, Formatting.Indented);
            File.WriteAllText(Path.Combine(Folder, $"{deck.Name}.json"), json);
        }

        public Deck Load(string name)
        {
            string path = Path.Combine(Folder, $"{name}.json");
            if (!File.Exists(path)) return null;
            string json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<Deck>(json);
        }

        public List<string> GetAvailableDecks()
        {
            if (!Directory.Exists(Folder)) return new List<string>();
            return Directory.GetFiles(Folder, "*.json")
                .Select(Path.GetFileNameWithoutExtension)
                .ToList();
        }
    }

    public class Deck
    {
        public string Name { get; set; }
        public List<Card> Cards { get; set; } = new List<Card>();

        public Deck() { }
        public Deck(string name) => Name = name;
    }
}