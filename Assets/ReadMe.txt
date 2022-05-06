ADDITIONAL INFO:

When regenerating an enum, the old enum file will be written over. Once you get everything working in the example project, try generating the existing 
config file and watch it show what enums are created. Try deleting the enum files and generate them again to see it in action.

For additional help, email me at UnityAssetHelp@savemeoniichan.com

REQUIREMENTS:

	The EnumGenerator class requires both Sqlite3 dlls and Json.net dlls. I have included the needed DLLs in the Plugins folder.
	The EnumGenerator also requires Project Settings -> Player -> Other Settings -> Api Compatability Level -> .NET 2.0 (not .NET 2.0 Subset).

Default structure of the config file

{
	database:[
		{}
		],
	resources:[
		{}
		]
}


PARAMETERS:

Database configuration parameters

	TableName: The name of the table that will be read from.
	EnumName: The name you want for the C# enum and file.
	VariableNameColumn: The name of the column you want to represent each enum value.
	VariableValueColumn: The column you want to populate the value of each enum variable.
		Note: This parameter is not required. If you leave this out, it will default to an
			integer value type and auto assign the enums with values 1...n based on the number
			of variable names are found. Without this parameter being set, VariableValueType does
			nothing.
	VariableValueType: The type of value backing the enum.
		This generator supports "byte", "short", "int", and "long".
		I did not add in unsigned values, but if it becomes a wanted feature then I will add it.
		This parameter does nothing if you don't have VariableValueColumn set.
		Note: This parameter is not required. It defaults to int if left out.
	DestinationPath: The location you want the generated enum file to be placed.
	
Resource folder parameters

	FolderPath: The path to the folder that you want to generate into an enum.
		The enum will be generated using the name of each file for its variable names.
		This was done that way to make it easier to call "Resources.LoadAsset()" using the enum.ToString()
		The value of each enum will be automatically set in the order they are recieved.
	EnumName: The name you want for the C# enum and file.
	DestinationPath: The location you want the generated enum file to be placed.
	ValidExtensions: If this parameter is set, then only these extensions will be looked at when generating the enum.
		Note: This parameter is not required. All extensions EXCEPT ".meta" will be looked at if left out.
	InvalidExtensions:If this parameter is set, then none of these extensions will be locked at when generating the enum.
		Any extensions that are in both Valid and Invalid will be ignored because they are invalid.
		NOTE: This parameter is not required. ".meta" will be ignored regardless of whether it is set or not.

		
EXAMPLE: 

Example config file: Note the different usages of parameters and what happens when each are generated.

{
	database:[
		{
			TableName:"BigIntTable",
			EnumName:"ValueId",
			VariableNameColumn:"some_value",
			VariableValueColumn:"bigint_id",
			VariableValueType:"long",
			DestinationPath:"/GeneratedEnums",
		},
		{
			TableName:"ByteTable",
			EnumName:"AnswerId",
			VariableNameColumn:"am_i_right",
			VariableValueColumn:"value",
			VariableValueType:"byte",
			DestinationPath:"/GeneratedEnums",
		},
		{
			TableName:"IntTable",
			EnumName:"IntId",
			VariableNameColumn:"name",
			VariableValueType:"long",
			DestinationPath:"/GeneratedEnums/MoreEnums",
		},
		{
			TableName:"IntTable",
			EnumName:"SecondIntId",
			VariableNameColumn:"name",
			VariableValueColumn:"second_int_column",
			DestinationPath:"/GeneratedEnums/MoreEnums",
		},
		{
			TableName:"LotsOfValues",
			EnumName:"LotsOfValues",
			VariableNameColumn:"name",
			VariableValueColumn:"id",
			DestinationPath:"/GeneratedEnums",
		}
	],
	resources:[
		{
			FolderPath:"/FolderOfImages",
			EnumName:"ImageId",
			DestinationPath:"/GeneratedEnums/ImageEnums",
			ValidExtensions:[".png", ".jpg"],
			InvalidExtensions:[".mp3", ".jpg"]
		},
		{
			FolderPath:"/FolderOfImages",
			EnumName:"ImageId_2",
			DestinationPath:"/GeneratedEnums/ImageEnums",
			InvalidExtensions:[".mp3", ".jpg"]
		},
		{
			FolderPath:"/FolderOfImages",
			EnumName:"ImageId_3",
			DestinationPath:"/GeneratedEnums/ImageEnums"
		},
	]
}