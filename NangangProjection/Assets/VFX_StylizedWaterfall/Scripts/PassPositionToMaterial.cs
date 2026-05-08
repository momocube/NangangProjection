using System.Collections.Generic;
using UnityEngine;

//[ExecuteInEditMode]
public class PassPositionToMaterial : MonoBehaviour {

    [SerializeField] private Material material;
    [SerializeField] private float maxRadiusOffset = 0.2f;

    [Header("Radius Speed")]
    [SerializeField] private float expandSpeed = 1.5f;
    [SerializeField] private float shrinkSpeed = 10.0f;
    [SerializeField] private float fadeOutSpeed = 0.8f;

    [Header("Enable this to process detected input positions.")]
    public bool inputPosDetect;

    [Header("Test by Mouse")]
    [Tooltip("Enable this to test by mouse click.")]
    public bool testByMouse;
    [Tooltip("Enable this before testing by mouse click if your Input Pos Detect has been enabled.")]
    public bool prepareToTestByMouse;

    [Header("Set input screen position to this list.")]
    [Tooltip("If one detected position needed to be removed, you need to add Vector4(0,0,0,-1) to this position and it would be disappeared and removed automatically.")]
    public List<Vector4> inputPosition = new();
    public bool setLeftUpperCornerAsOrigin = true;

    [SerializeField] private readonly int maxPositionNum = 20;
    [SerializeField] private int currentPositionNum;
    [SerializeField] private Vector4[] maxPosition;
    [SerializeField] private GameObject splashFX;
    [SerializeField] private int splashFXPoolNum = 40;
    [SerializeField] private GameObject[] splashFXPool;

    /// <summary>
    /// 由 SensorBridgeToPassPosition 設定：對應 slot 是否處於按壓狀態。
    /// 按壓 + 暴度已全開時，移動位置不縮小 radius（讓效果跟著手移動）。
    /// </summary>
    public bool[] slotPressHeld = new bool[20];

    /// <summary>
    /// 由 SensorBridgeToPassPosition 設定：對應 slot 是否正在淡出。
    /// 淡出時以較慢速度縮小 radius，視覺上緩慢消失。
    /// </summary>
    public bool[] slotFadingOut = new bool[20];

    private string positionProperty = "_ObjectPosition";
    private string radiusProperty = "_ObjectRadius";

    private float[] radiusOffset;
    private float[] radiusOffsetTemp;
    private Vector4[] maxPositionLast;
    private int lastPositionNum;

    private bool b_GetMouseButton;
    private int mouseIndex = -1;


    private void OnEnable()
    {
        Init();
        // 不再啟動 coroutine，改為 Update() 每幀呼叫 SyncShaderData()
    }

    /// <summary>
    /// 每幀將 inputPosition 轉換為世界座標、更新 SplashFX、複製 radiusOffsetTemp → radiusOffset。
    /// 原本是 coroutine 每 0.1s 執行一次，現改為每幀，讓 Shader 收到流暢的 radius 動畫。
    /// </summary>
    private void SyncShaderData()
    {
        currentPositionNum = inputPosition.Count;
        material.SetInt("currentPositionNum", Mathf.Min(currentPositionNum, maxPositionNum));

        if (lastPositionNum > currentPositionNum)
        {
            for (int i = currentPositionNum; i < lastPositionNum; i++)
                splashFXPool[i].SetActive(false);
        }

        int syncCount = Mathf.Min(currentPositionNum, maxPositionNum);
        for (int i = 0; i < syncCount; i++)
        {
            bool isParked = inputPosition[i].x < -1000f;
            maxPosition[i] = ConvertScreenToWorldPos(inputPosition[i]);
            if (!isParked)
                splashFXPool[i].transform.position = maxPosition[i] + new Vector4(0, 0, -0.7f, 0);
            splashFXPool[i].SetActive(!isParked);
            radiusOffset[i] = radiusOffsetTemp[i];
        }

        // 超過 maxPositionNum 的 overflow 處理（原始邏輯保留）
        if (currentPositionNum > maxPositionNum)
        {
            for (int i = maxPositionNum; i < currentPositionNum; i++)
            {
                var fxIndex = (i - maxPositionNum) % (currentPositionNum - maxPositionNum);
                splashFXPool[fxIndex + maxPositionNum].transform.position = inputPosition[i] + new Vector4(0, 0, -0.7f, 0);
                splashFXPool[fxIndex + maxPositionNum].SetActive(true);
            }
        }

        lastPositionNum = currentPositionNum;
    }

    private void Init()
    {
        if (splashFXPool == null || splashFXPool.Length == 0)
        {
            splashFXPool = new GameObject[splashFXPoolNum];
            for (int i = 0; i < splashFXPoolNum; i++)
            {
                splashFXPool[i] = Instantiate(splashFX, transform);
                splashFXPool[i].transform.eulerAngles = Vector3.zero;
                splashFXPool[i].SetActive(false);
            }
        }
        material.SetFloat("maxRadiusOffset", maxRadiusOffset);

        if (maxPosition == null || maxPosition.Length == 0)
            maxPosition = new Vector4[maxPositionNum];

        if (maxPositionLast == null || maxPositionLast.Length == 0)
            maxPositionLast = new Vector4[maxPositionNum];

        if (radiusOffset == null || radiusOffset.Length == 0)
            radiusOffset = new float[maxPositionNum];

        if (radiusOffsetTemp == null || radiusOffsetTemp.Length == 0)
            radiusOffsetTemp = new float[maxPositionNum];

        if (slotPressHeld == null || slotPressHeld.Length < maxPositionNum)
            slotPressHeld = new bool[maxPositionNum];

        if (slotFadingOut == null || slotFadingOut.Length < maxPositionNum)
            slotFadingOut = new bool[maxPositionNum];

        material.SetInt("maxPositionNum", maxPositionNum);
    }

