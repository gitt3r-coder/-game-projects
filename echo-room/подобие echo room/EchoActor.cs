using System.Collections.Generic;

namespace подобие_echo_room
{
    public class EchoActor : Actor
    {
        private readonly double delay;
        private int nextActionIndex;

        public double Delay
        {
            get { return delay; }
        }

        public EchoActor(string name, int row, int column, double delay)
            : base(name, row, column, true)
        {
            this.delay = delay;
            nextActionIndex = 0;
        }

        public void Update(GameEngine engine, List<GameAction> actions)
        {
            while (nextActionIndex < actions.Count &&
                   actions[nextActionIndex].Time + delay <= engine.CurrentTime)
            {
                engine.PerformEchoAction(this, actions[nextActionIndex]);
                nextActionIndex++;
            }
        }
    }
}
