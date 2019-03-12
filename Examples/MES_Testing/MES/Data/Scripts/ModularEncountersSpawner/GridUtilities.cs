using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.GameSystems;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using ModularEncountersSpawner;
using ModularEncountersSpawner.Configuration;
using ModularEncountersSpawner.Templates;
using ModularEncountersSpawner.Spawners;

namespace ModularEncountersSpawner{
	
	public static class GridUtilities{
		
		
		
		public static void ReplenishGridSystems(IMyCubeGrid cubeGrid, bool randomReplaced){
			
			var errorLogBuilder = new StringBuilder();
			errorLogBuilder.Append("Error: Something has gone wrong with Spawner Inventory Replenishment.").AppendLine();
			errorLogBuilder.Append("Please provide this data to Mod Author of Modular Encounters Spawner.").AppendLine();
			errorLogBuilder.Append(" - Start Replenish Of Inventories").AppendLine();
			
			try{
				
				errorLogBuilder.Append(" - Get Grid Terminal System").AppendLine();
				var gts = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(cubeGrid);
				var blockList = new List<IMyTerminalBlock>();
				gts.GetBlocksOfType<IMyTerminalBlock>(blockList);
				
				errorLogBuilder.Append(" - Iterate Through Blocks").AppendLine();
				foreach(var block in blockList){
					
					//Weapon Replenishing
					var weaponBlockDefinition = block.SlimBlock.BlockDefinition as MyWeaponBlockDefinition;
					
					if(weaponBlockDefinition != null){
						
						errorLogBuilder.Append("   - Identified Weapon: ").Append(weaponBlockDefinition.Id.ToString()).AppendLine();
						var weaponDefinition = MyDefinitionManager.Static.GetWeaponDefinition(weaponBlockDefinition.WeaponDefinitionId);
						
						if(weaponDefinition == null){
							
							errorLogBuilder.Append("   - Weapon Has No Weapon Definition. Skip.").AppendLine();
							return;
							
						}
						
						var ammoMagazineDefinitionList = new List<MyAmmoMagazineDefinition>();
						
						foreach(var ammoId in weaponDefinition.AmmoMagazinesId){
							
							var tempAmmoMag = MyDefinitionManager.Static.GetAmmoMagazineDefinition(ammoId);
							ammoMagazineDefinitionList.Add(tempAmmoMag);
							
						}
						
						if(ammoMagazineDefinitionList.Count == 0){
							
							errorLogBuilder.Append("   - Weapon Has No Ammo Magazine Definitions. Skip.").AppendLine();
							return;
							
						}
						
						var ammo = ammoMagazineDefinitionList[SpawnResources.rnd.Next(0, ammoMagazineDefinitionList.Count)];
						int totalAdds = 0;
						
						errorLogBuilder.Append("   - Add Ammo To Weapon.").AppendLine();
						while(totalAdds < 100){
							
							var definitionId = new MyDefinitionId(typeof(MyObjectBuilder_AmmoMagazine), ammo.Id.SubtypeId.ToString());
							var content = (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(definitionId);
							MyObjectBuilder_InventoryItem inventoryItem = new MyObjectBuilder_InventoryItem { Amount = 1, Content = content };
							
							if(block.GetInventory().CanItemsBeAdded(1, definitionId) == true){
									
								block.GetInventory().AddItems(1, inventoryItem.Content);
								totalAdds++;
									
							}else{
								
								totalAdds = 101;
								
							}
							
						}
						
						errorLogBuilder.Append("   - Ammo Adding Complete.").AppendLine();
						errorLogBuilder.Append("   - Assign Ammo To Gun Object.").AppendLine();
						
						var weaponBlock = block as IMyUserControllableGun;
						var gunObject = weaponBlock as IMyGunObject<Sandbox.Game.Weapons.MyDeviceBase>;
						
						if(gunObject != null){
							
							var switchAmmo = gunObject.GunBase.SwitchAmmoMagazineToNextAvailable();
							
						}
						
						errorLogBuilder.Append("   - Ammo Assign To Gun Object Complete.").AppendLine();
						
						var turretBlock = block as IMyLargeTurretBase;
						
						if(turretBlock != null && randomReplaced == true){
							
							errorLogBuilder.Append("   - Weapon Is Randomly Added Turret. Set Targeting.").AppendLine();
							
							try{
								
								turretBlock.SetValue<bool>("TargetSmallShips", true);
								turretBlock.SetValue<bool>("TargetLargeShips", true);
								turretBlock.SetValue<bool>("TargetStations", true);
								turretBlock.SetValue<bool>("TargetNeutrals", true);
								turretBlock.SetValue<bool>("TargetCharacters", true);
								
							}catch(Exception exc){
								
								errorLogBuilder.Append("   - Something Went Wrong With Random Turret Targeting...").AppendLine();
								
							}
							
						}
						
					}
					
					if(block as IMyReactor != null){
						
						errorLogBuilder.Append(" - Identified Reactor Block.").AppendLine();
						errorLogBuilder.Append("   - Filling Reactor With Uranium.").AppendLine();
						
						var powerDef = block.SlimBlock.BlockDefinition as MyPowerProducerDefinition;
						var totalFuelAdd = (MyFixedPoint)powerDef.MaxPowerOutput;
						
						var definitionId = new MyDefinitionId(typeof(MyObjectBuilder_Ingot), "Uranium");
						var content = (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(definitionId);
						MyObjectBuilder_InventoryItem inventoryItem = new MyObjectBuilder_InventoryItem { Amount = totalFuelAdd, Content = content };
						
						if(block.GetInventory().CanItemsBeAdded(totalFuelAdd, definitionId) == true){
								
							block.GetInventory().AddItems(totalFuelAdd, inventoryItem.Content);
							
						}
						
						errorLogBuilder.Append("   - Completed Reactor Filling.").AppendLine();
						
					}
					
					if(block as IMyParachute != null){
						
						errorLogBuilder.Append(" - Identified Parachute Block.").AppendLine();
						errorLogBuilder.Append("   - Filling Parachute With Canvas Components.").AppendLine();
						
						MyFixedPoint totalAdd = 5;
						
						if(cubeGrid.GridSizeEnum == MyCubeSize.Large){
							
							totalAdd *= 5;
							
						}
						
						var definitionId = new MyDefinitionId(typeof(MyObjectBuilder_Component), "Canvas");
						var content = (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(definitionId);
						MyObjectBuilder_InventoryItem inventoryItem = new MyObjectBuilder_InventoryItem { Amount = totalAdd, Content = content };
						
						if(block.GetInventory().CanItemsBeAdded(totalAdd, definitionId) == true){
								
							block.GetInventory().AddItems(totalAdd, inventoryItem.Content);
							
						}
						
						errorLogBuilder.Append("   - Completed Parachute Filling.").AppendLine();
						
					}

				}
				
			}catch(Exception exc){
				
				
				Logger.AddMsg(errorLogBuilder.ToString());
				
			}

		}
		
		public static bool FixUnfinishedBlock(IMyInventory inv, IMySlimBlock slimBlock, long owner){
			
			bool success = true;
			
			Dictionary<string, int> missingParts = new Dictionary<string, int>();
			slimBlock.GetMissingComponents(missingParts);
			
			if(missingParts.Keys.Count == 0){
				
				return success;
				
			}
			
			foreach(var part in missingParts.Keys.ToList()){
				
				MyDefinitionId defId = new MyDefinitionId(typeof(MyObjectBuilder_Component), part);
				var content = (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(defId);
				MyObjectBuilder_InventoryItem inventoryItem = new MyObjectBuilder_InventoryItem { Amount = 1, Content = content };
				
				while(missingParts[part] > 0){
					
					if(inv.CanItemsBeAdded(1, defId) == true){
							
					inv.AddItems(1, inventoryItem.Content);
					missingParts[part]--;
							
					}else{
						
						//Logger.AddMsg("Failed To Add Repair Component To Container", true);
						success = false;
						break;
						
					}
					
				}
				
				slimBlock.MoveItemsToConstructionStockpile(inv);
				slimBlock.IncreaseMountLevel(10000, owner, inv);

			}
			
			return success;
			
		}
		
	}
	
}