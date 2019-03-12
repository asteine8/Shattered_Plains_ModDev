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

namespace ModularEncountersSpawner{
	
	public static class GridBuilderManipulation{
		
		public struct WeaponProfile{
			
			public MyWeaponBlockDefinition BlockDefinition;
			public MyWeaponDefinition WeaponDefinition;
			public List<MyAmmoMagazineDefinition> AmmoList;
			
		}
		
		public static Dictionary<string, MyCubeBlockDefinition> BlockDirectory = new Dictionary<string, MyCubeBlockDefinition>();
		public static Dictionary<string, WeaponProfile> WeaponProfiles = new Dictionary<string, WeaponProfile>();
		public static Dictionary<string, float> PowerProviderBlocks = new Dictionary<string, float>();
		public static List<string> ForwardGunIDs = new List<string>();
		public static List<string> TurretIDs = new List<string>();
		
		public static List<string> BlacklistedWeaponSubtypes = new List<string>();
		public static Dictionary<string, float> PowerDrainingWeapons = new Dictionary<string, float>();
		
		public static bool EnergyShieldModDetected = false;
		public static bool DefenseShieldModDetected = false;
		public static Dictionary<string, float> ShieldBlocksSmallGrid = new Dictionary<string, float>();
		public static Dictionary<string, float> ShieldBlocksLargeGrid = new Dictionary<string, float>();
		
