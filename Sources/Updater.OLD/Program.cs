namespace SwiftXP.SPT.TheModfather.Updater
{
    class Program
    {
        // =============================================================
        //               WINDOWS API IMPORT (P/Invoke)
        // =============================================================

        // Zugriff auf das Konsolenfenster holen
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetConsoleWindow();

        // Aktuelle Fensterposition und -größe abrufen
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        // Fenster verschieben
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        // Bildschirmauflösung abrufen
        [DllImport("user32.dll", SetLastError = true)]
        static extern int GetSystemMetrics(int nIndex);

        // Z-Order (Ebenen) des Fensters ändern (für TopMost)
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        // =============================================================
        //               STRUKTUREN & KONSTANTEN
        // =============================================================

        // Struktur für Fensterkoordinaten
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        // Konstanten für Bildschirm
        const int SM_CXSCREEN = 0; // Screen Width
        const int SM_CYSCREEN = 1; // Screen Height

        // Konstanten für TopMost
        static readonly IntPtr HWND_TOPMOST = new IntPtr(-1); // Handle für "Ganz oben"
        const uint SWP_NOSIZE = 0x0001;     // Größe beibehalten
        const uint SWP_NOMOVE = 0x0002;     // Position beibehalten (wird hier ignoriert, da MoveWindow das macht)
        const uint SWP_SHOWWINDOW = 0x0040; // Fenster anzeigen

        // =============================================================
        //               MAIN PROGRAMM
        // =============================================================

        static void Main(string[] args)
        {
            // 1. Fenster in die Mitte schieben
            CenterConsoleWindow();

            // 2. Fenster "Always on Top" setzen
            SetConsoleTopMost();

            // Programmlogik
            Console.WriteLine("==========================================");
            Console.WriteLine("   FENSTER: ZENTRIERT & ALWAYS ON TOP");
            Console.WriteLine("==========================================");
            Console.WriteLine();
            Console.WriteLine("Dieses Fenster schwebt nun über allen anderen.");
            Console.WriteLine();
            Console.WriteLine("Drücke eine Taste zum Beenden...");

            Console.ReadKey();
        }

        // =============================================================
        //               HELFER METHODEN
        // =============================================================

        static void CenterConsoleWindow()
        {
            IntPtr consoleHandle = GetConsoleWindow();

            // Aktuelle Größe holen
            RECT r;
            GetWindowRect(consoleHandle, out r);
            int windowWidth = r.Right - r.Left;
            int windowHeight = r.Bottom - r.Top;

            // Bildschirmgröße holen
            int screenWidth = GetSystemMetrics(SM_CXSCREEN);
            int screenHeight = GetSystemMetrics(SM_CYSCREEN);

            // Neue Position berechnen
            int newX = (screenWidth - windowWidth) / 2;
            int newY = (screenHeight - windowHeight) / 2;

            // Verschieben
            MoveWindow(consoleHandle, newX, newY, windowWidth, windowHeight, true);
        }

        static void SetConsoleTopMost()
        {
            IntPtr consoleHandle = GetConsoleWindow();

            // Setzt das Fenster auf TopMost (-1), ignoriert aber Positionsargumente (SWP_NOMOVE | SWP_NOSIZE)
            SetWindowPos(consoleHandle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
        }
    }
}