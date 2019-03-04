using System; 
using System.Collections.Generic; 
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Common;
using VRage.Game.Components;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine;
using Sandbox.Game;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using VRage.Game;
using VRage.Game.Components;
using VRage.ObjectBuilders;
using VRage.ModAPI;
using VRage;

/*  
  Welcome to Modding API. This is second of two sample scripts that you can modify for your needs,
  in this case simple script is prepared that will alter behaviour of sensor block
  This type of scripts will be executed automatically  when sensor (or your defined) block is added to world
 */
namespace TestScript
{
   //here you can use any objectbuiler e.g. MyObjectBuilder_Door, MyObjectBuilder_Decoy
   [MyEntityComponentDescriptor(typeof(MyObjectBuilder_SensorBlock))]
   public class EvilSensor : MyGameLogicComponent
  {
       MyObjectBuilder_EntityBase m_objectBuilder = null;
       static String[] OreNames;
       //here you can use any inferface to your block type e.g. Sandbox.ModAPI.IMyDoort
       //if block is missing in Sandbox.ModAPI, you can use Sandbox.ModAPI.Ingame namespace to search for blockt
       Sandbox.ModAPI.IMySensorBlock Sensor;

       //if you suscribed to events, please always unsuscribe them in close method 
       public override void Close() 
       { 
           Sensor.StateChanged -= sensor_StateChanged; 
       } 

       public override void Init(MyObjectBuilder_EntityBase objectBuilder)
       {
           //here you can add new update interval, in this case we would like to update each 100TH frame
           //you can also update each frame, each 10Th frame 
           // you can combine update intervals, so you can update every frame , every 10TH frame and every 100TH frame
           Entity.NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;
           if (OreNames == null)
           {
               MyDefinitionManager.Static.GetOreTypeNames(out OreNames);
           }
           m_objectBuilder = objectBuilder;
           Sensor = Entity as Sandbox.ModAPI.IMySensorBlock;
           Sensor.StateChanged += sensor_StateChanged;
       }

       void sensor_StateChanged(bool obj)
       {
           if(!obj) return;
           string ore = null;
           foreach(var o in OreNames)
           {
               if (Sensor.CustomName.StartsWith(o, StringComparison.InvariantCultureIgnoreCase))
               {
                   ore = o;
                   break;
               }
           }
           if (ore == null)
               return;
           // We want to spawn ore and throw it at entity which entered sensor
           MyObjectBuilder_FloatingObject floatingBuilder = new MyObjectBuilder_FloatingObject();
           floatingBuilder.Item = new MyObjectBuilder_InventoryItem() { Amount = 100, Content = new MyObjectBuilder_Ore() { SubtypeName = ore } };
           floatingBuilder.PersistentFlags = MyPersistentEntityFlags2.InScene; // Very important
           floatingBuilder.PositionAndOrientation = new MyPositionAndOrientation()
           {
               Position = Sensor.WorldMatrix.Translation + Sensor.WorldMatrix.Forward * 1.5, // Spawn ore 1.5m in front of the sensor
               Forward = (VRageMath.Vector3)Sensor.WorldMatrix.Forward,
               Up = (VRageMath.Vector3)Sensor.WorldMatrix.Up,
           };
           var floatingObject = Sandbox.ModAPI.MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd(floatingBuilder);
           // Now it only creates ore, we will throw it later
       }

       public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
       {
           return m_objectBuilder;
       }

      //diferrence between UpdateAfter and UpdateBefore simulation is that UpdateAfter  is called after physics simulation and UpdateBefore is called
      //before physics simulation

      //this is called when  MyEntityUpdateEnum.EACH_FRAME is used as update interval
      public override void UpdateAfterSimulation()
      {
      }

      //this is called when  MyEntityUpdateEnum.EACH_10TH_FRAME is used as update interval
      public override void UpdateAfterSimulation10()
      {
      }

      //this is called when  MyEntityUpdateEnum.EACH_100TH_FRAME is used as update interval
      public override void UpdateAfterSimulation100()
      {
      }

      public override void UpdateBeforeSimulation()
      {
      }

      public override void UpdateBeforeSimulation10()
      {
      }

      public override void UpdateBeforeSimulation100()
      {
      }
	}
}
