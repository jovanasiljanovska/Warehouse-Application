using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;

namespace Warehouse.Tests.Integration.Infrastructure;

public sealed class NoOpAntiforgery : IAntiforgery
{
    public AntiforgeryTokenSet GetAndStoreTokens(HttpContext httpContext)
        => new("test-token", "test-cookie", "__RequestVerificationToken", "X-CSRF-TOKEN");

    public AntiforgeryTokenSet GetTokens(HttpContext httpContext)
        => new("test-token", "test-cookie", "__RequestVerificationToken", "X-CSRF-TOKEN");

    public Task<bool> IsRequestValidAsync(HttpContext httpContext) => Task.FromResult(true);

    public void SetCookieTokenAndHeader(HttpContext httpContext)
    {
       // throw new NotImplementedException();
    }

    public Task ValidateRequestAsync(HttpContext httpContext) => Task.CompletedTask;
}
