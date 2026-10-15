using UnityEngine;

// A collectible power gem. Collect 3 to transform (handled in PlayerCombat).
// Uses distance checks instead of trigger physics so it works with zero setup.
public class PowerGem : MonoBehaviour
{
    public float collectDistance = 1.3f;
    public float pickupDelay = 0.6f; // a gem knocked loose can't be re-grabbed instantly
    private float spinSpeed = 120f;
    private float bobPhase;
    private float spawnTime;

    void Start()
    {
        bobPhase = Random.Range(0f, Mathf.PI * 2f);
        spawnTime = Time.time;
    }

    void Update()
    {
        if (Time.time - spawnTime < pickupDelay) { SpinOnly(); return; }
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
        Vector3 p = transform.position;
        p.y = 1.0f + Mathf.Sin(Time.time * 2.5f + bobPhase) * 0.2f;
        transform.position = p;

        var players = GameObject.FindGameObjectsWithTag("Player");
        for (int i = 0; i < players.Length; i++)
        {
            var combat = players[i].GetComponent<PlayerCombat>();
            if (combat == null || combat.IsTransformed) continue;
            float flatDist = Vector2.Distance(
                new Vector2(transform.position.x, transform.position.z),
                new Vector2(players[i].transform.position.x, players[i].transform.position.z));
            if (flatDist <= collectDistance)
            {
                combat.AddGem();
                Destroy(gameObject);
                return;
            }
        }
    }

    void SpinOnly()
    {
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
        Vector3 p = transform.position;
        p.y = 1.0f + Mathf.Sin(Time.time * 2.5f + bobPhase) * 0.2f;
        transform.position = p;
    }

    public static GameObject Spawn(Vector3 position)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "PowerGem";
        go.transform.position = position;
        go.transform.localScale = new Vector3(0.45f, 0.7f, 0.45f);
        var col = go.GetComponent<Collider>();
        if (col != null) Destroy(col); // distance-based pickup, no physics needed
        var rend = go.GetComponent<Renderer>();
        if (rend != null) rend.material.color = new Color(0.9f, 0.15f, 0.25f); // ruby red
        var light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.2f, 0.3f);
        light.intensity = 2f;
        light.range = 3f;
        go.AddComponent<PowerGem>();
        return go;
    }
}
