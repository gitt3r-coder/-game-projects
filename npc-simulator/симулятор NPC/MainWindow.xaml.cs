using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace симулятор_NPC
{
    public partial class MainWindow : Window
    {
        private readonly ObservableCollection<Npc> _npcs = new ObservableCollection<Npc>();
        private readonly ObservableCollection<string> _events = new ObservableCollection<string>();
        private readonly ObservableCollection<string> _dialogue = new ObservableCollection<string>();
        private readonly DispatcherTimer _timer = new DispatcherTimer();
        private readonly Random _random = new Random();
        private readonly Dictionary<string, Point> _locationPoints = new Dictionary<string, Point>();
        private int _hour = 8;
        private int _day = 1;
        private bool _isPaused;
        private string _weather = "ясно";

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            SizeChanged += MainWindow_SizeChanged;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            CreateTown();
            BindCollections();
            SelectFirstNpc();
            UpdateClock();
            UpdateMarkers();

            _timer.Interval = TimeSpan.FromSeconds(4);
            _timer.Tick += Timer_Tick;
            _timer.Start();

            AddEvent("Город проснулся. NPC начинают день со своими планами.");
        }

        private void CreateTown()
        {
            _npcs.Add(new Npc("Мира", "бариста", "Кафе", "дружелюбное", 58, "любит свежие новости и быстро запоминает добрые поступки"));
            _npcs.Add(new Npc("Антон", "механик", "Мастерская", "сосредоточенное", 45, "ценит помощь в работе и недоверчив к хаосу"));
            _npcs.Add(new Npc("Лера", "библиотекарь", "Библиотека", "спокойное", 52, "замечает связи между событиями и хранит городские слухи"));
            _npcs.Add(new Npc("Борис", "торговец", "Рынок", "осторожное", 40, "следит за репутацией игрока и выгодой для рынка"));

            foreach (Npc npc in _npcs)
            {
                npc.Remember("День " + _day + ": начал день в локации " + npc.Location + ".");
            }
        }

        private void BindCollections()
        {
            NpcListBox.ItemsSource = _npcs;
            EventLogListBox.ItemsSource = _events;
            DialogueListBox.ItemsSource = _dialogue;
        }

        private void SelectFirstNpc()
        {
            if (_npcs.Count > 0)
            {
                NpcListBox.SelectedIndex = 0;
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            AdvanceHour();
        }

        private void AdvanceHour()
        {
            _hour++;
            if (_hour >= 24)
            {
                _hour = 6;
                _day++;
                AddEvent("Начался день " + _day + ". Часть старых воспоминаний стала городскими слухами.");
            }

            if (_hour == 9 || _hour == 14 || _hour == 19)
            {
                ChangeWeather();
            }

            foreach (Npc npc in _npcs)
            {
                SimulateNpc(npc);
            }

            MaybeNpcConversation();
            UpdateClock();
            RefreshNpcViews();
            UpdateMarkers();
        }

        private void ChangeWeather()
        {
            string[] options = { "ясно", "ветрено", "дождь", "туман", "тепло" };
            _weather = options[_random.Next(options.Length)];
            AddEvent("Погода изменилась: " + _weather + ".");
        }

        private void SimulateNpc(Npc npc)
        {
            string oldLocation = npc.Location;
            npc.Location = ChooseNextLocation(npc);
            npc.Mood = ChooseMood(npc);

            if (oldLocation != npc.Location)
            {
                npc.Remember("День " + _day + ", " + FormatHour() + ": перешел из " + oldLocation + " в " + npc.Location + ".");
                AddEvent(npc.Name + " идет в " + npc.Location + ".");
            }

            if (_random.NextDouble() < 0.35)
            {
                string observation = MakeObservation(npc);
                npc.Remember("День " + _day + ", " + FormatHour() + ": " + observation);
                AddEvent(npc.Name + ": " + observation);
            }
        }

        private string ChooseNextLocation(Npc npc)
        {
            if (_hour < 9)
            {
                return npc.Name == "Мира" ? "Кафе" : "Площадь";
            }

            if (_hour >= 19)
            {
                string[] evening = { "Кафе", "Парк", "Улица игрока" };
                return evening[_random.Next(evening.Length)];
            }

            if (npc.Name == "Мира")
            {
                return PickWeighted("Кафе", "Площадь", "Рынок");
            }

            if (npc.Name == "Антон")
            {
                return PickWeighted("Мастерская", "Рынок", "Площадь");
            }

            if (npc.Name == "Лера")
            {
                return PickWeighted("Библиотека", "Парк", "Площадь");
            }

            return PickWeighted("Рынок", "Площадь", "Кафе");
        }

        private string PickWeighted(string main, string second, string third)
        {
            int roll = _random.Next(100);
            if (roll < 55)
            {
                return main;
            }

            if (roll < 80)
            {
                return second;
            }

            return third;
        }

        private string ChooseMood(Npc npc)
        {
            if (npc.Trust >= 75)
            {
                return "доверчивое";
            }

            if (npc.Trust <= 25)
            {
                return "настороженное";
            }

            if (_weather == "дождь")
            {
                return npc.Name == "Лера" ? "уютное" : "уставшее";
            }

            string[] moods = { "спокойное", "любопытное", "занятое", "разговорчивое" };
            return moods[_random.Next(moods.Length)];
        }

        private string MakeObservation(Npc npc)
        {
            if (npc.Location == "Рынок")
            {
                return "заметил оживленную торговлю на рынке.";
            }

            if (npc.Location == "Парк")
            {
                return "видел, как жители обсуждали последние новости в парке.";
            }

            if (npc.Location == "Улица игрока")
            {
                return "подумал, что игрок может повлиять на настроение города.";
            }

            if (npc.Location == "Библиотека")
            {
                return "нашел запись о старом городском споре.";
            }

            if (npc.Location == "Мастерская")
            {
                return "услышал шум ремонта в мастерской.";
            }

            return "запомнил тихий момент в локации " + npc.Location + ".";
        }

        private void MaybeNpcConversation()
        {
            List<IGrouping<string, Npc>> groups = _npcs.GroupBy(n => n.Location).Where(g => g.Count() > 1).ToList();
            if (groups.Count == 0 || _random.NextDouble() > 0.6)
            {
                return;
            }

            IGrouping<string, Npc> group = groups[_random.Next(groups.Count)];
            Npc first = group.ElementAt(0);
            Npc second = group.ElementAt(1);
            string topic = ChooseSharedTopic(first, second);

            first.Remember("День " + _day + ", " + FormatHour() + ": обсудил с " + second.Name + " тему: " + topic + ".");
            second.Remember("День " + _day + ", " + FormatHour() + ": обсудил с " + first.Name + " тему: " + topic + ".");
            AddEvent(first.Name + " и " + second.Name + " разговаривают в локации " + group.Key + ": " + topic + ".");
        }

        private string ChooseSharedTopic(Npc first, Npc second)
        {
            if (first.Memories.Any(m => m.Contains("игрок")) || second.Memories.Any(m => m.Contains("игрок")))
            {
                return "что игрок сделал сегодня";
            }

            string[] topics = { "погода", "рынок", "новые слухи", "настроение жителей", "планы на вечер" };
            return topics[_random.Next(topics.Length)];
        }

        private void NpcListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshNpcViews();
        }

        private void GreetButton_Click(object sender, RoutedEventArgs e)
        {
            InteractWithSelected("поздоровался", 4);
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            InteractWithSelected("помог с личной задачей", 12);
        }

        private void NewsButton_Click(object sender, RoutedEventArgs e)
        {
            InteractWithSelected("рассказал важную новость", 7);
            SpreadPlayerNews();
        }

        private void TroubleButton_Click(object sender, RoutedEventArgs e)
        {
            InteractWithSelected("создал проблему в городе", -14);
        }

        private void AskButton_Click(object sender, RoutedEventArgs e)
        {
            Npc npc = SelectedNpc;
            if (npc == null)
            {
                return;
            }

            string answer = npc.MakeAnswer(_weather);
            _dialogue.Insert(0, "Игрок: Что происходит в городе?");
            _dialogue.Insert(0, npc.Name + ": " + answer);
            npc.Remember("День " + _day + ", " + FormatHour() + ": игрок спросил о городе.");
            TrimCollection(_dialogue, 30);
            RefreshNpcViews();
        }

        private void InteractWithSelected(string action, int trustDelta)
        {
            Npc npc = SelectedNpc;
            if (npc == null)
            {
                return;
            }

            npc.Trust = Clamp(npc.Trust + trustDelta, 0, 100);
            npc.Mood = trustDelta >= 0 ? "расположенное" : "раздраженное";
            string memory = "День " + _day + ", " + FormatHour() + ": игрок " + action + ".";
            npc.Remember(memory);

            string line = npc.ReactToPlayerAction(action, trustDelta);
            _dialogue.Insert(0, "Игрок " + action + ".");
            _dialogue.Insert(0, npc.Name + ": " + line);
            AddEvent(npc.Name + " запомнил: игрок " + action + ".");

            TrimCollection(_dialogue, 30);
            RefreshNpcViews();
        }

        private void SpreadPlayerNews()
        {
            foreach (Npc npc in _npcs.Where(n => n != SelectedNpc))
            {
                if (_random.NextDouble() < 0.55)
                {
                    npc.Remember("День " + _day + ", " + FormatHour() + ": услышал слух, что игрок рассказал важную новость.");
                    npc.Trust = Clamp(npc.Trust + 2, 0, 100);
                }
            }
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            _isPaused = !_isPaused;
            if (_isPaused)
            {
                _timer.Stop();
                PauseButton.Content = "Продолжить";
                AddEvent("Симуляция поставлена на паузу.");
            }
            else
            {
                _timer.Start();
                PauseButton.Content = "Пауза";
                AddEvent("Симуляция продолжается.");
            }
        }

        private void NextHourButton_Click(object sender, RoutedEventArgs e)
        {
            AdvanceHour();
        }

        private void RefreshNpcViews()
        {
            Npc npc = SelectedNpc;
            NpcListBox.Items.Refresh();

            if (npc == null)
            {
                return;
            }

            MemoryListBox.ItemsSource = npc.Memories;
            SelectedNpcNameText.Text = npc.Name + " - " + npc.Role;
            SelectedNpcStateText.Text = "Локация: " + npc.Location + " | Настроение: " + npc.Mood + " | Отношение: " + npc.Trust + "/100";
            TrustBar.Value = npc.Trust;
        }

        private void AddEvent(string text)
        {
            _events.Insert(0, "[" + FormatHour() + "] " + text);
            TrimCollection(_events, 60);
        }

        private void TrimCollection(ObservableCollection<string> collection, int max)
        {
            while (collection.Count > max)
            {
                collection.RemoveAt(collection.Count - 1);
            }
        }

        private void UpdateClock()
        {
            ClockText.Text = "День " + _day + ", " + FormatHour();
            WeatherText.Text = "Погода: " + _weather;
        }

        private string FormatHour()
        {
            return _hour.ToString("00") + ":00";
        }

        private Npc SelectedNpc
        {
            get { return NpcListBox.SelectedItem as Npc; }
        }

        private int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateMarkers();
        }

        private void UpdateMarkers()
        {
            _locationPoints["Парк"] = new Point(0.16, 0.16);
            _locationPoints["Площадь"] = new Point(0.50, 0.16);
            _locationPoints["Кафе"] = new Point(0.84, 0.16);
            _locationPoints["Библиотека"] = new Point(0.16, 0.50);
            _locationPoints["Рынок"] = new Point(0.50, 0.50);
            _locationPoints["Мастерская"] = new Point(0.84, 0.50);
            _locationPoints["Улица игрока"] = new Point(0.50, 0.84);

            PositionMarker(MarkerMira, _npcs.FirstOrDefault(n => n.Name == "Мира"), -16, -16);
            PositionMarker(MarkerAnton, _npcs.FirstOrDefault(n => n.Name == "Антон"), 8, -14);
            PositionMarker(MarkerLera, _npcs.FirstOrDefault(n => n.Name == "Лера"), -14, 10);
            PositionMarker(MarkerBoris, _npcs.FirstOrDefault(n => n.Name == "Борис"), 10, 10);
        }

        private void PositionMarker(Ellipse marker, Npc npc, double offsetX, double offsetY)
        {
            if (marker == null || npc == null || !_locationPoints.ContainsKey(npc.Location))
            {
                return;
            }

            FrameworkElement parent = marker.Parent as FrameworkElement;
            if (parent == null || parent.ActualWidth <= 0 || parent.ActualHeight <= 0)
            {
                return;
            }

            Point relative = _locationPoints[npc.Location];
            Canvas.SetLeft(marker, parent.ActualWidth * relative.X + offsetX);
            Canvas.SetTop(marker, parent.ActualHeight * relative.Y + offsetY);
            marker.ToolTip = npc.Name + " - " + npc.Location;
        }
    }

    public class Npc
    {
        private readonly ObservableCollection<string> _memories = new ObservableCollection<string>();

        public Npc(string name, string role, string location, string mood, int trust, string personality)
        {
            Name = name;
            Role = role;
            Location = location;
            Mood = mood;
            Trust = trust;
            Personality = personality;
        }

        public string Name { get; private set; }
        public string Role { get; private set; }
        public string Location { get; set; }
        public string Mood { get; set; }
        public int Trust { get; set; }
        public string Personality { get; private set; }

        public ObservableCollection<string> Memories
        {
            get { return _memories; }
        }

        public string DisplayName
        {
            get
            {
                return Name + " | " + Role + " | " + Location + " | " + Mood + " | " + Trust + "/100";
            }
        }

        public void Remember(string memory)
        {
            _memories.Insert(0, memory);
            while (_memories.Count > 12)
            {
                _memories.RemoveAt(_memories.Count - 1);
            }
        }

        public string ReactToPlayerAction(string action, int trustDelta)
        {
            if (trustDelta > 8)
            {
                return "Я это запомню. Теперь к тебе доверия заметно больше.";
            }

            if (trustDelta > 0)
            {
                return "Хорошо, что ты так сделал. Город реагирует на мелочи.";
            }

            return "Такое тоже остается в памяти. Мне нужно время, чтобы снова доверять.";
        }

        public string MakeAnswer(string weather)
        {
            string lastMemory = _memories.Count > 0 ? _memories[0] : "пока ничего важного не произошло";

            if (Trust >= 70)
            {
                return "Расскажу честно: " + lastMemory + " Еще я думаю, что погода сейчас влияет на маршруты жителей: " + weather + ".";
            }

            if (Trust <= 30)
            {
                return "Я не уверен, что хочу делиться всем. Но последнее, что помню: " + lastMemory;
            }

            return "В городе все связано. " + lastMemory + " Мой характер: " + Personality + ".";
        }
    }
}
