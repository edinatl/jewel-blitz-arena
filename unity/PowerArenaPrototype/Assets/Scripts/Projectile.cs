using UnityEngine;

// Simple projectile used by the Blaster weapon, Bomb weapon, and the
// transformed special attack. Created procedurally at runtime (no prefab needed).
public class Projectile : MonoBehaviour
{
    public float damage = 10f;
    public bool explosive = false;
    public float explosionRadius = 2.5f;
    public GameObject owner;          // who fired it; they can't hit themselves
    public float lifeTime = 4f;

    private bool exploded = false;

    void Update()
    {
        lifeTime -= Time.deltaTime;
        if (lifeTime <= 0f)
        {
            if (explosive) Explode();
            else Destroy(gameObject);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (explosive)
        {
            Explode();
            return;
        }

        if (collision.gameObject == owner) return;

        var health = collision.gameObject.GetComponent<Health>();
        var combat = collision.gameObject.GetComponent<PlayerCombat>();
        if (combat != null && combat.IsRolling) { Destroy(gameObject); return; }
        if (health != null)
        {
            Vector3 dir = collision.contacts.Length > 0
                ? (collision.transform.position - transform.position).normalized
                : transform.forward;
            dir.y = 0.3f;
            health.TakeDamage(damage);
            if (combat != null)
            {
                combat.ApplyKnockback(dir, 6f);
                combat.RegisterHitTaken(damage); // any hit knocks a gem loose
                var ownerCombat = owner != null ? owner.GetComponent<PlayerCombat>() : null;
                if (ownerCombat != null && ownerCombat.LastWeaponName == "Freeze Ray")
                    combat.ApplySlow(1.2f); // UNIQUE: Freeze Ray slows its victim
                if (ownerCombat != null && ownerCombat.LastWeaponName == "Lightning Gun")
                {
                    // UNIQUE: Lightning Gun arcs to another nearby fighter.
                    var others = FindObjectsOfType<Health>();
                    for (int i = 0; i < others.Length; i++)
                    {
                        if (others[i].gameObject == collision.gameObject || others[i].gameObject == owner) continue;
                        if (Vector3.Distance(others[i].transform.position, collision.transform.position) < 3.2f)
                        {
                            others[i].TakeDamage(6f);
                            var oc2 = others[i].GetComponent<PlayerCombat>();
                            if (oc2 != null) oc2.RegisterHitTaken(6f);
                        }
                    }
                }
            }
        }
        Destroy(gameObject);
    }

    void Explode()
    {
        if (exploded) return;
        exploded = true;

        // Damage everyone in radius except the owner.
        var all = FindObjectsOfType<Health>();
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].gameObject == owner) continue;
            float dist = Vector3.Distance(transform.position, all[i].transform.position);
            if (dist <= explosionRadius)
            {
                var combat = all[i].GetComponent<PlayerCombat>();
                if (combat != null && (combat.IsRolling || combat.IsKnockedDown)) continue;
                all[i].TakeDamage(damage);
                if (combat != null)
                {
                    Vector3 dir = (all[i].transform.position - transform.position).normalized;
                    dir.y = 0.4f;
                    combat.ApplyKnockback(dir, 9f);
                    combat.RegisterHitTaken(damage); // any hit knocks a gem loose
                }
            }
        }

        // Quick visual pop: expanding sphere that fades by shrinking lifetime.
        var flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        flash.transform.position = transform.position;
        flash.transform.localScale = Vector3.one * explosionRadius * 2f;
        var col = flash.GetComponent<Collider>();
        if (col != null) Destroy(col);
        var rend = flash.GetComponent<Renderer>();
        if (rend != null) rend.material.color = new Color(1f, 0.55f, 0.1f);
        Destroy(flash, 0.15f);

        Destroy(gameObject);
    }

    // Factory helpers -------------------------------------------------------

    public static GameObject Fire(GameObject owner, Vector3 origin, Vector3 direction,
                                  float speed, float damage, float scale, Color color,
                                  bool explosive, float gravityScale)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = explosive ? "Bomb" : "Projectile";
        go.transform.position = origin;
        go.transform.localScale = Vector3.one * scale;

        var rend = go.GetComponent<Renderer>();
        if (rend != null) rend.material.color = color;

        // v0.4: motion trail so shots read as fast, animated streaks.
        var trail = go.AddComponent<TrailRenderer>();
        trail.time = 0.22f;
        trail.startWidth = scale * 0.75f;
        trail.endWidth = 0f;
        trail.material = new Material(Shader.Find("Sprites/Default"));
        trail.startColor = color;
        trail.endColor = new Color(color.r, color.g, color.b, 0f);

        var rb = go.AddComponent<Rigidbody>();
        rb.useGravity = gravityScale > 0f;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        direction.Normalize();
        rb.velocity = direction * speed;
        if (gravityScale > 0f)
        {
            // Rigidbody gravity is global; for a lobbed bomb we just rely on
            // normal gravity and a lower forward speed plus an upward arc,
            // which the caller bakes into 'direction'.
        }

        var proj = go.AddComponent<Projectile>();
        proj.damage = damage;
        proj.owner = owner;
        proj.explosive = explosive;
        if (explosive) proj.lifeTime = 2.5f;
        return go;
    }
}
