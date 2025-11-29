using FindMyMeasure.Database;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;

namespace FindMyMeasure
{
    public class Utils
    {
        public static List<SemanticModel> ListAllLocalSemanticModels()
        {
            List<SemanticModel> semanticModels = new List<SemanticModel>();
            var ssasProcesses = from n in Process.GetProcesses() where n.ProcessName == "msmdsrv" select n;
            foreach (Process ssasProcess in ssasProcesses)
            {
                int? port = GetProcessAttachedPort(ssasProcess.Id);
                int pbiProcessID = GetParentPID(ssasProcess.Id); // Most likely the PowerBI Desktop ProcessID
                if (port != null)
                {
                    Process pbiProcess = Process.GetProcessById(pbiProcessID);
                    string pbiReportName = pbiProcess.MainWindowTitle;
                    int ssasProcessID = ssasProcess.Id;
                    var connectionString = $"Data Source=localhost:{port};Integrated Security=SSPI";

                    SemanticModel semanticModel = new SemanticModel(pbiReportName, connectionString);
                    semanticModels.Add(semanticModel);
                }
            }
            
            return semanticModels;
        }

        private static int GetParentPID(int pid)
        {
            using (Process p = new Process())
            {
                ProcessStartInfo ps = new ProcessStartInfo();
                ps.RedirectStandardOutput = true;
                ps.RedirectStandardInput = true;
                ps.RedirectStandardError = true;
                ps.CreateNoWindow = true;
                ps.UseShellExecute = false;
                ps.Arguments = "(gwmi win32_process | ? processid -eq  " + pid + ").ParentProcessId";
                ps.FileName = "powershell.exe";
                ps.WindowStyle = ProcessWindowStyle.Hidden;

                p.StartInfo = ps;
                p.Start();

                StreamReader stdOut = p.StandardOutput;
                StreamReader stdErr = p.StandardError;

                string content = stdOut.ReadToEnd();
                string err = stdErr.ReadToEnd();
                string exitCode = p.ExitCode.ToString();

                if (exitCode != "0")
                    throw new ArgumentException("Could not get information from process " + pid);

                return int.Parse(content);
            }
        }

        private static int? GetProcessAttachedPort(int pid)
        {
            int port;
            using (Process p = new Process())
            {
                ProcessStartInfo processStartInfo = new ProcessStartInfo();
                processStartInfo.UseShellExecute = false;
                processStartInfo.CreateNoWindow = true;
                processStartInfo.Arguments = "-nao -p TCP";
                processStartInfo.FileName = "netstat.exe";
                processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                processStartInfo.RedirectStandardInput = true;
                processStartInfo.RedirectStandardOutput = true;
                processStartInfo.RedirectStandardError = true;

                p.StartInfo = processStartInfo;
                p.Start();

                StreamReader stdOutput = p.StandardOutput;
                StreamReader stdError = p.StandardError;
                string output = stdOutput.ReadToEnd();
                string exitStatus = p.ExitCode.ToString();

                if (exitStatus != "0")
                    throw new ArgumentException("Process " + pid + " has no netstat entries");

                string[] lines = output.Split(new string[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                string processLine = lines.FirstOrDefault(x => x.EndsWith(pid.ToString()));

                if (processLine is null)
                    throw new ArgumentException("Process " + pid + " has no open ports");

                Match m = Regex.Match(processLine, "(?<=127\\.0\\.0\\.1:)[0-9]+");

                bool success = int.TryParse(m.Groups[0].Value, out port);
                if (!success)
                    return null;
            }
            if (port == default)
                return null;

            return port;
        }

        public static ResourceDictionary GetLanguageDictionary()
        {
            ResourceDictionary languageDict = new ResourceDictionary();
            switch (Thread.CurrentThread.CurrentCulture.Name)
            {
                //case "fr-FR":
                    //languageDict.Source = new Uri("pack://application:,,,/FindMyMeasure.Gui;component/Resources/StringResources.fr-FR.xaml", UriKind.Absolute);
                    //break;
                default:
                    languageDict.Source = new Uri("pack://application:,,,/FindMyMeasure.Gui;component/Resources/StringResources.xaml", UriKind.Absolute);
                    break;
            }
            return languageDict;
        }

    }
}
