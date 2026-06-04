using EcoHub.API.Data;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EcoHub.API.Services
{
    public class ReportService : IReportService
    {
        private readonly AppDbContext _context;

        public ReportService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<byte[]> GenerateOrderInvoiceAsync(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Items)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) return Array.Empty<byte>();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.Header().Text("EcoHub Invoice").FontSize(20).Bold().AlignCenter();
                    page.Content().Column(col =>
                    {
                        col.Item().Text($"Order #{order.Id}").FontSize(14).Bold();
                        col.Item().Text($"Customer: {order.User?.FirstName} {order.User?.LastName}");
                        col.Item().Text($"Email: {order.User?.Email}");
                        col.Item().Text($"Date: {order.CreatedAt:yyyy-MM-dd HH:mm}");
                        col.Item().Text($"Status: {order.Status}");
                        col.Item().PaddingVertical(10);
                        col.Item().LineHorizontal(1);
                        col.Item().PaddingVertical(10);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("Product").Bold();
                                header.Cell().Text("Qty").Bold();
                                header.Cell().Text("Unit Price").Bold();
                                header.Cell().Text("Total").Bold();
                            });

                            foreach (var item in order.Items)
                            {
                                table.Cell().Text(item.Product?.Name ?? "");
                                table.Cell().Text(item.Quantity.ToString());
                                table.Cell().Text($"{item.UnitPrice:C}");
                                table.Cell().Text($"{item.UnitPrice * item.Quantity:C}");
                            }
                        });

                        col.Item().PaddingVertical(10);
                        col.Item().LineHorizontal(1);
                        col.Item().PaddingVertical(10);
                        col.Item().Text($"Total: {order.TotalPrice:C}").FontSize(14).Bold().AlignRight();
                    });
                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("Thank you for shopping with EcoHub!");
                    });
                });
            });

            return document.GeneratePdf();
        }
    }
}
