using System.Collections.Generic;
using System;
using System.Text.RegularExpressions;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Oxide.Plugins
{
    [Info ("VPN Block", "Calytic/Pingu", "1.0.0")]
    // Original Author: Calytic | Maintained by: Pingu
    class VPNBlock : CovalencePlugin
    {
        Dictionary<string, string> headers = new Dictionary<string, string> ();
        string unauthorizedMessage;
        bool debug = false;

        void Loaded()
        {
            LoadMessages();
            permission.RegisterPermission("vpnblock.canvpn", this);
            debug = GetConfig("Debug", false);
            unauthorizedMessage = GetMsg("Unauthorized");
        }

        protected override void LoadDefaultConfig()
        {
            Config ["Debug"] = false;
        }

        void LoadMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"Unauthorized", "Unauthorized.  ISP/VPN not permitted"},
                {"Is Banned", "{0} is trying to connect from proxy VPN/ISP {1}"},
            }, this);
        }

        bool IsAllowed(IPlayer player)
        {
            if (player.IsAdmin) return true;
            return false;
        }

        bool hasAccess(IPlayer player, string permissionname)
        {
            if (player.IsAdmin) return true;
            return permission.UserHasPermission(player.Id, permissionname);
        }

        void OnUserConnected(IPlayer player)
        {
            if (hasAccess(player, "vpnblock.canvpn")) 
            {
                return;
            }

            string ip = player.Address;
            string url = string.Empty;

            url = string.Format("https://check.getipintel.net/check.php?ip={0}&contact=gleboy1000@gmail.com&format=json", ip.Split(':')[0]);
            webrequest.Enqueue(url, string.Empty, (code, response) => HandleGetIPIntelResponse(player, url, ip, code, response), this);
        }
        
        void FailResponse(string service, int code, string response)
        {
            if (debug)
            {
                string message = string.Empty;
                if(code != 200)
                {
                    message = $"Response Code: {code}";
                }
                if(!string.IsNullOrEmpty(response))
                {
                    if(!string.IsNullOrEmpty(message))
                    {
                        message += "\n";
                    }
                    message += $"Response: {response}";
                }
                PrintError ($"Service ({service}) error: {message}");
            }
            else
            {
                PrintError ($"Service ({service}) temporarily offline");
            }
        }

        void HandleGetIPIntelResponse(IPlayer player, string url, string ip, int code, string response)
        {
            if (code != 200 || string.IsNullOrEmpty(response)) 
            {
                FailResponse("GetIPIntel", code, response);
            } 
            else 
            {
                JObject json;
                try 
                {
                    json = JObject.Parse(response);
                } 
                catch (JsonReaderException e) 
                {
                    LogWarning("Error parsing URL response: {0}", url);
                    return;
                }

                if (debug) 
                {
                    Log(response);
                }

                if (json["status"] != "success") 
                {
                    LogWarning("GetIPIntel status");
                    return;
                }

                if (json["result"] >= 0.8)
                {
                    player.Kick(unauthorizedMessage);
                    LogWarning(GetMsg("Is Banned"), $"{player.Name} ({player.Id}/{ip})", playerIsp);
                }   
            }
        }

        T GetConfig<T> (string name, string name2, T defaultValue)
        {
            if (Config [name, name2] == null) {
                return defaultValue;
            }

            return (T)Convert.ChangeType(Config [name, name2], typeof (T));
        }

        T GetConfig<T> (string name, T defaultValue)
        {
            if (Config [name] == null)
                return defaultValue;

            return (T)Convert.ChangeType(Config [name], typeof (T));
        }

        string GetMsg(string key, object userID = null)
        {
            return lang.GetMessage(key, this, userID == null ? null : userID.ToString ());
        }
    }
}
