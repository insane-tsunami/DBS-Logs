using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

public sealed class LogListItem
{
    public long DetailId { get; set; }
    public long LogId { get; set; }
    public int LogType { get; set; }
    public string ExternalCode { get; set; }
    public string Description { get; set; }
    public int LogStatus { get; set; }
    public DateTime LogDateTime { get; set; }
    public long CreatedBy { get; set; }
    public string UserName { get; set; }
}

public sealed class LogPage
{
    public LogPage()
    {
        Items = new List<LogListItem>();
    }

    public IList<LogListItem> Items { get; private set; }
    public bool HasNextPage { get; set; }
    public long? NextCursor { get; set; }
}

public sealed class FilterOption
{
    public long Value { get; set; }
    public string Text { get; set; }
}

public sealed class LogDetailItem
{
    public long DetailId { get; set; }
    public long LogId { get; set; }
    public int LogType { get; set; }
    public string ExternalCode { get; set; }
    public string Description { get; set; }
    public string DetailData { get; set; }
    public int LogStatus { get; set; }
    public bool IsSystem { get; set; }
    public DateTime LogDateTime { get; set; }
    public long CreatedBy { get; set; }
    public string UserName { get; set; }
}

public static class LogRepository
{
    private const int PageSize = 100;

    private static SqlConnection OpenConnection()
    {
        ConnectionStringSettings settings = ConfigurationManager.ConnectionStrings["Credibox"];
        if (settings == null || String.IsNullOrWhiteSpace(settings.ConnectionString))
        {
            throw new ConfigurationErrorsException("A connection string 'Credibox' não está configurada.");
        }

        SqlConnection connection = new SqlConnection(settings.ConnectionString);
        connection.Open();
        return connection;
    }

    public static IList<FilterOption> GetLogTypes()
    {
        const string sql = @"
SELECT DISTINCT CAST(L.LOGTYPE AS BIGINT) AS Value,
       CONVERT(NVARCHAR(50), L.LOGTYPE) AS Text
FROM dbo.OSUSR_U6P_LOG L WITH (NOLOCK)
WHERE L.LOGTYPE IS NOT NULL
ORDER BY Text;";

        List<FilterOption> result = new List<FilterOption>();
        using (SqlConnection connection = OpenConnection())
        using (SqlCommand command = new SqlCommand(sql, connection))
        {
            command.CommandTimeout = 30;
            using (SqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    result.Add(new FilterOption
                    {
                        Value = Convert.ToInt64(reader["Value"]),
                        Text = Convert.ToString(reader["Text"])
                    });
                }
            }
        }

