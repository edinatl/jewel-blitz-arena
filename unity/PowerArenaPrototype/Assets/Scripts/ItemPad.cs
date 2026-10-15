using UnityEngine;

// Item pad (v0.8): a gold circle on the arena floor that pops up a
// random item. When a fighter picks the item up (Action button), the
// pad waits 1 second and pops up a new random item. The disc visual is
// created by PrototypeBuilder; this component runs the item cycle.
public class ItemPad : MonoBehaviour
{
    public float respawnDelay = 1f;

    private ItemPickup current;
    private float timer = -1f;

    void Start()
    {
        timer = Random.Range(0f, 0.6f);
    }

    void Update()
    {
        if (timer > 0f)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f) SpawnNow();
        }
    }

    void SpawnNow()
    {
        var go = ItemPickup.Spawn(transform.position, ItemCatalog.RandomItem());
        current = go.GetComponent<ItemPickup>();
        if (current != null)
        {
            current.OnPickedUp = () =>
            {
                current = null;
                timer = respawnDelay;
            };
        }
    }
}
