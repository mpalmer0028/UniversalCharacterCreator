using System.Collections.Generic;

namespace EnumGeneration
{
    internal class ResourceEnumConfig
    {
        public string FolderPath;
        public string EnumName;
        public string DestinationPath;
        public IList<string> ValidExtensions;
        public IList<string> InvalidExtensions;

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(FolderPath) &&
                   !string.IsNullOrEmpty(EnumName) &&
                   !string.IsNullOrEmpty(DestinationPath);
        }

        public override string ToString()
        {
            return "FolderPath: " + FolderPath + ",\n" +
                   "EnumName: " + EnumName + ",\n" +
                   "DestinationPath: " + DestinationPath + "\n" +
                   "ValidExtensions: " + ValidExtensions + "\n" +
                   "InvalidExtensions: " + InvalidExtensions + "\n";
        }
    }
}
