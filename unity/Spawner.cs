using UnityEngine;

// Spawns gems and items on timers at random spots inside the arena, and
// dresses the stage at match start with throwable furniture/props —
// in Power Stone, half the arena itself is ammunition.
public class Spawner : MonoBehaviour
{
    public float arenaHalfSize = 7f;

    [Header("Gems")]
    public float gemFirstDelay = 2f;
    public float gemInterval = 6f;
    public int maxGems = 2;

    [Header("Items (from the 110-item catalog)")]
    public float itemFirstDelay = 3f;
    public float itemInterval = 5f;
    public int maxGroundItems = 10; // includes the starting stage props

    private float gemTimer;
    private float itemTimer;

    // Stage furniture placed at match start — all throwable/catchable.
    private static readonly string[] StageProps =
        { "Cactus", "Cactus", "Crate", "Wooden Barrel", "Cactus", "Rock", "Bench", "Toolbox" };
    private static readonly Vector3[] PropSpots =
    {
        new Vector3(-6f, 0.9f, -5f), new Vector3(6f, 0.9f, -5f),
        new Vector3(-6f, 0.9f, 5f),  new Vector3(6f, 0.9f, 5f),
        new Vector3(0f, 0.9f, -6.5f), new Vector3(0f, 0.9f, 6.5f),
        new Vector3(-3f, 0.9f, 0f),  new Vector3(3f, 0.9f, 0f)
    };

    void Start()
    {
        gemTimer = gemFirstDelay;
        itemTimer = itemFirstDelay;

        for (int i = 0; i < StageProps.Length; i++)
        {
            var def = ItemCatalog.FindByName(StageProps[i]);
            if (def != null) ItemPickup.Spawn(PropSpots[i], def);
        }
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm != null && gm.MatchOver) return;

        gemTimer -= Time.deltaTime;
        if (gemTimer <= 0f)
        {
            gemTimer = gemInterval;
            if (FindObjectsOfType<PowerGem>().Length < maxGems)
                PowerGem.Spawn(RandomPos());
        }

        itemTimer -= Time.deltaTime;
        if (itemTimer <= 0f)
        {
            itemTimer = itemInterval;
            if (FindObjectsOfType<ItemPickup>().Length < maxGroundItems)
                ItemPickup.Spawn(RandomPos(), ItemCatalog.RandomItem());
        }
    }

    Vector3 RandomPos()
    {
        return new Vector3(Random.Range(-arenaHalfSize, arenaHalfSize), 1.0f,
                           Random.Range(-arenaHalfSize, arenaHalfSize));
    }
}
