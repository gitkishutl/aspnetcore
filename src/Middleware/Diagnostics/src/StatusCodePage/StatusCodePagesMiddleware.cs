// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Diagnostics
{
    /// <summary>
    /// A middleware for generating the response body of error status codes with no body.
    /// </summary>
    public class StatusCodePagesMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly StatusCodePagesOptions _options;

        /// <summary>
        /// Creates a new <see cref="StatusCodePagesMiddleware"/>
        /// </summary>
        /// <param name="next">The <see cref="RequestDelegate"/> representing the next middleware in the pipeline.</param>
        /// <param name="options">The options for configuring the middleware.</param>
        public StatusCodePagesMiddleware(RequestDelegate next, IOptions<StatusCodePagesOptions> options)
        {
            _next = next;
            _options = options.Value;
            if (_options.HandleAsync == null)
            {
                throw new ArgumentException("Missing options.HandleAsync implementation.");
            }
        }

        /// <summary>
        /// Executes the middleware.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/> for the current request.</param>
        /// <returns>A task that represents the execution of this middleware.</returns>
        public async Task Invoke(HttpContext context)
        {
            var statusCodeFeature = new StatusCodePagesFeature();
            context.Features.Set<IStatusCodePagesFeature>(statusCodeFeature);

            await _next(context);

            if (!statusCodeFeature.Enabled)
            {
                // Check if the feature is still available because other middleware (such as a web API written in MVC) could
                // have disabled the feature to prevent HTML status code responses from showing up to an API client.
                return;
            }

            // Do nothing if a response body has already been provided.
            if (context.Response.HasStarted
                || context.Response.StatusCode < 400
                || context.Response.StatusCode >= 600
                || context.Response.ContentLength.HasValue
                || !string.IsNullOrEmpty(context.Response.ContentType))
            {
                return;
            }

            var statusCodeContext = new StatusCodeContext(context, _options, _next);
            await _options.HandleAsync(statusCodeContext);
        }
    }
}
