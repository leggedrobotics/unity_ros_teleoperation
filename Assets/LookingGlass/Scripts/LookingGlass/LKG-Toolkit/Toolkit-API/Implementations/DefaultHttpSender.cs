using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace LookingGlass.Toolkit {
    public class DefaultHttpSender : IHttpSender, IDisposable {
        private HttpClient client;

        public int TimeoutSeconds {
            get { return (int) client.Timeout.TotalSeconds; }
            set { client.Timeout = TimeSpan.FromSeconds(value); }
        }

        public DefaultHttpSender() {
            client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(5);
        }

        public void Dispose() {
            if (client != null) {
                client.Dispose();
                client = null;
            }
        }

        private HttpMethod GetInnerMethod(HttpSenderMethod method) {
            switch (method) {
                case HttpSenderMethod.Get:      return HttpMethod.Get;
                case HttpSenderMethod.Post:     return HttpMethod.Post;
                case HttpSenderMethod.Put:      return HttpMethod.Put;
                default:
                    throw new NotSupportedException("Unsupported HTTP method: " + method);
            }
        }

        public string Send(HttpSenderMethod method, string url, string content) {
            HttpRequestMessage request = new(GetInnerMethod(method), url);
            if (method == HttpSenderMethod.Post || method == HttpSenderMethod.Put)
                request.Content = new StringContent(content);

            HttpResponseMessage response = client.SendAsync(request).Result;
            return response.Content.ReadAsStringAsync().Result;
        }

        public async Task<string> SendAsync(HttpSenderMethod method, string url, string content) {
            HttpRequestMessage request = new(GetInnerMethod(method), url);
            if (method == HttpSenderMethod.Post || method == HttpSenderMethod.Put)
                request.Content = new StringContent(content);

            HttpResponseMessage response = await client.SendAsync(request);
            return await response.Content.ReadAsStringAsync();
        }
    }
}
