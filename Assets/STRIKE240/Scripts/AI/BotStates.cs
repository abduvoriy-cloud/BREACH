using UnityEngine;

namespace Strike240
{
    internal abstract class BotState : IBotState
    {
        protected readonly BotAgent Owner;
        protected readonly BotStateMachine Machine;

        public abstract BotStateId Id { get; }

        protected BotState(BotAgent owner, BotStateMachine machine)
        {
            Owner = owner;
            Machine = machine;
        }

        public virtual void Enter() { }
        public abstract void Tick();
        public virtual void Exit() { }
    }

    internal sealed class BotIdleState : BotState
    {
        private float patrolAt;
        public override BotStateId Id => BotStateId.Idle;

        public BotIdleState(BotAgent owner, BotStateMachine machine) : base(owner, machine) { }

        public override void Enter()
        {
            if (Owner.AgentReady) Owner.Agent.isStopped = true;
            patrolAt = Time.time + Random.Range(0.8f, 1.8f);
        }

        public override void Tick()
        {
            if (Time.time >= patrolAt)
                Machine.ChangeState(BotStateId.Patrol);
        }

        public override void Exit()
        {
            if (Owner.AgentReady) Owner.Agent.isStopped = false;
        }
    }

    internal sealed class BotPatrolState : BotState
    {
        private float chooseAgainAt;
        private bool hasDestination;
        public override BotStateId Id => BotStateId.Patrol;

        public BotPatrolState(BotAgent owner, BotStateMachine machine) : base(owner, machine) { }

        public override void Enter()
        {
            if (Owner.AgentReady) Owner.Agent.isStopped = false;
            hasDestination = ChooseDestination();
            chooseAgainAt = Time.time + 0.5f;
        }

        public override void Tick()
        {
            if (Time.time < chooseAgainAt) return;
            chooseAgainAt = Time.time + 0.5f;

            if (!hasDestination || Owner.HasReachedDestination())
                hasDestination = ChooseDestination();
        }

        private bool ChooseDestination()
        {
            return Owner.TryFindPatrolPoint(out Vector3 point) && Owner.TrySetDestination(point);
        }
    }

    internal sealed class BotInvestigateState : BotState
    {
        private float enteredAt;
        private bool hasDestination;
        public override BotStateId Id => BotStateId.Investigate;

        public BotInvestigateState(BotAgent owner, BotStateMachine machine) : base(owner, machine) { }

        public override void Enter()
        {
            enteredAt = Time.time;
            if (Owner.AgentReady) Owner.Agent.isStopped = false;
            hasDestination = Owner.Vision != null &&
                             Owner.TrySetDestination(Owner.Vision.LastKnownPosition);
        }

        public override void Tick()
        {
            if (!hasDestination || Owner.HasReachedDestination() ||
                Time.time - enteredAt > Owner.InvestigationTimeout)
            {
                Machine.ChangeState(BotStateId.Search);
            }
        }
    }

    internal sealed class BotChaseState : BotState
    {
        private float nextDestinationUpdate;
        public override BotStateId Id => BotStateId.Chase;

        public BotChaseState(BotAgent owner, BotStateMachine machine) : base(owner, machine) { }

        public override void Enter()
        {
            if (Owner.AgentReady) Owner.Agent.isStopped = false;
            nextDestinationUpdate = 0f;
        }

        public override void Tick()
        {
            if (Owner.Vision == null || Owner.Vision.Player == null ||
                Time.time < nextDestinationUpdate) return;

            nextDestinationUpdate = Time.time + Owner.ChaseUpdateInterval;
            if (Owner.HasUnreachablePath ||
                !Owner.TrySetDestination(Owner.Vision.Player.transform.position))
                Machine.ChangeState(BotStateId.Investigate);
        }
    }

    internal sealed class BotEngageState : BotState
    {
        public override BotStateId Id => BotStateId.Engage;

        public BotEngageState(BotAgent owner, BotStateMachine machine) : base(owner, machine) { }

        public override void Enter()
        {
            if (Owner.AgentReady) Owner.Agent.isStopped = true;
            Owner.BeginEngagement();
        }

        public override void Tick()
        {
            if (Owner.Vision != null && Owner.Vision.Player != null)
            {
                Owner.FacePosition(Owner.Vision.Player.AimPoint);
                Owner.TryFireAtPlayer();
            }
        }

        public override void Exit()
        {
            Owner.EndEngagement();
            if (Owner.AgentReady) Owner.Agent.isStopped = false;
        }
    }

    internal sealed class BotSearchState : BotState
    {
        private const int SearchPointCount = 3;
        private int pointsVisited;
        private float nextCheckAt;
        private bool hasDestination;
        public override BotStateId Id => BotStateId.Search;

        public BotSearchState(BotAgent owner, BotStateMachine machine) : base(owner, machine) { }

        public override void Enter()
        {
            pointsVisited = 0;
            nextCheckAt = Time.time;
            Owner.BeginSearch();
            if (Owner.AgentReady) Owner.Agent.isStopped = false;
            ChooseSearchPoint();
        }

        public override void Tick()
        {
            if (Time.time < nextCheckAt) return;
            nextCheckAt = Time.time + 0.4f;

            if (pointsVisited >= SearchPointCount || Time.time >= Owner.SearchDeadline)
            {
                FinishSearch();
                return;
            }

            if (!hasDestination || Owner.HasReachedDestination())
                ChooseSearchPoint();
        }

        private void ChooseSearchPoint()
        {
            pointsVisited++;
            hasDestination = Owner.TryFindSearchPoint(out Vector3 point) && Owner.TrySetDestination(point);
            if (!hasDestination && pointsVisited >= SearchPointCount)
                FinishSearch();
        }

        private void FinishSearch()
        {
            Owner.Vision?.ForgetPlayer();
            Machine.ChangeState(BotStateId.Patrol);
        }
    }
}
