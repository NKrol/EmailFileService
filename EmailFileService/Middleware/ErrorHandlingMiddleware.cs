using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EmailFileService.Exception;
using Microsoft.AspNetCore.Http;

namespace EmailFileService.Middleware
{
    public class ErrorHandlingMiddleware : IMiddleware
    {
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            try
            {
                await next.Invoke(context);
            }
            catch (FileExistException fileExistException)
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync(fileExistException.Message);
            }
            catch (ForbidException forbidException)
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync(forbidException.Message);
            }
            catch (NotFoundException notFoundException)
            {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync(notFoundException.Message);
            }
            catch (CreatedAccountException e)
            {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync(e.Message);
            }
            catch (InvalidOperationException e)
            {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync(e.Message);
            }
            catch (System.Exception e)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync(e.Message + e.Source);
            }
        }
    }
}
