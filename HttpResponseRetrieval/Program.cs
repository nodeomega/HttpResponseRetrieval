using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace HttpResponseRetrieval
{
    public class OutResult
    {
        public string RedirectTo { get; set; }
        public HttpStatusCode ResponseCode { get; set; }
    }

    internal class Program
    {

        static void Main(string[] args)
        {
            string fileName = args.FirstOrDefault(x => x.ToLower().StartsWith("file="))?.Replace("file=", "") ?? "urls.txt";
            bool parseLinks = args.Any(x => x.ToLower().StartsWith("parselinks")),
                fileDeclared = args.Any(x => x.ToLower().StartsWith("file=")),
                urlDeclared = args.Any(x => x.ToLower().StartsWith("url="));

            string singleUrl = args.FirstOrDefault(x => x.ToLower().StartsWith("url="))?.Replace("url=", "");

            // shows help if either -help is called, or if file and url were both specified.
            if (args.Any(x => x.ToLower() == "-help") || fileDeclared && urlDeclared)
            {
                Console.WriteLine($"Usage: ");
                Console.WriteLine($"HttpResponseRetrieval [file=<filename.txt>|url=<url>] [parselinks]");
                Console.WriteLine($"- url: parses the given url.");
                Console.WriteLine($"- specifying url will ignore file.");
                Console.WriteLine(string.Empty);
                Console.WriteLine($"- file: parses the urls in the text file, one line at a time.");
                Console.WriteLine($"- if the url argument is declared, file will be ignored.");
                Console.WriteLine(string.Empty);
                Console.WriteLine($"- parselinks: if specified, will parse all 200 result urls and create text files");
                Console.WriteLine($"  with all of the anchor tag hrefs (ignoring in-page links, telephone, mail, and ftp)");
                Console.WriteLine($"  for each url.  This allows repeated use of this tool in order to parse links.");
                Console.WriteLine($"  Files are appended to, and not overwritten.  To reset, delete the generated text files.");
                Console.WriteLine(string.Empty);
                return;
            }

            if (!string.IsNullOrWhiteSpace(singleUrl))
            {
                OutResult test = GetHttpStatusCode(singleUrl, parseLinks);

                Console.WriteLine($"{singleUrl}: {(int)test.ResponseCode}");
                if (!string.IsNullOrWhiteSpace(test.RedirectTo))
                {
                    Console.WriteLine($"-> {test.RedirectTo}");
                }
                return;
            }

            var fileIn = new StreamReader(fileName);
            string url;
            //var urls = new List<string>();

            while ((url = fileIn.ReadLine()) != null)
            {
                OutResult test = GetHttpStatusCode(url, parseLinks);

                Console.WriteLine($"{url}: {(int)test.ResponseCode}");
                if (!string.IsNullOrWhiteSpace(test.RedirectTo))
                {
                    Console.WriteLine($"-> {test.RedirectTo}");
                }
            }

            //var urls = new[] {"http://phoenixiaastrology.com/kfjsdklfjdsfjl;kdsjlkdjf", "http://phoenixiaastrology.com", "https://healthypawspetinsurance.com/Frequent-Questions", "https://www.healthypawspetinsurance.com/blog/category/pet-heroes/" };

            //foreach (string url in urls)
            //{
            //}
        }

        private static OutResult GetHttpStatusCode(string url, bool parseLinks = false)
        {
            var result = new OutResult();

            HttpWebResponse response = null;

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.AllowAutoRedirect = false;

                response = (HttpWebResponse)request.GetResponse();

                // StreamReader sr = new StreamReader(response.GetResponseStream());
                // Console.Write(sr.ReadToEnd());

                result.ResponseCode = response.StatusCode;
                switch (result.ResponseCode)
                {
                    case HttpStatusCode.MovedPermanently:
                        result.RedirectTo = response.Headers["Location"];
                        break;
                    case HttpStatusCode.OK:
                        if (parseLinks)
                        {
                            Stream responseStream = response.GetResponseStream();
                            using (var reader = new StreamReader(responseStream, Encoding.UTF8))
                            {
                                string temp = reader.ReadToEnd();
                                const string links = @"<a ([^>]+)";
                                const string hyperLink = @"href=""([^<\s]+)*""";
                                MatchCollection linkReg = Regex.Matches(temp, links);
                                List<string> rawLinks =
                                (from Match l in linkReg
                                    where l.Groups.Count > 1
                                    select l.Groups[1].ToString().Trim()).ToList();

                                rawLinks = rawLinks.Distinct().ToList();

                                List<string> collectedLinks = new List<string>();

                                Console.WriteLine($"{rawLinks[0]} == {rawLinks[1]} : {rawLinks[0] == rawLinks[1]}");

                                foreach (string l in rawLinks)
                                {
                                    MatchCollection reg = Regex.Matches(l, hyperLink);
                                    //Console.WriteLine(reg.Count > 0
                                    //    ? $"hyperlinks found in next URL: -> {reg.Count}"
                                    //    : $"hyperlinks found in next URL: -> no matches found :(");
                                    if (reg.Count <= 0) continue;
                                    foreach (Match k in reg)
                                    {
                                        if (k.Groups.Count <= 1) continue;
                                        string thisUrl = k.Groups[1].ToString();
                                        if (thisUrl.StartsWith("http://") || thisUrl.StartsWith("https://"))
                                        {
                                            collectedLinks.Add($"{thisUrl}");
                                        }
                                        else if (thisUrl.StartsWith("#") || thisUrl.StartsWith("tel:") ||
                                                 thisUrl.StartsWith("mailto:") || thisUrl.StartsWith("ftp:"))
                                        {
                                            // do nothing.
                                        }
                                        else
                                        {
                                            if (url.EndsWith("/") && thisUrl.StartsWith("/"))
                                            {
                                                collectedLinks.Add($"{url}{thisUrl.Remove(0, 1)}");
                                            }
                                            else
                                            {
                                                collectedLinks.Add($"{url}{thisUrl}");
                                            }
                                        }
                                    }
                                }
                                collectedLinks = collectedLinks.Distinct().ToList();
                                using (
                                    var file =
                                        new StreamWriter(
                                            $"{url.Replace("http://", "").Replace("https://", "").Replace("/", "-").Replace("=", "-")}-urls.txt",
                                            true))
                                {
                                    //file.WriteLine($"{thisUrl}");
                                    collectedLinks.ForEach(x => file.WriteLine($"{x}"));
                                }
                            }
                        }
                        break;
                    case HttpStatusCode.Continue:
                        break;
                    case HttpStatusCode.SwitchingProtocols:
                        break;
                    case HttpStatusCode.Created:
                        break;
                    case HttpStatusCode.Accepted:
                        break;
                    case HttpStatusCode.NonAuthoritativeInformation:
                        break;
                    case HttpStatusCode.NoContent:
                        break;
                    case HttpStatusCode.ResetContent:
                        break;
                    case HttpStatusCode.PartialContent:
                        break;
                    case HttpStatusCode.MultipleChoices:
                        break;
                    case HttpStatusCode.Found:
                        break;
                    case HttpStatusCode.SeeOther:
                        break;
                    case HttpStatusCode.NotModified:
                        break;
                    case HttpStatusCode.UseProxy:
                        break;
                    case HttpStatusCode.Unused:
                        break;
                    case HttpStatusCode.TemporaryRedirect:
                        break;
                    case HttpStatusCode.BadRequest:
                        break;
                    case HttpStatusCode.Unauthorized:
                        break;
                    case HttpStatusCode.PaymentRequired:
                        break;
                    case HttpStatusCode.Forbidden:
                        break;
                    case HttpStatusCode.NotFound:
                        break;
                    case HttpStatusCode.MethodNotAllowed:
                        break;
                    case HttpStatusCode.NotAcceptable:
                        break;
                    case HttpStatusCode.ProxyAuthenticationRequired:
                        break;
                    case HttpStatusCode.RequestTimeout:
                        break;
                    case HttpStatusCode.Conflict:
                        break;
                    case HttpStatusCode.Gone:
                        break;
                    case HttpStatusCode.LengthRequired:
                        break;
                    case HttpStatusCode.PreconditionFailed:
                        break;
                    case HttpStatusCode.RequestEntityTooLarge:
                        break;
                    case HttpStatusCode.RequestUriTooLong:
                        break;
                    case HttpStatusCode.UnsupportedMediaType:
                        break;
                    case HttpStatusCode.RequestedRangeNotSatisfiable:
                        break;
                    case HttpStatusCode.ExpectationFailed:
                        break;
                    case HttpStatusCode.UpgradeRequired:
                        break;
                    case HttpStatusCode.InternalServerError:
                        break;
                    case HttpStatusCode.NotImplemented:
                        break;
                    case HttpStatusCode.BadGateway:
                        break;
                    case HttpStatusCode.ServiceUnavailable:
                        break;
                    case HttpStatusCode.GatewayTimeout:
                        break;
                    case HttpStatusCode.HttpVersionNotSupported:
                        break;
                    default:
                        break;
                }
            }
            catch (WebException e)
            {
                if (e.Status == WebExceptionStatus.ProtocolError)
                {
                    response = (HttpWebResponse)e.Response;
                    //Console.Write("Errorcode: {0}", (int)response.StatusCode);
                    result.ResponseCode = response.StatusCode;
                }
                else
                {
                    //Console.Write("Error: {0}", e.Status);
                    if (response != null) result.ResponseCode = response.StatusCode;
                }
            }
            finally
            {
                response?.Close();
            }

            return result;
        }
    }
}
