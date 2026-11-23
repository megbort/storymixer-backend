using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace StoryMixer.Api.Features.Auth;

public class BearerSecurityRequirementTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var requiresAuthorization = context.Description.ActionDescriptor.EndpointMetadata.OfType<IAuthorizeData>().Any();

        if (requiresAuthorization)
        {
            operation.Security =
            [
                new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference(BearerSecuritySchemeTransformer.SchemeName, context.Document)] = [],
                },
            ];
        }

        return Task.CompletedTask;
    }
}