    public Vector4 ConvertScreenToWorldPos(Vector4 inputScreenPos)
    {
        inputScreenPos.z = 10;
        if(setLeftUpperCornerAsOrigin) inputScreenPos.y = Screen.height - inputScreenPos.y;
        return Camera.main.ScreenToWorldPoint(inputScreenPos);
    }


    void Update(){

        if (inputPosDetect)
        {
            for (int i = 0; i < Mathf.Min(20, inputPosition.Count); i++)
            {
                if (inputPosition[i].w == 0)
                {
                    if (currentPositionNum < maxPositionNum)
                    {
                        if (i > currentPositionNum - 1) break;
                    }

                    // 停車格（-9999）：radius 強制最小，不讓閒置 slot 累積到最大
                    if (inputPosition[i].x < -1000f)
                    {
                        radiusOffsetTemp[i] = -0.2f;
                        maxPositionLast[i]  = inputPosition[i];
                    }
                    // 淡出狀態：緩慢縮小 radius，視覺上消失
                    else if (i < slotFadingOut.Length && slotFadingOut[i])
                    {
                        if (radiusOffsetTemp[i] > -0.2f)
                            radiusOffsetTemp[i] -= Time.deltaTime * fadeOutSpeed;
                        else
                            radiusOffsetTemp[i] = -0.2f;
                        maxPositionLast[i] = inputPosition[i];
                    }
                    else
                    {
                        bool posStable = Mathf.Abs(inputPosition[i].x - maxPositionLast[i].x) < 0.01f;
                        bool holdOpen  = i < slotPressHeld.Length && slotPressHeld[i];

                        if (posStable || holdOpen)
                        {
                            if (radiusOffsetTemp[i] <= maxRadiusOffset)
                                radiusOffsetTemp[i] += Time.deltaTime * expandSpeed;
                            else
                                radiusOffsetTemp[i] = maxRadiusOffset;
                        }
                        else
                        {
                            if (radiusOffsetTemp[i] >= -0.2f)
                                radiusOffsetTemp[i] -= Time.deltaTime * shrinkSpeed;
                            else
                                radiusOffsetTemp[i] = -0.2f;
                        }

                        maxPositionLast[i] = inputPosition[i];
                    }
                }
                else if (inputPosition[i].w == -1)
                {
                    if (currentPositionNum < maxPositionNum)
                    {
                        if (i > currentPositionNum - 1) break;
                    }

                    if (radiusOffsetTemp[i] >= -0.2f)
                        radiusOffsetTemp[i] -= Time.deltaTime * fadeOutSpeed;
                    else
                    {
                        radiusOffsetTemp[i] = -0.2f;
                        splashFXPool[i].SetActive(false);
                        inputPosition.RemoveAt(i);
                    }
                }
            }

            if (prepareToTestByMouse)
            {
                if (inputPosition.Count != 0)
                {
                    for (int i = 0; i < inputPosition.Count; i++)
                        inputPosition[i] += new Vector4(0, 0, 0, -1);
                }
                prepareToTestByMouse = false;
            }
        }

        if(testByMouse)
        {
            if (inputPosDetect)
            {
                inputPosDetect = false;
            }

            if (Input.GetMouseButton(0))
            {
                Vector3 screenPoint = Input.mousePosition;
                screenPoint.z = 10;

                if (!b_GetMouseButton)
                {
                    inputPosition.Add(screenPoint);
                    mouseIndex = inputPosition.Count - 1;
                    b_GetMouseButton = true;
                }
                inputPosition[mouseIndex] = screenPoint;



                for (int i = mouseIndex; i < mouseIndex + 1; i++)
                {
                    if (Mathf.Abs(ConvertScreenToWorldPos(inputPosition[i]).x - maxPositionLast[i].x) < 0.01f)
                    {
                        if (radiusOffsetTemp[i] <= maxRadiusOffset)
                            radiusOffsetTemp[i] += Time.deltaTime * expandSpeed;
                        else
                            radiusOffsetTemp[i] = maxRadiusOffset;
                    }
                    else
                    {
                        if (radiusOffsetTemp[i] >= -0.2f)
                            radiusOffsetTemp[i] -= Time.deltaTime * shrinkSpeed;
                        else
                            radiusOffsetTemp[i] = -0.2f;
                    }

                    maxPositionLast[i] = ConvertScreenToWorldPos(inputPosition[i]);
                }
            }
            else
            {
                if (mouseIndex != -1)
                {
                    for (int i = mouseIndex; i < mouseIndex + 1; i++)
                    {
                        if (currentPositionNum < maxPositionNum)
                        {
                            if (i > currentPositionNum - 1) break;
                        }

                        if (radiusOffsetTemp[i] >= -0.2f)
                            radiusOffsetTemp[i] -= Time.deltaTime * fadeOutSpeed;
                        else
                        {
                            if (b_GetMouseButton)
                            {
                                radiusOffsetTemp[i] = -0.2f;
                                splashFXPool[i].SetActive(false);
                                inputPosition.RemoveAt(i);
                                mouseIndex = -1;
                            }
                            b_GetMouseButton = false;
                        }
                    }
                }
            }
        }

        // 每幀同步位置、SplashFX、radius 到 Shader（原本 coroutine 每 0.1s 才執行）
        SyncShaderData();
        material.SetVectorArray(positionProperty, maxPosition);
        material.SetFloatArray(radiusProperty, radiusOffset);
    }
}
