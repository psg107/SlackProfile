using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;

namespace SlackProfile.Helpers
{
    public static class HttpContentHelper
    {
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item"></param>
        /// <returns></returns>
        public static JsonContent ToJsonContent<T>(this T item) where T : class
        {
            var jsonContent = JsonContent.Create<T>(item, mediaType: new MediaTypeHeaderValue("application/json")
            {
                CharSet = Encoding.UTF8.WebName
            });

            return jsonContent;
        }
    }
}
