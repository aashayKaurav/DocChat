using System.Text;
using UglyToad.PdfPig;

namespace DocChat.Consumer.Services;

public class PdfParserService
{
    public string ExtractText(string filePath)
    {
        var sb = new StringBuilder();

        using var document = PdfDocument.Open(filePath);

        foreach (var page in document.GetPages())
        {
            sb.AppendLine(page.Text);
        }

        return sb.ToString();
    }
}