		public static Random Rnd = new Random();
		public static bool SetupComplete = false;
		
		
		public static void ProcessGrid(MyObjectBuilder_CubeGrid cubeGrid, bool allowRandomizeWeapons = false, bool allowReplaceBlocks = true, bool allowShields = false){
			
			if(SetupComplete == false){
				
				SetupComplete = true;
				Setup();
				
			}
			
			Dictionary<Vector3I, MyObjectBuilder_CubeBlock> blockMap = new Dictionary<Vector3I, MyObjectBuilder_CubeBlock>();
			List<MyObjectBuilder_CubeBlock> weaponBlocks = new List<MyObjectBuilder_CubeBlock>();
			List<MyObjectBuilder_CubeBlock> replaceBlocks = new List<MyObjectBuilder_CubeBlock>();
			float availablePower = 0;
			float gridBlockCount = 0;
			bool shieldBlocksDetected = false;
			
			//Build blockMap - This is used to determine which blocks occupy cells.
			foreach(var block in cubeGrid.CubeBlocks){
				
				gridBlockCount++;
				string defIdString = block.GetId().ToString(); //Get MyDefinitionId from ObjectBuilder
				MyCubeBlockDefinition blockDefinition = null;
				
				//Check if block directory has block.
				if(BlockDirectory.ContainsKey(defIdString) == true){
					
					blockDefinition = BlockDirectory[defIdString];
					
				}else{
					
					//If Block Definition Could Not Be Found, It 
					//Likely Means The Target Grid Is Using Modded 
					//Blocks And That Mod Is Not Loaded In The Game 
					//World.
					
					//Logger("Block Definition Could Not Be Found For [" + defIdString + "]. Weapon Randomizer May Produce Unexpected Results.");
					continue;
					
				}
				
				if(PowerProviderBlocks.ContainsKey(defIdString) == true){
					
					availablePower += PowerProviderBlocks[defIdString];
					
				}
				
				//Returns a list of all cells the block occupies
				var cellList = GetBlockCells(block.Min, blockDefinition.Size, block.BlockOrientation);
				
				//Adds to map. Throws warning if a cell was already occupied, since it should not be.
				foreach(var cell in cellList){
					
					if(blockMap.ContainsKey(cell) == false){
						
						blockMap.Add(cell, block);
						
					}else{
						
						//Logger("Cell for "+ defIdString +" Already Occupied By Another Block. This May Cause Issues.");
						
					}
					
				}
				
				//If block was a weapon, add it to the list of weapons we'll be replacing
				if(block as MyObjectBuilder_UserControllableGun != null){
					
					weaponBlocks.Add(block);
					
				}
				
				//TODO: Check CustomData For MES-Replace-Block Tag
				
			}
			
			availablePower *= 0.666f; //We only want to allow 2/3 of grid power to be used by weapons - this should be ok for most NPCs
			
			//Now Process Existing Weapon Blocks
			
			if(allowRandomizeWeapons == true){
				
				foreach(var weaponBlock in weaponBlocks){
					
					//Get details of weapon block being replaced
					string defIdString = weaponBlock.GetId().ToString();
					MyCubeBlockDefinition blockDefinition = BlockDirectory[defIdString];
					var weaponIds = new List<string>(); //Will either be gun or turret IDs, determined below.
					bool isTurret = false;
					
					if(weaponBlock as MyObjectBuilder_TurretBase != null){
						
						isTurret = true;
						weaponIds = new List<string>(TurretIDs);
						
					}else{
						
						weaponIds = new List<string>(ForwardGunIDs);
						
					}
					
					//Get Additional Details From Old Block.
					var oldBlocksCells = GetBlockCells(weaponBlock.Min, blockDefinition.Size, weaponBlock.BlockOrientation);
					var likelyMountingCell = GetLikelyBlockMountingPoint((MyWeaponBlockDefinition)blockDefinition, cubeGrid, blockMap, weaponBlock);
					var oldOrientation = (MyBlockOrientation)weaponBlock.BlockOrientation;
					var oldColor = (Vector3)weaponBlock.ColorMaskHSV;
					var oldLocalForward = GetLocalGridDirection(weaponBlock.BlockOrientation.Forward);
					var oldLocalUp = GetLocalGridDirection(weaponBlock.BlockOrientation.Up);
					
					var oldMatrix = new MatrixI(ref likelyMountingCell, ref oldLocalForward, ref oldLocalUp);
					
					//Remove The Old Block
					cubeGrid.CubeBlocks.Remove(weaponBlock);
					
					foreach(var cell in oldBlocksCells){
						
						blockMap.Remove(cell);
						
					}
					
					//Loop through weapon IDs and choose one at random each run of the loop
					while(weaponIds.Count > 0){
						
						if(weaponIds.Count == 0){
							
							break;
							
						}
						
						var randIndex = Rnd.Next(0, weaponIds.Count);
						var randId = weaponIds[randIndex];
						weaponIds.RemoveAt(randIndex);
						
						if(WeaponProfiles.ContainsKey(randId) == false){
							
							continue;
							
						}
						
						var weaponProfile = WeaponProfiles[randId];
						
						if(weaponProfile.BlockDefinition.CubeSize != cubeGrid.GridSizeEnum){
							
							continue;
							
						}
						
						
						bool isPowerHog = false;
						float powerDrain = 0;
						
						//Check against manually maintained list of Subtypes that draw energy for ammo generation.
						if(PowerDrainingWeapons.ContainsKey(weaponProfile.BlockDefinition.Id.SubtypeName) == true){
							
							if(PowerDrainingWeapons[weaponProfile.BlockDefinition.Id.SubtypeName] > availablePower){
								
								continue;
								
							}
							
							isPowerHog = true;
							powerDrain = PowerDrainingWeapons[weaponProfile.BlockDefinition.Id.SubtypeName];
							
						}
						
						//Calculate Min and Get Block Cells of where new weapon would be placed.
						var estimatedMin = CalculateMinPosition(weaponProfile.BlockDefinition.Size, likelyMountingCell, oldMatrix, isTurret);
						var newBlocksCells = GetBlockCells(estimatedMin, weaponProfile.BlockDefinition.Size, oldOrientation);
						bool foundOccupiedCell = false;
						
						//Check each cell against blockMap - skip weapon if a cell is occupied 
						foreach(var cell in newBlocksCells){
							
							if(blockMap.ContainsKey(cell) == true){
								
								foundOccupiedCell = true;
								break;
								
							}
							
						}
						
						if(foundOccupiedCell == true){
							
							continue;
							
						}
						
						//TODO: Learn How Mount Points Work And Try To Add That Check As Well
						//Existing Method Should Work in Most Cases Though
						
						//Create Object Builder From DefinitionID
						var newBlockBuilder = MyObjectBuilderSerializer.CreateNewObject((SerializableDefinitionId)weaponProfile.BlockDefinition.Id);
						
						//Determine If Weapon Is Turret or Gun. Build Object For That Type
						if(isTurret == true){
							
							var turretBuilder = newBlockBuilder as MyObjectBuilder_TurretBase;
							turretBuilder.EntityId = 0;
							turretBuilder.SubtypeName = weaponProfile.BlockDefinition.Id.SubtypeName;
							turretBuilder.Min = estimatedMin;
							turretBuilder.BlockOrientation = oldOrientation;
							turretBuilder.ColorMaskHSV = oldColor;
							
							var turretDef = (MyLargeTurretBaseDefinition)weaponProfile.BlockDefinition;
							
							if(turretDef.MaxRangeMeters <= 800){
								
								turretBuilder.Range = turretDef.MaxRangeMeters;
								
								
							}else if(gridBlockCount <= 800){
								
								if(turretDef.MaxRangeMeters <= 800){
									
									turretBuilder.Range = turretDef.MaxRangeMeters;
									
								}else{
									
									turretBuilder.Range = 800;
									
								}
								
							}else{
								
								var randRange = (float)Rnd.Next(800, (int)gridBlockCount);
								
								if(randRange > turretDef.MaxRangeMeters){
									
									randRange = turretDef.MaxRangeMeters;
									
								}
								
								turretBuilder.Range = randRange;
								
							}
							
							cubeGrid.CubeBlocks.Add(turretBuilder as MyObjectBuilder_CubeBlock);
							
						}else{
							
							var gunBuilder = newBlockBuilder as MyObjectBuilder_UserControllableGun;
							gunBuilder.EntityId = 0;
							gunBuilder.SubtypeName = weaponProfile.BlockDefinition.Id.SubtypeName;
							gunBuilder.Min = estimatedMin;
							gunBuilder.BlockOrientation = oldOrientation;
							gunBuilder.ColorMaskHSV = oldColor;
							
							cubeGrid.CubeBlocks.Add(gunBuilder as MyObjectBuilder_CubeBlock);
							
						}
						
						if(isPowerHog == true){
							
							availablePower -= powerDrain;
							
						}
						
						foreach(var cell in newBlocksCells){
							
							if(blockMap.ContainsKey(cell) == false){
								
								blockMap.Add(cell, (MyObjectBuilder_CubeBlock)newBlockBuilder);
								
							}
							
						}
						
						break;
						
					}

				}
				
			}
		
			//Process Replace Blocks
			if(allowReplaceBlocks == true){
				
				foreach(var replaceBlock in replaceBlocks){
					
					string defIdString = replaceBlock.TypeId.ToString() + "/" + replaceBlock.SubtypeName;
					MyCubeBlockDefinition blockDefinition = BlockDirectory[defIdString];
					var oldBlocksCells = GetBlockCells(replaceBlock.Min, blockDefinition.Size, replaceBlock.BlockOrientation);
					var likelyMountingCell = GetLikelyBlockMountingPoint((MyWeaponBlockDefinition)blockDefinition, cubeGrid, blockMap, replaceBlock);
					
				}
				
			}
			
			//Process Shield Blocks
			if(allowShields == true && gridBlockCount > 250){
				
				if(cubeGrid.GridSizeEnum == MyCubeSize.Small){
					
					//Small Grid Defense Shields
					if(DefenseShieldModDetected == true){
						
						foreach(var cell in blockMap.Keys.ToList()){
							
							if(blockMap[cell].SubtypeName == "SmallBlockArmorBlock" || blockMap[cell].SubtypeName == "SmallHeavyBlockArmorBlock"){
								
								
								
							}
							
						}
					
					//Small Grid Energy Shields
					}else if(EnergyShieldModDetected == true){
						
						
						
					}
					
				}else{
					
					//Large Grid Defense Shields
					if(DefenseShieldModDetected == true){
						
						
						
					//Large Grid Energy Shields
					}else if(EnergyShieldModDetected == true){
						
						
						
					}
					
				}
				
			}

		}
		
