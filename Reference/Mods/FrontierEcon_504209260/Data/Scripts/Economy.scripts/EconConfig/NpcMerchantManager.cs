﻿namespace Economy.scripts.EconConfig
{
    using System.Collections.Generic;
    using System.Linq;
    using Economy.scripts.EconStructures;

    /// <summary>
    /// checks for a valid NPC trader entry adds one if missing.
    /// </summary>
    public class NpcMerchantManager
    {
        /// <summary>
        /// Check we have our NPC banker ready.
        /// </summary>
        public static void VerifyAndCreate(EconDataStruct data)
        {
            // we look up our bank record based on our bogus NPC Steam Id/
            ClientAccountStruct myNpcAccount = data.Clients.FirstOrDefault(
                a => a.SteamId == EconomyConsts.NpcMerchantId);
            // Do it have an account already?
            if (myNpcAccount == null)
            {
                //nope, lets construct our bank record with a new balance
                myNpcAccount = AccountManager.CreateNewDefaultAccount(EconomyConsts.NpcMerchantId, EconomyScript.Instance.ServerConfig.NpcMerchantName, 0);

                //ok lets apply it
                data.Clients.Add(myNpcAccount);
                data.CreditBalance -= myNpcAccount.BankBalance;
                EconomyScript.Instance.ServerLogger.WriteInfo("Banker Account Created.");
            }
            else
            {
                EconomyScript.Instance.ServerLogger.WriteInfo("Banker Account Exists.");
            }

            // ShipSale was added recently, so this makes sure the list is created in existing data stores.
            if (data.ShipSale == null)
                data.ShipSale = new List<ShipSaleStruct>();
        }
    }
}
