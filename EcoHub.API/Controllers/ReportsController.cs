using EcoHub.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcoHub.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportsController(IReportService reportService)
        {
            _reportService = reportService;
        }

        [HttpGet("order/{orderId}/pdf")]
        public async Task<IActionResult> GetOrderPdf(int orderId)
        {
            var pdfBytes = await _reportService.GenerateOrderInvoiceAsync(orderId);
            if (pdfBytes.Length == 0) return NotFound();
            return File(pdfBytes, "application/pdf", $"invoice-order-{orderId}.pdf");
        }
    }
}
