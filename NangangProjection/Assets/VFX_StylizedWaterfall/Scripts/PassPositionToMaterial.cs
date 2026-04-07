using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//[ExecuteInEditMode]
public class PassPositionToMaterial : MonoBehaviour {

    [SerializeField] private Material material;
    
    private string positionProperty = "_ObjectPosition";
    private string radiusProperty = "_ObjectRadius";
    [SerializeField] private float maxRadiusOffset = 0.2f;
    private float radiusOffset;
    private float radiusOffsetTemp = 0;

    //J
    [SerializeField] private List<Vector4> inputScreenPosition = new();
    [SerializeField] private List<Vector4> inputPosition = new();
    [SerializeField] private readonly int maxPositionNum = 20;
    [SerializeField] private int currentPositionNum;
    [SerializeField] private Vector4[] maxPosition;
    [SerializeField] private GameObject splashFX;
    [SerializeField] private int splashFXPoolNum = 40;
    [SerializeField] private GameObject[] splashFXPool;
    [SerializeField] private int lastPositionNum;


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

            if (Input.GetMouseButton(0) && b_GetMouseButton) 
                radiusOffset = radiusOffsetTemp;
            else if (!Input.GetMouseButton(0) && b_GetMouseButton)
                radiusOffset = radiusOffsetTemp;
            else
                radiusOffset = maxRadiusOffset;

                material.SetFloat(radiusProperty, radiusOffset);

            if (lastPositionNum > currentPositionNum)
            {
                for (int i = currentPositionNum; i < lastPositionNum; i++)
                {
                    splashFXPool[i].SetActive(false);
                }
            }
                

            if (currentPositionNum < maxPositionNum)
            {
                for (int i = 0; i < currentPositionNum; i++)
                {
                    maxPosition[i] = inputPosition[i];
                    splashFXPool[i].transform.position = maxPosition[i] + new Vector4(0,0,-0.7f,0);
                    splashFXPool[i].SetActive(true);
                }
            }
            else
            {
                for (int i = 0; i < maxPositionNum; i++)
                {
                    maxPosition[i] = inputPosition[i];
                    splashFXPool[i].transform.position = maxPosition[i] + new Vector4(0, 0, -0.7f, 0);
                    splashFXPool[i].SetActive(true);
                }

                for (int i = maxPositionNum; i < currentPositionNum; i++)
                {
                    var fxIndex = (i - maxPositionNum) % (currentPositionNum - maxPositionNum);
                    splashFXPool[fxIndex + maxPositionNum].transform.position = inputPosition[i] + new Vector4(0, 0, -0.7f, 0);
                    splashFXPool[fxIndex + maxPositionNum].SetActive(true);
                }
            }

            material.SetVectorArray(positionProperty, maxPosition);

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

        //if (radiusOffset == null || radiusOffset.Length == 0)
        //    radiusOffset = new float[maxPositionNum];

        material.SetInt("maxPositionNum", maxPositionNum);
    }

    private bool b_GetMouseButton;
    private int mouseIndex;
    private Vector3 mouseLastPos;
    void Update(){
        
        if (Input.GetMouseButton(0))
        {
            Vector3 screenPoint = Input.mousePosition;
            screenPoint.z = 10;

            if (!b_GetMouseButton)
            {
                inputPosition.Add(Camera.main.ScreenToWorldPoint(screenPoint));
                if(inputPosition.Count == 1)
                    mouseIndex = 0;
                else
                    mouseIndex = inputPosition.Count - 1;
                b_GetMouseButton = true;
            }
            
            inputPosition[mouseIndex] = Camera.main.ScreenToWorldPoint(screenPoint);

            if (Mathf.Abs(inputPosition[mouseIndex].x - mouseLastPos.x) < 0.01f)
            {
                if (radiusOffsetTemp <= maxRadiusOffset)
                    radiusOffsetTemp += Time.deltaTime * 1.5f;
                else
                    radiusOffsetTemp = maxRadiusOffset;
            }
            else
            {
                if (radiusOffsetTemp >= -0.2f)
                    radiusOffsetTemp -= Time.deltaTime * 10.0f;
                else
                    radiusOffsetTemp = -0.2f;
            }

            mouseLastPos = inputPosition[mouseIndex];
        }
        else
        {
            if (radiusOffsetTemp >= 0)
                radiusOffsetTemp -= Time.deltaTime * 0.8f;
            else
            {
                if (b_GetMouseButton)
                {
                    radiusOffsetTemp = 0;
                    splashFXPool[mouseIndex].SetActive(false);
                    inputPosition.RemoveAt(mouseIndex);
                }                    
                b_GetMouseButton = false;
            }
        }

    }

    private void LateUpdate()
    {
            
    }
}
