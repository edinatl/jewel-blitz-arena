using System.IO;
using JewelBlitzArena.Data;
using JewelBlitzArena.Meta;
using JewelBlitzArena.PresentationBridge;
using JewelBlitzArena.Sim;
using UnityEngine;

namespace JewelBlitzArena.Presentation
{
    public enum GreyboxSimMode
    {
        /// <summary>Offline MatchSession driven by Meta Custom config.</summary>
        OfflineMeta = 0,

        /// <summary>Local 2P over DualSessionHarness loopback (rollback path).</summary>
        LoopbackNet = 1,
    }

    /// <summary>
    /// Fixed 60 Hz sim step driven from Unity. Presentation-only; no float rules in Sim.
    /// Boot → Meta menu → Custom match, or loopback net greybox.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class MatchDriver : MonoBehaviour
    {
        public const float SimDt = 1f / Simulation.TickRateHz;

        [SerializeField] private GreyboxSimMode simMode = GreyboxSimMode.OfflineMeta;
        [SerializeField] private ulong seed = 0x53335F484F5354UL; // S3_HOST
        [SerializeField] private int actorCount = 2;
        [SerializeField] private bool autoStep = true;
        [SerializeField] private int loopbackDeliveryDelay;
        [SerializeField] private TextAsset? contentBlob;

        private MetaGreyboxFlow? _metaFlow;
        private LoopbackGreyboxRunner? _loopback;
        private MatchSession _offlineSession = new MatchSession();
        private float _accum;
        private ContentCatalog? _catalog;
        private bool _inMenu = true;

        public GreyboxSimMode SimMode => simMode;

        public bool InMenu => _inMenu;

        public MetaGreyboxFlow? MetaFlow => _metaFlow;

        public MatchSession Session
        {
            get
            {
                if (simMode == GreyboxSimMode.OfflineMeta && _metaFlow != null && _metaFlow.MatchLive)
                {
                    return _metaFlow.Session;
                }

                return _offlineSession;
            }
        }

        public LoopbackGreyboxRunner? Loopback => _loopback;

        public ContentCatalog? Catalog => _catalog;

        public float Alpha { get; private set; }

        public ulong LastHash { get; private set; }

        public int RollbackCount => _loopback?.RollbackCount ?? 0;

        public bool DesyncFlagged => _loopback?.DesyncFlagged ?? false;

        public bool IsStalling => _loopback?.IsStalling ?? false;

        public int StallFrames => _loopback?.StallFrames ?? 0;

        public bool ShowDisconnectWarning => _loopback?.ShowDisconnectWarning ?? false;

        public bool DisconnectReplaceReady => _loopback?.DisconnectReplaceReady ?? false;

        public int CpuReplacedMask => _loopback?.CpuReplacedMask ?? 0;

        private void Awake()
        {
            if (contentBlob != null && contentBlob.bytes != null && contentBlob.bytes.Length > 0)
            {
                _catalog = ContentBlobReader.Read(contentBlob.bytes);
            }
            else
            {
                string streaming = Path.Combine(Application.streamingAssetsPath, "s5_stub.jba");
                if (File.Exists(streaming))
                {
                    _catalog = ContentBlobReader.Read(File.ReadAllBytes(streaming));
                }
            }

            string profilePath = Path.Combine(Application.persistentDataPath, "profile.jbp");
            _metaFlow = new MetaGreyboxFlow(profilePath);
            _metaFlow.SetCatalog(_catalog);
            _metaFlow.BootProfile();
            _inMenu = true;
        }

        private void Update()
        {
            if (_inMenu || !autoStep)
            {
                return;
            }

            _accum += Time.unscaledDeltaTime;
            int steps = 0;
            while (_accum >= SimDt && steps < 8)
            {
                _accum -= SimDt;
                StepOnce();
                steps++;
            }

            Alpha = Mathf.Clamp01(_accum / SimDt);
        }

        public void EnterMenu()
        {
            if (_metaFlow != null && _metaFlow.MatchLive)
            {
                _metaFlow.CompleteCustom();
            }

            _inMenu = true;
            _accum = 0f;
            Alpha = 0f;
        }

        public void StartCustomFromMeta(ModeRequest? request = null)
        {
            simMode = GreyboxSimMode.OfflineMeta;
            ModeRequest req = request ?? new ModeRequest
            {
                Mode = ModeId.Custom,
                Seed = seed,
                ActorCount = actorCount,
                StageId = SampleContentFactory.CitrineBazaarStageId,
            };
            req.Seed = seed;
            req.ActorCount = actorCount;
            if (req.StageId == 0)
            {
                req.StageId = SampleContentFactory.CitrineBazaarStageId;
            }

            _metaFlow!.StartCustom(req);
            _offlineSession = _metaFlow.Session;
            LastHash = _metaFlow.Session.Hash();
            _inMenu = false;
            _accum = 0f;
            Alpha = 0f;
        }

        public void StartLoopbackNet(ModeRequest? request = null)
        {
            simMode = GreyboxSimMode.LoopbackNet;
            ModeRequest req = request ?? new ModeRequest
            {
                Mode = ModeId.Custom,
                Seed = seed,
                ActorCount = 2,
            };
            MatchConfig config = _metaFlow!.BootApi.BeginCustom(req);
            _loopback = new LoopbackGreyboxRunner(
                inputDelay: JewelBlitzArena.Net.SessionConfig.DefaultInputDelay,
                deliveryDelayFrames: loopbackDeliveryDelay);
            _loopback.Reset(config, _catalog);
            LastHash = _loopback.Hash;
            _inMenu = false;
            _accum = 0f;
            Alpha = 0f;
        }

        public void RestartMatch()
        {
            if (simMode == GreyboxSimMode.LoopbackNet)
            {
                StartLoopbackNet();
            }
            else
            {
                StartCustomFromMeta();
            }
        }

        public void FinishMatchToMenu()
        {
            if (simMode == GreyboxSimMode.OfflineMeta && _metaFlow != null && _metaFlow.MatchLive)
            {
                _metaFlow.CompleteCustom();
            }

            EnterMenu();
        }

        public void GetDisplayStates(out MatchState previous, out MatchState current)
        {
            if (simMode == GreyboxSimMode.LoopbackNet && _loopback != null)
            {
                previous = _loopback.Previous;
                current = _loopback.Current;
                return;
            }

            MatchSession session = Session;
            previous = session.Previous;
            current = session.Current;
        }

        private void StepOnce()
        {
            InputFrame p0 = LocalInputReader.ReadPlayer1();
            InputFrame p1 = LocalInputReader.ReadPlayer2();

            if (simMode == GreyboxSimMode.LoopbackNet && _loopback != null)
            {
                _loopback.Tick(p0, p1);
                LastHash = _loopback.Hash;
                return;
            }

            if (_metaFlow != null && _metaFlow.MatchLive)
            {
                _metaFlow.Step(p0, p1);
                LastHash = _metaFlow.Session.Hash();
                return;
            }

            _offlineSession.Step(p0, p1);
            LastHash = _offlineSession.Hash();
        }
    }
}
