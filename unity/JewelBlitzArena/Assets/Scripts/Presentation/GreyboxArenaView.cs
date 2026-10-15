using JewelBlitzArena.PresentationBridge;
using JewelBlitzArena.Sim;
using UnityEngine;

namespace JewelBlitzArena.Presentation
{
    /// <summary>
    /// Minimal primitives for actors (capsules), jewels (spheres), loose items (cubes).
    /// Positions are Fix→float display samples with optional interpolation.
    /// </summary>
    public sealed class GreyboxArenaView : MonoBehaviour
    {
        [SerializeField] private MatchDriver? driver;
        [SerializeField] private float actorHeight = 1.6f;
        [SerializeField] private float actorRadius = 0.35f;
        [SerializeField] private float jewelRadius = 0.22f;
        [SerializeField] private float itemSize = 0.4f;
        [SerializeField] private bool interpolate = true;

        private Transform[] _actors = System.Array.Empty<Transform>();
        private Transform[] _jewels = System.Array.Empty<Transform>();
        private Transform[] _items = System.Array.Empty<Transform>();
        private Transform? _floor;

        private static readonly Color[] ActorColors =
        {
            new Color(0.85f, 0.35f, 0.25f),
            new Color(0.25f, 0.55f, 0.85f),
            new Color(0.35f, 0.75f, 0.4f),
            new Color(0.85f, 0.75f, 0.25f),
        };

        private void Start()
        {
            if (driver == null)
            {
                driver = FindFirstObjectByType<MatchDriver>();
            }

            EnsurePools();
            BuildFloor();
        }

        private void LateUpdate()
        {
            if (driver == null || driver.InMenu)
            {
                SetAllInactive();
                return;
            }

            float alpha = interpolate ? driver.Alpha : 1f;
            driver.GetDisplayStates(out MatchState prev, out MatchState curr);
            SyncActors(ref prev, ref curr, alpha);
            SyncJewels(ref prev, ref curr, alpha);
            SyncItems(ref prev, ref curr, alpha);
        }

        private void SetAllInactive()
        {
            for (int i = 0; i < _actors.Length; i++)
            {
                if (_actors[i] != null)
                {
                    _actors[i].gameObject.SetActive(false);
                }
            }

            for (int i = 0; i < _jewels.Length; i++)
            {
                if (_jewels[i] != null)
                {
                    _jewels[i].gameObject.SetActive(false);
                }
            }

            for (int i = 0; i < _items.Length; i++)
            {
                if (_items[i] != null)
                {
                    _items[i].gameObject.SetActive(false);
                }
            }
        }

        private void EnsurePools()
        {
            if (_actors.Length == PoolCaps.Actors)
            {
                return;
            }

            _actors = new Transform[PoolCaps.Actors];
            for (int i = 0; i < PoolCaps.Actors; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                go.name = $"Actor_{i}";
                go.transform.SetParent(transform, false);
                go.transform.localScale = new Vector3(actorRadius * 2f, actorHeight * 0.5f, actorRadius * 2f);
                var renderer = go.GetComponent<Renderer>();
                renderer.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")
                                                       ?? Shader.Find("Standard"));
                renderer.sharedMaterial.color = ActorColors[i];
                var col = go.GetComponent<Collider>();
                if (col != null)
                {
                    Destroy(col);
                }

                go.SetActive(false);
                _actors[i] = go.transform;
            }

            _jewels = new Transform[PoolCaps.Jewels];
            for (int i = 0; i < PoolCaps.Jewels; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = $"Jewel_{i}";
                go.transform.SetParent(transform, false);
                go.transform.localScale = Vector3.one * (jewelRadius * 2f);
                var renderer = go.GetComponent<Renderer>();
                renderer.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")
                                                       ?? Shader.Find("Standard"));
                renderer.sharedMaterial.color = new Color(0.95f, 0.55f, 0.85f);
                var col = go.GetComponent<Collider>();
                if (col != null)
                {
                    Destroy(col);
                }

                go.SetActive(false);
                _jewels[i] = go.transform;
            }

