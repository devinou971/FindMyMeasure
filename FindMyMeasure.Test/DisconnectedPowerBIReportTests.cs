using FindMyMeasure.Database;
using FindMyMeasure.PowerBI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace FindMyMeasure.Test
{
    [TestClass]
    public class DisconnectedPowerBIReportTests
    {
        static SemanticModel StoreSalesModel;
        static PowerBIReport StoreSalesReport;

        static SemanticModel CorporateSpendModel;
        static PowerBIReport CorporateSpendReport;

        [ClassInitialize]
        public static void SetupClass(TestContext testContext)
        {
            // Runs once before everything
            StoreSalesModel = new SemanticModel("Store Sales", SemanticModel.RunMode.DisconnectedMode);
            CorporateSpendModel = new SemanticModel("Corporate Spend", SemanticModel.RunMode.DisconnectedMode);

            StoreSalesReport = PowerBIReport.LoadFromPbix(@"testReports\Store Sales.pbix", StoreSalesModel, true, true);
            CorporateSpendReport = PowerBIReport.LoadFromPbix(@"testReports\Corporate Spend.pbix", CorporateSpendModel, true, true);
        }

        [TestMethod]
        public void TestMeasureCount()
        {
            Assert.AreEqual(11, StoreSalesModel.GetMeasures().Count());
            Assert.AreEqual(7, CorporateSpendModel.GetMeasures().Count());
        }

    }
}
