using Content.Shared.CCVar;
using Content.Shared.Movement;
using Content.Shared.Movement.Components;
using JetBrains.Annotations;
using Robust.Shared.Configuration;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;


namespace Content.Shared.Friction
{
    public abstract class SharedTileFrictionController : VirtualController
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;

        protected SharedMoverController Mover = default!;

        private float _stopSpeed;
        private float _frictionModifier;

        public override void Initialize()
        {
            base.Initialize();

            var configManager = IoCManager.Resolve<IConfigurationManager>();

            configManager.OnValueChanged(CCVars.TileFrictionModifier, SetFrictionModifier, true);
            configManager.OnValueChanged(CCVars.StopSpeed, SetStopSpeed, true);

            SubscribeLocalEvent<TileFrictionModifierComponent, ComponentGetState>(OnGetState);
            SubscribeLocalEvent<TileFrictionModifierComponent, ComponentHandleState>(OnHandleState);
        }

        private void OnHandleState(EntityUid uid, TileFrictionModifierComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not TileFrictionComponentState tileState) return;
            component.Modifier = tileState.Modifier;
        }

        private void OnGetState(EntityUid uid, TileFrictionModifierComponent component, ref ComponentGetState args)
        {
            args.State = new TileFrictionComponentState(component.Modifier);
        }

        private void SetStopSpeed(float value) => _stopSpeed = value;

        private void SetFrictionModifier(float value) => _frictionModifier = value;

        public override void Shutdown()
        {
            base.Shutdown();
            var configManager = IoCManager.Resolve<IConfigurationManager>();

            configManager.UnsubValueChanged(CCVars.TileFrictionModifier, SetFrictionModifier);
            configManager.UnsubValueChanged(CCVars.StopSpeed, SetStopSpeed);
        }

        public override void UpdateBeforeMapSolve(bool prediction, SharedPhysicsMapComponent mapComponent, float frameTime)
        {
            base.UpdateBeforeMapSolve(prediction, mapComponent, frameTime);

            var frictionQuery = GetEntityQuery<TileFrictionModifierComponent>();
            var xformQuery = GetEntityQuery<TransformComponent>();

            foreach (var body in mapComponent.AwakeBodies)
            {
                // Only apply friction when it's not a mob (or the mob doesn't have control)
                if (prediction && !body.Predict ||
                    body.BodyStatus == BodyStatus.InAir ||
                    Mover.UseMobMovement(body.Owner)) continue;

                if (body.LinearVelocity.Equals(Vector2.Zero) && body.AngularVelocity.Equals(0f)) continue;

                DebugTools.Assert(!Deleted(body.Owner));
                var surfaceFriction = GetTileFriction(body, xformQuery.GetComponent(body.Owner));
                var bodyModifier = 1f;

                if (frictionQuery.TryGetComponent(body.Owner, out var frictionComp))
                {
                    bodyModifier = frictionComp.Modifier;
                }

                var friction = _frictionModifier * surfaceFriction * bodyModifier;

                ReduceLinearVelocity(prediction, body, friction, frameTime);
                ReduceAngularVelocity(prediction, body, friction, frameTime);
            }
        }

        private void ReduceLinearVelocity(bool prediction, PhysicsComponent body, float friction, float frameTime)
        {
            var speed = body.LinearVelocity.Length;

            if (speed <= 0.0f) return;

            // This is the *actual* amount that speed will drop by, we just do some multiplication around it to be easier.
            var drop = 0.0f;
            float control;

            if (friction > 0.0f)
            {
                // TBH I can't really tell if this makes a difference.
                if (!prediction)
                {
                    control = speed < _stopSpeed ? _stopSpeed : speed;
                }
                else
                {
                    control = speed;
                }

                drop += control * friction * frameTime;
            }

            var newSpeed = MathF.Max(0.0f, speed - drop);

            newSpeed /= speed;
            body.LinearVelocity *= newSpeed;
        }

        private void ReduceAngularVelocity(bool prediction, PhysicsComponent body, float friction, float frameTime)
        {
            var speed = MathF.Abs(body.AngularVelocity);

            if (speed <= 0.0f) return;

            // This is the *actual* amount that speed will drop by, we just do some multiplication around it to be easier.
            var drop = 0.0f;
            float control;

            if (friction > 0.0f)
            {
                // TBH I can't really tell if this makes a difference.
                if (!prediction)
                {
                    control = speed < _stopSpeed ? _stopSpeed : speed;
                }
                else
                {
                    control = speed;
                }

                drop += control * friction * frameTime;
            }

            var newSpeed = MathF.Max(0.0f, speed - drop);

            newSpeed /= speed;
            body.AngularVelocity *= newSpeed;
        }

        [Pure]
        private float GetTileFriction(PhysicsComponent body, TransformComponent xform)
        {
            var coords = xform.Coordinates;

            // TODO: Make IsWeightless event-based; we already have grid traversals tracked so just raise events
            if (body.Owner.IsWeightless(body, coords, _mapManager) ||
                !_mapManager.TryGetGrid(xform.GridEntityId, out var grid))
                return 0.0f;

            if (!coords.IsValid(EntityManager)) return 0.0f;

            var tile = grid.GetTileRef(coords);
            var tileDef = _tileDefinitionManager[tile.Tile.TypeId];
            return tileDef.Friction;
        }

        [NetSerializable, Serializable]
        private sealed class TileFrictionComponentState : ComponentState
        {
            public float Modifier;

            public TileFrictionComponentState(float modifier)
            {
                Modifier = modifier;
            }
        }
    }
}
