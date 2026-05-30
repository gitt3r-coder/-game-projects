using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using BlackBookAppWPF.Controls;

namespace BlackBookAppWPF
{
    public partial class BattleWindow : Window
    {
        private readonly Game game;
        private readonly string bossName;
        private readonly int bossLevel;
        private readonly MissionDefinition mission;
        private readonly Random rng = new Random();

        private readonly List<Card> drawPile = new List<Card>();
        private readonly List<Card> discardPile = new List<Card>();
        private readonly List<Card> hand = new List<Card>();
        private readonly List<CardControl> handControls = new List<CardControl>();

        private int playerMaxHP;
        private int playerHP;
        private int playerShield;
        private int playerBurn;
        private int mana;
        private int maxMana;

        private int bossMaxHP;
        private int bossHP;
        private int bossShield;
        private int bossBurn;
        private int bossWeak;
        private bool bossStunned;
        private string bossGlyph;
        private BossIntent currentIntent;

        private int turn;
        private Card selectedCard;

        public BattleWindow(Game game, string bossName, int bossLevel)
        {
            InitializeComponent();
            this.game = game;
            this.bossName = bossName;
            this.bossLevel = bossLevel;

            InitializeBattle();
        }

        public BattleWindow(Game game, MissionDefinition mission)
        {
            InitializeComponent();
            this.game = game;
            this.mission = mission;
            this.bossName = mission.EnemyName;
            this.bossLevel = mission.BossLevel;

            InitializeBattle();
        }

        private void InitializeBattle()
        {
            playerMaxHP = 110 + Math.Min(40, game.GetDeckPower() / 120);
            playerHP = playerMaxHP;
            bossMaxHP = mission?.BossHealth ?? 240 + bossLevel * 180;
            bossHP = bossMaxHP;
            bossGlyph = mission?.Glyph ?? (bossLevel >= 2 ? "♛" : "♜");

            lblBossTitle.Text = mission == null ? bossName : $"{mission.Number}. {mission.Title}";
            lblBossGlyph.Text = bossGlyph;

            List<Card> source = game.CurrentDeck.Cards.Select(game.CreateCardCopy).ToList();
            drawPile.AddRange(source.OrderBy(c => rng.Next()));

            AddLog(mission?.Intro ?? "Книга раскрыта. Чернила шевелятся на страницах.");
            AddLog($"Противник: {bossName}. Уровень угрозы: {bossLevel}.");
            if (mission != null)
                AddLog($"Цель: {mission.Objective}");
            lblBattlePulse.Text = mission?.Objective ?? "Черная книга раскрыта";
            ShowAction(mission == null ? "Бой начинается" : $"Миссия {mission.Number}");
            StartPlayerTurn();
        }

        private void StartPlayerTurn()
        {
            turn++;
            playerShield = 0;
            bossShield = Math.Max(0, bossShield / 2);

            ApplyStartOfTurnEffects();
            if (CheckBattleEnd())
                return;

            maxMana = Math.Min(10, 2 + turn);
            mana = maxMana;

            DrawUntilHandSize(5);
            RollBossIntent();
            selectedCard = null;

            AddLog($"Ход {turn}: мана восстановлена до {mana}.");
            ShowAction($"Ваш ход {turn}");
            ShowFloatingText($"+{mana} маны", FloatingTarget.Center, Brushes.DeepSkyBlue);
            RefreshHand(true);
            UpdateUI();
        }

        private void ApplyStartOfTurnEffects()
        {
            if (bossBurn > 0)
            {
                int damage = bossBurn;
                bossHP -= damage;
                AddLog($"Ожог босса наносит {damage} урона.");
                ShowAction("Ожог срабатывает");
                ShowFloatingText($"-{damage}", FloatingTarget.Boss, Brushes.OrangeRed);
                PulseBoss();
                bossBurn = Math.Max(0, bossBurn - 6);
            }

            if (playerBurn > 0)
            {
                int damage = playerBurn;
                playerHP -= damage;
                AddLog($"Ожог игрока наносит {damage} урона.");
                ShowFloatingText($"-{damage}", FloatingTarget.Player, Brushes.OrangeRed);
                PulsePanel(TopPanel);
                playerBurn = Math.Max(0, playerBurn - 5);
            }
        }

