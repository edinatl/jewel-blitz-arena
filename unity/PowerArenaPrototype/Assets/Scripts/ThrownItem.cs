using UnityEngine;

// An item flying through the air after being thrown. Moves kinematically
// (own velocity + gravity) and checks players every frame, in this order:
//   1. CATCH — target has empty hands, their catch window is open, and
//      they're facing the item: they catch it and now hold it.
//   2. HIT   — damage + knockback + a gem knocked loose. Explosives blow up.
//   3. LAND  — hits the floor/walls: explosives blow up, everything else
//      becomes a ground pickup again for anyone to grab.
public class ThrownItem : MonoBehaviour
{
    public ItemDefinition Def;
    public GameObject Thrower;
    public Vector3 Velocity;

    private float age = 0f;
    private const float Gravity = -20f;
    private const float ArenaHalf = 8.6f;
    private bool resolved = false;

    void Update()
    {
        if (resolved) return;
        age += Time.deltaTime;

        Velocity.y += Gravity * Time.deltaTime;
        transform.position += Velocity * Time.deltaTime;
        transform.Rotate(Vector3.right, 360f * Time.deltaTime, Space.World);
        transform.Rotate(Vector3.up, 240f * Time.deltaTime, Space.World);

        // Wall bounce: stay in the arena, shed some speed.
        Vector3 p = transform.position;
        if (p.x > ArenaHalf || p.x < -ArenaHalf)
        {
            Velocity.x = -Velocity.x * 0.6f;
            p.x = Mathf.Clamp(p.x, -ArenaHalf, ArenaHalf);
            transform.position = p;
        }
        if (p.z > ArenaHalf || p.z < -ArenaHalf)
        {
            Velocity.z = -Velocity.z * 0.6f;
            p.z = Mathf.Clamp(p.z, -ArenaHalf, ArenaHalf);
            transform.position = p;
        }

        // Player contact
        var players = GameObject.FindGameObjectsWithTag("Player");
        for (int i = 0; i < players.Length; i++)
        {
            var other = players[i];
            if (other == Thrower && age < 0.3f) continue; // can't hit/catch yourself at launch
            Vector3 toPlayer = other.transform.position + Vector3.up * 1.0f - transform.position;
            if (toPlayer.magnitude > 1.25f) continue;

            var combat = other.GetComponent<PlayerCombat>();
            var health = other.GetComponent<Health>();
            if (combat == null || health == null || health.IsKO) continue;
            // Rolling fighters duck clean under it; knocked-down are safe.
            if (combat.IsRolling || combat.IsKnockedDown) continue;

            // CATCH
            if (!combat.HasItem && combat.CatchWindowActive)
            {
                Vector3 incoming = -Velocity.normalized;
                float facing = Vector3.Dot(other.transform.forward,
                    new Vector3(incoming.x, 0f, incoming.z).normalized);
                if (facing > 0.1f)
                {
                    combat.NotifyCaught();
                    combat.EquipItem(Def);
                    resolved = true;
                    Destroy(gameObject);
                    return;
                }
            }

            // HIT
            resolved = true;
            if (Def.IsExplosive)
            {
                Explode(transform.position);
            }
            else
            {
                health.TakeDamage(Def.ThrowDamage);
                Vector3 dir = new Vector3(Velocity.x, 0f, Velocity.z).normalized;
                dir.y = 0.35f;
                combat.ApplyKnockback(dir, Def.Knockback);
                combat.RegisterHitTaken(Def.ThrowDamage); // any hit knocks a gem loose
                LandAsPickup();
            }
            Destroy(gameObject);
            return;
        }

        // Floor
        if (transform.position.y <= 0.55f)
        {
            resolved = true;
            if (Def.IsExplosive) Explode(transform.position);
            else LandAsPickup();
            Destroy(gameObject);
        }

        if (age > 6f) // safety cleanup
        {
            resolved = true;
            if (!Def.IsExplosive) LandAsPickup();
            Destroy(gameObject);
        }
    }

    void LandAsPickup()
    {
        Vector3 p = transform.position;
        p.y = 0.9f;
        ItemPickup.Spawn(p, Def);
    }

    void Explode(Vector3 at)
    {
        var all = FindObjectsOfType<Health>();
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].gameObject == Thrower) continue;
            float dist = Vector3.Distance(at, all[i].transform.position);
            if (dist <= 2.6f)
            {
                all[i].TakeDamage(Def.Damage);
                var combat = all[i].GetComponent<PlayerCombat>();
                if (combat != null)
                {
                    Vector3 dir = (all[i].transform.position - at).normalized;
                    dir.y = 0.4f;
                    combat.ApplyKnockback(dir, 10f);
                    combat.RegisterHitTaken(Def.Damage); // any hit knocks a gem loose
                }
            }
        }
        var flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        flash.transform.position = at;
        flash.transform.localScale = Vector3.one * 5.2f;
        ItemVisual.StripCollider(flash);
        var rend = flash.GetComponent<Renderer>();
        if (rend != null) rend.material.color = new Color(1f, 0.55f, 0.1f);
        Destroy(flash, 0.15f);
    }

    public static GameObject Throw(GameObject thrower, ItemDefinition def,
                                   Vector3 origin, Vector3 direction)
    {
        var go = ItemVisual.Build(def, "Thrown_" + def.Name);
        ItemVisual.StripCollider(go);
        go.transform.position = origin;
        var thrown = go.AddComponent<ThrownItem>();
        thrown.Def = def;
        thrown.Thrower = thrower;
        Vector3 dir = direction.normalized;
        thrown.Velocity = dir * def.ThrowSpeed + Vector3.up * 4.5f; // lobbed arc
        return go;
    }
}
