using System;
using System.Threading.Tasks;

namespace XrayClient.Core
{
    public class ServiceController
    {
        private ConfigGenerator _configGenerator = new ConfigGenerator();
        private XrayProcessWrapper _xrayProcess = new XrayProcessWrapper();

        public event Action<string> OnLog
        {
            add => _xrayProcess.OnLog += value;
            remove => _xrayProcess.OnLog -= value;
        }

        public ServiceController()
        {
            ResourceManager.EnsureBinaries();
        }

        public async Task StartConnection(bool enableTun)
        {
            try
            {
               _xrayProcess.Stop(); 
               await _configGenerator.FetchAndGenerateConfig(enableTun);
               _xrayProcess.Start();
            }
            catch (Exception ex)
            {
                // Relay error to log
                // In a real app we might use a proper logger
                throw;
            }
        }

        public void StopConnection()
        {
            _xrayProcess.Stop();
        }
        
        public bool IsConnected => _xrayProcess.IsRunning;
    }
}
