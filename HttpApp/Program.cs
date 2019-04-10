using System;
using System.Collections.Generic;
using System.IO;
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
    class Program
    {
        static async Task Main(string[] args)
        {
            while (true)
            {
                var input = GetFileFromPrompt("a valid file");

                var document = XDocument.Load(input);
                var children = document.Root?.Elements()?.ToList();

                var headers = new XDocument(GetElementByName(children, "Header"));
                var request = new XDocument(GetElementByName(children, "Request").Elements().First());
                var endpoint = GetElementByName(children, "Endpoint")?.Value;
                var auth = new XDocument(GetElementByName(children, "Auth"));

                HttpClientHandler handler = null;
                if (auth?.Root?.Value != null)
                {
                    var login = auth.Root.Elements().ToList();
                    var certificate = GetElementByName(login, "Certificate")?.Value;
                    var password = GetElementByName(login, "Password")?.Value;

                    handler = GetClientHandler(certificate, password);
                }

                using (var client = handler != null ? new HttpClient(handler, true) : new HttpClient())
                {
                    AddHeaders(client, headers);

                    var httpContent = new StringContent(request.ToString(), Encoding.UTF8, "application/xml");

                    Console.WriteLine("Response from service:");
                    Console.WriteLine("----------------------");
                    var response = await client.PostAsync(endpoint, httpContent);

                    var responseString = await response.Content.ReadAsStringAsync();

                    Console.WriteLine(responseString);
                    Console.WriteLine("----------------------");
                    Console.WriteLine("----------------------");
                }

            }

            //if (args.Length > 1)
            //{
            //    try
            //    {
            //        await File.WriteAllTextAsync(args[1], responseString);
            //    }
            //    catch (Exception e)
            //    {
            //        Console.WriteLine("Could not complete writing file successfully");
            //        throw;
            //    }
            //}
        }


        private static XElement GetElementByName(List<XElement> elements, string name)
        {
            return elements.Find(it => it.Name == name);
        }

        private static void AddHeaders(HttpClient client, XDocument headerDoc)
        {
            var headers = headerDoc.Root?.Value.Split(";").ToList().FindAll(it => it.Contains(":"));
            foreach (var header in headers ?? new List<string>())
            {
                var split = header.Trim().Split(":");
                client.DefaultRequestHeaders.Add(split[0], split[1]);
            }
        }

        private static HttpClientHandler GetClientHandler(string certificate, string password)
        {
            X509Certificate2 certPfx;
            try
            {
                certPfx = new X509Certificate2(certificate, password);
            }
            catch (CryptographicException e)
            {
                Console.WriteLine($"Error occured while trying to load certificate: {e.Message}");
                return null;
            }
            var clientHandler = new HttpClientHandler();
            clientHandler.SslProtocols = SslProtocols.Tls12;
            clientHandler.ClientCertificateOptions = ClientCertificateOption.Manual;
            clientHandler.ClientCertificates.Add(certPfx);
            clientHandler.ServerCertificateCustomValidationCallback +=
                (HttpRequestMessage req, X509Certificate2 cert2, X509Chain chain, SslPolicyErrors err) => true;
            return clientHandler;
        }

        private static string GetFileFromPrompt(string typeOfSpecification)
        {
            var message = string.Format("Please specify {0}:", typeOfSpecification);
            Console.WriteLine(message);
            var line = Console.ReadLine();
            while (!File.Exists(line))
            {
                Console.WriteLine(message);
                line = Console.ReadLine();
            }

            return line;
        }
    }
}
