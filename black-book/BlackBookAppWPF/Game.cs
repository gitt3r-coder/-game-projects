using System;
using System.Collections.Generic;
using System.Linq;

namespace BlackBookAppWPF
{
    public class Game
    {
        public PlayerProfile Profile { get; private set; }
        public Deck CurrentDeck { get; set; }
        public CardStorage Storage { get; private set; }
        public List<MissionDefinition> Missions { get; private set; }
        private List<Card> cardPool;
        private readonly Random rng = new Random();

        public event Action OnGameUpdated;
        public event Action<Card> OnCardOpened;

        public Game()
        {
            Storage = new CardStorage();
            InitializeCardPool();
            InitializeMissions();
            Profile = PlayerProfile.Load();
        }

        private void InitializeMissions()
        {
            Missions = new List<MissionDefinition>
            {
                new MissionDefinition
                {
                    Number = 1,
                    Title = "Пробуждение книги",
                    EnemyName = "Свечной страж",
                    Objective = "Выжить первые ходы и победить стража. Учебная миссия: враг атакует редко и слабо.",
                    Intro = "Первая страница горит спокойным светом. Это проверка, а не казнь.",
                    Glyph = "I",
                    BossLevel = 1,
                    BossHealth = 230,
                    DamageMultiplier = 0.62,
                    ShieldMultiplier = 0.65,
                    RewardGold = 180,
                    RewardRarity = 2,
                    RecommendedDeckPower = 160
                },
                new MissionDefinition
                {
                    Number = 2,
                    Title = "Тропа через рощу",
                    EnemyName = "Лесной надсмотрщик",
                    Objective = "Победить врага, который начинает ставить щит. Нужны пробивание, ожог или стабильный урон.",
                    Intro = "Тропа сжимается за спиной. Лес проверяет, умеете ли вы давить защиту.",
                    Glyph = "II",
                    BossLevel = 2,
                    BossHealth = 360,
                    DamageMultiplier = 0.78,
                    ShieldMultiplier = 0.9,
                    RewardGold = 260,
                    RewardRarity = 3,
                    RecommendedDeckPower = 260
                },
                new MissionDefinition
                {
                    Number = 3,
                    Title = "Болотная печать",
                    EnemyName = "Кикимора-печатница",
                    Objective = "Пережить ожоги и ответить контролем. Лечение и щиты становятся важными.",
                    Intro = "Чернила густеют, как болотная вода. Ошибки теперь остаются на коже.",
                    Glyph = "III",
                    BossLevel = 3,
                    BossHealth = 500,
                    DamageMultiplier = 0.96,
                    ShieldMultiplier = 1.0,
                    RewardGold = 360,
                    RewardRarity = 3,
                    RecommendedDeckPower = 380
                },
                new MissionDefinition
                {
                    Number = 4,
                    Title = "Колокольня костей",
                    EnemyName = "Костяной звонарь",
                    Objective = "Сломать тяжелую оборону и не отдавать темп. Здесь помогают добор и мана.",
                    Intro = "Каждый удар колокола отмеряет не время, а ваши ошибки.",
                    Glyph = "IV",
                    BossLevel = 4,
                    BossHealth = 680,
                    DamageMultiplier = 1.12,
                    ShieldMultiplier = 1.28,
                    RewardGold = 500,
                    RewardRarity = 4,
                    RecommendedDeckPower = 540
                },
                new MissionDefinition
                {
                    Number = 5,
                    Title = "Морок черной страницы",
                    EnemyName = "Морок-Князь",
                    Objective = "Финальная миссия: враг сильно бьет, лечится и наказывает пустые ходы.",
                    Intro = "Последняя страница не листается. Она смотрит в ответ.",
                    Glyph = "V",
                    BossLevel = 5,
                    BossHealth = 880,
                    DamageMultiplier = 1.32,
                    ShieldMultiplier = 1.42,
                    RewardGold = 750,
                    RewardRarity = 5,
                    RecommendedDeckPower = 720
                },
                new MissionDefinition
                {
                    Number = 6,
                    Title = "Северная метель",
                    EnemyName = "Ледяной Воевода",
                    Objective = "Враг часто ослабляет и бьет сериями. Нужны лечение, добор и карты с низкой стоимостью.",
                    Intro = "Страница покрылась инеем. Любой пустой ход здесь превращается в трещину.",
                    Glyph = "VI",
                    BossLevel = 6,
                    BossHealth = 1040,
                    DamageMultiplier = 1.45,
                    ShieldMultiplier = 1.15,
                    RewardGold = 920,
                    RewardRarity = 5,
                    RecommendedDeckPower = 900
                },
                new MissionDefinition
                {
                    Number = 7,
                    Title = "Пепельный пир",
                    EnemyName = "Жар-Птица Падшая",
                    Objective = "Много ожога и прямого давления. Побеждает колода, которая быстро заканчивает бой.",
                    Intro = "Пепел поднимается вверх, будто вспоминает, как был крыльями.",
                    Glyph = "VII",
                    BossLevel = 7,
                    BossHealth = 1220,
                    DamageMultiplier = 1.58,
                    ShieldMultiplier = 1.05,
                    RewardGold = 1100,
                    RewardRarity = 5,
                    RecommendedDeckPower = 1080
                },
                new MissionDefinition
                {
                    Number = 8,
                    Title = "Серебряная осада",
                    EnemyName = "Стальной Привратник",
                    Objective = "Босс ставит огромные щиты. Берите пробивание, казнь и постоянный урон.",
                    Intro = "Врата стоят посреди страницы. Они открываются только после хорошего удара.",
                    Glyph = "VIII",
                    BossLevel = 8,
                    BossHealth = 1420,
                    DamageMultiplier = 1.68,
                    ShieldMultiplier = 1.75,
                    RewardGold = 1320,
                    RewardRarity = 5,
                    RecommendedDeckPower = 1280
                },
                new MissionDefinition
                {
                    Number = 9,
                    Title = "Гамаюн молчит",
                    EnemyName = "Птица Пустого Пророчества",
                    Objective = "Длинный бой на ресурсы. Нужны мана, добор и умение не тратить все карты в один ход.",
                    Intro = "Когда вещая птица молчит, будущее приходится добывать силой.",
                    Glyph = "IX",
                    BossLevel = 9,
                    BossHealth = 1650,
                    DamageMultiplier = 1.82,
                    ShieldMultiplier = 1.45,
                    RewardGold = 1550,
                    RewardRarity = 5,
                    RecommendedDeckPower = 1500
                },
                new MissionDefinition
                {
                    Number = 10,
                    Title = "Чернобог на пороге",
                    EnemyName = "Чернобог",
                    Objective = "Финальный босс кампании. Он атакует, лечится, ставит щит и наказывает слабые колоды.",
                    Intro = "Порог исчез. Осталась только дверь, которая открывается внутрь тени.",
                    Glyph = "X",
                    BossLevel = 10,
                    BossHealth = 1950,
                    DamageMultiplier = 2.0,
                    ShieldMultiplier = 1.85,
                    RewardGold = 2200,
                    RewardRarity = 5,
                    RecommendedDeckPower = 1800
                }
            };
        }

