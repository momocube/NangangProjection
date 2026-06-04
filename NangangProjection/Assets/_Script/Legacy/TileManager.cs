using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 管理所有小格（SensorTile）與大格覆蓋層（LargeTileOverlay）。
///
/// 畫面佈局：
///   - 小格 Prefab：最小感應單位，填滿畫面
///   - 大格 Overlay Prefab：純邊框，疊在小格上方，標示 25×25cm 方塊邊界
///
/// 參數說明（Inspector 可調，不需改程式）：
///   screenCols / screenRows   → 小格欄列數（依 UDP 精度確認後填入）
///   largeCols / largeRows     → 大格欄列數（= screenCols/subCols）
///   subCols / subRows         → 每個大格內小格排列（待實機確認）
///
/// 觸發路徑：
///   A. 滑鼠/觸控  → SensorTile.OnPointerEnter → OnTilePressed
///   B. UDP 設備   → UdpSensorReceiver → OnSensorPoint(pixelX, pixelY)
/// </summary>
public class TileManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────
    public static TileManager Instance { get; private set; }

    // ── Inspector：場景參考 ───────────────────────────────────
    [Header("Prefabs")]
    [Tooltip("小格 Prefab（含 Image + 3個 TMP Text 子物件）")]
    public GameObject smallTilePrefab;

    [Tooltip("大格邊框 Overlay Prefab（純 Image，無需 Text）")]
    public GameObject largeTileOverlayPrefab;

    [Tooltip("小格放置的容器（Canvas 下的 RectTransform）")]
    public RectTransform smallTileContainer;

    [Tooltip("大格 Overlay 放置的容器（在 smallTileContainer 上方，同尺寸）")]
    public RectTransform largeTileContainer;

    // ── Inspector：格子尺寸設定 ───────────────────────────────
    [Header("格子設定（依設備確認後填入）")]

    [Tooltip("小格欄數（畫面寬度/小格像素寬）\n" +
             "★ 等 UDP 精度確認後調整\n" +
             "  若 UDP = 大格精度：screenCols = 32（= 2048/64）\n" +
             "  若 UDP = 小格精度：screenCols = 64 或 128（待測）")]
    public int screenCols = 32;

    [Tooltip("小格列數（畫面高度/小格像素高）")]
    public int screenRows = 12;

    [Tooltip("每個大格內，橫向小格數\n2列×4欄 → subCols=4；4列×2欄 → subCols=2")]
    public int subCols = 2;   // ← 待實機確認後調整

    [Tooltip("每個大格內，縱向小格數")]
    public int subRows = 1;   // ← 待實機確認後調整

    [Tooltip("畫面寬度（px）")]
    public int screenWidth = 2048;

    [Tooltip("畫面高度（px）")]
    public int screenHeight = 768;

    // ── Inspector：遊戲設定 ───────────────────────────────────
    [Header("測試設定")]
    public float roundDuration = 5f;

    // ── 推導值（程式自動計算）────────────────────────────────
    private int _largeCols;   // 大格欄數 = screenCols / subCols
    private int _largeRows;   // 大格列數 = screenRows / subRows
    private int _smallPixW;   // 每小格像素寬
    private int _smallPixH;   // 每小格像素高

    // ── 資料結構 ──────────────────────────────────────────────
    private readonly List<SensorTile>       _smallTiles   = new();
    private readonly Dictionary<int, SensorTile> _smallMap = new(); // smallID → tile

    private readonly List<LargeTileOverlay>       _overlays   = new();
    private readonly Dictionary<int, List<SensorTile>> _largeMap = new(); // largeID → [smallTiles]
    private readonly Dictionary<int, LargeTileOverlay>  _overlayMap = new(); // largeID → overlay

    // ── 狀態機 ────────────────────────────────────────────────
    private enum State { Idle, Running }
    private State _state = State.Idle;
    private float _roundStart;

    // ─────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        Input.multiTouchEnabled = true;
        CalculateDerivedValues();
        BuildTiles();
    }

    // ── 推導尺寸 ──────────────────────────────────────────────
    void CalculateDerivedValues()
    {
        _largeCols  = screenCols / subCols;
        _largeRows  = screenRows / subRows;
        _smallPixW  = screenWidth  / screenCols;
        _smallPixH  = screenHeight / screenRows;

        Debug.Log($"[TileManager] 小格 {screenCols}×{screenRows}（共{screenCols * screenRows}格）" +
                  $"  大格 {_largeCols}×{_largeRows}（共{_largeCols * _largeRows}格）" +
                  $"  每小格 {_smallPixW}×{_smallPixH}px");
    }

    // ══════════════════════════════════════════════════════════
    // 建立格子
    // ══════════════════════════════════════════════════════════
    void BuildTiles()
    {
        ClearAll();

        // ── 1. 建立小格 ──────────────────────────────────────
        for (int r = 0; r < screenRows; r++)
        {
            for (int c = 0; c < screenCols; c++)
            {
                GameObject go   = Instantiate(smallTilePrefab, smallTileContainer);
                SensorTile tile = go.GetComponent<SensorTile>();

                // 注入 Text 參考
                TextMeshProUGUI[] texts = go.GetComponentsInChildren<TextMeshProUGUI>();
                if (texts.Length >= 3)
                {
                    tile.labelID    = texts[0];
                    tile.labelTime  = texts[1];
                    tile.labelCount = texts[2];
                }

                // 計算 ID
                int smallID = r * screenCols + c + 1; // 1-based
                int largeC  = c / subCols;
                int largeR  = r / subRows;
                int largeID = largeR * _largeCols + largeC + 1; // 1-based
                int subIdx  = (r % subRows) * subCols + (c % subCols);

                tile.tileID      = smallID;
                tile.largeTileID = largeID;
                tile.subIndex    = subIdx;

                // 棋盤交替色（以大格為單位，偶數大格內小格偏色）
                bool alt = ((largeC + largeR) % 2 == 1);
                tile.Init(alt);

                if (tile.labelID != null)
                    tile.labelID.text = smallID.ToString();

                _smallTiles.Add(tile);
                _smallMap[smallID] = tile;

                // 加入大格分組
                if (!_largeMap.ContainsKey(largeID))
                    _largeMap[largeID] = new List<SensorTile>();
                _largeMap[largeID].Add(tile);
            }
        }

        // ── 2. 建立大格 Overlay ───────────────────────────────
        if (largeTileOverlayPrefab != null && largeTileContainer != null)
        {
            for (int r = 0; r < _largeRows; r++)
            {
                for (int c = 0; c < _largeCols; c++)
                {
                    GameObject go      = Instantiate(largeTileOverlayPrefab, largeTileContainer);
                    LargeTileOverlay ov = go.GetComponent<LargeTileOverlay>();
                    int largeID        = r * _largeCols + c + 1;
                    ov.largeTileID     = largeID;

                    _overlays.Add(ov);
                    _overlayMap[largeID] = ov;
                }
            }
        }
    }

    void ClearAll()
    {
        foreach (Transform child in smallTileContainer)  Destroy(child.gameObject);
        foreach (Transform child in largeTileContainer)  Destroy(child.gameObject);
        _smallTiles.Clear(); _smallMap.Clear();
        _overlays.Clear();   _overlayMap.Clear();
        _largeMap.Clear();
    }

    // ══════════════════════════════════════════════════════════
    // 公開觸發入口
    // ══════════════════════════════════════════════════════════

    /// <summary>滑鼠/觸控路徑：小格直接呼叫</summary>
    public void OnTilePressed(SensorTile tile)
    {
        HandleTrigger(tile);
    }

    /// <summary>
    /// UDP 路徑：傳入像素座標（設備廣播值）。
    /// 自動換算成小格 ID，再觸發。
    /// </summary>
    public void OnSensorPoint(int pixelX, int pixelY)
    {
        // 邊界保護
        if (pixelX < 0 || pixelX >= screenWidth)  return;
        if (pixelY < 0 || pixelY >= screenHeight)  return;

        int col     = pixelX / _smallPixW;
        int row     = pixelY / _smallPixH;
        int smallID = row * screenCols + col + 1;

        if (!_smallMap.TryGetValue(smallID, out SensorTile tile)) return;
        HandleTrigger(tile);
    }

    // ── 核心狀態機 ────────────────────────────────────────────
    private void HandleTrigger(SensorTile tile)
    {
        switch (_state)
        {
            case State.Idle:
                StartRound(tile);
                break;

            case State.Running:
                if (!tile.IsTouchedThisRound)
                {
                    float elapsed    = Time.realtimeSinceStartup - _roundStart;
                    bool largeTouched = IsLargeTileFirstTouch(tile.largeTileID);

                    // 點亮小格
                    tile.SetAsFollower(elapsed, largeTouched);

                    // 點亮大格 Overlay（若整個大格第一次被碰到）
                    if (largeTouched)
                        ActivateLargeOverlay(tile.largeTileID);
                }
                else
                {
                    tile.AddRepeat();
                }
                break;
        }
    }

    // ── 新一輪 ────────────────────────────────────────────────
    void StartRound(SensorTile originTile)
    {
        // 重置所有小格
        foreach (var t in _smallTiles) t.ResetTile();
        // 重置所有大格 Overlay
        foreach (var ov in _overlays) ov.ResetOverlay();

        _roundStart = Time.realtimeSinceStartup;

        // 起點小格（綠）+ 起點大格（亮邊框）
        originTile.SetAsOrigin(isLargeTileHit: false);
        ActivateLargeOverlay(originTile.largeTileID);

        _state = State.Running;
        StartCoroutine(RoundTimer());
    }

    IEnumerator RoundTimer()
    {
        yield return new WaitForSecondsRealtime(roundDuration);
        _state = State.Idle;
    }

    // ── 輔助：該大格是否為本輪第一次觸碰 ────────────────────
    private bool IsLargeTileFirstTouch(int largeID)
    {
        if (!_largeMap.TryGetValue(largeID, out var cells)) return false;
        // 如果該大格內所有小格都還沒被碰（除了剛剛正在處理的那顆），代表第一次
        int touchedCount = 0;
        foreach (var c in cells)
            if (c.IsTouchedThisRound) touchedCount++;
        return touchedCount == 0; // 在 SetAsFollower 之前呼叫，所以 = 0 代表第一次
    }

    private void ActivateLargeOverlay(int largeID)
    {
        if (_overlayMap.TryGetValue(largeID, out var ov))
            ov.SetActive(true);
    }
}
