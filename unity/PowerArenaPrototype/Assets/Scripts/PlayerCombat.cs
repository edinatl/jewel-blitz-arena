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
    public int gold = 0;
    public bool IsTransformed { get; private set; }
    // v0.5: transformation runs on a Power Gauge (0-100) that drains over
    // ~transformDuration seconds. Power Drive costs 25, Power Fusion 50.
    public float PowerGauge { get; private set; }
    public float TransformTimeLeft { get { return PowerGauge * transformDuration / 100f; } }
    public ItemDefinition HeldItem { get; private set; }
    public bool HasItem { get { return HeldItem != null; } }
    public string HeldItemName { get { return HeldItem != null ? HeldItem.Name : ""; } }
    public float LastCatchTime { get; private set; }
    public float LastAttackTime { get; private set; }  // FighterAnimator reads these
    public float LastHitTime { get; private set; }

    public float transformDuration = 12f;
    public int gemsToTransform = 3;

    private int itemUsesLeft = 0;
    private int busterShots = 0;
    public string LastWeaponName { get; private set; }
    public float SlowUntil { get; private set; }
    public bool IsSlowed { get { return Time.time < SlowUntil; } }
    public void ApplySlow(float seconds) { SlowUntil = Time.time + seconds; }
    private GameObject heldVisual;
    private float nextAttackTime = 0f;
    private float catchWindowUntil = -1f;

    // v0.5 mechanics: combo knockdown, dodge roll, per-character Power Drive.
    private int hitStreak = 0;
    private float lastStreakTime = -10f;
    private float knockdownUntil = -1f;
    private float rollUntil = -1f;
    private string characterName = "Soldier";
    public const float RollDuration = 0.38f;
    public float RollStartTime { get; private set; }
    public bool IsRolling { get { return Time.time < rollUntil; } }
    public bool IsKnockedDown { get { return Time.time < knockdownUntil; } }

    // v0.6: opponent grabs. Heavy (the Action button) with empty hands
    // next to a foe grabs them overhead; any attack input then throws
    // them. The victim mashes action buttons to escape early.
    public PlayerCombat Grabbing { get; private set; }
    public PlayerCombat GrabbedBy { get; private set; }
    public float thrownFoeUntil = -1f;
    private float grabUntil = -1f;
    private float nextGrabTime = 0f;
    private int mashCount = 0;
    public bool IsGrabbed { get { return GrabbedBy != null; } }

    public void SetHeldKinematic(bool held)
    {
        if (rb != null) rb.isKinematic = held;
    }

    bool TryGrab()
    {
        if (Grabbing != null || GrabbedBy != null || IsTransformed || Time.time < nextGrabTime) return false;
        GameObject opp = FindOpponent();
        if (opp == null) return false;
        Vector3 to = opp.transform.position - transform.position; to.y = 0f;
        if (to.magnitude > 1.9f || to.magnitude < 0.01f) return false;
        if (Vector3.Dot(transform.forward, to.normalized) < 0.2f) return false;
        var oc = opp.GetComponent<PlayerCombat>();
        var oh = opp.GetComponent<Health>();
        if (oc == null || oh == null || oh.IsKO) return false;
        if (oc.IsTransformed || oc.IsRolling || oc.IsKnockedDown || oc.GrabbedBy != null) return false;
        Grabbing = oc;
        oc.GrabbedBy = this;
        oc.mashCount = 0;
        oc.SetHeldKinematic(true);
        oc.thrownFoeUntil = -1f;
        grabUntil = Time.time + 1.1f;
        nextAttackTime = Time.time + 0.3f;
        LastAttackTime = Time.time;
        return true;
    }

    public void ThrowGrabbed()
    {
        if (Grabbing == null) return;
        var v = Grabbing;
        Grabbing = null;
        v.GrabbedBy = null;
        v.SetHeldKinematic(false);
        nextGrabTime = Time.time + 0.8f;
        nextAttackTime = Time.time + 0.4f;
        LastAttackTime = Time.time;
        v.thrownFoeUntil = Time.time + 0.55f;
        var vrb = v.GetComponent<Rigidbody>();
        if (vrb != null) vrb.velocity = transform.forward * 11f + Vector3.up * 3.5f;
    }

    public void BreakGrab()
    {
        if (Grabbing == null) return;
        var v = Grabbing;
        Grabbing = null;
        v.GrabbedBy = null;
        v.SetHeldKinematic(false);
        nextGrabTime = Time.time + 0.8f;
    }

    public void EscapeGrab()
    {
        if (GrabbedBy == null) return;
        var g = GrabbedBy;
        GrabbedBy = null;
        g.Grabbing = null;
        SetHeldKinematic(false);
        g.nextGrabTime = Time.time + 0.8f;
        if (rb != null) rb.velocity = -g.transform.forward * 4f + Vector3.up * 3f;
    }

    public void RegisterMash()
    {
        if (GrabbedBy == null) return;
        mashCount++;
        if (mashCount >= 4) EscapeGrab();
    }

    void TakeThrownLanding()
    {
        if (health != null) health.TakeDamage(12f);
        if (gems > 0 && !IsTransformed) DropOneGem();
        knockdownUntil = Time.time + 0.45f;
        LastHitTime = Time.time;
    }

    // Recovery roll: jump pressed late in a knockdown stands you
    // straight into the roll instead of serving the full 0.85s.
    public bool TryRecoveryRoll()
    {
        if (!IsKnockedDown) return false;
        if (knockdownUntil - Time.time > 0.42f) return false;
        knockdownUntil = -1f;
        hitStreak = 0;
        return true;
    }

    public void StartRoll()
    {
        rollUntil = Time.time + RollDuration;
        RollStartTime = Time.time;
    }

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
        var vis = GetComponent<CharacterVisual>();
        if (vis != null) characterName = vis.character;
        RollStartTime = -10f;
        LastCatchTime = -10f;
        LastAttackTime = -10f;
        LastHitTime = -10f;
    }

    void Update()
    {
        if (Grabbing != null)
        {
            if (health.IsKO) BreakGrab();
            else
            {
                Grabbing.transform.position = transform.position + Vector3.up * 2.15f + transform.forward * 0.35f;
                if (Time.time >= grabUntil) ThrowGrabbed();
            }
        }
        if (thrownFoeUntil > 0f && Time.time >= thrownFoeUntil)
        {
            thrownFoeUntil = -1f;
            if (!health.IsKO) TakeThrownLanding();
        }
        if (IsTransformed)
        {
            PowerGauge -= Time.deltaTime * (100f / transformDuration);
            if (PowerGauge <= 0f) EndTransform();
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
        if (health.IsKO || IsKnockedDown || Time.time < nextAttackTime) return;
        if (GrabbedBy != null) return;
        if (Grabbing != null) { ThrowGrabbed(); return; }
        LastAttackTime = Time.time;

        // Transformed: light attack is your POWER DRIVE (costs 25 gauge).
        if (IsTransformed) { PowerDrive(); return; }

        if (HasItem) { UseItem(); return; }

        // Air attack: a diving strike while airborne.
        if (!IsGrounded())
        {
            nextAttackTime = Time.time + 0.35f;
            if (rb != null)
            {
                Vector3 v = rb.velocity;
                rb.velocity = new Vector3(v.x + transform.forward.x * 3.5f, v.y, v.z + transform.forward.z * 3.5f);
            }
            MeleeHit(10f, attackRange + 0.2f, 8f);
            return;
        }

        nextAttackTime = Time.time + lightCooldown;
        MeleeHit(lightDamage, attackRange, 6f);
    }

    bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, 1.25f);
    }

    // Power Drive — your transformed special, per character. Costs 25 gauge.
    void PowerDrive()
    {
        nextAttackTime = Time.time + 0.55f;
        PowerGauge = Mathf.Max(0f, PowerGauge - 25f);
        Vector3 origin = transform.position + Vector3.up * 1.1f + transform.forward * 0.9f;
        if (characterName == "Samurai")
        {
            // Blade Wave: one big, fast slash projectile.
            Projectile.Fire(gameObject, origin, transform.forward, 17f, 22f, 0.55f,
                            new Color(0.66f, 0.9f, 1f), false, 0f);
        }
        else
        {
            // Rocket Salvo (Soldier / default): a fan of 3 explosive shells.
            for (int i = -1; i <= 1; i++)
            {
                Vector3 dir = Quaternion.Euler(0f, i * 10f, 0f) * transform.forward;
                Projectile.Fire(gameObject, origin, dir, 15f, 13f, 0.32f,
                                new Color(1f, 0.63f, 0.15f), true, 0f);
            }
        }
        if (PowerGauge <= 0f) EndTransform();
    }

    // TRIANGLE button. Transformed: POWER FUSION (50 gauge) — three
    // telegraphed explosions rain on the opponent's position. Not
    // transformed: DISCARD your held item onto the ground instead.
    public void TryFusion()
    {
        if (health.IsKO || IsKnockedDown) return;
        if (GrabbedBy != null) return;
        if (Grabbing != null) { ThrowGrabbed(); return; }
        if (IsTransformed)
        {
            if (PowerGauge < 50f || Time.time < nextAttackTime) return;
            PowerGauge -= 50f;
            nextAttackTime = Time.time + 0.9f;
            LastAttackTime = Time.time;
            GameObject opp = FindOpponent();
            if (opp != null) StartCoroutine(FusionRoutine(opp.transform.position));
            if (PowerGauge <= 0f) EndTransform();
            return;
        }
        if (HasItem)
        {
            ItemDefinition def = HeldItem;
            ClearItem();
            Vector3 drop = transform.position + transform.forward * 1.1f;
            drop.y = 0.9f;
            ItemPickup.Spawn(drop, def);
        }
    }

    GameObject FindOpponent()
    {
        var players = GameObject.FindGameObjectsWithTag("Player");
        for (int i = 0; i < players.Length; i++)
            if (players[i] != gameObject) return players[i];
        return null;
    }

    System.Collections.IEnumerator FusionRoutine(Vector3 targetPos)
    {
        for (int i = 0; i < 3; i++)
        {
            yield return new WaitForSeconds(i == 0 ? 0.35f : 0.22f);
            Vector3 at = targetPos + new Vector3(Random.Range(-1.3f, 1.3f), 0f, Random.Range(-1.3f, 1.3f));
            at.y = 1.0f;
            ExplodeAt(at, 16f, 2.4f);
        }
    }

    // Shared explosion: damages everyone in radius except the owner;
    // rolling and knocked-down fighters come through untouched.
    void ExplodeAt(Vector3 at, float dmg, float radius)
    {
        var all = FindObjectsOfType<Health>();
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].gameObject == gameObject) continue;
            var oc = all[i].GetComponent<PlayerCombat>();
            if (oc != null && (oc.IsRolling || oc.IsKnockedDown)) continue;
            float dist = Vector3.Distance(at, all[i].transform.position);
            if (dist <= radius)
            {
                all[i].TakeDamage(dmg);
                if (oc != null)
                {
                    Vector3 dir = (all[i].transform.position - at).normalized;
                    dir.y = 0.4f;
                    oc.ApplyKnockback(dir, 10f);
                    oc.RegisterHitTaken(dmg);
                }
            }
        }
        var flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        flash.transform.position = at;
        flash.transform.localScale = Vector3.one * radius * 2f;
        ItemVisual.StripCollider(flash);
        var rend = flash.GetComponent<Renderer>();
        if (rend != null) rend.material.color = new Color(1f, 0.55f, 0.1f);
        Destroy(flash, 0.15f);
    }

    public void TryHeavyAttack()
    {
        if (health.IsKO || IsKnockedDown || Time.time < nextAttackTime) return;
        if (GrabbedBy != null) return;
        if (Grabbing != null) { ThrowGrabbed(); return; }
        LastAttackTime = Time.time;

        // Holding anything? Heavy = THROW it. This is the heart of the game.
        if (HasItem)
        {
            // CIRCLE: throwables THROW in the facing direction; anything
            // else DROPS at your feet (Ben's call). (SQUARE still throws
            // throwables aimed at the closest foe.)
            if (HeldItem.Archetype == ItemArchetype.Throwable || HeldItem.Archetype == ItemArchetype.Explosive)
            {
                nextAttackTime = Time.time + 0.45f;
                ThrowHeldItem();
            }
            else
            {
                nextAttackTime = Time.time + 0.3f;
                DropHeldItem();
            }
            return;
        }

        // Empty hands + heavy: PICK UP an item at your feet first,
        // otherwise GRAB a foe next to you (the PS2 Action moves).
        if (TryPickupNearest()) { nextAttackTime = Time.time + 0.25f; return; }
        if (TryGrab()) return;
        StartCatchWindow(); // CIRCLE is the catch button too (Ben's call)

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
            // A dodge roll beats any melee swing; a knocked-down fighter is safe.
            if (otherCombat != null && (otherCombat.IsRolling || otherCombat.IsKnockedDown)) continue;
            if (otherHealth != null) otherHealth.TakeDamage(damage);
            if (otherCombat != null)
            {
                Vector3 dir = flat.normalized; dir.y = 0.35f;
                otherCombat.ApplyKnockback(dir, knockback);
                otherCombat.RegisterHitTaken(damage);
                // UNIQUE item twists on a landed swing:
                if (HeldItem != null && HeldItem.Name == "Golden Club")
                    GoldCoin.Spawn(players[i].transform.position + new Vector3(0f, 0.6f, 0f));
                if (HeldItem != null && HeldItem.Name == "Spring Punch Glove")
                    otherCombat.ApplyKnockback(dir, 16f); // BOING: huge extra launch
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
        // Dodge roll = clean miss. Knocked down = can't be juggled.
        if (IsRolling || IsKnockedDown) return;
        LastHitTime = Time.time;
        if (gems > 0 && !IsTransformed) DropOneGem();

        // Combo rule: the 3rd hit inside 1.2s is a KNOCKDOWN — you hit the
        // floor for 0.85s and lose another gem, or spill gold coins if
        // you're carrying none.
        if (!IsTransformed)
        {
            if (Time.time - lastStreakTime < 1.2f) hitStreak++; else hitStreak = 1;
            lastStreakTime = Time.time;
            if (hitStreak >= 3)
            {
                hitStreak = 0;
                knockdownUntil = Time.time + 0.85f;
                if (GrabbedBy != null) GrabbedBy.BreakGrab();
                if (gems > 0) DropOneGem();
                else for (int i = 0; i < 3; i++)
                    GoldCoin.Spawn(transform.position + new Vector3(Random.Range(-0.7f, 0.7f), 0f, Random.Range(-0.7f, 0.7f)));
            }
        }
    }

    void DropOneGem()
    {
        gems--;
        Vector2 rand = Random.insideUnitCircle.normalized * 1.9f;
        Vector3 dropPos = transform.position + new Vector3(rand.x, 0f, rand.y);
        dropPos.x = Mathf.Clamp(dropPos.x, -8f, 8f);
        dropPos.z = Mathf.Clamp(dropPos.z, -8f, 8f);
        dropPos.y = 1.0f;
        PowerGem.Spawn(dropPos);
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
        PowerGauge = 100f;
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
        PowerGauge = 0f;
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
        LastWeaponName = def.Name;
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
                if (def.Name == "Flame Thrower")
                {
                    // UNIQUE: a cone of three flame jets.
                    for (int k = -1; k <= 1; k++)
                    {
                        Vector3 dir = Quaternion.Euler(0f, k * 9f, 0f) * transform.forward;
                        Projectile.Fire(gameObject, origin, dir, 12f, 5f, 0.34f,
                                        new Color(1f, 0.48f, 0.16f), false, 0f);
                    }
                }
                else if (def.Name == "Mega Buster")
                {
                    // UNIQUE: rapid pellets; every 4th shot is a charged blast.
                    busterShots++;
                    bool charged = busterShots % 4 == 0;
                    Projectile.Fire(gameObject, origin, transform.forward, 24f,
                                    charged ? 18f : 6f, charged ? 0.5f : 0.2f,
                                    charged ? new Color(0.62f, 0.91f, 1f) : new Color(1f, 0.88f, 0.3f),
                                    charged, 0f);
                }
                else
                {
                    bool explosiveShot = def.Name == "Rocket Launcher" || def.Name == "Grenade Launcher";
                    Projectile.Fire(gameObject, origin, transform.forward, 20f, def.Damage, 0.28f,
                                    def.Color, explosiveShot, 0f);
                }
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
                // SQUARE with a throwable: the throw AIMS at the closest
                // opponent. (HEAVY throws manually, in the direction the
                // character is facing.)
                nextAttackTime = Time.time + 0.45f;
                ThrowHeldItem(AimedThrowDirection());
                return; // ThrowHeldItem already cleared the item
        }

        if (def.Archetype != ItemArchetype.Throwable && def.Archetype != ItemArchetype.Explosive)
        {
            itemUsesLeft--;
            if (itemUsesLeft <= 0) ClearItem();
        }
    }

    // Manual pickup (v0.7): the Action button picks up the nearest item
    // within reach. Returns false when there's nothing to grab.
    public bool TryPickupNearest()
    {
        if (HasItem) return false;
        var pickups = FindObjectsOfType<ItemPickup>();
        ItemPickup best = null;
        float bd = 2.2f;
        for (int i = 0; i < pickups.Length; i++)
        {
            Vector3 to = pickups[i].transform.position - transform.position;
            to.y = 0f;
            if (to.magnitude < bd) { bd = to.magnitude; best = pickups[i]; }
        }
        if (best == null) return false;
        best.PickupBy(this);
        return true;
    }

    Vector3 AimedThrowDirection()
    {
        GameObject opp = FindOpponent();
        if (opp == null) return transform.forward;
        Vector3 dir = opp.transform.position - transform.position;
        dir.y = 0f;
        return dir.sqrMagnitude > 0.01f ? dir.normalized : transform.forward;
    }

    // Drop the held item at your feet as a ground pickup.
    public void DropHeldItem()
    {
        if (!HasItem) return;
        ItemDefinition def = HeldItem;
        ClearItem();
        Vector3 spot = transform.position + transform.forward * 0.9f;
        spot.y = 0.9f;
        ItemPickup.Spawn(spot, def);
    }

    public void ThrowHeldItem()
    {
        ThrowHeldItem(transform.forward);
    }

    public void ThrowHeldItem(Vector3 direction)
    {
        if (!HasItem) return;
        ItemDefinition def = HeldItem;
        Vector3 origin = transform.position + Vector3.up * 1.3f + transform.forward * 0.7f;
        ClearItem();
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f) direction = transform.forward;
        ThrownItem.Throw(gameObject, def, origin, direction.normalized);
    }
}
