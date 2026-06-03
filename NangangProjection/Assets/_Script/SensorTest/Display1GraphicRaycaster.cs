using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// ▶ 防誤觸保護（光域704場景）─ 辨識地板踩踏注入的滑鼠事件，防止誤觸控制台按鈕。
/// 停用此元件 → 踩踏可能誤觸控制台按鈕。
///
/// Display 1 raycaster that ignores held left-click input only when it is
/// explicitly identified as Windows injected input.
/// </summary>
[RequireComponent(typeof(Canvas))]
public class Display1GraphicRaycaster : GraphicRaycaster
{
    const int  WH_MOUSE_LL    = 14;
    const int  SM_CXSCREEN    = 0;
    const int  SM_CYSCREEN    = 1;
    const uint LLMHF_INJECTED = 0x01;
    const uint WM_LBUTTONDOWN = 0x0201;
    const uint WM_LBUTTONUP   = 0x0202;

    delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

    enum LeftButtonSource
    {
        Unknown,
        Real,
        Injected
    }

    [StructLayout(LayoutKind.Sequential)]
    struct POINT
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct MSLLHOOKSTRUCT
    {
        public POINT  pt;
        public uint   mouseData;
        public uint   flags;
        public uint   time;
        public IntPtr dwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll")]
    static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    static extern int GetSystemMetrics(int nIndex);

    static LowLevelMouseProc _hookProc;
    static IntPtr            _hookId = IntPtr.Zero;
    static bool              _hookAvailable;
    static int               _activeRaycasterCount;
    static LeftButtonSource  _leftButtonSource = LeftButtonSource.Unknown;
    static POINT             _leftButtonDownPoint;

    // source-based suppression：sensor 合成事件後短暫讓 Display 1 raycast 回空
    static bool  _realLeftDown;
    static float _sensorSuppressUntil;   // Time.realtimeSinceStartup 時間點
    const float  SensorSuppressWindow = 0.25f;

    bool _registeredHookUser;

    protected override void OnEnable()
    {
        base.OnEnable();

        if (_registeredHookUser) return;
        _registeredHookUser = true;
        _activeRaycasterCount++;
        InstallHook();
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        UnregisterHookUser();
    }

    void OnDestroy() => UnregisterHookUser();

    void UnregisterHookUser()
    {
        if (!_registeredHookUser) return;

        _registeredHookUser = false;
        _activeRaycasterCount = Mathf.Max(0, _activeRaycasterCount - 1);
        if (_activeRaycasterCount == 0)
            RemoveHook();
    }

    static void InstallHook()
    {
        if (_hookId != IntPtr.Zero) return;

        _hookProc = HookCallback;
        _hookId   = SetWindowsHookEx(WH_MOUSE_LL, _hookProc, IntPtr.Zero, 0);
        _hookAvailable = _hookId != IntPtr.Zero;

        if (!_hookAvailable)
            Debug.LogWarning($"[D1Raycaster] WH_MOUSE_LL install failed (err {Marshal.GetLastWin32Error()}); Display 1 UI input will stay enabled.");
        else
            Debug.Log("[D1Raycaster] WH_MOUSE_LL installed for Display 1 input filtering.");
    }

    static void RemoveHook()
    {
        if (_hookId == IntPtr.Zero) return;

        UnhookWindowsHookEx(_hookId);
        _hookId           = IntPtr.Zero;
        _hookAvailable    = false;
        _leftButtonSource = LeftButtonSource.Unknown;
    }

    static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var  hs         = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
            bool isInjected = (hs.flags & LLMHF_INJECTED) != 0;
            bool isSensorInput = hs.dwExtraInfo.ToInt64() == SensorClickSimulator.SensorInputExtraInfo;
            uint msg        = (uint)wParam.ToInt64();

            if (msg == WM_LBUTTONDOWN)
            {
                _leftButtonSource = isInjected ? LeftButtonSource.Injected : LeftButtonSource.Real;
                _leftButtonDownPoint = hs.pt;
                _realLeftDown = !isSensorInput;
                if (isSensorInput) MarkSensorPointerActivity();
            }
            else if (msg == WM_LBUTTONUP)
            {
                if (isSensorInput) MarkSensorPointerActivity();
                else _realLeftDown = false;
                _leftButtonSource = LeftButtonSource.Unknown;
            }
        }

        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    static void MarkSensorPointerActivity()
    {
        _sensorSuppressUntil = Time.realtimeSinceStartup + SensorSuppressWindow;
    }

    /// <summary>
    /// 由 SensorClickSimulator 在每次 SendInput 之前主動呼叫，
    /// 不依賴 hook 時序，確保 suppression 在 OS 事件抵達 Unity 前已設定。
    /// </summary>
    public static void NotifySensorClick()
    {
        _sensorSuppressUntil = Time.realtimeSinceStartup + SensorSuppressWindow;
    }

    public static bool ShouldSuppressDisplay1Pointer()
    {
        return !_realLeftDown && Time.realtimeSinceStartup < _sensorSuppressUntil;
    }

    static bool IsPointOnPrimaryDisplay(POINT point)
    {
        int width  = GetSystemMetrics(SM_CXSCREEN);
        int height = GetSystemMetrics(SM_CYSCREEN);

        if (width <= 0) width = Screen.width;
        if (height <= 0) height = Screen.height;

        return point.x >= 0 &&
               point.y >= 0 &&
               point.x < width &&
               point.y < height;
    }

    public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
    {
        if (ShouldSuppressDisplay1Pointer()) return;

        // Block only confirmed injected left-button holds that started outside
        // the primary display. This filters Display 2 sensor clicks while still
        // allowing tools/remoting that report Display 1 clicks as injected.
        if (_hookAvailable &&
            Input.GetMouseButton(0) &&
            _leftButtonSource == LeftButtonSource.Injected &&
            !IsPointOnPrimaryDisplay(_leftButtonDownPoint))
        {
            return;
        }

        base.Raycast(eventData, resultAppendList);
    }
}
