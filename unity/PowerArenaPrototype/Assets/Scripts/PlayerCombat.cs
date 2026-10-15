using UnityEngine;

// Combat, gems, transformation, held items, throwing, and catching.
// Rules in this build (Ben's calls, 2026-10-15):
//  - ANY hit on a fighter who is carrying gems knocks one gem loose.
//  - Heavy attack THROWS your held item. Light attack uses it.
//  - Pressing light attack with empty hands opens a short catch window:
//    a thrown item flying at you in that window (and you're facing it)
//    is caught, and you hold it instead of taking the hit.
public class PlayerCombat : MonoBehaviour
{
    [Header("Tuning")]
    public float lightDamage = 8f;
    public float heavyDamage = 14f;
    public float attackRange = 2.0f;
    public float lightCooldown = 0.35f;
    public float heavyCooldown = 0.8f;
    public float catchWindowSeconds = 0.28f;

    [Header("State (read-only in play)")]
    public int gems = 0;
    public bool IsTransformed { get; private set; }
    public float TransformTimeLeft { get; private set; }
    public ItemDefinition HeldItem { get; private set; }
    public bool HasItem { get { return HeldItem != null; } }
    public string HeldItemName { get { return HeldItem != null ? HeldItem.Name : ""; } }
    public float LastCatchTime { get; private set; }
    public float LastAttackTime { get; private set; }  // FighterAnimator reads these
    public float LastHitTime { get; private set; }

    public float transformDuration = 12f;
    public int gemsToTransform = 3;

    private int itemUsesLeft = 0;
    private GameObject heldVisual;
    private float nextAttackTime = 0f;
    private float catchWindowUntil = -1f;

    private Health health;
    private Rigidbody rb;
    private Color baseColor;
    private Renderer bodyRenderer;
    private Light transformLight;
    private Vector3 baseScale;

    public bool CatchWindowActive { get { return Time.time <= catchWindowUntil; } }

    void Awake()
    {
        health = GetComponent<Health>();
        rb = GetComponent<Rigidbody>();
        bodyRenderer = GetComponent<Renderer>();
        if (bodyRenderer != null) baseColor = bodyRenderer.material.color;
        baseScale = transform.localScale;
        LastCatchTime = -10f;
        LastAttackTime = -10f;
        LastHitTime = -10f;
    }

    void Update()
    {
        if (IsTransformed)
        {
            TransformTimeLeft -= Time.deltaTime;
            if (TransformTimeLeft <= 0f) EndTransform();
        }
    }

    // --- Catching ---------------------------------------------------------------

    public void StartCatchWindow()
    {
        catchWindowUntil = Time.time + catchWindowSeconds;
    }

    public void NotifyCaught()
    {
        LastCatchTime = Time.time;
    }

    // --- Attacks --------------------------------------------------------------------

    public void TryLightAttack()
    {
        if (health.IsKO || Time.time < nextAttackTime) return;
        LastAttackTime = Time.time;
        if (HasItem) { UseItem(); return; }
        nextAttackTime = Time.time + lightCooldown;
        MeleeHit(IsTransformed ? lightDamage * 2f : lightDamage,
                 IsTransformed ? attackRange + 0.6f : attackRange, 6f);
    }

    public void TryHeavyAttack()
    {
        if (health.IsKO || Time.time < nextAttackTime) return;
        LastAttackTime = Time.time;

        // Holding anything? Heavy = THROW it. This is the heart of the game.
        if (HasItem)
        {
            nextAttackTime = Time.time + 0.45f;
            ThrowHeldItem();
            return;
        }

        if (IsTransformed)
        {
            nextAttackTime = Time.time + 0.6f;
            Vector3 origin = transform.position + Vector3.up * 1.0f + transform.forward * 1.0f;
            Projectile.Fire(gameObject, origin, transform.forward, 16f, 20f, 0.6f,
                            new Color(1f, 0.8f, 0.2f), false, 0f);
            return;
        }

        nextAttackTime = Time.time + heavyCooldown;
        MeleeHit(heavyDamage, attackRange, 10f);
    }

