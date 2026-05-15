using EcoHub.API.Data;
using EcoHub.Shared.Enums;
using EcoHub.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoHub.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PaymentsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PaymentsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<ActionResult<PaymentDto>> ProcessPayment(ProcessPaymentRequest request)
        {
            var order = await _context.Orders.FindAsync(request.OrderId);
            if (order == null) return NotFound();

            var payment = new Data.Models.Payment
            {
                OrderId = request.OrderId,
                Amount = order.TotalPrice,
                Method = request.Method,
                Status = PaymentStatus.Paid,
                CreatedAt = DateTime.UtcNow
            };
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            return Ok(new PaymentDto
            {
                Id = payment.Id,
                OrderId = payment.OrderId,
                Amount = payment.Amount,
                Method = payment.Method,
                Status = payment.Status,
                CreatedAt = payment.CreatedAt
            });
        }
    }
}
