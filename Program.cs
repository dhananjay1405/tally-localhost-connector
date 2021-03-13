using System;
using System.Threading.Tasks;
using System.Text;
using System.Net;
using System.Net.NetworkInformation;
using System.IO;
using System.Net.Http;

namespace tally_localhost_connector
{
    class Program
    {
        private static int port = 9001; // default port is 9001 (unless overrided by commandline arguments)
        private static string version = "1.0.0";
        static async Task Main(string[] args)
        {
            try
            {
                if (args.Length > 1)
                    ParseArgs(args);

                Console.WriteLine(string.Format("Tally Localhost Connector | Version: {0}", version));

                HttpListener httpListener = new HttpListener();
                httpListener.Prefixes.Add(string.Format("http://localhost:{0}/", port));
                httpListener.Start();
                Console.WriteLine(string.Format("Listener started on http://localhost:{0}", port));
                while (true)
                {
                    HttpListenerContext context = httpListener.GetContext();
                    HttpListenerRequest req = context.Request;
                    using (StreamReader sr = new StreamReader(req.InputStream))
                    {
                        string payload = sr.ReadToEnd();
                        HttpListenerResponse res = context.Response;
                        if (!IsTallyRunning())
                            res.StatusCode = 404;
                        else
                        {
                            string content = await FetchTallyData(payload);
                            res.AddHeader("Access-Control-Allow-Origin", "*");
                            res.StatusCode = 200;
                            byte[] buff = Encoding.UTF8.GetBytes(content);
                            res.ContentLength64 = buff.Length;
                            res.ContentType = "text/plain";
                            res.ContentEncoding = Encoding.UTF8;
                            res.OutputStream.Write(buff, 0, buff.Length);
                        }
                        res.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw ex;
            }

        }

        static async Task<string> FetchTallyData(string strReq)
        {
            try
            {
                HttpClient httpClient = new HttpClient();
                HttpContent payload = new StringContent(strReq, Encoding.Unicode);
                HttpResponseMessage resp = await httpClient.PostAsync("http://localhost:9000", payload);
                if (resp.StatusCode == HttpStatusCode.OK)
                    return await resp.Content.ReadAsStringAsync();
                else
                    return string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return string.Empty;
            }
        }

        static bool IsTallyRunning()
        {
            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] ipEndPoint = ipGlobalProperties.GetActiveTcpListeners();
            if (ipEndPoint == null)
                return false;

            for (int i = 0; i < ipEndPoint.Length; i++)
                if (ipEndPoint[i].Port == 9000)
                    return true;

            return false;
        }

        static void ParseArgs(string[] args)
        {
            if (args.Length == 2)
            {
                if (args[0] == "--port" || args[0] == "-port")
                {
                    bool parseResult = int.TryParse(args[1], out int _port);
                    if (parseResult)
                        port = _port;
                }
            }
        }
    }
}
