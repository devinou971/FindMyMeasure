using FindMyMeasure.Gui.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;

namespace FindMyMeasure.Gui.Test
{
    [TestClass]
    public class ReportViewModelTests
    {
        [TestMethod]
        public void TestConstructor()
        {
            ReportSelectionViewModel viewModel = new ReportSelectionViewModel();
            Assert.AreEqual(true, viewModel.AnalyseHiddenPagesAllChecked);
            Assert.AreEqual(true, viewModel.AnalyseHiddenVisualsAllChecked);
        }

        [TestMethod]
        public void TestAddReport()
        {
            ReportSelectionViewModel viewModel = new ReportSelectionViewModel();
            viewModel.AddReportConfigToList(@"testReports\SSAS_Source1_Basic_visuals.pbix");
            Assert.AreEqual(1, viewModel.ReportConfigList.Count);
        }

        [TestMethod]
        public void TestAddReportWrongPathError()
        {
            ReportSelectionViewModel viewModel = new ReportSelectionViewModel();
            Assert.Throws<FileNotFoundException>(() => { viewModel.AddReportConfigToList(@"not a real report"); });
        }

        [TestMethod]
        public void TestSetAnalyseHiddenPagesGlobally()
        {
            ReportSelectionViewModel viewModel = new ReportSelectionViewModel();
            viewModel.AddReportConfigToList(@"testReports\SSAS_Source1_Basic_visuals.pbix");

            viewModel.SetAllReportsToAnalyseHiddenPages(false);
            Assert.IsFalse(viewModel.AnalyseHiddenPagesAllChecked);
            Assert.IsFalse(viewModel.ReportConfigList[0].AnalyseHiddenPages);

            viewModel.SetAllReportsToAnalyseHiddenPages(true);
            Assert.IsTrue(viewModel.AnalyseHiddenPagesAllChecked);
            Assert.IsTrue(viewModel.ReportConfigList[0].AnalyseHiddenPages);
        }

        [TestMethod]
        public void TestSetAnalyseHiddenPagesLocally()
        {
            ReportSelectionViewModel viewModel = new ReportSelectionViewModel();
            viewModel.AddReportConfigToList(@"testReports\SSAS_Source1_Basic_visuals.pbix");

            viewModel.ReportConfigList[0].AnalyseHiddenPages = false;
            Assert.IsFalse(viewModel.AnalyseHiddenPagesAllChecked);

            viewModel.ReportConfigList[0].AnalyseHiddenPages = true;
            Assert.IsTrue(viewModel.AnalyseHiddenPagesAllChecked);
        }

        [TestMethod]
        public void TestSetAnalyseHiddenVisualsGlobally()
        {
            ReportSelectionViewModel viewModel = new ReportSelectionViewModel();
            viewModel.AddReportConfigToList(@"testReports\SSAS_Source1_Basic_visuals.pbix");

            viewModel.SetAllReportsToAnalyseHiddenVisuals(false);
            Assert.IsFalse(viewModel.AnalyseHiddenVisualsAllChecked);
            Assert.IsFalse(viewModel.ReportConfigList[0].AnalyseHiddenVisuals);

            viewModel.SetAllReportsToAnalyseHiddenVisuals(true);
            Assert.IsTrue(viewModel.AnalyseHiddenVisualsAllChecked);
            Assert.IsTrue(viewModel.ReportConfigList[0].AnalyseHiddenVisuals);
        }

        [TestMethod]
        public void TestSetAnalyseHiddenVisualsLocally()
        {
            ReportSelectionViewModel viewModel = new ReportSelectionViewModel();
            viewModel.AddReportConfigToList(@"testReports\SSAS_Source1_Basic_visuals.pbix");

            viewModel.ReportConfigList[0].AnalyseHiddenVisuals = false;
            Assert.IsFalse(viewModel.AnalyseHiddenVisualsAllChecked);

            viewModel.ReportConfigList[0].AnalyseHiddenVisuals = true;
            Assert.IsTrue(viewModel.AnalyseHiddenVisualsAllChecked);
        }

        [TestMethod]
        public void TestRemoveReport()
        {
            ReportSelectionViewModel viewModel = new ReportSelectionViewModel();
            viewModel.AddReportConfigToList(@"testReports\SSAS_Source1_Basic_visuals.pbix");
            var id = viewModel.ReportConfigList[0].ReportId;
            viewModel.RemoveReportCommand.Execute(id);
            Assert.AreEqual(0, viewModel.ReportConfigList.Count);
        }

        [TestMethod]
        public void TestStartAnalysis() {
            ReportSelectionViewModel viewModel = new ReportSelectionViewModel();
            viewModel.AddReportConfigToList(@"testReports\SSAS_Source1_Basic_visuals.pbix");

            viewModel.StartAnalysisCommand.Execute(null);
            while (viewModel.IsBusy) { }
            Assert.IsNotNull(viewModel.ReportAnalysisResult);
            Assert.AreEqual(7, viewModel.ReportAnalysisResult.SemanticModels.First().GetTables().Count());
            Assert.AreEqual(19, viewModel.ReportAnalysisResult.SemanticModels.First().GetMeasures().Count());
            Assert.AreEqual(4, viewModel.ReportAnalysisResult.SemanticModels.First().GetHierarchies().Count());
            Assert.AreEqual(114, viewModel.ReportAnalysisResult.SemanticModels.First().GetColumns().Count());
            Assert.AreEqual(8, viewModel.ReportAnalysisResult.SemanticModels.First().GetRelationships().Count());

        }
    }
}