        private void DrawUntilHandSize(int targetSize)
        {
            while (hand.Count < targetSize)
            {
                Card card = DrawCard();
                if (card == null)
                    break;

                hand.Add(card);
            }
        }

        private Card DrawCard()
        {
            if (drawPile.Count == 0)
                ReshuffleDiscard();

            if (drawPile.Count == 0)
                return null;

            Card card = drawPile[0];
            drawPile.RemoveAt(0);
            return card;
        }

        private void DrawCards(int count)
        {
            for (int i = 0; i < count; i++)
            {
                Card card = DrawCard();
                if (card == null)
                {
                    AddLog("В книге больше нет готовых страниц для добора.");
                    return;
                }

                hand.Add(card);
                AddLog($"Добрана карта: {card.Name}.");
                ShowFloatingText("+карта", FloatingTarget.Center, Brushes.LightSkyBlue);
            }
        }

        private void ReshuffleDiscard()
        {
            if (discardPile.Count == 0)
                return;

            drawPile.AddRange(discardPile.OrderBy(c => rng.Next()));
            discardPile.Clear();
            AddLog("Сброс перемешан обратно в книгу.");
            ShowAction("Книга перемешана");
        }

        private void RollBossIntent()
        {
            int roll = rng.Next(100);
            int baseDamage = 16 + bossLevel * 9 + turn * 2;

            if (roll < 44)
            {
                int damage = ScaleBossDamage(baseDamage);
                currentIntent = new BossIntent("Режущий выпад", $"Атака {damage}", damage, 0, 0, 0);
            }
            else if (roll < 64)
            {
                int damage = ScaleBossDamage(baseDamage + 18);
                currentIntent = new BossIntent("Тяжёлый удар", $"Сильная атака {damage}", damage, 0, 0, 0);
            }
            else if (roll < 80)
            {
                int damage = ScaleBossDamage(8 * bossLevel);
                int shield = ScaleBossShield(28 * bossLevel);
                currentIntent = new BossIntent("Костяной заслон", $"Щит {shield}, атака {damage}", damage, shield, 0, 0);
            }
            else if (roll < 93)
            {
                int damage = ScaleBossDamage(10 * bossLevel);
                int burn = Math.Max(1, ScaleBossDamage(8 * bossLevel) / 2);
                currentIntent = new BossIntent("Проклятая гарь", $"Ожог {burn}, атака {damage}", damage, 0, burn, 0);
            }
            else
            {
                int damage = ScaleBossDamage(14 * bossLevel);
                int heal = ScaleBossShield(22 * bossLevel);
                currentIntent = new BossIntent("Пожирание света", $"Атака {damage}, лечение {heal}", damage, 0, 0, heal);
            }
        }

        private int ScaleBossDamage(int baseValue)
        {
            double multiplier = mission?.DamageMultiplier ?? 1.0;
            return Math.Max(1, (int)Math.Round(baseValue * multiplier));
        }

        private int ScaleBossShield(int baseValue)
        {
            double multiplier = mission?.ShieldMultiplier ?? 1.0;
            return Math.Max(1, (int)Math.Round(baseValue * multiplier));
        }

        private void RefreshHand(bool animate = false)
        {
            HandPanel.Children.Clear();
            handControls.Clear();

            for (int i = 0; i < hand.Count; i++)
            {
                Card card = hand[i];
                CardControl control = new CardControl
                {
                    Card = card,
                    Margin = new Thickness(7),
                    Tag = i,
                    Opacity = card.Cost <= mana ? 1.0 : 0.58,
                    ToolTip = card.Cost <= mana
                        ? $"Можно сыграть: стоит {card.Cost} маны"
                        : $"Не хватает маны: стоит {card.Cost}, доступно {mana}"
                };

                control.OnCardClicked += HandCardClicked;
                HandPanel.Children.Add(control);
                handControls.Add(control);

                if (animate)
                    AnimateCardEnter(control, i);
            }
        }