		public static void Setup(){
			
			try{
				
				//Setup Blacklisted Subtype IDs - Move This To A Separate Config Someday
				BlacklistedWeaponSubtypes.Add("Large_SC_LaserDrill_HiddenStatic");//Automated Beam Turrets
				BlacklistedWeaponSubtypes.Add("Large_SC_LaserDrill_HiddenTurret");//Automated Beam Turrets
				BlacklistedWeaponSubtypes.Add("Large_SC_LaserDrill");//Automated Beam Turrets
				BlacklistedWeaponSubtypes.Add("Large_SC_LaserDrillTurret");//Automated Beam Turrets
				BlacklistedWeaponSubtypes.Add("Spotlight_Turret_Large");//Shaostoul - Spotlight Turret
				BlacklistedWeaponSubtypes.Add("Spotlight_Turret_Light_Large");//Shaostoul - Spotlight Turret
				BlacklistedWeaponSubtypes.Add("Spotlight_Turret_Small");//Shaostoul - Spotlight Turret
				BlacklistedWeaponSubtypes.Add("SmallSpotlight_Turret_Small");//Shaostoul - Spotlight Turret
				BlacklistedWeaponSubtypes.Add("ShieldChargerBase_Large");//GSF - Shield Charger
				BlacklistedWeaponSubtypes.Add("LDualPulseLaserBase_Large");//GSF - GTF Large Dual Pulse Laser Turret
				BlacklistedWeaponSubtypes.Add("AegisLargeBeamBase_Large");//GSF - Aegis Large Multi-Laser
				BlacklistedWeaponSubtypes.Add("AegisMediumeamBase_Large");//GSF - Aegis Medium Multi-Laser
				BlacklistedWeaponSubtypes.Add("XLGigaBeamGTFBase_Large");//GSF - GTF XL Citadel Dual Beam Turret
				BlacklistedWeaponSubtypes.Add("XLDualPulseLaserBase_Large");//GSF - GTF XL Citadel Dual Pulse Turret
				
				//Setup Power Hogging Weapons Reference
				PowerDrainingWeapons.Add("NovaTorpedoLauncher_Large", 20); //Nova Heavy Plasma Torpedo
				PowerDrainingWeapons.Add("LargeDualBeamGTFBase_Large", 1050); //GTF Large Dual Beam Laser Turret
				PowerDrainingWeapons.Add("LargeStaticLBeamGTF_Small", 787.50f); //GTF Large Heavy Beam Laser
				PowerDrainingWeapons.Add("LargeStaticLBeamGTF_Large", 787.50f); //GTF Large Heavy Beam Laser
				PowerDrainingWeapons.Add("AegisSmallBeamBase_Large", 828); //Aegis Small Multi-Laser
				PowerDrainingWeapons.Add("AegisMarauderBeamStatic_Large", 18400); //Aegis Gungnir Large Beam Cannon
				PowerDrainingWeapons.Add("MediumQuadBeamGTFBase_Large", 330); //GTF Medium Quad Beam Turret
				PowerDrainingWeapons.Add("MPulseLaserBase_Small", 225); //GTF Medium Pulse Turret
				PowerDrainingWeapons.Add("MPulseLaserBase_Large", 225); //GTF Medium Pulse Turret
				PowerDrainingWeapons.Add("AegisLargeBeamStatic_Large", 7820); //Aegis Large Static Beam Laser
				PowerDrainingWeapons.Add("AegisMediumBeamStaticS_Small", 1797.45f); //Aegis Medium Static Beam Laser
				PowerDrainingWeapons.Add("AegisMediumBeamStatic_Large", 1797.45f); //Aegis Medium Static Beam Laser
				PowerDrainingWeapons.Add("AegisSmallBeamStatic_Large", 828); //Aegis Small Static Beam Laser
				PowerDrainingWeapons.Add("SDualPlasmaBase_Large", 6); //GTF Small Dual Blaster Turret
				PowerDrainingWeapons.Add("LSDualPlasmaStatic_Small", 12); //GTF Dual Static Blaster
				PowerDrainingWeapons.Add("LSDualPlasmaStatic_Large", 12); //GTF Dual Static Blaster
				PowerDrainingWeapons.Add("MDualPlasmaBase_Large", 12); //GTF Medium Dual Blaster Turret
				PowerDrainingWeapons.Add("ThorStatic_Small", 10); //Thor Plasma Cannon
				PowerDrainingWeapons.Add("ThorStatic_Large", 10); //Thor Plasma Cannon
				PowerDrainingWeapons.Add("LPlasmaTriBlasterBase_Large", 24); //GTF Large Tri Blaster Turret
				PowerDrainingWeapons.Add("ThorTurretBase_Large", 10); //Thor Dual Plasma Cannon
				PowerDrainingWeapons.Add("XLCitadelPlasmaCannonBarrel_Large", 48); //GTF XL Citadel Plasma Cannon Turret
				PowerDrainingWeapons.Add("SmallBeamBaseGTF_Large", 15); //GTF Small Beam Turret
				PowerDrainingWeapons.Add("SSmallBeamStaticGTF_Small", 15); //
				PowerDrainingWeapons.Add("Interior_Pulse_Laser_Base_Large", 15); //GTF Pulse Laser Interior Turret
				PowerDrainingWeapons.Add("SmallPulseLaser_Base_Large", 15); //GTF Small Pulse Turret
				PowerDrainingWeapons.Add("MediumStaticLPulseGTF_Small", 241.50f); //GTF Medium Static Pulse Laser
				PowerDrainingWeapons.Add("MediumStaticLPulseGTF_Large", 241.50f); //GTF Medium Static Pulse Laser

				//Shield Blocks
				ShieldBlocksSmallGrid.Add("SmallShipMicroShieldGeneratorBase", 0.2f);
				ShieldBlocksSmallGrid.Add("SmallShipSmallShieldGeneratorBase", 4.5f);
				
				ShieldBlocksLargeGrid.Add("LargeShipSmallShieldGeneratorBase", 4.5f);
				ShieldBlocksLargeGrid.Add("LargeShipLargeShieldGeneratorBase", 85);

			}catch(Exception exc){
				
				Logger.AddMsg("Caught Error Setting Up Weapon Replacer Blacklist and Power-Draining Weapon References.");
				Logger.AddMsg("Unwanted Blocks May Be Used When Replacing Weapons.");
				
			}
						
			var allDefs = MyDefinitionManager.Static.GetAllDefinitions();
			
			//Check For Energy Shield Mod
			if(MES_SessionCore.ActiveMods.Contains(484504816) == true){
				
				EnergyShieldModDetected = true;
				
			}
			
			//Check For Defense Shield Mod
			if(MES_SessionCore.ActiveMods.Contains(1365616918) == true || MES_SessionCore.ActiveMods.Contains(1492804882) == true){
				
				DefenseShieldModDetected = true;
				
			}
				
			foreach(MyDefinitionBase definition in allDefs.Where( x => x is MyCubeBlockDefinition)){
				
				if(BlockDirectory.ContainsKey(definition.Id.ToString()) == false){
					
					BlockDirectory.Add(definition.Id.ToString(), definition as MyCubeBlockDefinition);
					
				}else{
					
					Logger.AddMsg("Block Reference Setup Found Duplicate DefinitionId Detected And Skipped: " + definition.Id.ToString());
					continue;
					
				}
				
				if(BlacklistedWeaponSubtypes.Contains(definition.Id.SubtypeName) == true){
					
					continue;
					
				}
				
				if(definition as MyPowerProducerDefinition != null){
					
					var powerBlock = definition as MyPowerProducerDefinition;
					
					if(PowerProviderBlocks.ContainsKey(definition.Id.ToString()) == false){
						
						PowerProviderBlocks.Add(definition.Id.ToString(), powerBlock.MaxPowerOutput);
						
					}
					
				}
				
				var weaponBlock = definition as MyWeaponBlockDefinition;
				
				if(weaponBlock == null || definition.Public == false){
					
					continue;
					
				}
					
				var weaponDefinition = MyDefinitionManager.Static.GetWeaponDefinition(weaponBlock.WeaponDefinitionId);
				
				if(weaponDefinition == null){
					
					continue;
					
				}
				
				var ammoDefList = new List<MyAmmoMagazineDefinition>();
				
				foreach(var defId in weaponDefinition.AmmoMagazinesId){
					
					var ammoMagDef = MyDefinitionManager.Static.GetAmmoMagazineDefinition(defId);
					
					if(ammoMagDef != null){
						
						ammoDefList.Add(ammoMagDef);
						
					}
					
				}
				
				WeaponProfile weaponProfile;
				weaponProfile.BlockDefinition = weaponBlock;
				weaponProfile.WeaponDefinition = weaponDefinition;
				weaponProfile.AmmoList = ammoDefList;
				
				bool goodSize = false;
				
				if(weaponBlock as MyLargeTurretBaseDefinition != null){
					
					if(weaponBlock.Size.X == weaponBlock.Size.Z && weaponBlock.Size.X % 2 != 0){
						
						goodSize = true;
						TurretIDs.Add(weaponBlock.Id.ToString());
						
					}
		
				}else{
					
					if(weaponBlock.Size.X == weaponBlock.Size.Y && weaponBlock.Size.X % 2 != 0){
						
						goodSize = true;
						ForwardGunIDs.Add(weaponBlock.Id.ToString());
						
					}
					
				}
				
				if(goodSize == false){
					
					continue;
					
				}
				
				if(WeaponProfiles.ContainsKey(weaponBlock.Id.ToString()) == false){
					
					WeaponProfiles.Add(weaponBlock.Id.ToString(), weaponProfile);
					
				}else{
					
					Logger.AddMsg("Weapon Profile Already Exists And Is Being Skipped For: " + weaponBlock.Id.ToString());
					
				}

			}
			
		}
		
