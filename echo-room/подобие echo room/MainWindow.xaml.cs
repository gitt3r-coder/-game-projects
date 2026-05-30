using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Windows.Controls;

namespace подобие_echo_room
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int CellSize = 48;

        private readonly GameEngine game;
        private readonly DispatcherTimer timer;
        private DateTime lastTick;

        public MainWindow()
        {
            InitializeComponent();

            game = new GameEngine();
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(80);
            timer.Tick += Timer_Tick;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Focus();
            lastTick = DateTime.Now;
            timer.Start();
            DrawGame();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            DateTime now = DateTime.Now;
            double seconds = (now - lastTick).TotalSeconds;
            lastTick = now;

            game.Update(seconds);
            DrawGame();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.W || e.Key == Key.Up)
                game.MovePlayer(Direction.Up);
            else if (e.Key == Key.S || e.Key == Key.Down)
                game.MovePlayer(Direction.Down);
            else if (e.Key == Key.A || e.Key == Key.Left)
                game.MovePlayer(Direction.Left);
            else if (e.Key == Key.D || e.Key == Key.Right)
                game.MovePlayer(Direction.Right);
            else if (e.Key == Key.E)
                game.InteractPlayer();
            else if (e.Key == Key.R)
                RestartGame();

            DrawGame();
        }

        private void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            RestartGame();
        }

        private void RestartGame()
        {
            game.Restart();
            lastTick = DateTime.Now;
            DrawGame();
            Focus();
        }

        private void DrawGame()
        {
            GameCanvas.Children.Clear();

            for (int row = 0; row < GameEngine.Rows; row++)
            {
                for (int column = 0; column < GameEngine.Columns; column++)
                {
                    DrawCell(row, column);
                }
            }

            DrawObjects();
            DrawActors();
            UpdatePanel();
        }

        private void DrawCell(int row, int column)
        {
            Rectangle cell = new Rectangle();
            cell.Width = CellSize - 2;
            cell.Height = CellSize - 2;
            cell.Fill = game.IsWall(row, column)
                ? new SolidColorBrush(Color.FromRgb(31, 36, 48))
                : new SolidColorBrush(Color.FromRgb(18, 24, 34));
            cell.Stroke = new SolidColorBrush(Color.FromRgb(37, 46, 63));
            cell.StrokeThickness = 1;

            Canvas.SetLeft(cell, column * CellSize);
            Canvas.SetTop(cell, row * CellSize);
            GameCanvas.Children.Add(cell);
        }

        private void DrawObjects()
        {
            DrawExit(game.Exit);
            DrawConsole(game.PastConsole, Color.FromRgb(112, 88, 255));
            DrawConsole(game.PresentConsole, Color.FromRgb(35, 178, 154));
            DrawDoor();
        }

        private void DrawExit(ExitObject exitObject)
        {
            Rectangle exit = new Rectangle();
            exit.Width = CellSize - 12;
            exit.Height = CellSize - 12;
            exit.RadiusX = 3;
            exit.RadiusY = 3;
            exit.Fill = new SolidColorBrush(Color.FromRgb(229, 197, 72));
            Canvas.SetLeft(exit, exitObject.Column * CellSize + 6);
            Canvas.SetTop(exit, exitObject.Row * CellSize + 6);
            GameCanvas.Children.Add(exit);

            DrawText("EXIT", exitObject.Row, exitObject.Column, Brushes.Black, 12);
        }

        private void DrawConsole(ConsoleObject console, Color color)
        {
            Rectangle body = new Rectangle();
            body.Width = CellSize - 14;
            body.Height = CellSize - 14;
            body.RadiusX = 5;
            body.RadiusY = 5;
            body.Fill = console.IsActive(game)
                ? new SolidColorBrush(Color.FromRgb(238, 245, 255))
                : new SolidColorBrush(color);
            body.Stroke = Brushes.White;
            body.StrokeThickness = console.IsActive(game) ? 3 : 1;

            Canvas.SetLeft(body, console.Column * CellSize + 7);
            Canvas.SetTop(body, console.Row * CellSize + 7);
            GameCanvas.Children.Add(body);

            string text = console.Owner == ConsoleOwner.EchoOnly ? "PAST" : "NOW";
            Brush textBrush = console.IsActive(game) ? Brushes.Black : Brushes.White;
            DrawText(text, console.Row, console.Column, textBrush, 11);
        }

        private void DrawDoor()
        {
            bool isOpen = game.Door.IsOpen(game);
            Rectangle door = new Rectangle();
            door.Width = CellSize - 2;
            door.Height = CellSize - 2;
            door.Fill = isOpen
                ? new SolidColorBrush(Color.FromRgb(50, 160, 104))
                : new SolidColorBrush(Color.FromRgb(166, 61, 73));
            door.Stroke = Brushes.White;
            door.StrokeThickness = isOpen ? 1 : 2;

            Canvas.SetLeft(door, game.Door.Column * CellSize);
            Canvas.SetTop(door, game.Door.Row * CellSize);
            GameCanvas.Children.Add(door);

            DrawText(isOpen ? "OPEN" : "LOCK", game.Door.Row, game.Door.Column, Brushes.White, 11);
        }

        private void DrawActors()
        {
            foreach (EchoActor echo in game.Echoes)
            {
                DrawActor(echo, Color.FromRgb(151, 129, 255), 0.45);
            }

            DrawActor(game.Player, Color.FromRgb(96, 214, 255), 1.0);
        }

        private void DrawActor(Actor actor, Color color, double opacity)
        {
            Ellipse ellipse = new Ellipse();
            ellipse.Width = CellSize - 14;
            ellipse.Height = CellSize - 14;
            ellipse.Fill = new SolidColorBrush(color);
            ellipse.Stroke = Brushes.White;
            ellipse.StrokeThickness = 2;
            ellipse.Opacity = opacity;

            Canvas.SetLeft(ellipse, actor.Column * CellSize + 7);
            Canvas.SetTop(ellipse, actor.Row * CellSize + 7);
            GameCanvas.Children.Add(ellipse);
        }

        private void DrawText(string text, int row, int column, Brush brush, int fontSize)
        {
            TextBlock label = new TextBlock();
            label.Text = text;
            label.Foreground = brush;
            label.FontSize = fontSize;
            label.FontWeight = FontWeights.Bold;
            label.Width = CellSize;
            label.TextAlignment = TextAlignment.Center;

            Canvas.SetLeft(label, column * CellSize);
            Canvas.SetTop(label, row * CellSize + 16);
            GameCanvas.Children.Add(label);
        }

        private void UpdatePanel()
        {
            TimeText.Text = string.Format("Время: {0:00.0} c    Эхо: {1}", game.CurrentTime, game.Echoes.Count);
            StatusText.Text = game.LastMessage;

            TimelineList.Items.Clear();
            int start = Math.Max(0, game.Actions.Count - 18);

            for (int i = start; i < game.Actions.Count; i++)
            {
                TimelineList.Items.Add(game.Actions[i].GetTimelineText(GameEngine.EchoDelay));
            }
        }
    }
}
