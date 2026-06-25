<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Detail.aspx.cs" Inherits="DetailPage" %>
<!DOCTYPE html>
<html lang="pt">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Detalhe do log - DBS Logs</title>
    <link rel="stylesheet" href="Content/site.css" />
</head>
<body>
<form id="MainForm" runat="server">
<header class="topbar"><div><h1>Detalhe do log</h1><p>Eventos associados ao mesmo LogId</p></div></header>
<main class="container">
    <p><a class="button" href="Viewer.aspx">Voltar à listagem</a></p>
    <asp:Panel ID="ErrorPanel" runat="server" CssClass="alert alert-error" Visible="false"><strong>Não foi possível carregar o detalhe.</strong><asp:Label ID="ErrorMessageLabel" runat="server" /></asp:Panel>
    <asp:Panel ID="ContentPanel" runat="server">
        <section class="panel summary-grid">
            <div><span>LogId</span><strong><asp:Label ID="LogIdLabel" runat="server" /></strong></div>
            <div><span>Tipo</span><strong><asp:Label ID="LogTypeLabel" runat="server" /></strong></div>
            <div><span>Proposta/operação</span><strong><asp:Label ID="ExternalCodeLabel" runat="server" /></strong></div>
            <div><span>Eventos</span><strong><asp:Label ID="DetailCountLabel" runat="server" /></strong></div>
        </section>
        <asp:Repeater ID="DetailsRepeater" runat="server">
            <ItemTemplate>
                <article class="panel event-card">
                    <div class="event-header"><div><strong>#<%# Eval("DetailId") %></strong><span><%# Eval("LogDateTime", "{0:dd/MM/yyyy HH:mm:ss}") %></span></div><span class='status <%# StatusClass(Eval("LogStatus")) %>'><%# StatusText(Eval("LogStatus")) %></span></div>
                    <dl class="metadata"><dt>Descrição</dt><dd><%#: Eval("Description") %></dd><dt>Utilizador</dt><dd><%#: Eval("UserName") %></dd><dt>Sistema</dt><dd><%# Convert.ToBoolean(Eval("IsSystem")) ? "Sim" : "Não" %></dd></dl>
                    <div class="detail-block"><div class="detail-title">DetailData</div><pre><%#: Eval("DetailData") %></pre></div>
                </article>
            </ItemTemplate>
        </asp:Repeater>
    </asp:Panel>
</main>
</form>
</body>
</html>
