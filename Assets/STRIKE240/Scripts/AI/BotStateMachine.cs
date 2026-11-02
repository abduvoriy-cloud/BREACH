namespace Strike240
{
    public enum BotStateId
    {
        Idle,
        Patrol,
        Investigate,
        Chase,
        Engage,
        Search
    }

    internal interface IBotState
    {
        BotStateId Id { get; }
        void Enter();
        void Tick();
        void Exit();
    }

    /// <summary>Owns state transitions so individual behaviors stay small and focused.</summary>
    public sealed class BotStateMachine
    {
        private readonly BotAgent owner;
        private readonly IBotState idle;
        private readonly IBotState patrol;
        private readonly IBotState investigate;
        private readonly IBotState chase;
        private readonly IBotState engage;
        private readonly IBotState search;
        private IBotState current;

        public BotStateId CurrentState => current == null ? BotStateId.Idle : current.Id;

        public BotStateMachine(BotAgent agent)
        {
            owner = agent;
            idle = new BotIdleState(agent, this);
            patrol = new BotPatrolState(agent, this);
            investigate = new BotInvestigateState(agent, this);
            chase = new BotChaseState(agent, this);
            engage = new BotEngageState(agent, this);
            search = new BotSearchState(agent, this);
        }

        public void Start() => ChangeState(BotStateId.Idle);

        public void Tick()
        {
            if (!owner.IsAlive) return;
            EvaluatePerceptionTransitions();
            current?.Tick();
        }

        public void ChangeState(BotStateId stateId)
        {
            IBotState next = GetState(stateId);
            if (next == null || current == next) return;
            current?.Exit();
            current = next;
            current.Enter();
        }

        private void EvaluatePerceptionTransitions()
        {
            BotVision vision = owner.Vision;
            if (vision == null || vision.Player == null || !vision.Player.IsAlive)
            {
                if (CurrentState != BotStateId.Idle && CurrentState != BotStateId.Patrol)
                    ChangeState(BotStateId.Idle);
                return;
            }

            if (vision.IsPlayerVisible)
            {
                if (vision.HasDetectedPlayer)
                {
                    float distance = Vector3Distance(owner.transform.position, vision.Player.transform.position);
                    float engageLimit = CurrentState == BotStateId.Engage
                        ? owner.EngageDistance + 2f
                        : owner.EngageDistance;
                    ChangeState(distance <= engageLimit ? BotStateId.Engage : BotStateId.Chase);
                }
                else if (CurrentState == BotStateId.Idle || CurrentState == BotStateId.Patrol)
                {
                    ChangeState(BotStateId.Investigate);
                }
                return;
            }

            if (!vision.HasDetectedPlayer || !vision.HasSeenPlayer) return;

            float timeSinceSeen = UnityEngine.Time.time - vision.LastSeenTime;
            if (timeSinceSeen <= owner.InvestigationMemorySeconds)
            {
                if (CurrentState != BotStateId.Investigate && CurrentState != BotStateId.Search)
                    ChangeState(BotStateId.Investigate);
            }
            else if (CurrentState != BotStateId.Search)
            {
                ChangeState(BotStateId.Search);
            }
        }

        private IBotState GetState(BotStateId stateId)
        {
            switch (stateId)
            {
                case BotStateId.Patrol: return patrol;
                case BotStateId.Investigate: return investigate;
                case BotStateId.Chase: return chase;
                case BotStateId.Engage: return engage;
                case BotStateId.Search: return search;
                default: return idle;
            }
        }

        private static float Vector3Distance(UnityEngine.Vector3 a, UnityEngine.Vector3 b)
        {
            return UnityEngine.Vector3.Distance(a, b);
        }
    }
}
