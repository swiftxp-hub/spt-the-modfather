using System;
using System.Collections.Generic;
using UnityEngine;
using SwiftXP.SPT.TheModfather.Client.Data;
using SwiftXP.SPT.TheModfather.Client.Enums;
using System.Globalization;

namespace SwiftXP.SPT.TheModfather.Client.UI
{
    public static class ModfatherUI
    {
        private const float ReferenceHeight = 1080f;
        private const float WindowWidth = 900f;
        private const float WindowHeight = 600f;

        private static Vector2 s_scrollPos = Vector2.zero;
        private static float s_scale = 1f;

        // Caching
        private static Texture2D? s_texScrollTrack;
        private static Texture2D? s_texScrollThumb;

        public static void Draw(
            PluginState pluginState,
            UpdateUiState updateUiState,
            Action onAccept,
            Action onDecline,
            Action onCancel,
            Action onErrorContinue)
        {
            s_scale = Screen.height / ReferenceHeight;
            Matrix4x4 oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(s_scale, s_scale, 1f));

            try
            {
                float virtualScreenWidth = Screen.width / s_scale;
                float virtualScreenHeight = Screen.height / s_scale;

                // Optionaler Background
                DrawRect(new Rect(0, 0, virtualScreenWidth, virtualScreenHeight), ModfatherUIColors.DimmerBackground);

                Rect winRect = new(
                    (virtualScreenWidth - WindowWidth) / 2f,
                    (virtualScreenHeight - WindowHeight) / 2f,
                    WindowWidth,
                    WindowHeight
                );

                DrawWindowFrame(winRect);
                DrawHeader(winRect, "THE MODFATHER // UPDATE-CHECK");

                // Status Text kommt jetzt aus dem UI-State Objekt
                DrawStatusText(winRect, updateUiState.StatusText);

                // --- LAYOUT ---
                float btnWidth = 260f;
                float btnHeight = 45f;
                float bottomMargin = 15f;
                float btnY = winRect.y + WindowHeight - btnHeight - bottomMargin;
                float sideMargin = 30f;

                float barWidth = winRect.width - 150f;
                float barHeight = 45f;
                Rect barRect = new Rect(
                    winRect.x + 75f,
                    winRect.y + (WindowHeight / 2f) - (barHeight / 2f) - 20f,
                    barWidth,
                    barHeight
                );

                // --- LOGIK WEICHE ---
                // Ableitung der Flags aus dem zentralen State Enum
                bool isError = pluginState == PluginState.Error;

                bool showProgress = pluginState == PluginState.CheckingForUpdates
                    || pluginState == PluginState.Updating
                    || pluginState == PluginState.Initializing
                    || pluginState == PluginState.UpdateComplete;

                bool showButtons = pluginState == PluginState.UpdateAvailable;

                // A) FEHLER (Höchste Priorität für visuelles Feedback)
                if (isError)
                {
                    // Roter Balken bei Fehler, Daten aus uiData
                    DrawProgressBar(barRect, updateUiState.Progress, updateUiState.ProgressDetail, new Color(0.7f, 0.0f, 0.0f, 1f));

                    Rect errBtnRect = new Rect(winRect.x + (winRect.width - btnWidth) / 2f, btnY, btnWidth, btnHeight);
                    if (DrawButton(errBtnRect, "IGNORE & START GAME", false)) onErrorContinue?.Invoke();
                }
                // B) PROGRESS (Check oder Update)
                else if (showProgress)
                {
                    // Normaler oranger Balken
                    DrawProgressBar(barRect, updateUiState.Progress, updateUiState.ProgressDetail, ModfatherUIColors.AccentOrange);

                    // Cancel Button ist für beide Phasen verfügbar
                    Rect cancelRect = new Rect(winRect.x + (winRect.width - btnWidth) / 2f, btnY, btnWidth, btnHeight);
                    if (DrawButton(cancelRect, "CANCEL", false)) onCancel?.Invoke();
                }
                // C) LISTE & AUSWAHL
                else if (showButtons && updateUiState.SyncActions != null)
                {
                    float listStartY = winRect.y + 95f;
                    float listHeight = (btnY - 20f) - listStartY;

                    Rect listRect = new Rect(winRect.x + 20, listStartY, winRect.width - 40, listHeight);

                    // Liste der Aktionen kommt aus uiData
                    DrawActionList(listRect, updateUiState.SyncActions);

                    Rect rectBtnUpdate = new(winRect.x + sideMargin, btnY, btnWidth, btnHeight);
                    if (DrawButton(rectBtnUpdate, "ACCEPT OFFER", true)) onAccept?.Invoke();

                    Rect rectBtnSkip = new(winRect.x + winRect.width - btnWidth - sideMargin, btnY, btnWidth, btnHeight);
                    if (DrawButton(rectBtnSkip, "WALK AWAY", false)) onDecline?.Invoke();
                }
            }
            finally
            {
                GUI.matrix = oldMatrix;
            }
        }

