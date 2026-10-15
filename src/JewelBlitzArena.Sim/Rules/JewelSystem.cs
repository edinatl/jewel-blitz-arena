namespace JewelBlitzArena.Sim
{
    /// <summary>
    /// S1 jewel <b>rules</b> facade over S2 <see cref="JewelRuntime"/> entities.
    /// Blitz enter/revert and knockdown drops live here; spawn/pickup physics stay in S2.
    /// </summary>
    public static class JewelSystem
    {
        public static void BeginBlitz(ref MatchState state, int actorIndex)
        {
            if (StageRuntime.IsBlitzSuppressed(in state))
            {
                return;
            }

            ref ActorSlot actor = ref MatchStateAccess.Actor(ref state, actorIndex);
            JewelRuntime.ConsumeOwnedJewels(ref state, actorIndex);

            actor.JewelsHeld = 0;
            actor.IsBlitzed = true;
            actor.Luster = MatchRules.LusterMax;
            actor.TransformFrames = MatchRules.BlitzTransformFrames;
            actor.InvulnFrames = MatchRules.BlitzTransformFrames;
            actor.Action = (byte)ActorAction.Transform;
            actor.ActionPhase = (byte)ActionPhase.Startup;
            actor.ActionTimer = MatchRules.BlitzTransformFrames;
            actor.HitApplied = 0;
        }

        public static void OnKnockdownJewelDrop(ref MatchState state, int actorIndex)
        {
            ref ActorSlot actor = ref MatchStateAccess.Actor(ref state, actorIndex);
            if (actor.IsBlitzed || actor.JewelsHeld == 0)
            {
                return;
            }

            JewelRuntime.TryDropOneHeldJewel(ref state, actorIndex);
        }

        public static void OnRevert(ref MatchState state, int actorIndex)
        {
            ref ActorSlot actor = ref MatchStateAccess.Actor(ref state, actorIndex);
            actor.IsBlitzed = false;
            actor.Luster = 0;
            actor.JewelsHeld = 0;
            actor.DrossFrames = MatchRules.DrossFrames;
            actor.RevertFrames = MatchRules.RevertFrames;
            actor.Action = (byte)ActorAction.Revert;
            actor.ActionTimer = MatchRules.RevertFrames;
            actor.ActionPhase = (byte)ActionPhase.Recovery;

            int scheduled = 0;
            for (int i = 0; i < PoolCaps.Jewels && scheduled < MatchRules.BlitzJewelCount; i++)
            {
                if (MatchStateAccess.GetJewelRespawnFrame(ref state, i) == 0
                    && !MatchStateAccess.GetJewel(ref state, i).IsActive)
                {
                    MatchStateAccess.SetJewelRespawnFrame(
                        ref state, i, state.Frame + MatchRules.JewelRespawnDelayFrames);
                    scheduled++;
                }
            }
        }

        public static void TickScheduledRespawns(ref MatchState state)
        {
            for (int i = 0; i < PoolCaps.Jewels; i++)
            {
                int when = MatchStateAccess.GetJewelRespawnFrame(ref state, i);
                if (when > 0 && state.Frame >= when)
                {
                    MatchStateAccess.SetJewelRespawnFrame(ref state, i, 0);
                    JewelRuntime.TrySpawnOnField(ref state, JewelRuntime.PickSpawnPointPublic(ref state));
                }
            }
        }
    }
}
