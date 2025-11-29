# FindMyMeasure Example : Exporting Measure Localisation

This project aims to show how to use the FindMyMeasure SDK to export measure localisation data.

It will say wich measure of a semantic model is used in which report and page.

It will export the localisation data to a CSV file named `output.csv` in the output directory.

It will have 3 columns:
- Model Name
- Measure Name
- Report Name
- Page Name

## How to run

From Visual Studio

1. Open the reports listed in `reportList.txt` in Power BI Desktop.
2. Open the solution `FindMyMeasure.sln` in Visual Studio.
3. Set `ExportMeasureLocation` (or the solution start project) and build the solution.
4. Run the `ExportMeasureLocation` project (F5 or Ctrl+F5) or run the compiled executable from the output folder.


## How it works

This example will be using several Power BI Desktop files located in the same directory as the executable.

### The report list

You can list the reports to analyse in the "reportList.txt" file located in the same directory as the executable."
Here is an example of the content of the "reportList.txt" file:
```
C:\Users\Contoso\Store Sales.pbix
C:\Users\Contoso\Revenue Opportunities.pbix
C:\Users\Contoso\Regional Sales Sample.pbix
C:\Users\Contoso\Competitive Marketing Analysis.pbix
```


### Configuring the exporter

- Edit `reportList.txt` to control which reports are exported.
- Edit `App.config` to adjust runtime settings:
  - `CsvDelimiter` : The delimiter used in the CSV file. Default is `;`
  - `ExportPath` : The path where the CSV file will be exported. Default is `output.csv` in the output directory.
  - `AnalyseHiddenPages` : If set to true, hidden pages will also be analysed. Default is false.
  - `AnalyseHiddenVisuals` : If set to true, hidden visuals will also be analysed. Default is true.

