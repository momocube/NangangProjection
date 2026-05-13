using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Display 1 專用 GraphicRaycaster。
///
/// 安裝 WH_MOUSE_LL 全域滑鼠 Hook，在 OS 層攔截所有帶
/// SensorClickSimulator.SensorExtraInfo 標記的感應器 SendInput 事件：
///   • 透過 PostMessage 將點擊轉送到 Display 2 目標視窗
///   • 回傳 (IntPtr)(-1) 使原始事件不進入任何視窗的 message queue
/// 真實滑鼠（LLMHF_INJECTED 未設定）正常放行，Display 1 UI 只響應真實滑鼠。
/// </summary>
[RequireComponent(typeof(Canvas))]
public class Display1GraphicRaycaster : GraphicRaycaster
{
    const int  WH_MOUSE_LL    = 14;
    const uint LLMHF_INJECTED = 0x01;
    const uint WM_MOUSEMOVE   = 0x0200;
    const uint WM_LBUTTONDOWN = 0x0201;
    const uint WM_LBUTTONUP   = 0x0202;
    const uint MK_LBUTTON     = 0x0001;

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

    [DllImport("user32.dll")]
    static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    // 靜態：只裝一次 Hook，跨場景持續有效
    static LowLevelMouseProc _hookProc;          // 保住 delegate 不被 GC
    static IntPtr            _hookId    = IntPtr.Zero;
    static bool              _hookDragging;      // 追蹤感應器按下狀態，MOUSEMOVE 帶正確 wParam

    static IntPtr MakeLParam(int x, int y) => (IntPtr)((y << 16) | (x & 0xFFFF));

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
            Debug.Log("[D1Raycaster] WH_MOUSE_LL 安裝完成，感應器 SendInput 已與 Display 1 隔離");
    }

    static void RemoveHook()
    {
        if (_hookId == IntPtr.Zero) return;
        UnhookWindowsHookEx(_hookId);
        _hookId       = IntPtr.Zero;
        _hookDragging = false;
    }

    // ── Hook 回呼 ─────────────────────────────────────────────────────────

    static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var hs = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));

            bool isSensor = (hs.flags & LLMHF_INJECTED) != 0
                         && hs.dwExtraInfo.ToInt64() == SensorClickSimulator.SensorExtraInfo.ToInt64();

            if (isSensor)
            {
                uint msg  = (uint)wParam.ToInt64();
                var  hwnd = SensorClickSimulator.SensorTargetHwnd;

                // 更新拖曳狀態（供 MOUSEMOVE wParam 使用）
                if (msg == WM_LBUTTONDOWN) _hookDragging = true;
                if (msg == WM_LBUTTONUP)   _hookDragging = false;

                if (hwnd != IntPtr.Zero)
                {
                    var lp = MakeLParam(SensorClickSimulator.SensorTargetLocalX,
                                        SensorClickSimulator.SensorTargetLocalY);
                    switch (msg)
                    {
                        case WM_LBUTTONDOWN:
                            PostMessage(hwnd, WM_LBUTTONDOWN, (IntPtr)MK_LBUTTON, lp);
                            break;
                        case WM_LBUTTONUP:
                            PostMessage(hwnd, WM_LBUTTONUP, IntPtr.Zero, lp);
                            break;
                        case WM_MOUSEMOVE:
                            PostMessage(hwnd, WM_MOUSEMOVE,
                                        _hookDragging ? (IntPtr)MK_LBUTTON : IntPtr.Zero, lp);
                            break;
                    }
                }

                return (IntPtr)(-1); // 攔截：原始事件不送進任何視窗
            }
        }
        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    // ── Raycast（Hook 已在 OS 層完成隔離，此處直接放行真實滑鼠）────────────

    public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
        => base.Raycast(eventData, resultAppendList);
}
