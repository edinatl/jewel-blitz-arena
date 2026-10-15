using UnityEngine;

// Patrolling hazard orb: bounces around the arena, hits hurt and knock back.
// The stage itself is a threat, like Power Stone 2's moving stages.
public class StageHazard : MonoBehaviour
{
    public float arenaHalfSize = 8f;
    public float speed = 4f;
    public float damage = 10f;
    public float hitCooldownPerPlayer = 1f;

    private Vector3 dir;
    private float p1Cooldown = 0f;
    private float p2Cooldown = 0f;

    void Start()
    {
        dir = new Vector3(1f, 0f, 0.6f).normalized;
    }

    void Update()
    {
        p1Cooldown -= Time.deltaTime;
        p2Cooldown -= Time.deltaTime;

        Vector3 p = transform.position;
        p += dir * speed * Time.deltaTime;
        if (p.x > arenaHalfSize || p.x < -arenaHalfSize) { dir.x = -dir.x; p.x = Mathf.Clamp(p.x, -arenaHalfSize, arenaHalfSize); }
        if (p.z > arenaHalfSize || p.z < -arenaHalfSize) { dir.z = -dir.z; p.z = Mathf.Clamp(p.z, -arenaHalfSize, arenaHalfSize); }
        p.y = 1.0f + Mathf.Sin(Time.time * 3f) * 0.15f;
        transform.position = p;
        transform.Rotate(Vector3.forward, 180f * Time.deltaTime);

        var gm = GameManager.Instance;
        if (gm != null && gm.MatchOver) return;

        var players = GameObject.FindGameObjectsWithTag("Player");
        for (int i = 0; i < players.Length; i++)
        {
            float cd = (i == 0) ? p1Cooldown : p2Cooldown;
            if (cd > 0f) continue;
            float dist = Vector2.Distance(
                new Vector2(transform.position.x, transform.position.z),
                new Vector2(players[i].transform.position.x, players[i].transform.position.z));
            if (dist < 1.2f)
            {
                var health = players[i].GetComponent<Health>();
                var combat = players[i].GetComponent<PlayerCombat>();
                if (health != null) health.TakeDamage(damage);
                if (combat != null)
                {
                    Vector3 away = (players[i].transform.position - transform.position).normalized;
                    away.y = 0.35f;
                    combat.ApplyKnockback(away, 8f);
                    combat.RegisterHitTaken(damage); // any hit knocks a gem loose
                }
                if (i == 0) p1Cooldown = hitCooldownPerPlayer; else p2Cooldown = hitCooldownPerPlayer;
            }
        }
    }

    public static GameObject Create(Vector3 position)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "StageHazard";
        go.transform.position = position;
        go.transform.localScale = Vector3.one * 1.1f;
        var col = go.GetComponent<Collider>();
        if (col != null) Destroy(col);
        var rend = go.GetComponent<Renderer>();
        if (rend != null) rend.material.color = new Color(0.55f, 0.1f, 0.7f); // hazard purple
        var light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(0.7f, 0.2f, 1f);
        light.intensity = 1.5f;
        light.range = 4f;
        go.AddComponent<StageHazard>();
        return go;
    }
}
