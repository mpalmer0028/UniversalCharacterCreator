using System;
using Mono.Data.Sqlite;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace EnumGeneration
{
    [Serializable]
    public class EnumGenerator : MonoBehaviour
    {
        #region Const variables

        private const string CLASS_NAME = "$CLASS_NAME";
        private const string ENUM_VALUE_TYPE = "$ENUM_VALUE_TYPE";
        private const string ENUM_NAME = "$ENUM_NAME";
        private const string ENUM_VALUE = "$ENUM_VALUE";
        private const string REPEAT_START = "$REPEAT_START";
        private const string REPEAT_END = "$REPEAT_END";

        #endregion

        #region Editor variables

        [Header("File paths")] [Tooltip("When set to true, the paths will be assumed to be relative to the application data path.")] public
            bool RelativeToAssets = true;

        [Tooltip("The location of the database file. Include the name of the database file.")] [SerializeField] private
            string _sQLiteDatabasePath = "/StreamingAssets/database.db";

        public string SQLiteDatabasePath
        {
            get
            {
                if (RelativeToAssets)
                {
                    return Application.dataPath + _sQLiteDatabasePath;
                }
                return _sQLiteDatabasePath;
            }
            set { _sQLiteDatabasePath = value; }
        }

        [Tooltip("The location of the json configuration file for generating enums.")] [SerializeField] private string
            _generateEnumConfigFile = "/EnumGenerator/enumconfig.json";

        public string GenerateEnumConfigFile
        {
            get
            {
                if (RelativeToAssets)
                {
                    return Application.dataPath + _generateEnumConfigFile;
                }
                return _generateEnumConfigFile;
            }
            set { _generateEnumConfigFile = value; }
        }

        [Tooltip("The location of the C# enum template file.")] [SerializeField] private string _enumTemplatePath =
            "/EnumGenerator/Enumeration.cs.template";

        public string EnumTemplateFile
        {
            get
            {
                if (RelativeToAssets)
                {
                    return Application.dataPath + _enumTemplatePath;
                }
                return _enumTemplatePath;
            }
            set { _enumTemplatePath = value; }
        }

        [Header("Generation")] [Tooltip("Set true to generate resource enums when generation is executed.")] public bool
            ResourceEnums = true;

        [Tooltip("Set true to generate SQLite enums when generation is executed.")] public bool SqliteEnums = true;

        #endregion

        #region Private variables

        private string _cachedTemplate;

        #endregion

        #region Generation

        /// <summary>
        /// Generates enum files by reading the config settings and acting on them with the assets and within the provided database file.
        /// </summary>
        public void GenerateEnumFiles()
        {
            IList<DatabaseEnumConfig> databaseEnums;
            IList<ResourceEnumConfig> resourceEnums;

            LoadConfig(out databaseEnums, out resourceEnums);

            if (ResourceEnums)
            {
                GenerateFromResources(resourceEnums);
            }

            if (SqliteEnums)
            {
                GenerateFromDatabase(databaseEnums);
            }
        }

        /// <summary>
        /// Generates enums from a list of DatabaseEnumConfig objects.
        /// </summary>
        /// <param name="database"></param>
        private void GenerateFromDatabase(IList<DatabaseEnumConfig> database)
        {
            // connect to database
            // go through each config
            // create enum from info

            if (!File.Exists(SQLiteDatabasePath))
            {
                throw new FileNotFoundException("The SQLite database file could not be found.\n\n" +
                                                "Path: " + SQLiteDatabasePath + "\n");
            }

            string connectionString = string.Format("URI=file:{0}", SQLiteDatabasePath);

            using (var connection = new SqliteConnection(connectionString))
            {
                try
                {
                    // note, individual settings for each parse so that we can error out individual configs
                    connection.Open();
                    if (connection.State != ConnectionState.Open)
                    {
                        throw new Exception("Was unable to open a connection to the SQLite database file.");
                    }

                    foreach (var config in database)
                    {
                        // Check if the folder where we will put the enum file exists or not
                        if (!Directory.Exists(Application.dataPath + config.DestinationPath))
                        {
                            Debug.LogError(
                                "Attempted to generate database config, but the destination folder path didn't exist.\n\n" +
                                config
                                );
                            continue;
                        }

                        GenerateEnumFromDatabaseConfig(connection, config);
                    }
                }
                catch (Exception e)
                {
                    throw new Exception("There was an error with the SQLite Database.", e);
                }
            }
        }

        /// <summary>
        /// Generates a enum from a database config on the database conenction given.
        /// </summary>
        /// <param name="connection">Connection to the database.</param>
        /// <param name="config">The config decribing what to generate into the enum.</param>
        private void GenerateEnumFromDatabaseConfig(SqliteConnection connection, DatabaseEnumConfig config)
        {
            try
            {
                IList<object> variableNameColumns = GetEnumValueName(connection, config.TableName,
                    config.VariableNameColumn, QueryValueType.STRING);

                QueryValueType variableValueType = QueryValueType.INT;

                IList<object> variableValueColumns = null;
                if (!string.IsNullOrEmpty(config.VariableValueColumn))
                {
                    variableValueType = GetVariableValueType(config.VariableValueType);
                    variableValueColumns = GetEnumValueName(connection, config.TableName, config.VariableValueColumn,
                        variableValueType);
                }

                string variableValueTypeString = variableValueType.ToString().ToLower();

                CreateCSharpFile(config.DestinationPath, config.EnumName, variableValueTypeString, variableNameColumns,
                    variableValueColumns);
            }
            catch (Exception e)
            {
                throw new Exception("Was unable to parse database configuration\n\n" + config.ToString(), e);
            }
        }

        /// <summary>
        /// Queries the database for a list of values.
        /// All the values in the list will be the given queryValueType.
        /// </summary>
        /// <param name="connection">Database connection.</param>
        /// <param name="tableName">Table to query.</param>
        /// <param name="columnName">Column to query.</param>
        /// <param name="queryValueType">The type of value that is being read.</param>
        /// <returns></returns>
        private IList<object> GetEnumValueName(SqliteConnection connection, string tableName, string columnName,
            QueryValueType queryValueType)
        {
            var result = new List<object>();

            string query = string.Format("Select {0} From {1}", columnName, tableName);
            using (IDbCommand dbcmd = connection.CreateCommand())
            {
                dbcmd.CommandText = query;
                using (IDataReader reader = dbcmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(GetValueFromSqliteReader(reader, queryValueType));
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Generates enums from a list of ResourceEnumConfig objects.
        /// </summary>
        /// <param name="resources"></param>
        private void GenerateFromResources(IList<ResourceEnumConfig> resources)
        {
            foreach (var config in resources)
            {
                string folderPath = Application.dataPath + config.FolderPath;

                // Check if the folder for the resources actually exists or not
                if (!Directory.Exists(folderPath))
                {
                    Debug.LogError("Attempted to generate resource config, but the folder path didn't exist.\n" +
                                   folderPath + "\n\n" +
                                   config
                        );
                    continue;
                }

                // Check if the folder where we will put the enum file exists or not
                if (!Directory.Exists(Application.dataPath + config.DestinationPath))
                {
                    Debug.LogError(
                        "Attempted to generate resource config, but the destination folder path didn't exist.\n\n" +
                        config
                        );
                    continue;
                }

                GenerateEnumForResourceFolder(config, folderPath);
            }
        }

        /// <summary>
        /// Generates the enums for a specific resource folder.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="folderPath"></param>
        private void GenerateEnumForResourceFolder(ResourceEnumConfig config, string folderPath)
        {
            var filenames = GetValidFilenamesInAllDirectory(folderPath, config);

            // Trim the values down so that they are only equal to what the resource is called.
            for (int i = 0; i < filenames.Count; i++)
            {
                filenames[i] = Path.GetFileNameWithoutExtension(filenames[i]);
            }

            CreateCSharpFile(config.DestinationPath, config.EnumName, "int", ConvertStringListToObjectList(filenames));
        }

        #endregion

        #region SQLite

        /// <summary>
        /// Represents the value syou can read from the database.
        /// </summary>
        private enum QueryValueType
        {
            BYTE,
            SHORT,
            INT,
            LONG,
            STRING
        }

        /// <summary>
        /// Reads the Sqlite table based off a given value type for the value you are reading.
        /// </summary>
        /// <param name="reader">reader that has processed the query.</param>
        /// <param name="queryValueType">The type of value to read.</param>
        /// <returns></returns>
        private object GetValueFromSqliteReader(IDataReader reader, QueryValueType queryValueType)
        {
            switch (queryValueType)
            {
                case QueryValueType.STRING:
                    return reader.GetString(0);
                case QueryValueType.BYTE:
                    return reader.GetByte(0);
                case QueryValueType.SHORT:
                    return reader.GetInt16(0);
                case QueryValueType.INT:
                    return reader.GetInt32(0);
                case QueryValueType.LONG:
                    return reader.GetInt64(0);
            }

            return reader.GetString(0);
        }

        #endregion

        #region C# File Construction

        /// <summary>
        /// Constructs a C# enum file with the given information.
        /// </summary>
        /// <param name="destinationPath">Where the enum should be placed.</param>
        /// <param name="enumName">The name of the enum class itself</param>
        /// <param name="enumValueType">The type of value the enum is represented by, byte, short, int, long</param>
        /// <param name="enumValueName">A list of individual enum values</param>
        /// <param name="enumValueInt">A list of individual enum int values. 
        /// This list should be null or equal to the number of enumvaluenames and each index in each list corresponds to eachother.</param>
        public void CreateCSharpFile(string destinationPath, string enumName, string enumValueType,
            IList<object> enumValueName, IList<object> enumValueInt = null)
        {
            // If we don't get supplied int values, we default them to 1...enumValueName.Count
            if (enumValueInt == null)
            {
                enumValueInt = new List<object>();
                for (int i = 0; i < enumValueName.Count; i++)
                {
                    enumValueInt.Add(i + 1);
                }
            }

            // Save the cache for later usage
            //if (string.IsNullOrEmpty(_cachedTemplate))
            //{
            // Doesn't really save much to cache it. If you do, you need to restart Unity to see changes take effect.
            _cachedTemplate = File.ReadAllText(EnumTemplateFile);
            //}

            string template = _cachedTemplate;
            template = template.Replace(CLASS_NAME, enumName);
            template = template.Replace(ENUM_VALUE_TYPE, enumValueType);

            // find indexes and get rid of the junk
            int repeatStart = template.IndexOf(REPEAT_START, StringComparison.InvariantCulture);
            template = template.Replace(REPEAT_START, string.Empty);
            int repeatEnd = template.IndexOf(REPEAT_END, StringComparison.InvariantCulture);
            template = template.Replace(REPEAT_END, string.Empty);

            // Info we need to build the enum
            string repeat = template.Substring(repeatStart, repeatEnd - repeatStart);
            template = template.Replace(repeat, string.Empty);
            string firstHalf = template.Substring(0, repeatStart);
            string secondHalf = template.Substring(repeatStart);

            // String builder for first half + enums + second half
            var sb = new StringBuilder();
            sb.Append(firstHalf);

            // Build each line that is valid into the 
            for (int index = 0; index < enumValueName.Count; index++)
            {
                string enumLine = repeat.Replace(ENUM_NAME, enumValueName[index].ToString());
                enumLine = enumLine.Replace(ENUM_VALUE, enumValueInt[index].ToString());
                sb.Append(enumLine);

                // Newline for each line except the last. Purely stylistic since whitespace doesn't matter in this case
                if (index != enumValueName.Count - 1)
                {
                    sb.Append('\n');
                }
            }

            // finish it off with the second half
            sb.Append(secondHalf);

            // Then write the file down
	        File.WriteAllText(Application.dataPath + destinationPath + "/" + enumName + ".cs", sb.ToString());

	        Debug.Log("Finished generating " + Application.dataPath + destinationPath + "/" + enumName + ".cs");
	        Debug.Log(sb.ToString());
	        //Debug.Log("Finished generating " + enumName + ".cs");
        }

        #endregion

        #region Loading config file

        /// <summary>
        /// Parses the config file for the enum generator and creates two lists which describe what enums to generate.
        /// </summary>
        /// <param name="databaseConfigs"></param>
        /// <param name="resourcesConfigs"></param>
        private void LoadConfig(out IList<DatabaseEnumConfig> databaseConfigs,
            out IList<ResourceEnumConfig> resourcesConfigs)
        {
            databaseConfigs = null; // new List<DatabaseEnumConfig>();
            resourcesConfigs = null; // new List<ResourceEnumConfig>();

            if (!File.Exists(GenerateEnumConfigFile))
            {
                throw new FileNotFoundException("Could not find " + GenerateEnumConfigFile);
            }

            using (StreamReader file = File.OpenText(GenerateEnumConfigFile))
            {
                using (JsonTextReader reader = new JsonTextReader(file))
                {
                    JObject json = (JObject) JToken.ReadFrom(reader);

                    databaseConfigs = ParseTokenForDatabaseConfigs(json);
                    resourcesConfigs = ParseTokenForResourceConfigs(json);
                }
            }
        }

        /// <summary>
        /// Parses the config files database settings and returns a list of DatabaseEnumConfig objects.
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        private List<DatabaseEnumConfig> ParseTokenForDatabaseConfigs(JObject json)
        {
            var database = json.GetValue("database");
            var databaseConfigs = new List<DatabaseEnumConfig>();

            foreach (var config in database.Children())
            {
                var databaseConfig = new DatabaseEnumConfig()
                {
                    DestinationPath = FixDirectoryPathString(GetValueOrNull(config, "DestinationPath")),
                    EnumName = GetValueOrNull(config, "EnumName"),
                    TableName = GetValueOrNull(config, "TableName"),
                    VariableValueColumn = GetValueOrNull(config, "VariableValueColumn"),
                    VariableNameColumn = GetValueOrNull(config, "VariableNameColumn"),
                    VariableValueType = GetValueOrNull(config, "VariableValueType"),
                };

                if (databaseConfig.IsValid())
                {
                    databaseConfigs.Add(databaseConfig);
                }
                else
                {
                    Debug.LogError("Encountered an invalid database configuration when attempting to load json.\n\n" +
                                   config.ToString());
                }
            }

            return databaseConfigs;
        }

        /// <summary>
        /// Parses the config files resource settings and returns a list of ResourceEnumConfig objects.
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        private List<ResourceEnumConfig> ParseTokenForResourceConfigs(JObject json)
        {
            var resources = json.GetValue("resources");
            var resourceConfigs = new List<ResourceEnumConfig>();

            foreach (var config in resources.Children())
            {
                var resourceConfig = new ResourceEnumConfig()
                {
                    EnumName = GetValueOrNull(config, "EnumName"),
                    DestinationPath = FixDirectoryPathString(GetValueOrNull(config, "DestinationPath")),
                    FolderPath = FixDirectoryPathString(GetValueOrNull(config, "FolderPath")),
                    ValidExtensions = FixExtensionList(GetValuesAsStrings(config["ValidExtensions"])),
                    InvalidExtensions = FixExtensionList(GetValuesAsStrings(config["InvalidExtensions"]))
                };

                if (resourceConfig.IsValid())
                {
                    resourceConfigs.Add(resourceConfig);
                }
                else
                {
                    Debug.LogError("Encountered an invalid resource configuration when attempting to load json.\n\n" +
                                   config.ToString());
                }
            }

            return resourceConfigs;
        }

        #endregion

        #region Misc

        /// <summary>
        /// Attempts to get the value off of a JToken as a string, and if it can't be found, it returns null.
        /// </summary>
        /// <param name="token"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        private string GetValueOrNull(JToken token, string key)
        {
            object value = token[key];
            if (value != null)
            {
                return value.ToString();
            }
            return null;
        }

        /// <summary>
        /// Returns a list of all valid filenames in the directory and its sub directories.
        /// </summary>
        /// <param name="currentDirectoryPath"></param>
        /// <param name="config">Resource enum config settings.</param>
        private IList<string> GetValidFilenamesInAllDirectory(string currentDirectoryPath, ResourceEnumConfig config)
        {
            var result = new List<string>();
            string[] filenamesFullPath = Directory.GetFiles(currentDirectoryPath, "*.*", SearchOption.AllDirectories);

            int i;

            for (i = 0; i < filenamesFullPath.Length; i++)
            {
                // We don't want meta info
                if (filenamesFullPath[i].EndsWith(".meta"))
                {
                    continue;
                }

                // If we have a list of valid extensions, then we only want filenames that match them.
                if (config.ValidExtensions.Count > 0 &&
                    !config.ValidExtensions.Contains(Path.GetExtension(filenamesFullPath[i])))
                {
                    continue;
                }

                // If we have a list of invalid extensions, then we don't want filenames that match them.
                if (config.InvalidExtensions.Contains(Path.GetExtension(filenamesFullPath[i])))
                {
                    continue;
                }

                result.Add(filenamesFullPath[i]);
            }

            return result;
        }

        /// <summary>
        /// Automatically adds a beginning directory path seperator the the given string if it doesn't exist.
        /// </summary>
        /// <param name="directoryPath"></param>
        /// <returns></returns>
        private string FixDirectoryPathString(string directoryPath)
        {
            if (string.IsNullOrEmpty(directoryPath))
            {
                return null;
            }
	        if (directoryPath[0] != '/')
            {
                return "/" + directoryPath;
            }
            return directoryPath;
        }

        /// <summary>
        /// Returns all the values of the provided token as a list of strings.
        /// </summary>
        /// <param name="token">The token representing an array of strings.</param>
        /// <returns></returns>
        private IList<string> GetValuesAsStrings(JToken token)
        {
            if (token == null)
            {
                return new List<string>();
            }
            return token.Values<string>().ToList();
        }

        /// <summary>
        /// Fixes the list of extensions by ensuring that they begin with a '.'.
        /// If an extension already has '.' in it, then it will be trimmed.
        /// </summary>
        /// <param name="extensions"></param>
        /// <returns></returns>
        private IList<string> FixExtensionList(IList<string> extensions)
        {
            for (int i = 0; i < extensions.Count; i++)
            {
                if (!string.IsNullOrEmpty(extensions[i]))
                {
                    int periodIndex = extensions[i].IndexOf('.');

                    if (periodIndex > 0)
                    {
                        extensions[i] = extensions[i].Substring(periodIndex);
                        // extension would just be '.' which defaults to ''
                        if (extensions[i].Length == 1)
                        {
                            extensions[i] = string.Empty;
                        }
                    }

                    else if (periodIndex < 0)
                    {
                        extensions[i] = '.' + extensions[i];
                    }
                }
            }
            return extensions;
        }

        /// <summary>
        /// Takes a string and returns an enum based off what the string represents.
        /// Defaults to int.
        /// </summary>
        /// <param name="variableValueType"></param>
        /// <returns></returns>
        private QueryValueType GetVariableValueType(string variableValueType)
        {
            if (variableValueType == null)
            {
                return QueryValueType.INT;
            }

            switch (variableValueType.ToUpper())
            {
                case "BYTE":
                    return QueryValueType.BYTE;
                case "SHORT":
                    return QueryValueType.SHORT;
                case "INT":
                    return QueryValueType.INT;
                case "LONG":
                    return QueryValueType.LONG;
            }

            return QueryValueType.INT;
        }

        /// <summary>
        /// Converts the list of strings to a list of objects.
        /// Casting normally a IList(string) to a IList(object) results in null.
        /// </summary>
        /// <param name="list">The list to cast</param>
        /// <returns></returns>
        private IList<object> ConvertStringListToObjectList(IList<string> list)
        {
            if (list == null)
            {
                return null;
            }

            return list.Cast<object>().ToList();
        }

        #endregion
    }
}