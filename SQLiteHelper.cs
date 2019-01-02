using System;
using System.Data;
using System.Data.SQLite;
using System.Text.RegularExpressions;

public class SQLiteHelper
{
    /// <summary>
    /// Shortcut method to execute dataset from SQL Statement and object[] arrray of parameter values
    /// </summary>
    /// <param name="connectionString">SQLite Connection string</param>
    /// <param name="commandText">SQL Statement with embedded "@param" style parameter names</param>
    /// <param name="paramList">object[] array of parameter values</param>
    /// <returns></returns>
    public static DataSet ExecuteDataSet(string connectionString, string commandText, object[] paramList)
    {
        SQLiteConnection cn = new SQLiteConnection(connectionString);

        return ExecuteDataSet(cn, commandText, paramList);
    }

    /// <summary>
    /// Shortcut method to execute dataset from SQL Statement and object[] arrray of  parameter values
    /// </summary>
    /// <param name="cn">Connection.</param>
    /// <param name="commandText">Command text.</param>
    /// <param name="paramList">Param list.</param>
    /// <returns></returns>
    public static DataSet ExecuteDataSet(SQLiteConnection cn, string commandText, object[] paramList)
    {
        SQLiteCommand cmd = cn.CreateCommand();

        cmd.CommandText = commandText;

        if (paramList != null)
        {
            AttachParameters(cmd, commandText, paramList);
        }

        if (cn.State == ConnectionState.Closed)
        {
            cn.Open();
        }

        DataSet ds = new DataSet();

        SQLiteDataAdapter da = new SQLiteDataAdapter(cmd);

        da.Fill(ds);

        da.Dispose();

        cmd.Dispose();

        cn.Close();

        return ds;
    }

    /// <summary>
    /// NOTE: You should explicitly close the Command.connection you passed in as
    /// well as call Dispose on the Command  after reader is closed.
    /// We do this because IDataReader has no underlying Connection Property.
    /// </summary>
    /// <param name="cmd">SQLiteCommand Object</param>
    /// <param name="commandText">SQL Statement with optional embedded "@param" style parameters</param>
    /// <param name="paramList">object[] array of parameter values</param>
    /// <returns>SQLiteDataReader</returns>
    public static SQLiteDataReader ExecuteReader(string connectionString, string commandText, object[] paramList)
    {
        SQLiteConnection cn = new SQLiteConnection(connectionString);

        SQLiteCommand cmd = cn.CreateCommand();

        cmd.CommandText = commandText;

        if (paramList != null)
        {
            AttachParameters(cmd, commandText, paramList);
        }

        if (cn.State == ConnectionState.Closed)
        {
            cn.Open();
        }

        SQLiteDataReader rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection);

