using Asp.Versioning;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using Umbraco.Cms.Api.Common.OpenApi;
using Umbraco.Cms.Api.Management.OpenApi;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.Community.AIProvenance.Api;

/// <summary>
/// Registers the Swagger document for the package's backoffice API.
/// </summary>
/// <remarks>
/// Umbraco 18 replaces Swashbuckle, so this folder changes for the Umbraco 18 build.
/// </remarks>
public sealed class AiProvenanceApiComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddSingleton<IOperationIdHandler, AiProvenanceOperationIdHandler>();

        builder.Services.Configure<SwaggerGenOptions>(options =>
        {
            options.SwaggerDoc(Constants.ApiName, new OpenApiInfo
            {
                Title = "AI Provenance Backoffice API",
                Version = "1.0",
            });

            options.OperationFilter<AiProvenanceSecurityRequirementsOperationFilter>();
        });
    }

    /// <summary>
    /// Adds the backoffice authentication requirement to the package's Swagger document.
    /// </summary>
    public sealed class AiProvenanceSecurityRequirementsOperationFilter : BackOfficeSecurityRequirementsOperationFilterBase
    {
        protected override string ApiName => Constants.ApiName;
    }

    /// <summary>
    /// Gives the package's operations readable IDs, such as GetDocumentMarks, for the generated TypeScript client.
    /// </summary>
    public sealed class AiProvenanceOperationIdHandler : OperationIdHandler
    {
        public AiProvenanceOperationIdHandler(IOptions<ApiVersioningOptions> apiVersioningOptions)
            : base(apiVersioningOptions)
        {
        }

        protected override bool CanHandle(ApiDescription apiDescription, ControllerActionDescriptor controllerActionDescriptor) =>
            controllerActionDescriptor.ControllerTypeInfo.Namespace?.StartsWith(
                typeof(AiProvenanceApiComposer).Namespace!, StringComparison.Ordinal) is true;
    }
}
