using System.Collections.Generic;
using UnityEngine;

// HD animated sprite fighter (v0.7): a camera-facing quad that plays the
// generated sprite-sheet frames — the same frames the web demos use —
// for the Soldier and Samurai. Frame choice mirrors the web demos:
// ko / hurt / attack (punch vs swing) / catch / run cycle / idle, with
// gold variants while transformed, a spin while dodge-rolling, and a
// red flash when hit. The primitive CharacterVisual body is hidden
// while this is active (FighterAnimator keeps animating it harmlessly).
// Frames live in Assets/Resources/Sprites/ and load by name, e.g.
// "soldier_idle", "samurai_swing_gold".
public class SpriteBillboard : MonoBehaviour
{
    public string character = "Soldier";

    private PlayerCombat combat;
    private Health health;
    private Rigidbody rb;
    private Transform quad;
    private MeshRenderer quadRenderer;
    private Material mat;
    private Transform camT;
    private string currentKey = "";
    private readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();

    void Start()
    {
        combat = GetComponent<PlayerCombat>();
        health = GetComponent<Health>();
        rb = GetComponent<Rigidbody>();

        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        ItemVisual.StripCollider(go);
        go.name = name + "_Sprite";
        quad = go.transform;
        quad.localScale = new Vector3(1.9f, 1.94f, 1f);
        quadRenderer = go.GetComponent<MeshRenderer>();
        mat = new Material(Shader.Find("Unlit/Transparent"));
        quadRenderer.material = mat;
        quad.position = transform.position + Vector3.up * 0.97f;

        // Hide every primitive body renderer (the capsule is already off).
        foreach (var r in GetComponentsInChildren<MeshRenderer>())
            r.enabled = false;
    }

    Texture2D LoadTex(string key)
    {
        if (cache.ContainsKey(key)) return cache[key];
        var tex = Resources.Load<Texture2D>("Sprites/" + key);
        cache[key] = tex;
        return tex;
    }

    string PickFrame()
    {
        if (health != null && health.IsKO) return "ko";
        if (combat == null) return "idle";
        if (combat.GrabbedBy != null) return "hurt";
        if (combat.IsKnockedDown) return "ko";
        float t = Time.time;
        if (t - combat.LastHitTime < 0.18f) return "hurt";
        if (t - combat.LastAttackTime < 0.24f) return combat.HasItem ? "swing" : "punch";
        if (combat.CatchWindowActive) return "punch";
        float spd = rb != null ? new Vector2(rb.velocity.x, rb.velocity.z).magnitude : 0f;
        if (spd > 0.7f) return Mathf.FloorToInt(t * 7f) % 2 == 0 ? "run1" : "run2";
        return "idle";
    }

    GameObject FindOpponent()
    {
        var players = GameObject.FindGameObjectsWithTag("Player");
        for (int i = 0; i < players.Length; i++)
            if (players[i] != gameObject) return players[i];
        return null;
    }

    void LateUpdate()
    {
        if (quad == null) return;
        if (camT == null && Camera.main != null) camT = Camera.main.transform;

        bool gold = combat != null && combat.IsTransformed;
        string frame = PickFrame();
        string prefix = character.ToLower();
        string key = prefix + "_" + frame + (gold ? "_gold" : "");
        if (key != currentKey)
        {
            Texture2D tex = LoadTex(key);
            if (tex == null && gold) tex = LoadTex(prefix + "_" + frame);
            if (tex != null)
            {
                currentKey = key;
                mat.mainTexture = tex;
            }
        }

        // Follow the fighter; lift with physics height (jump / held overhead).
        quad.position = transform.position + Vector3.up * 0.97f;

        // Face the camera (Y billboard).
        float yaw = 0f;
        if (camT != null)
        {
            Vector3 toCam = camT.position - quad.position;
            toCam.y = 0f;
            if (toCam.sqrMagnitude > 0.01f) yaw = Mathf.Atan2(toCam.x, toCam.z);
        }
        // Flip toward the opponent, measured in camera space.
        float flip = 1f;
        GameObject opp = FindOpponent();
        if (opp != null && camT != null)
        {
            float ox = camT.InverseTransformPoint(opp.transform.position).x;
            float mx = camT.InverseTransformPoint(transform.position).x;
            flip = ox >= mx ? 1f : -1f;
        }
        float rollA = 0f;
        if (combat != null && combat.IsRolling)
            rollA = -360f * Mathf.Clamp01((Time.time - combat.RollStartTime) / PlayerCombat.RollDuration) * flip;

        Quaternion q = Quaternion.Euler(0f, yaw * Mathf.Rad2Deg, 0f) * Quaternion.Euler(0f, 0f, rollA);
        quad.rotation = q;
        float sc = gold ? 1.16f : 1f;
        quad.localScale = new Vector3(1.9f * flip * sc, 1.94f * sc, 1f);

        bool hurtFlash = combat != null && Time.time - combat.LastHitTime < 0.12f && !(health != null && health.IsKO);
        mat.color = hurtFlash ? new Color(1f, 0.55f, 0.55f) : Color.white;
    }

    void OnDestroy()
    {
        if (quad != null) Destroy(quad.gameObject);
        if (mat != null) Destroy(mat);
    }
}
