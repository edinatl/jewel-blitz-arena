using UnityEngine;

// Power Stone-style arena camera: fixed angle, frames all fighters,
// zooms out as they spread apart, zooms in as they close in.
public class ArenaCamera : MonoBehaviour
{
    public float pitch = 42f;          // degrees looking down
    public float minDistance = 11f;
    public float maxDistance = 22f;
    public float smooth = 4f;
    public Vector2 arenaClamp = new Vector2(7.5f, 7.5f);

    void LateUpdate()
    {
        var players = GameObject.FindGameObjectsWithTag("Player");
        if (players.Length == 0) return;

        Vector3 center = Vector3.zero;
        float spread = 0f;
        for (int i = 0; i < players.Length; i++) center += players[i].transform.position;
        center /= players.Length;

        for (int i = 0; i < players.Length; i++)
            spread = Mathf.Max(spread, Vector3.Distance(center, players[i].transform.position));

        center.x = Mathf.Clamp(center.x, -arenaClamp.x, arenaClamp.x);
        center.z = Mathf.Clamp(center.z, -arenaClamp.y, arenaClamp.y);
        center.y = 0f;

        float dist = Mathf.Clamp(minDistance + spread * 1.1f, minDistance, maxDistance);
        float rad = pitch * Mathf.Deg2Rad;
        Vector3 desired = center + new Vector3(0f, Mathf.Sin(rad) * dist, -Mathf.Cos(rad) * dist);

        transform.position = Vector3.Lerp(transform.position, desired, smooth * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation,
            Quaternion.LookRotation(center + Vector3.up * 1f - transform.position), smooth * Time.deltaTime);
    }
}
