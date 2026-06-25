using System.ComponentModel.DataAnnotations;

namespace QuickCart.Contracts.Carts;

/// <summary>Add a product to the cart (or increase its quantity). Validation lives on the ctor
/// parameters per the .NET 10 convention used across this API, so [ApiController] rejects bad
/// input as 400 before it reaches the domain.</summary>
public sealed record AddCartItemRequest(
    [Required] Guid ProductId,
    [Range(1, 10_000, ErrorMessage = "Quantity must be between 1 and 10,000.")]
    int Quantity);

/// <summary>Set the absolute quantity for a product already in the cart.</summary>
public sealed record UpdateCartItemRequest(
    [Range(1, 10_000, ErrorMessage = "Quantity must be between 1 and 10,000.")]
    int Quantity);
