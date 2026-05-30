using System;

namespace подобие_echo_room
{
    public enum GameActionType
    {
        Move,
        Interact
    }

    public enum Direction
    {
        None,
        Up,
        Down,
        Left,
        Right
    }

    public class GameAction
    {
        public double Time { get; private set; }
        public GameActionType Type { get; private set; }
        public Direction Direction { get; private set; }

        public GameAction(double time, GameActionType type, Direction direction)
        {
            Time = time;
            Type = type;
            Direction = direction;
        }

        public string GetTimelineText(double echoDelay)
        {
            string actionText = Type == GameActionType.Move
                ? "шаг " + GetDirectionName(Direction)
                : "взаимодействие";

            return string.Format("{0:00.0} c -> {1:00.0} c: {2}", Time, Time + echoDelay, actionText);
        }

        private string GetDirectionName(Direction direction)
        {
            switch (direction)
            {
                case Direction.Up:
                    return "вверх";
                case Direction.Down:
                    return "вниз";
                case Direction.Left:
                    return "влево";
                case Direction.Right:
                    return "вправо";
                default:
                    return "";
            }
        }
    }
}
