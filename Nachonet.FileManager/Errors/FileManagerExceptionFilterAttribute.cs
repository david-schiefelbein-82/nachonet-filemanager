using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Nachonet.FileManager.Errors
{
    public class FileManagerExceptionFilterAttribute : ExceptionFilterAttribute
    {
        public override void OnException(ExceptionContext context)
        {
            if (context.Exception is Exception ex)
            {
                HttpStatusCode statusCode = HttpStatusCode.InternalServerError;

                if (ex is FileManagerException fmEx)
                    statusCode = fmEx.StatusCode;

                var cr = new ContentResult
                {
                    StatusCode = (int)statusCode,
                    Content = ex.Message,
                    ContentType = "text/plain"
                };


                try
                {
                    string msg = ex.Message;
                    msg = msg.Replace('\r', ' ');
                    msg = msg.Replace('\n', ' ');
                    context.HttpContext.Response.Headers["ErrorMessage"] = msg;
                }
                catch (Exception addEx)
                {
                    System.Diagnostics.Debug.WriteLine(addEx.ToString());
                }

                context.HttpContext.Response.Headers["ErrorType"] = ex.GetType().Name;
                context.Result = cr;
            }
            else
            {
                base.OnException(context);
            }

        }

        public override Task OnExceptionAsync(ExceptionContext context)
        {
            return base.OnExceptionAsync(context);
        }
    }
}

