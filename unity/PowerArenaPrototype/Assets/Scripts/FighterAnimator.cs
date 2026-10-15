using UnityEngine;

// Procedural animation for the primitive-built fighters — no animation clips
// or rigged models needed. Reads the fighter's live state (velocity, ground,
// attacks, catch window, hits taken, transform, KO) and poses the
// CharacterVisual's limb pivots every frame, Power Stone style:
//   idle breathing + bounce, run cycle with swinging arms/legs, jump tuck,
//   punch / weapon swing on attack, both-arms-up catch pose, hurt flinch,
//   transform aura pulse, and a fall-over KO.
public class FighterAnimator : MonoBehaviour
{
    private Rigidbody rb;
    private Health health;
    private PlayerCombat combat;
    private CharacterVisual visual;

    private float runPhase;
    private float koBlend;
    private float flinchBlend;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        health = GetComponent<Health>();
        combat = GetComponent<PlayerCombat>();
        visual = GetComponentInChildren<CharacterVisual>();
    }

    bool Grounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, 1.35f);
    }

    void LateUpdate()
    {
        if (visual == null || visual.Body == null) return;
        float dt = Time.deltaTime;
        float t = Time.time;

        // --- KO: tip the whole figure over and stay down -------------------
        if (health != null && health.IsKO)
        {
            koBlend = Mathf.Min(1f, koBlend + dt * 2.2f);
            float fall = Mathf.Sin(koBlend * Mathf.PI * 0.5f);
            visual.Body.localRotation = Quaternion.Euler(-88f * fall, 0f, 0f);
            visual.Body.localPosition = new Vector3(0f, -1.02f + 0.55f * fall, -0.35f * fall);
            visual.ArmL.localRotation = Quaternion.Euler(-0.5f * fall, 0f, 0.7f * fall);
            visual.ArmR.localRotation = Quaternion.Euler(-0.5f * fall, 0f, -0.7f * fall);
            return;
        }
        koBlend = 0f;

        Vector3 flatVel = rb != null ? new Vector3(rb.velocity.x, 0f, rb.velocity.z) : Vector3.zero;
        float speed = flatVel.magnitude;
        float speedF = Mathf.Clamp01(speed / 6f);
        bool grounded = Grounded();

        // --- locomotion phase ----------------------------------------------
        if (grounded && speed > 0.4f) runPhase += dt * (4f + speed * 1.6f);
        float swing = Mathf.Sin(runPhase) * 0.75f * speedF;
        float swingOpp = Mathf.Sin(runPhase + Mathf.PI) * 0.75f * speedF;

        float armLx = swingOpp, armRx = swing;
        float legLx = swing, legRx = swingOpp;

        // Idle breathing when standing still.
        if (speedF < 0.08f)
        {
            float breathe = Mathf.Sin(t * 2.2f) * 0.05f;
            armLx = breathe; armRx = -breathe;
            legLx = 0f; legRx = 0f;
        }

        // Airborne: tuck the legs, arms up.
        if (!grounded)
        {
            legLx = 0.55f; legRx = -0.25f;
            armLx = -2.3f; armRx = -2.0f;
        }

        // --- attacks ---------------------------------------------------------
        float sinceAtk = combat != null ? t - combat.LastAttackTime : 99f;
        if (sinceAtk < 0.24f)
        {
            float p = sinceAtk / 0.24f;                 // 0 → 1 over the swing
            float thrust = Mathf.Sin(p * Mathf.PI);      // out and back
            if (combat != null && combat.HasItem)
            {
                // Weapon swing: big overhead arc with the right arm.
                armRx = -0.4f - 2.1f * thrust;
                armLx = 0.3f * thrust;
            }
            else
            {
                // Bare-hand punch: right arm snaps forward.
                armRx = -1.55f * thrust;
                armLx = 0.35f * thrust;
            }
        }

        // Catch pose: both arms forward while the catch window is open.
        if (combat != null && combat.CatchWindowActive && sinceAtk >= 0.24f)
        {
            armLx = -0.95f; armRx = -0.95f;
        }

        // --- hurt flinch ------------------------------------------------------
        float sinceHit = combat != null ? t - combat.LastHitTime : 99f;
        float flinch = sinceHit < 0.18f ? Mathf.Sin((sinceHit / 0.18f) * Mathf.PI) : 0f;
        flinchBlend = flinch;

        // --- apply poses -------------------------------------------------------
        visual.ArmL.localRotation = Quaternion.Euler(armLx, 0f, 0.10f);
        visual.ArmR.localRotation = Quaternion.Euler(armRx, 0f, -0.10f);
        visual.LegL.localRotation = Quaternion.Euler(legLx, 0f, 0f);
        visual.LegR.localRotation = Quaternion.Euler(legRx, 0f, 0f);

        // Body: run bounce + lean into movement + flinch rock-back.
        float bob = grounded ? Mathf.Abs(Mathf.Sin(runPhase)) * 0.07f * speedF : 0f;
        float lean = 0.14f * speedF - 0.30f * flinchBlend;
        visual.Body.localRotation = Quaternion.Euler(lean, 0f, 0f);
        Vector3 bp = new Vector3(0f, -1.02f + bob, 0f);

        // Transform: gold tint + pulsing aura scale.
        bool transformed = combat != null && combat.IsTransformed;
        visual.SetGold(transformed);
        float aura = transformed ? 1f + Mathf.Sin(t * 9f) * 0.035f : 1f;
        visual.Body.localPosition = bp;
        visual.Body.localScale = new Vector3(aura, aura * (1f - 0.10f * flinchBlend), aura);

        // Head: slight look-around bob while idle, snaps down when hurt.
        if (visual.Head != null)
            visual.Head.localRotation = Quaternion.Euler(0.25f * flinchBlend, speedF < 0.08f ? Mathf.Sin(t * 0.7f) * 0.18f : 0f, 0f);
    }
}
