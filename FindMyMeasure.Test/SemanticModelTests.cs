using FindMyMeasure.Database;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace FindMyMeasure.Test
{
    [TestClass]
    public class SemanticModelTests
    {

        static SemanticModel corporateSpendModel;
        static SemanticModel storeSalesModel;

        [AssemblyInitialize]
        public static void SetupEnvironment(TestContext testContext)
        {
            // Runs once before everything

        }

        [AssemblyCleanup]
        public static void TearDown()
        {
            // Runs once after everything
        }
        

        
        [ClassInitialize]
        public static void SetupClass(TestContext testContext)
        {
            // Runs at initialization of instance of class

            var semanticModels = Utils.ListAllLocalSemanticModels();
            corporateSpendModel = semanticModels.FirstOrDefault(x => x.Name == "Corporate Spend");
            storeSalesModel = semanticModels.FirstOrDefault(x => x.Name == "Store Sales");
            if (corporateSpendModel == null)
            {
                throw new Exception("Couldn't find the semantic mode of \"Corporate Spend.pbix\" report. Are you sure the report is opened ?");
            }
            if (storeSalesModel == null)
            {
                throw new Exception("Couldn't find the semantic mode of \"Store Sales.pbix\" report. Are you sure the report is opened ?");
            }

            corporateSpendModel.LoadFullModel();
            storeSalesModel.LoadFullModel();
        }

        [ClassCleanup]
        public static void CleanupClass() { 
            // Runs once after the tests in this class
        }


        
        [TestInitialize]
        public void SetupTest()
        {
            //Runs before every test
        }

        [TestCleanup]
        public void TearDownTest() { 
            // Runs after every test
        }
        
        

        [TestMethod]
        public void TestLoadMeasures()
        {
            Assert.AreEqual(15, corporateSpendModel.GetMeasures().Count());
            Assert.AreEqual(32, storeSalesModel.GetMeasures().Count());
        }

        [TestMethod]
        public void TestLoadRelationships()
        {
            Assert.AreEqual(7+1, corporateSpendModel.GetRelationships().Count()); // There is 1 more relationship due to the hidden table "LocalDate_XXXX"
        }
    }
}
