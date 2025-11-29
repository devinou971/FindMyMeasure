# ExportUsages Example

Overview :

This small example demonstrates how to export usage information using the FindMyMeasure library. 
It aims to mimic the export functionnality of MeasureKiller, but as a standalone example project.

The exported CSV file will contain the following columns:
- `Model` : The name of the Semantic Model
- `Type` : Measure, Column or Calculated Column
- `Name` : Name of the measure / Column
- `Table` : Table name
- `Status` : Used, Unused or UsedByUnused
- `NumberOfUses` : #
- `UsedInType` : The dependent object. Could be from the semantic model, like a Measure, or it could be from the report like a visual
- `UsedInName` : The name of the dependent
- `UsedInTable` : If the dependent is in the semantic model, then the table name will be displayed here
- `UsedInReport` : If the dependent is in the report, then the report name will be here
- `UsedInReportPage` : If the dependent is in the report, then the page name will be here


## Prerequisites to build

- Visual Studio2017/2019/2022 (any edition)
- The solution targets `.NET Framework4.7.2`

## How to run

From Visual Studio

1. Open the reports listed in `reportList.txt` in Power BI Desktop.
2. Open the solution `FindMyMeasure.sln` in Visual Studio.
3. Set `ExportUsages` (or the solution start project) and build the solution.
4. Run the `ExportUsages` project (F5 or Ctrl+F5) or run the compiled executable from the output folder.


## How it works

1. The example reads `reportList.txt` to get the list of reports to process.
2. For each entry it uses the FindMyMeasure library to gather usage information.

`public string Utils.ExtractConnectionStringFromReport( string reportPath )` is used to extract the connection string from a pbix file.

When the connection string is obtained, the semantic model is built.
```csharp
SemanticModel semanticModel = null
// Find out the model name and connection string
semanticModel = new SemanticModel(modelName, connectionString);
// ...
semanticModel.LoadFullModel();
```

Finally, the PowerBI report is opened and analysed to extract usage information.
```csharp
PowerBIReport powerBIReport = PowerBIReport.LoadFromPbix(reportPath, semanticModel, Properties.Settings.Default.AnalyseHiddenPages, Properties.Settings.Default.AnalyseHiddenVisuals);
```

3. It exports the results to the output file. By default, the output file is `output.csv`.


### Configuration

- Edit `reportList.txt` to control which reports are exported.
- Edit `App.config` to adjust runtime settings:
  - `CsvDelimiter` : The delimiter used in the CSV file. Default is `;`
  - `ExportPath` : The path where the CSV file will be exported. Default is `output.csv` in the output directory.
  - `AnalyseHiddenPages` : If set to true, hidden pages will also be analysed. Default is false.
  - `AnalyseHiddenVisuals` : If set to true, hidden visuals will also be analysed. Default is true.

