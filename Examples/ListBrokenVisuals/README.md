# ListBrokenVisuals Example

Purpose

The `ListBrokenVisuals` example is a small console tool that analyzes a Power BI report and prints warnings for visuals that reference missing measures or columns in the semantic model.

## Prerequisites to build and run

- Visual Studio2017/2019/2022 (any edition)
- Power BI Desktop (the target report must be open in Power BI Desktop)
- .NET Framework4.7.2 (project targets this framework)
- The sample report included in the repository: `reportExamples\Store Sales - With broken visuals.pbix`

## Running the example

- Open the sample report in Power BI Desktop: `reportExamples\Store Sales - With broken visuals.pbix`.
- Open the `ListBrokenVisuals` project in Visual Studio and run it, or build the project and run the produced executable.
- The tool prints a header and then any detected missing column/measure warnings to standard output.


## How it works

1. The program attempts to find a local semantic model that matches the name `Store Sales - With broken visuals` using `Utils.ListAllLocalSemanticModels()`.
2. If found, it calls `LoadFullModel()` to load tables, columns and measures metadata.
3. A `WarningSubscriber` (local class in `Program.cs`) is registered with `AnalysisWarningPublisher` to receive warnings:
 - `IMissingColumnWarningSubscriber.OnWarningReceived` for `MissingColumnWarning`
 - `IMissingMeasureWarningSubscriber.OnWarningReceived` for `MissingMeasureWarning`
4. The sample report is loaded with `PowerBIReport.LoadFromPbix(...)`. During loading/analysis, the library may publish missing-column or missing-measure warnings. Registered subscribers print human-readable messages to the console.


## Expected output

Console lines similar to:

The column <ColumnName> from table <TableName> used in <VisualName> doesn't exist
The measure <MeasureName> from table <TableName> used in <VisualName> doesn't exist


## Relevant files

- `Program.cs` - example entry point and `WarningSubscriber` implementation
- `FindMyMeasure\PowerBI\AnalysisWarningPublisher.cs` - warning publisher used by the analysis components
- `FindMyMeasure\WarningClasses\MissingColumnWarning.cs` and `MissingMeasureWarning.cs` - warning types

