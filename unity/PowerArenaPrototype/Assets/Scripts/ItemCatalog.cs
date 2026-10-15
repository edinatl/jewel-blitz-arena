using UnityEngine;

public enum ItemArchetype { Melee, Gun, Throwable, Explosive, Food, Special }
public enum ItemShape { Box, Sphere, Cylinder, Capsule }

// One item definition. 111 distinct items live in ItemCatalog below —
// each with its own name, damage, uses, throw stats, heal, knockback,
// size, shape, and color. Behavior is driven by these stats, so a
// Revolver, a Chair, and a Roast Chicken all genuinely play differently.
public class ItemDefinition
{
    public string Name;
    public ItemArchetype Archetype;
    public float Damage;       // melee hit / shot / explosion damage
    public int Uses;           // swings / shots / eats before it's gone (throwing always consumes the held copy)
    public float ThrowDamage;  // damage when it hits someone mid-flight
    public float ThrowSpeed;
    public float Heal;         // Food only
    public float Cooldown;
    public float Knockback;
    public float Scale;
    public ItemShape Shape;
    public Color Color;

    public bool IsExplosive { get { return Archetype == ItemArchetype.Explosive; } }
    // Anything can be thrown. These archetypes exist to be thrown.
    public bool IsThrowFocused
    {
        get { return Archetype == ItemArchetype.Throwable || Archetype == ItemArchetype.Explosive; }
    }
}

public static class ItemCatalog
{
    private static ItemDefinition[] all;

    public static ItemDefinition[] All
    {
        get
        {
            if (all == null) Build();
            return all;
        }
    }

    public static int Count { get { return All.Length; } }

    public static ItemDefinition RandomItem()
    {
        return All[Random.Range(0, All.Length)];
    }

    public static ItemDefinition FindByName(string name)
    {
        for (int i = 0; i < All.Length; i++)
            if (All[i].Name == name) return All[i];
        return null;
    }

    static void Build()
    {
        var list = new System.Collections.Generic.List<ItemDefinition>();

        string[] melee = {
            "Baseball Bat","Cricket Bat","Wooden Club","Steel Pipe","Wrench","Hammer",
            "Mallet","Frying Pan","Katana","Broadsword","Rapier","Battle Axe",
            "War Mace","Spear","Halberd","Chainsaw","Laser Sword","Spiked Club",
            "Golf Club","Tennis Racket","Broom","Shovel","Torch","Umbrella"
        };
        for (int i = 0; i < melee.Length; i++)
            list.Add(Make(melee[i], ItemArchetype.Melee, 12f + (i % 5), 3 + (i % 3),
                          7f, 13f, 0f, 0.5f, 10f + (i % 4), 0.9f, ItemShape.Cylinder, i, 24));

        string[] guns = {
            "Blaster Pistol","Revolver","Shotgun","Machine Gun","Laser Gun","Plasma Rifle",
            "Flame Thrower","Freeze Ray","Rocket Launcher","Grenade Launcher","Crossbow","Bow",
            "Slingshot","Water Cannon","Bubble Gun","Lightning Gun","Sonic Blaster","Railgun"
        };
        for (int i = 0; i < guns.Length; i++)
            list.Add(Make(guns[i], ItemArchetype.Gun, 9f + (i % 6), 3 + (i % 4),
                          8f, 14f, 0f, 0.4f, 6f, 0.55f, ItemShape.Box, i, 18));

        string[] throwables = {
            "Chair","Stool","Crate","Wooden Barrel","Metal Barrel","Rock","Brick","Book",
            "Vase","Plant Pot","Cactus","Table Lamp","Bench","Small Table","Tire","Bowling Ball",
            "Dumbbell","Kettle","Bucket","Toolbox","Backpack","Globe","Television",
            "Microwave","Safe","Anvil","Treasure Chest","Ale Barrel","Statue Head"
        };
        for (int i = 0; i < throwables.Length; i++)
        {
            // Heavier props (later in the list) fly slower and hit harder.
            bool heavy = i >= 20;
            list.Add(Make(throwables[i], ItemArchetype.Throwable, 6f, 99,
                          heavy ? 16f : 10f + (i % 4), heavy ? 10f : 15f, 0f, 0.5f,
                          heavy ? 12f : 8f, heavy ? 0.85f : 0.5f, ItemShape.Box, i, 28));
        }

        string[] explosives = {
            "Bomb","Big Bomb","Dynamite Bundle","Grenade","Fire Bomb","Ice Bomb",
            "Shock Bomb","Powder Keg","Oil Drum","Gas Can","Firework Crate","Land Mine",
            "Sticky Bomb","Holy Bomb","Poison Bomb","Cluster Bomb"
        };
        for (int i = 0; i < explosives.Length; i++)
            list.Add(Make(explosives[i], ItemArchetype.Explosive, 18f + (i % 6), 1,
                          12f, 12f, 0f, 0.6f, 10f, 0.5f, ItemShape.Sphere, i, 16));

        string[] food = {
            "Cake","Roast Chicken","Pizza","Burger","Sushi Platter","Apple",
            "Banana","Watermelon","Cheese Wheel","Honey Jar","Energy Drink","Magic Potion"
        };
        for (int i = 0; i < food.Length; i++)
            list.Add(Make(food[i], ItemArchetype.Food, 4f, 1,
                          5f, 14f, 18f + (i % 5) * 3f, 0.5f, 4f, 0.45f, ItemShape.Sphere, i, 12));

        string[] special = {
            "Fire Wand","Ice Wand","Lightning Rod","Wind Fan","Poison Dagger","Vampire Fang",
            "Golden Hammer","Giant Glove","Shock Glove","Magnet Coil","Gravity Orb","Mirror Shield"
        };
        for (int i = 0; i < special.Length; i++)
            list.Add(Make(special[i], ItemArchetype.Special, 14f + (i % 5), 4,
                          9f, 13f, 0f, 0.45f, 13f, 0.6f, ItemShape.Capsule, i, 12));

        all = list.ToArray();
    }

