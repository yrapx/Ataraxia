using System.Diagnostics;

namespace ataraxia
{
    class AtaraxiaManager
    {
        MemoryManager mm = new MemoryManager();

        public long scoreHackAddress;
        public long damageHackAddress;
        public long antiGrenadesAddress;
        public long antiBombAddress;
        public long fastPlantAddress;
        public long fastDefuseAddress;
        public long wallshotAddress;
        public long rapidFireAddress;
        public long infinityAmmoAddress;
        public long fastKnifeAddress;

        public long ScanAoB(string signature, string functionName)
        {
            DateTime startTime = DateTime.Now;
            IEnumerable<long> results = mm.AoBScan(signature, true, true).Result;
            long address = results.FirstOrDefault();
            if (address != 0)
            {
                double scanTime = (DateTime.Now - startTime).TotalMilliseconds;
                Console.WriteLine($"{functionName} address: 0x{address.ToString("X")} [Scan time: {scanTime:F0}ms]");
            }
            else
            {
                Console.WriteLine($"{functionName} address not found. Press any key to exit...");
                Console.WriteLine("Something went wrong during the hook...");
                Console.ReadKey();
                Environment.Exit(-1);
            }
            return address;
        }

        public void ReplaceAoB(long address, string signature)
        {
            if (address != 0)
            {
                mm.WriteMemory(address.ToString("X"), "bytes", signature);
                Console.WriteLine($"Wrote signature [{signature}] to address 0x{address:X}");
            }
            else
            {
                Console.WriteLine($"Failed to write signature [{signature}]: Invalid address.");
            }
        }

        void Hook()
        {
            scoreHackAddress = ScanAoB("FE 0F 1D F8 F6 57 01 A9 F4 4F 02 A9 55 BF 01", "Score Hack");
            damageHackAddress = ScanAoB("FF 43 01 D1 E8 0B 00 FD FE 0F 00 F9 F8 5F 02 A9 F6 57 03 A9 F4 4F 04 A9 F5 CD", "Damage Hack");
            antiGrenadesAddress = ScanAoB("EA 0F 19 FC E9 23 01 6D FE 6F 02 A9 FA 67 03 A9 F8 5F 04 A9 F6 57 05 A9 F4 4F 06 A9 F6", "Anti-Grenades");
            antiBombAddress = ScanAoB("FF C3 00 D1 E8 0B 00 FD FE 0F 00 F9 F4 4F 02 A9 74 49 01 90 88 AE", "Anti-Bomb");
            fastPlantAddress = ScanAoB("FF C3 00 D1 E8 0B 00 FD FE 0F 00 F9 F4 4F 02 A9 74 49 01 90 88 B6", "Fast Plant");
            fastDefuseAddress = ScanAoB("FF C3 00 D1 E8 0B 00 FD FE 0F 00 F9 F4 4F 02 A9 F4 45 01 F0 88 52", "Fast Defuse");
            wallshotAddress = ScanAoB("FF C3 00 D1 FE 57 01 A9 F4 4F 02 A9 14 BE 01 D0 88 3A", "Wallshot");
            rapidFireAddress = ScanAoB("FF C3 00 D1 FE 57 01 A9 F4 4F 02 A9 14 BE 01 D0 88 0E", "Rapid Fire");
            infinityAmmoAddress = ScanAoB("FF C3 00 D1 FE 0B 00 F9 F4 4F 02 A9 94 84 01 F0 88 06", "Infinity Ammo");
            fastKnifeAddress = ScanAoB("FF C3 00 D1 E8 0B 00 FD FE 0F 00 F9 F4 4F 02 A9 14 7F 01 F0 88 B6", "Fast Knife");

            Program p = new Program();
            p.Start();
            Console.WriteLine("Successfully hooked.");
        }

        void RestoreBytes()
        {
            ReplaceAoB(scoreHackAddress, "FE 0F 1D F8");
            ReplaceAoB(damageHackAddress, "FF 43 01 D1");
            ReplaceAoB(antiGrenadesAddress, "EA 0F 19 FC");
            ReplaceAoB(antiBombAddress, "FF C3 00 D1");
            ReplaceAoB(fastPlantAddress, "FF C3 00 D1");
            ReplaceAoB(fastDefuseAddress, "FF C3 00 D1");
            ReplaceAoB(wallshotAddress, "FF C3 00 D1");
            ReplaceAoB(rapidFireAddress, "FF C3 00 D1");
            ReplaceAoB(infinityAmmoAddress, "FF C3 00 D1");
            ReplaceAoB(fastKnifeAddress, "FF C3 00 D1");
            Console.WriteLine("All bytes restored.");
        }

        void TerminateBlueStacks()
        {
            try
            {
                Process[] processes = Process.GetProcessesByName("HD-Player");
                if (processes.Length > 0)
                {
                    foreach (Process process in processes)
                    {
                        process.Kill();
                        Console.WriteLine($"BlueStacks process [HD-Player.exe] [PID: {process.Id}] terminated.");
                    }
                }
                else
                {
                    Console.WriteLine("BlueStacks process [HD-Player.exe] not found.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error terminating BlueStacks process [HD-Player.exe]: {ex.Message}");
            }
        }

        public void Unhook()
        {
            Console.WriteLine("Unhooking...");
            RestoreBytes();
            Console.WriteLine("Terminating BlueStacks...");
            TerminateBlueStacks();
            Console.Out.Flush();
            Environment.Exit(0);
        }

        public void Initialize()
        {
            int PID = mm.GetProcIdFromName("HD-Player");
            if (PID > 0)
            {
                Console.WriteLine($"BlueStacks process [HD-Player.exe] [PID: {PID}] found! Hooking...");
                mm.OpenProcess(PID);
                Hook();
            }
            else
            {
                Console.WriteLine("BlueStacks process [HD-Player.exe] not found. Press any key to exit...");
                Console.ReadKey();
                Environment.Exit(-1);
            }
        }
    }
}
