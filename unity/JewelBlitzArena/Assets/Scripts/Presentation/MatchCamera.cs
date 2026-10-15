using JewelBlitzArena.PresentationBridge;
using JewelBlitzArena.Sim;
using UnityEngine;

namespace JewelBlitzArena.Presentation
{
    /// <summary>Shared overhead camera stub — framing living actors; no split screen.</summary>
    public sealed class MatchCamera : MonoBehaviour
    {
        [SerializeField] private MatchDriver? driver;
        [SerializeField] private Camera? targetCamera;
        [SerializeField] private float height = 14f;
        [SerializeField] private float pitchDegrees = 55f;
        [SerializeField] private float followLerp = 8f;
        [SerializeField] private float minOrthoSize = 8f;
        [SerializeField] private float padding = 3f;

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
            if (driver == null || targetCamera == null)
            {
                return;
            }

            MatchState state = driver.Session.Current;
            Vector3 center = Vector3.zero;
            int alive = 0;
            float maxDist = 0f;

            for (int i = 0; i < state.ActorCount; i++)
            {
                ActorSlot actor = MatchStateAccess.Actor(ref state, i);
                if (!actor.IsAlive)
                {
                    continue;
                }

                Vector3 p = new Vector3(
                    FixDisplay.ToFloat(actor.PosX),
                    0f,
                    FixDisplay.ToFloat(actor.PosZ));
                center += p;
                alive++;
            }

            if (alive == 0)
            {
                center = Vector3.zero;
            }
            else
            {
                center /= alive;
            }

            for (int i = 0; i < state.ActorCount; i++)
            {
                ActorSlot actor = MatchStateAccess.Actor(ref state, i);
                if (!actor.IsAlive)
                {
                    continue;
                }

                Vector3 p = new Vector3(
                    FixDisplay.ToFloat(actor.PosX),
                    0f,
                    FixDisplay.ToFloat(actor.PosZ));
                maxDist = Mathf.Max(maxDist, Vector3.Distance(center, p));
            }

            Quaternion rot = Quaternion.Euler(pitchDegrees, 0f, 0f);
            Vector3 desired = center + rot * new Vector3(0f, 0f, -height);
            transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-followLerp * Time.deltaTime));
            transform.rotation = rot;

            targetCamera.orthographicSize = Mathf.Max(minOrthoSize, maxDist + padding);
        }
    }
}
