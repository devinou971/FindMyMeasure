using FindMyMeasure.Gui.Models;
using FindMyMeasure.Interfaces;
using FindMyMeasure.PowerBI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace FindMyMeasure.Gui.Services
{
    internal class ExportResultsService
    {
        private static string[] charactersToEscape = new string[] {"\\", "\"", "'"};
        public static async Task ExportAnalysisResultsToCSV(IEnumerable<DataGridUsageRecord> records, string outputPath, Encoding encoding, char sep = ',', char escapeCharacter = '\\')
        {
            var csvContent = BuildCSVContent(records, sep, escapeCharacter);
            WriteTextToFile(csvContent, outputPath, encoding);
        }

        private static string EscapeCharactersInString(string str, char escapeCharacter)
        {
            string result = str;
            foreach(string character in ExportResultsService.charactersToEscape)
            {
                result = result.Replace(character, $"{escapeCharacter}{character}");
            }
            return result;
        }

        private static string BuildCSVContent(IEnumerable<DataGridUsageRecord> records, char sep, char escapeCharacter)
        {
            List<string> lines = new List<string>();
            lines.Add($"Model{sep}ArtifactType{sep}ArtifactName{sep}ArtifactTableName{sep}Status{sep}NumberOfUses{sep}UsedInType{sep}UsedInName{sep}UsedInTable{sep}UsedInReport{sep}UsedInReportPage");

            foreach (DataGridUsageRecord record in records)
            {
                string artifactName = EscapeCharactersInString(record.Name, escapeCharacter);
                string artifactTableName = EscapeCharactersInString(record.Table, escapeCharacter);

                if (record.NbOfUsage == 0)
                    lines.Add($"{record.Model}{sep}{record.Type}{sep}{artifactName}{sep}{artifactTableName}{sep}{record.UsageState.ToString()}{sep}{record.NbOfUsage}{sep}{sep}{sep}{sep}{sep}");
                else
                {
                    foreach (IModelReferenceTarget dependent in record.DataInput.GetDependents())
                    {
                        var usedInType = dependent.Type;
                        var usedInName = EscapeCharactersInString(dependent.Name, escapeCharacter);
                        var usedInTable = (dependent is IDataInput) ? EscapeCharactersInString(((IDataInput)dependent).ParentTable.Name, escapeCharacter) : "";
                        var usedInReport = "";
                        var usedInReportPage = "";

                        // Extract report and page context if this is a PowerBI object
                        if (dependent is IPowerBILeafNode)
                        {
                            switch (dependent)
                            {
                                case PowerBIReport pbiReport:
                                    {
                                        usedInReport = pbiReport.Name;
                                        break;
                                    }
                                case ReportPage reportPage:
                                    {
                                        usedInReport = reportPage.GetPowerBIReport().Name;
                                        usedInReportPage = reportPage.Name;
                                        break;
                                    }
                                case Visual visual:
                                    {
                                        usedInReport = visual.GetReportPage().GetPowerBIReport().Name;
                                        usedInReportPage = visual.GetReportPage().Name;
                                        break;
                                    }
                                default:
                                    break;
                            }
                        }
                        lines.Add($"{record.Model}{sep}{record.Type}{sep}{artifactName}{sep}{artifactTableName}{sep}{record.UsageState}{sep}{record.NbOfUsage}{sep}{usedInType}{sep}{usedInName}{sep}{usedInTable}{sep}{usedInReport}{sep}{usedInReportPage}");
                    }
                }
            }
            return string.Join("\n", lines);
        }

        private static void WriteTextToFile(string filePath, string text, Encoding encoding)
        {
            FileAttributes attributes = File.GetAttributes(filePath);
            if (attributes.HasFlag(FileAttributes.Directory))
            {
                throw new Exception("Path is a directory, not a file");
            }

            File.WriteAllText(filePath, text, encoding);
            
        }
    }
}
