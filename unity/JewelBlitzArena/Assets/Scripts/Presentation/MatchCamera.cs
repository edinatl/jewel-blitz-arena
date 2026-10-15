using JewelBlitzArena.Data;
using JewelBlitzArena.PresentationBridge;
using JewelBlitzArena.Sim;
using UnityEngine;

namespace JewelBlitzArena.Presentation
{
    /// <summary>
    /// Shared overhead camera — frames living actors clamped to the phase camera volume (S4).
    /// </summary>
    public sealed class MatchCamera : MonoBehaviour
    {
        [SerializeField] private MatchDriver? driver;
        [SerializeField] private Camera? targetCamera;
        [SerializeField] private float height = 14f;
        [SerializeField] private float pitchDegrees = 55f;
        [SerializeField] private float followLerp = 8f;
        [SerializeField] private float padding = 3f;

        private int _lastPhase = -1;

        private void Start()
        {
            if (driver == null)
            {
                driver = FindFirstObjectByType<MatchDriver>();
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (targetCamera != null)
            {
                targetCamera.orthographic = true;
            }
        }

        private void LateUpdate()
        {
            if (driver == null || targetCamera == null || driver.InMenu)
            {
                return;
            }

            driver.GetDisplayStates(out _, out MatchState state);
            ContentCatalog? catalog = driver.Catalog;
            CameraFrame frame = StageCameraVolumes.FrameFromCatalog(catalog, ref state, padding);

            // Snap on phase change / scripted transition path; otherwise critically-damped-ish follow.
            float lerp = followLerp;
            if (frame.PhaseIndex != _lastPhase || frame.InScriptedPath)
            {
                lerp = frame.InScriptedPath ? 60f : 40f;
                _lastPhase = frame.PhaseIndex;
            }

            Quaternion rot = Quaternion.Euler(pitchDegrees, 0f, 0f);
            Vector3 look = new Vector3(frame.LookX, 0f, frame.LookZ);
            Vector3 desired = look + rot * new Vector3(0f, 0f, -Mathf.Max(height, frame.LookY));
            float t = 1f - Mathf.Exp(-lerp * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, desired, t);
            transform.rotation = rot;

            targetCamera.orthographicSize = Mathf.Lerp(
                targetCamera.orthographicSize,
                frame.OrthoSize,
                t);
        }
    }
}