        private void HandCardClicked(object sender, Card card)
        {
            selectedCard = card;

            foreach (CardControl control in handControls)
                control.SetSelectState(ReferenceEquals(control.Card, card));

            lblSelectedCardName.Text = $"{card.Name} ({card.TypeName})";
            lblSelectedCardText.Text =
                $"Стоимость: {card.Cost}\n" +
                $"Урон: {card.Power}\n" +
                $"Эффект: {card.EffectText}\n\n" +
                $"{card.Description}\n\n" +
                $"{card.Quote}";

            btnPlayCard.IsEnabled = true;
            PulsePanel(SelectedInfoPanel);
        }

        private void BtnPlayCard_Click(object sender, RoutedEventArgs e)
        {
            if (selectedCard == null)
                return;

            if (selectedCard.Cost > mana)
            {
                AddLog($"Не хватает маны для карты {selectedCard.Name}: нужно {selectedCard.Cost}, есть {mana}.");
                ShowAction("Не хватает маны");
                ShowFloatingText("мало маны", FloatingTarget.Center, Brushes.LightSkyBlue);
                UpdateUI();
                return;
            }

            mana -= selectedCard.Cost;
            ResolveCard(selectedCard);
            discardPile.Add(selectedCard);
            hand.Remove(selectedCard);
            selectedCard = null;
            btnPlayCard.IsEnabled = false;
            lblSelectedCardName.Text = "Нет выбора";
            lblSelectedCardText.Text = "Выберите карту из руки. Здесь появится стоимость, урон, эффект и роль карты в текущем ходе.";

            if (CheckBattleEnd())
                return;

            RefreshHand(false);
            UpdateUI();
        }

        private void ResolveCard(Card card)
        {
            AddLog($"Сыграна карта: {card.Name}.");
            ShowAction(card.Name);

            if (card.EffectType == CardEffectType.DoubleStrike)
            {
                DealBossDamage(Math.Max(1, card.Power / 2), false, card.Name);
                DealBossDamage(card.Power - Math.Max(1, card.Power / 2), false, card.Name);
            }
            else
            {
                bool pierce = card.EffectType == CardEffectType.Pierce;
                DealBossDamage(card.Power, pierce, card.Name);
            }

            switch (card.EffectType)
            {
                case CardEffectType.Shield:
                    playerShield += card.EffectValue;
                    AddLog($"Получен щит: {card.EffectValue}.");
                    ShowFloatingText($"+{card.EffectValue} щит", FloatingTarget.Player, Brushes.LightSkyBlue);
                    PulsePanel(TopPanel);
                    break;
                case CardEffectType.Heal:
                    HealPlayer(card.EffectValue);
                    break;
                case CardEffectType.Burn:
                    bossBurn += card.EffectValue;
                    AddLog($"На босса наложен ожог: {card.EffectValue}.");
                    ShowFloatingText($"ожог {card.EffectValue}", FloatingTarget.Boss, Brushes.Orange);
                    break;
                case CardEffectType.Weak:
                    bossWeak += card.EffectValue;
                    AddLog($"Босс ослаблен на {card.EffectValue} ход.");
                    ShowFloatingText("слабость", FloatingTarget.Boss, Brushes.MediumPurple);
                    PulseBoss();
                    break;
                case CardEffectType.Draw:
                    DrawCards(card.EffectValue);
                    RefreshHand(true);
                    break;
                case CardEffectType.Drain:
                    HealPlayer(card.EffectValue);
                    break;
                case CardEffectType.Execute:
                    if (bossHP <= bossMaxHP * 0.35)
                    {
                        DealBossDamage(card.EffectValue, true, "Казнь");
                        AddLog("Казнь сработала по раненому боссу.");
                        ShowAction("Казнь!");
                    }
                    else
                    {
                        AddLog("Казнь ждёт, пока босс будет ниже 35% здоровья.");
                        ShowFloatingText("рано", FloatingTarget.Boss, Brushes.LightGray);
                    }
                    break;
                case CardEffectType.Mana:
                    mana = Math.Min(10, mana + card.EffectValue);
                    AddLog($"Искра возвращает {card.EffectValue} маны.");
                    ShowFloatingText($"+{card.EffectValue} маны", FloatingTarget.Center, Brushes.DeepSkyBlue);
                    break;
                case CardEffectType.Stun:
                    bossStunned = true;
                    if (card is Trap)
                    {
                        int trapShield = 24 + bossLevel * 8;
                        playerShield += trapShield;
                        AddLog("Ловушка ставит заслон перед игроком.");
                        ShowFloatingText($"+{trapShield} щит", FloatingTarget.Player, Brushes.LightSkyBlue);
                    }
                    AddLog("Босс оглушён и пропустит действие.");
                    ShowFloatingText("оглушение", FloatingTarget.Boss, Brushes.Gold);
                    PulseBoss();
                    break;
            }

            lblBattlePulse.Text = card.Rarity >= 5
                ? $"Легендарная карта гремит по страницам: {card.Name}"
                : card.EffectText;
        }

