using System;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class TransparentWindow : MonoBehaviour {

    [DllImport("user32.dll")]
    public static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT point);

    [DllImport("user32.dll")]
    private static extern bool ScreenToClient(IntPtr hWnd, ref POINT point);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    static extern int SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

    private struct MARGINS {
        public int cxLeftWidth;
        public int cxRightWidth;
        public int cyTopHeight;
        public int cyBottomHeight;
    }

    private struct POINT {
        public int x;
        public int y;
    }

    [DllImport("Dwmapi.dll")]
    private static extern uint DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS margins);

    const int GWL_EXSTYLE = -20;

    const uint WS_EX_LAYERED = 0x00080000;
    const uint WS_EX_TRANSPARENT = 0x00000020;

    static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);

    const uint LWA_COLORKEY = 0x00000001;

    private IntPtr hWnd;
    private readonly List<RaycastResult> uiRaycastResults = new List<RaycastResult>();
    private RectTransform menuRect;

    private void Start() {
        //MessageBox(new IntPtr(0), "Hello World!", "Hello Dialog", 0);

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        hWnd = GetActiveWindow();

        MARGINS margins = new MARGINS { cxLeftWidth = -1 };
        DwmExtendFrameIntoClientArea(hWnd, ref margins);

        SetWindowLong(hWnd, GWL_EXSTYLE, WS_EX_LAYERED | WS_EX_TRANSPARENT);
        //SetLayeredWindowAttributes(hWnd, 0, 0, LWA_COLORKEY);

        SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0, 0);
#endif

        Application.runInBackground = true;
    }

    private void Update() {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        Camera mainCamera = Camera.main;
        if (mainCamera == null) {
            return;
        }

        SetClickthrough(!IsPointerOverInteractiveContent(mainCamera));
#endif
    }

    public void SetMenu(RectTransform menu) {
        menuRect = menu;
    }

    private bool IsPointerOverInteractiveContent(Camera mainCamera) {
        Vector2 pointerPosition = GetPointerScreenPosition();
        if (menuRect != null && menuRect.gameObject.activeInHierarchy &&
            RectTransformUtility.RectangleContainsScreenPoint(menuRect, pointerPosition, mainCamera)) {
            return true;
        }

        if (Physics.Raycast(mainCamera.ScreenPointToRay(pointerPosition))) {
            return true;
        }

        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null) {
            return false;
        }

        uiRaycastResults.Clear();
        var pointerEventData = new PointerEventData(eventSystem) {
            position = pointerPosition
        };
        eventSystem.RaycastAll(pointerEventData, uiRaycastResults);
        return uiRaycastResults.Count > 0;
    }

    private Vector2 GetPointerScreenPosition() {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        if (GetCursorPos(out POINT point) && ScreenToClient(hWnd, ref point)) {
            return new Vector2(point.x, Screen.height - point.y);
        }
#endif
        return Input.mousePosition;
    }

    private void SetClickthrough(bool clickthrough) {
        if (clickthrough) {
            SetWindowLong(hWnd, GWL_EXSTYLE, WS_EX_LAYERED | WS_EX_TRANSPARENT);
        } else {
            SetWindowLong(hWnd, GWL_EXSTYLE, WS_EX_LAYERED);
        }
    }
}