            _items = new Transform[PoolCaps.LooseItems];
            for (int i = 0; i < PoolCaps.LooseItems; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = $"Item_{i}";
                go.transform.SetParent(transform, false);
                go.transform.localScale = Vector3.one * itemSize;
                var renderer = go.GetComponent<Renderer>();
                renderer.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")
                                                       ?? Shader.Find("Standard"));
                renderer.sharedMaterial.color = new Color(0.65f, 0.55f, 0.35f);
                var col = go.GetComponent<Collider>();
                if (col != null)
                {
                    Destroy(col);
                }

                go.SetActive(false);
                _items[i] = go.transform;
            }
        }

        private void BuildFloor()
        {
            if (_floor != null)
            {
                return;
            }

            float half = FixDisplay.ToFloat(PhysicsConstants.ArenaHalfExtent);
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "ArenaFloor";
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, -0.05f, 0f);
            go.transform.localScale = new Vector3(half * 2f, 0.1f, half * 2f);
            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")
                                                   ?? Shader.Find("Standard"));
            renderer.sharedMaterial.color = new Color(0.22f, 0.24f, 0.26f);
            var col = go.GetComponent<Collider>();
            if (col != null)
            {
                Destroy(col);
            }

            _floor = go.transform;
        }

        private void SyncActors(ref MatchState prev, ref MatchState curr, float alpha)
        {
            for (int i = 0; i < PoolCaps.Actors; i++)
            {
                Transform t = _actors[i];
                if (i >= curr.ActorCount)
                {
                    t.gameObject.SetActive(false);
                    continue;
                }

                ActorSlot a = MatchStateAccess.Actor(ref curr, i);
                ActorSlot b = MatchStateAccess.Actor(ref prev, i);
                if (!a.IsAlive)
                {
                    t.gameObject.SetActive(false);
                    continue;
                }

                t.gameObject.SetActive(true);
                Vector3 p = LerpPos(b.PosX, b.PosY, b.PosZ, a.PosX, a.PosY, a.PosZ, alpha);
                p.y += actorHeight * 0.5f;
                t.position = p;
                float scale = a.IsBlitzed ? 1.25f : 1f;
                t.localScale = new Vector3(actorRadius * 2f * scale, actorHeight * 0.5f * scale, actorRadius * 2f * scale);
            }
        }

        private void SyncJewels(ref MatchState prev, ref MatchState curr, float alpha)
        {
            for (int i = 0; i < PoolCaps.Jewels; i++)
            {
                Transform t = _jewels[i];
                JewelSlot a = MatchStateAccess.GetJewel(ref curr, i);
                JewelSlot b = MatchStateAccess.GetJewel(ref prev, i);
                if (!a.IsOnField)
                {
                    t.gameObject.SetActive(false);
                    continue;
                }

                t.gameObject.SetActive(true);
                Vector3 p = LerpPos(b.PosX, b.PosY, b.PosZ, a.PosX, a.PosY, a.PosZ, alpha);
                p.y += jewelRadius;
                t.position = p;
            }
        }

        private void SyncItems(ref MatchState prev, ref MatchState curr, float alpha)
        {
            for (int i = 0; i < PoolCaps.LooseItems; i++)
            {
                Transform t = _items[i];
                LooseItemSlot a = MatchStateAccess.GetLooseItem(ref curr, i);
                LooseItemSlot b = MatchStateAccess.GetLooseItem(ref prev, i);
                bool show = a.IsActive && (a.IsOnField || a.InFlight);
                if (!show)
                {
                    t.gameObject.SetActive(false);
                    continue;
                }

                t.gameObject.SetActive(true);
                Vector3 p = LerpPos(b.PosX, b.PosY, b.PosZ, a.PosX, a.PosY, a.PosZ, alpha);
                p.y += itemSize * 0.5f;
                t.position = p;
            }
        }

        private static Vector3 LerpPos(
            Fix ax, Fix ay, Fix az,
            Fix bx, Fix by, Fix bz,
            float alpha)
        {
            float x0 = FixDisplay.ToFloat(ax);
            float y0 = FixDisplay.ToFloat(ay);
            float z0 = FixDisplay.ToFloat(az);
            float x1 = FixDisplay.ToFloat(bx);
            float y1 = FixDisplay.ToFloat(by);
            float z1 = FixDisplay.ToFloat(bz);
            return new Vector3(
                Mathf.Lerp(x0, x1, alpha),
                Mathf.Lerp(y0, y1, alpha),
                Mathf.Lerp(z0, z1, alpha));
        }
    }
}
