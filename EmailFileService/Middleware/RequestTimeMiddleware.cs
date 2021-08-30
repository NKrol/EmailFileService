using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace EmailFileService.Middleware
{
    public class RequestTimeMiddleware : IMiddleware
    {
        private readonly Stopwatch _stopWatch;
        private readonly ILogger<RequestTimeMiddleware> _logger;

        public RequestTimeMiddleware(ILogger<RequestTimeMiddleware> logger)
        {
            _stopWatch = new Stopwatch();
            _logger = logger;
        }
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            _stopWatch.Start();
            await next.Invoke(context);
            _stopWatch.Stop();


            var elapsedMilliSecond = _stopWatch.ElapsedMilliseconds;

            if (elapsedMilliSecond / 1000 > 2)
            {
                var message =
                    $"Request [{context.Request.Method}] at {context.Request.Path} took {elapsedMilliSecond} ms";

                _logger.LogInformation(message);

            }
        }
    }
}
