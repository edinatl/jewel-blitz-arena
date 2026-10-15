using UnityEngine;

// Movement, per-player keyboard controls, jump, and a simple solo-play AI.
// Player 2 ships with AI ON so Ben can play alone immediately — pressing any
// Player 2 key takes manual control for the rest of the match.
public class PlayerController : MonoBehaviour
{
    [Header("Identity")]
    public string displayName = "Player";
    public int playerIndex = 0;

    [Header("Keys")]
    public KeyCode upKey = KeyCode.W;
    public KeyCode downKey = KeyCode.S;
    public KeyCode leftKey = KeyCode.A;
    public KeyCode rightKey = KeyCode.D;
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode attackKey = KeyCode.F;
    public KeyCode heavyKey = KeyCode.G;
    public KeyCode fusionKey = KeyCode.H;

    [Header("Movement")]
    public float moveSpeed = 6f;
    public float jumpForce = 8f;
    public float turnSpeed = 14f;

    [Header("AI (Player 2 solo mode)")]
    public bool aiEnabled = false;
    private bool aiTakenOver = false;
    private float aiAttackTimer = 0f;
    private float aiJumpTimer = 0f;
    private float aiCatchCooldown = 0f;

    private Rigidbody rb;
    private Health health;
    private PlayerCombat combat;
    private Vector3 moveInput;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        health = GetComponent<Health>();
        combat = GetComponent<PlayerCombat>();
    }

    void Update()
    {
        if (health != null && health.IsKO)
        {
            moveInput = Vector3.zero;
            return;
        }
        if (combat != null && combat.IsKnockedDown)
        {
            moveInput = Vector3.zero;
            return;
        }

        // Human takeover: any P2 key press disables AI for this match.
        if (aiEnabled && !aiTakenOver && playerIndex == 1)
        {
            if (Input.GetKeyDown(upKey) || Input.GetKeyDown(downKey) ||
                Input.GetKeyDown(leftKey) || Input.GetKeyDown(rightKey) ||
                Input.GetKeyDown(attackKey) || Input.GetKeyDown(heavyKey) ||
                Input.GetKeyDown(fusionKey) || Input.GetKeyDown(jumpKey))
            {
                aiTakenOver = true;
                aiEnabled = false;
            }
        }

        if (aiEnabled && !aiTakenOver)
        {
            UpdateAI();
            return;
        }

        float x = 0f, z = 0f;
        if (Input.GetKey(upKey)) z += 1f;
        if (Input.GetKey(downKey)) z -= 1f;
        if (Input.GetKey(leftKey)) x -= 1f;
        if (Input.GetKey(rightKey)) x += 1f;
        moveInput = new Vector3(x, 0f, z);

        if (Input.GetKeyDown(jumpKey)) HandleJumpKey();
        if (Input.GetKeyDown(fusionKey) && combat != null) combat.TryFusion();
        if (Input.GetKeyDown(attackKey) && combat != null)
        {
            // Empty hands + light attack = CATCH window (also swings, harmless).
            // Time it as a thrown item reaches you and it's yours.
            if (!combat.HasItem) combat.StartCatchWindow();
            combat.TryLightAttack();
        }
        if (Input.GetKeyDown(heavyKey) && combat != null) combat.TryHeavyAttack();
    }

    void FixedUpdate()
    {
        if (rb == null) return;
        if (health != null && health.IsKO) return;

        if (rollTimeLeft > 0f)
        {
            rollTimeLeft -= Time.fixedDeltaTime;
            Vector3 rv = rb.velocity;
            rb.velocity = new Vector3(rollDir.x * 10.5f, rv.y, rollDir.z * 10.5f);
            FaceDirection(rollDir);
            return;
        }
        if (IsGrounded()) wallJumped = false;

        float speed = moveSpeed;
        if (combat != null && combat.IsTransformed) speed *= 1.25f;

        Vector3 vel = rb.velocity;
        Vector3 desired = moveInput.normalized * speed;
        if (moveInput.sqrMagnitude > 0.01f)
        {
            rb.velocity = new Vector3(desired.x, vel.y, desired.z);
            FaceDirection(moveInput);
        }
        else
        {
            rb.velocity = new Vector3(0f, vel.y, 0f);
            FaceOpponent();
        }
    }

    void FaceDirection(Vector3 dir)
    {
        Quaternion target = Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.z), Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, turnSpeed * Time.deltaTime);
    }

    void FaceOpponent()
    {
        GameObject opp = FindOpponent();
        if (opp == null) return;
        Vector3 to = opp.transform.position - transform.position;
        to.y = 0f;
        if (to.sqrMagnitude < 0.04f) return;
        Quaternion target = Quaternion.LookRotation(to.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, turnSpeed * 0.5f * Time.deltaTime);
    }

    GameObject FindOpponent()
    {
        var players = GameObject.FindGameObjectsWithTag("Player");
        for (int i = 0; i < players.Length; i++)
            if (players[i] != gameObject) return players[i];
        return null;
    }

    bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, 1.25f);
    }

    void Jump()
    {
        if (rb == null) return;
        Vector3 v = rb.velocity;
        rb.velocity = new Vector3(v.x, jumpForce, v.z);
    }

    // CROSS button (v0.5): grounded + moving = DODGE ROLL (invincible for
    // 0.38s), grounded + standing = jump, airborne near a wall = WALL JUMP
    // back toward the arena center (once per airtime).
    private float rollTimeLeft = 0f;
    private Vector3 rollDir = Vector3.forward;
    private bool wallJumped = false;

    void HandleJumpKey()
    {
        if (IsGrounded())
        {
            wallJumped = false;
            if (moveInput.sqrMagnitude > 0.05f) StartRoll(moveInput);
            else Jump();
        }
        else if (!wallJumped)
        {
            Vector3 p = transform.position;
            if (Mathf.Abs(p.x) > 7.2f || Mathf.Abs(p.z) > 7.2f)
            {
                wallJumped = true;
                Vector3 toCenter = new Vector3(-p.x, 0f, -p.z).normalized;
                if (rb != null)
                    rb.velocity = new Vector3(toCenter.x * 5f, jumpForce * 0.9f, toCenter.z * 5f);
            }
        }
    }

    public void StartRoll(Vector3 dir)
    {
        if (combat != null) combat.StartRoll();
        rollTimeLeft = PlayerCombat.RollDuration;
        dir.y = 0f;
        rollDir = dir.sqrMagnitude > 0.01f ? dir.normalized : transform.forward;
    }

    // --- Very simple bot: chase gems, otherwise chase + hit the opponent ------

    void UpdateAI()
    {
        if (combat != null && combat.IsKnockedDown) { moveInput = Vector3.zero; return; }
        aiAttackTimer -= Time.deltaTime;
        aiJumpTimer -= Time.deltaTime;
        aiCatchCooldown -= Time.deltaTime;

        // CATCH: a thrown item is coming at us and our hands are empty —
        // face it and open the catch window (bot reflexes: 75% of the time).
        if (combat != null && !combat.HasItem && aiCatchCooldown <= 0f)
        {
            var thrown = FindObjectsOfType<ThrownItem>();
            for (int i = 0; i < thrown.Length; i++)
            {
                if (thrown[i].Thrower == gameObject) continue;
                Vector3 toItem = thrown[i].transform.position - transform.position;
                if (toItem.magnitude > 3.2f || toItem.magnitude < 0.6f) continue;
                float closing = Vector3.Dot(thrown[i].Velocity.normalized, -toItem.normalized);
                if (closing > 0.5f)
                {
                    aiCatchCooldown = 1f;
                    FaceDirection(-thrown[i].Velocity);
                    float roll = Random.value;
                    if (roll < 0.65f) combat.StartCatchWindow();
                    else if (roll < 0.85f)
                        StartRoll(new Vector3(-thrown[i].Velocity.x, 0f, -thrown[i].Velocity.z));
                    break;
                }
            }
        }

        GameObject target = null;
        float targetStopDist = 1.7f;

        // Prefer a nearby gem if we don't have 3 yet.
        var gems = FindObjectsOfType<PowerGem>();
        float bestGemDist = 6f;
        for (int i = 0; i < gems.Length; i++)
        {
            float d = Vector3.Distance(transform.position, gems[i].transform.position);
            if (d < bestGemDist) { bestGemDist = d; target = gems[i].gameObject; targetStopDist = 0.8f; }
        }

        // No item in hand? Grab a nearby one — items win fights.
        if (target == null && combat != null && !combat.HasItem)
        {
            var items = FindObjectsOfType<ItemPickup>();
            float bestItemDist = 7f;
            for (int i = 0; i < items.Length; i++)
            {
                float d = Vector3.Distance(transform.position, items[i].transform.position);
                if (d < bestItemDist) { bestItemDist = d; target = items[i].gameObject; targetStopDist = 0.8f; }
            }
        }

        if (target == null) target = FindOpponent();
        if (target == null) { moveInput = Vector3.zero; return; }

        Vector3 to = target.transform.position - transform.position;
        to.y = 0f;
        float dist = to.magnitude;

        moveInput = dist > targetStopDist ? to.normalized : Vector3.zero;

        // Transformed with gauge to burn: let the Fusion fly sometimes.
        if (combat != null && combat.IsTransformed && combat.PowerGauge >= 50f &&
            aiAttackTimer <= 0f && Random.value < 0.3f)
        {
            aiAttackTimer = 1.4f;
            combat.TryFusion();
            return;
        }

        // Ranged behavior: holding a throwable/explosive/gun at distance.
        if (combat != null && combat.HasItem && aiAttackTimer <= 0f)
        {
            var opp = FindOpponent();
            if (opp != null)
            {
                float oppDist = Vector3.Distance(transform.position, opp.transform.position);
                var def = combat.HeldItem;
                if (def != null && def.IsThrowFocused && oppDist > 2.5f && oppDist < 10f)
                {
                    FaceDirection(opp.transform.position - transform.position);
                    aiAttackTimer = 1.1f;
                    combat.TryHeavyAttack(); // heavy = throw it at them
                    return;
                }
                if (def != null && def.Archetype == ItemArchetype.Gun && oppDist < 12f)
                {
                    FaceDirection(opp.transform.position - transform.position);
                    aiAttackTimer = 0.8f;
                    combat.TryLightAttack(); // shoot
                    return;
                }
            }
        }

        if (dist <= 1.9f && target.CompareTag("Player"))
        {
            FaceDirection(to);
            if (aiAttackTimer <= 0f && combat != null)
            {
                aiAttackTimer = 0.55f;
                // Holding a throwable point-blank: lob it anyway sometimes,
                // otherwise swing/shoot/eat with light.
                if (combat.HasItem && combat.HeldItem != null && combat.HeldItem.IsThrowFocused && Random.value < 0.4f)
                    combat.TryHeavyAttack();
                else if (Random.value < 0.25f && !combat.HasItem)
                    combat.TryHeavyAttack();
                else
                    combat.TryLightAttack();
            }
            if (aiJumpTimer <= 0f)
            {
                aiJumpTimer = Random.Range(2f, 4f);
                if (Random.value < 0.35f && IsGrounded()) Jump();
            }
        }
    }
}
