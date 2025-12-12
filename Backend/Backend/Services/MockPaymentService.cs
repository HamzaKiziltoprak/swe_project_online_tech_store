using Backend.DTOs;

namespace Backend.Services
{
    /// <summary>
    /// Mock Payment Service - Simulates payment processing
    /// In production, this would integrate with real payment gateways
    /// </summary>
    public interface IPaymentService
    {
        Task<PaymentResponse> ProcessPaymentAsync(decimal amount, string paymentMethod, int userId);
        Task<PaymentResponse> RefundPaymentAsync(string transactionId, decimal amount);
        Task<bool> ValidatePaymentMethodAsync(int userId);
    }

    public class MockPaymentService : IPaymentService
    {
        private readonly ILogger<MockPaymentService> _logger;
        private readonly Random _random;

        public MockPaymentService(ILogger<MockPaymentService> logger)
        {
            _logger = logger;
            _random = new Random();
        }

        /// <summary>
        /// Process payment - Mock implementation
        /// Simulates: 80% success, 15% insufficient funds, 5% failure
        /// </summary>
        public async Task<PaymentResponse> ProcessPaymentAsync(decimal amount, string paymentMethod, int userId)
        {
            // Simulate processing delay
            await Task.Delay(500);

            var randomValue = _random.Next(100);
            var transactionId = $"TXN-{DateTime.UtcNow:yyyyMMddHHmmss}-{_random.Next(1000, 9999)}";

            // 80% success rate
            if (randomValue < 80)
            {
                _logger.LogInformation($"Payment authorized: {transactionId}, Amount: {amount}, User: {userId}");
                return new PaymentResponse
                {
                    Success = true,
                    TransactionId = transactionId,
                    Message = "Payment authorized successfully",
                    Status = "Authorized"
                };
            }
            // 15% insufficient funds
            else if (randomValue < 95)
            {
                _logger.LogWarning($"Payment failed - Insufficient funds: User {userId}, Amount: {amount}");
                return new PaymentResponse
                {
                    Success = false,
                    TransactionId = transactionId,
                    Message = "Insufficient funds",
                    Status = "InsufficientFunds"
                };
            }
            // 5% general failure
            else
            {
                _logger.LogError($"Payment failed - General error: User {userId}, Amount: {amount}");
                return new PaymentResponse
                {
                    Success = false,
                    TransactionId = transactionId,
                    Message = "Payment processing failed. Please try again.",
                    Status = "Failed"
                };
            }
        }

        /// <summary>
        /// Process refund - Mock implementation
        /// Always succeeds in mock
        /// </summary>
        public async Task<PaymentResponse> RefundPaymentAsync(string transactionId, decimal amount)
        {
            await Task.Delay(300);

            var refundTransactionId = $"REFUND-{DateTime.UtcNow:yyyyMMddHHmmss}-{_random.Next(1000, 9999)}";

            _logger.LogInformation($"Refund processed: {refundTransactionId}, Original: {transactionId}, Amount: {amount}");

            return new PaymentResponse
            {
                Success = true,
                TransactionId = refundTransactionId,
                Message = "Refund processed successfully",
                Status = "Refunded"
            };
        }

        /// <summary>
        /// Validate payment method - Mock implementation
        /// Simulates checking if user has a valid payment method
        /// </summary>
        public async Task<bool> ValidatePaymentMethodAsync(int userId)
        {
            await Task.Delay(100);

            // In mock, all users are assumed to have valid payment methods
            // In production, this would check with the payment gateway
            _logger.LogInformation($"Payment method validated for user: {userId}");
            return true;
        }
    }
}
