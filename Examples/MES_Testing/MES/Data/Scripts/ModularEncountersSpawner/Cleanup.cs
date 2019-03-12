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

	public static class Cleanup{

		public static void CleanupProcess(bool singleCheckParameters = false){
			
			MES_SessionCore.PlayerList.Clear();
			MyAPIGateway.Players.GetPlayers(MES_SessionCore.PlayerList);
			var grids = new List<IMyCubeGrid>(NPCWatcher.ActiveNPCs.Keys.ToList());
			
			foreach(var cubeGrid in grids){
				
				if(NPCWatcher.ActiveNPCs.ContainsKey(cubeGrid) == false){
					
					continue;
					
				}
				
				if(cubeGrid == null){
					
					NPCWatcher.ActiveNPCs.Remove(cubeGrid);
					
				}
				
				if(NPCWatcher.ActiveNPCs[cubeGrid].CleanupIgnore == true){
					
					continue;
					
				}
				
				if(HasLegacyIgnoreTag(cubeGrid) == true){
						
					continue;
						
				}
				
				if(NPCWatcher.ActiveNPCs[cubeGrid].SpawnType == "Other"){
					
					if(IsSubgridForNormalEncounter(cubeGrid) == true){
						
						continue;
						
					}
					
				}
				
				var cleanSettings = GetCleaningSettingsForType(NPCWatcher.ActiveNPCs[cubeGrid].SpawnType);
				
				if(cleanSettings.UseCleanupSettings == false){
					
					continue;
					
				}
				
				var powered = IsGridPowered(cubeGrid);
				
				if(singleCheckParameters == false){
					
					var outsideDistance = IsDistanceFurtherThanPlayers(cleanSettings, cubeGrid.GetPosition(), powered);
					
					if(outsideDistance == true && cleanSettings.CleanupDistanceStartsTimer == false){
						
						Logger.AddMsg("Cleanup: " + NPCWatcher.ActiveNPCs[cubeGrid].SpawnType + "/" + cubeGrid.CustomName + " Is Further Than Allowed Distance From Player. Grid Marked For Despawn.");
						NPCWatcher.ActiveNPCs[cubeGrid].FlagForDespawn = true;
						continue;
						
					}
					
					var timerIsExpired = TimerExpired(cubeGrid, cleanSettings, outsideDistance, powered);
										
					if(timerIsExpired == true){
						
						Logger.AddMsg("Cleanup: " + NPCWatcher.ActiveNPCs[cubeGrid].SpawnType + "/" + cubeGrid.CustomName + " Timer Has Expired. Grid Marked For Despawn.");
						NPCWatcher.ActiveNPCs[cubeGrid].FlagForDespawn = true;
						continue;
						
					}else{
						
						if(cleanSettings.CleanupDistanceStartsTimer == true && cleanSettings.CleanupResetTimerWithinDistance == true && outsideDistance == false){
							
							NPCWatcher.ActiveNPCs[cubeGrid].CleanupTime = 0;
							
							if(cubeGrid.Storage != null){
								
								if(cubeGrid.Storage.ContainsKey(NPCWatcher.GuidCleanupTimer) == false){
				
									cubeGrid.Storage.Add(NPCWatcher.GuidCleanupTimer, NPCWatcher.ActiveNPCs[cubeGrid].CleanupTime.ToString());
									
								}else{
									
									cubeGrid.Storage[NPCWatcher.GuidCleanupTimer] = NPCWatcher.ActiveNPCs[cubeGrid].CleanupTime.ToString();
									
								}
								
							}
							
						}
						
					}
					
				}
				
				if(singleCheckParameters == true){
					
					if(NPCWatcher.ActiveNPCs[cubeGrid].CheckedBlockCount == false && cleanSettings.CleanupUseBlockLimit == true){
						
						NPCWatcher.ActiveNPCs[cubeGrid].CheckedBlockCount = true;
						var blockList = new List<IMySlimBlock>();
						cubeGrid.GetBlocks(blockList);
						
						if(blockList.Count > cleanSettings.CleanupBlockLimitTrigger){
							
							NPCWatcher.ActiveNPCs[cubeGrid].FlagForDespawn = true;
							
						}
						
					}
					
					if(NPCWatcher.ActiveNPCs[cubeGrid].DisabledBlocks == false && cleanSettings.UseBlockDisable == true){
						
						NPCWatcher.ActiveNPCs[cubeGrid].DisabledBlocks = true;
						var gts = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(cubeGrid);
						List<IMyFunctionalBlock> blockList = new List<IMyFunctionalBlock>();
						gts.GetBlocksOfType<IMyFunctionalBlock>(blockList);
						
						foreach(var block in blockList){
							
							if(cleanSettings.DisableAirVent == true){
								
								if(block as IMyAirVent != null){
									
									block.Enabled = false;
									
								}
								
							}
							
							if(cleanSettings.DisableAntenna == true){
								
								if(block as IMyRadioAntenna != null){
									
									block.Enabled = false;
									
								}
								
							}
							
							if(cleanSettings.DisableArtificialMass == true){
								
								if(block as IMyArtificialMassBlock != null){
									
									block.Enabled = false;
									
								}
								
							}
							
							if(cleanSettings.DisableAssembler == true){
								
								if(block as IMyAssembler != null){
									
									block.Enabled = false;
									
								}
								
							}
							
							if(cleanSettings.DisableBattery == true){
								
								if(block as IMyBatteryBlock != null){
									
									block.Enabled = false;
									
								}
								
							}
							
							if(cleanSettings.DisableBeacon == true){
								
								if(block as IMyBeacon != null){
									
									block.Enabled = false;
									
								}
								
							}
							
							if(cleanSettings.DisableCollector == true){
								
								if(block as IMyCollector != null){
									
									block.Enabled = false;
									
								}
								
							}
							
							if(cleanSettings.DisableConnector == true){
								
								if(block as IMyShipConnector != null){
									
									block.Enabled = false;
									
								}
								
							}
							
							if(cleanSettings.DisableConveyorSorter == true){
								
								if(block as IMyConveyorSorter != null){
									
									block.Enabled = false;
									
								}
								
							}
							
							if(cleanSettings.DisableDecoy == true){
								
								if(block as IMyDecoy != null){
									
									block.Enabled = false;
									
								}
								
							}
							
							if(cleanSettings.DisableDrill == true){
								
								if(block as IMyShipDrill != null){
									
									block.Enabled = false;
									
								}
								
							}
							
							if(cleanSettings.DisableJumpDrive == true){
								
								if(block as IMyJumpDrive != null){
									
									block.Enabled = false;
									
								}
								
							}
							
							if(cleanSettings.DisableGasGenerator == true){
								
								if(block as IMyGasGenerator != null){
									
									block.Enabled = false;
									
								}
								
							}
							
							if(cleanSettings.DisableGasTank == true){
								
								if(block as IMyGasTank != null){
									
									block.Enabled = false;
									
								}
								
							}
							
							if(cleanSettings.DisableGatlingGun == true){
								
								if(block as IMySmallGatlingGun != null){
									
									block.Enabled = false;
									
								}
								
							}
							
							if(cleanSettings.DisableGatlingTurret == true){
								
								if(block as IMyLargeGatlingTurret != null){
									
									block.Enabled = false;
									
								}
								
							}
							
							if(cleanSettings.DisableGravityGenerator == true){
								
								if(block as IMyGravityGeneratorBase != null){
									
									block.Enabled = false;
									
								}
								
							}
							
							if(cleanSettings.DisableGrinder == true){
								
								if(block as IMyShipGrinder != null){
									
									block.Enabled = false;
									
								}
								
							}
							
							if(cleanSettings.DisableGyro == true){
								
								if(block as IMyGyro != null){
									
									block.Enabled = false;
									
								}
								
							}
							
							if(cleanSettings.DisableInteriorTurret == true){
								
								if(block as IMyLargeInteriorTurret != null){
									
									block.Enabled = false;
									
								}
								
							}
							
							if(cleanSettings.DisableLandingGear == true){
								
								if(block as IMyLandingGear != null){
									
									block.Enabled = false;
									
								}
								
							}
							
							if(cleanSettings.DisableLaserAntenna == true){
								
								if(block as IMyLaserAntenna != null){
									
									block.Enabled = false;
									
								}
								
							}
							
							if(cleanSettings.DisableLcdPanel == true){
								
								if(block as IMyTextPanel != null){
									
									block.Enabled = false;
									
								}
								
							}
							
							if(cleanSettings.DisableLightBlock == true){
								
								if(block as IMyLightingBlock != null){
									
									block.Enabled = false;
									
								}
								
							}
							
							if(cleanSettings.DisableMedicalRoom == true){
								
								if(block as IMyMedicalRoom != null){
									
									block.Enabled = false;
									
								}
								
							}
							
							if(cleanSettings.DisableMergeBlock == true){
								
								if(block as IMyShipMergeBlock != null){
									
									block.Enabled = false;
									
								}
								
							}
							
							if(cleanSettings.DisableMissileTurret == true){
								
								if(block as IMyLargeMissileTurret != null){
									
									block.Enabled = false;
									
								}
								
							}
							
							if(cleanSettings.DisableOxygenFarm == true){
								
								if(block as IMyOxygenFarm != null){
									
									block.Enabled = false;
									
								}
								
							}
							
							if(cleanSettings.DisableParachuteHatch == true){
								
								if(block as IMyParachute != null){
									
									block.Enabled = false;
									
								}
								
							}
							
							if(cleanSettings.DisablePiston == true){
								
								if(block as IMyPistonBase != null){
									
									block.Enabled = false;
									
								}
								
							}
							
							if(cleanSettings.DisableProgrammableBlock == true){
								
								if(block as IMyProgrammableBlock != null){
									
									block.Enabled = false;
									
								}
								
							}
							
							if(cleanSettings.DisableProjector == true){
								
								if(block as IMyProjector != null){
									
									block.Enabled = false;
									
								}
								
							}
							
							if(cleanSettings.DisableReactor == true){
								
								if(block as IMyReactor != null){
									
									block.Enabled = false;
									
								}
								
							}
							
							if(cleanSettings.DisableRefinery == true){
								
								if(block as IMyRefinery != null){
									
									block.Enabled = false;
									
								}
								
							}
							
							if(cleanSettings.DisableRocketLauncher == true){
								
								if(block as IMySmallMissileLauncher != null){
									
									block.Enabled = false;
									
								}
								
							}
							
							if(cleanSettings.DisableReloadableRocketLauncher == true){
								
								if(block as IMySmallMissileLauncherReload != null){
									
									block.Enabled = false;
									
								}
								
							}
							
							if(cleanSettings.DisableRotor == true){
								
								if(block as IMyMotorStator != null){
									
									block.Enabled = false;
									
								}
								
							}
							
							if(cleanSettings.DisableSensor == true){
								
								if(block as IMySensorBlock != null){
									
									block.Enabled = false;
									
								}
								
							}
							
							if(cleanSettings.DisableSolarPanel == true){
								
								if(block as IMySolarPanel != null){
									
									block.Enabled = false;
									
								}
								
							}
							
							if(cleanSettings.DisableSoundBlock == true){
								
								if(block as IMySoundBlock != null){
									
									block.Enabled = false;
									
								}
								
							}
							
							if(cleanSettings.DisableSpaceBall == true){
								
								if(block as IMySpaceBall != null){
									
									block.Enabled = false;
									
								}
								
							}
							
							if(cleanSettings.DisableTimerBlock == true){
								
								if(block as IMyTimerBlock != null){
									
									block.Enabled = false;
									
								}
								
							}
							
							if(cleanSettings.DisableThruster == true){
								
								if(block as IMyThrust != null){
									
									block.Enabled = false;
									
								}
								
							}
							
							if(cleanSettings.DisableWelder == true){
								
								if(block as IMyShipWelder != null){
									
									block.Enabled = false;
									
								}
								
							}
							
							if(cleanSettings.DisableUpgradeModule == true){
								
								if(block as IMyUpgradeModule != null){
									
									block.Enabled = false;
									
								}
								
							}
							
						}
						
					}
					
					continue;
					
				}
				
			}
			
		}
		
		public static bool IsDistanceFurtherThanPlayers(CleanupSettings cleanSettings, Vector3D coords, bool powered){
			
			if(cleanSettings.CleanupUseDistance == false){
				
				return false;
				
			}
			
			var player = SpawnResources.GetNearestPlayer(coords);
			
			if(player == null){
				
				return false;
				
			}
			
			double distanceToCheck = 0;
			
			if(cleanSettings.CleanupUnpoweredOverride == true && powered == false){
				
				distanceToCheck = cleanSettings.CleanupUnpoweredDistanceTrigger;
				
			}else{
				
				distanceToCheck = cleanSettings.CleanupDistanceTrigger;
				
			}
			
			if(Vector3D.Distance(player.GetPosition(), coords) > distanceToCheck){
				
				return true;
				
			}
			
			return false;
			
		}
		
		public static bool TimerExpired(IMyCubeGrid cubeGrid, CleanupSettings cleanSettings, bool distanceCheck, bool powered){
			
			if(cleanSettings.CleanupUseTimer == false){
				
				return false;
				
			}
			
			int timeTriggerToUse = 0;
			
			if(cleanSettings.CleanupUnpoweredOverride == true && powered == false){
				
				timeTriggerToUse = cleanSettings.CleanupUnpoweredTimerTrigger;
				
			}else{
				
				timeTriggerToUse = cleanSettings.CleanupTimerTrigger;
				
			}
			
			if(cleanSettings.CleanupDistanceStartsTimer == true && distanceCheck == false){
				
				return false;
				
			}
			
			NPCWatcher.ActiveNPCs[cubeGrid].CleanupTime += Settings.General.NpcCleanupCheckTimerTrigger;
			
			if(cubeGrid.Storage != null){
								
				if(cubeGrid.Storage.ContainsKey(NPCWatcher.GuidCleanupTimer) == false){

					cubeGrid.Storage.Add(NPCWatcher.GuidCleanupTimer, NPCWatcher.ActiveNPCs[cubeGrid].CleanupTime.ToString());
					
				}else{
					
					cubeGrid.Storage[NPCWatcher.GuidCleanupTimer] = NPCWatcher.ActiveNPCs[cubeGrid].CleanupTime.ToString();
					
				}
				
			}
			
			if(NPCWatcher.ActiveNPCs[cubeGrid].CleanupTime >= timeTriggerToUse){
				
				return true;
				
			}
			
			return false;
			
		}
		
		public static CleanupSettings GetCleaningSettingsForType(string spawnType){
		
			var thisSettings = new CleanupSettings();
			
			if(spawnType == "SpaceCargoShip"){
				
				thisSettings.UseCleanupSettings = Settings.SpaceCargoShips.UseCleanupSettings;
				thisSettings.CleanupUseDistance = Settings.SpaceCargoShips.CleanupUseDistance;
				thisSettings.CleanupUseTimer = Settings.SpaceCargoShips.CleanupUseTimer;
				thisSettings.CleanupDistanceStartsTimer = Settings.SpaceCargoShips.CleanupDistanceStartsTimer;
				thisSettings.CleanupResetTimerWithinDistance = Settings.SpaceCargoShips.CleanupResetTimerWithinDistance;
				thisSettings.CleanupDistanceTrigger = Settings.SpaceCargoShips.CleanupDistanceTrigger;
				thisSettings.CleanupTimerTrigger = Settings.SpaceCargoShips.CleanupTimerTrigger;
				thisSettings.CleanupIncludeUnowned = Settings.SpaceCargoShips.CleanupIncludeUnowned;
				thisSettings.CleanupUnpoweredOverride = Settings.SpaceCargoShips.CleanupUnpoweredOverride;
				thisSettings.CleanupUnpoweredDistanceTrigger = Settings.SpaceCargoShips.CleanupUnpoweredDistanceTrigger;
				thisSettings.CleanupUnpoweredTimerTrigger = Settings.SpaceCargoShips.CleanupUnpoweredTimerTrigger;
				
				
			}
			
			if(spawnType == "RandomEncounter"){
				
				thisSettings.UseCleanupSettings = Settings.RandomEncounters.UseCleanupSettings;
				thisSettings.CleanupUseDistance = Settings.RandomEncounters.CleanupUseDistance;
				thisSettings.CleanupUseTimer = Settings.RandomEncounters.CleanupUseTimer;
				thisSettings.CleanupDistanceStartsTimer = Settings.RandomEncounters.CleanupDistanceStartsTimer;
				thisSettings.CleanupResetTimerWithinDistance = Settings.RandomEncounters.CleanupResetTimerWithinDistance;
				thisSettings.CleanupDistanceTrigger = Settings.RandomEncounters.CleanupDistanceTrigger;
				thisSettings.CleanupTimerTrigger = Settings.RandomEncounters.CleanupTimerTrigger;
				thisSettings.CleanupIncludeUnowned = Settings.RandomEncounters.CleanupIncludeUnowned;
				thisSettings.CleanupUnpoweredOverride = Settings.RandomEncounters.CleanupUnpoweredOverride;
				thisSettings.CleanupUnpoweredDistanceTrigger = Settings.RandomEncounters.CleanupUnpoweredDistanceTrigger;
				thisSettings.CleanupUnpoweredTimerTrigger = Settings.RandomEncounters.CleanupUnpoweredTimerTrigger;
				
			}
			
			if(spawnType == "BossEncounter"){
				
				thisSettings.UseCleanupSettings = Settings.BossEncounters.UseCleanupSettings;
				thisSettings.CleanupUseDistance = Settings.BossEncounters.CleanupUseDistance;
				thisSettings.CleanupUseTimer = Settings.BossEncounters.CleanupUseTimer;
				thisSettings.CleanupDistanceStartsTimer = Settings.BossEncounters.CleanupDistanceStartsTimer;
				thisSettings.CleanupResetTimerWithinDistance = Settings.BossEncounters.CleanupResetTimerWithinDistance;
				thisSettings.CleanupDistanceTrigger = Settings.BossEncounters.CleanupDistanceTrigger;
				thisSettings.CleanupTimerTrigger = Settings.BossEncounters.CleanupTimerTrigger;
				thisSettings.CleanupIncludeUnowned = Settings.BossEncounters.CleanupIncludeUnowned;
				thisSettings.CleanupUnpoweredOverride = Settings.BossEncounters.CleanupUnpoweredOverride;
				thisSettings.CleanupUnpoweredDistanceTrigger = Settings.BossEncounters.CleanupUnpoweredDistanceTrigger;
				thisSettings.CleanupUnpoweredTimerTrigger = Settings.BossEncounters.CleanupUnpoweredTimerTrigger;
				
			}
			
			if(spawnType == "PlanetaryCargoShip"){
				
				thisSettings.UseCleanupSettings = Settings.PlanetaryCargoShips.UseCleanupSettings;
				thisSettings.CleanupUseDistance = Settings.PlanetaryCargoShips.CleanupUseDistance;
				thisSettings.CleanupUseTimer = Settings.PlanetaryCargoShips.CleanupUseTimer;
				thisSettings.CleanupDistanceStartsTimer = Settings.PlanetaryCargoShips.CleanupDistanceStartsTimer;
				thisSettings.CleanupResetTimerWithinDistance = Settings.PlanetaryCargoShips.CleanupResetTimerWithinDistance;
				thisSettings.CleanupDistanceTrigger = Settings.PlanetaryCargoShips.CleanupDistanceTrigger;
				thisSettings.CleanupTimerTrigger = Settings.PlanetaryCargoShips.CleanupTimerTrigger;
				thisSettings.CleanupIncludeUnowned = Settings.PlanetaryCargoShips.CleanupIncludeUnowned;
				thisSettings.CleanupUnpoweredOverride = Settings.PlanetaryCargoShips.CleanupUnpoweredOverride;
				thisSettings.CleanupUnpoweredDistanceTrigger = Settings.PlanetaryCargoShips.CleanupUnpoweredDistanceTrigger;
				thisSettings.CleanupUnpoweredTimerTrigger = Settings.PlanetaryCargoShips.CleanupUnpoweredTimerTrigger;
				
			}
			
			if(spawnType == "PlanetaryInstallation"){

				thisSettings.UseCleanupSettings = Settings.PlanetaryInstallations.UseCleanupSettings;
				thisSettings.CleanupUseDistance = Settings.PlanetaryInstallations.CleanupUseDistance;
				thisSettings.CleanupUseTimer = Settings.PlanetaryInstallations.CleanupUseTimer;
				thisSettings.CleanupDistanceStartsTimer = Settings.PlanetaryInstallations.CleanupDistanceStartsTimer;
				thisSettings.CleanupResetTimerWithinDistance = Settings.PlanetaryInstallations.CleanupResetTimerWithinDistance;
				thisSettings.CleanupDistanceTrigger = Settings.PlanetaryInstallations.CleanupDistanceTrigger;
				thisSettings.CleanupTimerTrigger = Settings.PlanetaryInstallations.CleanupTimerTrigger;
				thisSettings.CleanupIncludeUnowned = Settings.PlanetaryInstallations.CleanupIncludeUnowned;
				thisSettings.CleanupUnpoweredOverride = Settings.PlanetaryInstallations.CleanupUnpoweredOverride;
				thisSettings.CleanupUnpoweredDistanceTrigger = Settings.PlanetaryInstallations.CleanupUnpoweredDistanceTrigger;
				thisSettings.CleanupUnpoweredTimerTrigger = Settings.PlanetaryInstallations.CleanupUnpoweredTimerTrigger;

			}
			
			if(spawnType == "Other"){
				
				thisSettings.UseCleanupSettings = Settings.OtherNPCs.UseCleanupSettings;
				thisSettings.CleanupUseDistance = Settings.OtherNPCs.CleanupUseDistance;
				thisSettings.CleanupUseTimer = Settings.OtherNPCs.CleanupUseTimer;
				thisSettings.CleanupDistanceStartsTimer = Settings.OtherNPCs.CleanupDistanceStartsTimer;
				thisSettings.CleanupResetTimerWithinDistance = Settings.OtherNPCs.CleanupResetTimerWithinDistance;
				thisSettings.CleanupDistanceTrigger = Settings.OtherNPCs.CleanupDistanceTrigger;
				thisSettings.CleanupTimerTrigger = Settings.OtherNPCs.CleanupTimerTrigger;
				thisSettings.CleanupIncludeUnowned = Settings.OtherNPCs.CleanupIncludeUnowned;
				thisSettings.CleanupUnpoweredOverride = Settings.OtherNPCs.CleanupUnpoweredOverride;
				thisSettings.CleanupUnpoweredDistanceTrigger = Settings.OtherNPCs.CleanupUnpoweredDistanceTrigger;
				thisSettings.CleanupUnpoweredTimerTrigger = Settings.OtherNPCs.CleanupUnpoweredTimerTrigger;
				
			}
			
			return thisSettings;
			
		}
		
		public static bool IsSubgridForNormalEncounter(IMyCubeGrid cubeGrid){
			
			if(cubeGrid == null || MyAPIGateway.Entities.Exist(cubeGrid) == false){
				
				return false;
				
			}
			
			var gridGroups = MyAPIGateway.GridGroups.GetGroup(cubeGrid, GridLinkTypeEnum.Mechanical);
			
			if(gridGroups.Contains(cubeGrid) == false){
				
				gridGroups.Add(cubeGrid);
				
			}
			
			foreach(var grid in gridGroups){
				
				if(NPCWatcher.ActiveNPCs.ContainsKey(grid) == true){
					
					var spawnType = NPCWatcher.ActiveNPCs[grid].SpawnType;
					
					if(spawnType != "Other" && spawnType != "UnknownSource"){
						
						return true;
						
					}
					
				}
				
			}
			
			return false;
			
		}
		
		public static bool HasLegacyIgnoreTag(IMyCubeGrid cubeGrid){
			
			if(cubeGrid == null || MyAPIGateway.Entities.Exist(cubeGrid) == false){
				
				return false;
				
			}
			
			var gridGroups = MyAPIGateway.GridGroups.GetGroup(cubeGrid, GridLinkTypeEnum.Mechanical);
			
			if(gridGroups.Contains(cubeGrid) == false){
				
				gridGroups.Add(cubeGrid);
				
			}
			
			foreach(var grid in gridGroups){
				
				if(grid.CustomName.Contains("[NPC-IGNORE]") == true){
					
					if(NPCWatcher.ActiveNPCs.ContainsKey(grid) == true){
						
						NPCWatcher.ActiveNPCs[grid].CleanupIgnore = true;
						
					}
					
					return true;
					
				}
				
			}
			
			return false;
			
		}
		
		public static bool IsGridPowered(IMyCubeGrid cubeGrid){
			
			
			if(string.IsNullOrEmpty(MyVisualScriptLogicProvider.GetEntityName(cubeGrid.EntityId)) == true){
				
				MyVisualScriptLogicProvider.SetName(cubeGrid.EntityId, cubeGrid.EntityId.ToString());
				
			}
			
			return MyVisualScriptLogicProvider.HasPower(MyVisualScriptLogicProvider.GetEntityName(cubeGrid.EntityId));
			
		}
		
	}
	
}