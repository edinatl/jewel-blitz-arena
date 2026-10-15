using UnityEngine;

// Health + knockout state for a fighter. Attached to each player by the builder.
public class Health : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth = 100f;
    public bool IsKO { get; private set; }

    // Raised once when health hits zero. GameManager listens for this.
    public System.Action<Health> OnKO;

    void Awake()
    {
        currentHealth = maxHealth;
        IsKO = false;
    }

    public void TakeDamage(float amount)
    {
        if (IsKO) return;
        currentHealth -= amount;
        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            IsKO = true;
            if (OnKO != null) OnKO(this);
        }
    }
}