        private void InitializeCardPool()
        {
            cardPool = new List<Card>();

            AddCreature("domovoy", "Домовой-Хранитель", 1, 16, 1, "Дом", "hearth",
                "Даёт щит и держит удар, пока печь тёплая.",
                "У него ключи от всех дверей, даже от тех, которых нет.",
                CardEffectType.Shield, 10);
            AddCreature("kikimora", "Кикимора с Трясин", 2, 24, 1, "Болото", "swamp",
                "Тонкая болотная рука ослабляет следующую атаку врага.",
                "Тина шепчет её имя раньше ветра.",
                CardEffectType.Weak, 1);
            AddCreature("leshy-scout", "Леший-Следопыт", 2, 28, 1, "Лес", "forest",
                "Быстрый звериный удар без лишней магии.",
                "В чужом лесу тропа всегда смотрит на тебя.",
                CardEffectType.None, 0);
            AddCreature("poludnica", "Полудница", 2, 18, 1, "Поле", "sun",
                "Солнечный порез и добор одной карты.",
                "В полдень тень становится самым честным свидетелем.",
                CardEffectType.Draw, 1);
            AddCreature("vodyanoy", "Водяной Князь", 3, 40, 2, "Река", "river",
                "Затягивает жизнь врага и лечит хозяина книги.",
                "На дне каждая клятва звучит громче.",
                CardEffectType.Drain, 12);
            AddCreature("rusalka", "Русалка-Заводь", 3, 32, 2, "Река", "moon-water",
                "Лечит игрока и оставляет босса под водой.",
                "Её песня помнит тех, кто не вернулся.",
                CardEffectType.Heal, 18);
            AddCreature("bereginya", "Берегиня Рощи", 3, 22, 2, "Лес", "grove",
                "Слабый удар, но мощная защита на ход.",
                "Она не нападает первой. Обычно ей и не нужно.",
                CardEffectType.Shield, 24);
            AddCreature("psoglavets", "Псоглавец", 4, 58, 2, "Север", "fang",
                "Пробивает щиты и броню босса.",
                "Снег вокруг него всегда пахнет железом.",
                CardEffectType.Pierce, 0);
            AddCreature("leshy", "Леший Старший", 4, 62, 3, "Лес", "ancient-forest",
                "Ослабляет босса на несколько ходов.",
                "Если лес замолчал, значит он слушает приказ.",
                CardEffectType.Weak, 2);
            AddCreature("alik", "Алконост", 4, 48, 3, "Небо", "songbird",
                "Песнь лечит и возвращает темп.",
                "Горе отступает, когда она складывает крылья.",
                CardEffectType.Heal, 24);
            AddCreature("sirin", "Сирин Ночной", 5, 68, 3, "Небо", "nightbird",
                "Проклятая песня обжигает босса.",
                "Красота бывает предупреждением.",
                CardEffectType.Burn, 14);
            AddCreature("zmey", "Змей Подколодный", 5, 76, 3, "Пепел", "serpent",
                "Двойной удар двумя головами.",
                "Сначала слышишь шипение. Потом спорят уже две тени.",
                CardEffectType.DoubleStrike, 0);
            AddCreature("vasilisk", "Василиск", 6, 92, 4, "Камень", "basilisk",
                "Оглушает босса, если взгляд встретился.",
                "Камень тоже когда-то был испугом.",
                CardEffectType.Stun, 1);
            AddCreature("firebird", "Жар-Птица", 7, 112, 4, "Пламя", "firebird",
                "Сильный удар и долгий ожог.",
                "Одно перо может пережечь ночь.",
                CardEffectType.Burn, 24);
            AddCreature("koschey-guard", "Костяной Воевода", 7, 98, 4, "Кость", "bone-guard",
                "Даёт огромный щит и давит строем.",
                "Его войско не боится смерти по очевидной причине.",
                CardEffectType.Shield, 42);
            AddCreature("gamayun", "Гамаюн Вещая", 8, 120, 5, "Судьба", "oracle",
                "Бьёт и добирает две карты, раскрывая будущий ход.",
                "Она знает конец истории, но всё равно поёт красиво.",
                CardEffectType.Draw, 2);
            AddCreature("chernobog", "Чернобог на Пороге", 9, 160, 5, "Тьма", "black-sun",
                "Казнит раненого босса, добивая конец боя.",
                "Дверь открылась внутрь тени.",
                CardEffectType.Execute, 80);
            AddCreature("marya", "Марья-Моревна", 8, 132, 5, "Сталь", "war-maiden",
                "Пробивающий легендарный удар и стальная воля.",
                "Она не входит в легенду. Легенда строится вокруг неё.",
                CardEffectType.Pierce, 0);

            AddSpell("fern-flower", "Папоротников Цвет", 2, 26, 2, "Пламя", "fern",
                "Даёт дополнительную ману и открывает короткий рывок.",
                "Цветёт секунду. Этой секунды достаточно.",
                CardEffectType.Mana, 2);
            AddSpell("ice-whirl", "Ледяной Вихрь", 3, 44, 2, "Север", "ice",
                "Остужает босса, ослабляя его атаку.",
                "Север умеет говорить одним дыханием.",
                CardEffectType.Weak, 1);
            AddSpell("thunder", "Громовая Стрела", 4, 64, 3, "Гроза", "storm",
                "Пробивающий удар молнии.",
                "Небо не промахивается, оно просто выбирает момент.",
                CardEffectType.Pierce, 0);
            AddSpell("living-water", "Живая Вода", 3, 8, 3, "Река", "living-water",
                "Большое лечение вместо грубой силы.",
                "Не всякая вода течёт вниз. Эта тянет обратно к жизни.",
                CardEffectType.Heal, 42);
            AddSpell("ember-script", "Огненная Буквица", 4, 52, 3, "Пламя", "ember-rune",
                "Накладывает ожог древним словом.",
                "Буква вспыхнула раньше, чем её успели прочитать.",
                CardEffectType.Burn, 18);
            AddSpell("black-prayer", "Чёрный Заговор", 5, 70, 4, "Тьма", "dark-rune",
                "Вытягивает силу и лечит владельца.",
                "Слова уходят в землю, а возвращаются долгом.",
                CardEffectType.Drain, 30);
            AddSpell("star-salt", "Звёздная Соль", 5, 40, 4, "Судьба", "stars",
                "Добирает карты и ускоряет следующий удар.",
                "Её сыплют не в пищу, а в прорехи мира.",
                CardEffectType.Draw, 3);
            AddSpell("book-eclipse", "Затмение Книги", 7, 108, 5, "Тьма", "eclipse",
                "Оглушает босса и оставляет ожог на страницах.",
                "Когда книга закрывается, мир моргает.",
                CardEffectType.Stun, 1);

            AddTrap("witch-bind", "Ведьмины Путы", 2, 20, 1, "Тьма", "binds",
                "Дешёвая защита и слабость врага.",
                "Узел держит крепче, если его завязали молча.",
                CardEffectType.Shield, 18);
            AddTrap("bear-trap", "Медвежий Капкан", 4, 72, 2, "Лес", "trap",
                "Грубый урон по неосторожному врагу.",
                "Железо тоже умеет ждать.",
                CardEffectType.None, 0);
            AddTrap("mirror-marsh", "Зеркальная Топь", 3, 34, 3, "Болото", "mirror-swamp",
                "Отражает часть удара щитом и ослабляет босса.",
                "Вода показывает не лицо, а намерение.",
                CardEffectType.Weak, 2);
            AddTrap("raven-seal", "Вороний Знак", 4, 50, 3, "Судьба", "raven",
                "Добирает карту и оставляет метку на враге.",
                "Ворон не каркает дважды без причины.",
                CardEffectType.Draw, 1);
            AddTrap("silver-circle", "Серебряный Круг", 5, 38, 4, "Сталь", "silver-circle",
                "Мощная защита, которая переживает тяжёлый ход.",
                "Черта на полу иногда важнее стены.",
                CardEffectType.Shield, 55);
            AddTrap("grave-bell", "Погребальный Колокол", 6, 90, 4, "Кость", "bell",
                "Казнит врага, если бой уже переломлен.",
                "Он звонит не по мёртвым. Он зовёт новых.",
                CardEffectType.Execute, 45);
            AddTrap("iron-gate", "Железные Врата", 6, 64, 5, "Сталь", "gate",
                "Оглушает босса и даёт защиту.",
                "Некоторые двери лучше ставить посреди поля боя.",
                CardEffectType.Stun, 1);
        }

