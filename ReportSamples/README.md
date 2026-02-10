# Report samples

This is a collection of PowerBI samples you can use to try out the software. \
This folder also includes an SSAS model backup used for testing. \
They are also used in the tests.

Most of the samples are from Microsoft. I had to modify some for testing purposes.

Here is the list of all the samples and their sources : 

- `Corporate Spend.pbix` : This is the base report from the Microsoft samples. You can find the wiki [here](https://learn.microsoft.com/en-us/power-bi/create-reports/sample-corporate-spend) and the source file [here](https://github.com/microsoft/powerbi-desktop-samples/blob/main/new-power-bi-service-samples/Corporate%20Spend.pbix). 
- `Store Sales.pbix` : This is the base report from the Microsoft samples. You can find the wiki [here](https://learn.microsoft.com/en-us/power-bi/create-reports/sample-store-sales) and the source file [here](https://github.com/microsoft/powerbi-desktop-samples/blob/main/new-power-bi-service-samples/Store%20Sales.pbix)
- `Store Sales - With broken visuals.pbix` : This is a modified version of the `Store Sales.pbix` report where I deleted the measure `Total Sales Var %`, thus making some visuals broken. This is used in the code example [ListBrokenVisuals](../Examples/ListBrokenVisuals)
- `SSAS_Source1_Basic_visuals.pbix` : This is a rough report I created quickly to test Reports in Live connection mode. The data source is the `Adventure Works Internet Sales Database` I downloaded from this [microsoft repo](https://github.com/Microsoft/sql-server-samples/releases/tag/adventureworks-analysis-services)

You'll also find the `Adventure Works Internet Sales Database.abf` backup file for SSAS. 