		public static Vector3I CalculateMinPosition(Vector3I size, Vector3I mountingCell, MatrixI mountingMatrix, bool isTurret){
						
			Vector3I minPosition = Vector3I.Zero;
			
			if(isTurret == true){
				
				var cellList = new List<Vector3I>();
				
				//Move Cells Distance
				int moveCellDist = (int)Math.Floor((double)size.X / 2);
				cellList.Add(Vector3I.Transform(new Vector3I(moveCellDist, 0, moveCellDist), mountingMatrix));
				cellList.Add(Vector3I.Transform(new Vector3I(moveCellDist * -1, 0, moveCellDist), mountingMatrix));
				cellList.Add(Vector3I.Transform(new Vector3I(moveCellDist, 0, moveCellDist * -1), mountingMatrix));
				cellList.Add(Vector3I.Transform(new Vector3I(moveCellDist * -1, 0, moveCellDist * -1), mountingMatrix));
				
				cellList.Add(Vector3I.Transform(new Vector3I(moveCellDist, size.Y - 1, moveCellDist), mountingMatrix));
				cellList.Add(Vector3I.Transform(new Vector3I(moveCellDist * -1, size.Y - 1, moveCellDist), mountingMatrix));
				cellList.Add(Vector3I.Transform(new Vector3I(moveCellDist, size.Y - 1, moveCellDist * -1), mountingMatrix));
				cellList.Add(Vector3I.Transform(new Vector3I(moveCellDist * -1, size.Y - 1, moveCellDist * -1), mountingMatrix));
				
				for(int i = 0; i < cellList.Count; i++){
					
					if(i == 0){
						
						minPosition = cellList[i];
						continue;
						
					}
					
					minPosition = Vector3I.Min(minPosition, cellList[i]);
					
				}
					
				
			}else{
				
				var forwardDist = size.Z - 1;
				Vector3I otherEnd = mountingMatrix.ForwardVector * forwardDist + mountingCell;
				minPosition = Vector3I.Min(mountingCell, otherEnd);
				
			}
			
			return minPosition;
			
		}
		