        private void AddCreature(string id, string name, int cost, int power, int rarity, string faction, string artKey,
            string description, string quote, CardEffectType effectType, int effectValue)
        {
            cardPool.Add(Create(new Creature(), id, name, cost, power, rarity, faction, artKey, description, quote, effectType, effectValue));
        }

        private void AddSpell(string id, string name, int cost, int power, int rarity, string faction, string artKey,
            string description, string quote, CardEffectType effectType, int effectValue)
        {
            cardPool.Add(Create(new Spell(), id, name, cost, power, rarity, faction, artKey, description, quote, effectType, effectValue));
        }

        private void AddTrap(string id, string name, int cost, int power, int rarity, string faction, string artKey,
            string description, string quote, CardEffectType effectType, int effectValue)
        {
            cardPool.Add(Create(new Trap(), id, name, cost, power, rarity, faction, artKey, description, quote, effectType, effectValue));
        }

        private Card Create(Card card, string id, string name, int cost, int power, int rarity, string faction, string artKey,
            string description, string quote, CardEffectType effectType, int effectValue)
        {
            card.Id = id;
            card.Name = name;
            card.Cost = cost;
            card.Power = power;
            card.Rarity = rarity;
            card.Faction = faction;
            card.ArtKey = artKey;
            card.Description = description;
            card.Quote = quote;
            card.EffectType = effectType;
            card.EffectValue = effectValue;
            return card;
        }

