namespace ExamplePlugin.Utility
{
    using System;
    using System.IO;
    using System.Linq;
    using NLog;
    using Sandbox.Common;
    using Sandbox.Common.ObjectBuilders;
    using Sandbox.ModAPI;
    using System.Collections.Generic;
    using SEModAPIExtensions.API;
    using SEModAPIInternal.API.Common;
    using SEModAPIInternal.API.Entity;
    using SEModAPIInternal.API.Entity.Sector.SectorObject;
    using SEModAPIInternal.API.Server;
    using VRage;
    using VRageMath;
    using SEModAPI.API.Definitions;
    using System.Text;
    public class Communication
	{

        //This class is used for communication with a client mod
        //the Essentials client mod listens to this on 'port' 9000
        //change that if you want to implement your own comms
        //alternatively if you just want to send chat messages you can use
        /*
            ChatManager.Instance.SendPrivateChatMessage();
            ChatManager.Instance.SendPublicChatMessage();
        */
            


		private static readonly Logger Log = LogManager.GetLogger( "PluginLog" );
		private static Random m_random = new Random( );

        public static void SendPublicInformation( string infoText )
		{
			if ( infoText == "" )
				return;

            ServerMessageItem MessageItem = new ServerMessageItem( );
                MessageItem.From = PluginSettings.Instance.ServerChatName;
                MessageItem.Message = infoText;

            string messageString = MyAPIGateway.Utilities.SerializeToXML( MessageItem );
            byte[ ] data = Encoding.Unicode.GetBytes( messageString );

            if ( ChatManager.EnableData )
            {
                BroadcastDataMessage( DataMessageType.Message, data );
            }
            else
                ChatManager.Instance.SendPublicChatMessage( infoText );

            ChatManager.Instance.AddChatHistory( new ChatManager.ChatEvent( DateTime.Now, 0, infoText ) );
        }

		public static void SendPrivateInformation( ulong playerId, string infoText, string from = null )
		{
			if ( infoText == "" )
				return;

            ServerMessageItem MessageItem = new ServerMessageItem( );
            
            if(from == null )
                MessageItem.From = PluginSettings.Instance.ServerChatName;
            else
                MessageItem.From = from;

            MessageItem.Message = infoText;

            string messageString = MyAPIGateway.Utilities.SerializeToXML( MessageItem );
            byte[ ] data = new byte[messageString.Length];

            for ( int r = 0; r < messageString.Length; r++ )
            {
                data[r] = (byte)messageString[r];
            }
            
            SendDataMessage( playerId, DataMessageType.Message, data );
        }
              
		public static void Notification( ulong steamId, MyFontEnum color, int timeInSeconds, string message )
		{
            ServerNotificationItem MessageItem = new ServerNotificationItem( );
                MessageItem.color = color;
                MessageItem.time = timeInSeconds * 1000;
                MessageItem.message= message;

            string messageString = MyAPIGateway.Utilities.SerializeToXML( MessageItem );
            byte[ ] data = new byte[messageString.Length];

            for ( int r = 0; r < messageString.Length; r++ )
            {
                data[r] = (byte)messageString[r];
            }

            if ( steamId != 0 )
                SendDataMessage( steamId, DataMessageType.Notification, data );
            else
                BroadcastDataMessage( DataMessageType.Notification, data );
        }		

		public static void DisplayDialog( ulong steamId, string header, string subheader, string content, string buttonText = "OK" )
		{
            ServerDialogItem MessageItem = new ServerDialogItem( );
                MessageItem.title = header;
                MessageItem.header = subheader;
                MessageItem.content = content;
                MessageItem.buttonText = buttonText;

            string messageString = MyAPIGateway.Utilities.SerializeToXML( MessageItem );
            byte[ ] data = new byte[messageString.Length];

            for ( int r = 0; r < messageString.Length; r++ )
            {
                data[r] = (byte)messageString[r];
            }

            SendDataMessage( steamId, DataMessageType.Dialog, data );
        }

        public static void DisplayDialog( ulong steamId, ServerDialogItem MessageItem)
        {
            string messageString = MyAPIGateway.Utilities.SerializeToXML( MessageItem );
            byte[ ] data = new byte[messageString.Length];

            for ( int r = 0; r < messageString.Length; r++ )
            {
                data[r] = (byte)messageString[r];
            }

            SendDataMessage( steamId, DataMessageType.Dialog, data );
        }

        public static void SendDataMessage( ulong steamId, DataMessageType messageType, byte[ ] data )
		{
            //this may be unsafe, but whatever, my sanity requires the enum
            long msgId = (long)messageType;

            //this block adds message length and id so the mod knows what to do with the data
			string msgIdString = msgId.ToString( );
			byte[ ] newData = new byte[ data.Length + msgIdString.Length + 1 ];
			newData[ 0 ] = (byte)msgIdString.Length;
			for ( int r = 0; r < msgIdString.Length; r++ )
				newData[ r + 1 ] = (byte)msgIdString[ r ];

			Buffer.BlockCopy( data, 0, newData, msgIdString.Length + 1, data.Length );

            Wrapper.GameAction( ( ) =>
            {
                MyAPIGateway.Multiplayer.SendMessageTo(9000, newData, steamId);
            } );
        }

		public static void BroadcastDataMessage( DataMessageType messageType, byte[ ] data )
        {
            //this may be unsafe, but whatever, my sanity requires the enum
            long msgId = (long)messageType;

            //this block adds message length and id so the mod knows what to do with the data
            string msgIdString = msgId.ToString( );
			byte[ ] newData = new byte[ data.Length + msgIdString.Length + 1 ];
			newData[ 0 ] = (byte)msgIdString.Length;
			for ( int r = 0; r < msgIdString.Length; r++ )
				newData[ r + 1 ] = (byte)msgIdString[ r ];

			Buffer.BlockCopy( data, 0, newData, msgIdString.Length + 1, data.Length );

		    Wrapper.GameAction(() =>
		    {
		        MyAPIGateway.Multiplayer.SendMessageToOthers(9000, newData);
		    });
        }

        public class ServerMessageItem
		{
			public string From { get; set; }
			public string Message { get; set; }
		}

        public class ServerDialogItem
        {
            public string title { get; set; }
            public string header { get; set; }
            public string content { get; set; }
            public string buttonText { get; set; }
        }

        public class ServerNotificationItem
        {
            public MyFontEnum color { get; set; }
            public int time { get; set; }
            public string message { get; set; }
        }
        

        public enum DataMessageType
        {
            Test = 5000,
            VoxelHeader,
            VoxelPart,
            Message,
            RemoveStubs,
            ChangeServer,
            ServerSpeed,
            Credits,

            //skipped a few addresses to avoid conflict
            //just in case
            Dialog = 5020,
            Move,
            Notification,
            MaxSpeed,
            ServerInfo
        }        
	}
}
