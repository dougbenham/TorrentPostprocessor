using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace TorrentPostprocessor
{
    class Program
    {
        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
        static extern bool CreateHardLink(
            string lpFileName,
            string lpExistingFileName,
            IntPtr lpSecurityAttributes
        );

        private static readonly string _executableDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        private static void Log(string m = "")
        {
            File.AppendAllText($"{_executableDirectory}\\TorrentPostprocessor.log", m + Environment.NewLine);
        }

        static void Main(string[] args)
        {
            Log(Environment.CommandLine);

            if (args.Length <= 0)
            {
                Log("Invalid arguments");
                return;
            }

            var path = args[0];
            foreach (var mediaFilePath in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).Where(s => new[] {".mp4", ".avi", ".mkv"}.Contains(Path.GetExtension(s))))
            {
                var mediaFile = Path.GetFileNameWithoutExtension(mediaFilePath);
                var mediaFolder = Path.GetDirectoryName(mediaFilePath);
                var size = 0l;
                var files = GetSubtitles(mediaFolder, mediaFile);
                if (files.Length == 0)
                    files = GetSubtitles(mediaFolder, null);
                if (files.Length == 0)
                    files = GetSubtitles(path, mediaFile);
                if (files.Length == 0)
                    files = GetSubtitles(path, null);

                var filtered = files.Where(s => Path.GetFileNameWithoutExtension(s).IndexOf("eng", StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
                if (filtered.Length == 0)
                    filtered = files.Where(s => Path.GetFileNameWithoutExtension(s).IndexOf("en", StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
                if (filtered.Length == 0)
                    filtered = files;

                foreach (var subPath in filtered)
                {
                    var targetSubPath = mediaFolder + "\\" + mediaFile + Path.GetExtension(subPath);
                    if (subPath == targetSubPath)
                        continue;

                    var newSize = new FileInfo(subPath).Length;
                    if (newSize <= size)
                        continue;

                    size = newSize;
                    Log($"Linking {subPath} to {targetSubPath}");
                    if (File.Exists(targetSubPath))
                        File.Delete(targetSubPath);
                    CreateHardLink(targetSubPath, subPath, IntPtr.Zero);
                }
            }
        }

        private static string[] GetSubtitles(string folder, string filter)
        {
            var folder2 = folder + "\\";
            return Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories)
                .Where(s =>
                {
                    if (!new[] {".srt", ".sub", ".ass"}.Contains(Path.GetExtension(s), StringComparer.OrdinalIgnoreCase))
                        return false;

                    if (!s.StartsWith(folder2, StringComparison.OrdinalIgnoreCase))
                        return false;

                    s = s.Substring(folder2.Length);
                    if (filter != null && s.IndexOf(filter, StringComparison.OrdinalIgnoreCase) == -1)
                        return false;
                        
                    return true;
                }).ToArray();
        }
    }
}
