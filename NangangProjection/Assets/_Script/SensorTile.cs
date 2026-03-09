using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class SensorTile : MonoBehaviour, IPointerDownHandler
{
    [HideInInspector] public int tileID;

    // 子物件 Text 參考（由 TileManager 在建立時注入）
    [HideInInspector] public TextMeshProUGUI labelID;
    [HideInInspector] public TextMeshProUGUI labelTime;
    [HideInInspector] public TextMeshProUGUI labelCount;

    private Image _bg;

    // 顏色定義
    private static readonly Color ColIdle = new Color(0.25f, 0.25f, 0.25f);
    private static readonly Color ColGreen = new Color(0.10f, 0.80f, 0.30f);
    private static readonly Color ColYellow = new Color(0.95f, 0.80f, 0.10f);

    // 該輪狀態
    private bool _touchedThisRound;
    private int _repeatCount;

    void Awake()
    {
        _bg = GetComponent<Image>();
    }

    // ── Unity UI 觸控 / 點擊事件 ──────────────────────────────
    public void OnPointerDown(PointerEventData eventData)
    {
        TileManager.Instance.OnTilePressed(this);
    }

    // ── 由 TileManager 呼叫 ───────────────────────────────────

    /// <summary>設為第一個觸發的綠色磚（本輪起點）</summary>
    public void SetAsOrigin()
    {
        _touchedThisRound = true;
        _repeatCount = 0;
        _bg.color = ColGreen;
        labelTime.text = "0.00";
        labelCount.text = "";
    }

    /// <summary>設為後續觸發的黃色磚，記錄時間差</summary>
    public void SetAsFollower(float elapsed)
    {
        _touchedThisRound = true;
        _repeatCount = 0;
        _bg.color = ColYellow;
        labelTime.text = elapsed.ToString("0.00");
        labelCount.text = "";
    }

    /// <summary>重複點擊同一顆已觸發的磚</summary>
    public void AddRepeat()
    {
        _repeatCount++;
        labelCount.text = "x" + (_repeatCount + 1);   // 第2次點擊顯示 x2
    }

    /// <summary>完整重置（新一輪開始時呼叫）</summary>
    public void ResetTile()
    {
        _touchedThisRound = false;
        _repeatCount = 0;
        _bg.color = ColIdle;
        labelTime.text = "";
        labelCount.text = "";
    }

    public bool IsTouchedThisRound => _touchedThisRound;
}