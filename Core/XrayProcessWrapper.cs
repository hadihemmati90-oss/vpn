using System;
using System.Diagnostics;
using System.IO;

namespace XrayClient.Core
{
    public class XrayProcessWrapper
    {
        private Process? _process;
        public event Action<string>? OnLog;

        public bool IsRunning => _process != null && !_process.HasExited;

        public void Start()
        {
            Stop();

            string xrayPath = Path.Combine(ResourceManager.BinDir, "xray.exe");
            if (!File.Exists(xrayPath))
                throw new FileNotFoundException("xray.exe not found in bin folder.");

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = xrayPath,
                Arguments = "run -c config.json",
                WorkingDirectory = ResourceManager.BinDir,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                Verb = "runas"
            };

            psi.EnvironmentVariables["XRAY_LOCATION_ASSET"] = ResourceManager.BinDir;

            _process = new Process { StartInfo = psi };
            
            _process.OutputDataReceived += (s, e) => 
            { 
                if (!string.IsNullOrEmpty(e.Data)) OnLog?.Invoke(e.Data); 
            };
            _process.ErrorDataReceived += (s, e) => 
            { 
                if (!string.IsNullOrEmpty(e.Data)) OnLog?.Invoke(e.Data); 
            };

            _process.Start();
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();
        }

        public void Stop()
        {
            if (_process != null && !_process.HasExited)
            {
                try { _process.Kill(); } catch { }
                _process.Dispose();
                _process = null;
            }
        }
    }
}
