using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// 單一感應小格（最小顯示單位）。
/// 觸發來源：
///   A. 滑鼠/觸控測試  → OnPointerEnter
///   B. UDP 實體設備   → TileManager.OnSensorPoint → TileManager 直接呼叫此磚
/// </summary>
public class SensorTile : MonoBehaviour, IPointerEnterHandler
{
    // ── 識別 ──────────────────────────────────────────────────
    [HideInInspector] public int tileID;        // 全域小格 ID（1-based）
    [HideInInspector] public int largeTileID;   // 所屬大格 ID（1-based）
    [HideInInspector] public int subIndex;      // 在大格內的子格索引（0-based）

    // ── 由 TileManager 注入的子物件參考 ─────────────────────
    [HideInInspector] public TextMeshProUGUI labelID;
    [HideInInspector] public TextMeshProUGUI labelTime;
    [HideInInspector] public TextMeshProUGUI labelCount;

    private Image _bg;

    // ── 顏色 ──────────────────────────────────────────────────
    private static readonly Color ColIdle        = new Color(0.18f, 0.18f, 0.18f);
    private static readonly Color ColIdleAlt     = new Color(0.22f, 0.22f, 0.22f); // 大格內棋盤交替色
    private static readonly Color ColOriginSmall = new Color(0.10f, 0.80f, 0.30f); // 小格起點：綠
    private static readonly Color ColHitSmall    = new Color(0.95f, 0.80f, 0.10f); // 小格命中：黃
    private static readonly Color ColOriginLarge = new Color(0.05f, 0.60f, 0.20f); // 大格起點：深綠
    private static readonly Color ColHitLarge    = new Color(0.80f, 0.60f, 0.05f); // 大格命中：深黃

    // ── 狀態 ──────────────────────────────────────────────────
    private bool _touchedThisRound;
    private int  _repeatCount;
    private bool _isAltCell; // 棋盤格交替背景

    void Awake()
    {
        _bg = GetComponentInChildren<Image>();
    }

    // ── 初始化（TileManager 建立時呼叫）─────────────────────
    public void Init(bool altCell)
    {
        _isAltCell = altCell;
        SetIdleColor();
    }

    // ── 滑鼠測試路徑 ─────────────────────────────────────────
    public void OnPointerEnter(PointerEventData eventData)
    {
        TileManager.Instance.OnTilePressed(this);
    }

    // ── 由 TileManager 呼叫的狀態方法 ────────────────────────

    public void SetAsOrigin(bool isLargeTileHit)
    {
        _touchedThisRound = true;
        _repeatCount      = 0;
        _bg.color         = isLargeTileHit ? ColOriginLarge : ColOriginSmall;
        if (labelTime  != null) labelTime.text  = "0.00";
        if (labelCount != null) labelCount.text = "";
    }

    public void SetAsFollower(float elapsed, bool isLargeTileHit)
    {
        _touchedThisRound = true;
        _repeatCount      = 0;
        _bg.color         = isLargeTileHit ? ColHitLarge : ColHitSmall;
        if (labelTime  != null) labelTime.text  = elapsed.ToString("0.00");
        if (labelCount != null) labelCount.text = "";
    }

    public void AddRepeat()
    {
        _repeatCount++;
        if (labelCount != null)
            labelCount.text = "x" + (_repeatCount + 1);
    }

    public void ResetTile()
    {
        _touchedThisRound = false;
        _repeatCount      = 0;
        SetIdleColor();
        if (labelTime  != null) labelTime.text  = "";
        if (labelCount != null) labelCount.text = "";
    }

    private void SetIdleColor()
    {
        _bg.color = _isAltCell ? ColIdleAlt : ColIdle;
    }

    public bool IsTouchedThisRound => _touchedThisRound;
}
