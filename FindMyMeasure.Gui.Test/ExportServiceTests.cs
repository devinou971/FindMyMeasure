using FindMyMeasure.Database;
using FindMyMeasure.Gui.Models;
using FindMyMeasure.Gui.Services;
using FindMyMeasure.PowerBI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FindMyMeasure.Gui.Test
{
    [TestClass]
    public class ExportServiceTests
    {
        public List<DataGridUsageRecord> records;

        [TestInitialize]
        public void GenerateMockData()
        {
            ulong idGen = 0;

            records = new List<DataGridUsageRecord>();
            Table table1 = new Table("Table1", idGen++);
            Table table2 = new Table("Table2", idGen++);

            Column c11 = new Column(idGen++, "column1.1", table1);
            Column c12 = new Column(idGen++, "column1.2", table1);
            Column c21 = new Column(idGen++, "column2.1", table2);
            Column c22 = new Column(idGen++, "column2.2", table2);
            Column c23 = new Column(idGen++, "calculatedColumn2.3", "expr", table2);

            Measure m11 = new Measure(idGen++, "measure1.1", "expr", table1);
            Measure m21 = new Measure(idGen++, "measure2.1", "expr", table2);

            m11.AddDependent(m21);
            c22.AddDependent(c23);
            c22.AddDependent(m11);

            var report1 = new PowerBIReport("report1", "", null);
            var reportPage1 = new ReportPage("page1", "page1", report1);
            var visual1 = new Visual("visual1", "title1", "card", reportPage1);

            c22.AddDependent(visual1);

            this.records.Add(new DataGridUsageRecord(c11, "SemanticModel1"));
            this.records.Add(new DataGridUsageRecord(c12, "SemanticModel1"));
            this.records.Add(new DataGridUsageRecord(c21, "SemanticModel1"));
            this.records.Add(new DataGridUsageRecord(c22, "SemanticModel1"));
            this.records.Add(new DataGridUsageRecord(c23, "SemanticModel1"));
            this.records.Add(new DataGridUsageRecord(m11, "SemanticModel1"));
            this.records.Add(new DataGridUsageRecord(m21, "SemanticModel1"));
        }

        [TestMethod]
        public void ExportToCSVTest()
        {
            
            ExportResultsService.ExportAnalysisResultsToCSV(this.records, "testResult.csv", Encoding.UTF8, ',');

            Assert.IsTrue(File.Exists("testResult.csv"));
            string content = File.ReadAllText("testResult.csv", Encoding.UTF8);

            Assert.AreEqual("Model,ArtifactType,ArtifactName,ArtifactTableName,Status,NumberOfUses,UsedInType,UsedInName,UsedInTable,UsedInReport,UsedInReportPage\n" +
                "\"SemanticModel1\",Column,\"column1.1\",\"Table1\",Unused,0,,,,,\n" +
                "\"SemanticModel1\",Column,\"column1.2\",\"Table1\",Unused,0,,,,,\n" +
                "\"SemanticModel1\",Column,\"column2.1\",\"Table2\",Unused,0,,,,,\n" +
                "\"SemanticModel1\",Column,\"column2.2\",\"Table2\",Used,3,CalculatedColumn,\"calculatedColumn2.3\",\"Table2\",\"\",\"\"\n" +
                "\"SemanticModel1\",Column,\"column2.2\",\"Table2\",Used,3,Measure,\"measure1.1\",\"Table1\",\"\",\"\"\n" +
                "\"SemanticModel1\",Column,\"column2.2\",\"Table2\",Used,3,card,\"visual1\",\"\",\"report1\",\"page1\"\n" +
                "\"SemanticModel1\",CalculatedColumn,\"calculatedColumn2.3\",\"Table2\",Unused,0,,,,,\n" +
                "\"SemanticModel1\",Measure,\"measure1.1\",\"Table1\",UsedByUnused,1,Measure,\"measure2.1\",\"Table2\",\"\",\"\"\n" +
                "\"SemanticModel1\",Measure,\"measure2.1\",\"Table2\",Unused,0,,,,,", content);

        }

        [TestMethod]
        public void ExportToWindows1252CSVTest()
        {

            ExportResultsService.ExportAnalysisResultsToCSV(this.records, "testResult.csv", Encoding.GetEncoding("Windows-1252"), ';');

            Assert.IsTrue(File.Exists("testResult.csv"));
            string content = File.ReadAllText("testResult.csv", Encoding.GetEncoding("Windows-1252"));

            Assert.AreEqual("Model;ArtifactType;ArtifactName;ArtifactTableName;Status;NumberOfUses;UsedInType;UsedInName;UsedInTable;UsedInReport;UsedInReportPage\n" +
                "\"SemanticModel1\";Column;\"column1.1\";\"Table1\";Unused;0;;;;;\n" +
                "\"SemanticModel1\";Column;\"column1.2\";\"Table1\";Unused;0;;;;;\n" +
                "\"SemanticModel1\";Column;\"column2.1\";\"Table2\";Unused;0;;;;;\n" +
                "\"SemanticModel1\";Column;\"column2.2\";\"Table2\";Used;3;CalculatedColumn;\"calculatedColumn2.3\";\"Table2\";\"\";\"\"\n" +
                "\"SemanticModel1\";Column;\"column2.2\";\"Table2\";Used;3;Measure;\"measure1.1\";\"Table1\";\"\";\"\"\n" +
                "\"SemanticModel1\";Column;\"column2.2\";\"Table2\";Used;3;card;\"visual1\";\"\";\"report1\";\"page1\"\n" +
                "\"SemanticModel1\";CalculatedColumn;\"calculatedColumn2.3\";\"Table2\";Unused;0;;;;;\n" +
                "\"SemanticModel1\";Measure;\"measure1.1\";\"Table1\";UsedByUnused;1;Measure;\"measure2.1\";\"Table2\";\"\";\"\"\n" +
                "\"SemanticModel1\";Measure;\"measure2.1\";\"Table2\";Unused;0;;;;;", content);
        }
    }
}
