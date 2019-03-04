﻿namespace Economy.scripts.EconStructures
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Serialization;
    using MissionStructures;

    [XmlType("EconData")]
    public class EconDataStruct
    {
        /// <summary>
        /// The represents the World's credit balance, for unaccounted credits and debits.
        /// Sum with all account balances, should be equal to 0.
        /// </summary>
        public decimal CreditBalance;

        //[Obsolete("Replaced with Clients")]
        public List<BankAccountStruct> Accounts;

        public List<ClientAccountStruct> Clients;

        public List<MarketStruct> Markets;

        public List<OrderBookStruct> OrderBook;

        public List<ShipSaleStruct> ShipSale;

        public List<MissionBaseStruct> Missions;
    }
}
