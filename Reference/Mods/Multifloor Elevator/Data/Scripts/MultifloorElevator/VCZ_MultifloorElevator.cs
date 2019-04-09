using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character.Components;
using Sandbox.Game.Lights;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace Vicizlat.MultifloorElevator
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class ElevatorSessionComp : MySessionComponentBase
    {
        public static bool _init;

        public override void UpdateBeforeSimulation()
        {
            base.UpdateBeforeSimulation();
            if (MyAPIGateway.Session == null) return;
            if (!_init)
            {
                Logging.Instance.WriteLine("<<< Debug Log Started >>>");
                Communication.RegisterHandlers();
                _init = true;
            }
        }

        protected override void UnloadData()
        {
            Communication.UnregisterHandlers();
            Logging.Instance.WriteLine("<<< Debug Log Closed >>>");
            Logging.Instance.Close();
        }
    }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_AdvancedDoor), false, new string[]
    { "VCZ_Elevator_15ms", "VCZ_Elevator_9ms", "VCZ_Elevator", "VCZ_Elevator_5ms", "VCZ_Elevator_3ms", "VCZ_Elevator_1ms" })]
    public partial class MultifloorElevator : MyGameLogicComponent
    {
        private readonly int MaxHeight = 198;                           // Max Blocks the elevator can travel(200-2)
        private Vector3D minCabin = new Vector3D(-2.0f, -4.5f, -2.0f);  // LEFT, BOTTOM(-2f - 2.5), FRONT
        private Vector3D maxCabin = new Vector3D(2.0f, -0.5f, 2.0f);    // RIGHT, TOP(2f - 2.5), BACK
        private float CABIN_SPHERE_RADIUS = 3.5f;

        private int StartingFloor;
        private bool TargetFloorChanged = true;
        public int TargetBlockCompare = 1;
        private int oldTargetBlockCompare = 0;
        private bool CurrentFloorChanged = true;
        public int CurrentBlockCompare = 1;
        private int oldCurrentBlockCompare = 0;
        private bool GridBlocksCountChanged = true;
        private int ElevatorBlocksCount = 0;
        private int oldElevatorBlocksCount = 1;

        private IMyAdvancedDoor Elevator_block;
        private IMyAdvancedDoor CurrentBlock;
        private IMyAdvancedDoor TargetBlock;
        private IMyAdvancedDoor StartingBlock;
        private readonly IMyAdvancedDoor[] _floors = new IMyAdvancedDoor[9];

        public MyEntitySubpart ElevatorCabin;
        private MatrixD TopFloorMatrix;
        private Vector3D CabinPosition;
        private bool RunOnce = true;
        private bool LockedLogWritten = false;
        private bool ShouldMoveEntity = false;
        private bool ElevatorReady;
        private int frame = -1;
        private int second = -1;

        private readonly List<string> ElevatorIDs = new List<string>
        { "VCZ_Elevator_Middle", "VCZ_Elevator_Top", "VCZ_Elevator_Filler", "VCZ_Elevator_FillerDouble", "VCZ_Elevator_Filler_Vent" };
        private List<IMyCubeBlock> FloorsList = new List<IMyCubeBlock>();
        private List<IMyEntity> EntitiesList = new List<IMyEntity>();
        private List<IMyEntity> ContainedEntitiesList = new List<IMyEntity>();
        private IMyEntity[] ContainedEntity;
        private Vector3D[] offset;

        private string ElevatorStatus;
        private string TooFarAway;
        private string BottomFloorStatus;
        private int MiddleFloorsCount;
        private string TopFloorStatus;
        private int ElevatorHeight;
        private string NotStarted;
        private string Unfinished;
        private string ExtraFloors;
        private string ReachedMaxH;

        private bool IgnoreDistanceLock;
        private bool oldIgnoreDistanceLock;
        private bool PlayElevatorMusic;
        private bool oldPlayElevatorMusic;
        private bool PlayElevatorMusicNearCabin;
        private bool oldPlayElevatorMusicNearCabin;
        private int MusicSelector;
        private int oldMusicSelector;
        private int MusicVolume;
        private int oldMusicVolume;
        private MySoundPair ElevatorMusic;
        private MyEntity3DSoundEmitter Sound;
        private MyLight Light;
        private Color CabinLightColor = new Color(255, 255, 255);
        private Color oldCabinLightColor;
        private float CabinLightRange;
        private float oldCabinLightRange;
        private float CabinLightIntensity;
        private float oldCabinLightIntensity;

        //private MyEntitySubpart ChristmasLights;
        //private bool ShowChristmasLights = true;
        //private bool oldShowChristmasLights;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME; // Sets the update rate.
            Elevator_block = (IMyAdvancedDoor)Entity; // Required.
            Elevator_block.AppendingCustomInfo += AppendingCustomInfo;
        }

        public override void Close()
        {
            NeedsUpdate = MyEntityUpdateEnum.NONE; // Required.
            MyLights.RemoveLight(Light);
        }

        public override void UpdateAfterSimulation()
        {
            try
            {
                if (!ElevatorSessionComp._init) return;
                if (Elevator_block.CubeGrid.Physics == null) return;     // Ignore ghost grids (projections).
                if (!Elevator_block.IsFunctional) return;                // Ignore damaged or build progress blocks.
                if (frame++ == 59) frame = 0;
                if (frame == 0 && second++ == 9) second = 0;

                UpdateOnce();

                if (ElevatorCabin.Closed.Equals(true))
                {
                    ElevatorCabin.Subparts.Clear();
                    GetSubparts();
                }

                if (frame == 25) CheckTerminalControlSettings();
                if (frame == 55) SaveTerminalControlSettings();
                if ((second == 0 && frame == 0) || (second == 5 && frame == 0)) CheckBlocks();
                if ((second == 1 && frame == 0) || (second == 6 && frame == 0)) FindFloors();
                if (second == 9 && frame == 0 && !IgnoreDistanceLock) DistanceLock();

                if (ElevatorReady)
                {
                    GetCurrentFloor();
                    CloseAndLockFloors(false);
                    CheckFloorDisplays();
                    UpdateFloorDisplays();
                    if (!ShouldMoveEntity)
                    {
                        UpdateEntities();
                        FindDesiredFloor();
                    }
                    else
                    {
                        if (StartingBlock != TargetBlock) StartingBlock.CloseDoor();
                        else StopElevator();
                        StartElevator();
                        MoveEntity();
                    }
                    bool RunawayCabin = Vector3D.Distance(TopFloorMatrix.Translation + (TopFloorMatrix.Up * 2.5), CabinPosition) <= 2.3f;
                    if (RunawayCabin) Elevator_block.CloseDoor(); // Return Cabin if it tries to go above Top Floor.
                    if (TargetBlock == _floors[0]) StopElevator(0.01f); // Stop any Elevator with max precision for the First Floor.
                    else
                    {
                        if (Elevator_block.BlockDefinition.SubtypeId == "VCZ_Elevator_15ms") StopElevator(0.15f); // Use lower precision for the fast Elevator.
                        else StopElevator(0.1f); // Stop all other Elevators with normal precision for every other Floor.
                    }
                }
                else CloseAndLockFloors(true);

                UpdateExtras();
            }
            catch (Exception e)
            {
                Logging.Instance.WriteLine("[ Error in 'UpdateAfterSimulation': " + e.Message + " ]");
            }
        }

        private void UpdateExtras()
        {
            if (!MyAPIGateway.Utilities.IsDedicated)
            {
                //UpdateChristmasLights();
                Light.Color = CabinLightColor;
                Elevator_block.SetEmissivePartsForSubparts("Light", new Color(CabinLightColor.R, CabinLightColor.G, CabinLightColor.B), 0f);
                Light.Range = CabinLightRange;
                Light.Intensity = CabinLightIntensity;
                Light.Falloff = 1f;
                Light.Position = CabinPosition + (ElevatorCabin.WorldMatrix.Up * 1f);
                Light.LightOn = true;
                Light.UpdateLight();

                ElevatorMusic = new MySoundPair("ElevatorMusic" + MusicSelector);
                Sound.VolumeMultiplier = ((float)MusicVolume / 10);
                Sound.SetPosition(CabinPosition);
                Sound.Update();
            }
        }

        private void UpdateOnce()
        {
            if (!RunOnce) return;
            RunOnce = false;
            GetSubparts();
            LoadTerminalControlsSettings();
            CreateTerminalControls();
            if (!MyAPIGateway.Utilities.IsDedicated)
            {
                Sound = new MyEntity3DSoundEmitter(Elevator_block as MyEntity, false, 0) { CustomMaxDistance = 2.5f };
                Light = MyLights.AddLight();
                Light.Start("Light");
            }
        }

        private void GetSubparts()
        {
            try
            {
                Elevator_block.TryGetSubpart("VCZ_Elevator_Cabin", out ElevatorCabin);
                //Elevator_block.TryGetSubpart("VCZ_Elevator_ChristmasLights", out ChristmasLights);
                Elevator_block.SetEmissivePartsForSubparts("BlackDisplay", BLACK, 0f);
                GridBlocksCountChanged = true;
                CurrentFloorChanged = true;
            }
            catch (Exception e)
            {
                Logging.Instance.WriteLine("[ Error in 'GetSubparts': " + e.Message + " ]");
            }
        }

        private void CheckBlocks()
        {
            try
            {
                ElevatorBlocksCount = 0;
                var PrevBlock = Elevator_block.CubeGrid.GetCubeBlock(Elevator_block.Position + (Vector3I)Elevator_block.LocalMatrix.Down);
                if (PrevBlock != null) ElevatorBlocksCount++;
                for (int diff = 1; diff <= MaxHeight; diff++)
                {
                    var NextBlock = Elevator_block.CubeGrid.GetCubeBlock(Elevator_block.Position + (Vector3I)(Elevator_block.LocalMatrix.Up * diff));
                    if (NextBlock != null)
                    {
                        ElevatorBlocksCount++;
                        if (NextBlock.FatBlock.BlockDefinition.SubtypeId == "VCZ_Elevator_Top") break;
                    }
                }
                if (!GridBlocksCountChanged) GridBlocksCountChanged = ElevatorBlocksCount != oldElevatorBlocksCount;
                oldElevatorBlocksCount = ElevatorBlocksCount;
            }
            catch (Exception e)
            {
                Logging.Instance.WriteLine("[ Error in 'CheckBlocks': " + e.Message + " ]");
            }
        }

        private void FindFloors()
        {
            try
            {
                if (GridBlocksCountChanged)
                {
                    ElevatorReady = false;
                    MiddleFloorsCount = 0;
                    FloorsList.Clear();

                    var PrevBlock = Elevator_block.CubeGrid.GetCubeBlock(Elevator_block.Position + (Vector3I)Elevator_block.LocalMatrix.Down);
                    if (PrevBlock == null || PrevBlock.BlockDefinition.Id.SubtypeName != "VCZ_Elevator_Bottom")
                    {
                        BottomFloorStatus = "Error!";
                        NotStarted = "\nFirst floor not found \n or block found is not \n 'VCZ Elevator Bottom'.";
                    }
                    else
                    {
                        FloorsList.Add(PrevBlock.FatBlock);
                        NotStarted = "";
                        BottomFloorStatus = "Ready!";
                        for (int diff = 1; diff <= MaxHeight; diff++)
                        {
                            Vector3I NextBlockPos = Elevator_block.Position + (Vector3I)(Elevator_block.LocalMatrix.Up * diff);
                            IMySlimBlock NextBlock = Elevator_block.CubeGrid.GetCubeBlock(NextBlockPos);
                            if (NextBlock == null || !ElevatorIDs.Contains(NextBlock.BlockDefinition.Id.SubtypeName))
                            {
                                TopFloorStatus = "Error!";
                                Unfinished = "\nTop floor not found or \n elevator's path is blocked!";
                                break;
                            }
                            if (NextBlock.FatBlock is IMyAdvancedDoor)
                            {
                                FloorsList.Add(NextBlock.FatBlock);
                                if (NextBlock.FatBlock.BlockDefinition.SubtypeId == "VCZ_Elevator_Top")
                                {
                                    TopFloorMatrix = NextBlock.FatBlock.WorldMatrix;
                                    TopFloorStatus = "Ready!";
                                    Unfinished = "";
                                    ElevatorHeight = diff + 2;
                                    ElevatorReady = FloorsList.Count <= 9;
                                    break;
                                }
                                else MiddleFloorsCount++;
                            }
                            if (diff == MaxHeight && NextBlock.FatBlock.BlockDefinition.SubtypeId != "VCZ_Elevator_Top")
                            {
                                TopFloorStatus = "Error!";
                                ReachedMaxH = "\nMax Elevator height without Top floor!";
                            }
                            else ReachedMaxH = "";
                        }
                        ExtraFloors = FloorsList.Count > 9 ? "\nMore than the supproted 9 floors!" : "";
                    }

                    if (ElevatorReady)
                    {
                        for (int i = 0; i <= 8; i++)
                        {
                            _floors[i] = null;
                            Elevator_block.SetEmissivePartsForSubparts("LightFloor" + (i + 1), BLACK, 0f);
                            Elevator_block.SetEmissivePartsForSubparts("LookingAtLightFloor" + (i + 1), BLACK, 0f);
                            foreach (var Floor in FloorsList)
                            {
                                Floor.SetEmissivePartsForSubparts("LightFloor" + (i + 1), BLACK, 0f);
                            }
                        }
                        for (int i = 0; i < FloorsList.Count; i++)
                        {
                            _floors[i] = FloorsList.ElementAt(i) as IMyAdvancedDoor;
                            Elevator_block.SetEmissivePartsForSubparts("LightFloor" + (i + 1), WHITE, 1f);
                            foreach (var Floor in FloorsList)
                            {
                                Floor.SetEmissivePartsForSubparts("LightFloor" + (i + 1), WHITE, 1f);
                            }
                            _floors[i].SetEmissiveParts("BlackDisplay", BLACK, 0f);
                            _floors[i].SetEmissivePartsForSubparts("BlackDisplay", BLACK, 0f);
                            SetDigit(_floors[i], "ThisFloorDisplay_", i + 1, ORANGE, 1f);
                            if (_floors[i].BlockDefinition.SubtypeId == "VCZ_Elevator_Top") break;
                        }
                        ElevatorStatus = "Ready!";
                        CurrentFloorChanged = true;
                        TargetFloorChanged = true;
                    }
                    else ElevatorStatus = "Unfinished!";
                    Elevator_block.RefreshCustomInfo();
                    GridBlocksCountChanged = false;
                }
            }
            catch (Exception e)
            {
                Logging.Instance.WriteLine("[ Error in 'FindFloors': " + e.Message + " ]");
            }
        }

        private void DistanceLock()
        {
            if (Vector3D.Distance(Elevator_block.WorldMatrix.Translation, new Vector3D(0)) > 50000000)
            {
                if (!LockedLogWritten)
                {
                    Logging.Instance.WriteLine("Elevator locked! Distance from world center too large.");
                    LockedLogWritten = true;
                }
                Elevator_block.Enabled = false;
                ElevatorStatus = "Locked!";
                TooFarAway = "\nDistance from world center is\nmore than 50 000 000 meters.\nThis causes unpredictable behavior.";
                ElevatorReady = false;
                Elevator_block.RefreshCustomInfo();
            }
            else
            {
                LockedLogWritten = false;
                TooFarAway = "";
                GridBlocksCountChanged = true;
            }
        }

        private void AppendingCustomInfo(IMyTerminalBlock block, StringBuilder stringBuilder)
        {
            try
            {
                stringBuilder.Clear();
                stringBuilder.AppendLine("[> Elevator Status <]: " + ElevatorStatus);
                stringBuilder.AppendLine("Bottom Floor Status: " + BottomFloorStatus);
                stringBuilder.AppendLine("Middle Floors Count: " + MiddleFloorsCount);
                stringBuilder.AppendLine("Top Floor Status:    " + TopFloorStatus);
                stringBuilder.AppendLine("\n[Elevator Height]: " + ElevatorHeight + " blocks");
                stringBuilder.AppendLine("[Floors Count]: " + FloorsList.Count);
                stringBuilder.AppendLine(ElevatorReady ? "" : "\n[ Errors ]:" + NotStarted + Unfinished + ExtraFloors + ReachedMaxH + TooFarAway);
                if (!ElevatorReady) stringBuilder.AppendLine("\nElevator checks for new blocks \n every 5 seconds.");
                if (Elevator_block.BlockDefinition.SubtypeId == "VCZ_Elevator_1ms") stringBuilder.AppendLine("\nRequired power: 10kW");
                if (Elevator_block.BlockDefinition.SubtypeId == "VCZ_Elevator_3ms") stringBuilder.AppendLine("\nRequired power: 30kW");
                if (Elevator_block.BlockDefinition.SubtypeId == "VCZ_Elevator_5ms") stringBuilder.AppendLine("\nRequired power: 50kW");
                if (Elevator_block.BlockDefinition.SubtypeId == "VCZ_Elevator") stringBuilder.AppendLine("\nRequired power: 70kW");
                if (Elevator_block.BlockDefinition.SubtypeId == "VCZ_Elevator_9ms") stringBuilder.AppendLine("\nRequired power: 90kW");
                if (Elevator_block.BlockDefinition.SubtypeId == "VCZ_Elevator_15ms") stringBuilder.AppendLine("\nRequired power: 150kW");
            }
            catch (Exception e)
            {
                Logging.Instance.WriteLine("[ Error in 'AppendingCustomInfo': " + e.Message + " ]");
            }
        }

        private void GetCurrentFloor()
        {
            try
            {
                CabinPosition = ElevatorCabin.WorldMatrix.Translation + (ElevatorCabin.WorldMatrix.Down * 2.5);
                var CurrentFloorGridPos = Elevator_block.CubeGrid.WorldToGridInteger(CabinPosition);
                if (Elevator_block.CubeGrid.GetCubeBlock(CurrentFloorGridPos) != null)
                {
                    if (Elevator_block.CubeGrid.GetCubeBlock(CurrentFloorGridPos).FatBlock is IMyAdvancedDoor)
                    {
                        CurrentBlock = Elevator_block.CubeGrid.GetCubeBlock(CurrentFloorGridPos).FatBlock as IMyAdvancedDoor;
                    }
                    for (int i = 0; i <= 8; i++)
                    {
                        if (CurrentBlock == _floors[i])
                        {
                            CurrentBlockCompare = i + 1;
                            break;
                        }
                    }
                    if (TargetBlock == null) TargetBlock = CurrentBlock;
                }
            }
            catch (Exception e)
            {
                Logging.Instance.WriteLine("[ Error in 'GetCurrentFloor': " + e.Message + " ]");
            }
        }

        private void CloseAndLockFloors(bool LockAllFloors)
        {
            foreach (IMyAdvancedDoor Floor in FloorsList)
            {
                if (!LockAllFloors && Floor == CurrentBlock) continue;
                else Floor.CloseDoor();
                if (Floor.OpenRatio == 0) Floor.Enabled = false;
            }
        }

        private void CheckFloorDisplays()
        {
            try
            {
                if (!CurrentFloorChanged) CurrentFloorChanged = CurrentBlockCompare != oldCurrentBlockCompare;
                oldCurrentBlockCompare = CurrentBlockCompare;
                if (!TargetFloorChanged) TargetFloorChanged = TargetBlockCompare != oldTargetBlockCompare;
                oldTargetBlockCompare = TargetBlockCompare;
            }
            catch (Exception e)
            {
                Logging.Instance.WriteLine("[ Error in 'CheckFloorDisplays': " + e.Message + " ]");
            }
        }

        private void UpdateFloorDisplays()
        {
            try
            {
                if (CurrentFloorChanged)
                {
                    foreach (IMyAdvancedDoor Floor in FloorsList) SetDigit(Floor, "CurrentFloorDisplay_", CurrentBlockCompare, ORANGE, 1f);
                    SetDigitSubpart(Elevator_block, "CurrentFloorDisplay_", CurrentBlockCompare, ORANGE, 1f);
                    CurrentFloorChanged = false;
                }
                if (TargetFloorChanged)
                {
                    foreach (IMyAdvancedDoor Floor in FloorsList)
                    {
                        if (TargetBlockCompare > CurrentBlockCompare) SetDigit(Floor, "TargetFloorDisplay_", TargetBlockCompare, ORANGE, 1f);
                        else SetDigit(Floor, "TargetFloorDisplayLower_", TargetBlockCompare, ORANGE, 1f);
                    }
                    if (TargetBlockCompare == CurrentBlockCompare)
                    {
                        for (int i = 1; i <= FloorsList.Count; i++)
                        {
                            Elevator_block.SetEmissivePartsForSubparts("LightFloor" + i, WHITE, 1f);
                            Elevator_block.SetEmissivePartsForSubparts("LookingAtLightFloor" + i, BLACK, 0f);
                        }
                        foreach (IMyAdvancedDoor Floor in FloorsList)
                        {
                            Floor.SetEmissiveParts("CallBtn", BLACK, 0f);
                            Floor.SetEmissiveParts("Moving", BLACK, 0f);
                            Floor.SetEmissiveParts("MovingDown", BLACK, 0f);
                            SetDigit(Floor, "TargetFloorDisplay_", 8, BLACK, 0f);
                            SetDigit(Floor, "TargetFloorDisplayLower_", 8, BLACK, 0f);
                        }
                    }
                    TargetFloorChanged = false;
                }
            }
            catch (Exception e)
            {
                Logging.Instance.WriteLine("[ Error in 'UpdateFloorDisplays': " + e.Message + " ]");
            }
        }

        private void UpdateEntities()
        {
            try
            {
                var cabinOBB = new MyOrientedBoundingBoxD(new BoundingBoxD(minCabin, maxCabin), ElevatorCabin.WorldMatrix);
                var sphere = new BoundingSphereD(CabinPosition, CABIN_SPHERE_RADIUS);
                EntitiesList.Clear();
                ContainedEntitiesList.Clear();
                EntitiesList = MyAPIGateway.Entities.GetTopMostEntitiesInSphere(ref sphere);
                foreach (IMyEntity entity in EntitiesList)
                {
                    if (entity is IMyCharacter || entity is IMyFloatingObject || entity is IMyInventoryBag)
                    {
                        var minEntity = entity.Model != null ? entity.Model.BoundingBox.Min : entity.LocalAABB.Min;
                        var maxEntity = entity.Model != null ? entity.Model.BoundingBox.Max : entity.LocalAABB.Max;
                        var entityOBB = new MyOrientedBoundingBoxD(new BoundingBoxD(minEntity, maxEntity), entity.WorldMatrix);
                        if (cabinOBB.Contains(ref entityOBB) == ContainmentType.Contains)
                        {
                            ContainedEntitiesList.Add(entity);
                        }
                    }
                }
                if (ContainedEntitiesList.Count > 0)
                {
                    ContainedEntity = new IMyEntity[ContainedEntitiesList.Count];
                    offset = new Vector3D[ContainedEntitiesList.Count];
                    for (int i = 0; i < ContainedEntitiesList.Count; i++)
                    {
                        ContainedEntity[i] = ContainedEntitiesList.ElementAt(i) as IMyEntity;
                        offset[i] = ContainedEntity[i].WorldMatrix.Translation - CabinPosition;
                    }
                }
            }
            catch (Exception e)
            {
                Logging.Instance.WriteLine("[ Error in 'UpdateEntities': " + e.Message + " ]");
            }
        }

        private void FindDesiredFloor()
        {
            try
            {
                if (MyAPIGateway.Gui.ActiveGamePlayScreen != null) return;
                if (!MyAPIGateway.Multiplayer.IsServer || !MyAPIGateway.Utilities.IsDedicated)
                {
                    if (MyAPIGateway.Session.Player == null) return;
                    if (MyAPIGateway.Session.Player.Character == null) return;
                    for (int i = 1; i <= 9; i++) CurrentBlock.SetEmissivePartsForSubparts("LookingAtLightFloor" + i, BLACK, 0f);
                    var player = MyAPIGateway.Session.Player.Character;
                    if (PlayElevatorMusic && PlayElevatorMusicNearCabin && Vector3D.Distance(player.PositionComp.GetPosition(), CabinPosition) < 3f)
                    {
                        if (!Sound.IsPlaying) Sound.PlaySound(ElevatorMusic, false, false, false, true);
                    }
                    else Sound.StopSound(true);
                    if (player.Components.Get<MyCharacterDetectorComponent>()?.UseObject == null) return;
                    var DetectedEntity = player.Components.Get<MyCharacterDetectorComponent>().DetectedEntity;
                    if (!FloorsList.Contains(DetectedEntity)) return;
                    var dummy = player.Components.Get<MyCharacterDetectorComponent>()?.UseObject.Dummy as IMyModelDummy;
                    bool PressedUse = MyAPIGateway.Input.IsGameControlPressed(MyControlsSpace.USE);
                    bool PressedPrimTool = MyAPIGateway.Input.IsGameControlPressed(MyControlsSpace.PRIMARY_TOOL_ACTION);
                    for (int i = 1; i <= FloorsList.Count; i++)
                    {
                        if (dummy.Name == "detector_advanceddoor_010" && DetectedEntity == _floors[i - 1])
                        {
                            if (PressedUse || PressedPrimTool)
                            {
                                if (i != CurrentBlockCompare) Communication.RequestFloor(i, Elevator_block.EntityId);
                            }
                            break;
                        }
                        if (dummy.Name == "detector_advanceddoor_00" + i || dummy.Name == "detector_advanceddoor_01" + i ||
                            dummy.Name == "detector_advanceddoor_02" + i || dummy.Name == "detector_advanceddoor_03" + i)
                        {
                            CurrentBlock.SetEmissivePartsForSubparts("LookingAtLightFloor" + i, ORANGE, 1f);
                            if (PressedUse || PressedPrimTool)
                            {
                                if (i != CurrentBlockCompare) Communication.RequestFloor(i, Elevator_block.EntityId);
                            }
                            break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logging.Instance.WriteLine("[ Error in 'FindDesiredFloor': " + e.Message + " ]");
            }
        }

        public void SetTargetFloor(int selectedFloor)
        {
            try
            {
                Logging.Instance.WriteLine("Setting target floor: " + selectedFloor);
                TargetBlock = _floors[selectedFloor - 1];
                TargetBlockCompare = selectedFloor;
                StartingBlock = CurrentBlock;
                StartingFloor = CurrentBlockCompare;
                TargetBlock.SetEmissiveParts("CallBtn", ORANGE, 1f);
                Elevator_block.SetEmissivePartsForSubparts("LightFloor" + selectedFloor, ORANGE, 1f);
                Elevator_block.SetEmissivePartsForSubparts("LookingAtLightFloor" + selectedFloor, ORANGE, 1f);
                ShouldMoveEntity = true;
            }
            catch (Exception e)
            {
                Logging.Instance.WriteLine("[ Error in 'SetTargetFloor': " + e.Message + " ]");
            }
        }

        private void StartElevator()
        {
            try
            {
                if (StartingBlock != TargetBlock && StartingBlock.OpenRatio == 0 && StartingFloor <= FloorsList.Count)
                {
                    if (StartingFloor < TargetBlockCompare) Elevator_block.OpenDoor();
                    else Elevator_block.CloseDoor();
                    foreach (IMyAdvancedDoor Floor in FloorsList)
                    {
                        if (TargetBlockCompare > StartingFloor) Floor.SetEmissiveParts("Moving", ORANGE, 1f);
                        else Floor.SetEmissiveParts("MovingDown", ORANGE, 1f);
                    }
                    Elevator_block.Enabled = true;
                }
            }
            catch (Exception e)
            {
                Logging.Instance.WriteLine("[ Error in 'StartElevator': " + e.Message + " ]");
            }
        }

        private void MoveEntity()
        {
            try
            {
                for (int i = 0; i < ContainedEntitiesList.Count; i++) ContainedEntity[i].PositionComp.SetPosition(CabinPosition + offset[i]);
                if (PlayElevatorMusic && !MyAPIGateway.Utilities.IsDedicated)
                {
                    if (!Sound.IsPlaying && !PlayElevatorMusicNearCabin) Sound.PlaySound(ElevatorMusic, false, false, false, true);

                }
            }
            catch (Exception e)
            {
                Logging.Instance.WriteLine("[ Error in 'MoveEntity': " + e.Message + " ]");
            }
        }

        private void StopElevator(float precision = 0.1f)
        {
            try
            {
                if (Vector3D.Distance(TargetBlock.WorldMatrix.Translation, CabinPosition) < precision)
                {
                    TargetBlock.Enabled = true;
                    TargetBlock.OpenDoor();
                    TargetFloorChanged = true;
                    ShouldMoveEntity = false;
                    Elevator_block.Enabled = false;
                    if (!PlayElevatorMusicNearCabin && !MyAPIGateway.Utilities.IsDedicated) Sound.StopSound(true);
                }
            }
            catch (Exception e)
            {
                Logging.Instance.WriteLine("[ Error in 'StopElevator': " + e.Message + " ]");
            }
        }

        //private void UpdateChristmasLights()
        //{
        //    if (!MyAPIGateway.Utilities.IsDedicated)
        //    {
        //        if (ShowChristmasLights)
        //        {
        //            ChristmasLights.Render.Visible = true;
        //            if ((frame == 0 && second == 0) || (frame == 30 && second == 4) || (frame == 0 && second == 7))
        //                Elevator_block.SetEmissivePartsForSubparts("LightSequence1", Color.White, 1f);
        //            if ((frame == 30 && second == 0) || (frame == 0 && second == 4) || (frame == 30 && second == 7))
        //                Elevator_block.SetEmissivePartsForSubparts("LightSequence2", Color.Green, 1f);
        //            if ((frame == 0 && second == 1) || (frame == 30 && second == 3) || (frame == 0 && second == 8))
        //                Elevator_block.SetEmissivePartsForSubparts("LightSequence3", Color.Red, 1f);
        //            if ((frame == 30 && second == 1) || (frame == 0 && second == 3) || (frame == 0 && second == 5) || (frame == 30 && second == 6) || (frame == 30 && second == 8) || (frame == 30 && second == 9))
        //                for (int i = 1; i <= 3; i++) Elevator_block.SetEmissivePartsForSubparts("LightSequence" + i, Color.Transparent, 0f);
        //            if ((frame == 0 && second == 2) || (frame == 30 && second == 5) || (frame == 0 && second == 9))
        //                for (int i = 1; i <= 3; i++) Elevator_block.SetEmissivePartsForSubparts("LightSequence" + i, Color.Yellow, 1f);
        //        }
        //        else ChristmasLights.Render.Visible = false;
        //    }
        //}
    }
}