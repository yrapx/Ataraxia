using ClickableTransparentOverlay;
using System.Numerics;
using ImGuiNET;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace ataraxia
{
    class Program : Overlay
    {
        [DllImport("winmm.dll", SetLastError = true)]
        static extern bool PlaySound(string pszSound, IntPtr hmod, uint dwFlags);

        [DllImport("winmm.dll")]
        static extern int waveOutSetVolume(IntPtr hwo, uint dwVolume);

        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(int vKey);

        private const uint SND_FILENAME = 0x00020000;
        private const uint SND_ASYNC = 0x0001;

        static AtaraxiaManager am = new AtaraxiaManager();

        static void Main()
        {
            waveOutSetVolume(IntPtr.Zero, 0x20002000);
            am.Initialize();
        }

        Stopwatch lastSoundTime = Stopwatch.StartNew();
        const int soundDebounceMs = 100;

        List<FunctionSettings>? inGameFunctions;
        List<FunctionSettings>? inLobbyFunctions;

        bool isStyleApplied = false;
        bool isVisible = true;

        Stopwatch lastInsertKeyTime = Stopwatch.StartNew();
        const int insertKeyDebounceMs = 100;

        bool wasInsertPressed = false;

        float alpha = 0.0f;
        float animationSpeed = 0.05f;

        bool configTabSelected = false;
        bool functionsTabSelected = false;
        bool inGameTabSelected = false;
        bool inLobbyTabSelected = false;

        void Play1()
        {
            if (lastSoundTime.ElapsedMilliseconds < soundDebounceMs) return;
            lastSoundTime.Restart();

            PlaySound("1.wav", IntPtr.Zero, SND_FILENAME | SND_ASYNC);
        }

        void Play2()
        {
            if (lastSoundTime.ElapsedMilliseconds < soundDebounceMs) return;
            lastSoundTime.Restart();

            PlaySound("2.wav", IntPtr.Zero, SND_FILENAME | SND_ASYNC);
        }

        void Play3()
        {
            if (lastSoundTime.ElapsedMilliseconds < soundDebounceMs) return;
            lastSoundTime.Restart();

            PlaySound("3.wav", IntPtr.Zero, SND_FILENAME | SND_ASYNC);
        }

        class FunctionSettings
        {
            public string Name { get; set; } = string.Empty;
            public long Address { get; set; }
            public string ON { get; set; } = string.Empty;
            public string OFF { get; set; } = string.Empty;
            public bool isEnabled;
        }

        public Program()
        {
            inGameFunctions = new List<FunctionSettings>
            {
                new FunctionSettings { Name = "Score Hack", Address = am.scoreHackAddress, ON = "C0 03 5F D6", OFF = "FE 0F 1D F8", isEnabled = false },
                new FunctionSettings { Name = "Damage Hack", Address = am.damageHackAddress, ON = "C0 03 5F D6", OFF = "FF 43 01 D1", isEnabled = false },
                new FunctionSettings { Name = "Anti-Grenades", Address = am.antiGrenadesAddress, ON = "C0 03 5F D6", OFF = "EA 0F 19 FC", isEnabled = false },
                new FunctionSettings { Name = "Anti-Bomb", Address = am.antiBombAddress, ON = "C0 03 5F D6", OFF = "FF C3 00 D1", isEnabled = false },
                new FunctionSettings { Name = "Fast Plant", Address = am.fastPlantAddress, ON = "C0 03 5F D6", OFF = "FF C3 00 D1", isEnabled = false },
                new FunctionSettings { Name = "Fast Defuse", Address = am.fastDefuseAddress, ON = "C0 03 5F D6", OFF = "FF C3 00 D1", isEnabled = false }
            };

            inLobbyFunctions = new List<FunctionSettings>
            {
                new FunctionSettings { Name = "Wallshot", Address = am.wallshotAddress, ON = "C0 03 5F D6", OFF = "FF C3 00 D1", isEnabled = false },
                new FunctionSettings { Name = "Rapid Fire", Address = am.rapidFireAddress, ON = "C0 03 5F D6", OFF = "FF C3 00 D1", isEnabled = false },
                new FunctionSettings { Name = "Infinity Ammo", Address = am.infinityAmmoAddress, ON = "C0 03 5F D6", OFF = "FF C3 00 D1", isEnabled = false },
                new FunctionSettings { Name = "Fast Knife", Address = am.fastKnifeAddress, ON = "C0 03 5F D6", OFF = "FF C3 00 D1", isEnabled = false }
            };
        }

        void ApplyStyle()
        {
            if (isStyleApplied) return;

            ImGuiStylePtr style = ImGui.GetStyle();
            style.WindowRounding = 2.5f;
            style.FrameRounding = 2.5f;
            style.WindowPadding = new Vector2(5, 5);
            style.FramePadding = new Vector2(5, 5);

            ImGui.StyleColorsClassic();

            ReplaceFont("Font.ttf", 16, FontGlyphRangeType.English);

            isStyleApplied = true;
        }

        protected override void Render()
        {
            bool isInsertPressed = (GetAsyncKeyState(0x2D) & 0x8000) != 0;
            if (isInsertPressed && !wasInsertPressed && lastInsertKeyTime.ElapsedMilliseconds >= insertKeyDebounceMs)
            {
                lastInsertKeyTime.Restart();
                isVisible = !isVisible;
                if (isVisible)
                    alpha = 0;
                else
                    alpha = 1;
                if (!isVisible)
                    Play1();
            }
            wasInsertPressed = isInsertPressed;

            if (!isVisible && alpha <= 0) return;
            if (isVisible && alpha < 1)
                alpha = Math.Min(alpha + animationSpeed, 1);
            else if (!isVisible && alpha > 0)
                alpha = Math.Max(alpha - animationSpeed, 0);

            Vector2 windowSize = new Vector2(260, 280);
            ImGuiWindowFlags windowFlags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoBringToFrontOnFocus;

            ApplyStyle();

            Vector2 screenSize = ImGui.GetIO().DisplaySize;
            Vector2 windowPos = new Vector2((screenSize.X - windowSize.X) * 0.5f, (screenSize.Y - windowSize.Y) * 0.5f);
            ImGui.SetNextWindowPos(windowPos, ImGuiCond.Once);

            ImGui.SetNextWindowSize(windowSize, ImGuiCond.Once);
            ImGui.GetStyle().Alpha = alpha;

            ImGui.Begin("Ataraxia | External | 0.35.3 f1 | arm64-v8a", windowFlags);

            bool newConfig = false;
            bool newFunctions = false;
            bool newInGame = false;
            bool newInLobby = false;

            if (ImGui.BeginTabBar("MainTabBar"))
            {
                if (ImGui.BeginTabItem("Config"))
                {
                    newConfig = true;
                    ImGui.Separator();
                    ImGui.Text("Unhook");
                    if (ImGui.Button("Unhook"))
                    {
                        Play1();
                        am.Unhook();
                    }
                    ImGui.Separator();
                    ImGui.Text("Theme");
                    if (ImGui.Button("Classic"))
                    {
                        Play1();
                        ImGui.StyleColorsClassic();
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("Light"))
                    {
                        Play1();
                        ImGui.StyleColorsLight();
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("Dark"))
                    {
                        Play1();
                        ImGui.StyleColorsDark();
                    }
                    ImGui.Separator();
                    ImGui.Text("Info");
                    ImGui.Text("Made by @yrapx (Telegram)");
                    ImGui.Text("Cheat version: 1.0.0");
                    ImGui.Separator();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Functions"))
                {
                    newFunctions = true;
                    if (ImGui.BeginTabBar("FunctionsTabBar"))
                    {
                        if (ImGui.BeginTabItem("In Game"))
                        {
                            newInGame = true;
                            ImGui.Separator();
                            foreach (var function in inGameFunctions!)
                            {
                                if (ImGui.Checkbox(function.Name, ref function.isEnabled))
                                {
                                    am.ReplaceAoB(function.Address, function.isEnabled ? function.ON : function.OFF);
                                    if (function.isEnabled)
                                        Play2();
                                    else
                                        Play3();
                                }
                            }
                            ImGui.Separator();
                            ImGui.EndTabItem();
                        }

                        if (ImGui.BeginTabItem("In Lobby"))
                        {
                            newInLobby = true;
                            ImGui.Separator();
                            foreach (var function in inLobbyFunctions!)
                            {
                                if (ImGui.Checkbox(function.Name, ref function.isEnabled))
                                {
                                    am.ReplaceAoB(function.Address, function.isEnabled ? function.ON : function.OFF);
                                    if (function.isEnabled)
                                        Play2();
                                    else
                                        Play3();
                                }
                            }
                            ImGui.Separator();
                            ImGui.EndTabItem();
                        }
                        ImGui.EndTabBar();
                    }
                    ImGui.EndTabItem();
                }
                ImGui.EndTabBar();
            }

            if (newConfig && !configTabSelected)
            {
                Play1();
                configTabSelected = true;
                functionsTabSelected = false;
            }
            else if (!newConfig)
                configTabSelected = false;

            if (newFunctions && !functionsTabSelected)
            {
                Play1();
                functionsTabSelected = true;
                configTabSelected = false;
            }
            else if (!newFunctions)
                functionsTabSelected = false;

            if (newInGame && !inGameTabSelected)
            {
                Play1();
                inGameTabSelected = true;
                inLobbyTabSelected = false;
            }
            else if (!newInGame)
                inGameTabSelected = false;

            if (newInLobby && !inLobbyTabSelected)
            {
                Play1();
                inLobbyTabSelected = true;
                inGameTabSelected = false;
            }
            else if (!newInLobby)
                inLobbyTabSelected = false;

            ImGui.End();
        }
    }
}