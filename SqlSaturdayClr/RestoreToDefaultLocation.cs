using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using Microsoft.SqlServer.Server;
using Microsoft.Win32;

public partial class StoredProcedures
{
    private class BackupInfo
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public bool Log { get; set; } // Probably needs to become an enum for filestream
    }

    /// <summary>
    /// Restores a database moving the files to the correct locations
    /// </summary>
    /// <param name="dbName">The name of the database</param>
    /// <param name="path">The path of the backup file</param>
    /// <param name="progressAsRowSet">Set to true to return the restore and upgrade messages in tabular format.</param>
    /// <remarks></remarks>
    [SqlProcedure]
    public static void RestoreToDefaultLocation(SqlString dbName, SqlString path, SqlBoolean progressAsRowSet)
    {
        using (var cn = new SqlConnection("context connection=true"))
        using (var cmd = cn.CreateCommand())
        {
            cn.Open();
            cmd.CommandText = "SELECT COALESCE(SERVERPROPERTY ('InstanceName'), 'MSSQLSERVER')";
            var instanceName = (string) cmd.ExecuteScalar();
            SqlContext.Pipe.Send(string.Format("InstanceName: {0}", instanceName));
            var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default);
            SqlContext.Pipe.Send("HKLM");
            var instanceKeyName =
                (string )hklm.OpenSubKey("SOFTWARE\\Microsoft\\Microsoft SQL Server\\Instance Names\\SQL").GetValue(instanceName);
            SqlContext.Pipe.Send(string.Format("InstanceKey: {0}", instanceKeyName));
            var instanceKey =
                hklm.OpenSubKey("SOFTWARE\\Microsoft\\Microsoft SQL Server\\" + instanceKeyName);
            var mssqlServer = instanceKey.OpenSubKey("MSSQLServer");
            var backupFolder = (string) mssqlServer.GetValue("BackupDirectory");
            SqlContext.Pipe.Send(string.Format("BackupFolder: {0}", backupFolder));
            var sqlDataRoot = (string) instanceKey.OpenSubKey("Setup").GetValue("SQLDataRoot"); // TODO: I might actually want SQLPath
            SqlContext.Pipe.Send(string.Format("SQL Data Root: {0}", sqlDataRoot));
            var defaultData = (string) mssqlServer.GetValue("DefaultData", Path.Combine(sqlDataRoot, "Data"));
            SqlContext.Pipe.Send(string.Format("Default Data: {0}", defaultData));
            var defaultLog = (string)mssqlServer.GetValue("DefaultLog", Path.Combine(sqlDataRoot, "Data"));
            SqlContext.Pipe.Send(string.Format("Default Log: {0}", defaultLog));

            if (!Path.IsPathRooted(path.Value))
            {
                path = Path.Combine(backupFolder, path.Value);
            }
            if (!File.Exists(path.Value)) throw new FileNotFoundException("Backup file does not exist", path.Value);
            
            cmd.CommandText = string.Format("RESTORE FILELISTONLY FROM DISK='{0}'", path.Value);
            var dataFiles = new List<BackupInfo>();
            using (var rdr = cmd.ExecuteReader())
            while (rdr.Read())
            {
                dataFiles.Add(new BackupInfo
                {
                    Name = (string) rdr["LogicalName"],
                    Path = Path.GetFileName((string) rdr["PhysicalName"]),
                    Log = (string) rdr["Type"] == "L"
                });
            }

            cmd.CommandText = string.Format("RESTORE DATABASE {0} FROM DISK='{1}' WITH STATS=10, ", dbName.Value, path.Value);
            var withMoves = (from file in dataFiles
                select
                    string.Format("MOVE '{0}' TO '{1}'", file.Name,
                        Path.Combine(file.Log ? defaultLog : defaultData, file.Path))).ToArray();
            cmd.CommandText = string.Concat(cmd.CommandText, string.Join(", ", withMoves));

            if (progressAsRowSet.IsTrue)
            {
                SqlInfoMessageEventHandler infoMessageHandler = (sender, args) =>
                {
                    SqlContext.Pipe.SendResultsStart(GetMessageRecord());
                    foreach (var msg in args.Message.Split('\n'))
                    {
                        SqlContext.Pipe.SendResultsRow(GetMessageRecord(msg, args.Source));
                    }
                    SqlContext.Pipe.SendResultsEnd();
                };

                cn.InfoMessage += infoMessageHandler;
                cmd.ExecuteNonQuery();
                cn.InfoMessage -= infoMessageHandler;
            }
            else
            {
                SqlContext.Pipe.ExecuteAndSend(cmd);
            }
            cn.Close();
        }
    }

    private static SqlDataRecord GetMessageRecord()
    {
        return new SqlDataRecord
            (new SqlMetaData("Message", SqlDbType.NVarChar, -1),
                new SqlMetaData("Source", SqlDbType.NVarChar, -1),
                new SqlMetaData("Timestamp", SqlDbType.DateTime));
    }

    private static SqlDataRecord GetMessageRecord(string message, string source)
    {
        var record = new SqlDataRecord
                    (new SqlMetaData("Message", SqlDbType.NVarChar, -1),
                        new SqlMetaData("Source", SqlDbType.NVarChar, -1),
                        new SqlMetaData("Timestamp", SqlDbType.DateTime));
        record.SetString(0, message);
        record.SetString(1, source);
        record.SetDateTime(2, DateTime.Now);
        return record;
    }
}
