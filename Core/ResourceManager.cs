using System;
using System.IO;
using System.Reflection;
using System.Linq;

namespace XrayClient.Core
{
    public static class ResourceManager
    {
        public static string AppDir => AppDomain.CurrentDomain.BaseDirectory;
        public static string BinDir => Path.Combine(AppDir, "bin");

        public static void EnsureBinaries()
        {
            if (!Directory.Exists(BinDir))
                Directory.CreateDirectory(BinDir);

            ExtractResource("xray.exe");
            ExtractResource("wintun.dll");
        }

        private static void ExtractResource(string filename)
        {
            string destPath = Path.Combine(BinDir, filename);
            if (File.Exists(destPath)) return;

            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(r => r.EndsWith(filename, StringComparison.OrdinalIgnoreCase));

            if (resourceName != null)
            {
                using var stream = assembly.GetManifestResourceStream(resourceName);
                using var fileStream = File.Create(destPath);
                stream?.CopyTo(fileStream);
            }
        }
    }
}
