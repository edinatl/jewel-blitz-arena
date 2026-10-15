using UnityEngine;
using UnityEngine.SceneManagement;

// Match flow: timer, KO detection, winner, restart. Also draws the HUD
// with OnGUI so the prototype needs zero Canvas/prefab setup.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public float matchTime = 120f;
    public float TimeLeft { get; private set; }
    public bool MatchOver { get; private set; }
    public string WinnerText { get; private set; }

    private Health[] fighters;
    private float koTimer = -1f;
    private Health koLoser;

    void Awake()
    {
        Instance = this;
        TimeLeft = matchTime;
        MatchOver = false;
        WinnerText = "";
    }

    void Start()
    {
        fighters = FindObjectsOfType<Health>();
        for (int i = 0; i < fighters.Length; i++)
            fighters[i].OnKO += HandleKO;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            return;
        }

        if (MatchOver) return;

        TimeLeft -= Time.deltaTime;
        if (TimeLeft <= 0f)
        {
            TimeLeft = 0f;
            DecideByHealth();
        }

        if (koTimer > 0f)
        {
            koTimer -= Time.deltaTime;
            if (koTimer <= 0f) FinishMatch();
        }
    }

    void HandleKO(Health loser)
    {
        if (MatchOver || koTimer > 0f) return;
        koLoser = loser;
        koTimer = 1.2f; // brief beat before the banner
        // Tip the loser over.
        loser.transform.rotation = Quaternion.Euler(90f, loser.transform.rotation.eulerAngles.y, 0f);
    }

    void FinishMatch()
    {
        MatchOver = true;
        for (int i = 0; i < fighters.Length; i++)
        {
            if (fighters[i] != koLoser)
            {
                var ctrl = fighters[i].GetComponent<PlayerController>();
                WinnerText = (ctrl != null ? ctrl.displayName : "A fighter") + " WINS!";
                return;
            }
        }
        WinnerText = "DRAW!";
    }

    void DecideByHealth()
    {
        if (fighters == null || fighters.Length < 2) { WinnerText = "TIME UP!"; MatchOver = true; return; }
        if (fighters[0].currentHealth == fighters[1].currentHealth)
        {
            WinnerText = "DRAW!";
        }
        else
        {
            Health winner = fighters[0].currentHealth > fighters[1].currentHealth ? fighters[0] : fighters[1];
            var ctrl = winner.GetComponent<PlayerController>();
            WinnerText = (ctrl != null ? ctrl.displayName : "A fighter") + " WINS ON HEALTH!";
        }
        MatchOver = true;
    }

    // --- HUD (OnGUI, no setup required) ---------------------------------------

    private GUIStyle timerStyle;
    private GUIStyle bannerStyle;
    private GUIStyle subStyle;

    void EnsureStyles()
    {
        if (timerStyle != null) return;
        timerStyle = new GUIStyle(GUI.skin.label);
        timerStyle.richText = true;
        timerStyle.alignment = TextAnchor.MiddleCenter;
        timerStyle.fontSize = 20;
        bannerStyle = new GUIStyle(GUI.skin.label);
        bannerStyle.richText = true;
        bannerStyle.alignment = TextAnchor.MiddleCenter;
        bannerStyle.fontSize = 32;
        bannerStyle.fontStyle = FontStyle.Bold;
        subStyle = new GUIStyle(GUI.skin.label);
        subStyle.richText = true;
        subStyle.alignment = TextAnchor.MiddleCenter;
        subStyle.fontSize = 16;
    }

    void OnGUI()
    {
        EnsureStyles();
        var players = GameObject.FindGameObjectsWithTag("Player");
        // Sort left-to-right by playerIndex for stable HUD placement.
        System.Array.Sort(players, (a, b) =>
        {
            var ca = a.GetComponent<PlayerController>();
            var cb = b.GetComponent<PlayerController>();
            int ia = ca != null ? ca.playerIndex : 0;
            int ib = cb != null ? cb.playerIndex : 0;
            return ia.CompareTo(ib);
        });

        for (int i = 0; i < players.Length && i < 2; i++)
        {
            var health = players[i].GetComponent<Health>();
            var combat = players[i].GetComponent<PlayerCombat>();
            var ctrl = players[i].GetComponent<PlayerController>();
            if (health == null) continue;

            bool left = (i == 0);
            float w = 320f, h = 22f;
            float x = left ? 20f : Screen.width - w - 20f;
            float y = 16f;
            string label = ctrl != null ? ctrl.displayName : ("Player " + (i + 1));

            GUI.Label(new Rect(x, y, w, 20f), label + (combat != null && combat.IsTransformed ? "  - TRANSFORMED!" : ""));
            // Bar background + fill
            GUI.Box(new Rect(x, y + 20f, w, h), "");
            float frac = Mathf.Clamp01(health.currentHealth / health.maxHealth);
            Color old = GUI.color;
            GUI.color = combat != null && combat.IsTransformed ? new Color(1f, 0.8f, 0.2f) : new Color(0.25f, 0.85f, 0.35f);
            float fillW = (w - 4f) * frac;
            float fillX = left ? x + 2f : x + w - 2f - fillW;
            GUI.Box(new Rect(fillX, y + 22f, fillW, h - 4f), "");
            GUI.color = old;

            // Gem pips
            int gems = combat != null ? combat.gems : 0;
            string pips = "Gems: ";
            for (int g = 0; g < 3; g++) pips += (g < gems) ? "[X] " : "[ ] ";
            if (combat != null && combat.IsTransformed)
                pips = "Transformed: " + Mathf.CeilToInt(combat.TransformTimeLeft) + "s";
            GUI.Label(new Rect(x, y + 46f, w, 20f), pips);

            // Held item + catch flash
            if (combat != null)
            {
                string held = combat.HasItem ? ("Item: " + combat.HeldItemName) : "Item: -";
                if (Time.time - combat.LastCatchTime < 1.5f) held += "   CAUGHT!";
                GUI.Label(new Rect(x, y + 66f, w, 20f), held);
            }
        }

        // Timer, top center
        int secs = Mathf.CeilToInt(TimeLeft);
        GUI.Label(new Rect(Screen.width / 2f - 30f, 14f, 60f, 24f),
                  (secs / 60) + ":" + (secs % 60).ToString("00"), timerStyle);

        if (MatchOver)
        {
            GUI.Label(new Rect(Screen.width / 2f - 200f, Screen.height / 2f - 40f, 400f, 50f),
                      WinnerText, bannerStyle);
            GUI.Label(new Rect(Screen.width / 2f - 200f, Screen.height / 2f + 10f, 400f, 30f),
                      "Press R to restart", subStyle);
        }

        // Controls footer
        GUI.Label(new Rect(20f, Screen.height - 28f, Screen.width - 40f, 22f),
            "P1: WASD, SPACE jump, F light/use (empty hands = CATCH), G heavy/THROW item   |   P2 (AI on — any P2 key takes over): Arrows, RightShift jump, . light/catch, / heavy/throw   |   R restart");
    }
}
