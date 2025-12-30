using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace XrayClient.Core
{
    public class ConfigGenerator
    {
        private const string SubscriptionUrl = "https://griz5998.adnetworks.cl/E01X1Zu4KUtMbXGX/03838f85-00c9-468a-a890-e79a338b5e0b/#Griz";

        public async Task<string> FetchAndGenerateConfig(bool enableTun)
        {
            using HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
            
            string content = await client.GetStringAsync(SubscriptionUrl);
            var lines = content.Split(new[] { '\r', '\n', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            
            JObject? outbound = null;
            foreach (var line in lines)
            {
                 if (line.StartsWith("vless://"))
                 {
                     outbound = ParseVless(line);
                     if (outbound != null) break;
                 }
                 else if (line.StartsWith("vmess://"))
                 {
                     outbound = ParseVmess(line);
                     if (outbound != null) break;
                 }
            }

            if (outbound == null)
                throw new Exception("No valid config found in subscription.");

            string json = CreateConfigJson(outbound, enableTun);
            
            // Save to bin folder
            await File.WriteAllTextAsync(Path.Combine(ResourceManager.BinDir, "config.json"), json);
            
            return json;
        }

        private JObject? ParseVless(string uri)
        {
            try 
            {
                var uriObj = new Uri(uri);
                var uuid = uriObj.UserInfo;
                var host = uriObj.Host;
                var port = uriObj.Port;
                
                var queryDictionary = System.Web.HttpUtility.ParseQueryString(uriObj.Query);
                
                return new JObject
                {
                    ["protocol"] = "vless",
                    ["settings"] = new JObject
                    {
                        ["vnext"] = new JArray
                        {
                            new JObject
                            {
                                ["address"] = host,
                                ["port"] = port,
                                ["users"] = new JArray
                                {
                                    new JObject
                                    {
                                        ["id"] = uuid,
                                        ["encryption"] = queryDictionary["encryption"] ?? "none",
                                        ["flow"] = queryDictionary["flow"] ?? "" 
                                    }
                                }
                            }
                        }
                    },
                    ["streamSettings"] = new JObject
                    {
                        ["network"] = queryDictionary["type"] ?? "tcp",
                        ["security"] = queryDictionary["security"] ?? "none",
                        ["tlsSettings"] = new JObject
                        {
                            ["serverName"] = queryDictionary["sni"] ?? host,
                            ["allowInsecure"] = true
                        },
                        ["wsSettings"] = (queryDictionary["type"] == "ws") ? new JObject { ["path"] = queryDictionary["path"] ?? "/" } : null
                    }
                };
            }
            catch { return null; }
        }

         private JObject? ParseVmess(string uri)
        {
             try
             {
                 string base64 = uri.Substring(8);
                 // Handle padding
                 int mod4 = base64.Length % 4;
                 if (mod4 > 0) base64 += new string('=', 4 - mod4);
                 
                 string json = Encoding.UTF8.GetString(Convert.FromBase64String(base64));
                 JObject obj = JObject.Parse(json);
                 
                 return new JObject
                 {
                     ["protocol"] = "vmess",
                     ["settings"] = new JObject
                     {
                         ["vnext"] = new JArray
                         {
                             new JObject
                             {
                                 ["address"] = obj["add"],
                                 ["port"] = int.Parse(obj["port"].ToString()),
                                 ["users"] = new JArray
                                 {
                                     new JObject
                                     {
                                         ["id"] = obj["id"],
                                         ["alterId"] = int.Parse(obj["aid"]?.ToString() ?? "0"),
                                         ["security"] = obj["scy"] ?? "auto"
                                     }
                                 }
                             }
                         }
                     },
                     ["streamSettings"] = new JObject
                     {
                         ["network"] = obj["net"] ?? "tcp",
                         ["security"] = obj["tls"]?.ToString() == "tls" ? "tls" : "none",
                         ["tlsSettings"] = new JObject
                         {
                             ["serverName"] = obj["sni"] ?? obj["add"],
                             ["allowInsecure"] = true
                         },
                         ["wsSettings"] = (obj["net"]?.ToString() == "ws") ? new JObject { ["path"] = obj["path"] ?? "/" } : null
                     }
                 };
             }
             catch { return null; }
        }

        private string CreateConfigJson(JObject outbound, bool enableTun)
        {
            var config = new JObject
            {
                ["log"] = new JObject { ["loglevel"] = "warning" },
                ["inbounds"] = new JArray(),
                ["outbounds"] = new JArray { outbound, new JObject { ["protocol"] = "freedom", ["tag"] = "direct" } },
                ["routing"] = new JObject
                {
                    ["domainStrategy"] = "IPIfNonMatch",
                    ["rules"] = new JArray()
                }
            };

            var inbounds = (JArray)config["inbounds"];

            inbounds.Add(new JObject
            {
                ["tag"] = "socks",
                ["port"] = 10808,
                ["protocol"] = "socks",
                ["sniffing"] = new JObject { ["enabled"] = true, ["destOverride"] = new JArray { "http", "tls" } }
            });

            inbounds.Add(new JObject
            {
                ["tag"] = "http",
                ["port"] = 10809,
                ["protocol"] = "http"
            });

            if (enableTun)
            {
                inbounds.Add(new JObject
                {
                    ["tag"] = "tun-in",
                    ["protocol"] = "tun",
                    ["settings"] = new JObject
                    {
                        ["mtu"] = 1280
                    }
                });

                // Force everything to proxy except internal, with specific handling for tun
                // But simplified:
                var rules = (JArray)config["routing"]["rules"];
                rules.Add(new JObject
                {
                    ["type"] = "field",
                    ["inboundTag"] = new JArray { "tun-in" },
                    ["outboundTag"] = "proxy"
                });
                
                outbound["tag"] = "proxy";
            }
            else 
            {
                 outbound["tag"] = "proxy";
            }

            return config.ToString(Newtonsoft.Json.Formatting.Indented);
        }
    }
}
