using System;
using System.Windows.Media;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BlackBookAppWPF
{
    public enum CardEffectType
    {
        None,
        Shield,
        Heal,
        Burn,
        Weak,
        Draw,
        Drain,
        Execute,
        Mana,
        Stun,
        Pierce,
        DoubleStrike
    }

    [JsonConverter(typeof(CardJsonConverter))]
    public abstract class Card
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int Cost { get; set; }
        public int Power { get; set; }
        public int Rarity { get; set; }
        public string Description { get; set; }
        public string CardType { get; set; }
        public string Faction { get; set; }
        public string ArtKey { get; set; }
        public string Quote { get; set; }
        public CardEffectType EffectType { get; set; }
        public int EffectValue { get; set; }

        [JsonIgnore]
        public abstract string Icon { get; }

        [JsonIgnore]
        public abstract string TypeName { get; }

        [JsonIgnore]
        public string RarityName
        {
            get
            {
                switch (Rarity)
                {
                    case 5: return "Легендарная";
                    case 4: return "Эпическая";
                    case 3: return "Редкая";
                    case 2: return "Необычная";
                    default: return "Обычная";
                }
            }
        }

        [JsonIgnore]
        public string EffectText
        {
            get
            {
                switch (EffectType)
                {
                    case CardEffectType.Shield:
                        return $"Щит {EffectValue}";
                    case CardEffectType.Heal:
                        return $"Лечение {EffectValue}";
                    case CardEffectType.Burn:
                        return $"Ожог {EffectValue}";
                    case CardEffectType.Weak:
                        return $"Слабость {EffectValue}";
                    case CardEffectType.Draw:
                        return $"Добор {EffectValue}";
                    case CardEffectType.Drain:
                        return $"Вытягивание {EffectValue}";
                    case CardEffectType.Execute:
                        return $"Казнь +{EffectValue}";
                    case CardEffectType.Mana:
                        return $"Искра +{EffectValue}";
                    case CardEffectType.Stun:
                        return "Оглушение";
                    case CardEffectType.Pierce:
                        return "Пробивание";
                    case CardEffectType.DoubleStrike:
                        return "Двойной удар";
                    default:
                        return "Удар";
                }
            }
        }

        public SolidColorBrush GetRarityColor()
        {
            switch (Rarity)
            {
                case 5: return new SolidColorBrush(Color.FromRgb(255, 205, 86));
                case 4: return new SolidColorBrush(Color.FromRgb(204, 91, 255));
                case 3: return new SolidColorBrush(Color.FromRgb(70, 196, 255));
                case 2: return new SolidColorBrush(Color.FromRgb(90, 220, 130));
                default: return new SolidColorBrush(Color.FromRgb(170, 178, 190));
            }
        }
    }

    public class Creature : Card
    {
        public Creature()
        {
            CardType = "Creature";
        }

        public override string Icon => "🐺";
        public override string TypeName => "Существо";
    }

    public class Spell : Card
    {
        public Spell()
        {
            CardType = "Spell";
        }

        public override string Icon => "🔮";
        public override string TypeName => "Заклинание";
    }

    public class Trap : Card
    {
        public Trap()
        {
            CardType = "Trap";
        }

        public override string Icon => "🕸️";
        public override string TypeName => "Ловушка";
    }

    public class CardJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(Card).IsAssignableFrom(objectType);
        }

        public override bool CanWrite => false;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            JObject obj = JObject.Load(reader);
            string cardType = (string)obj["CardType"];
            Card card;

            switch (cardType)
            {
                case "Spell":
                    card = new Spell();
                    break;
                case "Trap":
                    card = new Trap();
                    break;
                default:
                    card = new Creature();
                    break;
            }

            serializer.Populate(obj.CreateReader(), card);
            return card;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotSupportedException();
        }
    }
}
