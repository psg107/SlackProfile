using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SlackProfile
{
    /// <summary>
    /// https://stackoverflow.com/questions/19260060/retrying-httpclient-unsuccessful-requests
    /// </summary>
    public class RetryHandler : DelegatingHandler
    {
        private const int MaxRetries = 5;
        private const int DelayMilliseconds = 5000;

        public RetryHandler(HttpMessageHandler innerHandler)
            : base(innerHandler)
        { }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            HttpResponseMessage response = null;
            for (int i = 0; i < MaxRetries; i++)
            {
                response = await base.SendAsync(request, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    return response;
                }

                await Task.Delay(DelayMilliseconds);
            }

            return response;
        }
    }
}