        public void OpenPack(int count)
        {
            for (int i = 0; i < count; i++)
            {
                Card newCard = RollCard();
                Profile.Collection.Add(newCard);
                OnCardOpened?.Invoke(newCard);
            }

            Profile.Save();
            OnGameUpdated?.Invoke();
        }

        private Card RollCard()
        {
            int roll = rng.Next(1, 101);
            int targetRarity;

            if (roll > 98)
                targetRarity = 5;
            else if (roll > 88)
                targetRarity = 4;
            else if (roll > 68)
                targetRarity = 3;
            else if (roll > 38)
                targetRarity = 2;
            else
                targetRarity = 1;

            List<Card> possibleCards = cardPool.Where(c => c.Rarity == targetRarity).ToList();
            if (possibleCards.Count == 0)
                possibleCards = cardPool.ToList();

            return CreateCardCopy(possibleCards[rng.Next(possibleCards.Count)]);
        }

        public Card CreateCardCopy(Card original)
        {
            Card clone;

            if (original is Spell)
                clone = new Spell();
            else if (original is Trap)
                clone = new Trap();
            else
                clone = new Creature();

            clone.Id = original.Id;
            clone.Name = original.Name;
            clone.Cost = original.Cost;
            clone.Power = original.Power;
            clone.Rarity = original.Rarity;
            clone.Description = original.Description;
            clone.Faction = original.Faction;
            clone.ArtKey = original.ArtKey;
            clone.Quote = original.Quote;
            clone.EffectType = original.EffectType;
            clone.EffectValue = original.EffectValue;

            return clone;
        }

