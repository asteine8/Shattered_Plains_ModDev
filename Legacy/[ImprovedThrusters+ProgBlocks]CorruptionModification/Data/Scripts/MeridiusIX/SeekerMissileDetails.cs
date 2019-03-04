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

namespace MeridiusIX{
	
	public class SeekerMissileDetails{
		
		public bool IsServer {get; set;}
		public IMyTerminalBlock TargetGrid {get; set;}
		public IMyWarhead Warhead {get; set;}
		public IMyCubeGrid MissileGrid {get; set;}
		public int VelocityTimer {get; set;}
		public int DistanceTimer {get; set;}
		public Vector3D LastValidPosition {get; set;}
		public Vector3D DistanceCheckPosition {get; set;}
		
		public SeekerMissileDetails(){
			
			IsServer = false;
			TargetGrid = null;
			Warhead = null;
			MissileGrid = null;
			VelocityTimer = 0;
			DistanceTimer = 0;
			LastValidPosition = new Vector3D(0,0,0);
			DistanceCheckPosition = new Vector3D(0,0,0);
			
		}
		
		public bool MissileRun(){
			
			if(this.TargetAndMissileValid() == false){
				
				return false;
				
			}
			
			if(this.IsServer == true){
				
				this.ProcessVelocity();
				
			}
			
			//this.ProcessEffects(); //Using Block Effect Instead
			
			return true;
			
		}
		
		public void ProcessEffects(){
			
			var player = MyAPIGateway.Session.LocalHumanPlayer;
			
			if(player == null){
				
				return;
				
			}
			
			if(player.Character == null){
				
				return;
				
			}
						
			if(this.MissileGrid == null){
				
				return;
				
			}
			
			if(Vector3D.Distance(this.MissileGrid.GetPosition(), player.GetPosition()) > 3000){
				
				return;
				
			}
			
			MyParticleEffect effect;
			if(MyParticlesManager.TryCreateParticleEffect("PlanetCrashDust", out effect) == true){
				
				effect.Loop = false;
				effect.WorldMatrix = this.MissileGrid.WorldMatrix;
				
			}
			
		}
		
		public void ProcessVelocity(){
			
			this.LastValidPosition = this.Warhead.GetPosition();
			
			if(this.VelocityTimer < 5){
				
				this.VelocityTimer++;
				return;
				
			}
						
			this.VelocityTimer = 0;
			
			/*try{*/
				
				//Do Initial Check
				double distanceToTarget = Vector3D.Distance(this.TargetGrid.GetPosition(), this.MissileGrid.GetPosition());
				var impactPosition = distanceToTarget * this.MissileGrid.WorldMatrix.Forward + this.MissileGrid.GetPosition();
				
				if(Vector3D.Distance(impactPosition, this.TargetGrid.GetPosition()) < 15){
					
					var newForward = Vector3D.Normalize(this.TargetGrid.GetPosition() - this.MissileGrid.GetPosition());
					var newUp = MyUtils.GetRandomPerpendicularVector(ref newForward);
					var newSpeed = newForward * 250;
					var newMatrix = MatrixD.CreateWorld(this.MissileGrid.GetPosition(), newForward, newUp);
					this.MissileGrid.WorldMatrix = newMatrix;
					this.MissileGrid.Physics.LinearVelocity = (Vector3)newSpeed;
					
				}else{
					
					var potentialDirectionList = new List<Vector3D>();
					potentialDirectionList.Add(Vector3D.Transform(new Vector3D(5,0,-25), this.MissileGrid.WorldMatrix));
					potentialDirectionList.Add(Vector3D.Transform(new Vector3D(0,5,-25), this.MissileGrid.WorldMatrix));
					potentialDirectionList.Add(Vector3D.Transform(new Vector3D(-5,0,-25), this.MissileGrid.WorldMatrix));
					potentialDirectionList.Add(Vector3D.Transform(new Vector3D(0,-5,-25), this.MissileGrid.WorldMatrix));
					
					var closestPosition = new Vector3D(0,0,0);
					double closestDistance = 0;
					
					foreach(var position in potentialDirectionList){
						
						if(closestDistance == 0){
							
							closestDistance = Vector3D.Distance(this.TargetGrid.GetPosition(), position);
							closestPosition = position;
							
						}
						
						if(Vector3D.Distance(this.TargetGrid.GetPosition(), position) < closestDistance){
							
							closestDistance = Vector3D.Distance(this.TargetGrid.GetPosition(), position);
							closestPosition = position;
							
						}
						
					}
					
					var newForward = Vector3D.Normalize(closestPosition - this.MissileGrid.GetPosition());
					var newUp = MyUtils.GetRandomPerpendicularVector(ref newForward);
					var newSpeed = newForward * 250;
					var newMatrix = MatrixD.CreateWorld(this.MissileGrid.GetPosition(), newForward, newUp);
					this.MissileGrid.WorldMatrix = newMatrix;
					this.MissileGrid.Physics.LinearVelocity = (Vector3)newSpeed;
					
				}

				
			/*}catch(Exception exc){
				
				
				
			}*/
			
			
		}
		
		public bool TargetAndMissileValid(){
			
			
			var targetEntity = TargetGrid as IMyEntity;
			if(this.TargetGrid == null){
				
				return false;
				
			}
			
			if(this.MissileGrid == null || MyAPIGateway.Entities.Exist(MissileGrid) == false){
				
				return false;
				
			}
			
			if(this.Warhead != null){
				
				if(this.Warhead.IsFunctional == false){
					
					this.MissileGrid.Delete();
					return false;
					
				}
				
			}
			
			this.DistanceTimer++;
			
			if(this.DistanceTimer >= 60){
				
				this.DistanceTimer = 0;
				
				if(this.DistanceCheckPosition == new Vector3D(0,0,0)){
					
					this.DistanceCheckPosition = this.LastValidPosition;
					
				}else{
					
					if(Vector3D.Distance(this.DistanceCheckPosition, this.LastValidPosition) > 80){
						
						this.DistanceCheckPosition = this.LastValidPosition;
						
					}else{
						
						this.MissileGrid.Delete();
						return false;
						
					}
					
				}
				
			}
			
			
			return true;
			
		}
		
	}
	
}