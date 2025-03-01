using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroCoreKit.Services.HttpService
{
    public interface IHttpService
    {
        Task<T> GetAsync<T>(string url, Dictionary<string, string>? headers = null);
        Task<T> PostAsync<T>(string url, object? data = null, Dictionary<string, string>? headers = null);
        Task<T> PutAsync<T>(string url, object? data = null, Dictionary<string, string>? headers = null);
        Task<T> DeleteAsync<T>(string url, Dictionary<string, string>? headers = null);
    }
}
