using System.Runtime.InteropServices;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace ataraxia
{
    class MemoryManager
    {
        IntPtr processHandle = IntPtr.Zero;

        const uint PROCESS_ALL_ACCESS = 0x1F0FFF;

        const uint PAGE_NOACCESS = 0x01;
        const uint PAGE_READWRITE = 0x04;
        const uint PAGE_WRITECOPY = 0x08;
        const uint PAGE_EXECUTE = 0x10;
        const uint PAGE_EXECUTE_READ = 0x20;
        const uint PAGE_EXECUTE_READWRITE = 0x40;
        const uint PAGE_EXECUTE_WRITECOPY = 0x80;

        const uint MEM_COMMIT = 0x1000;

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool ReadProcessMemory(IntPtr hProcess, long lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory(IntPtr hProcess, long lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern int VirtualQueryEx(IntPtr hProcess, long lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, uint dwLength);

        [StructLayout(LayoutKind.Sequential)]
        struct MEMORY_BASIC_INFORMATION
        {
            public long BaseAddress;
            public long AllocationBase;
            public uint AllocationProtect;
            public long RegionSize;
            public uint State;
            public uint Protect;
            public uint Type;
        }

        public int GetProcIdFromName(string name)
        {
            Process[] processes = Process.GetProcessesByName(name);
            if (processes.Length > 0)
            {
                return processes[0].Id;
            }
            return -1;
        }

        public bool OpenProcess(int pid)
        {
            processHandle = OpenProcess(PROCESS_ALL_ACCESS, false, pid);
            return processHandle != IntPtr.Zero;
        }

        public void WriteMemory(string address, string type, string value)
        {
            if (type == "bytes")
            {
                long addr = long.Parse(address, System.Globalization.NumberStyles.HexNumber);
                byte[] bytes = ParseAoB(value);
                int written = 0;
                WriteProcessMemory(processHandle, addr, bytes, bytes.Length, ref written);
            }
        }

        public Task<IEnumerable<long>> AoBScan(string signature, bool writable, bool executable)
        {
            return Task.Run(() =>
            {
                byte[] patternBytes = ParseAoB(signature);
                if (patternBytes.Length == 0) return new List<long>();
                long minAddress = 0x10000;
                long maxAddress = 0x7FFFFFFFFFFFL;
                var regions = new List<(long BaseAddress, long RegionSize)>();
                long current = minAddress;
                while (current < maxAddress)
                {
                    MEMORY_BASIC_INFORMATION mbi;
                    if (VirtualQueryEx(processHandle, current, out mbi, (uint)Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION))) == 0)
                    {
                        break;
                    }
                    if (mbi.RegionSize == 0)
                    {
                        break;
                    }
                    if (mbi.State == MEM_COMMIT && (mbi.Protect & PAGE_NOACCESS) == 0 && mbi.Type == 0x20000)
                    {
                        bool isWritable = (mbi.Protect & (PAGE_READWRITE | PAGE_WRITECOPY | PAGE_EXECUTE_READWRITE | PAGE_EXECUTE_WRITECOPY)) != 0;
                        bool isExecutable = (mbi.Protect & (PAGE_EXECUTE | PAGE_EXECUTE_READ | PAGE_EXECUTE_READWRITE | PAGE_EXECUTE_WRITECOPY)) != 0;
                        if ((writable && !isWritable) || (executable && !isExecutable))
                        {
                            current += mbi.RegionSize;
                            continue;
                        }
                        regions.Add((mbi.BaseAddress, mbi.RegionSize));
                    }
                    current += mbi.RegionSize;
                }
                var results = new ConcurrentBag<long>();
                Parallel.ForEach(regions, region =>
                {
                    long baseAddr = region.BaseAddress;
                    long size = region.RegionSize;
                    const int chunkSize = 0x4000000;
                    for (long offset = 0; offset < size; offset += chunkSize)
                    {
                        int readSize = (int)Math.Min(chunkSize, size - offset);
                        byte[] buffer = new byte[readSize];
                        int bytesRead = 0;
                        if (ReadProcessMemory(processHandle, baseAddr + offset, buffer, readSize, ref bytesRead) && bytesRead >= patternBytes.Length)
                        {
                            ReadOnlySpan<byte> bufferSpan = buffer.AsSpan(0, bytesRead);
                            ReadOnlySpan<byte> patternSpan = patternBytes.AsSpan();
                            int start = 0;
                            while (start <= bytesRead - patternBytes.Length)
                            {
                                int index = bufferSpan.Slice(start).IndexOf(patternSpan);
                                if (index == -1) break;
                                index += start;
                                results.Add(baseAddr + offset + index);
                                start = index + 1;
                            }
                        }
                    }
                });
                return results.ToList().AsEnumerable();
            });
        }

        byte[] ParseAoB(string sig)
        {
            string[] parts = sig.Split(' ');
            byte[] bytes = new byte[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                bytes[i] = Convert.ToByte(parts[i], 16);
            }
            return bytes;
        }

        ~MemoryManager()
        {
            if (processHandle != IntPtr.Zero)
            {
                CloseHandle(processHandle);
            }
        }
    }
}
