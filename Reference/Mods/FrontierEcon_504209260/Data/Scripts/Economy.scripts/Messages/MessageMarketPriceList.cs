﻿namespace Economy.scripts.Messages
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using EconConfig;
    using Economy.scripts.EconStructures;
    using Management;
    using ProtoBuf;
    using Sandbox.Common.ObjectBuilders.Definitions;
    using Sandbox.Definitions;
    using Sandbox.ModAPI;
    using VRage.Game;
    using VRage.Game.ObjectBuilders.Definitions;
    using VRage.ModAPI;
    using VRage.ObjectBuilders;

    [ProtoContract]
    public class MessageMarketPriceList : MessageBase
    {
        [ProtoMember(201)]
        public bool ShowOre;

        [ProtoMember(202)]
        public bool ShowIngot;

        [ProtoMember(203)]
        public bool ShowComponent;

        [ProtoMember(204)]
        public bool ShowAmmo;

        [ProtoMember(205)]
        public bool ShowTools;

        [ProtoMember(206)]
        public bool ShowGasses;

        [ProtoMember(207)]
        public string FindMarket;

        public static void SendMessage(bool showOre, bool showIngot, bool showComponent, bool showAmmo, bool showTools, bool showGasses, string findMarket)
        {
            ConnectionHelper.SendMessageToServer(new MessageMarketPriceList { ShowAmmo = showAmmo, ShowComponent = showComponent, ShowIngot = showIngot, ShowOre = showOre, ShowTools = showTools, ShowGasses = showGasses, FindMarket = findMarket });
        }

        public override void ProcessClient()
        {
            // never processed on client
        }

        public override void ProcessServer()
        {
            // update our own timestamp here
            AccountManager.UpdateLastSeen(SenderSteamId, SenderLanguage);
            EconomyScript.Instance.ServerLogger.WriteVerbose("Price List Request for from '{0}'", SenderSteamId);

            var player = MyAPIGateway.Players.FindPlayerBySteamId(SenderSteamId);
            var character = player.Character;
            if (character == null)
            {
                MessageClientTextMessage.SendMessage(SenderSteamId, "PRICELIST", "You are dead. You get market items values while dead.");
                return;
            }

            List<MarketStruct> markets;
            if (string.IsNullOrEmpty(FindMarket))
            {
                var position = ((IMyEntity)character).WorldMatrix.Translation;
                markets = MarketManager.FindMarketsFromLocation(position);
            }
            else
            {
                markets = MarketManager.FindMarketsFromName(FindMarket);
            }

            if (markets.Count == 0)
            {
                MessageClientTextMessage.SendMessage(SenderSteamId, "PRICELIST", "Sorry, your are not in range of any markets!");
                return;
            }

            // TODO: combine multiple markets to list best Buy and Sell prices that isn't blacklisted.


            var market = markets.FirstOrDefault();

            if (market == null) //hmmm looks like market name parameter checking was started but never done
            {
                MessageClientTextMessage.SendMessage(SenderSteamId, "PRICELIST", "That market does not exist.");
                return;  //as I understand it this would only trigger if no markets are defined? 
                //in which case should it read no markets exist?  but in that case
                // wont the count check above halt execution before this if statement???
                // remove these comments once read :)
            }

            string reply = null;

            MyAPIGateway.Parallel.StartBackground(delegate ()
            // Background processing occurs within this block.
            {

                try
                {
                    bool showAll = !ShowOre && !ShowIngot && !ShowComponent && !ShowAmmo && !ShowTools && !ShowGasses;

                    var orderedList = new Dictionary<MarketItemStruct, string>();
                    foreach (var marketItem in market.MarketItems)
                    {
                        if (marketItem.IsBlacklisted)
                            continue;

                        MyObjectBuilderType result;
                        if (MyObjectBuilderType.TryParse(marketItem.TypeId, out result))
                        {
                            var id = new MyDefinitionId(result, marketItem.SubtypeName);
                            var content = Support.ProducedType(id);

                            // Cannot check the Type of the item, without having to use MyObjectBuilderSerializer.CreateNewObject().

                            if (showAll ||
                                (ShowOre && content is MyObjectBuilder_Ore) ||
                                (ShowIngot && content is MyObjectBuilder_Ingot) ||
                                (ShowComponent && content is MyObjectBuilder_Component) ||
                                (ShowAmmo && content is MyObjectBuilder_AmmoMagazine) ||
                                (ShowTools && content is MyObjectBuilder_PhysicalGunObject) || // guns, welders, hand drills, grinders.
                                (ShowGasses && content is MyObjectBuilder_GasContainerObject) || // aka gas bottle.
                                (ShowGasses && content is MyObjectBuilder_GasProperties))
                            // Type check here allows mods that inherit from the same type to also appear in the lists.
                            {
                                var definition = MyDefinitionManager.Static.GetDefinition(marketItem.TypeId, marketItem.SubtypeName);
                                var name = definition == null ? marketItem.SubtypeName : definition.GetDisplayName();
                                orderedList.Add(marketItem, name);
                            }
                        }
                    }

                    orderedList = orderedList.OrderBy(kvp => kvp.Value).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                    var str = new SeTextBuilder();
                    str.AppendLine("Market: {0}\r\n", market.DisplayName);
                    str.AddLeftTrim(550, "Item");
                    str.AddRightText(650, "Buy at");
                    str.AddRightText(850, "Sell at");
                    str.AppendLine();

                    foreach (var kvp in orderedList)
                    {
                        decimal showBuy = kvp.Key.BuyPrice;
                        decimal showSell = kvp.Key.SellPrice;
                        if ((EconomyScript.Instance.ServerConfig.PriceScaling) && (market.MarketId == EconomyConsts.NpcMerchantId))
                        {  
                            showBuy = EconDataManager.PriceAdjust(kvp.Key.BuyPrice, kvp.Key.Quantity, PricingBias.Buy);
                            showSell = EconDataManager.PriceAdjust(kvp.Key.SellPrice, kvp.Key.Quantity, PricingBias.Sell);
                        }
                        // TODO: formatting of numbers, and currency name.
                        str.AddLeftTrim(550, kvp.Value);
                        str.AddRightText(650, showBuy.ToString("0.00", EconomyScript.ServerCulture));
                        str.AddRightText(850, showSell.ToString("0.00", EconomyScript.ServerCulture));
                        str.AppendLine();
                    }
                    reply = str.ToString();
                }
                catch (Exception ex)
                {
                    EconomyScript.Instance.ServerLogger.WriteException(ex);
                    MessageClientTextMessage.SendMessage(SenderSteamId, "PRICELIST", "Failed and died. Please contact the administrator.");
                }
            }, delegate ()
            // when the background processing is finished, this block will run foreground.
            {
                if (reply != null)
                {
                    try
                    {
                        MessageClientDialogMessage.SendMessage(SenderSteamId, "PRICELIST", " ", reply);
                    }
                    catch (Exception ex)
                    {
                        EconomyScript.Instance.ServerLogger.WriteException(ex);
                        MessageClientTextMessage.SendMessage(SenderSteamId, "PRICELIST", "Failed and died. Please contact the administrator.");
                    }
                }
            });
        }
    }
}
