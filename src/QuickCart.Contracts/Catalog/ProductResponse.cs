namespace QuickCart.Contracts.Catalog;

/// <summary>HTTP response shape for a product. Carries an image URL only — never binary data.</summary>
public sealed record ProductResponse(
    Guid ProductId,
    Guid CategoryId,
    string ProductName,
    string? Description,
    decimal Price,
    string? ImageUrl,
    int StockQuantity,
    bool IsAvailable);