        private void DealBossDamage(int amount, bool pierce, string source)
        {
            int damage = amount;
            if (!pierce && bossShield > 0)
            {
                int blocked = Math.Min(bossShield, damage);
                bossShield -= blocked;
                damage -= blocked;
                AddLog($"Щит босса поглотил {blocked} урона.");
                ShowFloatingText($"-{blocked} щит", FloatingTarget.Boss, Brushes.LightSkyBlue);
            }

            if (damage > 0)
            {
                bossHP -= damage;
                AddLog($"{source}: босс получает {damage} урона.");
                ShowFloatingText($"-{damage}", FloatingTarget.Boss, pierce ? Brushes.White : Brushes.OrangeRed);
                PulseBoss();
            }
            else
            {
                AddLog($"{source}: урон полностью заблокирован.");
                ShowFloatingText("блок", FloatingTarget.Boss, Brushes.LightSkyBlue);
            }
        }

        private void HealPlayer(int amount)
        {
            int before = playerHP;
            playerHP = Math.Min(playerMaxHP, playerHP + amount);
            int healed = playerHP - before;
            AddLog($"Игрок восстановил {healed} здоровья.");
            ShowFloatingText($"+{healed}", FloatingTarget.Player, Brushes.LightGreen);
            PulsePanel(TopPanel);
        }

        private void BtnEndTurn_Click(object sender, RoutedEventArgs e)
        {
            foreach (Card card in hand.ToList())
            {
                hand.Remove(card);
                discardPile.Add(card);
            }

            selectedCard = null;
            btnPlayCard.IsEnabled = false;
            ShowAction("Ход босса");
            BossTurn();

            if (CheckBattleEnd())
                return;

            StartPlayerTurn();
        }

