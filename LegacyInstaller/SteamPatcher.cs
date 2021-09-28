using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace LegacyInstaller
{
    class SteamPatcher
    {
        private const string ProcessName = "steam";
        private const string ModuleName = "steamclient.dll";
        private const string TargetString = "Depot download failed : Manifest not available";

        private const int PROCESS_VM_READ = 0x0010;
        private const int PROCESS_VM_WRITE = 0x0020;
        private const int PROCESS_VM_OPERATION = 0x0008;
        private const int PROCESS_QUERY_INFORMATION = 0x0400;
        private const int PROCESS_ALL_ACCESS = 0x1F0FFF;

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(int hProcess, int lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory(int hProcess, int lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesWritten);

        public static void ApplyPatch()
        {
            // Find steam process
            var steamProcess = Process.GetProcessesByName(ProcessName).FirstOrDefault();
            if (steamProcess == null)
                throw new ProcessNotFoundException();

            var steamClientModule = steamProcess.Modules.Cast<ProcessModule>().FirstOrDefault(module => module.ModuleName == ModuleName);
            if (steamClientModule == null)
                throw new ProcessNotFoundException();

            const int accessFlags = PROCESS_QUERY_INFORMATION | PROCESS_VM_OPERATION | PROCESS_VM_READ | PROCESS_VM_WRITE;
            IntPtr steamProcessHandle = OpenProcess(accessFlags, false, steamProcess.Id);
            if (steamProcessHandle == IntPtr.Zero)
                throw new UnableToOpenProcessException();

            IntPtr steamClientHandle = steamClientModule.BaseAddress;

            Debug.WriteLine($"Process handle: {steamProcessHandle.ToString("X")}");
            Debug.WriteLine($"Module handle: {steamClientHandle.ToString("X")}");

            int bytesRead = 0;
            byte[] buffer = new byte[steamClientModule.ModuleMemorySize];
            if (ReadProcessMemory((int)steamProcessHandle, (int)steamClientHandle, buffer, buffer.Length, ref bytesRead) == false)
                throw new MemoryUnreadableException();

            var strBytes = Encoding.UTF8.GetBytes(TargetString);
            var strIndex = Utilities.Search(buffer, strBytes);
            if (strIndex == -1)
                throw new StringNotFoundException();

            var strAddress = (int)steamClientHandle + strIndex;
            var pushBytes = BitConverter.GetBytes(strAddress).Prepend<byte>(0x68).ToArray();

            Debug.WriteLine($"String address: {strAddress.ToString("X")}");
            Debug.WriteLine(BitConverter.ToString(pushBytes));

            var pushIndex = Utilities.Search(buffer, pushBytes);
            if (pushIndex == -1)
                throw new PatternNotFoundException();

            Debug.WriteLine($"Pattern index: {pushIndex.ToString("X")}");

            // 0x0F 0x85 is JNZ instruction (jump if true)
            var index = pushIndex;
            while (buffer[index] != 0x0F && buffer[index + 1] != 0x85)
                index -= 1;

            // Either the patch has already been applied or the JNZ isn't there
            // We want to prevent writing over code which isn't actually part
            // of what we want to patch.
            if (index < pushIndex - 10)
                throw new PatchAlreadyAppliedException();

            // Replace 2-byte jnz with nop, jmp
            buffer[index] = 0x90;
            buffer[index + 1] = 0xE9;

            int bytesWritten = 0;
            var patchAddress = (int)steamClientHandle + index;
            if (WriteProcessMemory((int)steamProcessHandle, patchAddress, buffer.Skip(index).Take(2).ToArray(), 2, ref bytesWritten) == false)
                throw new UnableToWriteMemoryException();

            Debug.WriteLine("Wrote patch to memory.");
        }

        public class ProcessNotFoundException : Exception
        {
            public ProcessNotFoundException() : base() { }
            public ProcessNotFoundException(string message) : base(message) { }
            public ProcessNotFoundException(string message, Exception inner) : base(message, inner) { }
        }

        public class UnableToOpenProcessException : Exception
        {
            public UnableToOpenProcessException() : base() { }
            public UnableToOpenProcessException(string message) : base(message) { }
            public UnableToOpenProcessException(string message, Exception inner) : base(message, inner) { }
        }

        public class StringNotFoundException : Exception
        {
            public StringNotFoundException() : base() { }
            public StringNotFoundException(string message) : base(message) { }
            public StringNotFoundException(string message, Exception inner) : base(message, inner) { }
        }

        public class PatternNotFoundException : Exception
        {
            public PatternNotFoundException() : base() { }
            public PatternNotFoundException(string message) : base(message) { }
            public PatternNotFoundException(string message, Exception inner) : base(message, inner) { }
        }

        public class PatchAlreadyAppliedException : Exception
        {
            public PatchAlreadyAppliedException() : base() { }
            public PatchAlreadyAppliedException(string message) : base(message) { }
            public PatchAlreadyAppliedException(string message, Exception inner) : base(message, inner) { }
        }

        public class PatchAppliedIncorrectlyException : Exception
        {
            public PatchAppliedIncorrectlyException() : base() { }
            public PatchAppliedIncorrectlyException(string message) : base(message) { }
            public PatchAppliedIncorrectlyException(string message, Exception inner) : base(message, inner) { }
        }

        public class MemoryUnreadableException : Exception
        {
            public MemoryUnreadableException() : base() { }
            public MemoryUnreadableException(string message) : base(message) { }
            public MemoryUnreadableException(string message, Exception inner) : base(message, inner) { }
        }

        public class UnableToWriteMemoryException : Exception
        {
            public UnableToWriteMemoryException() : base() { }
            public UnableToWriteMemoryException(string message) : base(message) { }
            public UnableToWriteMemoryException(string message, Exception inner) : base(message, inner) { }
        }
    }
}