        return rdr;
    }

    /// <summary>
    /// Shortcut to ExecuteNonQuery with SqlStatement and object[] param values
    /// </summary>
    /// <param name="connectionString">SQLite Connection String</param>
    /// <param name="commandText">Sql Statement with embedded "@param" style parameters</param>
    /// <param name="paramList">object[] array of parameter values</param>
    /// <returns></returns>
    public static int ExecuteNonQuery(string connectionString, string commandText, params object[] paramList)
    {
        SQLiteConnection cn = new SQLiteConnection(connectionString);

        return ExecuteNonQuery(cn, commandText, paramList);
    }

    public static int ExecuteNonQuery(SQLiteConnection cn, string commandText, params object[] paramList)
    {
        SQLiteCommand cmd = cn.CreateCommand();

        cmd.CommandText = commandText;

        AttachParameters(cmd, commandText, paramList);

        if (cn.State == ConnectionState.Closed)
        {
            cn.Open();
        }

        int result = cmd.ExecuteNonQuery();

        cmd.Dispose();

        cn.Close();

        return result;
    }

    /// <summary>
    /// Shortcut to ExecuteScalar with Sql Statement embedded params and object[] param values
    /// </summary>
    /// <param name="connectionString">SQLite Connection String</param>
    /// <param name="commandText">SQL statment with embedded "@param" style parameters</param>
    /// <param name="paramList">object[] array of param values</param>
    /// <returns></returns>
    public static object ExecuteScalar(string connectionString, string commandText, params object[] paramList)
    {
        SQLiteConnection cn = new SQLiteConnection(connectionString);

        SQLiteCommand cmd = cn.CreateCommand();

        cmd.CommandText = commandText;

        AttachParameters(cmd, commandText, paramList);

        if (cn.State == ConnectionState.Closed)
        {
            cn.Open();
        }

        object result = cmd.ExecuteScalar();

        cmd.Dispose();

        cn.Close();

        return result;
    }

    /// <summary>
    /// Parses parameter names from SQL Statement, assigns values from object array ,   /// and returns fully populated ParameterCollection.
    /// </summary>
    /// <param name="commandText">Sql Statement with "@param" style embedded parameters</param>
    /// <param name="paramList">object[] array of parameter values</param>
    /// <returns>SQLiteParameterCollection</returns>
    /// <remarks>Status experimental. Regex appears to be handling most issues. Note that parameter object array must be in same ///order as parameter names appear in SQL statement.</remarks>
    private static SQLiteParameterCollection AttachParameters(SQLiteCommand cmd, string commandText, params object[] paramList)
    {
        if (paramList == null || paramList.Length == 0)
        {
            return null;
        }

        SQLiteParameterCollection coll = cmd.Parameters;

        string parmString = commandText.Substring(commandText.IndexOf("@"));

        // pre-process the string so always at least 1 space after a comma.
        parmString = parmString.Replace(",", " ,");

        // get the named parameters into a match collection
        string pattern = @"(@)\S*(.*?)\b";

        Regex ex = new Regex(pattern, RegexOptions.IgnoreCase);

        MatchCollection mc = ex.Matches(parmString);

        string[] paramNames = new string[mc.Count];

        int i = 0;

        foreach (Match m in mc)
        {
            paramNames[i] = m.Value;

            i++;
        }

        // now let's type the parameters
        int j = 0;

        Type t = null;

        foreach (object o in paramList)
        {
            t = o.GetType();

            SQLiteParameter parm = new SQLiteParameter();

            switch (t.ToString())
            {
                case ("DBNull"):
                case ("Char"):
                case ("SByte"):
                case ("UInt16"):
                case ("UInt32"):
                case ("UInt64"):
                    throw new SystemException("Invalid data type");

                case ("System.String"):
                    parm.DbType = DbType.String;
                    parm.ParameterName = paramNames[j];
                    parm.Value = (string)paramList[j];
                    coll.Add(parm);
                    break;

                case ("System.Byte[]"):
                    parm.DbType = DbType.Binary;
                    parm.ParameterName = paramNames[j];
                    parm.Value = (byte[])paramList[j];
                    coll.Add(parm);
                    break;

                case ("System.Int32"):
                    parm.DbType = DbType.Int32;
                    parm.ParameterName = paramNames[j];
                    parm.Value = (int)paramList[j];
                    coll.Add(parm);
                    break;

                case ("System.Boolean"):
                    parm.DbType = DbType.Boolean;
                    parm.ParameterName = paramNames[j];
                    parm.Value = (bool)paramList[j];
                    coll.Add(parm);
                    break;

                case ("System.DateTime"):
                    parm.DbType = DbType.DateTime;
                    parm.ParameterName = paramNames[j];
                    parm.Value = Convert.ToDateTime(paramList[j]);
                    coll.Add(parm);
                    break;

                case ("System.Double"):
                    parm.DbType = DbType.Double;
                    parm.ParameterName = paramNames[j];
                    parm.Value = Convert.ToDouble(paramList[j]);
                    coll.Add(parm);
                    break;

                case ("System.Decimal"):
                    parm.DbType = DbType.Decimal;
                    parm.ParameterName = paramNames[j];
                    parm.Value = Convert.ToDecimal(paramList[j]);
                    break;

                case ("System.Guid"):
                    parm.DbType = DbType.Guid;
                    parm.ParameterName = paramNames[j];
                    parm.Value = (System.Guid)(paramList[j]);
                    break;

                case ("System.Object"):

                    parm.DbType = DbType.Object;
                    parm.ParameterName = paramNames[j];
                    parm.Value = paramList[j];
                    coll.Add(parm);
                    break;

                default:
                    throw new SystemException("Value is of unknown data type");
            }

            j++;
        }

        return coll;
    }
}
