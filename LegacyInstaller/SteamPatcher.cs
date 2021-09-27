using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace LegacyInstaller
{
    class SteamPatcher
    {
        private const string DownpatcherResourcePath = "LegacyInstaller.SteamDepotDownpatcher.exe";
        private static string DownpatcherPath => Path.Combine(Path.GetTempPath(), "SteamDepotDownpatcher.exe");

        private static Process _downpatcherProcess;
        private static TaskCompletionSource<bool> _downpatcherTcs;

        public static async Task ApplyPatch()
        {
            if (_downpatcherProcess != null)
                return;

            if (!File.Exists(DownpatcherPath))
            {
                var downpatcherResourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(DownpatcherResourcePath);
                var downpatcherFileStream = File.Create(DownpatcherPath);
                downpatcherResourceStream.Seek(0, SeekOrigin.Begin);
                downpatcherResourceStream.CopyTo(downpatcherFileStream);
                downpatcherFileStream.Close();
            }

            _downpatcherProcess = new Process();
            _downpatcherProcess.StartInfo.FileName = DownpatcherPath;
            _downpatcherProcess.StartInfo.CreateNoWindow = true;
            _downpatcherProcess.StartInfo.RedirectStandardOutput = true;
            _downpatcherProcess.StartInfo.RedirectStandardError = true;
            _downpatcherProcess.StartInfo.UseShellExecute = false;
            _downpatcherProcess.OutputDataReceived += _downpatcherProcess_DataReceived;
            _downpatcherProcess.ErrorDataReceived += _downpatcherProcess_DataReceived;

            _downpatcherTcs = new TaskCompletionSource<bool>();
            _downpatcherProcess.Start();

            _downpatcherProcess.BeginOutputReadLine();
            _downpatcherProcess.BeginErrorReadLine();

            try
            {
                await _downpatcherTcs.Task;
            }
            catch
            {
                if (_downpatcherProcess.HasExited)
                    return;

                _downpatcherProcess.Kill();
                _downpatcherProcess = null;

                throw;
            }

            if (_downpatcherProcess.HasExited)
                return;

            _downpatcherProcess.Kill();
            _downpatcherProcess = null;
        }

        private static void _downpatcherProcess_DataReceived(object sender, DataReceivedEventArgs e)
        {
            if (_downpatcherTcs.Task.IsCompleted)
                return;

            if (e.Data == "error.ProcessNotFound")
                _downpatcherTcs.SetException(new ProcessNotFoundException());
            if (e.Data == "error.UnableToOpenProcess")
                _downpatcherTcs.SetException(new UnableToOpenProcessException());
            if (e.Data == "error.StringNotFound")
                _downpatcherTcs.SetException(new StringNotFoundException());
            if (e.Data == "error.PatternNotFound")
                _downpatcherTcs.SetException(new PatternNotFoundException());
            if (e.Data == "error.PatchAlreadyApplied")
                _downpatcherTcs.SetException(new PatchAlreadyAppliedException());
            if (e.Data == "error.PatchAppliedIncorrectly")
                _downpatcherTcs.SetException(new PatchAppliedIncorrectlyException());
            if (e.Data == "Press ENTER to close.")
                _downpatcherTcs.SetResult(true);
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
    }
}