        private static void DrawProgressBar(Rect rect, float progress, string text, Color fillColor)
        {
            DrawRect(rect, new Color(0.05f, 0.05f, 0.05f, 1f));
            DrawRect(new Rect(rect.x - 1, rect.y - 1, rect.width + 2, 1), ModfatherUIColors.WindowBorder);
            DrawRect(new Rect(rect.x - 1, rect.y + rect.height, rect.width + 2, 1), ModfatherUIColors.WindowBorder);
            DrawRect(new Rect(rect.x - 1, rect.y, 1, rect.height), ModfatherUIColors.WindowBorder);
            DrawRect(new Rect(rect.x + rect.width, rect.y, 1, rect.height), ModfatherUIColors.WindowBorder);

            float fillWidth = rect.width * Mathf.Clamp01(progress);
            if (fillWidth > 1f)
                DrawRect(new Rect(rect.x, rect.y, fillWidth, rect.height), fillColor);

            if (!string.IsNullOrEmpty(text))
            {
                GUIStyle textStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 16, normal = { textColor = Color.white } };
                GUIStyle shadowStyle = new GUIStyle(textStyle); shadowStyle.normal.textColor = new Color(0, 0, 0, 0.7f);
                GUI.Label(new Rect(rect.x + 1, rect.y + 1, rect.width, rect.height), text, shadowStyle);
                GUI.Label(rect, text, textStyle);
            }
        }

        private static void DrawActionList(Rect rect, IReadOnlyList<SyncAction> items)
        {
            DrawRect(rect, new Color(0.08f, 0.08f, 0.08f, 1f));
            float headerH = 26f;
            Rect headerRect = new Rect(rect.x, rect.y, rect.width, headerH);

            DrawListHeader(headerRect, items);

            float itemH = 32f;
            Rect scrollVisible = new Rect(rect.x, rect.y + headerH, rect.width, rect.height - headerH);
            Rect scrollContent = new Rect(0, 0, rect.width - 15f, items.Count * itemH);
            if (s_texScrollTrack == null) s_texScrollTrack = MakeTex(1, 1, new Color(0.05f, 0.05f, 0.05f, 1f));
            if (s_texScrollThumb == null) s_texScrollThumb = MakeTex(1, 1, new Color(0.3f, 0.3f, 0.3f, 1f));
            GUIStyle vScroll = new GUIStyle(GUI.skin.verticalScrollbar);
            vScroll.normal.background = s_texScrollTrack; vScroll.fixedWidth = 10f;
            GUIStyle vThumb = new GUIStyle(GUI.skin.verticalScrollbarThumb);
            vThumb.normal.background = s_texScrollThumb; vThumb.fixedWidth = 10f;
            s_scrollPos = GUI.BeginScrollView(scrollVisible, s_scrollPos, scrollContent, false, false, GUIStyle.none, vScroll);
            for (int i = 0; i < items.Count; i++)
            {
                Rect itemRect = new Rect(0, i * itemH, scrollContent.width, itemH);
                DrawListItem(itemRect, items[i], i % 2 == 0);
            }
            GUI.EndScrollView();
        }

        private static void DrawListHeader(Rect rect, IReadOnlyList<SyncAction> items)
        {
            DrawRect(rect, new Color(0.15f, 0.15f, 0.15f, 1f));
            GUIStyle style = new(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold, fontSize = 12, normal = { textColor = Color.gray } };
            GUI.Label(new Rect(rect.x + 10, rect.y, 150, rect.height), $"CHANGES ({items.Count})", style);
            float bW = 90f;

            if (GUI.Button(new Rect(rect.x + rect.width - bW - 5, rect.y + 2, bW, 22), "Select None"))
            {
                foreach (SyncAction item in items)
                {
                    // Nur abwählen, wenn NICHT Blacklisted
                    if (item.Type != SyncActionType.Blacklist)
                    {
                        item.IsSelected = false;
                    }
                }
            }

            if (GUI.Button(new Rect(rect.x + rect.width - (bW * 2) - 10, rect.y + 2, bW, 22), "Select All"))
            {
                foreach (SyncAction item in items) item.IsSelected = true;
            }
        }

        private static void DrawListItem(Rect rect, SyncAction item, bool isEven)
        {
            if (!isEven) DrawRect(rect, new Color(1, 1, 1, 0.03f));
            Vector2 scaledMouse = Event.current.mousePosition / s_scale;

            // Hover Overlay
            if (rect.Contains(scaledMouse)) DrawRect(rect, ModfatherUIColors.HoverOverlay);

            float centerY = rect.y + (rect.height - 16) / 2;
            Rect checkRect = new Rect(rect.x + 8, centerY, 16, 16);
            DrawRect(checkRect, ModfatherUIColors.WindowBorder);

            // Prüfen ob Blacklisted
            bool isBlacklisted = item.Type == SyncActionType.Blacklist;

            // Auswahlstatus erzwingen/zeichnen
            if (isBlacklisted)
            {
                // Sicherstellen, dass Blacklisted immer ausgewählt ist
                if (!item.IsSelected) item.IsSelected = true;
                // Graues "Locked" Quadrat
                DrawRect(new Rect(checkRect.x + 3, checkRect.y + 3, 10, 10), Color.gray);
            }
            else if (item.IsSelected)
            {
                // Normales oranges Quadrat
                DrawRect(new Rect(checkRect.x + 3, checkRect.y + 3, 10, 10), ModfatherUIColors.AccentOrange);
            }

            // Typ Badge
            Color typeCol = GetTypeColor(item.Type);
            Rect badgeRect = new Rect(rect.x + 32, centerY - 1, 60, 18);
            DrawRect(badgeRect, new Color(typeCol.r, typeCol.g, typeCol.b, 0.15f));
            GUIStyle typeStyle = new(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 10, fontStyle = FontStyle.Bold, normal = { textColor = typeCol } };
            GUI.Label(badgeRect, item.Type.ToString().ToUpper(CultureInfo.InvariantCulture), typeStyle);

            // Dateipfad Text (etwas grauer wenn blacklisted)
            Color textColor = isBlacklisted ? Color.gray : (item.IsSelected ? ModfatherUIColors.TextWhite : Color.gray);
            GUIStyle pathStyle = new(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontSize = 13, fontStyle = FontStyle.Normal, normal = { textColor = textColor } };
            GUI.Label(new Rect(rect.x + 100, rect.y, rect.width - 100, rect.height), item.RelativeFilePath, pathStyle);

            // Klick-Bereich (Nur aktiv wenn NICHT Blacklisted)
            if (!isBlacklisted)
            {
                if (GUI.Button(rect, string.Empty, GUIStyle.none))
                {
                    item.IsSelected = !item.IsSelected;
                }
            }
        }

        private static void DrawWindowFrame(Rect rect)
        {
            DrawRect(rect, ModfatherUIColors.WindowBorder);
            DrawRect(new Rect(rect.x + 1, rect.y + 1, rect.width - 2, rect.height - 2), ModfatherUIColors.WindowBackground);
            DrawRect(new Rect(rect.x, rect.y, rect.width, 3), ModfatherUIColors.AccentOrange);
        }

        private static void DrawHeader(Rect winRect, string title)
        {
            GUIStyle titleStyle = new(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 22, fontStyle = FontStyle.Bold, normal = { textColor = ModfatherUIColors.AccentOrange } };
            GUI.Label(new Rect(winRect.x + 20, winRect.y + 5, winRect.width - 40, 45), title, titleStyle);
            DrawRect(new Rect(winRect.x + 20, winRect.y + 48, winRect.width - 40, 1), new Color(1, 1, 1, 0.1f));
        }

        private static void DrawStatusText(Rect winRect, string text)
        {
            GUIStyle bodyStyle = new(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 16, normal = { textColor = ModfatherUIColors.TextWhite } };
            GUI.Label(new Rect(winRect.x, winRect.y + 55, winRect.width, 35), text, bodyStyle);
        }

        private static bool DrawButton(Rect rect, string text, bool isPrimary)
        {
            bool clicked = false;
            Color baseColor = isPrimary ? ModfatherUIColors.AccentOrange : ModfatherUIColors.ButtonSecondary;
            Color textColor = isPrimary ? Color.black : ModfatherUIColors.TextWhite;
            DrawRect(rect, baseColor);
            Vector2 scaledMouse = Event.current.mousePosition / s_scale;
            if (rect.Contains(scaledMouse)) DrawRect(rect, ModfatherUIColors.HoverOverlay);
            GUIStyle btnStyle = new(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 15, normal = { textColor = textColor } };
            GUI.Label(rect, text, btnStyle);
            Color oldColor = GUI.color; GUI.color = Color.clear;
            if (GUI.Button(rect, string.Empty)) { clicked = true; }
            GUI.color = oldColor;
            return clicked;
        }

        private static void DrawRect(Rect position, Color color)
        {
            Color oldColor = GUI.color; GUI.color = color;
            GUI.DrawTexture(position, Texture2D.whiteTexture);
            GUI.color = oldColor;
        }

        private static Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++) pix[i] = col;
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix); result.Apply();
            return result;
        }

        private static Color GetTypeColor(SyncActionType type)
        {
            return type switch
            {
                SyncActionType.Add => new Color(0.4f, 0.8f, 0.4f),
                SyncActionType.Delete => new Color(0.9f, 0.4f, 0.4f),
                SyncActionType.Update => new Color(0.4f, 0.6f, 1f),
                SyncActionType.Adopt => new Color(0.8f, 0.8f, 0.4f),
                SyncActionType.Blacklist => new Color(0.5f, 0.5f, 0.5f), // Grau für Blacklisted
                _ => Color.gray
            };
        }
    }
}