using JewelBlitzArena.Data;
using JewelBlitzArena.PresentationBridge;
using JewelBlitzArena.Sim;
using UnityEngine;

namespace JewelBlitzArena.Presentation
{
    /// <summary>
    /// Fixed 60 Hz sim step driven from Unity. Presentation-only; no float rules in Sim.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class MatchDriver : MonoBehaviour
    {
        public const float SimDt = 1f / Simulation.TickRateHz;

        [SerializeField] private ulong seed = 0x53335F484F5354UL; // S3_HOST
        [SerializeField] private int actorCount = 2;
        [SerializeField] private bool autoStep = true;
        [SerializeField] private TextAsset? contentBlob;

        private readonly MatchSession _session = new MatchSession();
        private float _accum;
        private ContentCatalog? _catalog;

        public MatchSession Session => _session;

        public float Alpha { get; private set; }

        public ulong LastHash { get; private set; }

        private void Awake()
        {
            if (contentBlob != null && contentBlob.bytes != null && contentBlob.bytes.Length > 0)
            {
                _catalog = ContentBlobReader.Read(contentBlob.bytes);
            }

            RestartMatch();
        }

        private void Update()
        {
            if (!autoStep)
            {
                return;
            }

            _accum += Time.unscaledDeltaTime;
            // Cap catch-up so a hitch does not explode the sim.
            int steps = 0;
            while (_accum >= SimDt && steps < 8)
            {
                _accum -= SimDt;
                StepOnce(ReadLocalInputs());
                steps++;
            }

            Alpha = Mathf.Clamp01(_accum / SimDt);
        }

        public void RestartMatch()
        {
            _accum = 0f;
            Alpha = 0f;
            MatchConfig config = MatchConfig.CreateDefault(seed, actorCount);
            _session.Reset(config, _catalog);
            LastHash = _session.Hash();
        }

        public void StepOnce(InputFrame[] inputs)
        {
            _session.Step(inputs);
            LastHash = _session.Hash();
        }

        public static InputFrame[] ReadLocalInputs()
        {
            var inputs = new InputFrame[PoolCaps.Actors];
            inputs[0] = LocalInputReader.ReadPlayer1();
            inputs[1] = LocalInputReader.ReadPlayer2();
            inputs[2] = InputFrame.Neutral;
            inputs[3] = InputFrame.Neutral;
            return inputs;
        }
    }
}
