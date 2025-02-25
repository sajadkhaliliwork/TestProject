using HtmlConvert.Contracts;
using HtmlConvert.HtmlConvert.Implementing;
using Microsoft.Extensions.Options;
using OfficeOpenXml;

namespace HtmlConvert.Implementing;
public class ExcelLinkProvider : ILinkProvider
{
    private readonly string _excelFilePath;

    public ExcelLinkProvider(IOptions<ExcelLinkOptions> options)
    {
        _excelFilePath = options.Value.Path;
    }

    public Dictionary<string, string> GetLinks()
    {
        var linkTable = new Dictionary<string, string>();

       
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        FileInfo fileInfo = new FileInfo(_excelFilePath);
        using (var package = new ExcelPackage(fileInfo))
        {

            ExcelWorksheet worksheet = package.Workbook.Worksheets[0];


            int rowCount = worksheet.Dimension.Rows;

            for (int row = 2; row <= rowCount; row++)
            {

                string key = worksheet.Cells[row, 1].GetValue<string>()?.Trim();
                string url = worksheet.Cells[row, 3].GetValue<string>()?.Trim();

                if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(url))
                {
                    linkTable[key] = url;
                }
            }
        }

        return linkTable;
    }
}
