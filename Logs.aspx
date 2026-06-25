<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Default.aspx.cs" Inherits="DefaultPage" %>
<!DOCTYPE html>
<html lang="pt">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>DBS Logs</title>
    <link rel="stylesheet" href="Content/site.css" />
</head>
<body>
<form id="MainForm" runat="server">
<header class="topbar"><div><h1>DBS Logs</h1><p>Consulta somente leitura dos logs funcionais da Credibox</p></div></header>
<main class="container">
    <asp:Panel ID="ErrorPanel" runat="server" CssClass="alert alert-error" Visible="false">
        <strong>Não foi possível consultar os logs.</strong>
        <asp:Label ID="ErrorMessageLabel" runat="server" />
    </asp:Panel>

    <section class="panel">
        <div class="filters">
            <div class="field"><label for="TypeDropDown">Tipo</label><asp:DropDownList ID="TypeDropDown" runat="server" ClientIDMode="Static" /></div>
            <div class="field"><label for="UserDropDown">Utilizador</label><asp:DropDownList ID="UserDropDown" runat="server" ClientIDMode="Static" /></div>
            <div class="field field-wide"><label for="ExternalCodeTextBox">Proposta/operação</label><asp:TextBox ID="ExternalCodeTextBox" runat="server" ClientIDMode="Static" MaxLength="250" /></div>
            <div class="field checkbox-field"><asp:CheckBox ID="ErrorsOnlyCheckBox" runat="server" Text="Apenas erros funcionais" Checked="true" /></div>
            <div class="actions">
                <asp:Button ID="SearchButton" runat="server" Text="Pesquisar" CssClass="button button-primary" OnClick="SearchButton_Click" />
                <asp:Button ID="ClearButton" runat="server" Text="Limpar" CssClass="button" CausesValidation="false" OnClick="ClearButton_Click" />
            </div>
        </div>
        <asp:HiddenField ID="CursorHiddenField" runat="server" />
    </section>

    <section class="panel table-panel">
        <div class="panel-header"><h2>Resultados</h2><asp:Label ID="ResultCountLabel" runat="server" /></div>
        <div class="table-wrap">
            <table>
                <thead><tr><th>Data/hora</th><th>Tipo</th><th>Proposta/operação</th><th>Descrição</th><th>Utilizador</th><th>Estado</th><th></th></tr></thead>
                <tbody>
                <asp:Repeater ID="LogsRepeater" runat="server">
                    <ItemTemplate>
                        <tr>
                            <td class="nowrap"><%# Eval("LogDateTime", "{0:dd/MM/yyyy HH:mm:ss}") %></td>
                            <td><%# Eval("LogType") %></td>
                            <td><%#: Eval("ExternalCode") %></td>
                            <td class="description"><%#: Eval("Description") %></td>
                            <td><%#: Eval("UserName") %></td>
                            <td><span class='status <%# StatusClass(Eval("LogStatus")) %>'><%# StatusText(Eval("LogStatus")) %></span></td>
                            <td><a class="link" href='Detail.aspx?id=<%# Eval("DetailId") %>'>Detalhes</a></td>
                        </tr>
                    </ItemTemplate>
                </asp:Repeater>
                </tbody>
            </table>
            <asp:Panel ID="EmptyPanel" runat="server" CssClass="empty">Nenhum registo encontrado.</asp:Panel>
        </div>
        <div class="pagination">
            <asp:HyperLink ID="StartLink" runat="server" NavigateUrl="Logs.aspx?errors=1" CssClass="button" Text="Voltar ao início" Visible="false" />
            <asp:HyperLink ID="NextLink" runat="server" CssClass="button button-primary" Text="Página seguinte" Visible="false" />
        </div>
    </section>
</main>
</form>
</body>
</html>
