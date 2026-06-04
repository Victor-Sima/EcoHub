namespace EcoHub.API.Services
{
    public interface IReportService
    {
        Task<byte[]> GenerateOrderInvoiceAsync(int orderId);
    }
}