		//This is used to calculate the 'center' position of the block that is used when you get 
		//VRage.Game.ModAPI.Ingame.IMySlimBlock.Position;
		//I didn't write this.. I lifted it from MySlimBlock, since that's the only place it seems
		//to exist.
		public static Vector3I ComputePositionInGrid(MatrixI localMatrix, Vector3I blockCenter, Vector3I blockSize, Vector3I min){
			
			Vector3I center = blockCenter;
			Vector3I vector3I = blockSize - 1;
			Vector3I value;
			Vector3I.TransformNormal(ref vector3I, ref localMatrix, out value);
			Vector3I a;
			Vector3I.TransformNormal(ref center, ref localMatrix, out a);
			Vector3I vector3I2 = Vector3I.Abs(value);
			Vector3I result = a + min;
			
			if (value.X != vector3I2.X){
				
				result.X += vector3I2.X;
				
			}
			
			if (value.Y != vector3I2.Y){
				
				result.Y += vector3I2.Y;
				
			}
			
			if (value.Z != vector3I2.Z){
				
				result.Z += vector3I2.Z;
				
			}
			
			return result;
			
		}
		
		//This returns a list of cells occupied by a block. Useful to get
		//blocks that occupy multiple cells.
		public static List<Vector3I> GetBlockCells(Vector3I Min, Vector3I Size, MyBlockOrientation blockOrientation){
			
			var cellList = new List<Vector3I>();
			cellList.Add(Min);
			
			var localMatrix = new MatrixI(blockOrientation);
			
			for(int i = 0; i < Size.X; i++){
				
				for(int j = 0; j < Size.Y; j++){
					
					for(int k = 0; k < Size.Z; k++){
						
						var stepSize = new Vector3I(i,j,k);
						var transformedSize = Vector3I.TransformNormal(stepSize, ref localMatrix);
						Vector3I.Abs(ref transformedSize, out transformedSize);
						var cell = Min + transformedSize;
						
						if(cellList.Contains(cell) == false){
							
							cellList.Add(cell);
							
						}
						
					}
					
				}
				
			}
			
			return cellList;
			
		}
		
