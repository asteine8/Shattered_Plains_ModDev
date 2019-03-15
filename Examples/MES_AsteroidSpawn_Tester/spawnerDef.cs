// VRage.Game.MyObjectBuilder_SpawnGroupDefinition
using ProtoBuf;
using System.ComponentModel;
using System.Xml.Serialization;
using VRage.Game;
using VRage.ObjectBuilders;
using VRageMath;

[XmlSerializerAssembly("VRage.Game.XmlSerializers")]
[ProtoContract]
[MyObjectBuilderDefinition(null, null)]
public class MyObjectBuilder_SpawnGroupDefinition : MyObjectBuilder_DefinitionBase
{
	[ProtoContract]
	public class SpawnGroupPrefab
	{
		[ProtoMember(18)]
		[XmlAttribute]
		public string SubtypeId;

		[ProtoMember(21)]
		public Vector3 Position;

		[ProtoMember(24)]
		[DefaultValue("")]
		public string BeaconText = "";

		[ProtoMember(27)]
		[DefaultValue(10f)]
		public float Speed = 10f;

		[ProtoMember(30)]
		[DefaultValue(false)]
		public bool PlaceToGridOrigin;

		[ProtoMember(33)]
		public bool ResetOwnership = true;

		[ProtoMember(36)]
		public string Behaviour;

		[ProtoMember(39)]
		public float BehaviourActivationDistance = 1000f;
	}

	[ProtoContract]
	public class SpawnGroupVoxel
	{
		[XmlAttribute]
		[ProtoMember(47)]
		public string StorageName;

		[ProtoMember(50)]
		public Vector3 Offset;

		[ProtoMember(53, IsRequired = false)]
		public bool CenterOffset;
	}

	[ProtoMember(57)]
	[DefaultValue(1f)]
	public float Frequency = 1f;

	[XmlArrayItem("Prefab")]
	[ProtoMember(60)]
	public SpawnGroupPrefab[] Prefabs;

	[ProtoMember(64)]
	[XmlArrayItem("Voxel")]
	public SpawnGroupVoxel[] Voxels;

	[DefaultValue(false)]
	[ProtoMember(68)]
	public bool IsEncounter;

	[ProtoMember(71)]
	[DefaultValue(false)]
	public bool IsPirate;

	[DefaultValue(false)]
	[ProtoMember(74)]
	public bool IsCargoShip;

	[ProtoMember(77)]
	[DefaultValue(false)]
	public bool ReactorsOn;
}