    static ItemDefinition Make(string name, ItemArchetype arch, float damage, int uses,
                               float throwDamage, float throwSpeed, float heal,
                               float cooldown, float knockback, float scale,
                               ItemShape shape, int index, int catCount)
    {
        // Distinct color per item: hue anchored per archetype, stepped per item.
        float archHue = 0f;
        switch (arch)
        {
            case ItemArchetype.Melee: archHue = 0.08f; break;
            case ItemArchetype.Gun: archHue = 0.55f; break;
            case ItemArchetype.Throwable: archHue = 0.35f; break;
            case ItemArchetype.Explosive: archHue = 0.0f; break;
            case ItemArchetype.Food: archHue = 0.22f; break;
            case ItemArchetype.Special: archHue = 0.78f; break;
        }
        float hue = Mathf.Repeat(archHue + index * (0.5f / Mathf.Max(1, catCount)), 1f);

        return new ItemDefinition
        {
            Name = name,
            Archetype = arch,
            Damage = damage,
            Uses = uses,
            ThrowDamage = throwDamage,
            ThrowSpeed = throwSpeed,
            Heal = heal,
            Cooldown = cooldown,
            Knockback = knockback,
            Scale = scale,
            Shape = shape,
            Color = Color.HSVToRGB(hue, arch == ItemArchetype.Explosive ? 0.35f : 0.6f, 0.95f)
        };
    }
}

// Builds the in-world visual for an item definition (ground, held, or thrown).
public static class ItemVisual
{
    public static GameObject Build(ItemDefinition def, string goName)
    {
        PrimitiveType prim;
        switch (def.Shape)
        {
            case ItemShape.Sphere: prim = PrimitiveType.Sphere; break;
            case ItemShape.Cylinder: prim = PrimitiveType.Cylinder; break;
            case ItemShape.Capsule: prim = PrimitiveType.Capsule; break;
            default: prim = PrimitiveType.Cube; break;
        }
        var go = GameObject.CreatePrimitive(prim);
        go.name = goName;
        var rend = go.GetComponent<Renderer>();
        if (rend != null) rend.material.color = def.Color;

        float s = def.Scale;
        switch (def.Archetype)
        {
            case ItemArchetype.Melee:
                go.transform.localScale = new Vector3(0.18f * s + 0.08f, 1.0f * s, 0.18f * s + 0.08f);
                break;
            case ItemArchetype.Gun:
                go.transform.localScale = new Vector3(0.28f, 0.28f, 0.85f * s + 0.2f);
                break;
            case ItemArchetype.Special:
                go.transform.localScale = new Vector3(0.35f, 0.8f * s, 0.35f);
                break;
            default: // Throwable / Explosive / Food: chunky, readable, catchable size
                go.transform.localScale = Vector3.one * (0.55f * s + 0.25f);
                break;
        }
        return go;
    }

    public static void StripCollider(GameObject go)
    {
        var col = go.GetComponent<Collider>();
        if (col != null) Object.Destroy(col);
    }
}
