using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SistemaERP.Application.Repositories;
using SistemaERP.Domain.Entities;
using SistemaERP.Application.Services;

namespace SistemaERP.Application.Services
{
    public class CashRegisterService : ICashRegisterService
    {
        private const string NUMBER_PREFIX = "CAJ-";

        private readonly ICashRegisterRepository _cashRegisterRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<CashRegisterService> _logger;

        public CashRegisterService(
            ICashRegisterRepository cashRegisterRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            ILogger<CashRegisterService> logger)
        {
            _cashRegisterRepository = cashRegisterRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<CashRegister> OpenAsync(
            Guid tenantId, Guid userId, decimal openingAmount, Guid? warehouseId = null, string? notes = null)
        {
            var existingOpen = await _cashRegisterRepository.GetOpenByUserIdAsync(tenantId, userId);
            if (existingOpen != null)
                throw new InvalidOperationException("Ya existe una caja abierta para este usuario.");

            var register = new CashRegister
            {
                TenantId = tenantId,
                CashRegisterNumber = await GenerateNextNumberAsync(tenantId),
                UserId = userId,
                WarehouseId = warehouseId,
                OpeningAmount = openingAmount,
                OpeningDate = DateTime.UtcNow,
                Status = CashRegisterStatus.Open,
                Notes = notes,
            };

            _logger.LogInformation("Opening cash register {Number} for user {UserId}.",
                register.CashRegisterNumber, userId);

            var created = await _cashRegisterRepository.AddAsync(register);

            await _auditService.LogAsync(
                tenantId,
                "CashRegisterOpened",
                "CashRegister",
                created.Id,
                new { created.CashRegisterNumber, created.OpeningAmount, created.UserId },
                userId: userId);

            return created;
        }

        public async Task CloseAsync(Guid cashRegisterId, decimal closingAmount, string? notes = null)
        {
            var register = await _cashRegisterRepository.GetByIdAsync(cashRegisterId);
            if (register == null)
                throw new InvalidOperationException("La caja no existe.");

            if (register.Status != CashRegisterStatus.Open)
                throw new InvalidOperationException("Solo se puede cerrar una caja que esté abierta.");

            var income = register.Movements
                .Where(m => m.Type == CashMovementType.Income)
                .Sum(m => m.Amount);
            var expense = register.Movements
                .Where(m => m.Type == CashMovementType.Expense)
                .Sum(m => m.Amount);

            register.ExpectedAmount = register.OpeningAmount + income - expense;
            register.Difference = closingAmount - register.ExpectedAmount;
            register.ClosingAmount = closingAmount;
            register.ClosingDate = DateTime.UtcNow;
            register.Status = CashRegisterStatus.Closed;
            if (notes != null) register.Notes = notes;

            _logger.LogInformation(
                "Closing cash register {Number}: expected {Expected}, difference {Difference}.",
                register.CashRegisterNumber, register.ExpectedAmount, register.Difference);

            await _cashRegisterRepository.UpdateAsync(register);

            await _auditService.LogAsync(
                register.TenantId,
                "CashRegisterClosed",
                "CashRegister",
                register.Id,
                new { register.CashRegisterNumber, register.ExpectedAmount, register.ClosingAmount, register.Difference, register.UserId },
                userId: register.UserId);
        }

        public async Task RegisterMovementAsync(
            Guid cashRegisterId,
            CashMovementType type,
            MovementReason reason,
            PaymentMethod paymentMethod,
            decimal amount,
            string description,
            Guid? saleId = null,
            Guid? userId = null,
            Guid? purchaseId = null)
        {
            var register = await _cashRegisterRepository.GetByIdAsync(cashRegisterId);
            if (register == null)
                throw new InvalidOperationException("La caja no existe.");

            if (register.Status != CashRegisterStatus.Open)
                throw new InvalidOperationException("Solo se pueden registrar movimientos en una caja abierta.");

            if (amount <= 0)
                throw new InvalidOperationException("El monto del movimiento debe ser mayor a cero.");

            // Validación previa del índice único parcial (CashRegisterId, SaleId, Type): evita
            // que la BD falle con excepción. Debe coincidir con el índice, por lo que se compara
            // también por Type: permite el movimiento de reversión (Type opuesto) para la misma
            // venta/compra, pero sigue bloqueando dos movimientos del MISMO Type (ej. dos Income).
            if (saleId.HasValue &&
                register.Movements.Any(m => m.SaleId == saleId.Value && m.Type == type))
            {
                throw new InvalidOperationException(
                    "Ya existe un movimiento de caja del mismo tipo registrado para esta venta en esta caja.");
            }

            // Lo mismo para PurchaseId, coherente con su índice (CashRegisterId, PurchaseId, Type).
            if (purchaseId.HasValue &&
                register.Movements.Any(m => m.PurchaseId == purchaseId.Value && m.Type == type))
            {
                throw new InvalidOperationException(
                    "Ya existe un movimiento de caja del mismo tipo registrado para esta compra en esta caja.");
            }

            var movement = new CashMovement
            {
                CashRegisterId = cashRegisterId,
                Type = type,
                Reason = reason,
                PaymentMethod = paymentMethod,
                Amount = amount,
                Description = description,
                SaleId = saleId,
                PurchaseId = purchaseId,
                CreatedBy = userId,
            };

            _logger.LogInformation(
                "Registering cash movement ({Type}/{Reason}) of {Amount} on register {Number}.",
                type, reason, amount, register.CashRegisterNumber);

            await _cashRegisterRepository.AddMovementAsync(movement);

            await _auditService.LogAsync(
                register.TenantId,
                "CashMovementRegistered",
                "CashMovement",
                movement.Id,
                new { movement.Type, movement.Reason, movement.Amount, movement.PaymentMethod, movement.SaleId, movement.PurchaseId, movement.Description },
                userId: userId);
        }

        public async Task<CashRegister?> GetOpenCashRegisterForUserAsync(Guid tenantId, Guid userId)
        {
            return await _cashRegisterRepository.GetOpenByUserIdAsync(tenantId, userId);
        }

        public async Task<IReadOnlyList<CashRegister>> GetAllAsync(int page = 1, int pageSize = 10)
        {
            return await _cashRegisterRepository.GetAllAsync(page, pageSize);
        }

        public async Task<CashRegister?> GetByIdAsync(Guid id)
        {
            return await _cashRegisterRepository.GetByIdAsync(id);
        }

        private async Task<string> GenerateNextNumberAsync(Guid tenantId)
        {
            var last = await _cashRegisterRepository.GetLastCashRegisterNumberAsync(tenantId);
            int next = 1;
            if (!string.IsNullOrEmpty(last) && last.StartsWith(NUMBER_PREFIX))
            {
                var numericPart = last.Substring(NUMBER_PREFIX.Length);
                if (int.TryParse(numericPart, out var lastNumber))
                    next = lastNumber + 1;
            }
            return $"{NUMBER_PREFIX}{next:D6}";
        }
    }
}
