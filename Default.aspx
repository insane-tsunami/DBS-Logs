<%@ Page Language="C#" %>
<%@ Import Namespace="System" %>

<!DOCTYPE html>
<html lang="pt">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>DBS Logs</title>
    <style>
        * { box-sizing: border-box; }
        body {
            margin: 0;
            padding: 48px;
            background: #f3f4f6;
            color: #111827;
            font-family: Arial, Helvetica, sans-serif;
        }
        .card {
            max-width: 760px;
            margin: 32px auto;
            padding: 24px;
            background: #ffffff;
            border: 1px solid #d1d5db;
            border-radius: 6px;
        }
        h1 { margin: 0 0 24px; }
        .status { color: #00875a; }
        code {
            padding: 3px 6px;
            background: #f3f4f6;
            font-family: Consolas, monospace;
        }
    </style>
</head>
<body>
    <main class="card">
        <h1>DBS Logs</h1>
        <p class="status">Aplicação ASP.NET em funcionamento.</p>
        <p>Servidor: <code><%= Server.HtmlEncode(Environment.MachineName) %></code></p>
        <p>Data e hora: <code><%= DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") %></code></p>
        <p>Runtime: <code><%= Server.HtmlEncode(Environment.Version.ToString()) %></code></p>
    </main>
</body>
</html>
