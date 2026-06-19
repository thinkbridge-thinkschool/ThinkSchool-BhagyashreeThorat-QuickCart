using System.ComponentModel.DataAnnotations;

namespace QuickCart.Contracts.Orders;

/// <summary>HTTP request body for creating an order. This is the public API boundary shape,
/// deliberately decoupled from the domain model. The DataAnnotations are placed on the record
/// primary-constructor parameters (no [property:] target) — .NET 10's validation requires the
/// metadata on the constructor parameter — so [ApiController] rejects malformed/oversized
/// input as 400 ValidationProblemDetails before it ever reaches the domain.</summary>
public sealed record CreateOrderRequest(
    Guid CustomerId,
    [Required]
    [MinLength(1, ErrorMessage = "An order must contain at least one line.")]
    [MaxLength(100, ErrorMessage = "An order cannot contain more than 100 lines.")]
    IReadOnlyList<CreateOrderLineDto> Lines);

public sealed record CreateOrderLineDto(
    Guid ProductId,
    [Required]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "ProductName must be 1–200 characters.")]
    string ProductName,
    [Range(0, 1_000_000, ErrorMessage = "UnitPrice must be between 0 and 1,000,000.")]
    decimal UnitPrice,
    [Range(1, 10_000, ErrorMessage = "Quantity must be between 1 and 10,000.")]
    int Quantity);
