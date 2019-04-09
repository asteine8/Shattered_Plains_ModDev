
IMyRemoteControl Remote;
IMyCameraBlock RaycastingCamera;
IMyTextPanel LCD;

bool blocksAvailible = true;

public Program() {
    try {
        Remote = GridTerminalSystem.GetBlockWithName("Remote Control") as IMyRemoteControl;
        RaycastingCamera = GridTerminalSystem.GetBlockWithName("Sensor Camera") as IMyCameraBlock;
        LCD = GridTerminalSystem.GetBlockWithName("Scanner LCD") as IMyTextPanel;

        RaycastingCamera.EnableRaycast = true;
    } catch (Exception e) {
        Echo("Required Blocks not named correctly or not present");
        blocksAvailible = false;
    }
    
}

void Main(string arg) {
    if (arg.Trim().ToLower() == "scan" && blocksAvailible) {
        string info = "Results:\n";
        string name = "undefined";
        string id = "";
        string type = "";

        Vector3D PlanetPosition = new Vector3D();
        Remote.TryGetPlanetPosition(out PlanetPosition);

        Vector3D NaturalGravity = Remote.GetNaturalGravity();

        MyDetectedEntityInfo raycastResult = RaycastingCamera.Raycast(50);

        if (raycastResult.IsEmpty() == false) {
            name = raycastResult.Name;
            id = raycastResult.EntityId.ToString();
            type = raycastResult.Type.ToString();


            info += "Entity Descriptor: " + name + "\n";
            info += "Registry ID: " + id + "\n";
            info += "Type of Object: " + type + "\n"; 
            info += "Position = " + PlanetPosition.ToString("0.00") + "\n";
            info += "Gravity = " + NaturalGravity.Length().ToString("0.00") + "\n\n";
        }
        else {
            info += "+++ Warning: Sensor Scan Failed +++\n";
        }

        Remote.CustomData = info;
        LCD.WriteText(info);
    }
}