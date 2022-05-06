
namespace EnumGeneration
{
    internal class DatabaseEnumConfig
    {
        public string TableName;
        public string EnumName;
        public string VariableNameColumn;
        public string VariableValueColumn;
        public string VariableValueType;
        public string DestinationPath;

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(TableName) &&
                   !string.IsNullOrEmpty(EnumName) &&
                   !string.IsNullOrEmpty(VariableNameColumn) &&
                   !string.IsNullOrEmpty(DestinationPath);
        }

        public override string ToString()
        {
            return "TableName: " + TableName + ",\n" +
                   "EnumName: " + EnumName + ",\n" +
                   "VariableNameColumn: " + VariableNameColumn + ",\n" +
                   "VariableValueColumn: " + VariableValueColumn + ",\n" +
                   "VariableValueType: " + VariableValueType + ",\n" +
                   "DestinationPath: " + DestinationPath + "\n";
        }
    }
}
