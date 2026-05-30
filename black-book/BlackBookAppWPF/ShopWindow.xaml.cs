using System;
using System.Windows;

namespace BlackBookAppWPF
{
    public partial class ShopWindow : Window
    {
        private Game game;

        public ShopWindow(Game game)
        {
            InitializeComponent();
            this.game = game;
            UpdateUI();
        }

        private void UpdateUI()
        {
            lblGold.Text = $"💰 Ваше золото: {game.Profile.Gold}";
        }

        private void BtnBooster_Click(object sender, RoutedEventArgs e)
        {
            BuyPack(100, 3, "малый бустер");
        }

        private void BtnBattleBooster_Click(object sender, RoutedEventArgs e)
        {
            BuyPack(230, 6, "боевой набор");
        }

        private void BtnLegendChance_Click(object sender, RoutedEventArgs e)
        {
            BuyPack(420, 10, "редкий фолиант");
        }

        private void BuyPack(int price, int count, string title)
        {
            if (game.Profile.Gold < price)
            {
                MessageBox.Show("Недостаточно золота! Побеждайте боссов или крутите колесо удачи.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            game.Profile.Gold -= price;
            game.OpenPack(count);
            UpdateUI();

            MessageBox.Show($"Вы открыли {title} и получили {count} карт!", "Успех",
                MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
