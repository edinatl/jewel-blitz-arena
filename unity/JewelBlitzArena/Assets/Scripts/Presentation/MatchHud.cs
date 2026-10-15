using JewelBlitzArena.Sim;
using UnityEngine;

namespace JewelBlitzArena.Presentation
{
    /// <summary>IMGUI greybox HUD: Life / jewels / Luster + hash for determinism checks.</summary>
    public sealed class MatchHud : MonoBehaviour
    {
        [SerializeField] private MatchDriver? driver;

        private void Start()
        {
            if (driver == null)
            {
                driver = FindFirstObjectByType<MatchDriver>();
            }
        }

        private void OnGUI()
        {
            if (driver == null)
            {
                return;
            }

            MatchState state = driver.Session.Current;
            const int pad = 12;
            GUILayout.BeginArea(new Rect(pad, pad, 420, 220));
            GUILayout.Label("Jewel Blitz Arena — greybox host (S3)");
            GUILayout.Label($"frame {state.Frame}  hash 0x{driver.LastHash:X16}");
            GUILayout.Label("P1 WASD+JKL  |  P2 arrows+123  |  R restart");

            for (int i = 0; i < state.ActorCount; i++)
            {
                ActorSlot a = MatchStateAccess.Actor(ref state, i);
                string blitz = a.IsBlitzed ? " BLITZ" : string.Empty;
                GUILayout.Label(
                    $"P{i + 1}  Life {a.Life}/{a.MaxLife}  Jewels {a.JewelsHeld}  Luster {a.Luster}{blitz}");
            }

            GUILayout.EndArea();

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.R)
            {
                driver.RestartMatch();
            }
        }
    }
}
