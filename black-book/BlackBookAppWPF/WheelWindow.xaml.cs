using System;
using System.Windows;
using System.Windows.Threading;

namespace BlackBookAppWPF
{
    public partial class WheelWindow : Window
    {
        private Game game;
        private Random rng = new Random();
        private DispatcherTimer animationTimer;
        private int animationStep = 0;
        private int currentResult;

        public WheelWindow(Game game)
        {
            InitializeComponent();
            this.game = game;
            CheckSpinAvailability();
        }

        private void CheckSpinAvailability()
        {
            var timePassed = DateTime.Now - game.Profile.LastSpin;
            if (timePassed.TotalHours < 12)
            {
                btnSpin.IsEnabled = false;
                lblResult.Text = $"❌ НЕДОСТУПНО ❌\nЖдите 12 часов!";
                lblResult.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        private void BtnSpin_Click(object sender, RoutedEventArgs e)
        {
            var timePassed = DateTime.Now - game.Profile.LastSpin;
            if (timePassed.TotalHours < 12)
            {
                MessageBox.Show("Духи устали. Подождите 12 часов!", "Ошибка");
                return;
            }

            btnSpin.IsEnabled = false;
            animationStep = 0;

            animationTimer = new DispatcherTimer();
            animationTimer.Interval = TimeSpan.FromMilliseconds(50);
            animationTimer.Tick += AnimationTimer_Tick;
            animationTimer.Start();
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            animationStep++;

            currentResult = rng.Next(1, 7) * 50;
            lblResult.Text = $"{currentResult} 💰";

            if (animationStep >= 20)
            {
                animationTimer.Stop();
                animationTimer = null;

                game.Profile.Gold += currentResult;
                game.Profile.LastSpin = DateTime.Now;
                game.Profile.Save();

                lblResult.Text = $"🎉 ВЫ ВЫИГРАЛИ:\n{currentResult} 💰 🎉";
                lblResult.Foreground = System.Windows.Media.Brushes.Gold;

                MessageBox.Show($"Поздравляем! Вы выиграли {currentResult} золота!",
                    "Победа!", MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
        }
    }
}