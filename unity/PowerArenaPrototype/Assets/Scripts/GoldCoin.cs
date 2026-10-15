using UnityEngine;

// A gold coin spilled by a knockdown (when the victim has no gem left to
// lose). Spins in place, magnetizes toward a nearby fighter, pays out 10.
public class GoldCoin : MonoBehaviour
{
    public int value = 10;
    private float age = 0f;

    void Update()
    {
        age += Time.deltaTime;
        transform.Rotate(Vector3.up, 180f * Time.deltaTime, Space.World);

        var players = GameObject.FindGameObjectsWithTag("Player");
        for (int i = 0; i < players.Length; i++)
        {
            var health = players[i].GetComponent<Health>();
            if (health != null && health.IsKO) continue;
            var combat = players[i].GetComponent<PlayerCombat>();
            if (combat != null && combat.IsKnockedDown) continue;
            Vector3 to = players[i].transform.position + Vector3.up * 0.9f - transform.position;
            float d = to.magnitude;
            if (d < 2.2f && d > 0.01f) transform.position += to.normalized * (2.6f * Time.deltaTime);
            if (d < 0.85f)
            {
                if (combat != null) combat.gold += value;
                Destroy(gameObject);
                return;
            }
        }
        if (age > 20f) Destroy(gameObject);
    }

    public static void Spawn(Vector3 position)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "GoldCoin";
        go.transform.position = new Vector3(position.x, 0.55f, position.z);
        go.transform.localScale = new Vector3(0.32f, 0.14f, 0.32f);
        ItemVisual.StripCollider(go);
        var rend = go.GetComponent<Renderer>();
        if (rend != null)
        {
            var m = new Material(Shader.Find("Standard"));
            m.color = new Color(1f, 0.85f, 0.29f);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", new Color(0.72f, 0.52f, 0.04f));
            rend.material = m;
        }
        go.AddComponent<GoldCoin>();
    }
}
