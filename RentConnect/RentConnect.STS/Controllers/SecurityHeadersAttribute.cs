namespace RentConnect.STS.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Filters;

    public class SecurityHeadersAttribute : ActionFilterAttribute
    {
        public override void OnResultExecuting(ResultExecutingContext context)
        {
            var result = context.Result;
            if (result is ViewResult)
            {
                // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/X-Content-Type-Options
                if (!context.HttpContext.Response.Headers.ContainsKey("X-Content-Type-Options"))
                {
                    context.HttpContext.Response.Headers.Add("X-Content-Type-Options", "nosniff");
                }

                // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/X-Frame-Options
                if (!context.HttpContext.Response.Headers.ContainsKey("X-Frame-Options"))
                {
                    context.HttpContext.Response.Headers.Add("X-Frame-Options", "SAMEORIGIN");
                }

                // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Content-Security-Policy

                //var csp = "default-src 'self'; connect-src wss://localhost:44353 wss://localhost:44385 http://localhost:5001 https://api.getaddress.io; style-src 'self' 'sha256-aqNNdDLnnrDOnTNdkJpYlAxKVJtLt9CtFLklmInuUAE='; object-src 'none'; frame-ancestors 'none'; sandbox allow-forms allow-same-origin allow-scripts allow-downloads; base-uri 'self'; img-src 'self' data:;";

                var csp = "default-src 'self'; connect-src https://lordhood.me https://api.getaddress.io; style-src 'self' 'sha256-aqNNdDLnnrDOnTNdkJpYlAxKVJtLt9CtFLklmInuUAE='; object-src 'none'; frame-ancestors 'none'; sandbox allow-forms allow-same-origin allow-scripts allow-downloads; base-uri 'self'; img-src 'self' data:;";

                // once for standards compliant browsers
                if (!context.HttpContext.Response.Headers.ContainsKey("Content-Security-Policy"))
                {
                    context.HttpContext.Response.Headers.Add("Content-Security-Policy", csp);
                }

                // and once again for IE
                if (!context.HttpContext.Response.Headers.ContainsKey("X-Content-Security-Policy"))
                {
                    context.HttpContext.Response.Headers.Add("X-Content-Security-Policy", csp);
                }

                // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Referrer-Policy
                var referrer_policy = "no-referrer";
                if (!context.HttpContext.Response.Headers.ContainsKey("Referrer-Policy"))
                {
                    context.HttpContext.Response.Headers.Add("Referrer-Policy", referrer_policy);
                }
            }
        }
    }
}