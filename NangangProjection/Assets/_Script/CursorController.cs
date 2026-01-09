using UnityEngine;

public class CursorController : MonoBehaviour
{
    // 是否在遊戲開始時自動隱藏
    public bool hideOnStart = true;

    void Start()
    {
        if (hideOnStart)
        {
            ToggleCursor(false);
        }
    }

    void Update()
    {
        // 按下大鍵盤的數字 5 (Alpha5)
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            // 讀取現在是否顯示，然後取反 (如果是開就關，是關就開)
            bool newState = !Cursor.visible;
            ToggleCursor(newState);
        }
    }

    void ToggleCursor(bool show)
    {
        // 1. 設定滑鼠可見度
        Cursor.visible = show;

        // 2. 關於鎖定狀態 (重要！)
        // 如果是要傳送 OSC 座標，我們'不能'鎖定滑鼠，只能隱藏。
        // 如果鎖定 (Locked)，滑鼠座標會卡在螢幕中間，您的 TD 就不會動了。

        if (!show)
        {
            // 隱藏時：不鎖定，但限制在視窗內 (避免滑鼠不小心點到視窗外面)
            Cursor.lockState = CursorLockMode.Confined;
        }
        else
        {
            // 顯示時：完全解除限制
            Cursor.lockState = CursorLockMode.None;
        }
    }
}