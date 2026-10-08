using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monitel.Mal.Context.CIM16.Xml.GOSTSO
{
    static class SimpleLogger
    {
        public static string logPath = @"C:\Program Files\Monitel\CK-11\Client\ImportExportGOST_SO.log";
        private const int MaxFileSizeMBytes = 10;
        private const long MaxFileSizeBytes = MaxFileSizeMBytes * 1024 * 1024;

        public static void WriteToLogInfo(string text)
        {
            WriteToLog($"[INFO] [{System.DateTime.Now}] {text}");
        }

        public static void WriteToLogError(Exception ex)
        {
            string text = $"Message: {ex.Message} | StackTrace: {ex.StackTrace}";
            WriteToLog($"[ERROR] [{System.DateTime.Now}] {text}");
        }

        private static void WriteToLog(string text)
        {
            CheckAndClearLog();
            File.AppendAllText(logPath, $"{text}{System.Environment.NewLine}");
        }

        private static void CheckAndClearLog()
        {
            FileInfo fileInfo = new FileInfo(logPath);
            if (fileInfo.Exists && fileInfo.Length >= MaxFileSizeBytes)
            {
                File.WriteAllText(logPath, string.Empty);
            }
        }
    }
}
