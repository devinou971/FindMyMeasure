using Microsoft.AnalysisServices.AdomdClient;
using FindMyMeasure.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FindMyMeasure.Database
{
    /// <summary>
    /// Represents a semantic model (Analysis Services, local PowerBI Desktop, etc.) containing tables, columns, measures, and relationships.
    /// Can operate in connected mode (querying a live model) or disconnected mode (accepting all references without validation).
    /// </summary>
    public class SemanticModel : IEquatable<SemanticModel>
    {
        private string _name;
        private string _connectionString;
        private HashSet<Table> _tables = new HashSet<Table>();
        private HashSet<Column> _columns = new HashSet<Column>();
        private HashSet<Measure> _measures = new HashSet<Measure>();
        private HashSet<Relationship> _relationships = new HashSet<Relationship>();
        private RunMode _currentRunMode;

        /// <summary>
        /// Defines how the semantic model operates.
        /// </summary>
        public enum RunMode
        {
            /// <summary>
            /// Connected mode: Queries a live semantic model (Analysis Services, PowerBI Desktop) to load metadata.
            /// This mode provides accurate dependency information but requires a valid connection.
            /// </summary>
            ConnectedMode,
            /// <summary>
            /// Disconnected mode: Does not connect to a semantic model. All referenced tables, columns, and measures
            /// are automatically accepted and created on-demand. Useful for analyzing reports without access to the model,
            /// without internet connection, or in testing scenarios.
            /// </summary>
            DisconnectedMode
        }

        /// <summary>
        /// Gets the name of this semantic model.
        /// </summary>
        public string Name { get => _name; }

        /// <summary>
        /// Gets the current operating mode of this semantic model.
        /// </summary>
        public RunMode CurrentRunMode { get => _currentRunMode; }

        /// <summary>
        /// Gets the connection string used to connect to this semantic model.
        /// </summary>
        public string ConnectionString { get => _connectionString; }

        /// <summary>
        /// Initializes a new instance of the SemanticModel class.
        /// </summary>
        /// <param name="name">The name of the semantic model.</param>
        /// <param name="connectionString">The connection string to the semantic model.</param>
        /// <param name="runMode">The operating mode (connected or disconnected).</param>
        public SemanticModel(string name, string connectionString, RunMode runMode)
        {
            this._name = name;
            this._connectionString = connectionString;
            this._currentRunMode = runMode;
        }

        public SemanticModel(string name, RunMode runMode) : this(name, "", runMode) { }

        /// <summary>
        /// Initializes a new instance in connected mode.
        /// </summary>
        /// <param name="name">The name of the semantic model.</param>
        /// <param name="connectionString">The connection string to the semantic model.</param>
        public SemanticModel(string name, string connectionString) : this(name, connectionString, RunMode.ConnectedMode) { }

        /// <summary>
        /// Initializes a new instance in connected mode with no explicit name.
        /// </summary>
        /// <param name="connectionString">The connection string to the semantic model.</param>
        public SemanticModel(string connectionString) : this("", connectionString, RunMode.ConnectedMode) { }

        /// <summary>
        /// Changes the operating mode of this semantic model.
        /// </summary>
        /// <param name="runMode">The new operating mode.</param>
        public void SetRunMode(RunMode runMode)
        {
            this._currentRunMode = runMode;
        }

        /// <summary>
        /// Loads all metadata from the semantic model (tables, columns, measures, relationships, and dependencies).
        /// In disconnected mode, this method does nothing.
        /// </summary>
        /// <exception cref="Exception">Thrown if the connection fails or metadata queries return invalid data.</exception>
        public void LoadFullModel()
        {
            if(this._currentRunMode == RunMode.DisconnectedMode)
                return;
            
            AdomdConnection adomdConnection = new AdomdConnection(this._connectionString);
            try
            {
                adomdConnection.Open();
                this.LoadTables(adomdConnection);
                this.LoadColumns(adomdConnection);
                this.LoadMeasures(adomdConnection);
                this.LoadRelationships(adomdConnection);
                this.LoadDependencies(adomdConnection);
            } catch
            {
                throw;
            } finally
            {
                adomdConnection.Close();
            }
        }

        /// <summary>
        /// Loads all tables from the semantic model using the TMSCHEMA_TABLES system view.
        /// </summary>
        /// <param name="adomdConnection">An open connection to the semantic model.</param>
        /// <exception cref="Exception">Thrown if the query fails.</exception>
        private void LoadTables(AdomdConnection adomdConnection)
        {
            AdomdCommand getTablesCommand = adomdConnection.CreateCommand();
            try
            {
                getTablesCommand.CommandText = "SELECT * from $SYSTEM.TMSCHEMA_TABLES";
                AdomdDataReader tableRecords = getTablesCommand.ExecuteReader();
                foreach (var tableRecord in tableRecords)
                {
                    ulong tableId = (ulong)tableRecord.GetValue(0);
                    string tableName = tableRecord.GetString(2);

                    Table table = new Table(tableName, tableId);
                    this._tables.Add(table);
                }
                tableRecords.Close();
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Loads all columns from the semantic model using the TMSCHEMA_COLUMNS system view.
        /// Resolves each column to its parent table.
        /// </summary>
        /// <param name="adomdConnection">An open connection to the semantic model.</param>
        /// <exception cref="Exception">Thrown if the query fails or a column's parent table cannot be found.</exception>
        private void LoadColumns(AdomdConnection adomdConnection)
        {
            // TODO : Load the description property of the columns
            try
            {
                AdomdCommand getColumnsCommand = adomdConnection.CreateCommand();
                getColumnsCommand.CommandText = "select * from $SYSTEM.TMSCHEMA_COLUMNS";
                AdomdDataReader columnRecords = getColumnsCommand.ExecuteReader();
                foreach (var columnRecord in columnRecords)
                {
                    ulong tableId = (ulong)columnRecord.GetValue(1);
                    ulong columnId = (ulong)columnRecord.GetValue(0);
                    // Columns can have explicit names (user-defined) or inferred names (system-generated)
                    string explicitColumnName = columnRecord.GetString(2);
                    string inferredColumnName = columnRecord.GetString(3);
                    string expression = columnRecord.GetString(22);

                    // Use explicit name if available, otherwise use inferred name
                    string columnName = explicitColumnName is null ? inferredColumnName : explicitColumnName;

                    if (this.TryFindTableById(tableId, out Table table))
                    {
                        Column column = new Column(columnId, columnName, expression, table);
                        table.AddColumn(column);
                        this._columns.Add(column);
                    }
                    else
                    {
                        throw new Exception("Could not find the table " + tableId + " for the column " + explicitColumnName);
                    }
                }
                columnRecords.Close();
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Loads all measures from the semantic model using the TMSCHEMA_MEASURES system view.
        /// Resolves each measure to its parent table and extracts its DAX formula.
        /// </summary>
        /// <param name="adomdConnection">An open connection to the semantic model.</param>
        /// <exception cref="Exception">Thrown if the query fails or a measure's parent table cannot be found.</exception>
        private void LoadMeasures(AdomdConnection adomdConnection)
        {
            // TODO : Load the description property of the measures
            try
            {
                AdomdCommand getMeasuresCommand = adomdConnection.CreateCommand();
                getMeasuresCommand.CommandText = "select * from $SYSTEM.TMSCHEMA_MEASURES";
                AdomdDataReader measureRecords = getMeasuresCommand.ExecuteReader();
                foreach (var measureRecord in measureRecords)
                {
                    ulong tableId = (ulong)measureRecord.GetValue(1);
                    ulong measureId = (ulong)measureRecord.GetValue(0);
                    string measureName = measureRecord.GetString(2);
                    string measureFormula = measureRecord.GetString(5);

                    if (this.TryFindTableById(tableId, out Table table))
                    {
                        Measure measure = new Measure(measureId, measureName, measureFormula, table);
                        table.AddMeasure(measure);
                        this._measures.Add(measure);
                    }
                    else
                        throw new Exception("Could not find the table " + tableId + " for measure " + measureName);
                }
                measureRecords.Close();
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Loads all relationships from the semantic model using the TMSCHEMA_RELATIONSHIPS system view.
        /// Relationships connect columns and define table cardinality.
        /// </summary>
        /// <param name="adomdConnection">An open connection to the semantic model.</param>
        /// <exception cref="Exception">Thrown if the query fails or a relationship's columns cannot be found.</exception>
        private void LoadRelationships(AdomdConnection adomdConnection)
        {
            //TODO : Load the "isActive" property of the relationship
            try
            {
                AdomdCommand getRelationshipsCommand = adomdConnection.CreateCommand();
                getRelationshipsCommand.CommandText = "select * from $SYSTEM.TMSCHEMA_RELATIONSHIPS";
                AdomdDataReader relationshipRecords = getRelationshipsCommand.ExecuteReader();
                foreach (var relationshipRecord in relationshipRecords)
                {
                    ulong fromColumnId = (ulong)relationshipRecord.GetValue(9);
                    ulong toColumnId = (ulong)relationshipRecord.GetValue(12);
                    string relationshipName = relationshipRecord.GetString(2);

                    // Relationships mark both source and target columns as used
                    if (this.TryFindColumnById(fromColumnId, out Column fromColumn) && this.TryFindColumnById(toColumnId, out Column toColumn))
                    {
                        Relationship relationship = new Relationship(relationshipName, fromColumn, toColumn);

                        fromColumn.AddDependent(relationship);
                        toColumn.AddDependent(relationship);

                        this._relationships.Add(relationship);
                    }
                    else
                        throw new Exception("Could not find the column " + fromColumnId + " or column " + toColumnId + " for relationship " + relationshipName);
                }
                relationshipRecords.Close();
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Loads dependencies between measures, columns, and calculated tables.
        /// Uses the DISCOVER_CALC_DEPENDENCY system view to find which objects reference which other objects.
        /// </summary>
        /// <param name="adomdConnection">An open connection to the semantic model.</param>
        /// <exception cref="Exception">Thrown if the query fails or a referenced object cannot be found.</exception>
        private void LoadDependencies(AdomdConnection adomdConnection)
        {
            try
            {
                AdomdCommand getDependenciesCommand = adomdConnection.CreateCommand();
                // Query only dependencies between data model objects (columns, measures, calculated tables)
                getDependenciesCommand.CommandText = @"
                    select * from $SYSTEM.DISCOVER_CALC_DEPENDENCY
                    WHERE (OBJECT_TYPE = 'CALC_COLUMN' or OBJECT_TYPE = 'MEASURE' or OBJECT_TYPE = 'CALC_TABLE') 
                    and (REFERENCED_OBJECT_TYPE = 'CALC_COLUMN' or REFERENCED_OBJECT_TYPE = 'COLUMN' or REFERENCED_OBJECT_TYPE = 'MEASURE')
                "; 
                AdomdDataReader dependencyRecords = getDependenciesCommand.ExecuteReader();

                foreach (var dependencyRecord in dependencyRecords)
                {
                    // The object that has the dependency (the one doing the referencing)
                    string objectType = dependencyRecord.GetString(1);
                    string tableName = dependencyRecord.GetString(2);
                    string objectName = dependencyRecord.GetString(3);

                    // The object being referenced
                    string referencedObjectType = dependencyRecord.GetString(5);
                    string referencedTableName = dependencyRecord.GetString(6);
                    string referencedObjectName = dependencyRecord.GetString(7);

                    // Find the referenced object (the dependency source)
                    IDataInput dataInput;
                    if (this.TryFindColumnByName(referencedObjectName, referencedTableName, out Column dependencyCol))
                        dataInput = dependencyCol as IDataInput;
                    else if (this.TryFindMeasureByName(referencedObjectName, out FindMyMeasure.Database.Measure dependencyMeasure))
                        dataInput = dependencyMeasure as IDataInput;
                    else
                        throw new Exception("Couldn't find dependency " + referencedObjectType + " : " + referencedTableName + "." + referencedObjectName);

                    // Find the object that uses the dependency (the consumer)
                    IModelReferenceTarget dataOutput;
                    if (this.TryFindColumnByName(objectName, tableName, out Column column))
                        dataOutput = column as IModelReferenceTarget;
                    else if (this.TryFindMeasureByName(objectName, out FindMyMeasure.Database.Measure measure))
                        dataOutput = measure as IModelReferenceTarget;
                    else if (this.TryFindTableByName(objectName, out Table table))
                        dataOutput = table as IModelReferenceTarget;
                    else
                        throw new Exception("Couldn't find " + objectType + " : " + tableName + "." + objectName + " during dependency building");

                    // Register the dependency relationship
                    dataInput.AddDependent(dataOutput);
                }
                dependencyRecords.Close();
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Attempts to find a table by its ID.
        /// </summary>
        /// <param name="tableId">The unique ID of the table.</param>
        /// <param name="table">The table if found; otherwise null.</param>
        /// <returns>True if the table was found; otherwise false.</returns>
        private bool TryFindTableById(ulong tableId, out Table table)
        {
            table = this._tables.FirstOrDefault(t => t.TableId == tableId);
            return table != null;
        }

        /// <summary>
        /// Attempts to find or create a table by name.
        /// In disconnected mode, creates the table if it doesn't exist.
        /// </summary>
        /// <param name="name">The name of the table.</param>
        /// <param name="table">The table if found or created; otherwise null.</param>
        /// <returns>True if the table was found or created; otherwise false.</returns>
        public bool TryFindTableByName(String name, out Table table)
        {
            // In disconnected mode, auto-create missing tables
            if (this._currentRunMode == RunMode.DisconnectedMode)
                this._tables.Add(new Table(name));
            table = this._tables.FirstOrDefault(t => t.Name == name);
            return table != null;
        }

        /// <summary>
        /// Attempts to find or create a measure by name.
        /// In disconnected mode, creates the measure if it doesn't exist.
        /// </summary>
        /// <param name="name">The name of the measure.</param>
        /// <param name="measure">The measure if found or created; otherwise null.</param>
        /// <returns>True if the measure was found or created; otherwise false.</returns>
        public bool TryFindMeasureByName(String name, out Measure measure)
        {
            // In disconnected mode, auto-create missing measures
            if (this._currentRunMode == RunMode.DisconnectedMode)
                this._measures.Add(new Measure(0, name, null, null));
            measure = this._measures.FirstOrDefault(m => m.Name == name);
            return measure != null;
        }

        /// <summary>
        /// Attempts to find or create a column by name and table name.
        /// In disconnected mode, creates the column if it doesn't exist.
        /// </summary>
        /// <param name="name">The name of the column.</param>
        /// <param name="tableName">The name of the table containing the column.</param>
        /// <param name="column">The column if found or created; otherwise null.</param>
        /// <returns>True if the column was found or created; otherwise false.</returns>
        public bool TryFindColumnByName(String name, String tableName, out Column column)
        {
            // In disconnected mode, auto-create missing columns
            if (this._currentRunMode == RunMode.DisconnectedMode)
                this._columns.Add(new Column(0, name, new Table(tableName)));
            column = this._columns.FirstOrDefault(c => c.Name == name && c.ParentTable.Name == tableName);
            return column != null;
        }

        /// <summary>
        /// Attempts to find a column by its ID.
        /// </summary>
        /// <param name="columnId">The unique ID of the column.</param>
        /// <param name="column">The column if found; otherwise null.</param>
        /// <returns>True if the column was found; otherwise false.</returns>
        public bool TryFindColumnById(ulong columnId, out Column column)
        {
            column = this._columns.FirstOrDefault(c => c.ColumnId == columnId);
            return column != null;
        }

        /// <summary>
        /// Gets all tables in this semantic model.
        /// </summary>
        public HashSet<Table> GetTables() => this._tables;

        /// <summary>
        /// Gets all measures in this semantic model.
        /// </summary>
        public HashSet<Measure> GetMeasures() => this._measures;

        /// <summary>
        /// Gets all columns in this semantic model.
        /// </summary>
        public HashSet<Column> GetColumns() => this._columns;

        /// <summary>
        /// Gets all relationships in this semantic model.
        /// </summary>
        public HashSet<Relationship> GetRelationships() => this._relationships;

        /// <summary>
        /// Determines whether this semantic model is equal to another.
        /// Two semantic models are equal if they have the same connection string.
        /// </summary>
        /// <param name="other">The semantic model to compare with.</param>
        /// <returns>True if the connection strings match; otherwise false.</returns>
        public bool Equals(SemanticModel other)
        {
            return other._connectionString == this._connectionString;   
        }

        /// <summary>
        /// Gets the hash code for this semantic model based on its connection string.
        /// </summary>
        public override int GetHashCode()
        {
            return this._connectionString.GetHashCode();
        }
    }
}
