using BuyOldBike_DAL.Entities;
using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuyOldBike_BLL.Services.Exports
{
    public class TransactionExportService
    {
        public void ExportTransactions(List<Order> transactions, string filePath)
        {
            using (var workbook = new XLWorkbook())
            {
                var sheet = workbook.Worksheets.Add("Transactions");

                sheet.Cell(1, 1).Value = "Payment ID";
                sheet.Cell(1, 2).Value = "User ID";
                sheet.Cell(1, 3).Value = "Amount";
                sheet.Cell(1, 4).Value = "Payment Type";
                sheet.Cell(1, 5).Value = "Status";
                sheet.Cell(1, 6).Value = "Created At";

                int row = 2;

                foreach (var t in transactions)
                {
                    sheet.Cell(row, 1).Value = t.OrderId.ToString();
                    sheet.Cell(row, 2).Value = t.Buyer?.ToString();
                    sheet.Cell(row, 3).Value = t.Listing?.Seller?.Email;
                    sheet.Cell(row, 4).Value = t.TotalAmount;
                    sheet.Cell(row, 5).Value = t.Status;
                    sheet.Cell(row, 6).Value = t.CreatedAt;

                    row++;
                }

                workbook.SaveAs(filePath);
            }
        }
    }
}
