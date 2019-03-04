using System; 
using System.Collections.Generic;
using System.Linq; 
using System.Text;  
using System.Threading.Tasks;
using Sandbox.ModAPI; 

/*   
  Welcome to Modding API. This is one of two sample scripts that you can modify for your needs, 
  in this case simple script is prepared that will show Hello world message in chat. 
  You need to run this script manually from chat to see it. To run it you first need to enable this in game 
  (press new World, than Custom World and Mods , you should see Script1 at the top), when world with mod loads, 
  please press F11 to see if there was any loading error during loading of the mod. When there is no mod loading errors  
  you can activate mod by opening chat window (by pressing Enter key). Than you need to call Main method of script class. 
   
  To do that you need to write this command : //call Script1_TestScript TestScript.Script ShowHelloWorld
  //call means that you want to call script
  Script1_TestScript is name of directory (if you have more script directories e.g. Script1, Script2 ... you need to change Script1 to your actual directory)
  TestScript.Script is name of tthe class with namespace , if you define new class you need to use new name e.g. when you create class Test in TestScript namespace
  you need to write : TestScript.Test 
  ShowHelloWorld is name of method, you can call only public static methods from chat window. 
   
   You can define your own namespaces / classes / methods to call 
 */ 
 
namespace TestScript 
{
    class Script
    {
      // ShowHelloWorld must be public static, you can define your own methods,
      // but to be able to call them from chat they must be public static 
       static public void ShowHelloWorld()
       {
            MyAPIGateway.Utilities.ShowMessage("Hello", "World !");
       }
       //by calling this method, you will see mission Screen
      static public void ShowMissionScreen()
      {
          MyAPIGateway.Utilities.ShowMissionScreen();
      }
   }
}
