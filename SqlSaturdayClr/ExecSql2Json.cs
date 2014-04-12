//------------------------------------------------------------------------------
// <copyright file="CSSqlFunction.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using fastJSON;
using Microsoft.SqlServer.Server;

public partial class UserDefinedFunctions
{
    private static readonly JSONParameters jsonParams = new JSONParameters
    {
        //UseOptimizedDatasetSchema = true,
        UseUTCDateTime = true,
        EnableAnonymousTypes = true,
    };

    [SqlFunction(DataAccess = DataAccessKind.Read, SystemDataAccess = SystemDataAccessKind.Read)]
    [return: SqlFacet(IsFixedLength = false, IsNullable = true, MaxSize = -1)]
    public static SqlString ExecSql2Json([SqlFacet(IsFixedLength = false, IsNullable = true, MaxSize = -1)] SqlString sql)
    {
        using (var cn = new SqlConnection("context connection=true"))
        using (var cmd = cn.CreateCommand())
        using (var da = new SqlDataAdapter(cmd))
        using (var ds = new DataSet("ExecSql2Json"))
        {
            cmd.CommandText = sql.Value;
            cn.Open();
            //try
            //{
                da.Fill(ds);
            //}
            //catch (SqlException ex)
            //{
                //return new SqlChars(ex.ToString());
            //}
            return new SqlString(JSON.ToNiceJSON(ds, jsonParams));
        }
    }
}