		//
		public static Vector3I GetLikelyBlockMountingPoint(MyWeaponBlockDefinition blockDefinition, MyObjectBuilder_CubeGrid cubeGrid, Dictionary<Vector3I, MyObjectBuilder_CubeBlock> blockMap, MyObjectBuilder_CubeBlock block){
			
			var direction = Vector3I.Zero;
			Vector3I likelyPosition = ComputePositionInGrid(new MatrixI(block.BlockOrientation), blockDefinition.Center, blockDefinition.Size, block.Min);
			
			if(TurretIDs.Contains(blockDefinition.Id.ToString()) == true){
				
				direction = Vector3I.Down;
				
			}else{
				
				direction = Vector3I.Backward;
				
			}
			
			var blockForward = GetLocalGridDirection(block.BlockOrientation.Forward);
			var blockUp = GetLocalGridDirection(block.BlockOrientation.Up);
			var blockLocalMatrix = new MatrixI(ref likelyPosition, ref blockForward, ref blockUp);
			bool loopBreak = false;
			
			while(loopBreak == false){
				
				var checkCell = Vector3I.Transform(direction, blockLocalMatrix);
				blockLocalMatrix = new MatrixI(ref checkCell, ref blockForward, ref blockUp);
				
				if(blockMap.ContainsKey(checkCell) == true){
					
					if(blockMap[checkCell] == block){
						
						likelyPosition = checkCell;
						
					}else{
						
						break;
						
					}
					
				}else{
					
					break;
					
				}
	
			}
			
			return likelyPosition;
			
		}
		
		//Translates a Base6Directions direction into a Vector3I
		public static Vector3I GetLocalGridDirection(Base6Directions.Direction Direction){
			
			if(Direction == Base6Directions.Direction.Forward){
				
				return new Vector3I(0,0,-1);
				
			}
			
			if(Direction == Base6Directions.Direction.Backward){
				
				return new Vector3I(0,0,1);
				
			}
			
			if(Direction == Base6Directions.Direction.Up){
				
				return new Vector3I(0,1,0);
				
			}
			
			if(Direction == Base6Directions.Direction.Down){
				
				return new Vector3I(0,-1,0);
				
			}
			
			if(Direction == Base6Directions.Direction.Left){
				
				return new Vector3I(-1,0,0);
				
			}
			
			if(Direction == Base6Directions.Direction.Right){
				
				return new Vector3I(1,0,0);
				
			}
			
			return Vector3I.Zero;
			
		}
		
	}
	
}