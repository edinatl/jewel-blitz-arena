using JewelBlitzArena.Meta;
using JewelBlitzArena.PresentationBridge;
using JewelBlitzArena.Sim;
using UnityEngine;

namespace JewelBlitzArena.Presentation
{
    /// <summary>
    /// Boot → menu shell → Custom / Loopback 2P. Completes Custom into Meta profile on Escape.
    /// </summary>
    public sealed class MetaMenuHud : MonoBehaviour
    {
        [SerializeField] private MatchDriver? driver;

        private CameraFrame _lastFrame;

        private void Start()
        {
            if (driver == null)
            {
                driver = FindFirstObjectByType<MatchDriver>();
            }
        }

        private void OnGUI()
        {
            if (driver == null || driver.MetaFlow == null)
            {
                return;
            }

            const int pad = 12;
            if (driver.InMenu)
            {
                DrawMenu(pad);
                return;
            }

            DrawMatchHud(pad);
            DrawEdgeBubbles();
        }

        private void DrawMenu(int pad)
        {
            GUILayout.BeginArea(new Rect(pad, pad, 460, 360));
            GUILayout.Label("Jewel Blitz Arena — Boot");
            PlayerProfile profile = driver!.MetaFlow!.Profile;
            GUILayout.Label($"Profile: {profile.DisplayName}  carats={profile.Carats}  flags=0x{profile.Flags:X}");
            GUILayout.Label(
                profile.Ledger.Owns(LedgerInventory.CustomGreyboxTokenItemId)
                    ? "Ledger: Custom greybox token owned"
                    : "Ledger: no Custom greybox token yet");
            GUILayout.Space(8);
            GUILayout.Label("Modes");

            foreach (MetaMenuEntry entry in driver.MetaFlow.BootApi.Menu.BuildEntries(profile))
            {
                string suffix = entry.Available
                    ? string.Empty
                    : $"  ({entry.UnavailableReason ?? "locked"})";
                if (entry.Available && entry.Mode == ModeId.Custom)
                {
                    if (GUILayout.Button($"▶ {entry.Label}{suffix}"))
                    {
                        driver.StartCustomFromMeta();
                    }
                }
                else if (entry.Available && entry.Mode == ModeId.Sparring)
                {
                    if (GUILayout.Button($"▶ {entry.Label}{suffix}"))
                    {
                        var req = new ModeRequest { Mode = ModeId.Sparring, ActorCount = 2 };
                        driver.StartCustomFromMeta(req);
                    }
                }
                else
                {
                    GUI.enabled = false;
                    GUILayout.Button($"{entry.Label}{suffix}");
                    GUI.enabled = true;
                }
            }

            GUILayout.Space(8);
            if (GUILayout.Button("▶ Loopback 2P (Net rollback greybox)"))
            {
                driver.StartLoopbackNet();
            }

            GUILayout.Label("P1 WASD+JKL  |  P2 arrows+123  |  Esc menu  |  R restart");
            GUILayout.EndArea();
        }