        private void BossTurn()
        {
            if (bossStunned)
            {
                bossStunned = false;
                AddLog("Босс пропускает ход из-за оглушения.");
                ShowAction("Босс оглушён");
                ShowFloatingText("пропуск", FloatingTarget.Boss, Brushes.Gold);
                return;
            }

            if (currentIntent == null)
                RollBossIntent();

            AddLog($"Босс использует: {currentIntent.Name}.");
            ShowAction(currentIntent.Name);
            PulseBoss();

            if (currentIntent.Shield > 0)
            {
                bossShield += currentIntent.Shield;
                AddLog($"Босс получает щит: {currentIntent.Shield}.");
                ShowFloatingText($"+{currentIntent.Shield} щит", FloatingTarget.Boss, Brushes.LightSkyBlue);
            }

            if (currentIntent.Damage > 0)
            {
                int damage = currentIntent.Damage;
                if (bossWeak > 0)
                {
                    damage = (int)Math.Ceiling(damage * 0.65);
                    AddLog("Слабость снижает атаку босса.");
                    ShowFloatingText("слабее", FloatingTarget.Boss, Brushes.MediumPurple);
                }
                TakePlayerDamage(damage);
            }

            if (currentIntent.Burn > 0)
            {
                playerBurn += currentIntent.Burn;
                AddLog($"Игрок получает ожог: {currentIntent.Burn}.");
                ShowFloatingText($"ожог {currentIntent.Burn}", FloatingTarget.Player, Brushes.Orange);
            }

            if (currentIntent.Heal > 0)
            {
                int before = bossHP;
                bossHP = Math.Min(bossMaxHP, bossHP + currentIntent.Heal);
                int healed = bossHP - before;
                AddLog($"Босс восстановил {healed} здоровья.");
                ShowFloatingText($"+{healed}", FloatingTarget.Boss, Brushes.LightGreen);
            }

            if (bossWeak > 0)
                bossWeak--;
        }

        private void TakePlayerDamage(int amount)
        {
            int damage = amount;
            if (playerShield > 0)
            {
                int blocked = Math.Min(playerShield, damage);
                playerShield -= blocked;
                damage -= blocked;
                AddLog($"Щит игрока поглотил {blocked} урона.");
                ShowFloatingText($"-{blocked} щит", FloatingTarget.Player, Brushes.LightSkyBlue);
            }

            if (damage > 0)
            {
                playerHP -= damage;
                AddLog($"Игрок получает {damage} урона.");
                ShowFloatingText($"-{damage}", FloatingTarget.Player, Brushes.OrangeRed);
                PulsePanel(TopPanel);
            }
        }

        private bool CheckBattleEnd()
        {
            UpdateUI();

            if (bossHP <= 0)
            {
                WinBattle();
                return true;
            }

            if (playerHP <= 0)
            {
                LoseBattle();
                return true;
            }

            return false;
        }

