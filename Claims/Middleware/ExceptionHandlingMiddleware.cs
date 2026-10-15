using FluentValidation;

namespace Claims.Middleware
{
    public class ExceptionHandlingMiddleware(RequestDelegate next)
    {
        public async Task Invoke(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (Exception error)
            {
                var title = string.Empty;

                if (error is ValidationException)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;

                    title = "Validation failed";
                }
                else
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;

                    title = "An unexpected error occurred.";
                }

                await Results.Problem(statusCode: context.Response.StatusCode, title: title, detail: error.Message).ExecuteAsync(context);
            }
        }
    }
}
