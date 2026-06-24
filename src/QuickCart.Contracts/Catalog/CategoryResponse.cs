namespace QuickCart.Contracts.Catalog;

/// <summary>HTTP response shape for a category.</summary>
public sealed record CategoryResponse(
    Guid CategoryId,
    string CategoryName,
    string? Description);