        private void WinBattle()
        {
            int reward = mission?.RewardGold ?? 160 + bossLevel * 190 + Math.Max(0, 8 - turn) * 25;
            int rewardRarity = mission?.RewardRarity ?? Math.Min(5, bossLevel + 2);
            Card rewardCard = game.GetRewardCard(rewardRarity);

            game.Profile.Gold += reward;
            game.Profile.Collection.Add(rewardCard);
            game.Profile.BossesDefeated = Math.Max(game.Profile.BossesDefeated, bossLevel);
            if (mission != null)
                game.Profile.HighestMissionCompleted = Math.Max(game.Profile.HighestMissionCompleted, mission.Number);
            game.Profile.Save();

            AddLog($"Победа. Награда: {reward} золота и карта {rewardCard.Name}.");
            ShowAction("Победа!");
            string nextMissionText = mission == null ? "" : $"\nПрогресс кампании: {game.Profile.HighestMissionCompleted}/{game.Missions.Count}";
            MessageBox.Show($"ПОБЕДА!\n\nНаграда: {reward} золота\nНовая карта: {rewardCard.Name} ({rewardCard.RarityName}){nextMissionText}",
                "Победа", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }

        private void LoseBattle()
        {
            AddLog("Поражение. Книга захлопнулась.");
            ShowAction("Поражение");
            MessageBox.Show("Поражение. Усильте колоду, добавьте щиты, лечение и контроль, затем возвращайтесь.",
                "Поражение", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = false;
            Close();
        }

        private void UpdateUI()
        {
            lblPlayerHP.Text = $"Игрок: {Math.Max(0, playerHP)}/{playerMaxHP}";
            lblBossHP.Text = $"{bossName}: {Math.Max(0, bossHP)}/{bossMaxHP}";
            lblPlayerShield.Text = $"Щит: {playerShield}";
            lblBossShield.Text = $"Щит: {bossShield}";
            lblMana.Text = $"Мана: {mana}/{maxMana}";
            UpdateManaPips();
            lblDeckStats.Text = $"Колода {drawPile.Count} | Сброс {discardPile.Count}";
            lblTurn.Text = $"Ход {turn}";
            lblIntent.Text = currentIntent == null ? "Намерение скрыто" : $"Намерение: {currentIntent.Description}";

            List<string> statuses = new List<string>();
            if (bossBurn > 0) statuses.Add($"ожог {bossBurn}");
            if (bossWeak > 0) statuses.Add($"слабость {bossWeak}");
            if (bossStunned) statuses.Add("оглушение");
            lblBossStatus.Text = $"Статусы: {(statuses.Count == 0 ? "нет" : string.Join(", ", statuses))}";

            playerHealthBar.Maximum = playerMaxHP;
            playerHealthBar.Value = Math.Max(0, Math.Min(playerMaxHP, playerHP));
            bossHealthBar.Maximum = bossMaxHP;
            bossHealthBar.Value = Math.Max(0, Math.Min(bossMaxHP, bossHP));
        }

        private void UpdateManaPips()
        {
            ManaPipsPanel.Items.Clear();
            int visibleMax = Math.Max(maxMana, mana);

            for (int i = 1; i <= visibleMax; i++)
            {
                bool filled = i <= mana;
                ManaPipsPanel.Items.Add(new Ellipse
                {
                    Width = 13,
                    Height = 13,
                    Margin = new Thickness(2, 1, 2, 0),
                    Fill = filled
                        ? new SolidColorBrush(Color.FromRgb(78, 220, 255))
                        : new SolidColorBrush(Color.FromRgb(45, 54, 68)),
                    Stroke = new SolidColorBrush(Color.FromRgb(150, 235, 255)),
                    StrokeThickness = filled ? 1.4 : 0.7,
                    ToolTip = filled ? "Доступная мана" : "Потраченная мана"
                });
            }
        }

        private void AddLog(string text)
        {
            BattleLogList.Items.Insert(0, text);
            while (BattleLogList.Items.Count > 80)
                BattleLogList.Items.RemoveAt(BattleLogList.Items.Count - 1);
        }

        private void ShowAction(string text)
        {
            lblActionBanner.Text = text;
            ActionBanner.Opacity = 0;

            ScaleTransform scale = ActionBanner.RenderTransform as ScaleTransform;
            if (scale != null)
            {
                scale.ScaleX = 0.96;
                scale.ScaleY = 0.96;
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(1.04, 1, TimeSpan.FromMilliseconds(320)));
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(1.04, 1, TimeSpan.FromMilliseconds(320)));
            }

            DoubleAnimation fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(120));
            DoubleAnimation fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(420))
            {
                BeginTime = TimeSpan.FromMilliseconds(900)
            };

