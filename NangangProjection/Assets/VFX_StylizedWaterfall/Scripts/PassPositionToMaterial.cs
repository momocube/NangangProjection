using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//[ExecuteInEditMode]
public class PassPositionToMaterial : MonoBehaviour {

    [SerializeField] private Material material;
    [SerializeField] private float maxRadiusOffset = 0.2f;

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
        StartCoroutine(PassPositionToShader(0.1f));
    }

    IEnumerator PassPositionToShader(float timeInterval)
    {
        while (true)
        {
            currentPositionNum = inputPosition.Count;
            material.SetInt("currentPositionNum", Mathf.Min(currentPositionNum, maxPositionNum));

            //闽超Ωゑヘ玡盎代翴ㄓ疭
            if (lastPositionNum > currentPositionNum)
            {
                for (int i = currentPositionNum; i < lastPositionNum; i++)
                {
                    splashFXPool[i].SetActive(false);
                }
            }

            //ヘ玡盎代翴计秖程(20)盎代翴计秖
            if (currentPositionNum < maxPositionNum)
            {
                for (int i = 0; i < currentPositionNum; i++)
                {
                    maxPosition[i] = ConvertScreenToWorldPos(inputPosition[i]);
                    splashFXPool[i].transform.position = maxPosition[i] + new Vector4(0,0,-0.7f,0);
                    splashFXPool[i].SetActive(true);

                    radiusOffset[i] = radiusOffsetTemp[i];
                }
            }
            else
            {
                for (int i = 0; i < maxPositionNum; i++)
                {
                    maxPosition[i] = ConvertScreenToWorldPos(inputPosition[i]);
                    splashFXPool[i].transform.position = maxPosition[i] + new Vector4(0, 0, -0.7f, 0);
                    splashFXPool[i].SetActive(true);

                    radiusOffset[i] = radiusOffsetTemp[i];
                }

                for (int i = maxPositionNum; i < currentPositionNum; i++)
                {
                    var fxIndex = (i - maxPositionNum) % (currentPositionNum - maxPositionNum);
                    splashFXPool[fxIndex + maxPositionNum].transform.position = inputPosition[i] + new Vector4(0, 0, -0.7f, 0);
                    splashFXPool[fxIndex + maxPositionNum].SetActive(true);
                }
            }

            lastPositionNum = currentPositionNum;

            yield return new WaitForSeconds(timeInterval);
        }
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

        material.SetInt("maxPositionNum", maxPositionNum);
    }

    public Vector4 ConvertScreenToWorldPos(Vector4 inputScreenPos)
    {
        inputScreenPos.z = 10;
        if(setLeftUpperCornerAsOrigin) inputScreenPos.y = Screen.height - inputScreenPos.y;
        return Camera.main.ScreenToWorldPoint(inputScreenPos);
    }


    void Update(){

        material.SetVectorArray(positionProperty, maxPosition);

        if (inputPosDetect)
        {
            for (int i = 0; i < Mathf.Min(20, inputPosition.Count); i++)
            {
                if (inputPosition[i].w == 0)
                {
                    //ヘ玡盎代翴计秖程(20)盎代翴计秖
                    if (currentPositionNum < maxPositionNum)
                    {
                        if (i > currentPositionNum - 1) break;
                    }

                    if (Mathf.Abs(inputPosition[i].x - maxPositionLast[i].x) < 0.01f)
                    {
                        if (radiusOffsetTemp[i] <= maxRadiusOffset)
                            radiusOffsetTemp[i] += Time.deltaTime * 1.5f;
                        else
                            radiusOffsetTemp[i] = maxRadiusOffset;
                    }
                    else
                    {
                        if (radiusOffsetTemp[i] >= -0.2f)
                            radiusOffsetTemp[i] -= Time.deltaTime * 10.0f;
                        else
                            radiusOffsetTemp[i] = -0.2f;
                    }

                    maxPositionLast[i] = inputPosition[i];
                }
                else if (inputPosition[i].w == -1)
                {
                    //ヘ玡盎代翴计秖程(20)盎代翴计秖
                    if (currentPositionNum < maxPositionNum)
                    {
                        if (i > currentPositionNum - 1) break;
                    }

                    if (radiusOffsetTemp[i] >= -0.2f)
                        radiusOffsetTemp[i] -= Time.deltaTime * 0.8f;
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

        if(testByMouse) //ノ菲公代刚
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
                            radiusOffsetTemp[i] += Time.deltaTime * 1.5f;
                        else
                            radiusOffsetTemp[i] = maxRadiusOffset;
                    }
                    else
                    {
                        if (radiusOffsetTemp[i] >= -0.2f)
                            radiusOffsetTemp[i] -= Time.deltaTime * 10.0f;
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
                        //ヘ玡盎代翴计秖程(20)盎代翴计秖
                        if (currentPositionNum < maxPositionNum)
                        {
                            if (i > currentPositionNum - 1) break;
                        }

                        if (radiusOffsetTemp[i] >= -0.2f)
                            radiusOffsetTemp[i] -= Time.deltaTime * 0.8f;
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
        material.SetFloatArray(radiusProperty, radiusOffset);
    }
}
