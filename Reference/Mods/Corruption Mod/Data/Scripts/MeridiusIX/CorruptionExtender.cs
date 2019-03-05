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
using Sandbox.ModAPI.Ingame;
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

namespace MeridiusIX{
	
	[MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
	
	public class CorruptionExtender : MySessionComponentBase{
		
		List<IMyFaction> npcfactions = new List<IMyFaction>();
		List<long> npcFactionOwners = new List<long>();
		string npcCorruptionTag = "CORRUPT";
		List<Sandbox.ModAPI.IMyProgrammableBlock> pbRecompileList = new List<Sandbox.ModAPI.IMyProgrammableBlock>();
		List<Sandbox.ModAPI.Ingame.IMyRefinery> shieldOverloadList = new List<Sandbox.ModAPI.Ingame.IMyRefinery>();
		bool processPBs = false;
		bool overloadShields = false;
		List<string>bossSpawnGroups = new List<string>();
		List<string> marauderPrefabList = new List<string>();
		
		Dictionary<IMyCubeGrid, int> shieldedGrids = new Dictionary<IMyCubeGrid, int>();
		
		List<SeekerMissileDetails> activeMissileList = new List<SeekerMissileDetails>();
		
		bool shieldModEnabled = false;
		
		List<long> energyCannonSourceEntity = new List<long>();
		List<int> energyCannonTimers = new List<int>();
		int badLaserLists = 0;
		
		List<long> teslaSourceEntity = new List<long>();
		List<long> teslaTargetEntity = new List<long>();
		List<List<Vector3D>> teslaEffectsLists = new List<List<Vector3D>>();
		List<int> teslaEffectsTimers = new List<int>();
		List<int> teslaEffectsTotalTimers = new List<int>();
		int badLists = 0;
		
		//LocalGPS
		IMyGps localGPSPlayer;
		
		//Enabled Drones - Someday I'll do the config file.
		bool enableBarnacleDrone = true;
		bool enableBlackmailDrone = true;
		bool enableFighterDrone = true;
		bool enableGlitchyDrone = true;
		bool enableGravityCannonDrone = true;
		bool enableHiveShip = true;
		bool enableHorseflyGatlingDrone = true;
		bool enableHorseflyMissileDrone = true;
		bool enableHoundDrone = true;
		bool enableHunterDrone = true;
		bool enableKamikazeGatlingDrone = true;
		bool enableKamikazeMissileDrone = true;
		bool enableKamikazeGatlingMkIIDrone = true;
		bool enableKamikazeMissileMkIIDrone = true;
		bool enableMediumCargoTransport = true;
		bool enableRelayDrone = true;
		bool enableScoutingDrone = true;
		bool enableSiegeDrone = true;
		bool enableSiegeMkIIDrone = true;
		bool enableSniperDrone = true;
		bool enableStrikeDrone = true;
		bool enableTunnelDrone = true;
		
		int tickCounter = 0;
		int encounterCreateTimer = 0;
		int encounterCreateTimerTrigger = 1800;
		int shieldCheckTimer = 0;
		List<MarauderEncounter> activeEncounters = new List<MarauderEncounter>();
		
		Random rnd = new Random();
		bool scriptInitialized = false;
		bool debugMode = false;
		bool betaMode = false;
		bool debugBeamTest = false;
		
		/*
		void AddGridLogger(IMyEntity entity){
			
			var cubeGrid = entity as IMyCubeGrid;
			
			if(cubeGrid != null){
				
				LogEntry(cubeGrid.CustomName + " Added");
				
			}
			
		}
		*/
		
		public override void UpdateBeforeSimulation(){
			
			if(scriptInitialized == false){
				
				//MyAPIGateway.Entities.OnEntityAdd += AddGridLogger;
				MyAPIGateway.Utilities.MessageEntered += CorruptionChatCommand;
				MyAPIGateway.Multiplayer.RegisterMessageHandler(6356, PlayEffect);
				MyAPIGateway.Multiplayer.RegisterMessageHandler(6756, TeslaEffect);
				MyAPIGateway.Multiplayer.RegisterMessageHandler(6716, EnergyCannonRegister);
				MyAPIGateway.Multiplayer.RegisterMessageHandler(6706, LocalGPSManager);
				
			}
						
			if(scriptInitialized == false && MyAPIGateway.Multiplayer.IsServer == true){
				
				/*
				//Debug/Test - GPS Shenanigans
				var serverPlayer = MyAPIGateway.Session.LocalHumanPlayer;
				var testGPS = MyAPIGateway.Session.GPS.Create("Test2", "First", new Vector3D(50000,50000,0), true);
				MyAPIGateway.Session.GPS.AddLocalGps(testGPS);
				MyVisualScriptLogicProvider.SetGPSColor("Test2", new Color(255,55,255), serverPlayer.IdentityId);
				*/
				
				MyAPIGateway.Multiplayer.RegisterMessageHandler(6456, AdminCommand);
				var randomDir = MyUtils.GetRandomVector3D();
				var randomSpawn = randomDir * 1000000;
				var prefab = MyDefinitionManager.Static.GetPrefabDefinition("Dummy PB-Proj");
				var gridOB = prefab.CubeGrids[0];
				gridOB.PositionAndOrientation = new MyPositionAndOrientation(randomSpawn, Vector3.Forward, Vector3.Up);
				MyAPIGateway.Entities.RemapObjectBuilder(gridOB);
				var entity = MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd(gridOB);
				
				//Add Boss Spawns
				bossSpawnGroups.Add("(CPC)HIVE_SHIP");
				bossSpawnGroups.Add("(CPC)BOSS_SQUADRON_ANTENNA");
				//bossSpawnGroups.Add("(CPC)METEOR_CANNON_DRONE_ANTENNA");
				//bossSpawnGroups.Add("(CPC)LASER_CANNON_DRONE_ANTENNA");
				//bossSpawnGroups.Add("(CPC)SIEGE_DRONE_3_ANTENNA");
				bossSpawnGroups.Add("(CPC)SWIFT_DRONE_ANTENNA");
				//bossSpawnGroups.Add("(CPC)STALKER_DRONE");
				
				//Add Marauder Spawns
				//marauderPrefabList.Add("(NPC-CPC) Marauder Drone");
				marauderPrefabList.Add("(NPC-CPC) Vengeance Drone");
				//marauderPrefabList.Add("(NPC-CPC) Maelstrom Drone");
				
				//Get Default Factions
				var defaultFactionList = MyDefinitionManager.Static.GetDefaultFactions();
				foreach(var faction in defaultFactionList){
					
					var defaultFaction = MyAPIGateway.Session.Factions.TryGetFactionByTag(faction.Tag);
					
					if(defaultFaction != null){
						
						npcfactions.Add(defaultFaction);
						npcFactionOwners.Add(defaultFaction.FounderId);
						
					}
					
				}
				
				//Check For Shield Mod
				var allDefs = MyDefinitionManager.Static.GetAllDefinitions();
				
				foreach(var definition in allDefs){
					
					if(definition.Id.SubtypeId.ToString().Contains("LargeShipSmallShieldGeneratorBase")){
						
						shieldModEnabled = true;
						
					}
					
					if(definition.Id.SubtypeId.ToString().Contains("LargeShipLargeShieldGeneratorBase")){
						
						shieldModEnabled = true;
						
					}
					
					if(definition.Id.SubtypeId.ToString().Contains("SmallShipSmallShieldGeneratorBase")){
						
						shieldModEnabled = true;
						
					}
					
					if(definition.Id.SubtypeId.ToString().Contains("SmallShipMicroShieldGeneratorBase")){
						
						shieldModEnabled = true;
						
					}
					
				}
				
				//PB Control - Corruption DebugPB
				var corruptionDebugPB = MyAPIGateway.TerminalControls.CreateProperty<bool, Sandbox.ModAPI.Ingame.IMyProgrammableBlock>("CorruptionDebugPB");
				corruptionDebugPB.Enabled = Block => true;
				corruptionDebugPB.Getter = Block => {
					
					if(IsBlockOwnerNPC(Block) == false){
						
						return false;
						
					}
					
					MyVisualScriptLogicProvider.ShowNotificationToAll(Block.CustomData, 1000);
					return true;
					
				};
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyProgrammableBlock>(corruptionDebugPB);
								
				//PB Control - Corruption Drone Despawn
				var corruptionDroneDespawn = MyAPIGateway.TerminalControls.CreateProperty<bool, Sandbox.ModAPI.Ingame.IMyProgrammableBlock>("CorruptionDroneDespawn");
				corruptionDroneDespawn.Enabled = Block => true;
				corruptionDroneDespawn.Getter = Block => {
					
					if(IsBlockOwnerNPC(Block) == false){
						
						return false;
						
					}
					
					bool results = TryDespawn((VRage.Game.ModAPI.IMyCubeGrid)Block.CubeGrid);
					return results;
					
				};
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyProgrammableBlock>(corruptionDroneDespawn);
				
				//PB Control - Corruption Drone Chat
				var corruptionDroneChat = MyAPIGateway.TerminalControls.CreateProperty<bool, Sandbox.ModAPI.Ingame.IMyProgrammableBlock>("CorruptionDroneChat");
				corruptionDroneChat.Enabled = Block => true;
				corruptionDroneChat.Getter = Block => {
					
					if(IsBlockOwnerNPC(Block) == false){
						
						return false;
						
					}
					
					bool results = DroneChat(Block);
					return results;
					
				};
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyProgrammableBlock>(corruptionDroneChat);
				
				
				//PB Control - Corruption Ownership Check
				var corruptionOwnerCheck = MyAPIGateway.TerminalControls.CreateProperty<bool, Sandbox.ModAPI.Ingame.IMyProgrammableBlock>("CorruptionOwnerCheck");
				corruptionOwnerCheck.Enabled = Block => true;
				corruptionOwnerCheck.Getter = Block => {
					
					if(IsBlockOwnerNPC(Block) == false){
						
						return false;
						
					}
					
					if(IsBlockOwnerCorruption(Block) == false){
						
						var result = ConvertGridToNeutralNPC((VRage.Game.ModAPI.IMyCubeGrid)Block.CubeGrid);
						return false;
						
					}
					
					return true;
					
				};
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyProgrammableBlock>(corruptionOwnerCheck);
				
				//PB Control - Corruption Ice Refill
				var corruptionIceRefill = MyAPIGateway.TerminalControls.CreateProperty<bool, Sandbox.ModAPI.Ingame.IMyProgrammableBlock>("CorruptionIceRefill");
				corruptionIceRefill.Enabled = Block => true;
				corruptionIceRefill.Getter = Block => {
					
					if(IsBlockOwnerNPC(Block) == false){
						
						return false;
						
					}
					
					var results = IceRefill((VRage.Game.ModAPI.IMyCubeGrid)Block.CubeGrid);
					return results;
					
				};
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyProgrammableBlock>(corruptionIceRefill);
				
				
				//PB Control - Corruption Teleport
				var corruptionTeleport = MyAPIGateway.TerminalControls.CreateProperty<bool, Sandbox.ModAPI.Ingame.IMyProgrammableBlock>("CorruptionTeleport");
				corruptionTeleport.Enabled = Block => true;
				corruptionTeleport.Getter = Block => {
					
					if(IsBlockOwnerNPC(Block) == false){
						
						return false;
						
					}
					
					var results = DroneTeleport((Sandbox.ModAPI.Ingame.IMyTerminalBlock)Block);
					return results;
					
				};
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyProgrammableBlock>(corruptionTeleport);
				
				//PB Control - Corruption Drone Hacking
				var corruptionHacking = MyAPIGateway.TerminalControls.CreateProperty<bool, Sandbox.ModAPI.Ingame.IMyProgrammableBlock>("CorruptionHacking");
				corruptionHacking.Enabled = Block => true;
				corruptionHacking.Getter = Block => {
					
					if(IsBlockOwnerNPC(Block) == false){
						
						return false;
						
					}
					
					var results = DroneHackingAttempt((Sandbox.ModAPI.Ingame.IMyTerminalBlock)Block);
					return results;
					
				};
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyProgrammableBlock>(corruptionHacking);
				
				//PB Control - Corruption Drone Hacking Targets
				var corruptionHackingTargets = MyAPIGateway.TerminalControls.CreateProperty<List<long>, Sandbox.ModAPI.Ingame.IMyProgrammableBlock>("CorruptionHackingTargets");
				corruptionHackingTargets.Enabled = Block => true;
				corruptionHackingTargets.Getter = Block => {
					
					if(IsBlockOwnerNPC(Block) == false){
						
						return new List<long>();
						
					}
					
					var results = DroneHackingTargets((Sandbox.ModAPI.Ingame.IMyTerminalBlock)Block);
					return results;
					
				};
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyProgrammableBlock>(corruptionHackingTargets);
				
				//PB Control - Corruption Spawning
				var corruptionSpawning = MyAPIGateway.TerminalControls.CreateProperty<bool, Sandbox.ModAPI.Ingame.IMyProgrammableBlock>("CorruptionSpawning");
				corruptionSpawning.Enabled = Block => true;
				corruptionSpawning.Getter = Block => {
					
					if(IsBlockOwnerNPC(Block) == false){
						
						return false;
						
					}
					
					var results = SpawnReinforcements((Sandbox.ModAPI.Ingame.IMyTerminalBlock)Block);
					return results;
					
				};
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyProgrammableBlock>(corruptionSpawning);
				
				//PB Control - Check Shield Mod
				var corruptionShieldMod = MyAPIGateway.TerminalControls.CreateProperty<bool, Sandbox.ModAPI.Ingame.IMyProgrammableBlock>("CorruptionShieldMod");
				corruptionShieldMod.Enabled = Block => true;
				corruptionShieldMod.Getter = Block => {
					
					if(IsBlockOwnerNPC(Block) == false){
						
						return false;
						
					}
					
					return shieldModEnabled;
					
				};
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyProgrammableBlock>(corruptionShieldMod);
				
				//PB Control - Meteor Launch
				var corruptionMeteorLaunch = MyAPIGateway.TerminalControls.CreateProperty<bool, Sandbox.ModAPI.Ingame.IMyProgrammableBlock>("CorruptionMeteorLaunch");
				corruptionMeteorLaunch.Enabled = Block => true;
				corruptionMeteorLaunch.Getter = Block => {
					
					if(IsBlockOwnerNPC(Block) == false){
						
						return false;
						
					}
					
					return CreateAndLaunchMeteor(Block);
					
				};
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyProgrammableBlock>(corruptionMeteorLaunch);
				
				//PB Control - Build Projections
				var corruptionProjectorBuild = MyAPIGateway.TerminalControls.CreateProperty<bool, Sandbox.ModAPI.Ingame.IMyProgrammableBlock>("CorruptionProjectorBuild");
				corruptionProjectorBuild.Enabled = Block => true;
				corruptionProjectorBuild.Getter = Block => {
					
					if(IsBlockOwnerNPC(Block) == false){
						
						return false;
						
					}
					
					return BuildProjectedBlocks(Block);
					
				};
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyProgrammableBlock>(corruptionProjectorBuild);
				
				//PB Control - Repair Blocks
				var corruptionRepairBlocks = MyAPIGateway.TerminalControls.CreateProperty<bool, Sandbox.ModAPI.Ingame.IMyProgrammableBlock>("CorruptionRepairBlocks");
				corruptionRepairBlocks.Enabled = Block => true;
				corruptionRepairBlocks.Getter = Block => {
					
					if(IsBlockOwnerNPC(Block) == false){
						
						return false;
						
					}
					
					return RepairBlocks(Block);
					
				};
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyProgrammableBlock>(corruptionRepairBlocks);
				
				//PB Control - Instant Rotate
				var corruptionInstantRotate = MyAPIGateway.TerminalControls.CreateProperty<bool, Sandbox.ModAPI.Ingame.IMyProgrammableBlock>("CorruptionInstantRotate");
				corruptionInstantRotate.Enabled = Block => true;
				corruptionInstantRotate.Getter = Block => {
					
					if(IsBlockOwnerNPC(Block) == false){
						
						return false;
						
					}
					
					return InstantRotateToTarget(Block);
					
				};
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyProgrammableBlock>(corruptionInstantRotate);
				
				//PB Control - Nearest Planet
				var corruptionNearestPlanet = MyAPIGateway.TerminalControls.CreateProperty<Vector3D, Sandbox.ModAPI.Ingame.IMyProgrammableBlock>("CorruptionNearestPlanet");
				corruptionNearestPlanet.Enabled = Block => true;
				corruptionNearestPlanet.Getter = Block => {
					
					if(IsBlockOwnerNPC(Block) == false){
						
						return new Vector3D(0,0,0);
						
					}
					
					return NearestPlanetLocation(Block);
					
				};
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyProgrammableBlock>(corruptionNearestPlanet);
				
				
				//PB Control - Calculate Grid Health
				var corruptionGridHealth = MyAPIGateway.TerminalControls.CreateProperty<float, Sandbox.ModAPI.Ingame.IMyProgrammableBlock>("CorruptionGridHealth");
				corruptionGridHealth.Enabled = Block => true;
				corruptionGridHealth.Getter = Block => {
					
					if(IsBlockOwnerNPC(Block) == false){
						
						return 0.0f;
						
					}
					
					return CalculateGridHealth(Block);
					
				};
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyProgrammableBlock>(corruptionGridHealth);
				
				//PB Control - Sniper Reload
				var corruptionSniperReload = MyAPIGateway.TerminalControls.CreateProperty<bool, Sandbox.ModAPI.Ingame.IMyProgrammableBlock>("CorruptionSniperReload");
				corruptionSniperReload.Enabled = Block => true;
				corruptionSniperReload.Getter = Block => {
					
					if(IsBlockOwnerNPC(Block) == false){
						
						return false;
						
					}
					
					return SniperReload(Block);
					
				};
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyProgrammableBlock>(corruptionSniperReload);
				
				
				//PB Control - Test Laser Line
				var corruptionTestLaser = MyAPIGateway.TerminalControls.CreateProperty<bool, Sandbox.ModAPI.Ingame.IMyProgrammableBlock>("CorruptionTestLaser");
				corruptionTestLaser.Enabled = Block => true;
				corruptionTestLaser.Getter = Block => {
					
					if(IsBlockOwnerNPC(Block) == false){
						
						return false;
						
					}
					
					
					Vector4 color1 = Color.Red.ToVector4();
					Vector4 color2 = Color.Orange.ToVector4();
					Vector4 color3 = Color.Yellow.ToVector4();
					Vector4 color4 = Color.White.ToVector4();
					VRage.Game.MySimpleObjectDraw.DrawLine(Block.GetPosition(), Block.WorldMatrix.Forward * 100 + Block.GetPosition(), MyStringId.GetOrCompute("WeaponLaser"), ref color1, 1.7f);
					VRage.Game.MySimpleObjectDraw.DrawLine(Block.GetPosition(), Block.WorldMatrix.Forward * 100 + Block.GetPosition(), MyStringId.GetOrCompute("WeaponLaser"), ref color2, 1.4f);
					VRage.Game.MySimpleObjectDraw.DrawLine(Block.GetPosition(), Block.WorldMatrix.Forward * 100 + Block.GetPosition(), MyStringId.GetOrCompute("WeaponLaser"), ref color3, 1.1f);
					VRage.Game.MySimpleObjectDraw.DrawLine(Block.GetPosition(), Block.WorldMatrix.Forward * 100 + Block.GetPosition(), MyStringId.GetOrCompute("WeaponLaser"), ref color4, 0.8f);
					return true;
					
				};
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyProgrammableBlock>(corruptionTestLaser);
				
				//PB Control - Laser Request
				var corruptionDrawRequest = MyAPIGateway.TerminalControls.CreateProperty<bool, Sandbox.ModAPI.Ingame.IMyProgrammableBlock>("CorruptionLaserRequest");
				corruptionDrawRequest.Enabled = Block => true;
				corruptionDrawRequest.Getter = Block => {
					
					if(IsBlockOwnerNPC(Block) == false){
						
						return false;
						
					}
					
					return EnergyCannonFireRequest(Block);
					
				};
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyProgrammableBlock>(corruptionDrawRequest);
				
				//PB Control - Secondary Laser Draw
				var corruptionSecondaryDrawLaser = MyAPIGateway.TerminalControls.CreateProperty<bool, Sandbox.ModAPI.Ingame.IMyProgrammableBlock>("CorruptionSecondaryDrawLaser");
				corruptionSecondaryDrawLaser.Enabled = Block => true;
				corruptionSecondaryDrawLaser.Getter = Block => {
					
					if(IsBlockOwnerNPC(Block) == false){
						
						return false;
						
					}
					
					return SecondaryLaserFire(Block);
					
				};
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyProgrammableBlock>(corruptionSecondaryDrawLaser);
				
				//PB Control - Laser Damage
				var corruptionLaserDamage = MyAPIGateway.TerminalControls.CreateProperty<bool, Sandbox.ModAPI.Ingame.IMyProgrammableBlock>("CorruptionLaserDamage");
				corruptionLaserDamage.Enabled = Block => true;
				corruptionLaserDamage.Getter = Block => {
					
					if(IsBlockOwnerNPC(Block) == false){
						
						return false;
						
					}
					
					return LaserAttackHit(Block);
					
				};
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyProgrammableBlock>(corruptionLaserDamage);
				
				
				//PB Control - Tesla Fire
				var corruptionTeslaFire = MyAPIGateway.TerminalControls.CreateProperty<bool, Sandbox.ModAPI.Ingame.IMyProgrammableBlock>("CorruptionTeslaFire");
				corruptionTeslaFire.Enabled = Block => true;
				corruptionTeslaFire.Getter = Block => {
					
					if(IsBlockOwnerNPC(Block) == false){
						
						return false;
						
					}
					
					return TeslaRequest(Block);
					
				};
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyProgrammableBlock>(corruptionTeslaFire);
				
				
				//PB Control - Invincibility
				var corruptionInvincibility = MyAPIGateway.TerminalControls.CreateProperty<bool, Sandbox.ModAPI.Ingame.IMyProgrammableBlock>("CorruptionInvincibility");
				corruptionInvincibility.Enabled = Block => true;
				corruptionInvincibility.Getter = Block => {
					
					if(IsBlockOwnerNPC(Block) == false){
						
						return false;
						
					}
					
					return InvincibilityToggle(Block);
					
				};
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyProgrammableBlock>(corruptionInvincibility);
				
				//PB Control - Spawn Boss Group
				var corruptionBossSpawn = MyAPIGateway.TerminalControls.CreateProperty<bool, Sandbox.ModAPI.Ingame.IMyProgrammableBlock>("CorruptionBossSpawn");
				corruptionBossSpawn.Enabled = Block => true;
				corruptionBossSpawn.Getter = Block => {
					
					if(IsBlockOwnerNPC(Block) == false){
						
						return false;
						
					}
					
					return TryBossSpawn(Block);
					
				};
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyProgrammableBlock>(corruptionBossSpawn);
				
				//PB Control - Corruption Beam Register Effect
				var corruptionBeamEffectRequest = MyAPIGateway.TerminalControls.CreateProperty<bool, Sandbox.ModAPI.Ingame.IMyProgrammableBlock>("CorruptionBeamEffectRequest");
				corruptionBeamEffectRequest.Enabled = Block => true;
				corruptionBeamEffectRequest.Getter = Block => {
					
					if(IsBlockOwnerNPC(Block) == false){
						
						return false;
						
					}
					
					return EnergyCannonFireRequest(Block);
					
				};
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyProgrammableBlock>(corruptionBeamEffectRequest);
				
				//PB Control - Track Entity Position
				var corruptionTrackEntity = MyAPIGateway.TerminalControls.CreateProperty<Vector3D, Sandbox.ModAPI.Ingame.IMyProgrammableBlock>("CorruptionTrackEntity");
				corruptionTrackEntity.Enabled = Block => true;
				corruptionTrackEntity.Getter = Block => {
					
					if(IsBlockOwnerNPC(Block) == false){
						
						return new Vector3D(0,0,0);
						
					}
					
					return TrackEntityPosition(Block);
					
				};
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyProgrammableBlock>(corruptionTrackEntity);
				
				//PB Control - Get Nearest Player Threat
				var corruptionNearestPlayerThreat = MyAPIGateway.TerminalControls.CreateProperty<long, Sandbox.ModAPI.Ingame.IMyProgrammableBlock>("CorruptionNearestPlayerThreat");
				corruptionNearestPlayerThreat.Enabled = Block => true;
				corruptionNearestPlayerThreat.Getter = Block => {
					
					if(IsBlockOwnerNPC(Block) == false){
						
						return 0;
						
					}
					
					return GetNearestPlayerThreat(Block);
					
				};
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyProgrammableBlock>(corruptionNearestPlayerThreat);
				
				
				//PB Control - Get Nearest Shield Coordinator
				var corruptionNearestShieldDrone = MyAPIGateway.TerminalControls.CreateProperty<long, Sandbox.ModAPI.Ingame.IMyProgrammableBlock>("CorruptionNearestShieldDrone");
				corruptionNearestShieldDrone.Enabled = Block => true;
				corruptionNearestShieldDrone.Getter = Block => {
					
					if(IsBlockOwnerNPC(Block) == false){
						
						return 0;
						
					}
					
					return GetNearestShieldCoordinator(Block);
					
				};
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyProgrammableBlock>(corruptionNearestShieldDrone);
				
				//PB Control - Corruption Reset Terminal Names
				var corruptionResetTerminalNames = MyAPIGateway.TerminalControls.CreateProperty<bool, Sandbox.ModAPI.Ingame.IMyProgrammableBlock>("CorruptionResetTerminalNames");
				corruptionResetTerminalNames.Enabled = Block => true;
				corruptionResetTerminalNames.Getter = Block => {
					
					return ResetTerminalNames(Block);
					
				};
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyProgrammableBlock>(corruptionResetTerminalNames);
				
				//PB Control - Corruption Player Ship Clone
				var corruptionClonePlayerShip = MyAPIGateway.TerminalControls.CreateProperty<bool, Sandbox.ModAPI.Ingame.IMyProgrammableBlock>("CorruptionClonePlayerShip");
				corruptionClonePlayerShip.Enabled = Block => true;
				corruptionClonePlayerShip.Getter = Block => {
					
					if(IsBlockOwnerNPC(Block) == false){
						
						return false;
						
					}
					
					return ClonePlayerGrid(Block);
					
				};
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyProgrammableBlock>(corruptionClonePlayerShip);
				
				//PB Control - Corruption Shield Register
				var corruptionShieldRegister = MyAPIGateway.TerminalControls.CreateProperty<bool, Sandbox.ModAPI.Ingame.IMyProgrammableBlock>("CorruptionShieldRegister");
				corruptionShieldRegister.Enabled = Block => true;
				corruptionShieldRegister.Getter = Block => {
					
					if(IsBlockOwnerNPC(Block) == false){
						
						return false;
						
					}
					
					return ShieldRegister(Block);
					
				};
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyProgrammableBlock>(corruptionShieldRegister);
				
				//PB Control - Corruption Missile Creation
				var corruptionMissileCreation = MyAPIGateway.TerminalControls.CreateProperty<bool, Sandbox.ModAPI.Ingame.IMyProgrammableBlock>("CorruptionMissileCreation");
				corruptionMissileCreation.Enabled = Block => true;
				corruptionMissileCreation.Getter = Block => {
					
					if(IsBlockOwnerNPC(Block) == false){
						
						return false;
						
					}
					
					return CreateSeekerMissile(Block);
					
				};
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyProgrammableBlock>(corruptionMissileCreation);
				
				/*
				//PB Control - Drone Rotate
				var corruptionDroneRotate = MyAPIGateway.TerminalControls.CreateProperty<bool, Sandbox.ModAPI.Ingame.IMyProgrammableBlock>("CorruptionDroneRotate");
				corruptionDroneRotate.Enabled = Block => true;
				corruptionDroneRotate.Getter = Block => {
					
					if(IsBlockOwnerNPC(Block) == false){
						
						return false;
						
					}
					
					return RotateDroneToTarget(Block);
					
				};
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyProgrammableBlock>(corruptionDroneRotate);
				*/
				
				MyAPIGateway.Utilities.InvokeOnGameThread(() => entity.Delete());
				
			}
			
			scriptInitialized = true;
			
			TeslaEffectFire();
			EnergyCannonEffectFire();
			
						
			if(MyAPIGateway.Multiplayer.IsServer == true){
				
				if(processPBs == true){
					
					if(pbRecompileList.Count == 0){
						
						processPBs = false;
						
					}
					
					for(int i = pbRecompileList.Count - 1; i >= 0; i--){
						
						if(pbRecompileList[i].IsRunning == false){
							
							pbRecompileList[i].Recompile();
							pbRecompileList[i].Run();
							pbRecompileList.RemoveAt(i);
							
						}
						
					}
					
				}
				
				if(overloadShields == true){
					
					if(shieldOverloadList.Count == 0){
						
						overloadShields = true;
						
					}
					
					var npcFaction = MyAPIGateway.Session.Factions.TryGetFactionByTag(npcCorruptionTag);
					var ownerFaction = npcFaction.FounderId;
					
					for(int i = shieldOverloadList.Count - 1; i >= 0; i--){
						
						var blockEntity = shieldOverloadList[i] as IMyEntity;
						MyVisualScriptLogicProvider.SetName(blockEntity.EntityId, blockEntity.EntityId.ToString());
						MyVisualScriptLogicProvider.DamageBlock(blockEntity.Name, 100, ownerFaction);
						
						if(shieldOverloadList[i].IsFunctional == false){
							
							shieldOverloadList.RemoveAt(i);
							
						}
						
					}
					
				}
				
				//Marauder Encounter
				if(betaMode == true){
					
					MarauderEncounterManager();
					ProcessActiveMissiles();
					
				}
				
				//Shield Grid Manager
				ShieldTimeout();
				

			}
			
		}
		
		void CorruptionChatCommand(string messageText, ref bool sendToOthers){
				
			var thisPlayer = MyAPIGateway.Session.Player;
			
			if(thisPlayer == null){
				
				return;
				
			}
		
			if(thisPlayer.PromoteLevel == MyPromoteLevel.Admin || thisPlayer.PromoteLevel == MyPromoteLevel.Owner){
				
				//Add Spawn Group
				if(messageText.StartsWith("/CPC")){
					
					sendToOthers = false;
					var sendData = MyAPIGateway.Utilities.SerializeToBinary<string>(messageText + "\n" + thisPlayer.Character.EntityId.ToString());
					bool sendStatus = MyAPIGateway.Multiplayer.SendMessageToServer(6456, sendData);
					return;
					
				}
								
			}
						
		}
		
		void AdminCommand(byte[] data){
			
			var receivedData = MyAPIGateway.Utilities.SerializeFromBinary<string>(data);
			
			if(receivedData.StartsWith("/CPCAddSpawnGroup")){
				
				AddSpawnGroup(receivedData);
				
			}
			
			if(receivedData.StartsWith("/CPCDebugMode")){
				
				if(receivedData.Contains("On")){
					
					debugMode = true;
					
				}else{
					
					debugMode = false;
					
				}
				
			}
			
			if(receivedData.StartsWith("/CPCBetaMode")){
				
				if(receivedData.Contains("On")){
					
					if(betaMode == false){
						
						MyVisualScriptLogicProvider.ShowNotificationToAll("Experimental Features Enabled", 10000);
						
					}
					
					betaMode = true;
										
				}else{
					
					if(betaMode == true){
						
						MyVisualScriptLogicProvider.ShowNotificationToAll("Experimental Features Disabled", 10000);
						
					}
					
					betaMode = false;
					
				}
				
			}
			
			if(receivedData.StartsWith("/CPCActivateBossEncounters")){
				
				encounterCreateTimer = encounterCreateTimerTrigger - 3;
				
			}
						
		}
		
		void EnergyCannonRegister(byte[] data){
			
			var receivedData = MyAPIGateway.Utilities.SerializeFromBinary<string>(data);
			long entityId = 0;
			
			if(long.TryParse(receivedData, out entityId) == false){
				
				return;
				
			}
			
			energyCannonSourceEntity.Add(entityId);
			energyCannonTimers.Add(0);
			
		}
		
		bool EnergyCannonFireRequest(Sandbox.ModAPI.Ingame.IMyTerminalBlock pb){
			
			var playerList = new List<IMyPlayer>();
			MyAPIGateway.Players.GetPlayers(playerList);
			
			foreach(var player in playerList){
			
				if(player.IsBot == true || player.Character == null){
				
					continue;
				
				}
				
				if(player.Character.IsDead == true){
				
					continue;
				
				}

				if(MeasureDistance(player.GetPosition(), pb.GetPosition()) < 5000){
					
					var sendData = MyAPIGateway.Utilities.SerializeToBinary<string>(pb.CustomData);
					bool sendClientStatus = MyAPIGateway.Multiplayer.SendMessageTo(6716, sendData, player.SteamUserId);
				
				}

			}
			
			return true;
			
		}
		
		void DebugBeamTest(){
			
			var playerList = new List<IMyPlayer>();
			MyAPIGateway.Players.GetPlayers(playerList);
			
			foreach(var player in playerList){
				
				if(player.IsBot == true || player.Character == null){
					
					continue;
					
				}
				
				Vector4 color1 = Color.Red.ToVector4();
				Vector3D pointA = player.GetPosition();
				Vector3D pointB = player.Character.WorldMatrix.Up * 50 + player.GetPosition();
				VRage.Game.MySimpleObjectDraw.DrawLine(pointA, pointB, MyStringId.GetOrCompute("WeaponLaser"), ref color1, 0.2f);
				
			}
			
		}
		
		void AddSpawnGroup(string message){
			
			//example msg: /CPCAddSpawnGroup|GROUP_NAME|5000
			var dataSplit = message.Split('\n');
			
			if(dataSplit.Length != 2){
				
				return;
				
			}

			var msgSplit = dataSplit[0].Split('|');
			
			if(msgSplit.Length != 3){
				
				return;
				
			}
			
			long characterEntityId = 0;
			IMyEntity characterEntity = null;
			double spawnDistance = 0;
			
			if(long.TryParse(dataSplit[1], out characterEntityId) == false){
				
				return;
				
			}
			
			if(MyAPIGateway.Entities.TryGetEntityById(characterEntityId, out characterEntity) == false){
				
				return;
				
			}
			
			if(double.TryParse(msgSplit[2], out spawnDistance) == false){
				
				return;
				
			}
			
			bool foundSpawnGroup = false;
			var spawningMatrix = MatrixD.CreateWorld(characterEntity.WorldMatrix.Forward * spawnDistance + characterEntity.GetPosition(), characterEntity.WorldMatrix.Forward, characterEntity.WorldMatrix.Up);
			var	spawnGroups = MyDefinitionManager.Static.GetSpawnGroupDefinitions();
			var npcFaction = MyAPIGateway.Session.Factions.TryGetFactionByTag(npcCorruptionTag);
			var ownerFaction = npcFaction.FounderId;
			var prefabNameList = new List<string>();
			var prefabCoordsList = new List<Vector3D>();
			var prefabBeaconList = new List<string>();
			
			foreach(var spawnGroup in spawnGroups){
				
				if(spawnGroup.Id.SubtypeName == msgSplit[1]){
					
					foundSpawnGroup = true;
					
					foreach(var prefab in spawnGroup.Prefabs){
												
						prefabNameList.Add(prefab.SubtypeId);
						prefabCoordsList.Add(Vector3D.Transform((Vector3D)prefab.Position, spawningMatrix));
						prefabBeaconList.Add(prefab.BeaconText);
						
					}
					
					break;
					
				}
				
			}
						
			if(foundSpawnGroup == false || prefabNameList.Count == 0){
				
				LogEntry("Debug: Couldn't find SpawnGroup");
				return;
				
			}
			
			var tempSpawningList = new List<IMyCubeGrid>();
			
			for(int i = 0; i < prefabNameList.Count; i++){
				
				MyAPIGateway.PrefabManager.SpawnPrefab(tempSpawningList, prefabNameList[i], prefabCoordsList[i], characterEntity.WorldMatrix.Forward, characterEntity.WorldMatrix.Up, new Vector3(0,0,0), spawningOptions: SpawningOptions.RotateFirstCockpitTowardsDirection | SpawningOptions.SpawnRandomCargo, beaconName: prefabBeaconList[i], ownerId: ownerFaction, updateSync: false);
				
			}
			
			
		}
		
		void EnergyCannonEffectFire(){
			
			if(energyCannonSourceEntity.Count == 0 || energyCannonTimers.Count == 0){
				
				return;
				
			}
			
			//Check if lists all have same count
			if(energyCannonSourceEntity.Count != energyCannonTimers.Count){
				
				badLaserLists++;
				
				if(badLaserLists >= 10){
					
					badLaserLists = 0;
					energyCannonSourceEntity.Clear();
					energyCannonTimers.Clear();
					
				}
				
				return;
				
			}
			
			for(int i = energyCannonSourceEntity.Count - 1; i >= 0; i--){
				
				if(energyCannonTimers[i] >= 60){
					
					energyCannonSourceEntity.RemoveAt(i);
					energyCannonTimers.RemoveAt(i);
					continue;
					
				}
				
				IMyEntity entity = null;
				
				if(MyAPIGateway.Entities.TryGetEntityById(energyCannonSourceEntity[i], out entity) == false){
					
					energyCannonSourceEntity.RemoveAt(i);
					energyCannonTimers.RemoveAt(i);
					continue;
					
				}
				
				var camera = entity as Sandbox.ModAPI.Ingame.IMyCameraBlock;
				
				if(camera == null){
					
					energyCannonSourceEntity.RemoveAt(i);
					energyCannonTimers.RemoveAt(i);
					continue;
					
				}
				
				energyCannonTimers[i]++;
								
				var coordsA = camera.GetPosition();
				var coordsB = camera.WorldMatrix.Forward * 4000 + camera.GetPosition();
				var hitInfoList = new List<IHitInfo>();
				MyAPIGateway.Physics.CastRay(coordsA, coordsB, hitInfoList);
				
				foreach(var hit in hitInfoList){
					
					if(hit.HitEntity == camera.CubeGrid as IMyEntity){
						
						continue;
						
					}
					
					coordsB = hit.Position;
					break;
					
				}
				
				if(energyCannonTimers[i] == 1){
					
					MyVisualScriptLogicProvider.PlaySingleSoundAtPosition("ArcEnergyCannonShot", (Vector3)coordsA);
					MyVisualScriptLogicProvider.PlaySingleSoundAtPosition("ArcEnergyCannonShot", (Vector3)coordsB);
					
				}
				
				LaserFire(coordsA, coordsB);
				
			}
			
		}
		
		void TeslaEffectFire(){
			
			
			//Check if lists are empty
			if(teslaSourceEntity.Count == 0 || teslaTargetEntity.Count == 0 || teslaEffectsLists.Count == 0 || teslaEffectsTimers.Count == 0 || teslaEffectsTotalTimers.Count == 0){
				
				return;
				
			}
			
			//Check if lists all have same count
			if(teslaTargetEntity.Count != teslaSourceEntity.Count || teslaEffectsLists.Count != teslaSourceEntity.Count || teslaEffectsTimers.Count != teslaSourceEntity.Count || teslaEffectsTotalTimers.Count != teslaSourceEntity.Count){
				
				badLists++;
				
				if(badLists >= 10){
					
					badLists = 0;
					teslaTargetEntity.Clear();
					teslaSourceEntity.Clear();
					teslaEffectsLists.Clear();
					teslaEffectsTimers.Clear();
					teslaEffectsTotalTimers.Clear();
					
				}
				
				return;
				
			}
			
			badLists = 0;
			
			for(int i = teslaSourceEntity.Count - 1; i >= 0; i--){
				
				if(teslaEffectsTotalTimers[i] >= 60){
					
					teslaTargetEntity.RemoveAt(i);
					teslaSourceEntity.RemoveAt(i);
					teslaEffectsLists.RemoveAt(i);
					teslaEffectsTimers.RemoveAt(i);
					teslaEffectsTotalTimers.RemoveAt(i);
					continue;
					
				}
				
				teslaEffectsTimers[i]++;
				
				var startCoords = new Vector3D(0,0,0);
				var endCoords = new Vector3D(0,0,0);
				IMyEntity cameraEntity = null;
				IMyEntity targetEntity = null;
				
				if(MyAPIGateway.Entities.TryGetEntityById(teslaSourceEntity[i], out cameraEntity) == false || MyAPIGateway.Entities.TryGetEntityById(teslaTargetEntity[i], out targetEntity) == false){
					
					teslaTargetEntity.RemoveAt(i);
					teslaSourceEntity.RemoveAt(i);
					teslaEffectsLists.RemoveAt(i);
					teslaEffectsTimers.RemoveAt(i);
					teslaEffectsTotalTimers.RemoveAt(i);
					continue;
					
				}
				
				startCoords = cameraEntity.GetPosition();
				endCoords = cameraEntity.WorldMatrix.Forward * MeasureDistance(cameraEntity.GetPosition(), targetEntity.GetPosition()) + cameraEntity.GetPosition();
				
				
				if(teslaEffectsTimers[i] >= 2 || teslaEffectsLists[i].Count == 0){
					
					teslaEffectsTimers[i] = 0;
					teslaEffectsLists[i].Clear();
					bool finalLeg = false;
					
					while(finalLeg == false){
						
						var forwardDir = Vector3D.Normalize(endCoords - startCoords);
						var upDir = Vector3D.CalculatePerpendicularVector(forwardDir);
						var beamMatrix = MatrixD.CreateWorld(startCoords, forwardDir, upDir);
						var offset = new Vector3D(0,0,0);
						
						if(teslaEffectsLists[i].Count == 0){
							
							forwardDir = Vector3D.Normalize(endCoords - startCoords);
							upDir = Vector3D.CalculatePerpendicularVector(forwardDir);
							beamMatrix = MatrixD.CreateWorld(startCoords, forwardDir, upDir);
							offset = new Vector3D(0,0,0);
							offset.X = (double)rnd.Next(-2, 3);
							offset.Y = (double)rnd.Next(-2, 3);
							offset.Z = (double)rnd.Next(-15, -5);
							teslaEffectsLists[i].Add(Vector3D.Transform(offset, beamMatrix));
							continue;
							
						}
						
						if(MeasureDistance(teslaEffectsLists[i][teslaEffectsLists[i].Count - 1], endCoords) <= 15){
							
							teslaEffectsLists[i].Add(endCoords);
							finalLeg = true;
							break;
							
						}
						
						forwardDir = Vector3D.Normalize(endCoords - teslaEffectsLists[i][teslaEffectsLists[i].Count - 1]);
						upDir = Vector3D.CalculatePerpendicularVector(forwardDir);
						beamMatrix = MatrixD.CreateWorld(teslaEffectsLists[i][teslaEffectsLists[i].Count - 1], forwardDir, upDir);
						offset = new Vector3D(0,0,0);
						offset.X = (double)rnd.Next(-2, 3);
						offset.Y = (double)rnd.Next(-2, 3);
						offset.Z = (double)rnd.Next(-15, -5);
						teslaEffectsLists[i].Add(Vector3D.Transform(offset, beamMatrix));
						
					}
					
				}
				
				var secondaryLaserPointList = teslaEffectsLists[i];
				
				if(teslaEffectsTotalTimers[i] == 0){
					
					MyVisualScriptLogicProvider.PlaySingleSoundAtPosition("ArcParticleElectricalDischarge", (Vector3)endCoords);
					MyVisualScriptLogicProvider.PlaySingleSoundAtPosition("ArcParticleElectricalDischarge", (Vector3)endCoords);
					MyVisualScriptLogicProvider.PlaySingleSoundAtPosition("ArcParticleElectricalDischarge", (Vector3)endCoords);
					MyVisualScriptLogicProvider.CreateParticleEffectAtPosition("Damage_Electrical_Damaged", endCoords);
					
				}
				
				for(int j = 0; j < secondaryLaserPointList.Count; j++){
					
					bool drawLaser = false;
					var pointA = new Vector3D(0,0,0);
					var pointB = new Vector3D(0,0,0);
					Vector4 color1 = Color.White.ToVector4();
					
					if(secondaryLaserPointList.Count <= 2){
						
						pointA = startCoords;
						pointB = secondaryLaserPointList[secondaryLaserPointList.Count - 1];
						VRage.Game.MySimpleObjectDraw.DrawLine(pointA, pointB, MyStringId.GetOrCompute("WeaponLaser"), ref color1, 0.2f);
						break;
						
					}
					
					if(j == 0){
						
						pointA = startCoords;
						pointB = secondaryLaserPointList[0];
						VRage.Game.MySimpleObjectDraw.DrawLine(pointA, pointB, MyStringId.GetOrCompute("WeaponLaser"), ref color1, 0.2f);
						continue;
						
					}
					
					if(j == secondaryLaserPointList.Count - 2){
						
						pointA = secondaryLaserPointList[j];
						pointB = secondaryLaserPointList[j + 1];
						VRage.Game.MySimpleObjectDraw.DrawLine(pointA, pointB, MyStringId.GetOrCompute("WeaponLaser"), ref color1, 0.2f);
						break;
						
					}
					
					pointA = secondaryLaserPointList[j];
					pointB = secondaryLaserPointList[j + 1];
					VRage.Game.MySimpleObjectDraw.DrawLine(pointA, pointB, MyStringId.GetOrCompute("WeaponLaser"), ref color1, 0.2f);
					
				}

				teslaEffectsTotalTimers[i]++;
				
			}
			
		}
		
		bool DroneChat(Sandbox.ModAPI.Ingame.IMyTerminalBlock block){
			
			var cubeGrid = (VRage.Game.ModAPI.IMyCubeGrid)block.CubeGrid;
			string chatColor = "Red";
			string gridName = cubeGrid.CustomName;
			gridName = gridName.Replace("(NPC-CPC) ", "");
			
			if(gridName == "Damaged Research Vessel"){
				
				chatColor = "Green";
				
			}
			
			string message = block.CustomData;
			block.CustomData = "";
			List<IMyPlayer> playerList = new List<IMyPlayer>();
			MyAPIGateway.Players.GetPlayers(playerList);
			double distance = 0;
			
			var gts = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(cubeGrid);
			List<Sandbox.ModAPI.Ingame.IMyTerminalBlock> blockList = new List<Sandbox.ModAPI.Ingame.IMyTerminalBlock>();
			gts.GetBlocksOfType<Sandbox.ModAPI.Ingame.IMyTerminalBlock>(blockList);
			
			foreach(var tblock in blockList){
				
				var antenna = tblock as Sandbox.ModAPI.Ingame.IMyRadioAntenna;
				
				if(antenna == null){
					
					continue;
					
				}
				
				if(antenna.IsFunctional == true && antenna.EnableBroadcasting == true && (double)antenna.Radius > distance && antenna.Enabled == true){
					
					distance = (double)antenna.Radius;
					
				}
				
			}
			
			foreach(var player in playerList){
				
				if(player.IsBot == true){
					
					continue;
					
				}
				
				if(MeasureDistance(player.GetPosition(), block.GetPosition()) < distance){
					
					MyVisualScriptLogicProvider.SendChatMessage(message.Replace("{Player}", player.DisplayName), gridName, player.IdentityId, chatColor);
					
				}
				
			}
			
			return true;
			
		}
		
		bool TryDespawn(IMyCubeGrid originGrid){
			
			//Get Grid Group
			var gridGroupList = MyAPIGateway.GridGroups.GetGroup(originGrid, GridLinkTypeEnum.Mechanical);
			foreach(var cubeGrid in gridGroupList){
				
				var bigOwners = cubeGrid.BigOwners;
				
				if(bigOwners.Count == 0){
					
					//Owned by Nobody
					return false;
					
				}
				
				foreach(var owner in bigOwners){
					
					if(owner == 0){
						
						continue;
						
					}
					
					if(npcFactionOwners.Contains(owner)){
						
						var faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(owner);
						
						if(faction != null){
							
							if(faction.IsEveryoneNpc() == true){
								
								continue;
								
							}
							
						}
						
					}
					
					return false;
								
				}
				
			}
			
			//From here, all grids are considered NPC and will now be deleted.
			foreach(var cubeGrid in gridGroupList){
				
				LogEntry("Deleting Drone: " + cubeGrid.CustomName);
				MyAPIGateway.Utilities.InvokeOnGameThread(() => cubeGrid.Delete());
				
			}

			return true;
			
		}
		
		bool IsBlockOwnerNPC(Sandbox.ModAPI.Ingame.IMyTerminalBlock block){
			
			foreach(var faction in npcfactions){
				
				var ownerFaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(block.OwnerId);
				if(ownerFaction == faction && ownerFaction.IsEveryoneNpc() == true){
					
					return true;
					
				}
				
			}
			
			return false;
			
		}
		
		bool IsBlockOwnerCorruption(Sandbox.ModAPI.Ingame.IMyTerminalBlock block){
			
			var npcFaction = MyAPIGateway.Session.Factions.TryGetFactionByTag(npcCorruptionTag);
			var ownerFaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(block.OwnerId);
			
			if(npcFaction == ownerFaction){
				
				return true;
				
			}
			
			return false;
			
		}
		
		bool ConvertGridToNeutralNPC(IMyCubeGrid originGrid){
			
			var gridGroupList = MyAPIGateway.GridGroups.GetGroup(originGrid, GridLinkTypeEnum.Mechanical);
			
			//Get NPC Identity From Tag
			var npcFaction = MyAPIGateway.Session.Factions.TryGetFactionByTag(npcCorruptionTag);
			
			if(npcFaction == null){
				
				return false;
				
			}
			
			//Convert Each Grid To Neutral NPC
			foreach(var cubeGrid in gridGroupList){
				
				cubeGrid.ChangeGridOwnership(npcFaction.FounderId, MyOwnershipShareModeEnum.None);
				
			}
			
			RemoveGridAuthorship(originGrid);
			
			var gts = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(originGrid);
			List<Sandbox.ModAPI.Ingame.IMyTerminalBlock> blockList = new List<Sandbox.ModAPI.Ingame.IMyTerminalBlock>();
			gts.GetBlocksOfType<Sandbox.ModAPI.Ingame.IMyTerminalBlock>(blockList);
				
			foreach(var block in blockList){
				
				//Recompile PBs since we did an ownership change.
				var pb = block as Sandbox.ModAPI.IMyProgrammableBlock;
				if(pb != null){
					
					pbRecompileList.Add(pb);
					processPBs = true;
					
				}
				
				var antenna = block as Sandbox.ModAPI.Ingame.IMyRadioAntenna;
				if(antenna != null && originGrid.CustomName == "(NPC-CPC) Damaged Research Vessel" && block.CustomName == "Damaged Research Vessel"){
					
					var blockEntity = block as IMyEntity;
					var cubeBlock = blockEntity as MyCubeBlock;
					cubeBlock.ChangeOwner(0, MyOwnershipShareModeEnum.None);
					cubeBlock.ChangeBlockOwnerRequest(0, MyOwnershipShareModeEnum.None);
					antenna.Enabled = true;
					antenna.EnableBroadcasting = true;
					
				}
				
			}
			
			return true;
			
		}
		
		bool IceRefill(IMyCubeGrid cubeGrid){
			
			if(MyAPIGateway.Session.CreativeMode == true){
				
				return false;
				
			}
			
			float iceAmount = 0;
			
			if(cubeGrid.GridSizeEnum == MyCubeSize.Large){
				
				iceAmount = 10000;
				
			}else{
				
				iceAmount = 2700;
				
			}
			
			var gts = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(cubeGrid);
			List<Sandbox.ModAPI.Ingame.IMyGasGenerator> blockList = new List<Sandbox.ModAPI.Ingame.IMyGasGenerator>();
			gts.GetBlocksOfType<Sandbox.ModAPI.Ingame.IMyGasGenerator>(blockList);
			bool result = false;
			
			foreach(var block in blockList){
				
				if(block.IsFunctional == false || IsBlockOwnerNPC((Sandbox.ModAPI.Ingame.IMyTerminalBlock)block) == false){
					
					continue;
					
				}
				
				var inventory = (VRage.Game.ModAPI.IMyInventory)block.GetInventory(0);
				
				float amount = iceAmount;
				var definitionId = new MyDefinitionId(typeof(MyObjectBuilder_Ore), "Ice");
				var definition = MyDefinitionManager.Static.GetDefinition(definitionId);
				var amountMFP = (MyFixedPoint)amount;
				var content = (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(definitionId);
				MyObjectBuilder_InventoryItem inventoryItem = new MyObjectBuilder_InventoryItem { Amount = amountMFP, Content = content };
				if(inventory.CanItemsBeAdded(amountMFP, definitionId) == true){
						
					inventory.AddItems(amountMFP, inventoryItem.Content);
						
				}
				
				result = true;
				
			}
			
			return result;
			
		}
		
		bool DroneTeleport(Sandbox.ModAPI.Ingame.IMyTerminalBlock pb){
		
			var teleportPosition = new Vector3D(0,0,0);
			var targetPosition = new Vector3D(0,0,0);
			var targetVelocity = new Vector3D(0,0,0);
			var dataSplit = pb.CustomData.Split('\n');
			pb.CustomData = "";
			
			if(dataSplit.Length != 3){
			
				return false;
			
			}
			
			if(Vector3D.TryParse(dataSplit[0], out teleportPosition) == false || Vector3D.TryParse(dataSplit[1], out targetPosition) == false || Vector3D.TryParse(dataSplit[2], out targetVelocity) == false){
			
				return false;
			
			}
			
			var safeTeleportPosition = MyAPIGateway.Entities.FindFreePlace(teleportPosition, 50, 10, 3, 10);
			
			if(safeTeleportPosition == null){
				
				return false;
				
			}
			
			var cubeGrid = (VRage.Game.ModAPI.IMyCubeGrid)pb.CubeGrid;
			var forwardDir = Vector3D.Normalize(targetPosition - teleportPosition);
			var upDir = MyUtils.GetRandomPerpendicularVector(ref forwardDir);
			var positionMatrix = MatrixD.CreateWorld((Vector3D)safeTeleportPosition, forwardDir, upDir);
			EffectRequest(cubeGrid.GetPosition(), (Vector3D)safeTeleportPosition);
			cubeGrid.WorldMatrix = positionMatrix;
			cubeGrid.Physics.LinearVelocity = (Vector3)targetVelocity;
			cubeGrid.Physics.AngularVelocity = new Vector3(0,0,0);
			
			return true;
			
		}
		
		bool InstantRotateToTarget(Sandbox.ModAPI.Ingame.IMyTerminalBlock pb){
			
			//Get Target Vector
			if(pb.CustomData == ""){
				
				return false;
				
			}
			
			var dataSplit = pb.CustomData.Split('\n');
			pb.CustomData = "";
			
			if(dataSplit.Length != 2){
				
				return false;
				
			}
			
			Vector3D targetCoords = new Vector3D(0,0,0);
			Vector3D upDir = new Vector3D(0,0,0);
			
			if(Vector3D.TryParse(dataSplit[0], out targetCoords) == false || Vector3D.TryParse(dataSplit[1], out upDir) == false){
				
				return false;
				
			}
			
			var cubeGrid = (VRage.Game.ModAPI.IMyCubeGrid)pb.CubeGrid;
			var targetMatrix = MatrixD.CreateLookAt(cubeGrid.GetPosition(), targetCoords, (Vector3)upDir);
			var finalMatrix = Matrix.CreateWorld(cubeGrid.GetPosition(), targetMatrix.Forward, targetMatrix.Up);
			cubeGrid.WorldMatrix = finalMatrix;
			
			return true;
			
		}
		
		List<long> DroneHackingTargets(Sandbox.ModAPI.Ingame.IMyTerminalBlock pb){
			
			var resultList = new List<long>();
			var entityList = new HashSet<IMyEntity>();
			MyAPIGateway.Entities.GetEntities(entityList);
			
			foreach(var entity in entityList){
				
				var cubeGrid = entity as IMyCubeGrid;
				
				if(cubeGrid != null){
					
					if(MeasureDistance(pb.GetPosition(), cubeGrid.GetPosition()) <= 5000){
						
						resultList.Add(entity.EntityId);
						
					}
					
				}
				
			}
			
			return resultList;
			
		}
		
		bool DroneHackingAttempt(Sandbox.ModAPI.Ingame.IMyTerminalBlock pb){
			
			long gridEntityId = 0;
			var droneGrid = (VRage.Game.ModAPI.IMyCubeGrid)pb.CubeGrid;
			string hackingType = "";
			bool validAntenna = false;
			bool validOwner = false;
			var corruptionFaction = MyAPIGateway.Session.Factions.TryGetFactionByTag("CORRUPT");
			
			if(corruptionFaction == null){
				
				LogEntry("Debug: Corruption Faction Not Found");
				return false;
				
			}
			
			long npcOwner = corruptionFaction.FounderId;
			var dataSplit = pb.CustomData.Split('\n');
			pb.CustomData = "";
			
			if(dataSplit.Length != 2){
				
				LogEntry("Debug: CustomData Bad");
				return false;
				
			}
			
			if(long.TryParse(dataSplit[0], out gridEntityId) == false){
				
				LogEntry("Debug: EntityId not correctly formatted");
				return false;
				
			}
				
			hackingType = dataSplit[1];	
			IMyEntity gridEntity = null;
			
			if(MyAPIGateway.Entities.TryGetEntityById(gridEntityId, out gridEntity) == false){
				
				LogEntry("Debug: Couldn't Get Grid From EntityId");
				return false;
				
			}
			
			var cubeGrid = gridEntity as IMyCubeGrid;
			
			if(cubeGrid == droneGrid){
				
				LogEntry("Debug: Targeted Grid Is Drone");
				return false;
				
			}
			
			//Do Check On Drone Grid For Active Antenna
			if(CheckDroneAntenna(droneGrid, MeasureDistance(cubeGrid.GetPosition(), droneGrid.GetPosition())) == false){
				
				LogEntry("Debug: Drone Antenna Invalid or Out Of Range");
				return false;
				
			}
			
			//BigOwners Check To Ensure Grid Is Not Fully NPC
			var gridOwners = cubeGrid.BigOwners;
			if(gridOwners.Count == 0){
				
				LogEntry("Debug: Big Owners 0");
				return false;
				
			}
			
			foreach(var owner in gridOwners){
				
				if(owner != npcOwner && owner != 0){
					
					validOwner = true;
					
				}
				
			}
			
			if(validOwner == false){
				
				LogEntry("Debug: No Valid Owner");
				return false;
				
			}
			
			//Get Blocks
			var gts = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(cubeGrid);
			var blockList = new List<Sandbox.ModAPI.Ingame.IMyTerminalBlock>();
			gts.GetBlocksOfType<Sandbox.ModAPI.Ingame.IMyTerminalBlock>(blockList);
			var gridNameList = new List<string>();
			
			//Filter Blocks
			for(int i = blockList.Count - 1; i >= 0; i--){
				
				var block = blockList[i];
				
				if(gridNameList.Contains(block.CubeGrid.CustomName) == false){
					
					gridNameList.Add(block.CubeGrid.CustomName);
					
				}
				
				//Check If Functional
				if(block.IsFunctional == false){
					
					blockList.RemoveAt(i);
					continue;
					
				}
				
				//Check If Owned By NPC or Nobody
				if(block.OwnerId == npcOwner){
					
					blockList.RemoveAt(i);
					continue;
					
				}
				
				//Check If Antenna and Valid
				var antenna = block as Sandbox.ModAPI.Ingame.IMyRadioAntenna;
				if(antenna != null){
					
					if((double)antenna.Radius >= MeasureDistance(antenna.GetPosition(), pb.GetPosition()) && antenna.Enabled == true && antenna.EnableBroadcasting == true && antenna.IsWorking == true){
						
						validAntenna = true;
						LogEntry("Hacker Drone Found Active Antenna Within Range. Antenna Name: " + antenna.CustomName + ". Antenna Grid Name: " + antenna.CubeGrid.CustomName);
						
					}
					
					blockList.RemoveAt(i);
					continue;
					
				}
				
			}
			
			if(validAntenna == false || blockList.Count == 0){
				
				LogEntry("Debug: No Valid Antenna");
				return false;
				
			}
			
			string joinedName = string.Join<string>(",", gridNameList);
			LogEntry("Attached Grids Being Hacked: " + joinedName);
			
			//Different Hacking Types
			
			if(hackingType == "HackBlock"){
				
				for(int i = blockList.Count - 1; i >= 0; i--){
					
					var block = blockList[i];
					if(block.OwnerId == 0){
					
						blockList.RemoveAt(i);
						continue;
					
					}
					
				}
				
				var randomBlock = blockList[rnd.Next(0, blockList.Count)];
				var blockEntity = randomBlock as IMyEntity;
				var cubeBlock = blockEntity as MyCubeBlock;
				cubeBlock.ChangeOwner(npcOwner, MyOwnershipShareModeEnum.None);
				cubeBlock.ChangeBlockOwnerRequest(npcOwner, MyOwnershipShareModeEnum.None);
				return true;
				
			}
			
			if(hackingType == "DisableProduction"){
				
				foreach(var block in blockList){
					
					var prodBlock = block as Sandbox.ModAPI.Ingame.IMyProductionBlock;
					
					if(prodBlock != null){
						
						prodBlock.Enabled = false;
						continue;
						
					}
					
					var blockEntity = block as IMyEntity;
					var cubeBlock = blockEntity as IMyCubeBlock;
					var slimBlock = cubeBlock.SlimBlock;
					var blockDefinition = slimBlock.BlockDefinition as MyDefinitionBase;
					
					if(blockDefinition.Id.SubtypeId.ToString().Contains("SELtdLargeNanobotBuildAndRepairSystem") || blockDefinition.Id.SubtypeId.ToString().Contains("SELtdSmallNanobotBuildAndRepairSystem")){
						
						var buildRepairBlock = block as Sandbox.ModAPI.Ingame.IMyShipWelder;
						buildRepairBlock.Enabled = false;
						
					}
					
				}
				
				return true;
				
			}
			
			if(hackingType == "DisableDefense"){
				
				foreach(var block in blockList){
					
					var turret = block as Sandbox.ModAPI.Ingame.IMyLargeTurretBase;
					
					if(turret != null){
						
						turret.Enabled = false;
						turret.SetValue("Range", (float)50);
						turret.SetValue("TargetMeteors", false);
						turret.SetValue("TargetMissiles", false);
						turret.SetValue("TargetSmallShips", false);
						turret.SetValue("TargetLargeShips", false);
						turret.SetValue("TargetCharacters", false);
						turret.SetValue("TargetStations", false);
						turret.SetValue("TargetNeutrals", false);
						continue;
						
					}
					
					var userGun = block as Sandbox.ModAPI.Ingame.IMyUserControllableGun;
					
					if(userGun != null){
						
						userGun.Enabled = false;
						
					}
					
				}
				
				return true;
				
			}
			
			if(hackingType == "DisableAutomation"){
				
				foreach(var block in blockList){
					
					var programmable = block as Sandbox.ModAPI.Ingame.IMyProgrammableBlock;
					var timer = block as IMyTimerBlock;
					
					if(programmable != null){
						
						programmable.Enabled = false;
						
					}
					
					if(timer != null){
						
						timer.Enabled = false;
						
					}
					
				}
				
				return true;
				
			}
			
			if(hackingType == "HudSpam"){
				
				foreach(var block in blockList){
					
					block.ShowOnHUD = true;
					
				}
				
				return true;
				
			}
			
			if(hackingType == "OverloadShields"){
				
				foreach(var block in blockList){
					
					if(block as Sandbox.ModAPI.Ingame.IMyRefinery == null){
						
						continue;
						
					}
					
					var blockEntity = block as IMyEntity;
					var cubeBlock = blockEntity as IMyCubeBlock;
					var slimBlock = cubeBlock.SlimBlock;
					var blockDefinition = slimBlock.BlockDefinition as MyDefinitionBase;
					bool foundShield = false;
					
					
					if(blockDefinition.Id.SubtypeId.ToString().Contains("LargeShipSmallShieldGeneratorBase")){
						
						foundShield = true;
						
					}
					
					if(blockDefinition.Id.SubtypeId.ToString().Contains("LargeShipLargeShieldGeneratorBase")){
						
						foundShield = true;
						
					}
					
					if(blockDefinition.Id.SubtypeId.ToString().Contains("SmallShipSmallShieldGeneratorBase")){
						
						foundShield = true;
						
					}
					
					if(blockDefinition.Id.SubtypeId.ToString().Contains("SmallShipMicroShieldGeneratorBase")){
						
						foundShield = true;
						
					}
					
					if(foundShield == true){
						
						//Add To List and Change Bool
						shieldOverloadList.Add(block as Sandbox.ModAPI.Ingame.IMyRefinery);
						overloadShields = true;

					}
					
				}
				
				return true;
				
			}
			
			if(hackingType == "CorruptNames"){
				
				string randomCharacters = " abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890,.<>/?;:'[]{}|-_=+)()*&^%$#@!";
				int blocksAffected = rnd.Next(1, blockList.Count);
				
				for(int i = 0; i < blocksAffected; i++){
					
					int nameLength = rnd.Next(10, 20);
					string corruptName = "";
					
					for(int j = 0; j < nameLength; j++){
						
						corruptName += randomCharacters[rnd.Next(0, randomCharacters.Length)];
						
					}
					
					blockList[rnd.Next(0, blockList.Count)].CustomName = corruptName;
					
				}
				
				return true;

			}
			
			if(hackingType == "GyroOverride"){
				
				foreach(var block in blockList){
					
					var gyro = block as Sandbox.ModAPI.Ingame.IMyGyro;
					
					if(block.IsFunctional == true && gyro != null){
						
						gyro.Enabled = true;
						gyro.GyroPower = 100; //Could also just be 1, comeback and check this.
						gyro.GyroOverride = true;
						gyro.Yaw = (float)rnd.Next(-3, 3);
						gyro.Pitch = (float)rnd.Next(-3, 3);
						gyro.Roll = (float)rnd.Next(-3, 3);
						return true;
						
					}
					
				}
				
				return true;
				
			}
			
			if(hackingType == "ThrustOverride"){
				
				var directionList = new List<Vector3D>();
				directionList.Add(cubeGrid.WorldMatrix.Forward);
				directionList.Add(cubeGrid.WorldMatrix.Backward);
				directionList.Add(cubeGrid.WorldMatrix.Up);
				directionList.Add(cubeGrid.WorldMatrix.Down);
				directionList.Add(cubeGrid.WorldMatrix.Left);
				directionList.Add(cubeGrid.WorldMatrix.Right);
				
				var randomThrustDirection = directionList[rnd.Next(0, directionList.Count)];
				
				foreach(var block in blockList){
					
					var thrust = block as Sandbox.ModAPI.Ingame.IMyThrust;
					
					if(block.IsFunctional == true && thrust != null && block.WorldMatrix.Forward == randomThrustDirection){
						
						thrust.Enabled = true;
						thrust.ThrustOverridePercentage = (float)rnd.Next(1, 100);
						
					}
					
				}
				
				return true;
				
			}
			
			if(hackingType == "HackLights"){
				
				foreach(var block in blockList){
					
					var light = block as Sandbox.ModAPI.Ingame.IMyLightingBlock;
					
					if(light != null){
						
						var newColor = new Color(0,0,0);
						newColor.R = (byte)rnd.Next(0, 255);
						newColor.G = (byte)rnd.Next(0, 255);
						newColor.B = (byte)rnd.Next(0, 255);
						light.Enabled = true;
						light.Color = newColor;
						light.BlinkIntervalSeconds = 10;
						light.BlinkLength = 50;
						light.BlinkOffset = rnd.Next(0, 100);
						
					}
					
				}
				
				return true;
				
			}
			
			return false;
			
		}
		
		bool ResetTerminalNames(Sandbox.ModAPI.Ingame.IMyTerminalBlock pb){
			
			var cubeGrid = (VRage.Game.ModAPI.IMyCubeGrid)pb.CubeGrid;
			
			//Get Blocks
			var gts = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(cubeGrid);
			var blockList = new List<Sandbox.ModAPI.IMyTerminalBlock>();
			gts.GetBlocksOfType<Sandbox.ModAPI.IMyTerminalBlock>(blockList);
			
			foreach(var block in blockList){
				
				var blockDef = block.SlimBlock.BlockDefinition;
				
				if(blockDef == null){
					
					continue;
					
				}
				
				block.CustomName = blockDef.DisplayNameText;
				block.ShowOnHUD = false;
				
			}
			
			return true;
			
		}
		
		bool SpawnReinforcements(Sandbox.ModAPI.Ingame.IMyTerminalBlock pb){
			
			var corruptionFaction = MyAPIGateway.Session.Factions.TryGetFactionByTag("CORRUPT");
			
			string spawnGroupName = "";
			var coords = new Vector3D(0,0,0);
			var velocity = new Vector3D(0,0,0);
			var forwardDir = new Vector3D(0,0,0);
			bool successfulSpawnMandatory = false;
			
			var upDir = new Vector3D(0,0,0);
			var dataSplit = pb.CustomData.Split('\n');
			pb.CustomData = "";
			
			if(dataSplit.Length != 5){
				
				LogEntry("Debug: Bad Data Provided");
				return false;
			
			}
			
			spawnGroupName = dataSplit[0];
			
			if(Vector3D.TryParse(dataSplit[1], out coords) == false || Vector3D.TryParse(dataSplit[2], out velocity) == false || Vector3D.TryParse(dataSplit[3], out forwardDir) == false || bool.TryParse(dataSplit[4], out successfulSpawnMandatory) == false){
				
				LogEntry("Debug: Couldn't Parse Data");
				return false;
			
			}
			
			upDir = MyUtils.GetRandomPerpendicularVector(ref forwardDir);
			MatrixD spawningMatrix = MatrixD.CreateWorld(coords, forwardDir, upDir);
			bool foundSpawnGroup = false;
			var	spawnGroups = MyDefinitionManager.Static.GetSpawnGroupDefinitions();
			var prefabNameList = new List<string>();
			var prefabCoordsList = new List<Vector3D>();
			var prefabBeaconList = new List<string>();
			
			foreach(var spawnGroup in spawnGroups){
				
				if(spawnGroup.Id.SubtypeName == spawnGroupName){
					
					foundSpawnGroup = true;
					
					foreach(var prefab in spawnGroup.Prefabs){
						
						var safeTeleportPosition = MyAPIGateway.Entities.FindFreePlace(coords, 50, 10, 3, 10);
			
						if(safeTeleportPosition == null){
							
							LogEntry("Debug: Spawn area not safe");
							if(successfulSpawnMandatory == true){
								
								return false;
								
							}else{
								
								continue;
								
							}
							
						}
						
						prefabNameList.Add(prefab.SubtypeId);
						prefabCoordsList.Add(Vector3D.Transform((Vector3D)prefab.Position, spawningMatrix));
						prefabBeaconList.Add(prefab.BeaconText);
						
					}
					
					break;
				}
				
			}
						
			if(foundSpawnGroup == false || prefabNameList.Count == 0){
				
				LogEntry("Debug: Couldn't find SpawnGroup");
				return false;
				
			}
			
			var tempSpawningList = new List<IMyCubeGrid>();
			
			for(int i = 0; i < prefabNameList.Count; i++){
				
				MyAPIGateway.PrefabManager.SpawnPrefab(tempSpawningList, prefabNameList[i], prefabCoordsList[i], forwardDir, upDir, (Vector3D)velocity, spawningOptions: SpawningOptions.RotateFirstCockpitTowardsDirection | SpawningOptions.SpawnRandomCargo, beaconName: prefabBeaconList[i], ownerId: corruptionFaction.FounderId, updateSync: false);
				
			}
			
			return true;
			
		}
		
		bool CreateAndLaunchMeteor(Sandbox.ModAPI.Ingame.IMyTerminalBlock pb){
			
			Vector3D spawnCoords = new Vector3D(0,0,0);
			Vector3D launchDirection = new Vector3D(0,0,0);
			Vector3D inheritedVelocity = new Vector3D(0,0,0);
			
			var dataSplit = pb.CustomData.Split('\n');
			pb.CustomData = "";
			
			if(dataSplit.Length != 3){
				
				return false;
				
			}
			
			if(Vector3D.TryParse(dataSplit[0], out spawnCoords) == false || Vector3D.TryParse(dataSplit[1], out launchDirection) == false || Vector3D.TryParse(dataSplit[2], out inheritedVelocity) == false){
				
				return false;
				
			}
			
			var definitionId = new MyDefinitionId(typeof(MyObjectBuilder_Ore), "Stone");
			var amount = (MyFixedPoint)10000;
			var content = (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(definitionId);
			MyObjectBuilder_InventoryItem inventoryItem = new MyObjectBuilder_InventoryItem { Amount = amount, Content = content, PhysicalContent = content};
			
			var velocity = launchDirection * 150;
			var up = Vector3D.CalculatePerpendicularVector(launchDirection);
			
			var meteorOB = new MyObjectBuilder_Meteor();
			meteorOB.Item = inventoryItem;
			meteorOB.PersistentFlags = MyPersistentEntityFlags2.InScene;
			meteorOB.PositionAndOrientation = new MyPositionAndOrientation(spawnCoords, (Vector3)launchDirection, (Vector3)up);			
			meteorOB.LinearVelocity = velocity;
			meteorOB.Integrity = 100;
			
			MyAPIGateway.Entities.RemapObjectBuilder(meteorOB);
			var entity = MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd(meteorOB);
			
			if(entity == null){
				
				LogEntry("Debug: Null Entity");
				return false;
				
			}
			
			return true;
			
		}
		
		/*
		bool RotateDroneToTarget(Sandbox.ModAPI.Ingame.IMyTerminalBlock pb){
			
			if(pb.CustomData == ""){
				
				return false;
				
			}
			
			var dataSplit = pb.CustomData.Split('|');
			
			if(dataSplit.Length != 2){
				
				LogEntry("Debug: Provided Data Bad");
				return false;
				
			}
			
			long remoteEntityId = 0;
			bool rotateMode = false;
			
			if(long.TryParse(dataSplit[0], out remoteEntityId) == false || bool.TryParse(dataSplit[1], out rotateMode) == false){
				
				LogEntry("Debug: Data Parse Bad");
				return false;
				
			}
			
			IMyEntity remoteEntity = null;
			
			if(MyAPIGateway.Entities.TryGetEntityById(remoteEntityId, out remoteEntity) == false){
				
				LogEntry("Debug: Entity not found");
				return false;
				
			}
			
			if(remoteEntity.Name == null){
				
				MyAPIGateway.Entities.SetEntityName(remoteEntity);
				
			}
			
			var closestCharacter = GetNearestCharacter(pb.GetPosition()) as IMyEntity;
			
			if(closestCharacter == null){
				
				LogEntry("Debug: Character to Entity Fail");
				return false;
				
			}
			
			MyAPIGateway.Entities.SetEntityName(closestCharacter);
			
			if(rotateMode == false){
				
				MyVisualScriptLogicProvider.DroneSetRotateToTarget(remoteEntity.Name, false);
				MyVisualScriptLogicProvider.DroneTargetClear(remoteEntity.Name);
				
			}
			
			if(rotateMode == true){
				
				MyVisualScriptLogicProvider.SetDroneBehaviourBasic(remoteEntity.Name);
				MyVisualScriptLogicProvider.DroneSetTarget(remoteEntity.Name, closestCharacter as MyEntity);
				MyVisualScriptLogicProvider.DroneSetRotateToTarget(remoteEntity.Name, true);
				
			}
			
			LogEntry("Debug: Drone AI: " + MyVisualScriptLogicProvider.DroneHasAI(remoteEntity.Name).ToString());
			
			return true;
			
		}
		
		IMyCharacter GetNearestCharacter(Vector3D droneCoords){
			
			var playerList = new List<IMyPlayer>();
			MyAPIGateway.Players.GetPlayers(playerList);
			
			if(playerList.Count == 0){
				
				return null;
				
			}
			
			IMyCharacter closestCharacter = null;
			double distance = 0;
			
			foreach(var player in playerList){
				
				if(player.IsBot == true){
					
					continue;
					
				}
				
				if(closestCharacter == null){
					
					closestCharacter = player.Character;
					distance = MeasureDistance(droneCoords, player.GetPosition());
					continue;
					
				}
				
				if(distance > MeasureDistance(droneCoords, player.GetPosition())){
					
					closestCharacter = player.Character;
					distance = MeasureDistance(droneCoords, player.GetPosition());
					
				}
				
			}
			
			return closestCharacter;
			
		}
		*/
		
		bool BuildProjectedBlocks(Sandbox.ModAPI.Ingame.IMyTerminalBlock pb){
			
			if(pb.CustomData == ""){
				
				return false;
				
			}
			
			var dataSplit = pb.CustomData.Split('\n');
			pb.CustomData = "";
			
			if(dataSplit.Length != 2){
				
				return false;
				
			}
			
			bool instantBuild = false;
			int blocksToBuild = 0;
			
			if(bool.TryParse(dataSplit[0], out instantBuild) == false || int.TryParse(dataSplit[1], out blocksToBuild) == false){
				
				return false;
				
			}
			
			var cubeGrid = (VRage.Game.ModAPI.IMyCubeGrid)pb.CubeGrid;
			var gts = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(cubeGrid);
			var blockList = new List<Sandbox.ModAPI.IMyProjector>();
			gts.GetBlocksOfType<Sandbox.ModAPI.IMyProjector>(blockList);
			int builtBlocks = 0;
			
			foreach(var projector in blockList){
				
				if(projector.IsWorking == false){
					
					continue;
					
				}
				
				var projectedGrid = projector.ProjectedGrid;
				
				if(projectedGrid == null){
					
					continue;
					
				}
				
				var slimBlockList = new List<IMySlimBlock>();
				projectedGrid.GetBlocks(slimBlockList);
				
				foreach(var block in slimBlockList){
					
					if(projector.CanBuild((VRage.Game.ModAPI.IMySlimBlock)block, true) == BuildCheckResult.OK){
						
						projector.Build((VRage.Game.ModAPI.IMySlimBlock)block, pb.OwnerId, pb.OwnerId, instantBuild);
						builtBlocks++;
						
					}
					
					if(builtBlocks >= blocksToBuild){
						
						return true;
						
					}
					
				}
				
			}
			
			return true;
			
		}
		
		bool RepairBlocks(Sandbox.ModAPI.Ingame.IMyTerminalBlock pb){
			
			if(pb.CustomData == ""){
				
				return false;
				
			}
			
			int blocksToBuild = 0;
			
			if(int.TryParse(pb.CustomData, out blocksToBuild) == false){
				
				return false;
				
			}
			
			pb.CustomData = "";
			var cubeGrid = (VRage.Game.ModAPI.IMyCubeGrid)pb.CubeGrid;
			var slimBlockList = new List<IMySlimBlock>();
			cubeGrid.GetBlocks(slimBlockList);
			int repairedBlocks = 0;
			
			var randomDir = MyUtils.GetRandomVector3D();
			var randomSpawn = randomDir * 10000000;
			var prefab = MyDefinitionManager.Static.GetPrefabDefinition("Dummy-Container");
			var gridOB = prefab.CubeGrids[0];
			gridOB.PositionAndOrientation = new MyPositionAndOrientation(randomSpawn, Vector3.Forward, Vector3.Up);
			MyAPIGateway.Entities.RemapObjectBuilder(gridOB);
			var entity = MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd(gridOB);
			
			foreach(var block in slimBlockList){
				
				if(block.HasDeformation == true){
					
					block.FixBones(0, 0);
					
				}

				if(block.BuildLevelRatio != 1 || block.CurrentDamage > 0){
					
					var cargoGrid = entity as IMyCubeGrid;
					
					if(cargoGrid == null){
						
						break;
						
					}
					
					var gts = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(cargoGrid);
					var container = gts.GetBlockWithName("Large Cargo Container") as Sandbox.ModAPI.IMyCargoContainer;
					
					if(container == null){
						
						break;
						
					}
					
					var containerInv = container.GetInventory(0);
					
					//Build Missing Items and Send To Inv
					var missingComps = new Dictionary<string, int>();
					block.GetMissingComponents(missingComps);
					
					foreach(var component in missingComps.Keys){
						
						var definitionId = new MyDefinitionId(typeof(MyObjectBuilder_Component), component);
						var amount = (MyFixedPoint)missingComps[component];
						var content = (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(definitionId);
						MyObjectBuilder_InventoryItem inventoryItem = new MyObjectBuilder_InventoryItem { Amount = amount, Content = content};
						
						containerInv.AddItems((MyFixedPoint)missingComps[component], inventoryItem.Content);
						
					}
					
					block.SpawnConstructionStockpile();
					block.IncreaseMountLevel(0.25f, pb.OwnerId, containerInv, 0f, false, MyOwnershipShareModeEnum.Faction);
					repairedBlocks++;
					
					if(repairedBlocks >= blocksToBuild){
						
						MyAPIGateway.Utilities.InvokeOnGameThread(() => entity.Delete());
						return true;
						
					}
					
				}
				
			}
			
			if(entity != null && MyAPIGateway.Entities.Exist(entity) == true){
				
				MyAPIGateway.Utilities.InvokeOnGameThread(() => entity.Delete());
				
			}
			
			return true;
			
		}
		
		void PlayEffect(byte[] data){
					
			var receivedData = MyAPIGateway.Utilities.SerializeFromBinary<string>(data);
			var dataSplit = receivedData.Split('\n');
			
			if(dataSplit.Length != 2){
				
				return;
				
			}
			
			var startCoords = new Vector3D(0,0,0);
			var endCoords = new Vector3D(0,0,0);
			
			if(Vector3D.TryParse(dataSplit[0], out startCoords) == false || Vector3D.TryParse(dataSplit[1], out endCoords) == false){
				
				return;
				
			}
			
			MyVisualScriptLogicProvider.CreateParticleEffectAtPosition("BlockDestroyedExplosion_Large3X", startCoords);
			MyVisualScriptLogicProvider.CreateParticleEffectAtPosition("BlockDestroyedExplosion_Large3X", endCoords);
			
			//MyVisualScriptLogicProvider.PlaySingleSoundAtPosition(subtype, (Vector3)coords);
			
			
					
		}
		
		void TeslaEffect(byte[] data){
					
			var receivedData = MyAPIGateway.Utilities.SerializeFromBinary<string>(data);
			var dataSplit = receivedData.Split('\n');
			
			if(dataSplit.Length != 2){
				
				return;
				
			}
			
			long sourceEntity = 0;
			long targetEntity = 0;
			
			if(long.TryParse(dataSplit[0], out sourceEntity) == false || long.TryParse(dataSplit[1], out targetEntity) == false){
				
				return;
				
			}
			
			teslaSourceEntity.Add(sourceEntity);
			teslaTargetEntity.Add(targetEntity);
			teslaEffectsLists.Add(new List<Vector3D>());
			teslaEffectsTimers.Add(0);
			teslaEffectsTotalTimers.Add(0);
			
		}
		
		bool TeslaRequest(Sandbox.ModAPI.Ingame.IMyTerminalBlock pb){
			
			var dataSplit = pb.CustomData.Split('\n');
			
			if(dataSplit.Length != 2){
				
				return false;
				
			}
			
			long sourceEntity = 0;
			long targetEntity = 0;
			
			if(long.TryParse(dataSplit[0], out sourceEntity) == false || long.TryParse(dataSplit[1], out targetEntity) == false){
				
				return false;
				
			}
			
			IMyEntity targetE = null;
			IMyCubeGrid targetGrid = null;
			IMyCharacter targetHuman = null;
			
			if(MyAPIGateway.Entities.TryGetEntityById(targetEntity, out targetE) == false){
				
				return false;
				
			}
			
			targetGrid = targetE as IMyCubeGrid;
			targetHuman = targetE as IMyCharacter;
			
			if(targetHuman != null){
				
				targetHuman.Kill();
				
			}
			
			if(targetGrid != null){
				
				var gts = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(targetGrid);
				var blockList = new List<Sandbox.ModAPI.Ingame.IMyFunctionalBlock>();
				gts.GetBlocksOfType<Sandbox.ModAPI.Ingame.IMyFunctionalBlock>(blockList);
				
				var blocksToAffect = rnd.Next(3, 6);
				
				if(blockList.Count < blocksToAffect){
					
					blocksToAffect = blockList.Count;
					
				}
				
				for(int i = 0; i < blocksToAffect; i++){
					
					blockList[rnd.Next(0, blockList.Count)].Enabled = false;
					
				}
				
			}
			
			List<IMyPlayer> activePlayers = new List<IMyPlayer>();
			MyAPIGateway.Players.GetPlayers(activePlayers);
			foreach(var player in activePlayers){
			
				if(player.IsBot == true || player.Character == null){
				
					continue;
				
				}
				
				if(player.Character.IsDead == true){
				
					continue;
				
				}

				if(MeasureDistance(player.GetPosition(), pb.GetPosition()) < 2000){
				
					var sendData = MyAPIGateway.Utilities.SerializeToBinary<string>(pb.CustomData);
					bool sendClientStatus = MyAPIGateway.Multiplayer.SendMessageTo(6756, sendData, player.SteamUserId);
				
				}

			}
			
			return true;

		}

		void EffectRequest(Vector3D startCoords, Vector3D endCoords){

			List<IMyPlayer> activePlayers = new List<IMyPlayer>();
			MyAPIGateway.Players.GetPlayers(activePlayers);
			foreach(var player in activePlayers){
			
				if(player.IsBot == true || player.Character == null){
				
					continue;
				
				}
				
				if(player.Character.IsDead == true){
				
					continue;
				
				}
				
				if(MeasureDistance(player.GetPosition(), startCoords) < 1000 || MeasureDistance(player.GetPosition(), endCoords) < 1000){
				
					string effectData = startCoords.ToString() + "\n" + endCoords.ToString();
					var sendData = MyAPIGateway.Utilities.SerializeToBinary<string>(effectData);
					bool sendClientStatus = MyAPIGateway.Multiplayer.SendMessageTo(6356, sendData, player.SteamUserId);
				
				}

			}

		}
		
		void RemoveGridAuthorship(IMyCubeGrid originGrid){
		
			var gridList = MyAPIGateway.GridGroups.GetGroup(originGrid, GridLinkTypeEnum.Mechanical);
			
			foreach(var grid in gridList){
				
				var gridOwners = grid.BigOwners;
				
				var gridEntity = grid as IMyEntity;
				var cubeGrid = gridEntity as MyCubeGrid;
				
				foreach(var owner in gridOwners){
					
					cubeGrid.TransferBlocksBuiltByID(owner, 0);
					
				}
			
			}
		
		}
		
		bool CheckDroneAntenna(IMyCubeGrid cubeGrid, double distanceToTarget){
			
			var gts = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(cubeGrid);
			var blockList = new List<Sandbox.ModAPI.Ingame.IMyTerminalBlock>();
			gts.GetBlocksOfType<Sandbox.ModAPI.Ingame.IMyTerminalBlock>(blockList);
			
			foreach(var block in blockList){
				
				var antenna = block as Sandbox.ModAPI.Ingame.IMyRadioAntenna;
				
				if(antenna == null){
					
					continue;
					
				}
				
				if(antenna.IsFunctional == true && antenna.EnableBroadcasting == true && antenna.Enabled == true && (double)antenna.Radius >= distanceToTarget){
					
					return true;
					
				}
				
			}
			
			return false;
			
		}
		
		float CalculateGridHealth(Sandbox.ModAPI.Ingame.IMyTerminalBlock pb){
			
			var cubeGrid = (VRage.Game.ModAPI.IMyCubeGrid)pb.CubeGrid;
			var blockList = new List<IMySlimBlock>();
			cubeGrid.GetBlocks(blockList);
			
			float totalIntegrity = 0;
			float totalDamage = 0;
			
			foreach(var block in blockList){
				
				totalDamage += block.CurrentDamage;
				totalIntegrity += block.BuildIntegrity;
				
			}
			
			return totalIntegrity - totalDamage;
			
		}
		
		bool SniperReload(Sandbox.ModAPI.Ingame.IMyTerminalBlock pb){
			
			var cubeGrid = (VRage.Game.ModAPI.IMyCubeGrid)pb.CubeGrid;
			var gts = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(cubeGrid);
			var sniperLauncher = gts.GetBlockWithName("Corruption Rocket Launcher Sniper") as Sandbox.ModAPI.Ingame.IMyUserControllableGun;
			var cargoContainer = gts.GetBlockWithName("Medium Cargo Container") as Sandbox.ModAPI.Ingame.IMyCargoContainer;
			
			if(sniperLauncher == null || cargoContainer == null){
				
				return false;
				
			}
			
			var sniperInv = (VRage.Game.ModAPI.IMyInventory)sniperLauncher.GetInventory(0);
			var cargoInv = (VRage.Game.ModAPI.IMyInventory)cargoContainer.GetInventory(0);
			
			if(sniperInv.Empty() == false){
				
				return true;
				
			}
			
			var definitionId = new MyDefinitionId(typeof(MyObjectBuilder_AmmoMagazine), "Corruption-Missile200mm-Sniper");
			var definition = MyDefinitionManager.Static.GetDefinition(definitionId);
			MyFixedPoint amountMFP = 1;
			var content = (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(definitionId);
			MyObjectBuilder_InventoryItem inventoryItem = new MyObjectBuilder_InventoryItem { Amount = amountMFP, Content = content };
			
			if(cargoInv.ContainItems(1, inventoryItem.Content) == false){
				
				return false;
				
			}
			
			for(int i = 0; i < 4; i++){
				
				if(cargoInv.ContainItems(1, inventoryItem.Content) == false){
					
					break;
					
				}
				
				cargoInv.RemoveItemsOfType(1, inventoryItem.Content);
				sniperInv.AddItems(1, inventoryItem.Content);
				
			}
			
			
			return true;
			
		}
		
		public void LaserFire(Vector3D startCoords, Vector3D endCoords){
						
			var beamOffset = rnd.Next(-10000, 10000);
			double beamOffsetDouble = 0;
			
			if(beamOffset != 0){
				
				beamOffsetDouble = (double)beamOffset / 50000;
				
			}
			
			startCoords.X += beamOffsetDouble;
			startCoords.Y += beamOffsetDouble;
			startCoords.Z += beamOffsetDouble;
			
			endCoords.X += beamOffsetDouble;
			endCoords.Y += beamOffsetDouble;
			endCoords.Z += beamOffsetDouble;
			
			Vector4 color1 = Color.Cyan.ToVector4();
			Vector4 color2 = Color.White.ToVector4();
			
			var colorList = new List<Vector4>();
			
			colorList.Add(Color.Yellow.ToVector4());
			colorList.Add(Color.LightYellow.ToVector4());
			colorList.Add(Color.PaleGreen.ToVector4());
			colorList.Add(Color.LimeGreen.ToVector4());
			colorList.Add(Color.Cyan.ToVector4());
			colorList.Add(Color.SkyBlue.ToVector4());
			var randColor = colorList[rnd.Next(0, colorList.Count)];
			
			var beamsize = (float)rnd.Next(25,75) / 10;
			
			VRage.Game.MySimpleObjectDraw.DrawLine(startCoords, endCoords, MyStringId.GetOrCompute("WeaponLaser"), ref randColor, beamsize);
			VRage.Game.MySimpleObjectDraw.DrawLine(startCoords, endCoords, MyStringId.GetOrCompute("WeaponLaser"), ref randColor, beamsize - 0.3f);
			VRage.Game.MySimpleObjectDraw.DrawLine(startCoords, endCoords, MyStringId.GetOrCompute("WeaponLaser"), ref randColor, beamsize - 0.6f);
			VRage.Game.MySimpleObjectDraw.DrawLine(startCoords, endCoords, MyStringId.GetOrCompute("WeaponLaser"), ref color2, beamsize - 1.9f);
			
		}
		
		bool SecondaryLaserFire(Sandbox.ModAPI.Ingame.IMyTerminalBlock pb){
			
			var dataSplit = pb.CustomData.Split('\n');
			pb.CustomData = "";
			
			if(dataSplit.Length != 2){
				
				return false;
				
			}
			
			Vector3D startCoords = new Vector3D(0,0,0);
			Vector3D endCoords = new Vector3D(0,0,0);
			
			if(Vector3D.TryParse(dataSplit[0], out startCoords) == false || Vector3D.TryParse(dataSplit[1], out endCoords) == false){
				
				return false;
				
			}
			
			var beamOffset = rnd.Next(-10000, 10000);
			double beamOffsetDouble = 0;
			
			if(beamOffset != 0){
				
				beamOffsetDouble = (double)beamOffset / 50000;
				
			}
			
			startCoords.X += beamOffsetDouble;
			startCoords.Y += beamOffsetDouble;
			startCoords.Z += beamOffsetDouble;
			
			endCoords.X += beamOffsetDouble;
			endCoords.Y += beamOffsetDouble;
			endCoords.Z += beamOffsetDouble;
			
			Vector4 color1 = Color.White.ToVector4();
			VRage.Game.MySimpleObjectDraw.DrawLine(startCoords, endCoords, MyStringId.GetOrCompute("WeaponLaser"), ref color1, 0.2f);
					
			return true;
			
		}
		
		bool LaserAttackHit(Sandbox.ModAPI.Ingame.IMyTerminalBlock pb){
			
			long entityId = 0;
			IMyEntity gridEntity = null;
			Vector3D startCoords = new Vector3D(0,0,0);
			Vector3D endCoords = new Vector3D(0,0,0);
			
			var dataSplit = pb.CustomData.Split('\n');
			pb.CustomData = "";
			
			if(dataSplit.Length != 3){
				
				return false;
				
			}
			
			if(Vector3D.TryParse(dataSplit[0], out startCoords) == false || Vector3D.TryParse(dataSplit[1], out endCoords) == false){
			
				return false;
			
			}
			
			if(long.TryParse(dataSplit[2], out entityId) == false){
				
				return false;
				
			}
			
			if(MyAPIGateway.Entities.TryGetEntityById(entityId, out gridEntity) == false){
				
				return false;
				
			}
			
			var cubeGrid = gridEntity as IMyCubeGrid;
			
			if(cubeGrid != null){
				
				var targetList = new List<IMySlimBlock>();
				IMySlimBlock targetBlock = null;
				double targetDistance = 0;
				cubeGrid.GetBlocks(targetList);
				
				foreach(var block in targetList){
					
					var blockCoords = new Vector3D(0,0,0);
					block.ComputeWorldCenter(out blockCoords);
					
					if(targetBlock == null){
						
						targetDistance = MeasureDistance(blockCoords, endCoords);
						targetBlock = block;
						continue;
						
					}
					
					if(MeasureDistance(blockCoords, endCoords) < targetDistance){
						
						targetDistance = MeasureDistance(blockCoords, endCoords);
						targetBlock = block;
						
					}
					
				}
							
				if(targetBlock == null){
					
					return false;
					
				}
				
				var targetBlockCoords = new Vector3D(0,0,0);
				targetBlock.ComputeWorldCenter(out targetBlockCoords);
				targetBlock.DoDamage(9999999, MyStringHash.GetOrCompute("WeaponLaser"), true);
				//MyVisualScriptLogicProvider.CreateExplosion(targetBlockCoords, 2, 999999);
				MyVisualScriptLogicProvider.CreateExplosion(targetBlockCoords, 10, 2000);
				
				
			}else{
				
				MyVisualScriptLogicProvider.CreateExplosion(endCoords, 10, 2000);
				
			}
			
			
			//cubeGrid.RayCastCells(startCoords, endCoords, coordsList);
			
			/*if(coordsList.Count == 0){
				
				MyVisualScriptLogicProvider.ShowNotificationToAll("Grid Raycast Failed", 3000, "White");
				return false;
				
			}*/
			
			/*foreach(var coords in coordsList){
				
				if(coords == null){
					
					MyVisualScriptLogicProvider.ShowNotificationToAll("Null Coords", 3000, "White");
					continue;
					
				}
				
				var targetBlock = cubeGrid.GetCubeBlock((Vector3I)coords);
				
				if(targetBlock != null){
				
					targetList.Add(targetBlock);
				
				}
				
			}*/
			
			/*if(targetList.Count == 0){
				
				MyVisualScriptLogicProvider.ShowNotificationToAll("No Targets Found", 3000, "White");
				return false;
				
			}*/
			
			/*foreach(var block in targetList){
				
				var blockCoords = new Vector3D(0,0,0);
				block.ComputeWorldCenter(out blockCoords);
				
				block.DoDamage(999999, MyStringHash.GetOrCompute("WeaponLaser"), true);
				//MyVisualScriptLogicProvider.CreateExplosion(blockCoords, 2, 999999);
				MyVisualScriptLogicProvider.CreateExplosion(blockCoords, 15, 10);
					
			}*/

			return true;
			
		}
		
		bool InvincibilityToggle(Sandbox.ModAPI.Ingame.IMyTerminalBlock pb){
			
			bool status = false;
			
			if(bool.TryParse(pb.CustomData, out status) == false){
				
				status = false;
				
			}
			
			var cubeGrid = (VRage.Game.ModAPI.IMyCubeGrid)pb.CubeGrid;
			var entity = cubeGrid as IMyEntity;
			var mCubeGrid = entity as MyCubeGrid;
			
			if(entity == null || mCubeGrid == null){
				
				return false;
				
			}
			
			var blockList = new List<IMySlimBlock>();
			cubeGrid.GetBlocks(blockList);
			if(status == false){
				
				foreach(var block in blockList){
					
					//Black to Cyan
					if(block.GetColorMask() == new Vector3(0,-0.97f,-0.51f)){
						
						cubeGrid.ColorBlocks(block.Position, block.Position, new Vector3(0.499889642f,0.199999988,0.55f));
						
					}
					
					//Red to Majenta
					if(block.GetColorMask() == new Vector3(0,0,0.05f)){
						
						cubeGrid.ColorBlocks(block.Position, block.Position, new Vector3(0.8231653f,0.199999988f,0.55f));
						
					}
					
				}
				
			}else{
				
				foreach(var block in blockList){
					
					//Cyan to Black
					if(block.GetColorMask() == new Vector3(0.499889642f,0.199999988,0.55f)){
						
						cubeGrid.ColorBlocks(block.Position, block.Position, new Vector3(0,-0.97f,-0.51f));
						
					}
					
					//Majenta to Red
					if(block.GetColorMask() == new Vector3(0.8231653f,0.199999988f,0.55f)){
						
						cubeGrid.ColorBlocks(block.Position, block.Position, new Vector3(0,0,0.05f));
						
					}
					
				}
				
			}
			
			mCubeGrid.DestructibleBlocks = status;
			mCubeGrid.Editable = status;
			return true;
			
		}
		
		bool TryBossSpawn(Sandbox.ModAPI.Ingame.IMyTerminalBlock pb){
			
			var nearestPlanetPosition = NearestPlanetLocation(pb);
			var spawningCoords = new Vector3D(0,0,0);
			var randomDir = MyUtils.GetRandomVector3D();
			
			if(MeasureDistance(nearestPlanetPosition, pb.GetPosition()) > 50000){
				
				spawningCoords = randomDir * 8000 + pb.GetPosition();
				
			}else{
				
				spawningCoords = Vector3D.Normalize(pb.GetPosition() - nearestPlanetPosition) * 8000 + pb.GetPosition();
				
			}
			
			var corruptionFaction = MyAPIGateway.Session.Factions.TryGetFactionByTag("CORRUPT");
			
			var randomBossGroup = bossSpawnGroups[rnd.Next(0, bossSpawnGroups.Count)];
			
			bool foundSpawnGroup = false;
			var	spawnGroups = MyDefinitionManager.Static.GetSpawnGroupDefinitions();
			var prefabNameList = new List<string>();
			var prefabCoordsList = new List<Vector3D>();
			var prefabBeaconList = new List<string>();
			var prefabForward = randomDir * -1;
			var prefabUp = MyUtils.GetRandomPerpendicularVector(ref prefabForward);
			MatrixD spawningMatrix = MatrixD.CreateWorld(spawningCoords, prefabForward, prefabUp);
			
			foreach(var spawnGroup in spawnGroups){
				
				if(spawnGroup.Id.SubtypeName == randomBossGroup){
					
					foundSpawnGroup = true;
					
					
					
					foreach(var prefab in spawnGroup.Prefabs){
						
						var safeTeleportPosition = MyAPIGateway.Entities.FindFreePlace(Vector3D.Transform((Vector3D)prefab.Position, spawningMatrix), 50, 10, 3, 10);
			
						if(safeTeleportPosition == null){
							
							return false;
							
						}
						
						prefabNameList.Add(prefab.SubtypeId);
						prefabCoordsList.Add(Vector3D.Transform((Vector3D)prefab.Position, spawningMatrix));
						prefabBeaconList.Add(prefab.BeaconText);
						
					}
					
					break;
				}
				
			}
						
			if(foundSpawnGroup == false || prefabNameList.Count == 0){
				
				return false;
				
			}
			
			var tempSpawningList = new List<IMyCubeGrid>();
			
			for(int i = 0; i < prefabNameList.Count; i++){
				
				MyAPIGateway.PrefabManager.SpawnPrefab(tempSpawningList, prefabNameList[i], prefabCoordsList[i], prefabForward, prefabUp, new Vector3D(0,0,0), spawningOptions: SpawningOptions.RotateFirstCockpitTowardsDirection | SpawningOptions.SpawnRandomCargo, beaconName: prefabBeaconList[i], ownerId: corruptionFaction.FounderId, updateSync: false);
				
			}
			
			return true;
			
		}
		
		long GetNearestPlayerThreat(Sandbox.ModAPI.Ingame.IMyTerminalBlock pb){
			
			long threatEntityId = 0;
			int threatScore = 0;
			
			IMyPlayer closestPlayer = null;
			
			var entityList = new HashSet<IMyEntity>();
			var playerList = new List<IMyPlayer>();
			MyAPIGateway.Entities.GetEntities(entityList);
			MyAPIGateway.Players.GetPlayers(playerList);
			
			foreach(var player in playerList){
				
				if(player.IsBot == true || player.Character == null){
					
					continue;
					
				}
				
				if(closestPlayer == null){
					
					closestPlayer = player;
					continue;
					
				}
				
				double pbToCurrentDist = MeasureDistance(player.GetPosition(), pb.GetPosition());
				double pbToPreviousDist = MeasureDistance(closestPlayer.GetPosition(), pb.GetPosition());
				
				if(pbToCurrentDist < pbToPreviousDist){
					
					closestPlayer = player;
					
				}
				
			}
			
			if(closestPlayer == null){
				
				return 0;
				
			}
			
			if(MeasureDistance(closestPlayer.GetPosition(), pb.GetPosition()) > 20000){
				
				return 0;
				
			}
			
			foreach(var entity in entityList){
				
				var cubeGrid = entity as IMyCubeGrid;
				
				if(cubeGrid == null){
					
					continue;
					
				}
				
				if(cubeGrid.BigOwners.Contains(pb.OwnerId) == true){
					
					continue;
					
				}
				
				if(MeasureDistance(closestPlayer.GetPosition(), cubeGrid.GetPosition()) > 10000){
					
					continue;
					
				}
								
				LogEntry("Debug: Potential Grid [" + cubeGrid.CustomName + "]");
				
				int currentThreatScore = 0;
				
				var mCubeGrid = entity as MyCubeGrid;
				
				if(mCubeGrid.IsPowered == false){
					
					currentThreatScore -= 10;
					
				}
				
				//Static Check
				if(cubeGrid.IsStatic == false){
					
					currentThreatScore += 3;
					
				}
				LogEntry("Debug: Passed Static Check");
				
				//Block Size Check
				if(cubeGrid.GridSizeEnum == MyCubeSize.Large){
					
					currentThreatScore += 3;
					
				}
				LogEntry("Debug: Passed Block Size Check");
				
				//Distance From Player
				var gridDistance = MeasureDistance(cubeGrid.GetPosition(), pb.GetPosition());
				if(gridDistance < 5000){
					
					currentThreatScore++;
					
					if(gridDistance < 4000){
						
						currentThreatScore++;
						
						if(gridDistance < 3000){
						
							currentThreatScore++;
							
							if(gridDistance < 2000){
							
								currentThreatScore += 2;
								
								if(gridDistance < 1000){
							
									currentThreatScore += 3;
								
								}
							
							}
						
						}
						
					}
					
				}
				LogEntry("Debug: Passed Target Distance Check");
				
				//Velocity Towards Drone
				
				if(cubeGrid.Physics != null){
					
					var velocity = (Vector3D)cubeGrid.Physics.LinearVelocity;
					
					if(velocity.Length() > 20){
						
						currentThreatScore += 2;
						
						var velocityDir = Vector3D.Normalize(velocity);
						var directionTest = velocityDir * 10 + cubeGrid.GetPosition();
						
						if(gridDistance > MeasureDistance(directionTest, cubeGrid.GetPosition())){
							
							currentThreatScore += 3;
							
						}
						
					}

					
				}
				LogEntry("Debug: Passed Speed Check");
				
				var gts = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(cubeGrid);
				List<Sandbox.ModAPI.Ingame.IMyTerminalBlock> blockList = new List<Sandbox.ModAPI.Ingame.IMyTerminalBlock>();
				gts.GetBlocksOfType<Sandbox.ModAPI.Ingame.IMyTerminalBlock>(blockList);
				
				if(blockList.Count == 0){
					
					continue;
					
				}
				
				foreach(var block in blockList){
					
					var gun = block as Sandbox.ModAPI.Ingame.IMyUserControllableGun;
					var seat = block as Sandbox.ModAPI.Ingame.IMyShipController;
					
					if(gun != null){
						
						currentThreatScore++;
						
					}
					
					if(seat != null){
						
						if(seat.IsUnderControl == true){
							
							currentThreatScore += 3;
							
						}
						
					}
					
				}
				LogEntry("Debug: Passed Block Check");
				
				if(currentThreatScore > threatScore){
					
					var randomBlock = blockList[rnd.Next(0, blockList.Count)];
					threatEntityId = randomBlock.EntityId;
					threatScore = currentThreatScore;
					
				}
								
			}
			
			return threatEntityId;
			
		}
		
		Vector3D TrackEntityPosition(Sandbox.ModAPI.Ingame.IMyTerminalBlock pb){
			
			Vector3D entityPosition = new Vector3D(0,0,0);
			
			var pbData = pb.CustomData;
			pb.CustomData = "";
			long targetEntityId = 0;
			IMyEntity targetEntity = null;
			
			if(long.TryParse(pbData, out targetEntityId) == false){
				
				return entityPosition;
				
			}
			
			if(MyAPIGateway.Entities.TryGetEntityById(targetEntityId, out targetEntity) == false){
				
				return entityPosition;
				
			}
			
			var cubeGrid = targetEntity as IMyCubeGrid;
			
			if(cubeGrid != null){
				
				if(cubeGrid.CustomName.Contains("(NPC-CPC)") == true){

					if(CorruptionDroneValidation(cubeGrid) == true){
						
						entityPosition = targetEntity.GetPosition();
						return entityPosition;
						
					}else{
						
						return entityPosition;
						
					}
					
				}
				
				entityPosition = targetEntity.GetPosition();
				
				return entityPosition;
				
			}
			
			var block = targetEntity as Sandbox.ModAPI.Ingame.IMyTerminalBlock;
			
			if(block != null){
				
				entityPosition = targetEntity.GetPosition();
				return entityPosition;
				
			}
				
			return entityPosition;
			
		}
		
		long GetNearestShieldCoordinator(Sandbox.ModAPI.Ingame.IMyTerminalBlock pb){
			
			long shieldCoordId = 0;
			double shieldDroneDist = 0;
			
			var entityList = new HashSet<IMyEntity>();
			MyAPIGateway.Entities.GetEntities(entityList);
			
			foreach(var entity in entityList){
				
				var cubeGrid = entity as IMyCubeGrid;
				
				if(cubeGrid == null){
					
					continue;
					
				}
				
				if(cubeGrid.CustomName != "(NPC-CPC) Shield Coordinator Drone"){
					
					continue;
					
				}
				
				double currentDistance = MeasureDistance(cubeGrid.GetPosition(), pb.GetPosition());
				
				if(currentDistance > 15000){
					
					continue;
					
				}
				
				
				
				if(CorruptionDroneValidation(cubeGrid) == true){
						
					if(shieldCoordId == 0){
						
						shieldCoordId = cubeGrid.EntityId;
						shieldDroneDist = currentDistance;
						continue;
						
					}
					
					if(currentDistance < shieldDroneDist){
						
						shieldCoordId = cubeGrid.EntityId;
						shieldDroneDist = currentDistance;
						continue;
						
					}
						
				}
				
			}
			
			return shieldCoordId;
			
		}
		
		bool CorruptionDroneValidation(IMyCubeGrid cubeGrid){
			
			var corruptFaction = MyAPIGateway.Session.Factions.TryGetFactionByTag("CORRUPT");
				
			if(corruptFaction == null){
				
				return false;
				
			}
			
			var corruptOwner = corruptFaction.FounderId;
			var gts = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(cubeGrid);
			List<Sandbox.ModAPI.Ingame.IMyTerminalBlock> blockList = new List<Sandbox.ModAPI.Ingame.IMyTerminalBlock>();
			gts.GetBlocksOfType<Sandbox.ModAPI.Ingame.IMyTerminalBlock>(blockList);
			bool workingProgram = false;
			bool workingRemote = false;
			bool workingAntenna = false;
			
			if(cubeGrid.BigOwners.Contains(corruptOwner) == false){
				
				//Grid is likely player owned.
				return true;
					
			}
			
			foreach(var block in blockList){
				
				if(workingProgram == true && workingRemote == true && workingAntenna == true){
					
					break;
					
				}
				
				var antenna = block as Sandbox.ModAPI.Ingame.IMyRadioAntenna;
				var remote = block as Sandbox.ModAPI.Ingame.IMyRemoteControl;
				var program = block as Sandbox.ModAPI.Ingame.IMyProgrammableBlock;
				
				if(antenna != null){
					
					if(antenna.IsWorking == true && antenna.OwnerId == corruptOwner){
						
						workingAntenna = true;
						
					}
					
				}
				
				if(remote != null){
					
					if(remote.IsWorking == true && remote.OwnerId == corruptOwner){
						
						workingRemote = true;
						
					}
					
				}
				
				if(program != null){
					
					if(program.IsWorking == true && program.OwnerId == corruptOwner){
						
						workingProgram = true;
						
					}
					
				}
				
			}
			
			if(workingProgram == true && workingRemote == true && workingAntenna == true){
					
				return true;
					
			}
			
			return false;
			
		}
		
		bool MaelstromEffect(Sandbox.ModAPI.Ingame.IMyTerminalBlock block){
			
			var entityList = new HashSet<IMyEntity>();
			MyAPIGateway.Entities.GetEntities(entityList);
			
			foreach(var entity in entityList){
				
				if(block.CustomData == ""){
					
					return false;
					
				}
				
				if(block.CustomData != "Push" && block.CustomData != "Pull"){
					
					return false;
					
				}
				
				Vector3D appliedSpeed = new Vector3D(0,0,0);
				
				if(block.CustomData == "Push"){
					
					appliedSpeed = Vector3D.Normalize(entity.GetPosition() - block.GetPosition());
					
				}else{
					
					appliedSpeed = Vector3D.Normalize(block.GetPosition() - entity.GetPosition());
					
				}
				
				block.CustomData = "";
				
				var distance = MeasureDistance(entity.GetPosition(), block.GetPosition());
				
				if(distance > 2000){
					
					continue;
					
				}
				
				if(entity.Physics == null){
					
					continue;
					
				}
				
				if(entity == block.CubeGrid as IMyEntity){
					
					continue;
					
				}
				
				var cubeGrid = entity as IMyCubeGrid;
				var character = entity as IMyCharacter;
				//var projectile = entity as IMyProjectile;

				//Rules For Grids
				if(cubeGrid != null){
					
					if(cubeGrid.IsStatic == true){
						
						continue;
						
					}
					
					//Get Mass 
					
				}
				
				//Rules For Characters
				
				//Rules For Projectiles
				
			}
			
			return true;
			
		}
		
		bool ClonePlayerGrid(Sandbox.ModAPI.Ingame.IMyTerminalBlock pb){
			
			var entityList = new HashSet<IMyEntity>();
			var eligibleGrids = new List<IMyCubeGrid>();
			MyAPIGateway.Entities.GetEntities(entityList);
			
			foreach(var entity in entityList){
				
				var cubeGrid = entity as IMyCubeGrid;
				
				if(cubeGrid == null){
					
					continue;
					
				}
				
				if(MeasureDistance(cubeGrid.GetPosition(), pb.GetPosition()) > 5000){
					
					continue;
					
				}
				
				if(cubeGrid.GridSizeEnum == MyCubeSize.Large){
					
					continue;
					
				}
				
				var mCubeGrid = entity as MyCubeGrid;
				if(mCubeGrid.IsPowered == false){
					
					continue;
					
				}
				
				var slimBlockList = new List<IMySlimBlock>();
				cubeGrid.GetBlocks(slimBlockList);
				
				if(slimBlockList.Count > 3000){
					
					continue;
					
				}
				
				var thrustDirectionList = new List<Vector3D>();
				bool hasCockpit = false;
				bool hasGyro = false;
				bool hasValidWeapon = false;
				
				var gts = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(cubeGrid);
				List<Sandbox.ModAPI.IMyTerminalBlock> blockList = new List<Sandbox.ModAPI.IMyTerminalBlock>();
				gts.GetBlocksOfType<Sandbox.ModAPI.IMyTerminalBlock>(blockList);
				
				foreach(var block in blockList){
					
					if(block.IsFunctional == false){
						
						continue;
						
					}
					
					var cockpit = block as Sandbox.ModAPI.IMyCockpit;
					
					if(cockpit != null){
						
						hasCockpit = true;
						
					}
					
					var gyro = block as Sandbox.ModAPI.IMyGyro;
					
					if(gyro != null){
						
						hasGyro = true;
						
					}
					
					var thrust = block as Sandbox.ModAPI.IMyThrust;
					
					if(thrust != null){
						
						if(thrustDirectionList.Contains(thrust.WorldMatrix.Forward) == false){
							
							thrustDirectionList.Add(thrust.WorldMatrix.Forward);
							
						}
						
					}
					
					var forwardGun = block as Sandbox.ModAPI.IMyUserControllableGun;
					
					if(forwardGun != null){
						
						hasValidWeapon = true;
						
					}
					
				}
				
				if(hasCockpit == true && hasGyro == true && thrustDirectionList.Count == 6 && hasValidWeapon == true){
					
					eligibleGrids.Add(cubeGrid);
					
				}
				
			}
			
			if(eligibleGrids.Count == 0){
				
				pb.CustomData = "There are no ideas worth stealing here.. Moving on.";
				return false;
				
			}
			
			var selectedGrid = eligibleGrids[rnd.Next(0, eligibleGrids.Count)];
			var gridObjectBuilder = selectedGrid.GetObjectBuilder(true) as MyObjectBuilder_CubeGrid;
			
			if(gridObjectBuilder == null){
				
				return false;
				
			}
			
			var gtsb = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(selectedGrid);
			var blockListb = new List<Sandbox.ModAPI.IMyCockpit>();
			gtsb.GetBlocksOfType<Sandbox.ModAPI.IMyCockpit>(blockListb);
			var cockpitPositionRef = new Dictionary<Vector3I, Vector3I>();
			
			foreach(var cockpit in blockListb){
				
				cockpitPositionRef.Add(cockpit.Min, cockpit.Position);
				
			}
			
			try{
				
				var cockpitList = new List<MyObjectBuilder_Cockpit>();
				
				foreach(var obBlock in gridObjectBuilder.CubeBlocks){
					
					var cockpit = obBlock as MyObjectBuilder_Cockpit;
					if(cockpit != null){
						
						cockpitList.Add(cockpit);
						
					}
					
					var battery = obBlock as MyObjectBuilder_BatteryBlock;
					if(battery != null){
						
						//Set Battery Stored Power To Max - Figure Out What Max Is
						
					}
					
					var pbob = obBlock as MyObjectBuilder_MyProgrammableBlock;
					if(pbob != null){
						
						pbob.Enabled = false;
						
					}
					
					var timerob = obBlock as MyObjectBuilder_TimerBlock;
					if(timerob != null){
						
						timerob.Enabled = false;
						
					}
					
					var sensorob = obBlock as MyObjectBuilder_SensorBlock;
					if(sensorob != null){
						
						sensorob.Enabled = false;
						
					}
					
					var warheadob = obBlock as MyObjectBuilder_Warhead;
					if(warheadob != null){
						
						warheadob.IsArmed = false;
						warheadob.IsCountingDown = false;
						
					}
					
				}
				
				foreach(var cockpit in cockpitList){
					
					string prefabName = "";
					Vector3D cockpitMinCoords = new Vector3D((double)cockpit.Min.X, (double)cockpit.Min.Y, (double)cockpit.Min.Z);
					Vector3D cockpitCoords = new Vector3D(0,0,0);
					
					if(cockpitPositionRef.ContainsKey((Vector3I)cockpitMinCoords) == true){
						
						cockpitCoords = (Vector3D)cockpitPositionRef[(Vector3I)cockpitMinCoords];
						
					}else{
						
						continue;
						
					}
					
					
					var cockpitMatrix = MatrixD.CreateWorld(cockpitCoords, Base6Directions.GetVector(cockpit.BlockOrientation.Forward), Base6Directions.GetVector(cockpit.BlockOrientation.Up));
					var cockpitOrientation = new MyBlockOrientation(cockpit.BlockOrientation.Forward, cockpit.BlockOrientation.Up);
					
					//Regular Cockpit
					if(cockpit.SubtypeId.ToString() == "SmallBlockCockpit"){
						
						prefabName = "(NPC-CPC) Cockpit Replacer";
						//Needs Second Matrix To Calculate This
						//cockpitCoords = new Vector3D((double)cockpit.Min.X, (double)cockpit.Min.Y, (double)cockpit.Min.Z);
						//var tempMatrix = MatrixD.CreateWorld(cockpitCoords, Base6Directions.GetVector(cockpit.BlockOrientation.Forward), Base6Directions.GetVector(cockpit.BlockOrientation.Up));
						//var cockpitOffset = new Vector3D(1, 1, 1);
						//var cockpitCoordsTransformed = Vector3D.Transform(cockpitOffset, tempMatrix);
						//cockpitMatrix = MatrixD.CreateWorld(cockpitCoordsTransformed, Base6Directions.GetVector(cockpit.BlockOrientation.Forward), Base6Directions.GetVector(cockpit.BlockOrientation.Up));
						
					}
					
					//Fighter Cockpit
					if(cockpit.SubtypeId.ToString() == "DBSmallBlockFighterCockpit"){
						
						prefabName = "(NPC-CPC) Fighter Cockpit Replacer";
						//cockpitCoords = new Vector3D((double)cockpit.Min.X, (double)cockpit.Min.Y, (double)cockpit.Min.Z);
						//var tempMatrix = MatrixD.CreateWorld(cockpitCoords, Base6Directions.GetVector(cockpit.BlockOrientation.Forward), Base6Directions.GetVector(cockpit.BlockOrientation.Up));
						//var cockpitOffset = new Vector3D(-1, 1, 2);
						//var cockpitCoordsTransformed = Vector3D.Transform(cockpitOffset, tempMatrix);
						//cockpitMatrix = MatrixD.CreateWorld(cockpitCoords, Base6Directions.GetVector(cockpit.BlockOrientation.Forward), Base6Directions.GetVector(cockpit.BlockOrientation.Up));
						
					}
					
					if(prefabName == ""){
						
						continue;
						
					}
					
					var prefab = MyDefinitionManager.Static.GetPrefabDefinition(prefabName);
					
					if(prefab == null){
						
						continue;
						
					}
					
					gridObjectBuilder.CubeBlocks.Remove(cockpit);
					
					var copiedPrefabGrid = prefab.CubeGrids[0].Clone() as MyObjectBuilder_CubeGrid;
					
					foreach(var prefabBlock in copiedPrefabGrid.CubeBlocks){
						
						var movedBlock = prefabBlock;
						
						//Block Position
						Vector3D movedBlockCoords = new Vector3D((double)movedBlock.Min.X, (double)movedBlock.Min.Y, (double)movedBlock.Min.Z);
						Vector3D transformedBlockCoords = Vector3D.Transform(movedBlockCoords, cockpitMatrix);
						movedBlock.Min = new SerializableVector3I((int)transformedBlockCoords.X, (int)transformedBlockCoords.Y, (int)transformedBlockCoords.Z);
						
						//Block Orientation
						var newForward = cockpitOrientation.TransformDirection(movedBlock.BlockOrientation.Forward);
						var newUp = cockpitOrientation.TransformDirection(movedBlock.BlockOrientation.Up);
						var newOrientation = new SerializableBlockOrientation(newForward, newUp);
						movedBlock.BlockOrientation = newOrientation;
						
						//Add it
						gridObjectBuilder.CubeBlocks.Add(movedBlock);
						
					}
					
				}
				
				gridObjectBuilder.LinearVelocity = new Vector3(0,0,0);
				gridObjectBuilder.AngularVelocity = new Vector3(0,0,0);
				var spawnCoordsDebug = pb.WorldMatrix.Forward * 100 + pb.GetPosition();
				gridObjectBuilder.PositionAndOrientation = new MyPositionAndOrientation(spawnCoordsDebug, Vector3.Forward, Vector3.Up);
				MyAPIGateway.Entities.RemapObjectBuilder(gridObjectBuilder);
				var entityLarge = MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd(gridObjectBuilder);
				
				//AfterSpawn
				var newGrid = entityLarge as IMyCubeGrid;
				
				//Block Lists
				var pbCloneList = new List<Sandbox.ModAPI.IMyProgrammableBlock>();
				var weapons = new List<Sandbox.ModAPI.IMyUserControllableGun>();
				var remotes = new List<Sandbox.ModAPI.IMyRemoteControl>();
				
				//Weapon Direction Reference
				var directionDictionary = new Dictionary<Vector3D, int>();
				var likelyForward = new Vector3D(0,0,0);

				//Get Blocks and Populate to Lists
				var gtsNew = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(newGrid);
				var blockListNew = new List<Sandbox.ModAPI.IMyTerminalBlock>();
				gtsNew.GetBlocksOfType<Sandbox.ModAPI.IMyTerminalBlock>(blockListNew);
				
				foreach(var block in blockListNew){
					
					if(block.IsFunctional == false){
						
						continue;
						
					}
					
					var remote = block as Sandbox.ModAPI.IMyRemoteControl;
					if(remote != null && block.CustomName == "Remote Control (CloneModule)"){
						
						remotes.Add(remote);
						
					}
					
					var weapon = block as Sandbox.ModAPI.IMyUserControllableGun;
					var turret = block as Sandbox.ModAPI.IMyLargeTurretBase;
					
					if(weapon != null){
						
						//GetAmmoAndFillWeapon(weapon, true);
						
					}
					
					if(weapon != null && turret == null){
						
						weapons.Add(weapon);
						
						if(directionDictionary.ContainsKey(weapon.WorldMatrix.Forward) == true){
							
							directionDictionary[weapon.WorldMatrix.Forward]++;
							
						}else{
							
							directionDictionary.Add(weapon.WorldMatrix.Forward, 1);
							
						}
						
					}
					
					var progBlock = block as Sandbox.ModAPI.IMyProgrammableBlock;
					if(progBlock != null && block.CustomName == "Programmable block (CloneModule)"){
						
						pbCloneList.Add(progBlock);
						
					}
					
				}
				
				newGrid.ChangeGridOwnership(pb.OwnerId, MyOwnershipShareModeEnum.None);
				CorruptionPaintJob(newGrid);
					
				
			}catch(Exception exp){
				
				MyVisualScriptLogicProvider.ShowNotificationToAll("Something Broke", 5000);
				pb.CustomData = "Grr.. Something is wrong with the fabrication. We cannot reproduce what we've scanned...";
				return false;
				
			}
			
			return true;
			
		}
		
		void CorruptionPaintJob(IMyCubeGrid cubeGrid){
			
			var blockList = new List<IMySlimBlock>();
			cubeGrid.GetBlocks(blockList);
			var colorDictionary = new Dictionary<Vector3, int>();
			
			foreach(var block in blockList){
				
				if(colorDictionary.ContainsKey(block.ColorMaskHSV) == true){
					
					colorDictionary[block.ColorMaskHSV]++;
					
				}else{
					
					colorDictionary.Add(block.ColorMaskHSV, 1);
					
				}
				
			}
			
			var highestColorValue = new Vector3(0,0,0);
			int highestColorCount = 0;
			var colorCountList = new List<Vector3>(colorDictionary.Keys);
			
			foreach(var color in colorCountList){
				
				if(colorDictionary[color] > highestColorCount){
					
					highestColorValue = color;
					highestColorCount = colorDictionary[color];
					
				}
				
			}
			
			foreach(var block in blockList){
				
				if(block.ColorMaskHSV == new Vector3(0,-0.97f,-0.51f) || block.ColorMaskHSV == new Vector3(0,0,0.05f)){
					
					continue;
					
				}
				
				if(block.ColorMaskHSV == highestColorValue){
					
					cubeGrid.ColorBlocks(block.Position, block.Position, new Vector3(0,-0.97f,-0.51f));
					
				}else{
					
					cubeGrid.ColorBlocks(block.Position, block.Position, new Vector3(0,0,0.05f));
					
				}
				
			}
			
		}

		void MarauderEncounterManager(){
			
			tickCounter++;
			
			if(tickCounter < 60){
				
				return;
				
			}
			
			tickCounter = 0;
			encounterCreateTimer++;
			
			var playerList = new List<IMyPlayer>();
			MyAPIGateway.Players.GetPlayers(playerList);
			
			//Try Create New Encounters
			if(encounterCreateTimer >= encounterCreateTimerTrigger){
				
				LogEntry("Debug: Create Encounter Start");
				
				encounterCreateTimer = 0;
				
				foreach(var player in playerList){
					
					if(player.Character == null || player.IsBot == true){
						
						LogEntry("Debug: Player Not Real");
						continue;
						
					}
					
					if(IsPlayerActiveInEncounter(player, true) == true){
						
						LogEntry("Debug: Player Already In Encounter");
						continue;
						
					}
					
					var factionPlayers = new List<IMyPlayer>();
					var faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(player.IdentityId);
					
					if(faction != null){
						
						foreach(var fPlayer in playerList){
							
							if(player.IsBot == true){
								
								continue;
								
							}
							
							var otherFaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(fPlayer.IdentityId);
							
							if(otherFaction != null){
								
								if(faction == otherFaction){
									
									factionPlayers.Add(fPlayer);
									
								}
								
							}
							
						}
						
					}else{
						
						factionPlayers.Add(player);
						
					}
					
					//TODO: Grid Check On Faction Players
					
					//Do a Gravity Check At Nearest Planet
					var planet = MyGamePruningStructure.GetClosestPlanet(player.GetPosition());
					var encounterCoords = new Vector3D(0,0,0);
					
					if(planet != null){
						
						var planetEntity = planet as IMyEntity;
						var gravityProvider = planetEntity.Components.Get<MyGravityProviderComponent>();
						
						if(gravityProvider.IsPositionInRange(player.GetPosition()) == true){
							
							if(gravityProvider.GetGravityMultiplier(player.GetPosition()) > 0.30f){
								
								LogEntry("Player Too Deep In Gravity Well");
								continue;
								
							}
							
						}
						
						if(MeasureDistance(planetEntity.GetPosition(), player.GetPosition()) > 60000){
							
							var randomDir = Vector3D.Normalize(MyUtils.GetRandomVector3D());
							var eventCoords = randomDir * 7000 + player.GetPosition();
							bool nearOtherPlayer = false;
							
							foreach(var otherPlayer in playerList){
								
								if(player == otherPlayer){
									
									continue;
									
								}
								
								if(MeasureDistance(otherPlayer.GetPosition(), player.GetPosition()) < 1000){
									
									nearOtherPlayer = true;
									
								}
								
							}
							
							if(nearOtherPlayer == true){
								
								LogEntry("Player Too Deep In Gravity Well");
								continue;
								
							}
							
							encounterCoords = eventCoords;
							
							
						}else{
							
							var up = Vector3D.Normalize(player.GetPosition() - planetEntity.GetPosition());
							var airCoords = up * 6000 + player.GetPosition();
							var randomDir = MyUtils.GetRandomPerpendicularVector(ref up);
							var eventCoords = randomDir * 1000 + airCoords;
							bool nearOtherPlayer = false;
							
							foreach(var otherPlayer in playerList){
								
								if(player == otherPlayer){
									
									continue;
									
								}
								
								if(MeasureDistance(otherPlayer.GetPosition(), player.GetPosition()) < 1000){
									
									nearOtherPlayer = true;
									
								}
								
							}
							
							if(nearOtherPlayer == true){
								
								continue;
								
							}
							
							encounterCoords = eventCoords;
							
						}

						
					}else{
						
						var randomDir = Vector3D.Normalize(MyUtils.GetRandomVector3D());
						var eventCoords = randomDir * 7000 + player.GetPosition();
						bool nearOtherPlayer = false;
						
						foreach(var otherPlayer in playerList){
							
							if(player == otherPlayer){
									
								continue;
									
							}
								
							if(MeasureDistance(otherPlayer.GetPosition(), player.GetPosition()) < 1000){
								
								nearOtherPlayer = true;
								
							}
							
						}
						
						if(nearOtherPlayer == true){
							
							continue;
							
						}
						
						encounterCoords = eventCoords;
						
					}
					
					
					var encounterSetup = new MarauderEncounter();
					encounterSetup.EncounterCoords = encounterCoords;
					encounterSetup.EncounterPlayers = factionPlayers;
					
					var exiledChallengeChat = new List<string>();
					exiledChallengeChat.Add("You've proven to be more trouble than I originally anticipated.");
					exiledChallengeChat.Add("The arrogance of your continuing survival has gone on long enough.");
					exiledChallengeChat.Add("I'm almost impressed that you've been able to resist for as long as you have.");
					exiledChallengeChat.Add("How long do you really think you can continue to survive against my drones?");
					
					var chatMessage = exiledChallengeChat[rnd.Next(0, exiledChallengeChat.Count)];
					var chatMgsB = "Let's see if you can handle a REAL threat. You'll find the coordinates attached to this transmission. We'll be waiting...";
					
					foreach(var factionPlayer in factionPlayers){
						
						if(playerList.Contains(factionPlayer) == false){
							
							continue;
							
						}
						
						var sendData = MyAPIGateway.Utilities.SerializeToBinary<string>(encounterSetup.NameGPS + "\n" + "Add" + "\n" + encounterCoords.ToString());
						bool sendStatus = MyAPIGateway.Multiplayer.SendMessageTo(6706, sendData, factionPlayer.SteamUserId);
						MyVisualScriptLogicProvider.SendChatMessage(chatMessage, "Exiled Engineer", factionPlayer.IdentityId, "Red");
						MyVisualScriptLogicProvider.SendChatMessage(chatMgsB, "Exiled Engineer", factionPlayer.IdentityId, "Red");
						
					}
					
					activeEncounters.Add(encounterSetup);
					
				}
				
			}
			
			//Process Existing Encounters
			if(activeEncounters.Count > 0){
				
				for(int i = activeEncounters.Count - 1; i >= 0; i--){
					
					var currentEncounter = activeEncounters[i];
					
					if(currentEncounter.EncounterTriggered == false){
						
						currentEncounter.EncounterTimer++;
						
					}else{
						
						currentEncounter.CooldownTimer++;
						
					}
					
					if(currentEncounter.EncounterTimer >= 2000 || currentEncounter.CooldownTimer >= 2000){
						
						foreach(var factionPlayer in currentEncounter.EncounterPlayers){
							
							if(playerList.Contains(factionPlayer) == false){
								
								continue;
								
							}
							
							var sendData = MyAPIGateway.Utilities.SerializeToBinary<string>(currentEncounter.NameGPS + "\n" + "Remove" + "\n" + currentEncounter.EncounterCoords.ToString());
							bool sendStatus = MyAPIGateway.Multiplayer.SendMessageTo(6706, sendData, factionPlayer.SteamUserId);
														
						}
						
						activeEncounters.RemoveAt(i);
						continue;
						
					}
					
					if(currentEncounter.EncounterTriggered == false){
						
						foreach(var player in currentEncounter.EncounterPlayers){
							
							if(playerList.Contains(player) == false){
								
								continue;
								
							}
							
							if(player.Character == null){
								
								continue;
								
							}
							
							if(MeasureDistance(player.GetPosition(), currentEncounter.EncounterCoords) < 300){
								
								currentEncounter.EncounterTriggered = true;
								
								foreach(var factionPlayer in currentEncounter.EncounterPlayers){
									
									if(playerList.Contains(factionPlayer) == false){
										
										continue;
										
									}
									
									var sendData = MyAPIGateway.Utilities.SerializeToBinary<string>(currentEncounter.NameGPS + "\n" + "Remove" + "\n" + currentEncounter.EncounterCoords.ToString());
									bool sendStatus = MyAPIGateway.Multiplayer.SendMessageTo(6706, sendData, factionPlayer.SteamUserId);
																
								}
								
								ActivateEncounter(currentEncounter, playerList);
								break;
								
							}
							
						}
						
					}
									
				}
				
			}
			
		}
		
		bool IsPlayerActiveInEncounter(IMyPlayer player, bool checkFaction = false, MarauderEncounter specificEncounter = null){
			
			foreach(var encounter in activeEncounters){
				
				if(specificEncounter != null && encounter != specificEncounter){
					
					continue;
					
				}
				
				if(encounter.EncounterPlayers.Contains(player) == true){
					
					return true;
					
				}
				
				if(checkFaction == true){
					
					var playerFaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(player.IdentityId);
					
					if(playerFaction == null){
						
						continue;
						
					}
					
					foreach(var ePlayer in encounter.EncounterPlayers){
						
						var eplayerFaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(player.IdentityId);
					
						if(eplayerFaction == null){
							
							continue;
							
						}
						
						if(playerFaction == eplayerFaction){
							
							return true;
							
						}
						
					}
					
				}
				
			}
			
			return false;
			
		}
		
		void ActivateEncounter(MarauderEncounter encounter, List<IMyPlayer> playerList){
			
			var entityList = new HashSet<IMyEntity>();
			MyAPIGateway.Entities.GetEntities(entityList);
			bool foundSpawnCoords = false;
			var spawnCoords = new Vector3D(0,0,0);
			
			for(int i = 0; i < 5; i++){
				
				bool tooClose = false;
				var tempSpawnCoords = Vector3D.Normalize(MyUtils.GetRandomVector3D()) * 3000 + encounter.EncounterCoords;
				var spawnCoordsSafe = MyAPIGateway.Entities.FindFreePlace(tempSpawnCoords, 100, 2, 3, 10);
				
				if(spawnCoordsSafe == null){
					
					continue;
					
				}
				
				//Check Against Players
				foreach(var player in playerList){
					
					if(player.Character == null){
						
						continue;
						
					}
					
					if(MeasureDistance((Vector3D)spawnCoordsSafe, player.GetPosition()) < 1000){
						
						tooClose = true;
						
					}
					
				}
				
				//Check Against Grids
				foreach(var entity in entityList){
					
					var cubeGrid = entity as IMyCubeGrid;
					
					if(cubeGrid == null){
						
						continue;
						
					}
					
					if(MeasureDistance((Vector3D)spawnCoordsSafe, cubeGrid.GetPosition()) < 1000){
						
						tooClose = true;
						
					}
					
					if(tooClose == false){
						
						spawnCoords = (Vector3D)spawnCoordsSafe;
						foundSpawnCoords = true;
						break;
						
					}
					
				}
				
			}
			
			if(foundSpawnCoords == false){
				
				LogEntry("Debug: No Spawn Coords Found");
				
				foreach(var player in playerList){
					
					if(encounter.EncounterPlayers.Contains(player) == true){
						
						MyVisualScriptLogicProvider.SendChatMessage("You don't seem to understand that answering to this challenge would be your end. Take some time to reflect on that.", "Exiled Engineer", player.IdentityId, "Red");
						
					}
					
				}
				
				return;
				
			}
			
			var prefabName = marauderPrefabList[rnd.Next(0, marauderPrefabList.Count - 1)];
			
			if(MyDefinitionManager.Static.GetPrefabDefinition(prefabName) == null){
				
				LogEntry("Debug: Prefab Not Found");
				return;
				
			}
			
			long corruptionFactionOwner = 0;
			
			try{
				
				corruptionFactionOwner = MyAPIGateway.Session.Factions.TryGetFactionByTag("CORRUPT").FounderId;
				
			}catch(Exception exc){
				
				LogEntry("Debug: Spawning Failed For Unknown Reason.");
				return;
				
			}
			
			var dummyList = new List<IMyCubeGrid>();
			var spawnForward = Vector3D.Normalize(encounter.EncounterCoords - spawnCoords);
			var spawnUp = MyUtils.GetRandomPerpendicularVector(ref spawnForward);
			
			MyAPIGateway.PrefabManager.SpawnPrefab(dummyList, prefabName, spawnCoords, (Vector3)spawnForward, (Vector3)spawnUp, new Vector3(0,0,0), new Vector3(0,0,0), null, SpawningOptions.SetNeutralOwner, corruptionFactionOwner);
			
			LogEntry("Debug: Encounter Activated! Drone Spawn Attempted: " + prefabName);
			
		}
		
		void LocalGPSManager(byte[] data){
			
			var receivedData = MyAPIGateway.Utilities.SerializeFromBinary<string>(data);
			var dataSplit = receivedData.Split('\n');
			
			if(receivedData.StartsWith("SeekerMissileInit") == true){
				
				SeekerMissileInit(receivedData);
				return;
				
			}
			
			if(dataSplit.Length != 3){
				
				LogEntry("Debug: Bad Network Msg Length");
				return;
				
			}
			
			string gpsName = dataSplit[0];
			string gpsMode = dataSplit[1];
			var gpsCoords = new Vector3D(0,0,0);
			
			if(Vector3D.TryParse(dataSplit[2], out gpsCoords) == false){
				
				LogEntry("Debug: Bad Coords Parse");
				return;
				
			}
			
			if(gpsMode == "Add"){
				
				try{
					
					localGPSPlayer = MyAPIGateway.Session.GPS.Create(gpsName, "These coordinates were sent to you by the Exiled Engineer, leader of the CORRUPT faction. Proceed with caution.", gpsCoords, true);
					MyAPIGateway.Session.GPS.AddLocalGps(localGPSPlayer);
					var localPlayer = MyAPIGateway.Session.LocalHumanPlayer;
					MyVisualScriptLogicProvider.SetGPSColor(gpsName, new Color(255,55,255), localPlayer.IdentityId);
					
					
				}catch(Exception e){
					
					LogEntry("Adding Custom GPS Failed");
					
				}
				
			}
			
			if(gpsMode == "Remove"){
				
				try{
					
					var localPlayer = MyAPIGateway.Session.LocalHumanPlayer;
					MyAPIGateway.Session.GPS.RemoveLocalGps(localGPSPlayer);
					
				}catch(Exception e){
					
					LogEntry("Debug: Removing Custom GPS Failed");
					
				}
				
			}
			
		}
		
		bool ShieldRegister(Sandbox.ModAPI.Ingame.IMyTerminalBlock pb){
			
			var cubeGrid = (VRage.Game.ModAPI.IMyCubeGrid)pb.CubeGrid;
			
			try{
				
				if(shieldedGrids.ContainsKey(cubeGrid) == false){
					
					shieldedGrids.Add(cubeGrid, 0);
					
				}else{
					
					shieldedGrids[cubeGrid] = 0;
					
				}
				
			}catch(Exception exc){
				
				return false;
				
			}
			
			return true;
			
		}
		
		void ShieldTimeout(){
			
			shieldCheckTimer++;
			
			if(shieldCheckTimer < 120){
				
				return;
				
			}
			
			shieldCheckTimer = 0;
			
			try{
				
				foreach(var grid in shieldedGrids.Keys){
					
					if(grid == null){
						
						continue;
						
					}
					
					if(shieldedGrids[grid] > 4){
						
						continue;
						
					}
										
					shieldedGrids[grid]++;
					
					if(shieldedGrids[grid] == 4){
						
						shieldedGrids[grid]++;
						var entity = grid as IMyEntity;
						var mCubeGrid = entity as MyCubeGrid;
						mCubeGrid.DestructibleBlocks = true;
						mCubeGrid.Editable = true;
						
						var blockList = new List<IMySlimBlock>();
						grid.GetBlocks(blockList);
						foreach(var block in blockList){
							
							//Cyan to Black
							if(block.GetColorMask() == new Vector3(0.499889642f,0.199999988,0.55f)){
								
								grid.ColorBlocks(block.Position, block.Position, new Vector3(0,-0.97f,-0.51f));
								
							}
							
							//Majenta to Red
							if(block.GetColorMask() == new Vector3(0.8231653f,0.199999988f,0.55f)){
								
								grid.ColorBlocks(block.Position, block.Position, new Vector3(0,0,0.05f));
								
							}
							
						}
						
					}
					
				}
				
			}catch(Exception exc){
				
				
				
			}
			
		}
		
		bool CreateSeekerMissile(Sandbox.ModAPI.Ingame.IMyTerminalBlock pb){
			
			var dataSplit = pb.CustomData.Split('\n');
			pb.CustomData = "";
			
			if(dataSplit.Length != 3){
				
				LogEntry("Debug: Bad Split");
				return false;
				
			}
			
			long targetId = 0;
			Vector3D spawnCoords = new Vector3D(0,0,0);
			Vector3D forwardDir = new Vector3D(0,0,0);
			Vector3D upDir = new Vector3D(0,0,0);
			IMyEntity targetEntity = null;
			
			if(long.TryParse(dataSplit[0], out targetId) == false || Vector3D.TryParse(dataSplit[1], out spawnCoords) == false || Vector3D.TryParse(dataSplit[2], out forwardDir) == false){
				
				LogEntry("Debug: Bad Parse");
				return false;
				
			}
			
			if(MyAPIGateway.Entities.TryGetEntityById(targetId, out targetEntity) == false){
				
				LogEntry("Debug: Bad Target Id to Entity");
				return false;
				
			}
			
			upDir = MyUtils.GetRandomPerpendicularVector(ref forwardDir);
			var velocity = forwardDir * 250;
			
			//Create Ob
			var cubeGridOb = new MyObjectBuilder_CubeGrid();
			cubeGridOb.PersistentFlags = MyPersistentEntityFlags2.InScene;
			cubeGridOb.IsStatic = false;
			cubeGridOb.GridSizeEnum = MyCubeSize.Small;
			cubeGridOb.LinearVelocity = (Vector3)velocity;
			cubeGridOb.AngularVelocity = new Vector3(0,0,0);
			cubeGridOb.PositionAndOrientation = new MyPositionAndOrientation(spawnCoords, (Vector3)forwardDir, (Vector3)upDir);
			var cubeBlockOb = new MyObjectBuilder_Warhead();
			cubeBlockOb.Min = new Vector3I(0,0,0);
			cubeBlockOb.SubtypeName = "CorruptSeekerMissile";
			cubeBlockOb.CustomName = "Corrupt Seeker Missile";
			cubeBlockOb.EntityId = 0;
			cubeBlockOb.Owner = pb.OwnerId;
			cubeBlockOb.CountdownMs = 30000;
			cubeBlockOb.IsArmed = true;
			cubeBlockOb.IsCountingDown = true;
			cubeBlockOb.BlockOrientation = new SerializableBlockOrientation(Base6Directions.Direction.Forward, Base6Directions.Direction.Up);
			var cubeBlockObB = new MyObjectBuilder_BatteryBlock();
			cubeBlockObB.SubtypeName = "CorruptDummyBattery";
			cubeBlockObB.Min = new Vector3I(0,0,-1);
			cubeBlockObB.EntityId = 0;
			cubeBlockObB.Owner = pb.OwnerId;
			cubeBlockObB.BlockOrientation = new SerializableBlockOrientation(Base6Directions.Direction.Forward, Base6Directions.Direction.Up);
			cubeGridOb.CubeBlocks.Add(cubeBlockOb);
			cubeGridOb.CubeBlocks.Add(cubeBlockObB);
			MyAPIGateway.Entities.RemapObjectBuilder(cubeGridOb);
			var entitySmall = MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd(cubeGridOb);
			
			var blockList = new List<IMySlimBlock>();
			var cubeGrid = entitySmall as IMyCubeGrid;
			cubeGrid.GetBlocks(blockList);
			
			var missileData = new SeekerMissileDetails();
			
			foreach(var block in blockList){
				
				if(block.FatBlock == null){
					
					continue;
					
				}
				
				block.FatBlock.SetDamageEffect(true);
				
				if(block.FatBlock as Sandbox.ModAPI.IMyWarhead != null){
					
					missileData.Warhead = block.FatBlock as Sandbox.ModAPI.IMyWarhead;
					
				}
				
			}

			if(MyAPIGateway.Multiplayer.IsServer == true){
				
				missileData.IsServer = true;
				
			}
			
			missileData.TargetGrid = targetEntity as Sandbox.ModAPI.IMyTerminalBlock;
			missileData.MissileGrid = entitySmall as IMyCubeGrid;
			
			activeMissileList.Add(missileData);
			
			return true;
			
		}
		
		void SeekerMissileInit(string receivedData){
			
			var dataSplit = receivedData.Split('\n');
			
			if(dataSplit.Length != 3){
				
				return;
				
			}
			
			long missileId = 0;
			long targetId = 0;
			
			if(long.TryParse(dataSplit[1], out missileId) == false || long.TryParse(dataSplit[2], out targetId) == false){
				
				return;
				
			}
			
			IMyEntity missileEntity = null;
			IMyEntity targetEntity = null;
			
			if(MyAPIGateway.Entities.TryGetEntityById(missileId, out missileEntity) == false || MyAPIGateway.Entities.TryGetEntityById(targetId, out targetEntity) == false){
				
				return;
				
			}
			
			var missileData = new SeekerMissileDetails();
			
			if(MyAPIGateway.Multiplayer.IsServer == true){
				
				missileData.IsServer = true;
				
			}
			
			missileData.TargetGrid = targetEntity as Sandbox.ModAPI.IMyTerminalBlock;
			missileData.MissileGrid = missileEntity as IMyCubeGrid;
			
			activeMissileList.Add(missileData);
			
		}
		
		void ProcessActiveMissiles(){
			
			if(activeMissileList.Count == 0){
				
				return;
				
			}
			
			for(int i = activeMissileList.Count - 1; i >= 0; i--){
				
				if(activeMissileList[i].MissileRun() == false){
					
					if(activeMissileList[i].LastValidPosition != new Vector3D(0,0,0)){
						
						MyVisualScriptLogicProvider.CreateExplosion(activeMissileList[i].LastValidPosition, 8, 15000);
						
					}
					
					activeMissileList.RemoveAt(i);
					
				}
				
			}
			
		}
		
		Vector3D NearestPlanetLocation(Sandbox.ModAPI.Ingame.IMyTerminalBlock pb){
			
			var result = new Vector3D(0,0,0);
			var entityList = new HashSet<IMyEntity>();
			MyAPIGateway.Entities.GetEntities(entityList, x => x is MyPlanet);
			
			foreach(var planet in entityList){
				
				if(result == new Vector3D(0,0,0)){
					
					result = planet.GetPosition();
					continue;
					
				}
				
				if(MeasureDistance(pb.GetPosition(), planet.GetPosition()) < MeasureDistance(pb.GetPosition(), result)){
					
					result = planet.GetPosition();
					
				}
				
			}
			
			return result;
			
		}
		
		double MeasureDistance(Vector3D coordsStart, Vector3D coordsEnd){
					
			double distance = Math.Round( Vector3D.Distance( coordsStart, coordsEnd ), 2 );
			return distance;
			
		}
		
		public void LogEntry(string argument){
			
			if(argument.Contains("Debug") == true && debugMode == false){
				
				return;
				
			}
			
			MyLog.Default.WriteLineAndConsole("Corruption: " + argument);
			
			if(debugMode == true){
				
				MyVisualScriptLogicProvider.ShowNotificationToAll(argument, 5000, "White");
				
			}
			
		}
		
		protected override void UnloadData(){
			
			MyAPIGateway.Multiplayer.UnregisterMessageHandler(6456, AdminCommand);
			MyAPIGateway.Multiplayer.UnregisterMessageHandler(6356, PlayEffect);
			MyAPIGateway.Multiplayer.UnregisterMessageHandler(6756, TeslaEffect);
			MyAPIGateway.Multiplayer.UnregisterMessageHandler(6716, EnergyCannonRegister);
			MyAPIGateway.Multiplayer.UnregisterMessageHandler(6706, LocalGPSManager);
			MyAPIGateway.Utilities.MessageEntered -= CorruptionChatCommand;
			//MyAPIGateway.Entities.OnEntityAdd -= AddGridLogger;
			
		}

	}
	
}