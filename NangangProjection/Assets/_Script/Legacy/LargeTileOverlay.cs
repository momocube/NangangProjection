using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 大格覆蓋層：在每個 25×25cm（64×64px）大格上方疊一個純邊框，
/// 視覺上區隔大格邊界，不接收事件（Raycast Target = false）。
/// 由 TileManager 建立並管理。
/// </summary>
[RequireComponent(typeof(Image))]
public class LargeTileOverlay : MonoBehaviour
{
    [HideInInspector] public int largeTileID; // 1-based

    private Image _border;

    // 邊框顏色（半透明白，在深色小格上清晰可見）
    private static readonly Color ColBorder        = new Color(1f, 1f, 1f, 0.25f);
    private static readonly Color ColBorderActive  = new Color(1f, 1f, 1f, 0.70f);

    void Awake()
    {
        _border = GetComponent<Image>();
        _border.color        = ColBorder;
        _border.raycastTarget = false; // 不攔截事件，讓小格接收
    }

    public void SetActive(bool active)
    {
        _border.color = active ? ColBorderActive : ColBorder;
    }

    public void ResetOverlay()
    {
        _border.color = ColBorder;
    }
}