            Storyboard storyboard = new Storyboard();
            storyboard.Children.Add(fadeIn);
            storyboard.Children.Add(fadeOut);
            Storyboard.SetTarget(fadeIn, ActionBanner);
            Storyboard.SetTarget(fadeOut, ActionBanner);
            Storyboard.SetTargetProperty(fadeIn, new PropertyPath(OpacityProperty));
            Storyboard.SetTargetProperty(fadeOut, new PropertyPath(OpacityProperty));
            storyboard.Begin();
        }

        private void ShowFloatingText(string text, FloatingTarget target, Brush brush)
        {
            TextBlock label = new TextBlock
            {
                Text = text,
                FontSize = target == FloatingTarget.Center ? 20 : 26,
                FontWeight = FontWeights.Bold,
                Foreground = brush,
                Opacity = 0,
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    BlurRadius = 10,
                    ShadowDepth = 0,
                    Opacity = 0.75
                },
                RenderTransform = new TranslateTransform()
            };

            BattleEffectsCanvas.Children.Add(label);
            Point start = GetFloatingStart(target);
            Canvas.SetLeft(label, start.X + rng.Next(-25, 25));
            Canvas.SetTop(label, start.Y + rng.Next(-10, 10));

            TranslateTransform translate = (TranslateTransform)label.RenderTransform;
            DoubleAnimation fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(90));
            DoubleAnimation rise = new DoubleAnimation(0, -54, TimeSpan.FromMilliseconds(760))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            DoubleAnimation fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(280))
            {
                BeginTime = TimeSpan.FromMilliseconds(520)
            };

            fadeOut.Completed += (s, e) => BattleEffectsCanvas.Children.Remove(label);
            fadeIn.Completed += (s, e) => label.BeginAnimation(OpacityProperty, fadeOut);
            label.BeginAnimation(OpacityProperty, fadeIn);
            translate.BeginAnimation(TranslateTransform.YProperty, rise);
        }

        private Point GetFloatingStart(FloatingTarget target)
        {
            double width = BattleStage.ActualWidth > 0 ? BattleStage.ActualWidth : 470;
            double height = BattleStage.ActualHeight > 0 ? BattleStage.ActualHeight : 280;

            switch (target)
            {
                case FloatingTarget.Player:
                    return new Point(width * 0.16, height * 0.66);
                case FloatingTarget.Boss:
                    return new Point(width * 0.52, height * 0.46);
                default:
                    return new Point(width * 0.42, height * 0.24);
            }
        }

        private void PulseBoss()
        {
            PulseScale(lblBossGlyph, 1.0, 1.13);
            DoubleAnimation aura = new DoubleAnimation(0.08, 0.28, TimeSpan.FromMilliseconds(120))
            {
                AutoReverse = true
            };
            BossAura.BeginAnimation(OpacityProperty, aura);
        }

        private void PulsePanel(UIElement element)
        {
            PulseScale(element, 1.0, 1.015);
        }

        private void PulseScale(UIElement element, double from, double to)
        {
            ScaleTransform scale = element.RenderTransform as ScaleTransform;
            if (scale == null)
            {
                scale = new ScaleTransform(1, 1);
                element.RenderTransform = scale;
                element.RenderTransformOrigin = new Point(0.5, 0.5);
            }

            DoubleAnimation pulse = new DoubleAnimation(from, to, TimeSpan.FromMilliseconds(110))
            {
                AutoReverse = true
            };
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, pulse);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, pulse);
        }

        private void AnimateCardEnter(UIElement element, int index)
        {
            element.Opacity = 0;
            TranslateTransform translate = new TranslateTransform(0, 18);
            element.RenderTransform = translate;

            DoubleAnimation fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(240))
            {
                BeginTime = TimeSpan.FromMilliseconds(index * 45)
            };
            DoubleAnimation slide = new DoubleAnimation(18, 0, TimeSpan.FromMilliseconds(260))
            {
                BeginTime = TimeSpan.FromMilliseconds(index * 45),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            element.BeginAnimation(OpacityProperty, fade);
            translate.BeginAnimation(TranslateTransform.YProperty, slide);
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Сдаться и покинуть бой?", "Сдаться",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                DialogResult = false;
                Close();
            }
        }

        private enum FloatingTarget
        {
            Player,
            Boss,
            Center
        }

        private class BossIntent
        {
            public string Name { get; }
            public string Description { get; }
            public int Damage { get; }
            public int Shield { get; }
            public int Burn { get; }
            public int Heal { get; }

            public BossIntent(string name, string description, int damage, int shield, int burn, int heal)
            {
                Name = name;
                Description = description;
                Damage = damage;
                Shield = shield;
                Burn = burn;
                Heal = heal;
            }
        }
    }
}
