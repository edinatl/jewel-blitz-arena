using UnityEngine;

// An item lying in the arena — spawned by the Spawner, dropped when a thrown
// item lands, or placed as stage furniture at match start. Walk over it with
// empty hands to pick it up.
public class ItemPickup : MonoBehaviour
{
    public ItemDefinition Def;
    public float pickupDistance = 1.35f;
    private float bobPhase;
    private float baseY = 0.9f;

    void Start()
    {
        bobPhase = Random.Range(0f, Mathf.PI * 2f);
    }

    void Update()
    {
        transform.Rotate(Vector3.up, 55f * Time.deltaTime, Space.World);
        Vector3 p = transform.position;
        p.y = baseY + Mathf.Sin(Time.time * 2f + bobPhase) * 0.12f;
        transform.position = p;

        var players = GameObject.FindGameObjectsWithTag("Player");
        for (int i = 0; i < players.Length; i++)
        {
            var combat = players[i].GetComponent<PlayerCombat>();
            if (combat == null || combat.HasItem) continue;
            var health = players[i].GetComponent<Health>();
            if (health != null && health.IsKO) continue;
            float flatDist = Vector2.Distance(
                new Vector2(transform.position.x, transform.position.z),
                new Vector2(players[i].transform.position.x, players[i].transform.position.z));
            if (flatDist <= pickupDistance)
            {
                combat.EquipItem(Def);
                Destroy(gameObject);
                return;
            }
        }
    }

    public static GameObject Spawn(Vector3 position, ItemDefinition def)
    {
        var go = ItemVisual.Build(def, "Item_" + def.Name);
        ItemVisual.StripCollider(go);
        go.transform.position = new Vector3(position.x, 0.9f, position.z);
        if (def.Archetype == ItemArchetype.Melee)
            go.transform.rotation = Quaternion.Euler(0f, 0f, 30f);
        var pickup = go.AddComponent<ItemPickup>();
        pickup.Def = def;
        return go;
    }
}
