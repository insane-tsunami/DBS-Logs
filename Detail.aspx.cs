using System;
using System.Collections.Generic;
using System.Web.UI;

public partial class DetailPage : Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
            BindDetails();
    }

    private void BindDetails()
    {
        long detailId;
        if (!Int64.TryParse(Request.QueryString["id"], out detailId))
        {
            ShowError("Identificador de detalhe inválido.");
            return;
        }

        try
        {
            IList<LogDetailItem> items = LogRepository.GetLogDetails(detailId);
            if (items.Count == 0)
            {
                ShowError("O registo solicitado não foi encontrado.");
                return;
            }

            LogDetailItem first = items[0];
            LogIdLabel.Text = Server.HtmlEncode(first.LogId.ToString());
            LogTypeLabel.Text = Server.HtmlEncode(first.LogType.ToString());
            ExternalCodeLabel.Text = Server.HtmlEncode(first.ExternalCode);
            DetailCountLabel.Text = items.Count + " eventos";
            DetailsRepeater.DataSource = items;
            DetailsRepeater.DataBind();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private void ShowError(string message)
    {
        ErrorMessageLabel.Text = Server.HtmlEncode(message);
        ErrorPanel.Visible = true;
        ContentPanel.Visible = false;
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
}