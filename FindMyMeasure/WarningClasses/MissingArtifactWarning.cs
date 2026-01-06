using FindMyMeasure.Interfaces;

namespace FindMyMeasure.WarningClasses
{
    public class MissingArtifactWarning : AnalysisWarning
    {
        private string _artifactName;
        private string _artifactType;
        private string _tableName;
        private IPowerBILeafNode _sender;
        public string ArtifactName => this._artifactName;
        public string ArtifactType=> this._artifactType;
        public string TableName => this._tableName;
        public IPowerBILeafNode Sender => this._sender;

        public MissingArtifactWarning(IPowerBILeafNode sender, string artifactType, string artifactName, string tableName) : base($"The artifact {artifactName}({artifactType}) from table {tableName} used in {sender.Name} doesn't exist in semantic model.")
        {
            this._sender = sender;
            this._artifactName = artifactName;
            this._artifactType= artifactType;
            this._tableName = tableName;
        }
    }
}
