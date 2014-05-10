//------------------------------------------------------------------------------
// <copyright file="CSSqlStoredProcedure.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;

public partial class StoredProcedures
{
    /// <summary>
    /// A SQLCLR wrapper method RAISERROR().
    /// </summary>
    /// <param name="message">The message raised by RAISERROR()</param>
    /// <param name="severity">The message severity.</param>
    /// <param name="state">The message state.</param>
    [SqlProcedure]
    public static void RaisError(string message, short severity = 0, short state = 1)
    {
        using (var cn = new SqlConnection("context connection=true"))
        {
            // Note you can't use overlaods. You get the following error:
            // More than one method, property or field was found with name 'RaisError' in class 'StoredProcedures' in assembly 'SqlSaturdayClr'. Overloaded methods, properties or fields are not supported.
            RaisErrorInt(cn, message, severity, state);
        }
    }

    /// <summary>
    /// A SQLCLR wrapper method RAISERROR().
    /// </summary>
    /// <param name="cn">The connection to execute the </param>
    /// <param name="message">The message raised by RAISERROR()</param>
    /// <param name="severity">The message severity.</param>
    /// <param name="state">The message state.</param>
    /// <param name="args">These are the <c>printf()</c> style arguments used as substitution parameters for <paramref name="message"/>.</param>
    /// <externalLink>
    /// <linkText>Canonical StackOverflow answer on parameterizing multi-string answers.</linkText>
    /// <linkUri>http://stackoverflow.com/a/337792/95195</linkUri>
    /// </externalLink>
    internal static void RaisErrorInt(SqlConnection cn, string message, short severity, short state, params object[] args)
    {
        using (var cmd = cn.CreateCommand())
        {
            cn.Open();

            cmd.Parameters.AddWithValue("@msg", message);
            cmd.Parameters.AddWithValue("@severity", severity);
            cmd.Parameters.AddWithValue("@state", state);

            if (args == null)
            {
                cmd.CommandText = "RAISERROR (@msg, @severity, @state)";
            }
            else
            {
                string[] argNames = new string[args.Length];
                for (int i = 0; i < args.Length; i++)
                {
                    argNames[i] = string.Format("@arg{0}", i);
                    cmd.Parameters.AddWithValue(argNames[i], args[i]);
                }
                cmd.CommandText = string.Format("RAISERROR (@msg, @severity, @state, {0})", string.Join(", ", argNames));
            }
            SqlContext.Pipe.ExecuteAndSend(cmd);
            cn.Close();
        }
    }
}
