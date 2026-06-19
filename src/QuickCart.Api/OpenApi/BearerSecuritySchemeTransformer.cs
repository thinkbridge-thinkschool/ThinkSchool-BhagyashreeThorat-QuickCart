using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace QuickCart.Api.OpenApi;

/// <summary>Describes the Entra ID JWT bearer scheme in the generated OpenAPI document and
/// marks the API as requiring it. Auth is *enforced* by the FallbackPolicy in Program.cs;
/// this transformer makes that contract visible to API explorers and generated clients.</summary>
internal sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        var bearer = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Entra ID (Azure AD) JWT bearer token. Send as: Authorization: Bearer <token>."
        };

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = bearer;

        document.Security ??= new List<OpenApiSecurityRequirement>();
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document, null)] = new List<string>()
        });

        return Task.CompletedTask;
    }
}
