using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TileManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────
    public static TileManager Instance { get; private set; }

    // ── Inspector 公開變數 ────────────────────────────────────
    [Header("測試設定")]
    [Tooltip("一輪持續秒數，可在 Inspector 自由調整")]
    public float roundDuration = 5f;

    [Header("場景參考")]
    public GameObject tilePrefab;     // 64x64 Button Prefab
    public Transform tileContainer;  // Canvas 下的容器物件

    // ── 私有狀態 ──────────────────────────────────────────────
    private readonly List<SensorTile> _tiles = new();

    private enum State { Idle, Running }
    private State _state = State.Idle;

    private float _roundStart;   // 本輪第一個磚被點擊的 Time.realtimeSinceStartup

    // ─────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        Input.multiTouchEnabled = true;
        BuildTiles();
    }

    // ── 建立 384 個磚 ─────────────────────────────────────────
    void BuildTiles()
    {
        // 清空舊物件（編輯器重跑時保險用）
        foreach (Transform child in tileContainer)
            Destroy(child.gameObject);
        _tiles.Clear();

        int cols = 32;   // 2048 / 64
        int rows = 12;   // 768  / 64
        int total = cols * rows;   // 384

        for (int i = 0; i < total; i++)
        {
            GameObject go = Instantiate(tilePrefab, tileContainer);
            SensorTile tile = go.GetComponent<SensorTile>();

            // 注入子 Text 參考（依 Prefab 的子物件順序）
            TextMeshProUGUI[] texts = go.GetComponentsInChildren<TextMeshProUGUI>();
            tile.labelID = texts[0];
            tile.labelTime = texts[1];
            tile.labelCount = texts[2];

            tile.tileID = i + 1;          // 1-based 顯示
            tile.labelID.text = (i + 1).ToString();
            tile.labelTime.text = "";
            tile.labelCount.text = "";

            _tiles.Add(tile);
        }
    }

    // ── 磚被按下（SensorTile 呼叫）───────────────────────────
    public void OnTilePressed(SensorTile tile)
    {
        switch (_state)
        {
            case State.Idle:
                // 任何按鈕都可以啟動新一輪
                StartRound(tile);
                break;

            case State.Running:
                if (!tile.IsTouchedThisRound)
                    tile.SetAsFollower(Time.realtimeSinceStartup - _roundStart);
                else
                    tile.AddRepeat();
                break;
        }
    }

    // ── 新一輪開始 ────────────────────────────────────────────
    void StartRound(SensorTile originTile)
    {
        // 1. 全部重置
        foreach (var t in _tiles) t.ResetTile();

        // 2. 設定起點磚
        _roundStart = Time.realtimeSinceStartup;
        originTile.SetAsOrigin();

        // 3. 切換狀態，啟動倒數
        _state = State.Running;
        StartCoroutine(RoundTimer());
    }

    // ── 倒數計時器 ────────────────────────────────────────────
    IEnumerator RoundTimer()
    {
        yield return new WaitForSecondsRealtime(roundDuration);
        _state = State.Idle;
        // 畫面結果保留，等待下一個按鈕啟動新一輪
    }
}