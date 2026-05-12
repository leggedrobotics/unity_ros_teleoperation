using System;
using System.Threading.Tasks;

namespace LookingGlass.Toolkit {
    public interface IHttpSender {
        public int TimeoutSeconds { get; set; }
        public string Send(HttpSenderMethod method, string url, string content);
        public Task<string> SendAsync(HttpSenderMethod method, string url, string content);
    }
}
