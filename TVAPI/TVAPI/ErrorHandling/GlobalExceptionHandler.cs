using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TVAPI.Application.Common;

namespace TVAPI.ErrorHandling
{
    /// <summary>
    /// Yakalanmamış bütün hataların geçtiği tek nokta. Hatayı loglar ve istemciye her zaman aynı
    /// biçimde (ProblemDetails) cevap döner. İç ayrıntılar (SQL, sunucu adı, stack trace) cevaba yazılmaz,
    /// yalnızca loga yazılır; istemci cevaptaki traceId ile logdaki kaydı eşleştirebilir.
    /// </summary>
    internal sealed class GlobalExceptionHandler : IExceptionHandler
    {
        // İstemci cevabı beklemeden bağlantıyı kapattığında kullanılan, standart dışı ama yaygın durum kodu.
        private const int StatusClientClosedRequest = 499;

        private readonly IProblemDetailsService _problemDetailsService;
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<GlobalExceptionHandler> logger)
        {
            _problemDetailsService = problemDetailsService;
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            // İstemci vazgeçtiyse ortada bir arıza yoktur; cevap yazılacak kimse de yoktur.
            if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
            {
                _logger.LogInformation("İstek istemci tarafından iptal edildi: {Path}", httpContext.Request.Path);
                httpContext.Response.StatusCode = StatusClientClosedRequest;
                return true;
            }

            var problem = ToProblem(exception);

            _logger.LogError(
                exception,
                "İstek başarısız: {Method} {Path} -> {StatusCode} ({Title})",
                httpContext.Request.Method,
                httpContext.Request.Path,
                problem.Status,
                problem.Title);

            httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;

            return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails = problem,
            });
        }

        private static ProblemDetails ToProblem(Exception exception) => exception switch
        {
            DataSourceUnavailableException => new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "Veri kaynağına ulaşılamıyor",
                Detail = "Veritabanı cevap vermedi ya da bağlantı kurulamadı. İstemci elindeki son veriyi kullanmalıdır.",
            },
            InvalidSourceDataException => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Kaynak veri bozuk",
                Detail = "Veri kaynağındaki kayıt beklenen biçimde değil.",
            },
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Beklenmeyen hata",
                Detail = "İstek işlenirken beklenmeyen bir hata oluştu.",
            },
        };
    }
}
