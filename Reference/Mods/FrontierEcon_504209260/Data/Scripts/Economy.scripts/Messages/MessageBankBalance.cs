﻿namespace Economy.scripts.Messages
{
    using System;
    using System.Linq;
    using EconConfig;
    using ProtoBuf;
    using Sandbox.ModAPI;

    [ProtoContract]
    public class MessageBankBalance : MessageBase
    {
        [ProtoMember(201)]
        public string UserName;

        public static void SendMessage(string userName)
        {
            ConnectionHelper.SendMessageToServer(new MessageBankBalance { UserName = userName });
        }

        public override void ProcessClient()
        {
            // never processed on client
        }

        public override void ProcessServer()
        {
            // update our own timestamp here
            AccountManager.UpdateLastSeen(SenderSteamId, SenderLanguage);

            EconomyScript.Instance.ServerLogger.WriteVerbose("Balance Request for '{0}' from '{1}'", UserName, SenderSteamId);

            if (string.IsNullOrEmpty(UserName)) //did we just type bal? show our balance  
            {
                // lets grab the current player data from our bankfile ready for next step
                // we look up our Steam Id/
                var account = EconomyScript.Instance.Data.Clients.FirstOrDefault(
                    a => a.SteamId == SenderSteamId);

                // check if we actually found it, add default if not
                if (account == null)
                {
                    EconomyScript.Instance.ServerLogger.WriteInfo("Creating new Bank Account for '{0}'", SenderDisplayName);
                    account = AccountManager.CreateNewDefaultAccount(SenderSteamId, SenderDisplayName, SenderLanguage);
                    EconomyScript.Instance.Data.Clients.Add(account);
                    EconomyScript.Instance.Data.CreditBalance -= account.BankBalance;
                }

                MessageClientTextMessage.SendMessage(SenderSteamId, "BALANCE",
                    "Your bank balance is {0:#,##0.######} {1}", account.BankBalance, EconomyScript.Instance.ServerConfig.CurrencyName);
            }
            else // if username is supplied, we want to know someone elses balance.
            {
                var player = MyAPIGateway.Players.FindPlayerBySteamId(SenderSteamId);
                if (player != null && player.IsAdmin()) // hold on there, are we an admin first?
                {
                    var account = EconomyScript.Instance.Data.Clients.FirstOrDefault(
                        a => a.NickName.Equals(UserName, StringComparison.InvariantCultureIgnoreCase));

                    string reply;
                    if (account == null)
                        reply = string.Format("Player '{0}' not found Balance: 0", UserName);
                    else
                        reply = string.Format("Player '{0}' Balance: {1}", account.NickName, account.BankBalance);

                    MessageClientTextMessage.SendMessage(SenderSteamId, "BALANCE", reply);
                }
            }
        }

    }
}
