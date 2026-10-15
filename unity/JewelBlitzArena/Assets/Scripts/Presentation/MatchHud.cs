using UnityEngine;

namespace JewelBlitzArena.Presentation
{
    /// <summary>
    /// Legacy stub — HUD lives in <see cref="MetaMenuHud"/> after Meta/Net wiring.
    /// Kept so old scenes referencing MatchHud do not break; redirects to MetaMenuHud.
    /// </summary>
    [RequireComponent(typeof(MetaMenuHud))]
    public sealed class MatchHud : MonoBehaviour
    {
    }
}
