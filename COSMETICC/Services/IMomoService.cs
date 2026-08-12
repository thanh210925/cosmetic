using System.Threading.Tasks;
using COSMETICC.Models;

namespace COSMETICC.Services
{
    public interface IMomoService
    {
        Task<MomoCreatePaymentResponseModel> CreatePaymentAsync(Order order, string? returnUrl = null, string? ipnUrl = null);
        MomoExecuteResponseModel PaymentExecuteAsync(IQueryCollection collection);
    }
}
