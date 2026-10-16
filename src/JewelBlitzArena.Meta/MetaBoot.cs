using System;
using JewelBlitzArena.Data;
using JewelBlitzArena.Sim;

namespace JewelBlitzArena.Meta
{
    /// <summary>
    /// Headless boot flow: load profile → Custom greybox MatchConfig → apply result → save.
    /// Presentation / CLI call this; Meta never steps Sim itself unless asked via helpers.
    /// </summary>
    public sealed class MetaBoot
    {
        private readonly ProfileStore _store;
        private readonly CustomModeOrchestrator _custom = new CustomModeOrchestrator();
        private readonly SparringModeOrchestrator _sparring = new SparringModeOrchestrator();
        private readonly ExpeditionModeOrchestrator _expedition = new ExpeditionModeOrchestrator();
        private readonly MetaMenuShell _menu = new MetaMenuShell();

        public MetaBoot(ProfileStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public PlayerProfile Profile { get; private set; } = PlayerProfile.CreateNew();

        public MetaMenuShell Menu => _menu;

        public CustomModeOrchestrator Custom => _custom;

        public SparringModeOrchestrator Sparring => _sparring;

        public ExpeditionModeOrchestrator Expedition => _expedition;

        public void Boot()
        {
            Profile = _store.LoadOrCreate();
        }

        public MatchConfig BeginCustom(ModeRequest? request = null)
        {
            EnsureBooted();
            ModeRequest req = request ?? new ModeRequest { Mode = ModeId.Custom };
            req.Mode = ModeId.Custom;
            return _custom.BuildConfig(Profile, req);
        }

        public MatchConfig BeginSparring(ModeRequest? request = null)
        {
            EnsureBooted();
            ModeRequest req = request ?? new ModeRequest { Mode = ModeId.Sparring, ActorCount = 2 };
            req.Mode = ModeId.Sparring;
            return _sparring.BuildConfig(Profile, req);
        }

        public MatchConfig BeginExpedition(ModeRequest? request = null)
        {
            EnsureBooted();
            ModeRequest req = request ?? new ModeRequest { Mode = ModeId.Expedition, ActorCount = 2 };
            req.Mode = ModeId.Expedition;
            return _expedition.BuildConfig(Profile, req);
        }

        public void CompleteCustom(in MatchResult result, ModeRequest? request = null)
        {
            EnsureBooted();
            ModeRequest req = request ?? new ModeRequest { Mode = ModeId.Custom };
            _custom.ApplyResult(Profile, result, req);
            _store.Save(Profile);
        }

        public void CompleteSparring(in MatchResult result, ModeRequest? request = null)
        {
            EnsureBooted();
            ModeRequest req = request ?? new ModeRequest { Mode = ModeId.Sparring };
            _sparring.ApplyResult(Profile, result, req);
            _store.Save(Profile);
        }

        public void CompleteExpedition(in MatchResult result, ModeRequest? request = null)
        {
            EnsureBooted();
            ModeRequest req = request ?? new ModeRequest { Mode = ModeId.Expedition };
            _expedition.ApplyResult(Profile, result, req);
            _store.Save(Profile);
        }

        /// <summary>Dispatch result to the active mode orchestrator and save profile.</summary>
        public void CompleteMode(ModeId mode, in MatchResult result, ModeRequest? request = null)
        {
            if (mode == ModeId.Sparring)
            {
                CompleteSparring(result, request);
            }
            else if (mode == ModeId.Expedition)
            {
                CompleteExpedition(result, request);
            }
            else
            {
                CompleteCustom(result, request);
            }
        }

        /// <summary>
        /// Convenience for tests/CLI: boot, build Custom config, run N neutral sim frames, apply, save.
        /// </summary>
        public MetaGreyboxRunResult RunCustomGreybox(
            int frames,
            ModeRequest? request = null,
            ContentCatalog? catalog = null)
        {
            EnsureBooted();
            ModeRequest req = request ?? new ModeRequest { Mode = ModeId.Custom, ActorCount = 2 };
            MatchConfig config = _custom.BuildConfig(Profile, req);

            var sim = new Simulation();
            sim.Reset(config, catalog);
            for (int i = 0; i < frames; i++)
            {
                sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
            }

            MatchResult result = sim.BuildResult();
            ulong hash = sim.Hash();
            _custom.ApplyResult(Profile, result, req);
            _store.Save(Profile);

            return new MetaGreyboxRunResult
            {
                Config = config,
                Result = result,
                Hash = hash,
                ProfileFlags = Profile.Flags,
                OwnsGreyboxToken = Profile.Ledger.Owns(LedgerInventory.CustomGreyboxTokenItemId),
            };
        }

        private void EnsureBooted()
        {
            if (Profile == null)
            {
                Boot();
            }
        }
    }

    public sealed class MetaGreyboxRunResult
    {
        public MatchConfig Config { get; set; }
        public MatchResult Result { get; set; }
        public ulong Hash { get; set; }
        public int ProfileFlags { get; set; }
        public bool OwnsGreyboxToken { get; set; }
    }
}