    void MeleeHit(float damage, float range, float knockback)
    {
        var players = GameObject.FindGameObjectsWithTag("Player");
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] == gameObject) continue;
            Vector3 toOther = players[i].transform.position - transform.position;
            float dist = new Vector2(toOther.x, toOther.z).magnitude;
            if (dist > range) continue;
            Vector3 flat = toOther; flat.y = 0f;
            if (Vector3.Dot(transform.forward, flat.normalized) < 0.35f) continue;

            var otherHealth = players[i].GetComponent<Health>();
            var otherCombat = players[i].GetComponent<PlayerCombat>();
            if (otherHealth != null) otherHealth.TakeDamage(damage);
            if (otherCombat != null)
            {
                Vector3 dir = flat.normalized; dir.y = 0.35f;
                otherCombat.ApplyKnockback(dir, knockback);
                otherCombat.RegisterHitTaken(damage);
            }
        }
    }

    public void ApplyKnockback(Vector3 direction, float force)
    {
        if (rb == null) return;
        Vector3 v = rb.velocity;
        rb.velocity = new Vector3(direction.x * force, Mathf.Max(v.y, direction.y * force), direction.z * force);
    }

    // ANY hit knocks a gem loose (Ben's rule) — no damage threshold.
    // The gem flings out away from the victim so the dropper can't
    // instantly re-grab it, and gems have a pickup delay (see PowerGem).
    public void RegisterHitTaken(float damage)
    {
        LastHitTime = Time.time;
        if (gems > 0 && !IsTransformed)
        {
            gems--;
            Vector2 rand = Random.insideUnitCircle.normalized * 1.9f;
            Vector3 dropPos = transform.position + new Vector3(rand.x, 0f, rand.y);
            dropPos.x = Mathf.Clamp(dropPos.x, -8f, 8f);
            dropPos.z = Mathf.Clamp(dropPos.z, -8f, 8f);
            dropPos.y = 1.0f;
            PowerGem.Spawn(dropPos);
        }
    }

    // --- Gems + transformation ---------------------------------------------------

    public void AddGem()
    {
        if (IsTransformed) return;
        gems++;
        if (gems >= gemsToTransform) StartTransform();
    }

    void StartTransform()
    {
        gems = 0;
        IsTransformed = true;
        TransformTimeLeft = transformDuration;
        transform.localScale = baseScale * 1.3f;
        if (bodyRenderer != null) bodyRenderer.material.color = new Color(1f, 0.8f, 0.15f);
        transformLight = gameObject.AddComponent<Light>();
        transformLight.type = LightType.Point;
        transformLight.color = new Color(1f, 0.8f, 0.2f);
        transformLight.intensity = 3f;
        transformLight.range = 5f;
    }

    void EndTransform()
    {
        IsTransformed = false;
        TransformTimeLeft = 0f;
        transform.localScale = baseScale;
        if (bodyRenderer != null) bodyRenderer.material.color = baseColor;
        if (transformLight != null) Destroy(transformLight);
    }

    // --- Items: hold, use, throw ---------------------------------------------------

    public void EquipItem(ItemDefinition def)
    {
        HeldItem = def;
        itemUsesLeft = def.Uses;

        if (heldVisual != null) Destroy(heldVisual);
        heldVisual = ItemVisual.Build(def, "Held_" + def.Name);
        ItemVisual.StripCollider(heldVisual);
        heldVisual.transform.SetParent(transform);
        heldVisual.transform.localPosition = new Vector3(0.55f, 1.35f, 0.35f);
        heldVisual.transform.localRotation = Quaternion.Euler(0f, 0f, 25f);
    }

    void ClearItem()
    {
        HeldItem = null;
        itemUsesLeft = 0;
        if (heldVisual != null) Destroy(heldVisual);
        heldVisual = null;
    }

    void UseItem()
    {
        ItemDefinition def = HeldItem;
        Vector3 origin = transform.position + Vector3.up * 1.1f + transform.forward * 0.8f;

        switch (def.Archetype)
        {
            case ItemArchetype.Melee:
            case ItemArchetype.Special:
                nextAttackTime = Time.time + def.Cooldown;
                MeleeHit(def.Damage, attackRange + 0.3f, def.Knockback);
                break;

            case ItemArchetype.Gun:
                nextAttackTime = Time.time + def.Cooldown;
                bool explosiveShot = def.Name == "Rocket Launcher" || def.Name == "Grenade Launcher";
                Projectile.Fire(gameObject, origin, transform.forward, 20f, def.Damage, 0.28f,
                                def.Color, explosiveShot, 0f);
                break;

            case ItemArchetype.Food:
                // Eat it: heal, item consumed.
                nextAttackTime = Time.time + def.Cooldown;
                if (health != null)
                    health.currentHealth = Mathf.Min(health.maxHealth, health.currentHealth + def.Heal);
                itemUsesLeft = 0;
                break;

            case ItemArchetype.Throwable:
            case ItemArchetype.Explosive:
                // A throw-focused item in hand: light attack lobs it too
                // (heavy throws everything — same result here).
                nextAttackTime = Time.time + 0.45f;
                ThrowHeldItem();
                return; // ThrowHeldItem already cleared the item
        }

        if (def.Archetype != ItemArchetype.Throwable && def.Archetype != ItemArchetype.Explosive)
        {
            itemUsesLeft--;
            if (itemUsesLeft <= 0) ClearItem();
        }
    }

    public void ThrowHeldItem()
    {
        if (!HasItem) return;
        ItemDefinition def = HeldItem;
        Vector3 origin = transform.position + Vector3.up * 1.3f + transform.forward * 0.7f;
        ClearItem();
        ThrownItem.Throw(gameObject, def, origin, transform.forward);
    }
}