        private void DrawMatchHud(int pad)
        {
            driver!.GetDisplayStates(out _, out MatchState state);
            _lastFrame = StageCameraVolumes.FrameFromCatalog(driver.Catalog, ref state);
            CameraVolume volume = StageCameraVolumes.Resolve(driver.Catalog, in state);
            GUILayout.BeginArea(new Rect(pad, pad, 520, 320));
            string mode = driver.SimMode == GreyboxSimMode.LoopbackNet ? "LoopbackNet" : "OfflineMeta";
            GUILayout.Label($"Jewel Blitz Arena — {mode}");
            GUILayout.Label(
                $"frame {state.Frame}  phase {state.PhaseIndex}  hash 0x{driver.LastHash:X16}");
            GUILayout.Label(
                $"camera vol r={volume.Radius:0.#}  center=({volume.CenterX:0.#},{volume.CenterY:0.#},{volume.CenterZ:0.#})");
            if (_lastFrame.InScriptedPath)
            {
                GUILayout.Label($"scripted cam path t={_lastFrame.PathT:0.00}");
            }

            if (_lastFrame.FitFailed)
            {
                GUILayout.Label($"edge bubbles={_lastFrame.EdgeBubbleCount} (fit failed)");
            }

            if (driver.SimMode == GreyboxSimMode.LoopbackNet)
            {
                GUILayout.Label($"rollbacks={driver.RollbackCount}  desync={driver.DesyncFlagged}");
                if (driver.CpuReplacedMask != 0)
                {
                    GUILayout.Label($"CPU replace mask=0x{driver.CpuReplacedMask:X}");
                }
                else if (driver.IsStalling)
                {
                    string warn = driver.ShowDisconnectWarning ? "  DISCONNECT TIMER" : string.Empty;
                    if (driver.DisconnectReplaceReady)
                    {
                        warn += "  REPLACE READY";
                    }

                    GUILayout.Label($"waiting for peer… stallFrames={driver.StallFrames}{warn}");
                }
            }

            if (driver.MetaFlow != null)
            {
                bool token = driver.MetaFlow.Profile.Ledger.Owns(LedgerInventory.CustomGreyboxTokenItemId);
                GUILayout.Label(token ? "profile: greybox token ✓" : "profile: finish match (Esc) to stamp token");
            }

            GUILayout.Label("P1 WASD+JKL  |  P2 arrows+123  |  R restart  |  Esc menu");

            string patchLine = MatchWorldPatchHud.FormatSummary(ref state);
            if (!string.IsNullOrEmpty(patchLine))
            {
                GUILayout.Label(patchLine);
            }

            for (int i = 0; i < state.ActorCount; i++)
            {
                ActorSlot a = MatchStateAccess.Actor(ref state, i);
                string blitz = a.IsBlitzed ? " BLITZ" : string.Empty;
                MatchHoldHud hold = MatchHoldHud.FromActor(ref state, in a);
                MatchStatusHud status = MatchStatusHud.FromActor(in a);
                string hands = hold.HasAny ? $"  hands {hold.FormatHands()}" : string.Empty;
                string active = status.HasActiveStatus ? $"  {status.FormatActive()}" : string.Empty;
                string immune = status.HasImmunityHalo ? $"  {status.FormatImmunity()}" : string.Empty;
                GUILayout.Label(
                    $"P{i + 1}  Life {a.Life}/{a.MaxLife}  Jewels {a.JewelsHeld}  Luster {a.Luster}{blitz}{hands}{active}{immune}");
            }

            GUILayout.EndArea();

            Event e = Event.current;
            if (e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.R)
                {
                    driver.RestartMatch();
                }
                else if (e.keyCode == KeyCode.Escape)
                {
                    driver.FinishMatchToMenu();
                }
            }
        }

        private void DrawEdgeBubbles()
        {
            if (!_lastFrame.FitFailed || _lastFrame.EdgeBubbleCount <= 0)
            {
                return;
            }

            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f;
            float rx = Screen.width * 0.42f;
            float ry = Screen.height * 0.38f;
            const float size = 56f;

            for (int i = 0; i < _lastFrame.EdgeBubbleCount; i++)
            {
                EdgeBubble b = _lastFrame.GetEdgeBubble(i);
                float x = cx + (b.EdgeU * rx) - (size * 0.5f);
                float y = cy - (b.EdgeV * ry) - (size * 0.5f);
                var rect = new Rect(x, y, size, size);
                Color prev = GUI.color;
                GUI.color = new Color(0.12f, 0.14f, 0.18f, 0.85f);
                GUI.Box(rect, string.Empty);
                GUI.color = Color.white;
                string pips = b.JewelsHeld > 0 ? $" ◆{b.JewelsHeld}" : string.Empty;
                string item = string.Empty;
                if (b.HasHeldItemRight && b.HasHeldItemLeft)
                {
                    item = $" R{b.HeldItemIdRight} L{b.HeldItemIdLeft}";
                }
                else if (b.HasHeldItemRight)
                {
                    item = $" R{b.HeldItemIdRight}";
                }
                else if (b.HasHeldItemLeft)
                {
                    item = $" L{b.HeldItemIdLeft}";
                }

                string halo = b.HasImmunityHalo ? " ◌" : string.Empty;
                GUI.Label(rect, $"P{b.ActorIndex + 1}{pips}{item}{halo}");
                GUI.color = prev;
            }
        }
    }
}
