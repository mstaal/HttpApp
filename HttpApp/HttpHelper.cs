using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace HttpApp
{
    public static class HttpHelper
    {
        private static void AddHeaders(HttpClient client, XDocument headerDoc)
        {
            var headers = headerDoc.Root?.Value.Split(";").ToList().FindAll(it => it.Contains(":"));
            foreach (var header in headers ?? new List<string>())
            {
                var split = header.Trim().Split(":");
                client.DefaultRequestHeaders.Add(split[0], split[1]);
            }
        }

        internal static HttpClientHandler GetClientHandler(string certificate, string password)
        {
            var clientHandler = new HttpClientHandler
            {
                SslProtocols = SslProtocols.Tls12,
                ClientCertificateOptions = ClientCertificateOption.Manual
            };
            try
            {
                var certPfx = new X509Certificate2(certificate, password);
                clientHandler.ClientCertificates.Add(certPfx);
            }
            catch (CryptographicException e)
            {
                Console.WriteLine($"Error occured while trying to load certificate: {e.Message}");
            }
            clientHandler.ServerCertificateCustomValidationCallback +=
                (HttpRequestMessage req, X509Certificate2 cert2, X509Chain chain, SslPolicyErrors err) => true;
            return clientHandler;
        }

        internal static async Task<string> GetClientResponse(
            string endpoint,
            XDocument request,
            XDocument headers,
            HttpClientHandler handler)
        {
            using (var client = handler != null ? new HttpClient(handler, true) : new HttpClient())
            {
                AddHeaders(client, headers);

                var httpContent = new StringContent(request.ToString(), Encoding.UTF8, "application/xml");

                Console.WriteLine("Response from service:");
                Console.WriteLine("----------------------");

                string response = null;
                try
                {
                    response = await (await client.PostAsync(endpoint, httpContent)).Content.ReadAsStringAsync();
                }
                catch (Exception e)
                {
                    var message = $"Error while trying to get response: {e.Message}";
                    Console.WriteLine(message);
                }

                Console.WriteLine(response);
                Console.WriteLine("----------------------");
                Console.WriteLine("----------------------");

                return response;
            }
        }
    }
}
