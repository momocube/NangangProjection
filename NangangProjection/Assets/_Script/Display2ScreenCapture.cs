using System;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// 每幀用 GDI BitBlt 截取 Display 2 整個螢幕，輸出為 Texture2D。
/// 掛在場景任意 GameObject 上，ConsoleGameOverlay 自動偵測並使用。
/// </summary>
public class Display2ScreenCapture : MonoBehaviour
{
    [Header("Display 2 螢幕座標（Windows 虛擬桌面）")]
    [Tooltip("Display 2 左上角 X（通常 = Display 1 寬度，預設 1920）")]
    public int display2X = 1920;
    [Tooltip("Display 2 左上角 Y（通常為 0）")]
    public int display2Y = 0;
    [Tooltip("Display 2 螢幕寬度（像素）")]
    public int display2Width = 1920;
    [Tooltip("Display 2 螢幕高度（像素）")]
    public int display2Height = 1080;

    [Header("效能設定")]
    [Range(1, 60)]
    [Tooltip("截圖更新頻率（fps）")]
    public int captureFPS = 30;

    public static Display2ScreenCapture Instance { get; private set; }
    public Texture2D CaptureTexture { get; private set; }

    private float _timer;
    private byte[] _pixels;

    // ── GDI P/Invoke ──────────────────────────────────────────────────────────

    [DllImport("user32.dll")] static extern IntPtr GetDC(IntPtr hwnd);
    [DllImport("user32.dll")] static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);
    [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleDC(IntPtr hdc);
    [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int w, int h);
    [DllImport("gdi32.dll")] static extern IntPtr SelectObject(IntPtr hdc, IntPtr hObj);
    [DllImport("gdi32.dll")] static extern bool BitBlt(IntPtr hdcDst, int xDst, int yDst, int w, int h, IntPtr hdcSrc, int xSrc, int ySrc, uint rop);
    [DllImport("gdi32.dll")] static extern bool DeleteDC(IntPtr hdc);
    [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr hObj);
    [DllImport("gdi32.dll")] static extern int GetDIBits(IntPtr hdc, IntPtr hbm, int start, int lines, byte[] bits, ref BITMAPINFO bmi, uint usage);

    const uint SRCCOPY = 0x00CC0020;

    [StructLayout(LayoutKind.Sequential)]
    struct BITMAPINFOHEADER
    {
        public int    biSize, biWidth, biHeight;
        public short  biPlanes, biBitCount;
        public int    biCompression, biSizeImage;
        public int    biXPelsPerMeter, biYPelsPerMeter;
        public int    biClrUsed, biClrImportant;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct BITMAPINFO
    {
        public BITMAPINFOHEADER bmiHeader;
        public int              bmiColors;
    }

    // ── Unity 生命週期 ────────────────────────────────────────────────────────

    void Awake()
    {
        Instance = this;
        CaptureTexture = new Texture2D(display2Width, display2Height, TextureFormat.RGB24, false);
        _pixels = new byte[display2Width * display2Height * 3];
    }

    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer < 1f / captureFPS) return;
        _timer = 0f;
        Capture();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ── 截圖核心 ──────────────────────────────────────────────────────────────

    void Capture()
    {
        IntPtr desktopDC = GetDC(IntPtr.Zero);
        IntPtr memDC     = CreateCompatibleDC(desktopDC);
        IntPtr hBmp      = CreateCompatibleBitmap(desktopDC, display2Width, display2Height);
        IntPtr old       = SelectObject(memDC, hBmp);

        BitBlt(memDC, 0, 0, display2Width, display2Height, desktopDC, display2X, display2Y, SRCCOPY);

        var bmi = new BITMAPINFO();
        bmi.bmiHeader.biSize        = Marshal.SizeOf(typeof(BITMAPINFOHEADER));
        bmi.bmiHeader.biWidth       = display2Width;
        bmi.bmiHeader.biHeight      = display2Height; // 正值 = bottom-up，與 Unity Texture2D 一致
        bmi.bmiHeader.biPlanes      = 1;
        bmi.bmiHeader.biBitCount    = 24;
        bmi.bmiHeader.biCompression = 0; // BI_RGB

        GetDIBits(memDC, hBmp, 0, display2Height, _pixels, ref bmi, 0);

        // GDI 回傳 BGR，Unity RGB24 需要 BGR→RGB
        for (int i = 0; i < _pixels.Length; i += 3)
        {
            byte b = _pixels[i];
            _pixels[i]     = _pixels[i + 2];
            _pixels[i + 2] = b;
        }

        CaptureTexture.LoadRawTextureData(_pixels);
        CaptureTexture.Apply();

        SelectObject(memDC, old);
        DeleteObject(hBmp);
        DeleteDC(memDC);
        ReleaseDC(IntPtr.Zero, desktopDC);
    }
}
