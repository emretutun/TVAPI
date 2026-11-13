using Microsoft.AspNetCore.Mvc.Filters;
using TVAPI.Application.Common;

namespace TVAPI.Filters
{
    /// <summary>
    /// Cevaba verinin tazeliğini anlatan iki başlık ekler:
    /// X-Data-Fetched-At: verinin kaynaktan alındığı an (ISO 8601).
    /// X-Data-Stale: kaynağa ulaşılamadığı için son başarılı veri döndüyse "true".
    /// Bilgi gövde yerine başlıkta durur; böylece cevap modelleri değişmez ve her uç için aynı şekilde çalışır.
    /// </summary>
    internal sealed class DataFreshnessHeaderFilter : IResultFilter
    {
        public const string FetchedAtHeader = "X-Data-Fetched-At";
        public const string StaleHeader = "X-Data-Stale";

        private readonly IDataFreshness _dataFreshness;

        public DataFreshnessHeaderFilter(IDataFreshness dataFreshness)
        {
            _dataFreshness = dataFreshness;
        }

        public void OnResultExecuting(ResultExecutingContext context)
        {
            // Veri okumayan uçlarda (örnek: /health) başlık eklenmez.
            if (_dataFreshness.OldestFetchedAt is not { } fetchedAt)
            {
                return;
            }

            var headers = context.HttpContext.Response.Headers;
            headers[FetchedAtHeader] = fetchedAt.ToString("O");
            headers[StaleHeader] = _dataFreshness.IsStale ? "true" : "false";
        }

        public void OnResultExecuted(ResultExecutedContext context)
        {
        }
    }
}
