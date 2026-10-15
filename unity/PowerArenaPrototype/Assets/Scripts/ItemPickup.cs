using UnityEngine;

// An item lying in the arena — spawned by the Spawner, dropped when a thrown
// item lands, or placed as stage furniture at match start. Since v0.7 it is
// picked up MANUALLY: stand next to it with empty hands and press the Action
// button (heavy key) — walking over it no longer picks it up.
public class ItemPickup : MonoBehaviour
{
    public ItemDefinition Def;
    public System.Action OnPickedUp; // ItemPad hooks this to schedule a respawn
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
    }

    // Called by PlayerCombat.TryPickupNearest (Action button).
    public void PickupBy(PlayerCombat combat)
    {
        combat.EquipItem(Def);
        if (OnPickedUp != null) OnPickedUp();
        Destroy(gameObject);
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