        public Card GetRandomCard()
        {
            int randomIndex = rng.Next(cardPool.Count);
            return CreateCardCopy(cardPool[randomIndex]);
        }

        public Card GetRewardCard(int minimumRarity)
        {
            List<Card> possibleCards = cardPool.Where(c => c.Rarity >= minimumRarity).ToList();
            if (possibleCards.Count == 0)
                possibleCards = cardPool.ToList();

            return CreateCardCopy(possibleCards[rng.Next(possibleCards.Count)]);
        }

        public void SaveAll()
        {
            Profile.Save();
            if (CurrentDeck != null)
                Storage.Save(CurrentDeck);
            OnGameUpdated?.Invoke();
        }

        public int GetDeckPower()
        {
            if (CurrentDeck == null) return 0;
            return CurrentDeck.Cards.Sum(card => card.Power);
        }

        public double GetAverageDeckCost()
        {
            if (CurrentDeck == null || CurrentDeck.Cards.Count == 0) return 0;
            int total = CurrentDeck.Cards.Sum(card => card.Cost);
            return (double)total / CurrentDeck.Cards.Count;
        }

        public List<Card> GetAllCards()
        {
            return cardPool.Select(CreateCardCopy).ToList();
        }

        public int GetTotalCardsCount() => cardPool.Count;

        public Dictionary<int, int> GetCardsByRarityStats()
        {
            Dictionary<int, int> stats = new Dictionary<int, int>();
            for (int i = 1; i <= 5; i++)
                stats[i] = 0;

            foreach (Card card in cardPool)
            {
                if (stats.ContainsKey(card.Rarity))
                    stats[card.Rarity]++;
            }
            return stats;
        }
    }
}
