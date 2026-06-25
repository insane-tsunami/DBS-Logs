using System;
using System.Collections.Generic;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class DefaultPage : Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            BindFilters();
            ApplyQueryString();
            BindLogs();
        }
    }

    protected void SearchButton_Click(object sender, EventArgs e)
    {
        BindLogs();
    }

    protected void ClearButton_Click(object sender, EventArgs e)
    {
        Response.Redirect("Default.aspx?errors=1", false);
        Context.ApplicationInstance.CompleteRequest();
    }

    private void BindFilters()
    {
        TypeDropDown.Items.Clear();
        TypeDropDown.Items.Add(new ListItem("Todos", String.Empty));
        foreach (FilterOption option in LogRepository.GetLogTypes())
            TypeDropDown.Items.Add(new ListItem(option.Text, option.Value.ToString()));

        UserDropDown.Items.Clear();
        UserDropDown.Items.Add(new ListItem("Todos", String.Empty));
        foreach (FilterOption option in LogRepository.GetUsers())
            UserDropDown.Items.Add(new ListItem(option.Text, option.Value.ToString()));
    }

    private void ApplyQueryString()
    {
        SelectValue(TypeDropDown, Request.QueryString["type"]);
        SelectValue(UserDropDown, Request.QueryString["user"]);
        ExternalCodeTextBox.Text = Request.QueryString["external"] ?? String.Empty;
        ErrorsOnlyCheckBox.Checked = Request.QueryString["errors"] == null || Request.QueryString["errors"] == "1";
        CursorHiddenField.Value = Request.QueryString["cursor"] ?? String.Empty;
    }

    private void BindLogs()
    {
        ErrorPanel.Visible = false;
        try
        {
            long? cursor = ParseNullableLong(CursorHiddenField.Value);
            int? logType = ParseNullableInt(TypeDropDown.SelectedValue);
            long? userId = ParseNullableLong(UserDropDown.SelectedValue);
            string externalCode = (ExternalCodeTextBox.Text ?? String.Empty).Trim();

            LogPage page = LogRepository.GetLogs(cursor, logType, userId, externalCode, ErrorsOnlyCheckBox.Checked);
            LogsRepeater.DataSource = page.Items;
            LogsRepeater.DataBind();
            EmptyPanel.Visible = page.Items.Count == 0;
            ResultCountLabel.Text = page.Items.Count + " registos nesta página";

            StartLink.Visible = cursor.HasValue;
            NextLink.Visible = page.HasNextPage && page.NextCursor.HasValue;
            if (NextLink.Visible)
            {
                NextLink.NavigateUrl = BuildUrl(page.NextCursor.Value, logType, userId, externalCode, ErrorsOnlyCheckBox.Checked);
            }
        }
        catch (Exception ex)
        {
            ErrorMessageLabel.Text = Server.HtmlEncode(ex.Message);
            ErrorPanel.Visible = true;
            LogsRepeater.DataSource = new List<LogListItem>();
            LogsRepeater.DataBind();
            EmptyPanel.Visible = true;
            ResultCountLabel.Text = "0 registos nesta página";
            NextLink.Visible = false;
        }
    }

    protected string StatusText(object value)
    {
        int status = Convert.ToInt32(value);
        return status == 3 ? "Erro" : status.ToString();
    }

    protected string StatusClass(object value)
    {
        return Convert.ToInt32(value) == 3 ? "status-error" : "status-neutral";
    }

    private string BuildUrl(long cursor, int? logType, long? userId, string externalCode, bool errorsOnly)
    {
        return "Default.aspx?cursor=" + cursor
            + "&type=" + (logType.HasValue ? logType.Value.ToString() : String.Empty)
            + "&user=" + (userId.HasValue ? userId.Value.ToString() : String.Empty)
            + "&external=" + Server.UrlEncode(externalCode)
            + "&errors=" + (errorsOnly ? "1" : "0");
    }

    private static void SelectValue(ListControl control, string value)
    {
        if (!String.IsNullOrEmpty(value) && control.Items.FindByValue(value) != null)
            control.SelectedValue = value;
    }

    private static long? ParseNullableLong(string value)
    {
        long parsed;
        return Int64.TryParse(value, out parsed) ? (long?)parsed : null;
    }

    private static int? ParseNullableInt(string value)
    {
        int parsed;
        return Int32.TryParse(value, out parsed) ? (int?)parsed : null;
    }
}