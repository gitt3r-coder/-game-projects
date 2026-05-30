using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BlackBookAppWPF
{
    public partial class MainWindow : Window
    {
        private Game game;

        public MainWindow()
        {
            InitializeComponent();
            game = new Game();
            game.OnGameUpdated += UpdateUI;
            UpdateUI();
            DisplayCampaign();
        }

        private void UpdateUI()
        {
            if (game?.Profile != null)
            {
                lblGold.Text = $"💰 Золото: {game.Profile.Gold}";
                lblCards.Text = $"📚 Карт: {game.Profile.Collection.Count}";
                lblMissionProgress.Text = $"🏰 Миссии: {game.Profile.HighestMissionCompleted}/{game.Missions.Count}";
            }

            if (game?.CurrentDeck != null)
            {
                int cardCount = game.CurrentDeck.Cards.Count;
                lblDeckSize.Text = $"📦 Колода: {cardCount}/50";
                lblDeckPower.Text = $"⚔️ Общая сила: {game.GetDeckPower()}";
                lblDeckAvgCost.Text = $"💧 Средняя стоимость: {game.GetAverageDeckCost():F1}";
                lblDeckCardCount.Text = $"🃏 Количество карт: {cardCount}";

                DeckListBox.Items.Clear();
                for (int i = 0; i < game.CurrentDeck.Cards.Count; i++)
                {
                    Card card = game.CurrentDeck.Cards[i];
                    string stars = new string('★', card.Rarity);
                    string emptyStars = new string('☆', 5 - card.Rarity);
                    DeckListBox.Items.Add($"{i + 1}. {card.Icon} {card.Name} | ⚔️{card.Power} | 💧{card.Cost} | {card.EffectText} | {stars}{emptyStars}");
                }
            }
            else
            {
                lblDeckSize.Text = "📦 Колода: 0/50";
                lblDeckPower.Text = "⚔️ Общая сила: 0";
                lblDeckAvgCost.Text = "💧 Средняя стоимость: 0";
                lblDeckCardCount.Text = "🃏 Количество карт: 0";
                DeckListBox.Items.Clear();
                DeckListBox.Items.Add("❌ Нет загруженной колоды");
            }
        }

        private void DisplayCards(string title, IEnumerable<Card> cards, bool showAddButton = false)
        {
            CardDisplayPanel.Items.Clear();

            List<Card> orderedCards = cards
                .OrderByDescending(c => c.Rarity)
                .ThenBy(c => c.CardType)
                .ThenBy(c => c.Cost)
                .ThenBy(c => c.Name)
                .ToList();

            CardDisplayPanel.Items.Add(CreateDisplayHeader(title, orderedCards, showAddButton));

            int? currentRarity = null;
            foreach (Card card in orderedCards)
            {
                if (!currentRarity.HasValue || currentRarity.Value != card.Rarity)
                {
                    currentRarity = card.Rarity;
                    CardDisplayPanel.Items.Add(CreateRarityHeader(card.Rarity, orderedCards.Count(c => c.Rarity == card.Rarity)));
                }

                Controls.CardControl cardControl = new Controls.CardControl
                {
                    Card = card,
                    Margin = new Thickness(7)
                };

                cardControl.OnCardClicked += (s, c) =>
                {
                    if (showAddButton && game.CurrentDeck != null && game.CurrentDeck.Cards.Count < 50)
                    {
                        game.CurrentDeck.Cards.Add(c);
                        UpdateUI();
                        MessageBox.Show($"Карта {c.Name} добавлена в колоду!\nЭффект: {c.EffectText}", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else if (showAddButton && game.CurrentDeck != null && game.CurrentDeck.Cards.Count >= 50)
                    {
                        MessageBox.Show("Колода полна! Максимум 50 карт.", "Предупреждение",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    else if (showAddButton && game.CurrentDeck == null)
                    {
                        MessageBox.Show("Сначала создайте колоду через редактор!", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                };

                CardDisplayPanel.Items.Add(cardControl);
            }
        }

        private Border CreateDisplayHeader(string title, List<Card> cards, bool isDeckEditor)
        {
            int uniqueCards = cards.Select(c => c.Id ?? c.Name).Distinct().Count();
            int totalPower = cards.Sum(c => c.Power);
            double avgCost = cards.Count == 0 ? 0 : cards.Average(c => c.Cost);

            StackPanel panel = new StackPanel();
            panel.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap
            });
            panel.Children.Add(new TextBlock
            {
                Text = $"{cards.Count} карт | уникальных: {uniqueCards} | сила: {totalPower} | средняя стоимость: {avgCost:F1}",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(178, 219, 232)),
                Margin = new Thickness(0, 6, 0, 0),
                TextWrapping = TextWrapping.Wrap
            });
            panel.Children.Add(new TextBlock
            {
                Text = isDeckEditor
                    ? "Сортировка: редкость, тип, стоимость и имя. Нажмите карту, чтобы добавить её в колоду."
                    : "Сортировка: редкость, тип, стоимость и имя. Одинаковые карты сохранены как отдельные экземпляры коллекции.",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(144, 150, 168)),
                Margin = new Thickness(0, 5, 0, 0),
                TextWrapping = TextWrapping.Wrap
            });

            return new Border
            {
                Width = 820,
                Background = new SolidColorBrush(Color.FromRgb(31, 37, 51)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(65, 92, 110)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(16),
                Margin = new Thickness(8, 8, 8, 12),
                Child = panel
            };
        }

        private Border CreateRarityHeader(int rarity, int count)
        {
            return new Border
            {
                Width = 820,
                Background = GetRarityHeaderBrush(rarity),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(8, 8, 8, 4),
                Child = new TextBlock
                {
                    Text = $"{GetRarityTitle(rarity)} | {count} карт",
                    FontSize = 15,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White,
                    TextWrapping = TextWrapping.Wrap
                }
            };
        }

        private string GetRarityTitle(int rarity)
        {
            switch (rarity)
            {
                case 5: return "★★★★★ Легендарные";
                case 4: return "★★★★ Эпические";
                case 3: return "★★★ Редкие";
                case 2: return "★★ Необычные";
                default: return "★ Обычные";
            }
        }

        private Brush GetRarityHeaderBrush(int rarity)
        {
            switch (rarity)
            {
                case 5: return new LinearGradientBrush(Color.FromRgb(125, 88, 22), Color.FromRgb(49, 37, 24), 0);
                case 4: return new LinearGradientBrush(Color.FromRgb(94, 43, 132), Color.FromRgb(37, 31, 55), 0);
                case 3: return new LinearGradientBrush(Color.FromRgb(35, 89, 130), Color.FromRgb(27, 39, 54), 0);
                case 2: return new LinearGradientBrush(Color.FromRgb(35, 105, 68), Color.FromRgb(28, 47, 40), 0);
                default: return new LinearGradientBrush(Color.FromRgb(70, 78, 88), Color.FromRgb(34, 38, 45), 0);
            }
        }

        private void DisplayCampaign()
        {
            CardDisplayPanel.Items.Clear();
            CardDisplayPanel.Items.Add(CreateCampaignHeader());

            foreach (MissionDefinition mission in game.Missions)
                CardDisplayPanel.Items.Add(CreateMissionCard(mission));
        }

        private Border CreateCampaignHeader()
        {
            StackPanel panel = new StackPanel();
            panel.Children.Add(new TextBlock
            {
                Text = "🏰 КАМПАНИЯ: БОССЫ ЧЕРНОЙ КНИГИ",
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap
            });
            panel.Children.Add(new TextBlock
            {
                Text = "Проходите миссии по порядку. Каждая следующая сложнее: больше здоровья, сильнее атаки, лучше щиты и ценнее награды.",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(190, 225, 235)),
                Margin = new Thickness(0, 8, 0, 0),
                TextWrapping = TextWrapping.Wrap
            });
            panel.Children.Add(new TextBlock
            {
                Text = "Перед боем: откройте бустеры в магазине, зайдите в редактор колоды, нажимайте карты для добавления, затем нажмите «Сохранить».",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(255, 216, 130)),
                Margin = new Thickness(0, 6, 0, 0),
                TextWrapping = TextWrapping.Wrap
            });

            return new Border
            {
                Width = 820,
                Background = new SolidColorBrush(Color.FromRgb(31, 37, 51)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(65, 92, 110)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(16),
                Margin = new Thickness(8, 8, 8, 12),
                Child = panel
            };
        }

        private Border CreateMissionCard(MissionDefinition mission)
        {
            bool unlocked = mission.Number <= game.Profile.HighestMissionCompleted + 1;
            bool completed = mission.Number <= game.Profile.HighestMissionCompleted;
            int deckPower = game.GetDeckPower();

            StackPanel panel = new StackPanel();
            panel.Children.Add(new TextBlock
            {
                Text = $"{mission.Number}. {mission.Title}",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = completed ? Brushes.LightGreen : Brushes.White,
                TextWrapping = TextWrapping.Wrap
            });
            panel.Children.Add(new TextBlock
            {
                Text = $"Босс: {mission.EnemyName} | здоровье: {mission.BossHealth} | награда: {mission.RewardGold} золота + карта",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(210, 218, 232)),
                Margin = new Thickness(0, 6, 0, 0),
                TextWrapping = TextWrapping.Wrap
            });
            panel.Children.Add(new TextBlock
            {
                Text = $"Цель: {mission.Objective}",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(166, 178, 196)),
                Margin = new Thickness(0, 6, 0, 0),
                TextWrapping = TextWrapping.Wrap
            });
            panel.Children.Add(new TextBlock
            {
                Text = $"Рекомендуемая сила колоды: {mission.RecommendedDeckPower}. Сейчас: {deckPower}.",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = deckPower >= mission.RecommendedDeckPower ? Brushes.LightGreen : Brushes.Orange,
                Margin = new Thickness(0, 6, 0, 0),
                TextWrapping = TextWrapping.Wrap
            });

            Button startButton = new Button
            {
                Content = completed ? "ПЕРЕИГРАТЬ МИССИЮ" : unlocked ? "НАЧАТЬ МИССИЮ" : "ЗАКРЫТО",
                Height = 38,
                Margin = new Thickness(0, 12, 0, 0),
                Background = unlocked ? new SolidColorBrush(Color.FromRgb(120, 48, 48)) : Brushes.Gray,
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                IsEnabled = unlocked,
                ToolTip = unlocked ? "Запустить одиночный бой против этого босса" : "Сначала пройдите предыдущую миссию"
            };
            startButton.Click += (s, e) => StartMission(mission);
            panel.Children.Add(startButton);

            return new Border
            {
                Width = 390,
                Background = new SolidColorBrush(completed ? Color.FromRgb(26, 52, 42) : unlocked ? Color.FromRgb(34, 38, 50) : Color.FromRgb(38, 38, 42)),
                BorderBrush = new SolidColorBrush(unlocked ? Color.FromRgb(110, 80, 70) : Color.FromRgb(70, 70, 76)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(14),
                Margin = new Thickness(8),
                Child = panel
            };
        }

        private void StartMission(MissionDefinition mission)
        {
            if (!HasPlayableDeck())
                return;

            if (game.GetDeckPower() < mission.RecommendedDeckPower)
            {
                MessageBoxResult result = MessageBox.Show(
                    $"Сила вашей колоды ниже рекомендации.\n\nРекомендуется: {mission.RecommendedDeckPower}\nСейчас: {game.GetDeckPower()}\n\nВсё равно начать?",
                    "Сложная миссия", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                    return;
            }

            BattleWindow battleWindow = new BattleWindow(game, mission) { Owner = this };
            battleWindow.ShowDialog();
            UpdateUI();
            DisplayCampaign();
        }

        private void BtnShop_Click(object sender, RoutedEventArgs e)
        {
            ShopWindow shopWindow = new ShopWindow(game) { Owner = this };
            if (shopWindow.ShowDialog() == true)
                UpdateUI();
        }

        private void BtnCollection_Click(object sender, RoutedEventArgs e)
        {
            if (game.Profile.Collection.Count == 0)
            {
                MessageBox.Show("Ваша коллекция пуста! Купите бустер в магазине.",
                    "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            DisplayCards("📔 ВАША КОЛЛЕКЦИЯ", game.Profile.Collection, false);
        }

        private void BtnDeckEditor_Click(object sender, RoutedEventArgs e)
        {
            if (game.CurrentDeck == null)
            {
                InputDialog inputDialog = new InputDialog("Введите имя новой колоды:", "Создание колоды", "Моя колода");
                if (inputDialog.ShowDialog() == true && !string.IsNullOrEmpty(inputDialog.Answer))
                    game.CurrentDeck = new Deck(inputDialog.Answer);
                else
                    return;
            }

            DisplayCards("📚 ВЫБЕРИТЕ КАРТЫ ДЛЯ ДОБАВЛЕНИЯ", game.Profile.Collection, true);
        }

        private void BtnWheel_Click(object sender, RoutedEventArgs e)
        {
            WheelWindow wheelWindow = new WheelWindow(game) { Owner = this };
            if (wheelWindow.ShowDialog() == true)
                UpdateUI();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            game.SaveAll();
            MessageBox.Show("Игра сохранена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnLoadDeck_Click(object sender, RoutedEventArgs e)
        {
            List<string> decks = game.Storage.GetAvailableDecks();
            if (decks.Any())
            {
                InputDialog inputDialog = new InputDialog("Введите имя колоды:", "Загрузка колоды", decks.First());
                if (inputDialog.ShowDialog() == true && !string.IsNullOrEmpty(inputDialog.Answer))
                {
                    game.CurrentDeck = game.Storage.Load(inputDialog.Answer);
                    UpdateUI();
                    MessageBox.Show($"Колода '{inputDialog.Answer}' загружена!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show("Нет сохраненных колод!\nСначала создайте колоду через редактор.",
                    "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnCampaign_Click(object sender, RoutedEventArgs e)
        {
            DisplayCampaign();
        }

        private bool HasPlayableDeck()
        {
            if (game.CurrentDeck != null && game.CurrentDeck.Cards.Count > 0)
                return true;

            MessageBox.Show("Сначала создайте или загрузите колоду!", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        private void DeckListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DeckListBox.SelectedIndex >= 0 && game.CurrentDeck != null)
            {
                btnRemoveFromDeck.Visibility = Visibility.Visible;
                btnRemoveFromDeck.Content = $"🗑️ УДАЛИТЬ КАРТУ #{DeckListBox.SelectedIndex + 1}";
            }
            else
            {
                btnRemoveFromDeck.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnRemoveFromDeck_Click(object sender, RoutedEventArgs e)
        {
            if (DeckListBox.SelectedIndex < 0 || game.CurrentDeck == null)
                return;

            Card cardToRemove = game.CurrentDeck.Cards[DeckListBox.SelectedIndex];
            MessageBoxResult result = MessageBox.Show($"Удалить карту '{cardToRemove.Name}' из колоды?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            game.CurrentDeck.Cards.RemoveAt(DeckListBox.SelectedIndex);
            UpdateUI();
            btnRemoveFromDeck.Visibility = Visibility.Collapsed;
            MessageBox.Show("Карта удалена из колоды!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnClearDeck_Click(object sender, RoutedEventArgs e)
        {
            if (game.CurrentDeck == null || game.CurrentDeck.Cards.Count == 0)
            {
                MessageBox.Show("Нет активной колоды или колода уже пуста!", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            MessageBoxResult result = MessageBox.Show($"Очистить всю колоду '{game.CurrentDeck.Name}'?\nБудет удалено {game.CurrentDeck.Cards.Count} карт.",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            game.CurrentDeck.Cards.Clear();
            UpdateUI();
            MessageBox.Show("Колода очищена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
