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

	public static class NPCWatcher{
		
		//NPC Faction and Founder Data
		public static List<long> NPCFactionFounders = new List<long>();
		public static List<string> NPCFactionTags = new List<string>();
		public static Dictionary<long, string> NPCFactionFounderToTag = new Dictionary<long, string>();
		public static Dictionary<string, long> NPCFactionTagToFounder = new Dictionary<string, long>();
		
		//NPC Parameter GUIDs
		public static Guid GuidStartCoords = new Guid("CC27ADFD-A121-477A-94B1-FB1B4E2E3046");
		public static Guid GuidEndCoords = new Guid("513F6C90-E0D9-4A8F-972E-09757FE32C19");
		public static Guid GuidSpawnType = new Guid("C9D22735-C76B-4DB4-AFB5-51D1E1516A05");
		public static Guid GuidCleanupTimer = new Guid("8E5E70C9-9C7B-429A-9D5D-036465948175");
		public static Guid GuidIgnoreCleanup = new Guid("7ADDED32-4069-4C52-891C-25F52478B2EB");
		public static Guid GuidWeaponsReplaced = new Guid("C0CD2D13-AA56-466E-BA44-D840658A772B");
		
		//Pending Boss Encounters
		public static List<BossEncounter> BossEncounters = new List<BossEncounter>();
		
		//Pending NPC Spawns and Deletions
		public static List<ActiveNPC> PendingNPCs = new List<ActiveNPC>();
		
		//Active Ships and Stations
		public static Dictionary<IMyCubeGrid, ActiveNPC> ActiveNPCs = new Dictionary<IMyCubeGrid, ActiveNPC>();
		
		//Spawned Voxels
		public static Dictionary<string, IMyEntity> SpawnedVoxels = new Dictionary<string, IMyEntity>();

		//Watcher Timers
		public static int NpcDistanceCheckTimer = 1;
		public static int NpcOwnershipCheckTimer = 10;
		public static int NpcCleanupCheckTimer = 60;
		public static int NpcBlacklistCheckTimer = 5;
		public static int NpcBossSignalCheckTimer = 10;
		public static int SpawnedVoxelCheckTimer = 900;
		
		//Active Boss Encounter
		public static Vector3D bossCoords = new Vector3D(0,0,0);
		public static IMyGps bossGps = null;
		
		public static bool ActiveNpcTypeLimitReachedForArea(string spawnType, Vector3D checkArea, int maxCount, double areaDistance){
			
			var count = 0;
			
			foreach(var cubeGrid in ActiveNPCs.Keys.ToList()){
				
				if(cubeGrid == null || MyAPIGateway.Entities.Exist(cubeGrid as IMyEntity) == false){
					
					continue;
					
				}
				
				if(ActiveNPCs.ContainsKey(cubeGrid) == false){
					
					continue;
					
				}
				if(Vector3D.
				Distance(cubeGrid.GetPosition(), checkArea) < areaDistance && ActiveNPCs[cubeGrid].SpawnType == spawnType){
					
					count++;
					
				}
				
			}
			
			if(count >= maxCount){
				
				return true;
				
			}
			
			return false;
			
		}
		
		public static void ActiveNpcMonitor(){
			
			NpcDistanceCheckTimer--;
			NpcOwnershipCheckTimer--;
			NpcCleanupCheckTimer--;
			SpawnedVoxelCheckTimer--;
			
			if(NpcBlacklistCheckTimer >= 0){
				
				NpcBlacklistCheckTimer--;
				
			}
			
			if(SpawnedVoxelCheckTimer <= 0){
				
				SpawnedVoxelCheckTimer = Settings.General.SpawnedVoxelCheckTimerTrigger;
				bool listModified = false;
				SpawnResources.RefreshEntityLists();
				
				foreach(var voxelId in SpawnedVoxels.Keys.ToList()){
					
					if(SpawnedVoxels[voxelId] == null || MyAPIGateway.Entities.Exist(SpawnedVoxels[voxelId]) == false){
						
						listModified = true;
						SpawnedVoxels.Remove(voxelId);
						continue;
						
					}
					
					bool closeGrid = false;
					
					foreach(var entity in SpawnResources.EntityList){
						
						if(entity as IMyCubeGrid == null && entity as IMyCharacter == null){
							
							continue;
							
						}
						
						if(Vector3D.Distance(entity.GetPosition(), SpawnedVoxels[voxelId].GetPosition()) < Settings.General.SpawnedVoxelMinimumGridDistance){
							
							closeGrid = true;
							break;
							
						}
						
					}
					
					if(closeGrid == true){
						
						continue;
						
					}
					
					Logger.AddMsg("Removed Voxels Spawned From NPC At Coords " + SpawnedVoxels[voxelId].GetPosition().ToString() + ". No Grids Within Range.");
					SpawnedVoxels[voxelId].Delete();
					SpawnedVoxels.Remove(voxelId);
					listModified = true;
					
				}
				
				if(listModified == true){
					
					var voxelIdList = new List<string>(SpawnedVoxels.Keys.ToList());
					string[] voxelIdArray = voxelIdList.ToArray();
					MyAPIGateway.Utilities.SetVariable<string[]>("MES-SpawnedVoxels", voxelIdArray);
					
				}
				
			}

			var grids = new List<IMyCubeGrid>(ActiveNPCs.Keys.ToList());
			
			foreach(var cubeGrid in grids){
				
				if(ActiveNPCs.ContainsKey(cubeGrid) == false){
						
					continue;
					
				}
				
				if(cubeGrid == null){
					
					if(ActiveNPCs.ContainsKey(cubeGrid) == true){
						
						ActiveNPCs.Remove(cubeGrid);
						
					}
					
					continue;
					
				}
				
				var gridEntity = cubeGrid as IMyEntity;
				
				if(MyAPIGateway.Entities.Exist(gridEntity) == false){
					
					if(ActiveNPCs.ContainsKey(cubeGrid) == true){
						
						ActiveNPCs.Remove(cubeGrid);
						
					}
					
					continue;
					
				}

				//NPC Ownership Check
				if(NpcOwnershipCheckTimer <= 0){
					
					if(NpcOwnershipCheck(cubeGrid) == false){
						
						ActiveNPCs[cubeGrid].FullyNPCOwned = false;
						ActiveNPCs.Remove(cubeGrid);
						RemoveGUIDs(cubeGrid);
						continue;
						
					}
					
					//Keen AI Handler
					if(ActiveNPCs[cubeGrid].KeenBehaviorCheck == false){
						
						ActiveNPCs[cubeGrid].KeenBehaviorCheck = true;
						
						if(string.IsNullOrEmpty(ActiveNPCs[cubeGrid].KeenAiName) == false){
							
							//TODO: Attach AI Here.
							if(string.IsNullOrEmpty(cubeGrid.Name) == true){
								
								MyVisualScriptLogicProvider.SetName(cubeGrid.EntityId, cubeGrid.EntityId.ToString());
								
							}
							
							MyVisualScriptLogicProvider.SetDroneBehaviourFull(cubeGrid.EntityId.ToString(), ActiveNPCs[cubeGrid].KeenAiName, true, false, null, false, null, 10, ActiveNPCs[cubeGrid].KeenAiTriggerDistance);
							
						}
						
					}
					
					//Ammo Fill Check
					if(ActiveNPCs[cubeGrid].ReplenishedSystems == false){
						
						ActiveNPCs[cubeGrid].ReplenishedSystems = true;
						GridUtilities.ReplenishGridSystems(cubeGrid, ActiveNPCs[cubeGrid].ReplacedWeapons);
						
					}
					
					if(ActiveNPCs[cubeGrid].SpawnType == "PlanetaryCargoShip"){
						
						for(int i = ActiveNPCs[cubeGrid].HydrogenTanks.Count - 1; i >= 0; i--){
							
							var tank = ActiveNPCs[cubeGrid].HydrogenTanks[i];
							
							if(tank == null){
								
								ActiveNPCs[cubeGrid].HydrogenTanks.RemoveAt(i);
								continue;
								
							}
							
							if(tank.IsFunctional == false || tank.IsWorking == false || tank.FilledRatio > 0.5){
								
								continue;
								
							}
							
							var ob = tank.SlimBlock.GetObjectBuilder(true);
							var obj = ob.Clone();

							cubeGrid.RemoveBlock(tank.SlimBlock);
							
							var newtank = obj as MyObjectBuilder_GasTank;
							newtank.FilledRatio = 1;
							newtank.EntityId = 0;
							var newblock = cubeGrid.AddBlock(newtank, true);
							ActiveNPCs[cubeGrid].HydrogenTanks.Add(newblock.FatBlock as IMyGasTank);
							ActiveNPCs[cubeGrid].HydrogenTanks.RemoveAt(i);
							continue;
							
						}
						
						for(int i = ActiveNPCs[cubeGrid].GasGenerators.Count - 1; i >= 0; i--){
							
							var generator = ActiveNPCs[cubeGrid].GasGenerators[i];
							
							if(generator == null){
								
								ActiveNPCs[cubeGrid].HydrogenTanks.RemoveAt(i);
								continue;
								
							}
							
							if(generator.IsFunctional == false || generator.IsWorking == false || (float)generator.GetInventory(0).CurrentVolume > (float)generator.GetInventory(0).MaxVolume / 2){
								
								continue;
								
							}
							
							var invToFill = generator.GetInventory(0).MaxVolume - generator.GetInventory(0).CurrentVolume;
							invToFill *= 1000;
							invToFill -= 10;
							MyDefinitionId defId = new MyDefinitionId(typeof(MyObjectBuilder_Ore), "Ice");
							var content = (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(defId);
							MyObjectBuilder_InventoryItem inventoryItem = new MyObjectBuilder_InventoryItem {Amount = invToFill, Content = content};
							generator.GetInventory(0).AddItems(invToFill, inventoryItem.Content);
							
						}
						
					}
					
				}
				
				//NPC Blacklist Check
				if(NpcBlacklistCheckTimer == 0){
					
					var blacklistNames = new List<string>(Settings.General.NpcGridNameBlacklist.ToList());
					
					if(blacklistNames.Contains(cubeGrid.CustomName) == true){
						
						Logger.AddMsg("Blacklisted NPC Ship Found and Removed: " + cubeGrid.CustomName);
						ActiveNPCs.Remove(cubeGrid);
						DeleteGrid(cubeGrid);
						continue;
						
					}
					
					
					
				}
				
			}
			
			//NPC Distance Check
			if(NpcDistanceCheckTimer <= 0){
				
				DistanceChecker();
				
			}
			
			if(NpcDistanceCheckTimer <= 0){
				
				NpcDistanceCheckTimer = Settings.General.NpcDistanceCheckTimerTrigger;
				
			}
			
			if(NpcOwnershipCheckTimer <= 0){
				
				NpcOwnershipCheckTimer = Settings.General.NpcOwnershipCheckTimerTrigger;
				
			}
			
			if(NpcBlacklistCheckTimer == 0){
				
				Cleanup.CleanupProcess(true);
				
			}
			
			if(NpcCleanupCheckTimer <= 0){
				
				Logger.AddMsg("Running Cleanup", true);
				NpcCleanupCheckTimer = Settings.General.NpcCleanupCheckTimerTrigger;
				Cleanup.CleanupProcess();
				
			}
			
		}
		
		public static void BossSignalWatcher(){
			
			if(BossEncounters.Count == 0){
				
				return;
				
			}
			
			foreach(var player in MES_SessionCore.PlayerList){
				
				if(player.IsBot == true || player.Character == null){
					
					continue;
					
				}
				
				for(int i = BossEncounters.Count - 1; i >= 0; i--){
				
					if(BossEncounters[i].PlayersInEncounter.Contains(player.IdentityId) == true){
						
						if(Vector3D.Distance(player.GetPosition(), BossEncounters[i].Position) < Settings.BossEncounters.TriggerDistance){
							
							BossEncounters[i].SpawnAttempts++;
							Logger.AddMsg("Player " + player.DisplayName + " Is Within Signal Distance Of Boss Encounter. Attempting Spawn.");
							SpawnResources.RefreshEntityLists();
							
							if(BossEncounterSpawner.SpawnBossEncounter(BossEncounters[i]) == true || BossEncounters[i].SpawnAttempts > 5){
								
								Logger.AddMsg("Removing Boss Encounter GPS");
								BossEncounterSpawner.RemoveGPSFromEncounter(BossEncounters[i]);
								BossEncounters.RemoveAt(i);
								continue;
								
							}
							
						}
						
					}
				
				}
				
			}
			
			for(int i = BossEncounters.Count - 1; i >= 0; i--){
				
				BossEncounters[i].Timer--;
				
				if(BossEncounters[i].Timer <= 0){
					
					Logger.AddMsg("Boss Encounter Timer Expired. Removing GPS.");
					BossEncounterSpawner.RemoveGPSFromEncounter(BossEncounters[i]);
					BossEncounters.RemoveAt(i);
					continue;
					
				}
				
			}

		}
					
		public static void DeleteGrid(IMyCubeGrid cubeGrid){
			
			if(cubeGrid == null){
				
				return;
				
			}
			
			Logger.AddMsg("Despawning Grid With ID: " + cubeGrid.EntityId.ToString());
			string deleteTurret = "";
			
			try{
				
				var gts = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(cubeGrid);
				var blockList = new List<IMyLargeTurretBase>();
				gts.GetBlocksOfType<IMyLargeTurretBase>(blockList);
				
				foreach(var block in blockList){
					
					deleteTurret = block.SlimBlock.BlockDefinition.Id.ToString();
					cubeGrid.RemoveBlock(block.SlimBlock);
					
				}
				
			}catch(Exception exc){
				
				Logger.AddMsg("Failed To Remove Turret At Despawn: " + deleteTurret);
				return;
				
			}
			
			try{
								
				var gridGroups = MyAPIGateway.GridGroups.GetGroup(cubeGrid, GridLinkTypeEnum.Physical);
				foreach(var grid in gridGroups){
				
					grid.Close();
				
				}
				
				if(cubeGrid != null){
					
					cubeGrid.Close();
					
				}
				
			}catch(Exception exc){
				
				Logger.AddMsg("Failed To Despawn Grid With ID: " + cubeGrid.EntityId.ToString());
				Logger.AddMsg(exc.ToString());
				
			}

		}
		
		public static void DistanceChecker(){
			
			var grids = new List<IMyCubeGrid>(ActiveNPCs.Keys.ToList());
			
			foreach(var cubeGrid in grids){
				
				if(ActiveNPCs.ContainsKey(cubeGrid) == false){
					
					continue;
					
				}
				
				if(cubeGrid == null || MyAPIGateway.Entities.Exist(cubeGrid) == false){
					
					ActiveNPCs.Remove(cubeGrid);
					continue;
					
				}
				
				if(ActiveNPCs[cubeGrid].FixTurrets == false && ActiveNPCs[cubeGrid].SpawnType != "OtherNPCs"){
				
					ActiveNPCs[cubeGrid].FixTurrets = true;
					
					try{

						var gts = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(cubeGrid);
						var blockList = new List<IMyLargeTurretBase>();
						gts.GetBlocksOfType<IMyLargeTurretBase>(blockList);
						
						var doorList = new List<IMyDoor>();
						gts.GetBlocksOfType<IMyDoor>(doorList);
						
						foreach(var turret in blockList){
							
							if(turret.CubeGrid.EntityId != cubeGrid.EntityId){
								
								continue;
								
							}
							
							var blockColor = turret.SlimBlock.ColorMaskHSV;
							cubeGrid.ColorBlocks(turret.Min, turret.Min, new Vector3(42, 41, 40));
							turret.SlimBlock.UpdateVisual();
							cubeGrid.ColorBlocks(turret.Min, turret.Min, blockColor);
							turret.SlimBlock.UpdateVisual();
							
							
						}
						
						foreach(var door in doorList){
							
							if(door.CubeGrid.EntityId != cubeGrid.EntityId){
								
								continue;
								
							}
							
							var blockColor = door.SlimBlock.ColorMaskHSV;
							cubeGrid.ColorBlocks(door.Min, door.Min, new Vector3(42, 41, 40));
							door.SlimBlock.UpdateVisual();
							cubeGrid.ColorBlocks(door.Min, door.Min, blockColor);
							door.SlimBlock.UpdateVisual();
							
						}
						
					}catch(Exception exc){
						
						Logger.AddMsg("Unexpected exception while applying subpart fix script");
						
					}
					
				}
				
				//Space / Lunar Cargo Ships
				if(ActiveNPCs[cubeGrid].SpawnType == "SpaceCargoShip" || ActiveNPCs[cubeGrid].SpawnType == "LunarCargoShip"){
					
					try{
						
						if(Vector3D.Distance(cubeGrid.GetPosition(), ActiveNPCs[cubeGrid].EndCoords) < Settings.SpaceCargoShips.DespawnDistanceFromEndPath == ActiveNPCs[cubeGrid].FlagForDespawn == false){
							
							ActiveNPCs[cubeGrid].FlagForDespawn = true;
							
						}
						
						if(ActiveNPCs[cubeGrid].FlagForDespawn == true){
							
							var player = SpawnResources.GetNearestPlayer(cubeGrid.GetPosition());
							
							if(player == null || Vector3D.Distance(cubeGrid.GetPosition(), player.GetPosition()) > Settings.SpaceCargoShips.DespawnDistanceFromPlayer){
								
								Logger.AddMsg("NPC Cargo Ship " + cubeGrid.CustomName + " Has Reached End Of Travel Path And Has Been Despawned.");
								ActiveNPCs.Remove(cubeGrid);
								DeleteGrid(cubeGrid);
								continue;
								
							}
							
						}
						
					}catch(Exception exc){
						
						Logger.AddMsg("Unexpected exception in Space Cargo Ship Distance Checker");
						
					}
				
				}
				
				//Atmo Cargo Ships
				if(ActiveNPCs[cubeGrid].SpawnType == "PlanetaryCargoShip"){
					
					try{
						
						bool skip = ActiveNPCs[cubeGrid].FlagForDespawn;
						
						if(ActiveNPCs[cubeGrid].Planet == null){
							
							Logger.AddMsg("Planet For Planetary Cargo Ship " + cubeGrid.CustomName + " / " + cubeGrid.EntityId + " No Longer Exists. The NPC Ship Will Be Despawned.");
							ActiveNPCs[cubeGrid].FlagForDespawn = true; 
							skip = true;
							
						}
						
						if(ActiveNPCs[cubeGrid].RemoteControl == null && skip == false){
							
							Logger.AddMsg("Planetary Cargo Ship " + cubeGrid.CustomName + " Remote Control Damaged, Missing, Or Inactive. Ship Now Identified As \"Other\" NPC.");
							ActiveNPCs[cubeGrid].SpawnType = "Other";
							
							if(cubeGrid.Storage != null){
								
								cubeGrid.Storage[GuidSpawnType] = "Other";
								
							}
							
							continue;
							
						}
						
						if(ActiveNPCs[cubeGrid].RemoteControl != null && skip == false){
							
							if(ActiveNPCs[cubeGrid].RemoteControl.IsFunctional == false || ActiveNPCs[cubeGrid].RemoteControl.IsAutoPilotEnabled == false){
								
								Logger.AddMsg("Planetary Cargo Ship " + cubeGrid.CustomName + " Remote Control Damaged, Missing, Or Inactive. Ship Now Identified As \"Other\" NPC.");
								ActiveNPCs[cubeGrid].SpawnType = "Other";
								
								if(cubeGrid.Storage != null){
									
									cubeGrid.Storage[GuidSpawnType] = "Other";
									
								}
								
								continue;
								
							}

						}
						
						if(skip == false){
							
							double elevation = SpawnResources.GetDistanceFromSurface(cubeGrid.PositionComp.WorldAABB.Center, ActiveNPCs[cubeGrid].Planet);
							//var getElevation = ActiveNPCs[cubeGrid].RemoteControl.TryGetPlanetElevation(Sandbox.ModAPI.Ingame.MyPlanetElevation.Surface, out elevation);
							
							if(elevation > Settings.PlanetaryCargoShips.DespawnAltitude && skip == false){
								
								Logger.AddMsg("Planetary Cargo Ship " + cubeGrid.CustomName + " Has Ascended Too High From Its Path And Will Be Despawned.");
								ActiveNPCs[cubeGrid].FlagForDespawn = true; 
								skip = true;
								
							}
							
							if(elevation < Settings.PlanetaryCargoShips.MinPathAltitude && skip == false/* && getElevation == true*/){
								
								Logger.AddMsg("Planetary Cargo Ship " + cubeGrid.CustomName + " Altitude Lower Than Allowed Threshold. Ship Now Identified As \"Other\" NPC.");
								ActiveNPCs[cubeGrid].SpawnType = "Other";
								
								if(cubeGrid.Storage != null){
									
									cubeGrid.Storage[GuidSpawnType] = "Other";
									
								}
								
								continue;
								
							}

						}
						
						var planetEntity = ActiveNPCs[cubeGrid].Planet as IMyEntity;
						var shipUpDir = Vector3D.Normalize(cubeGrid.GetPosition() - planetEntity.GetPosition());
						var coreDist = Vector3D.Distance(ActiveNPCs[cubeGrid].EndCoords, planetEntity.GetPosition());
						var pathCheckCoords = shipUpDir * coreDist + planetEntity.GetPosition();
						
						if(Vector3D.Distance(pathCheckCoords, ActiveNPCs[cubeGrid].EndCoords) < Settings.PlanetaryCargoShips.DespawnDistanceFromEndPath && skip == false){
							
							Logger.AddMsg("Planetary Cargo Ship " + cubeGrid.CustomName + " Has Reached End Of Path And Will Be Despawned.");
							ActiveNPCs[cubeGrid].FlagForDespawn = true; 
							skip = true;
							
						}
						
						
						
						if(ActiveNPCs[cubeGrid].FlagForDespawn == true){
							
							var player = SpawnResources.GetNearestPlayer(cubeGrid.GetPosition());
							
							if(player == null || Vector3D.Distance(cubeGrid.GetPosition(), player.GetPosition()) > Settings.PlanetaryCargoShips.DespawnDistanceFromPlayer){
								
								Logger.AddMsg("NPC Cargo Ship " + cubeGrid.CustomName + " Has Been Despawned.");
								ActiveNPCs.Remove(cubeGrid);
								DeleteGrid(cubeGrid);
								continue;
								
							}
							
						}

						
					}catch(Exception exc){
						
						Logger.AddMsg("Unexpected exception in Planetary Cargo Ship Distance Checker");
						
					}
					
				}
				
				//Random Encounters
				if(ActiveNPCs[cubeGrid].SpawnType == "RandomEncounter"){
					
					try{
						
						if(ActiveNPCs[cubeGrid].FlagForDespawn == true){
						
							var player = SpawnResources.GetNearestPlayer(cubeGrid.GetPosition());
							
							if(player == null || Vector3D.Distance(cubeGrid.GetPosition(), player.GetPosition()) > Settings.RandomEncounters.DespawnDistanceFromPlayer){
								
								Logger.AddMsg("NPC Random Encounter " + cubeGrid.CustomName + " Has Been Despawned.");
								ActiveNPCs.Remove(cubeGrid);
								DeleteGrid(cubeGrid);
								continue;
								
							}
							
						}
						
					}catch(Exception exc){
						
						Logger.AddMsg("Unexpected exception in Random Encounters Distance Checker");
						
					}
					
				}
				
				//Boss Encounters
				if(ActiveNPCs[cubeGrid].SpawnType == "BossEncounter"){
					
					try{
						
						if(ActiveNPCs[cubeGrid].FlagForDespawn == true){
							
							var player = SpawnResources.GetNearestPlayer(cubeGrid.GetPosition());
							
							if(player == null || Vector3D.Distance(cubeGrid.GetPosition(), player.GetPosition()) > Settings.BossEncounters.DespawnDistanceFromPlayer){
								
								Logger.AddMsg("NPC Boss Encounter " + cubeGrid.CustomName + " Has Been Despawned.");
								ActiveNPCs.Remove(cubeGrid);
								DeleteGrid(cubeGrid);
								continue;
								
							}
							
						}
						
					}catch(Exception exc){
						
						Logger.AddMsg("Unexpected exception in Boss Encounters Distance Checker");
						
					}
					
				}
				
				//Planetary Installations
				if(ActiveNPCs[cubeGrid].SpawnType == "PlanetaryInstallation"){
					
					try{
						
						if(ActiveNPCs[cubeGrid].FlagForDespawn == true){
							
							var player = SpawnResources.GetNearestPlayer(cubeGrid.GetPosition());
							
							if(player == null || Vector3D.Distance(cubeGrid.GetPosition(), player.GetPosition()) > Settings.PlanetaryInstallations.DespawnDistanceFromPlayer){
								
								Logger.AddMsg("NPC Planetary Installation " + cubeGrid.CustomName + " Has Been Despawned.");
								ActiveNPCs.Remove(cubeGrid);
								DeleteGrid(cubeGrid);
								continue;
								
							}
							
						}
						
					}catch(Exception exc){
						
						Logger.AddMsg("Unexpected exception in Planetary Installations Distance Checker");
						
					}
					
				}
				
				//Other NPCs
				if(ActiveNPCs[cubeGrid].SpawnType == "Other"){
					
					try{
						
						if(ActiveNPCs[cubeGrid].FlagForDespawn == true){
							
							var player = SpawnResources.GetNearestPlayer(cubeGrid.GetPosition());
							
							if(player == null || Vector3D.Distance(cubeGrid.GetPosition(), player.GetPosition()) > Settings.OtherNPCs.DespawnDistanceFromPlayer){
								
								Logger.AddMsg("NPC Grid " + cubeGrid.CustomName + " Has Been Despawned.");
								ActiveNPCs.Remove(cubeGrid);
								DeleteGrid(cubeGrid);
								continue;
								
							}
							
						}
						
					}catch(Exception exc){
						
						Logger.AddMsg("Unexpected exception in Other NPCs Distance Checker");
						
					}
					
				}
				
			}
			
		}
				
		public static void InitFactionData(){
			
			//Get NPC Faction Data
			var defaultFactionList = MyDefinitionManager.Static.GetDefaultFactions();
			
			foreach(var faction in defaultFactionList){
				
				//Get Default Factions and Add Them
				var defaultFaction = MyAPIGateway.Session.Factions.TryGetFactionByTag(faction.Tag);
				
				if(defaultFaction != null){
					
					if(defaultFaction.IsEveryoneNpc() == false){
						
						continue;
						
					}
					
					if(faction.DefaultRelation == MyRelationsBetweenFactions.Neutral){
						
						MyAPIGateway.Session.Factions.ChangeAutoAccept(defaultFaction.FactionId, defaultFaction.FounderId, false, true);
						
					}else{
						
						MyAPIGateway.Session.Factions.ChangeAutoAccept(defaultFaction.FactionId, defaultFaction.FounderId, false, false);
						
					}
					
					NPCFactionFounders.Add(defaultFaction.FounderId);
					NPCFactionTags.Add(defaultFaction.Tag);
					
					if(NPCFactionFounderToTag.ContainsKey(defaultFaction.FounderId) == false){
						
						NPCFactionFounderToTag.Add(defaultFaction.FounderId, defaultFaction.Tag);
						
					}
					
					if(NPCFactionTagToFounder.ContainsKey(defaultFaction.Tag) == false){
						
						NPCFactionTagToFounder.Add(defaultFaction.Tag, defaultFaction.FounderId);
						
					}

				}
								
			}
			
			//Get Existing / Remaining NPC Faction Data
			var allFactions = MyAPIGateway.Session.Factions.Factions;
			
			foreach(var faction in allFactions.Keys){
				
				var thisFaction = allFactions[faction];
				
				if(thisFaction.IsEveryoneNpc() == false){
					
					continue;
					
				}
				
				NPCFactionFounders.Add(thisFaction.FounderId);
					NPCFactionTags.Add(thisFaction.Tag);
				
				if(NPCFactionTagToFounder.ContainsKey(thisFaction.Tag) == false){
					
					NPCFactionTagToFounder.Add(thisFaction.Tag, thisFaction.FounderId);
					
				}
				
				if(NPCFactionFounderToTag.ContainsKey(thisFaction.FounderId) == false){
					
					NPCFactionFounderToTag.Add(thisFaction.FounderId, thisFaction.Tag);
					
				}
				
			}
			
			NPCFactionFounders.Add(0);
			NPCFactionTags.Add("Nobody");
			
			if(NPCFactionFounderToTag.ContainsKey(0) == false){
				
				NPCFactionFounderToTag.Add(0, "Nobody");
				
			}
			
			if(NPCFactionTagToFounder.ContainsKey("Nobody") == false){
				
				NPCFactionTagToFounder.Add("Nobody", 0);
				
			}

		}
		
		public static IMyEntity MakeDummyContainer(){
			
			var randomDir = MyUtils.GetRandomVector3D();
			var randomSpawn = randomDir * 10000000;
			var prefab = MyDefinitionManager.Static.GetPrefabDefinition("MES-Dummy-Container");
			var gridOB = prefab.CubeGrids[0];
			gridOB.PositionAndOrientation = new MyPositionAndOrientation(randomSpawn, Vector3.Forward, Vector3.Up);
			MyAPIGateway.Entities.RemapObjectBuilder(gridOB);
			var entity = MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd(gridOB);
			return entity;
			
		}
		
		public static void NewEntityDetected(IMyEntity entity){
			
			var cubeGrid = entity as IMyCubeGrid;
			
			if(cubeGrid == null){
				
				return;
				
			}
			
			Logger.AddMsg("New Grid Detected. Name: " + cubeGrid.CustomName + ". Static: " + cubeGrid.IsStatic.ToString(), true);
			
			int closestIndex = -1;
			double closestDist = -1;
			
			for(int i = 0; i < PendingNPCs.Count; i++){
				
				//Named Grid - No Previous
				if(PendingNPCs[i].GridName == cubeGrid.CustomName && closestDist == -1){
					
					closestDist = Vector3D.Distance(PendingNPCs[i].CurrentCoords, cubeGrid.GetPosition());
					closestIndex = i;
					continue;
					
				}
				
				//Named Grid - Closer Eligible
				if(PendingNPCs[i].GridName == cubeGrid.CustomName && Vector3D.Distance(PendingNPCs[i].CurrentCoords, cubeGrid.GetPosition()) < closestDist){
					
					closestDist = Vector3D.Distance(PendingNPCs[i].CurrentCoords, cubeGrid.GetPosition());
					closestIndex = i;
					continue;
					
				}
				
				//Mismatch Grid Names - Lookin' at you, Keen + Default Cargo Ships >:(
				
				
			}
						
			if(closestIndex >= 0){
				
				PendingNPCs[closestIndex].CubeGrid = cubeGrid;
				
				if(ActiveNPCs.ContainsKey(cubeGrid) == false){
					
					if(PendingNPCs[closestIndex].SpawnType == "PlanetaryCargoShip"){
						
						try{
							
							var gts = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(cubeGrid);
							var blockList = new List<IMyRemoteControl>();
							gts.GetBlocksOfType<IMyRemoteControl>(blockList);
							bool gotRemote = false;
							bool foundMain = false;
							IMyRemoteControl remoteControl = null;
							
							foreach(var block in blockList){
								
								if(block.IsFunctional == true){
									
									remoteControl = block;
									
									if(block.IsMainCockpit == true){
										
										remoteControl = block;
										break;
										
									}
									
								}
								
							}
							
							if(remoteControl == null){
								
								PendingNPCs[closestIndex].SpawnType = "Other";
							
							}else{
								
								remoteControl.ClearWaypoints();
								remoteControl.AddWaypoint(PendingNPCs[closestIndex].EndCoords, "Destination");
								remoteControl.SpeedLimit = PendingNPCs[closestIndex].AutoPilotSpeed;
								remoteControl.FlightMode = Sandbox.ModAPI.Ingame.FlightMode.OneWay;
								remoteControl.SetAutoPilotEnabled(true);
								PendingNPCs[closestIndex].RemoteControl = remoteControl;
								gts.GetBlocksOfType<IMyGasTank>(PendingNPCs[closestIndex].HydrogenTanks);
								gts.GetBlocksOfType<IMyGasGenerator>(PendingNPCs[closestIndex].GasGenerators);
								
							}
							
						}catch(Exception exc){
							
							Logger.AddMsg("Something went wrong with Planetary Cargo Ship Spawn.");
							
						}
				
					}else{
						
						//Planetary Cargo Ships Cannot Be Static
						if(cubeGrid.IsStatic == false && PendingNPCs[closestIndex].ForceStaticGrid == true){
							
							cubeGrid.IsStatic = true;
							
						}
						
					}
					
					ActiveNPCs.Add(cubeGrid, PendingNPCs[closestIndex]);

				}
				
				PendingNPCs.RemoveAt(closestIndex);

			}else{
				
				var activeNPC = CheckIfGridWasActiveNPC(cubeGrid);
				
				if(activeNPC != new ActiveNPC()){
					
					activeNPC.CubeGrid = cubeGrid;
					
					
				}else{
					
					activeNPC.Name = cubeGrid.CustomName;
					activeNPC.CubeGrid = cubeGrid;
					activeNPC.StartCoords = cubeGrid.GetPosition();
					activeNPC.EndCoords = cubeGrid.GetPosition();
					activeNPC.SpawnType = "UnknownSource";
					
					var planet = SpawnResources.GetNearestPlanet(cubeGrid.GetPosition());
							
					if(planet != null){
						
						if(SpawnResources.IsPositionInGravity(cubeGrid.GetPosition(), planet) == true){
							
							activeNPC.Planet = planet;
							
						}
						
					}

				}
				
				if(ActiveNPCs.ContainsKey(cubeGrid) == false){
						
					ActiveNPCs.Add(cubeGrid, activeNPC);
					
				}

			}
			
			//Init Entity Storage
			if(cubeGrid.Storage == null){
					
				cubeGrid.Storage = new MyModStorageComponent();
				
			}
			
			//Start Coords
			if(cubeGrid.Storage.ContainsKey(GuidStartCoords) == false){
				
				cubeGrid.Storage.Add(GuidStartCoords, ActiveNPCs[cubeGrid].StartCoords.ToString());
				
			}else{
				
				cubeGrid.Storage[GuidStartCoords] = ActiveNPCs[cubeGrid].StartCoords.ToString();
				
			}
			
			//End Coords
			if(cubeGrid.Storage.ContainsKey(GuidEndCoords) == false){
				
				cubeGrid.Storage.Add(GuidEndCoords, ActiveNPCs[cubeGrid].EndCoords.ToString());
				
			}else{
				
				cubeGrid.Storage[GuidEndCoords] = ActiveNPCs[cubeGrid].EndCoords.ToString();
				
			}
			
			//Spawn Type
			if(cubeGrid.Storage.ContainsKey(GuidSpawnType) == false){
				
				cubeGrid.Storage.Add(GuidSpawnType, ActiveNPCs[cubeGrid].SpawnType);
				
			}else{
				
				cubeGrid.Storage[GuidSpawnType] = ActiveNPCs[cubeGrid].SpawnType;
				
			}
			
			//Cleanup Timer
			if(cubeGrid.Storage.ContainsKey(GuidCleanupTimer) == false){
				
				cubeGrid.Storage.Add(GuidCleanupTimer, ActiveNPCs[cubeGrid].CleanupTime.ToString());
				
			}else{
				
				cubeGrid.Storage[GuidCleanupTimer] = ActiveNPCs[cubeGrid].CleanupTime.ToString();
				
			}
			
			//Ignore Cleanup
			if(cubeGrid.Storage.ContainsKey(GuidIgnoreCleanup) == false){
				
				cubeGrid.Storage.Add(GuidIgnoreCleanup, ActiveNPCs[cubeGrid].CleanupIgnore.ToString());
				
			}else{
				
				cubeGrid.Storage[GuidIgnoreCleanup] = ActiveNPCs[cubeGrid].CleanupIgnore.ToString();
				
			}
			
			//Replace Weapons
			if(cubeGrid.Storage.ContainsKey(GuidWeaponsReplaced) == false){
				
				cubeGrid.Storage.Add(GuidWeaponsReplaced, ActiveNPCs[cubeGrid].ReplacedWeapons.ToString());
				
			}else{
				
				cubeGrid.Storage[GuidWeaponsReplaced] = ActiveNPCs[cubeGrid].ReplacedWeapons.ToString();
				
			}
				
			NpcOwnershipCheckTimer = 2;
			NpcBlacklistCheckTimer = Settings.General.NpcBlacklistCheckTimerTrigger;

		}
		
		public static bool NpcOwnershipCheck(IMyCubeGrid cubeGrid){
			
			if(cubeGrid == null){
				
				return false;
				
			}
			
			if(ActiveNPCs.ContainsKey(cubeGrid) == false){
				
				return false;
				
			}
			
			string type = "";
			
			if(cubeGrid.Storage != null){
				
				if(cubeGrid.Storage.ContainsKey(GuidSpawnType) == true){
				
					type = cubeGrid.Storage[GuidSpawnType];
				
				}
				
			}
						
			var gridGroups = MyAPIGateway.GridGroups.GetGroup(cubeGrid, GridLinkTypeEnum.Mechanical);
			
			if(gridGroups.Contains(cubeGrid) == false){
				
				gridGroups.Add(cubeGrid);
				
			}
			
			var ownerList = new List<long>();
			
			foreach(var grid in gridGroups){
				
				foreach(var owner in cubeGrid.BigOwners){
					
					if(ownerList.Contains(owner) == false){
						
						ownerList.Add(owner);
						
					}
					
				}
				
				foreach(var owner in cubeGrid.SmallOwners){
					
					if(ownerList.Contains(owner) == false){
						
						ownerList.Add(owner);
						
					}
					
				}
				
			}
			
			bool foundNpcOwner = false;
			bool foundHumanOwner = false;
			
			foreach(var owner in ownerList){
				
				if(NPCFactionFounders.Contains(owner) == true && owner != 0){
					
					foundNpcOwner = true;
					
				}
				
				if(NPCFactionFounders.Contains(owner) == false && owner != 0){
					
					foundHumanOwner = true;
					break;
					
				}
				
			}
			
			if(foundHumanOwner == true){
				
				return false;
				
			}
			
			if(foundNpcOwner == false && foundHumanOwner == false && type == "UnknownSource"){
				
				return false;
				
			}
			
			if(type == "UnknownSource"){
				
				cubeGrid.Storage[GuidSpawnType] = "Other";
				ActiveNPCs[cubeGrid].SpawnType = "Other";
				
			}
				
			return true;
			
		}
		
		public static void RefreshBlockSubparts(IMyCubeGrid cubeGrid){
			
			cubeGrid.IsStatic = true;
			
			var gts = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(cubeGrid);
			var blockList = new List<IMyTerminalBlock>();
			gts.GetBlocksOfType<IMyTerminalBlock>(blockList);
			
			foreach(var block in blockList){
				
				if(block as IMyLargeTurretBase != null){
					
					//block.Init();
					/*
					var turret = block as IMyLargeTurretBase;
					var enabledState = turret.Enabled;
					turret.Enabled = true;
					turret.SyncAzimuth();
					turret.SyncElevation();
					turret.Enabled = enabledState;
					*/
					
				}
				
				if(block as IMyDoor != null){
					
					//block.Init();
					/*
					var door = block as IMyDoor;
					var enabledState = door.Enabled;
					door.Enabled = true;
					door.ToggleDoor();
					door.ToggleDoor();
					door.Enabled = enabledState;
					*/
					
				}
				
			}
			
			cubeGrid.IsStatic = false;
			
		}
		
		public static void RemoveGUIDs(IMyCubeGrid cubeGrid){
			
			if(cubeGrid == null){
				
				return;
				
			}
			
			var gridGroups = MyAPIGateway.GridGroups.GetGroup(cubeGrid, GridLinkTypeEnum.Mechanical);
			
			if(gridGroups.Contains(cubeGrid) == false){
				
				gridGroups.Add(cubeGrid);
				
			}
			
			foreach(var grid in gridGroups){
				
				if(grid.Storage == null){
					
					continue;
					
				}
				
				if(grid.Storage.ContainsKey(GuidStartCoords) == true){
					
					grid.Storage.Remove(GuidStartCoords);
					
				}
				
				if(grid.Storage.ContainsKey(GuidEndCoords) == true){
					
					grid.Storage.Remove(GuidEndCoords);
					
				}
				
				if(grid.Storage.ContainsKey(GuidSpawnType) == true){
					
					grid.Storage.Remove(GuidSpawnType);
					
				}
				
				if(grid.Storage.ContainsKey(GuidCleanupTimer) == true){
					
					grid.Storage.Remove(GuidCleanupTimer);
					
				}
				
				if(grid.Storage.ContainsKey(GuidIgnoreCleanup) == true){
					
					grid.Storage.Remove(GuidIgnoreCleanup);
					
				}
				
			}
			
		}
		
		public static void StartupScan(){
			
			SpawnResources.RefreshEntityLists();
			
			foreach(var entity in SpawnResources.EntityList){
				
				var cubeGrid = entity as IMyCubeGrid;
				
				if(cubeGrid == null){
					
					continue;
					
				}
				
				if(NPCWatcher.ActiveNPCs.ContainsKey(cubeGrid) == true){
					
					continue;
					
				}
				
				//Check For NPC by Spawner Tags
				var activeNPC = CheckIfGridWasActiveNPC(cubeGrid);
				
				if(activeNPC != new ActiveNPC()){
					
					ActiveNPCs.Add(cubeGrid, activeNPC);
					continue;
					
				}
				
				if(NPCWatcher.NpcOwnershipCheck(cubeGrid) == true){
					
					activeNPC = new ActiveNPC();
					activeNPC.Name = cubeGrid.CustomName;
					activeNPC.SpawnType = "UnknownSource";
					activeNPC.GridName = cubeGrid.CustomName;
					activeNPC.CubeGrid = cubeGrid;
					activeNPC.CleanupTime = 0;
					ActiveNPCs.Add(cubeGrid, activeNPC);
					
				}
				
			}
			
		}
		
		public static ActiveNPC CheckIfGridWasActiveNPC(IMyCubeGrid cubeGrid){
			
			var activeNPC = new ActiveNPC();
			
			if(cubeGrid.Storage != null){
					
				if(cubeGrid.Storage.ContainsKey(GuidSpawnType) == true){
				
					activeNPC.Name = cubeGrid.CustomName;
					activeNPC.GridName = cubeGrid.CustomName;
					activeNPC.CubeGrid = cubeGrid;
					activeNPC.SpawnType = cubeGrid.Storage[GuidSpawnType];
					
					if(cubeGrid.Storage.ContainsKey(GuidStartCoords) == true){
				
						var StartCoords = cubeGrid.GetPosition();
						Vector3D.TryParse(cubeGrid.Storage[GuidStartCoords], out StartCoords);
						activeNPC.StartCoords = StartCoords;
						
					}
					
					if(cubeGrid.Storage.ContainsKey(GuidEndCoords) == true){
						
						var EndCoords = cubeGrid.GetPosition();
						Vector3D.TryParse(cubeGrid.Storage[GuidEndCoords], out EndCoords);
						activeNPC.EndCoords = EndCoords;
						
					}
					
					if(cubeGrid.Storage.ContainsKey(GuidCleanupTimer) == true){
						
						int timer = 0;
						int.TryParse(cubeGrid.Storage[GuidCleanupTimer], out timer);
						activeNPC.CleanupTime = timer;
						
					}
					
					if(cubeGrid.Storage.ContainsKey(GuidIgnoreCleanup) == true){
						
						var cleanIgnore = false;
						bool.TryParse(cubeGrid.Storage[GuidIgnoreCleanup], out cleanIgnore);
						activeNPC.CleanupIgnore = cleanIgnore;
						
					}
					
					if(cubeGrid.Storage.ContainsKey(GuidWeaponsReplaced) == true){
						
						var weaponsReplaced = false;
						bool.TryParse(cubeGrid.Storage[GuidWeaponsReplaced], out weaponsReplaced);
						activeNPC.ReplacedWeapons = weaponsReplaced;
						
					}
					
					var planet = SpawnResources.GetNearestPlanet(cubeGrid.GetPosition());
					
					if(planet != null){
						
						if(SpawnResources.IsPositionInGravity(cubeGrid.GetPosition(), planet) == true){
							
							activeNPC.Planet = planet;
							
						}
						
					}
					
					if(activeNPC.SpawnType == "PlanetaryCargoShip"){
						
						var gts = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(cubeGrid);
						var blockList = new List<IMyRemoteControl>();
						gts.GetBlocksOfType<IMyRemoteControl>(blockList);
						
						foreach(var block in blockList){
							
							if(block.IsFunctional == true){
								
								if(block.IsAutoPilotEnabled == true){
									
									activeNPC.RemoteControl = block;
									
								}
								
							}
							
						}
						
						gts.GetBlocksOfType<IMyGasTank>(activeNPC.HydrogenTanks);
						gts.GetBlocksOfType<IMyGasGenerator>(activeNPC.GasGenerators);
						
					}
						
				}
				
			}
			
			return activeNPC;
			
		}
		
	}
	
}