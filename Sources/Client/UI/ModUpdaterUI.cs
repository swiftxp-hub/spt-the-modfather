using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace SwiftXP.SPT.TheModfather.Client.UI;

public class ModUpdaterUI : MonoBehaviour
{
    private Rect windowRect = new(0, 0, 600, 220);

    private const float BAR_HEIGHT = 40f;
    private const int HEADER_SIZE = 18;
    private const int STATUS_SIZE = 12;
    private const int FOOTER_SIZE = 12;

    private const string WINDOW_TITLE = "It's not personal, PMC. It's strictly updating...";

    private readonly Color c_tarkovBgDark = new(0.12f, 0.12f, 0.14f, 1f);
    private readonly Color c_tarkovBarBg = new(0.07f, 0.07f, 0.07f, 1f);
    private readonly Color c_tarkovGold = new(0.6f, 0.54f, 0.4f, 1f);
    private readonly Color c_textLight = new(0.9f, 0.9f, 0.9f, 1f);
    private readonly Color c_textGray = new(0.6f, 0.6f, 0.6f, 1f);

    private float currentProgress;
    private string loadingText = "Initializing...";
    private string footerText = string.Empty;

    private bool isVisible;

    private GUIStyle? windowStyle;
    private GUIStyle? headerStyle;
    private GUIStyle? statusStyle;
    private GUIStyle? footerStyle;
    private GUIStyle? backgroundBarStyle;
    private GUIStyle? foregroundBarStyle;

    private GameObject? blockerObject;

    void Awake()
    {
        windowRect.x = (Screen.width - windowRect.width) / 2;
        windowRect.y = (Screen.height - windowRect.height) / 2;
    }

    public void UpdateProgress(float progress, string text)
    {
        currentProgress = Mathf.Clamp01(progress);
        loadingText = text;

        if (!isVisible)
        {
            isVisible = true;
            CreateInputBlocker();
        }
    }

    public void UpdateFooter(string text)
    {
        footerText = text;

        if (!isVisible)
        {
            isVisible = true;
            CreateInputBlocker();
        }
    }

    public void Hide()
    {
        isVisible = false;
        DestroyInputBlocker();
    }

    void OnGUI()
    {
        if (!isVisible)
            return;

        if (windowStyle == null)
            InitStyles();

        float scaleX = Screen.width / 1920f;
        float scaleY = Screen.height / 1080f;
        float scale = Mathf.Min(scaleX, scaleY);

        Matrix4x4 oldMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));

        float virtualWidth = Screen.width / scale;
        float virtualHeight = Screen.height / scale;

        windowRect.x = (virtualWidth - windowRect.width) / 2;
        windowRect.y = (virtualHeight - windowRect.height) / 2;

        GUI.depth = -100;

        windowRect = GUI.Window(12345, windowRect, DrawWindowContent, "", windowStyle);

        GUI.matrix = oldMatrix;
    }

    void DrawWindowContent(int windowID)
    {
        float paddingX = 30f;
        float contentWidth = windowRect.width - (paddingX * 2);
        float currentY = 25f;

        Rect headerRect = new(paddingX, currentY, contentWidth, 35);
        GUI.Label(headerRect, WINDOW_TITLE, headerStyle);
        currentY += 45f;

        Rect statusRect = new(paddingX, currentY, contentWidth, 30);
        GUI.Label(statusRect, $"{loadingText.ToUpper(CultureInfo.InvariantCulture)} [{(int)(currentProgress * 100)}%]", statusStyle);
        currentY += 40f;

        Rect barBgRect = new(paddingX, currentY, contentWidth, BAR_HEIGHT);
        GUI.Box(barBgRect, "", backgroundBarStyle);

        float currentBarWidth = contentWidth * currentProgress;
        if (currentBarWidth > 2)
        {
            Rect barFgRect = new(barBgRect.x + 1, barBgRect.y + 1, currentBarWidth - 2, BAR_HEIGHT - 2);
            GUI.Box(barFgRect, "", foregroundBarStyle);
        }

        currentY += BAR_HEIGHT + 20f;

        Rect footerRect = new(paddingX, currentY, contentWidth, 25);
        GUI.Label(footerRect, footerText, footerStyle);
    }

    private void InitStyles()
    {
        windowStyle = new GUIStyle(GUI.skin.window);
        Texture2D bgTex = MakeTex(2, 2, c_tarkovBgDark);
        windowStyle.normal.background = bgTex;
        windowStyle.onNormal.background = bgTex;
        windowStyle.focused.background = bgTex;
        windowStyle.onFocused.background = bgTex;
        windowStyle.border = new RectOffset(0, 0, 0, 0);

        headerStyle = new GUIStyle(GUI.skin.label);
        headerStyle.alignment = TextAnchor.MiddleCenter;
        headerStyle.fontSize = HEADER_SIZE;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.normal.textColor = c_textLight;

        statusStyle = new GUIStyle(GUI.skin.label);
        statusStyle.alignment = TextAnchor.MiddleCenter;
        statusStyle.fontSize = STATUS_SIZE;
        statusStyle.fontStyle = FontStyle.Bold;
        statusStyle.normal.textColor = c_tarkovGold;

        footerStyle = new GUIStyle(GUI.skin.label);
        footerStyle.alignment = TextAnchor.MiddleCenter;
        footerStyle.fontSize = FOOTER_SIZE;
        footerStyle.fontStyle = FontStyle.Italic;
        footerStyle.normal.textColor = c_textGray;

        backgroundBarStyle = new GUIStyle(GUI.skin.box);
        backgroundBarStyle.normal.background = MakeTex(2, 2, c_tarkovBarBg);
        backgroundBarStyle.border = new RectOffset(0, 0, 0, 0);

        foregroundBarStyle = new GUIStyle(GUI.skin.box);
        foregroundBarStyle.normal.background = MakeTex(2, 2, c_tarkovGold);
        foregroundBarStyle.border = new RectOffset(0, 0, 0, 0);
    }

    private void CreateInputBlocker()
    {
        if (blockerObject != null)
            return;

        blockerObject = new GameObject("TheModfatherInputBlocker_Canvas");
        DontDestroyOnLoad(blockerObject);

        Canvas canvas = blockerObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;

        CanvasScaler scaler = blockerObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        blockerObject.AddComponent<GraphicRaycaster>();

        GameObject panel = new GameObject("BlockerPanel");
        panel.transform.SetParent(blockerObject.transform, false);

        Image img = panel.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0.85f);
        img.raycastTarget = true;

        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private void DestroyInputBlocker()
    {
        if (blockerObject != null)
        {
            Destroy(blockerObject);
            blockerObject = null;
        }
    }

    private static Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];

        for (int i = 0; i < pix.Length; ++i)
            pix[i] = col;

        Texture2D result = new(width, height);

        result.SetPixels(pix);
        result.Apply();

        return result;
    }
}