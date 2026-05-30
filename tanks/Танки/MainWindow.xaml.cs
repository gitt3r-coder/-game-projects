using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Tanki
{
    public partial class MainWindow : Window
    {
        private const double FixedTimeStep = 1.0 / 60.0;

        private GameEngine _game;
        private Renderer _renderer;
        private Stopwatch _stopwatch;
        private TimeSpan _previousFrameTime;
        private double _accumulator;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _game = new GameEngine();
            _renderer = new Renderer(GameEngine.ScreenWidth, GameEngine.ScreenHeight);
            GameImage.Source = _renderer.Bitmap;

            Focusable = true;
            Focus();

            _stopwatch = Stopwatch.StartNew();
            _previousFrameTime = _stopwatch.Elapsed;
            _accumulator = 0;

            CompositionTarget.Rendering += OnRendering;
        }

        private void OnRendering(object sender, EventArgs e)
        {
            if (_game == null || _renderer == null)
                return;

            TimeSpan now = _stopwatch.Elapsed;
            double deltaSeconds = (now - _previousFrameTime).TotalSeconds;
            _previousFrameTime = now;

            if (deltaSeconds > 0.1)
                deltaSeconds = 0.1;

            _accumulator += deltaSeconds;
            while (_accumulator >= FixedTimeStep)
            {
                _game.Update();
                _accumulator -= FixedTimeStep;
            }

            _renderer.Render(_game);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (HandleMetaKeys(e.Key))
                return;

            HandleGameplayKey(e.Key, true);
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            HandleGameplayKey(e.Key, false);
        }

        private bool HandleMetaKeys(Key key)
        {
            if (_game == null)
                return false;

            if (key == Key.Enter)
            {
                if (_game.State == GameState.Briefing)
                {
                    _game.BeginMission();
                    return true;
                }

                if (_game.State == GameState.Victory)
                {
                    _game.AdvanceMission();
                    _accumulator = 0;
                    return true;
                }

                if (_game.State == GameState.Defeat)
                {
                    _game.StartGame();
                    _accumulator = 0;
                    return true;
                }
            }

            if (key == Key.P && _game.State != GameState.Victory && _game.State != GameState.Defeat)
            {
                _game.TogglePause();
                return true;
            }

            if (key == Key.R)
            {
                _game.StartGame();
                _accumulator = 0;
                return true;
            }

            if (key == Key.Escape && _game.State == GameState.Running)
            {
                _game.TogglePause();
                return true;
            }

            return false;
        }

        private void HandleGameplayKey(Key key, bool state)
        {
            if (_game == null)
                return;

            if (key == Key.W) _game.P1Up = state;
            if (key == Key.S) _game.P1Down = state;
            if (key == Key.A) _game.P1Left = state;
            if (key == Key.D) _game.P1Right = state;
            if (key == Key.F) _game.P1Fire = state;

            if (key == Key.U && state)
                _game.Player1.Upgrade();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            CompositionTarget.Rendering -= OnRendering;
        }
    }
}