        return result;
    }

    public static IList<FilterOption> GetUsers()
    {
        const string sql = @"
SELECT CAST(U.ID AS BIGINT) AS Value,
       COALESCE(NULLIF(U.NAME, ''), NULLIF(U.USERNAME, ''), CONVERT(NVARCHAR(50), U.ID)) AS Text
FROM DBSPlatform.dbo.ossys_User U WITH (NOLOCK)
ORDER BY Text;";

        List<FilterOption> result = new List<FilterOption>();
        using (SqlConnection connection = OpenConnection())
        using (SqlCommand command = new SqlCommand(sql, connection))
        {
            command.CommandTimeout = 30;
            using (SqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    result.Add(new FilterOption
                    {
                        Value = Convert.ToInt64(reader["Value"]),
                        Text = Convert.ToString(reader["Text"])
                    });
                }
            }
        }

        return result;
    }

    public static LogPage GetLogs(long? cursor, int? logType, long? createdBy, string externalCode, bool errorsOnly)
    {
        const string sql = @"
SELECT TOP (101)
       CAST(D.ID AS BIGINT) AS DetailId,
       CAST(D.LOGID AS BIGINT) AS LogId,
       CAST(L.LOGTYPE AS INT) AS LogType,
       CONVERT(NVARCHAR(250), L.EXTERNALCODE1) AS ExternalCode,
       CONVERT(NVARCHAR(1000), D.DESCRIPTION) AS Description,
       CAST(D.LOGSTATUS AS INT) AS LogStatus,
       D.LOGDATETIME,
       CAST(D.LOGCREATEDBY AS BIGINT) AS CreatedBy,
       COALESCE(NULLIF(U.NAME, ''), NULLIF(U.USERNAME, ''), CONVERT(NVARCHAR(50), D.LOGCREATEDBY)) AS UserName
FROM dbo.OSUSR_U6P_LODDETAILS D WITH (NOLOCK)
INNER JOIN dbo.OSUSR_U6P_LOG L WITH (NOLOCK) ON L.ID = D.LOGID
LEFT JOIN DBSPlatform.dbo.ossys_User U WITH (NOLOCK) ON U.ID = D.LOGCREATEDBY
WHERE D.ISSYSTEM = 0
  AND (@Cursor IS NULL OR D.ID < @Cursor)
  AND (@LogType IS NULL OR L.LOGTYPE = @LogType)
  AND (@CreatedBy IS NULL OR D.LOGCREATEDBY = @CreatedBy)
  AND (@ExternalCode = '' OR L.EXTERNALCODE1 = @ExternalCode)
  AND (@ErrorsOnly = 0 OR D.LOGSTATUS = 3)
ORDER BY D.ID DESC;";

        LogPage page = new LogPage();
        using (SqlConnection connection = OpenConnection())
        using (SqlCommand command = new SqlCommand(sql, connection))
        {
            command.CommandTimeout = 30;
            command.Parameters.Add("@Cursor", SqlDbType.BigInt).Value = (object)cursor ?? DBNull.Value;
            command.Parameters.Add("@LogType", SqlDbType.Int).Value = (object)logType ?? DBNull.Value;
            command.Parameters.Add("@CreatedBy", SqlDbType.BigInt).Value = (object)createdBy ?? DBNull.Value;
            command.Parameters.Add("@ExternalCode", SqlDbType.NVarChar, 250).Value = (externalCode ?? String.Empty).Trim();
            command.Parameters.Add("@ErrorsOnly", SqlDbType.Bit).Value = errorsOnly;

            using (SqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    if (page.Items.Count == PageSize)
                    {
                        page.HasNextPage = true;
                        break;
                    }

                    page.Items.Add(new LogListItem
                    {
                        DetailId = Convert.ToInt64(reader["DetailId"]),
                        LogId = Convert.ToInt64(reader["LogId"]),
                        LogType = Convert.ToInt32(reader["LogType"]),
                        ExternalCode = reader["ExternalCode"] == DBNull.Value ? String.Empty : Convert.ToString(reader["ExternalCode"]),
                        Description = reader["Description"] == DBNull.Value ? String.Empty : Convert.ToString(reader["Description"]),
                        LogStatus = Convert.ToInt32(reader["LogStatus"]),
                        LogDateTime = Convert.ToDateTime(reader["LOGDATETIME"]),
                        CreatedBy = Convert.ToInt64(reader["CreatedBy"]),
                        UserName = Convert.ToString(reader["UserName"])
                    });
                }
            }
        }

        if (page.HasNextPage && page.Items.Count > 0)
        {
            page.NextCursor = page.Items[page.Items.Count - 1].DetailId;
        }

        return page;
    }

    public static IList<LogDetailItem> GetLogDetails(long selectedDetailId)
    {
        const string sql = @"
DECLARE @LogId BIGINT;
SELECT @LogId = LOGID
FROM dbo.OSUSR_U6P_LODDETAILS WITH (NOLOCK)
WHERE ID = @SelectedDetailId;

SELECT CAST(D.ID AS BIGINT) AS DetailId,
       CAST(D.LOGID AS BIGINT) AS LogId,
       CAST(L.LOGTYPE AS INT) AS LogType,
       CONVERT(NVARCHAR(250), L.EXTERNALCODE1) AS ExternalCode,
       CONVERT(NVARCHAR(1000), D.DESCRIPTION) AS Description,
       CONVERT(NVARCHAR(MAX), D.DETAILDATA) AS DetailData,
       CAST(D.LOGSTATUS AS INT) AS LogStatus,
       CAST(COALESCE(D.ISSYSTEM, D.ISSISTEM, 0) AS BIT) AS IsSystem,
       D.LOGDATETIME,
       CAST(D.LOGCREATEDBY AS BIGINT) AS CreatedBy,
       COALESCE(NULLIF(U.NAME, ''), NULLIF(U.USERNAME, ''), CONVERT(NVARCHAR(50), D.LOGCREATEDBY)) AS UserName
FROM dbo.OSUSR_U6P_LODDETAILS D WITH (NOLOCK)
INNER JOIN dbo.OSUSR_U6P_LOG L WITH (NOLOCK) ON L.ID = D.LOGID
LEFT JOIN DBSPlatform.dbo.ossys_User U WITH (NOLOCK) ON U.ID = D.LOGCREATEDBY
WHERE D.LOGID = @LogId
ORDER BY D.ID ASC;";

        List<LogDetailItem> result = new List<LogDetailItem>();
        using (SqlConnection connection = OpenConnection())
        using (SqlCommand command = new SqlCommand(sql, connection))
        {
            command.CommandTimeout = 30;
            command.Parameters.Add("@SelectedDetailId", SqlDbType.BigInt).Value = selectedDetailId;

            using (SqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    result.Add(new LogDetailItem
                    {
                        DetailId = Convert.ToInt64(reader["DetailId"]),
                        LogId = Convert.ToInt64(reader["LogId"]),
                        LogType = Convert.ToInt32(reader["LogType"]),
                        ExternalCode = reader["ExternalCode"] == DBNull.Value ? String.Empty : Convert.ToString(reader["ExternalCode"]),
                        Description = reader["Description"] == DBNull.Value ? String.Empty : Convert.ToString(reader["Description"]),
                        DetailData = reader["DetailData"] == DBNull.Value ? String.Empty : Convert.ToString(reader["DetailData"]),
                        LogStatus = Convert.ToInt32(reader["LogStatus"]),
                        IsSystem = Convert.ToBoolean(reader["IsSystem"]),
                        LogDateTime = Convert.ToDateTime(reader["LOGDATETIME"]),
                        CreatedBy = Convert.ToInt64(reader["CreatedBy"]),
                        UserName = Convert.ToString(reader["UserName"])
                    });
                }
            }
        }

        return result;
    }
}