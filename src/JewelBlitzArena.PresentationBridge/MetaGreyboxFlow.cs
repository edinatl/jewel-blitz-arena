using System.IO;
using JewelBlitzArena.Data;
using JewelBlitzArena.Meta;
using JewelBlitzArena.Sim;

namespace JewelBlitzArena.PresentationBridge
{
    /// <summary>
    /// Headless Boot → Custom/Sparring greybox → Complete path shared by CLI and Unity host.
    /// Presentation owns the flow; Meta builds config / stamps profile; Sim steps the match.
    /// </summary>
    public sealed class MetaGreyboxFlow
    {
        private readonly MetaBoot _boot;
        private readonly MatchSession _session = new MatchSession();
        private ModeRequest? _activeRequest;
        private MatchConfig _activeConfig;
        private ContentCatalog? _catalog;
        private bool _matchLive;

        public MetaGreyboxFlow(string profilePath)
        {
            _boot = new MetaBoot(new ProfileStore(profilePath));
        }

        public MetaBoot BootApi => _boot;

        public MatchSession Session => _session;

        public bool MatchLive => _matchLive;

        public PlayerProfile Profile => _boot.Profile;

        public MatchConfig ActiveConfig => _activeConfig;

        public void BootProfile() => _boot.Boot();

        public void SetCatalog(ContentCatalog? catalog) => _catalog = catalog;

        public MatchConfig StartCustom(ModeRequest? request = null)
        {
            _boot.Boot();
            ModeRequest req = request ?? new ModeRequest { Mode = ModeId.Custom, ActorCount = 2 };
            if (req.Mode != ModeId.Sparring)
            {
                req.Mode = ModeId.Custom;
            }

            _activeRequest = req;
            _activeConfig = req.Mode == ModeId.Sparring
                ? _boot.BeginSparring(req)
                : _boot.BeginCustom(req);
            _session.Reset(_activeConfig, _catalog);
            _matchLive = true;
            return _activeConfig;
        }

        public void Step(InputFrame p0, InputFrame p1, InputFrame p2 = default, InputFrame p3 = default)
        {
            if (!_matchLive)
            {
                return;
            }

            _session.Step(p0, p1, p2, p3);
        }

        public MatchResult CompleteCustom()
        {
            if (!_matchLive)
            {
                return default;
            }

            MatchResult result = _session.Sim.BuildResult();
            ModeId mode = _activeRequest?.Mode ?? ModeId.Custom;
            _boot.CompleteMode(mode, result, _activeRequest);
            _matchLive = false;
            return result;
        }

        /// <summary>Full headless Custom greybox for tests/CLI.</summary>
        public MetaGreyboxRunResult RunScripted(
            int frames,
            ModeRequest? request = null,
            bool walk = true)
        {
            MatchConfig config = StartCustom(request);
            for (int f = 0; f < frames; f++)
            {
                InputFrame p0 = InputFrame.Neutral;
                InputFrame p1 = InputFrame.Neutral;
                if (walk)
                {
                    p0 = InputQuantizer.FromCardinal(1, 0);
                    p1 = InputQuantizer.FromCardinal(-1, 0);
                }

                Step(p0, p1);
            }

            MatchResult result = CompleteCustom();
            return new MetaGreyboxRunResult
            {
                Config = config,
                Result = result,
                Hash = _session.Hash(),
                ProfileFlags = Profile.Flags,
                OwnsGreyboxToken = Profile.Ledger.Owns(LedgerInventory.CustomGreyboxTokenItemId),
            };
        }

        public static string DefaultTempProfilePath(string? prefix = null)
        {
            string dir = Path.Combine(Path.GetTempPath(), prefix ?? ("jba-meta-" + Path.GetRandomFileName()));
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "profile.jbp");
        }
    }
}
