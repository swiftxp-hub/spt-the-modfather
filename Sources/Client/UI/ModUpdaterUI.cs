using UnityEngine;
using UnityEngine.UI;

namespace SwiftXP.SPT.TheModfather.Client.UI;

public class ModUpdaterUI : MonoBehaviour
    {
        // =================================================================================
        // KONFIGURATION & KONSTANTEN
        // =================================================================================
        
        // Basis-Größe bei 1920x1080 (wird automatisch skaliert)
        private Rect windowRect = new Rect(0, 0, 600, 220); 

        // Layout-Abstände und Größen
        private const float BAR_HEIGHT = 40f;   
        private const int HEADER_SIZE = 20;     
        private const int STATUS_SIZE = 12;     
        private const int FOOTER_SIZE = 12;     

        // Texte
        private const string WINDOW_TITLE = "It's not personal, PMC. It's strictly updating...";
        private const string FOOTER_TEXT = "The game will close automatically. An external tool will finish the update.";

        // Farben (EFT Palette)
        private readonly Color c_tarkovBgDark = new Color(0.12f, 0.12f, 0.14f, 1f); // Dunkles Grau
        private readonly Color c_tarkovBarBg  = new Color(0.07f, 0.07f, 0.07f, 1f); // Fast Schwarz
        private readonly Color c_tarkovGold   = new Color(0.6f, 0.54f, 0.4f, 1f);   // Tarkov Gold
        private readonly Color c_textLight    = new Color(0.9f, 0.9f, 0.9f, 1f);    // Fast Weiß
        private readonly Color c_textGray     = new Color(0.6f, 0.6f, 0.6f, 1f);    // Footer Grau

        // =================================================================================
        // STATE VARIABLES
        // =================================================================================
        
        private float currentProgress = 0f;
        private string loadingText = "Initializing...";
        private bool isVisible = false;

        // Styles (werden in InitStyles erstellt)
        private GUIStyle windowStyle;
        private GUIStyle headerStyle;
        private GUIStyle statusStyle;
        private GUIStyle footerStyle;
        private GUIStyle backgroundBarStyle;
        private GUIStyle foregroundBarStyle;

        // Referenz auf den Input-Blocker (Canvas)
        private GameObject blockerObject;

        // =================================================================================
        // UNITY LIFECYCLE
        // =================================================================================

        void Awake()
        {
            // Initial zentrieren (wird im OnGUI nochmal für Skalierung korrigiert)
            windowRect.x = (Screen.width - windowRect.width) / 2;
            windowRect.y = (Screen.height - windowRect.height) / 2;
        }

        // =================================================================================
        // PUBLIC API
        // =================================================================================

        public void UpdateProgress(float progress, string text)
        {
            currentProgress = Mathf.Clamp01(progress);
            loadingText = text;

            if (!isVisible)
            {
                isVisible = true;
                CreateInputBlocker(); // Blocker aktivieren
            }
        }

        public void Hide()
        {
            isVisible = false;
            DestroyInputBlocker(); // Blocker entfernen
        }

        // =================================================================================
        // ONGUI (IMMEDIATE MODE GUI) - Zeichnet das Fenster
        // =================================================================================

        void OnGUI()
        {
            if (!isVisible) return;
            if (windowStyle == null) InitStyles();

            // 1. Skalierung berechnen (Basis 1920x1080)
            // Wir nehmen den kleineren Faktor, um das Seitenverhältnis zu wahren
            float scaleX = Screen.width / 1920f;
            float scaleY = Screen.height / 1080f;
            float scale = Mathf.Min(scaleX, scaleY);

            // 2. Matrix für Skalierung setzen
            Matrix4x4 oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));

            // 3. Position neu berechnen (basierend auf virtueller Auflösung)
            float virtualWidth = Screen.width / scale;
            float virtualHeight = Screen.height / scale;
            
            windowRect.x = (virtualWidth - windowRect.width) / 2;
            windowRect.y = (virtualHeight - windowRect.height) / 2;

            // 4. Draw Order: Ganz nach vorne
            GUI.depth = -100;

            // 5. Fenster zeichnen
            // Titel ist leer (""), da wir ihn manuell zeichnen für bessere Kontrolle
            windowRect = GUI.Window(12345, windowRect, DrawWindowContent, "", windowStyle);

            // 6. Matrix zurücksetzen
            GUI.matrix = oldMatrix;
        }

        void DrawWindowContent(int windowID)
        {
            float paddingX = 30f;
            float contentWidth = windowRect.width - (paddingX * 2);
            float currentY = 25f; // Start Y-Position

            // --- HEADER ---
            Rect headerRect = new Rect(paddingX, currentY, contentWidth, 35);
            GUI.Label(headerRect, WINDOW_TITLE, headerStyle);
            currentY += 45f;

            // --- STATUS TEXT ---
            Rect statusRect = new Rect(paddingX, currentY, contentWidth, 30);
            GUI.Label(statusRect, $"{loadingText.ToUpper()} [{(int)(currentProgress * 100)}%]", statusStyle);
            currentY += 40f;

            // --- PROGRESS BAR ---
            // Hintergrund
            Rect barBgRect = new Rect(paddingX, currentY, contentWidth, BAR_HEIGHT);
            GUI.Box(barBgRect, "", backgroundBarStyle);

            // Vordergrund (Gold)
            float currentBarWidth = contentWidth * currentProgress;
            if (currentBarWidth > 2)
            {
                // 1 Pixel Offset für den "Rahmen"-Effekt
                Rect barFgRect = new Rect(barBgRect.x + 1, barBgRect.y + 1, currentBarWidth - 2, BAR_HEIGHT - 2);
                GUI.Box(barFgRect, "", foregroundBarStyle);
            }
            currentY += BAR_HEIGHT + 20f;

            // --- FOOTER ---
            Rect footerRect = new Rect(paddingX, currentY, contentWidth, 25);
            GUI.Label(footerRect, FOOTER_TEXT, footerStyle);
        }

        // =================================================================================
        // STYLING
        // =================================================================================

        private void InitStyles()
        {
            // Fenster Style (Flacher Hintergrund, kein Rand)
            windowStyle = new GUIStyle(GUI.skin.window);
            Texture2D bgTex = MakeTex(2, 2, c_tarkovBgDark);
            windowStyle.normal.background = bgTex;
            windowStyle.onNormal.background = bgTex;
            windowStyle.focused.background = bgTex;
            windowStyle.onFocused.background = bgTex;
            windowStyle.border = new RectOffset(0, 0, 0, 0);

            // Header Style
            headerStyle = new GUIStyle(GUI.skin.label);
            headerStyle.alignment = TextAnchor.MiddleCenter;
            headerStyle.fontSize = HEADER_SIZE;
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.normal.textColor = c_textLight;

            // Status Style
            statusStyle = new GUIStyle(GUI.skin.label);
            statusStyle.alignment = TextAnchor.MiddleCenter;
            statusStyle.fontSize = STATUS_SIZE;
            statusStyle.fontStyle = FontStyle.Bold;
            statusStyle.normal.textColor = c_tarkovGold;

            // Footer Style
            footerStyle = new GUIStyle(GUI.skin.label);
            footerStyle.alignment = TextAnchor.MiddleCenter;
            footerStyle.fontSize = FOOTER_SIZE;
            footerStyle.fontStyle = FontStyle.Italic;
            footerStyle.normal.textColor = c_textGray;

            // Balken Styles
            backgroundBarStyle = new GUIStyle(GUI.skin.box);
            backgroundBarStyle.normal.background = MakeTex(2, 2, c_tarkovBarBg);
            backgroundBarStyle.border = new RectOffset(0, 0, 0, 0);

            foregroundBarStyle = new GUIStyle(GUI.skin.box);
            foregroundBarStyle.normal.background = MakeTex(2, 2, c_tarkovGold);
            foregroundBarStyle.border = new RectOffset(0, 0, 0, 0);
        }

        // =================================================================================
        // INPUT BLOCKER (Unity UI System)
        // =================================================================================

        private void CreateInputBlocker()
        {
            if (blockerObject != null) return;

            // 1. GameObject erstellen
            blockerObject = new GameObject("MyModInputBlocker_Canvas");
            DontDestroyOnLoad(blockerObject);

            // 2. Canvas Setup
            Canvas canvas = blockerObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000; // Überlagert EFT UI

            // 3. Scaler Setup (Damit der Blocker auch bei 4K/720p funktioniert)
            CanvasScaler scaler = blockerObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // 4. Raycaster (Fängt Klicks ab)
            blockerObject.AddComponent<GraphicRaycaster>();

            // 5. Schwarzes Panel
            GameObject panel = new GameObject("BlockerPanel");
            panel.transform.SetParent(blockerObject.transform, false);

            Image img = panel.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0.85f); // 85% Schwarz
            img.raycastTarget = true; // WICHTIG: Blockiert Raycasts

            // Vollbild
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

        // =================================================================================
        // HILFSFUNKTIONEN
        // =================================================================================

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; ++i) pix[i] = col;
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }
    }