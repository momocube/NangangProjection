using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Display 1 專用 GraphicRaycaster。
///
/// 以 WH_MOUSE_LL Hook 追蹤「真實硬體滑鼠左鍵」的按下狀態：
///   • 非 INJECTED 的 LEFTDOWN → _isRealMouseDown = true
///   • 非 INJECTED 的 LEFTUP   → _isRealMouseDown = false
///   • INJECTED（感應器 SendInput）→ 完全不影響 _isRealMouseDown
///
/// Raycast 過濾：
///   Input.GetMouseButton(0) 為 true 且 _isRealMouseDown 為 false
///   → 這個點擊來自感應器 → return（Display 1 無反應）
///   → 滑鼠 hover / 真實點擊 → base.Raycast 正常執行
///
/// 大量觸碰事件完全不影響 _isRealMouseDown，故多點同時觸碰也不干擾操作者。
/// </summary>
[RequireComponent(typeof(Canvas))]
public class Display1GraphicRaycaster : GraphicRaycaster
{
    const int  WH_MOUSE_LL    = 14;
    const uint LLMHF_INJECTED = 0x01;
    const uint WM_LBUTTONDOWN = 0x0201;
    const uint WM_LBUTTONUP   = 0x0202;

    delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    struct POINT { public int x, y; }

    [StructLayout(LayoutKind.Sequential)]
    struct MSLLHOOKSTRUCT
    {
        public POINT  pt;
        public uint   mouseData, flags, time;
        public IntPtr dwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll")]
    static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    static LowLevelMouseProc _hookProc;
    static IntPtr            _hookId         = IntPtr.Zero;
    static bool              _isRealMouseDown; // 真實硬體左鍵是否按住

    // ── 生命週期 ──────────────────────────────────────────────────────────

    protected override void OnEnable()
    {
        base.OnEnable();
        InstallHook();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        RemoveHook();
    }

    void OnDestroy() => RemoveHook();

    // ── Hook 安裝 / 卸除 ──────────────────────────────────────────────────

    static void InstallHook()
    {
        if (_hookId != IntPtr.Zero) return;
        _hookProc = HookCallback;
        _hookId   = SetWindowsHookEx(WH_MOUSE_LL, _hookProc, IntPtr.Zero, 0);
        if (_hookId == IntPtr.Zero)
            Debug.LogWarning($"[D1Raycaster] WH_MOUSE_LL 安裝失敗 (err {Marshal.GetLastWin32Error()})");
        else
            Debug.Log("[D1Raycaster] WH_MOUSE_LL 已啟動，Display 1 只響應真實滑鼠左鍵");
    }

    static void RemoveHook()
    {
        if (_hookId == IntPtr.Zero) return;
        UnhookWindowsHookEx(_hookId);
        _hookId          = IntPtr.Zero;
        _isRealMouseDown = false;
    }

    // ── Hook 回呼：只追蹤真實滑鼠狀態，所有事件原樣放行 ────────────────────

    static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var  hs     = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
            bool isReal = (hs.flags & LLMHF_INJECTED) == 0; // 非 INJECTED = 真實硬體
            uint msg    = (uint)wParam.ToInt64();

            if (isReal)
            {
                if (msg == WM_LBUTTONDOWN) _isRealMouseDown = true;
                if (msg == WM_LBUTTONUP)   _isRealMouseDown = false;
            }
            // INJECTED（感應器）：完全不改變 _isRealMouseDown
        }
        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    // ── Raycast 過濾 ──────────────────────────────────────────────────────

    public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
    {
        // Input.GetMouseButton(0) 為 true 表示左鍵正在被按（真實或感應器）
        // _isRealMouseDown 為 false 表示真實滑鼠沒有按下
        // → 兩者同時成立 = 感應器點擊，直接 return 不讓任何 UI 收到事件
        if (Input.GetMouseButton(0) && !_isRealMouseDown)
            return;

        base.Raycast(eventData, resultAppendList);
    }
}
