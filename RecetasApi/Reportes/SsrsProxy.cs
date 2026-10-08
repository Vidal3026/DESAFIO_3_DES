using System.Net;
using Yarp.ReverseProxy.Forwarder;

namespace RecetasApi.Reportes
{
    // Reenvía las peticiones de /ReportServer/... hacia el servidor de SSRS.
    // Así el visor de SSRS se sirve desde el mismo origen que la aplicación web:
    //  - SSRS envía "X-Frame-Options: SAMEORIGIN", que bloquea el iframe si el origen es distinto.
    //  - Solo los usuarios autenticados con Identity pueden ver los reportes.
    //  - El navegador no pide credenciales de Windows: la app se autentica ante SSRS
    //    con la cuenta de Windows con la que se ejecuta.
    public static class SsrsProxy
    {
        public static IServiceCollection AddSsrsProxy(this IServiceCollection services)
        {
            services.AddHttpForwarder();
            return services;
        }

        public static IEndpointConventionBuilder MapSsrsProxy(this WebApplication app)
        {
            var servidor = (app.Configuration["Ssrs:ServidorUrl"] ?? "http://localhost").TrimEnd('/');

            var cliente = new HttpMessageInvoker(new SocketsHttpHandler
            {
                UseProxy = false,
                AllowAutoRedirect = false,
                AutomaticDecompression = DecompressionMethods.None,
                UseCookies = false,
                Credentials = CredentialCache.DefaultCredentials
            });

            var transformador = new TransformadorSsrs(servidor);

            return app.Map("/ReportServer/{**resto}", async (HttpContext context, IHttpForwarder forwarder) =>
            {
                var error = await forwarder.SendAsync(context, servidor, cliente, ForwarderRequestConfig.Empty, transformador);
                if (error != ForwarderError.None && !context.Response.HasStarted)
                {
                    context.Response.StatusCode = StatusCodes.Status502BadGateway;
                    await context.Response.WriteAsync("No se pudo conectar con SQL Server Reporting Services.");
                }
            }).RequireAuthorization();
        }

        // SSRS responde algunas redirecciones con la URL absoluta del servidor
        // (http://localhost/ReportServer/...). Se convierten en rutas relativas
        // para que el navegador siga usando el proxy de esta aplicación.
        private sealed class TransformadorSsrs : HttpTransformer
        {
            private readonly string _servidor;

            public TransformadorSsrs(string servidor)
            {
                _servidor = servidor;
            }

            public override async ValueTask<bool> TransformResponseAsync(HttpContext httpContext, HttpResponseMessage? proxyResponse, CancellationToken cancellationToken)
            {
                var resultado = await base.TransformResponseAsync(httpContext, proxyResponse, cancellationToken);

                var location = httpContext.Response.Headers.Location.ToString();
                if (location.StartsWith(_servidor + "/", StringComparison.OrdinalIgnoreCase))
                {
                    httpContext.Response.Headers.Location = location.Substring(_servidor.Length);
                }

                return resultado;
            }
        }
    }
}
