﻿namespace ExamplePlugin
{

    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.IO;
    using SEModAPIExtensions.API.Plugin;
    using SEModAPIExtensions.API.Plugin.Events;
    using SEModAPIExtensions.API;

    using SEModAPIInternal.API.Common;
    using NLog;
    using ExamplePlugin.Settings;
    using ExamplePlugin.UtilityClasses;
    using ChatHandlers;
    using ProcessHandlers;
    using System.Runtime.InteropServices;
    using SEModAPI.API.Utility;
    using Utility;

    public class PluginExample : IPlugin, IChatEventHandler
    {

        public static Logger Log;
        private static string _pluginPath;

        #region Private Fields
        internal static PluginExample Instance;
        //private static ControlForm controlForm;
        private Thread _processThread;
        private List<Thread> _processThreads;
        private List<ProcessHandlerBase> _processHandlers;
        private List<ChatHandlerBase> _chatHandlers;
        private bool _running = true;

        #endregion

        #region "Properties"

        public static string PluginPath
        {
            get
            {
                return _pluginPath;
            }
            set
            {
                _pluginPath = value;
            }
        }

        [Category( "Chat Settings" )]
        [Description( "Chat messages sent from the server will show this name. \r\nNote: This is set separately from SESE and Essentials." )]
        [Browsable( true )]
        [ReadOnly( false )]
        public string ServerChatName
        {
            get
            {
                return PluginSettings.Instance.ServerChatName;
            }
            set
            {
                PluginSettings.Instance.ServerChatName = value;
            }
        }
        
        [Category( "Dialog Example" )]
        [Description( "Shows a dialog window" )]
        [Browsable( true )]
        [ReadOnly( false )]
        public MTObservableCollection<SettingsDialogItem> Dialog
        {
            get
            {
                return PluginSettings.Instance.ExampleMTO;
            }
            set
            {
                PluginSettings.Instance.ExampleMTO = value;
            }
        }

        [Category( "Example field" )]
        [Description( "this is an int" )]
        [Browsable( true )]
        [ReadOnly( false )]
        public int Example
        {
            get
            {
                return PluginSettings.Instance.Example;
            }
            set
            {
                PluginSettings.Instance.Example = value;
            }
        }
        #endregion

        #region "Methods"

        public void OnChatReceived( ChatManager.ChatEvent obj )
        {
            if ( obj.SourceUserId == 0 )
                return;


            if ( obj.Message[0] == '/' )
            {

            }
            return;
        }
        #endregion

        #region Constructors and Initializers

        private void DoInit( string path )
        {
            Instance = this;
            //controlForm.Text = "Testing";
            _pluginPath = path;

            // Load our settings
            PluginSettings.Instance.Load( );

            // Setup process handlers
            _processHandlers = new List<ProcessHandlerBase>
            {

            };

            // Setup chat handlers
            _chatHandlers = new List<ChatHandlerBase>
            {

            };

            _processThreads = new List<Thread>( );
            _processThread = new Thread( PluginProcessing );
            _processThread.Start( );
            Log.Info( "Plugin '{0}' initialized. (Version: {1}  ID: {2})", Name, Version, Id );
        }

        #endregion

        #region Processing Loop
        private void PluginProcessing( )
        {
            try
            {
                foreach ( ProcessHandlerBase handler in _processHandlers )
                {
                    ProcessHandlerBase currentHandler = handler;
                    Thread thread = new Thread( ( ) =>
                    {
                        while ( _running )
                        {
                            if ( currentHandler.CanProcess( ) )
                            {
                                try
                                {
                                    currentHandler.Handle( );
                                }
                                catch ( Exception ex )
                                {
                                    Log.Warn( "Handler Problems: {0} - {1}", currentHandler.GetUpdateResolution( ), ex );
                                }

                            // Let's make sure LastUpdate is set to now otherwise we may start processing too quickly
                            currentHandler.LastUpdate = DateTime.Now;
                            }

                            Thread.Sleep( 100 );
                        }
                    } );

                    _processThreads.Add( thread );
                    thread.Start( );
                }

                foreach ( Thread thread in _processThreads )
                    thread.Join( );
                        
            }
            catch ( ThreadAbortException ex )
            {
                Log.Trace( ex );
            }
            catch ( Exception ex )
            {
                Log.Error( ex );
            }
            finally
            {
                // MyAPIGateway.Entities.OnEntityAdd -= OnEntityAdd;
                // MyAPIGateway.Entities.OnEntityRemove -= OnEntityRemove;
            }
        }
        #endregion

        #region IPlugin Members
        public void Init( )
        {
            //register object builder assembly
            string path = System.IO.Path.Combine( AppDomain.CurrentDomain.BaseDirectory, "SpaceEngineers.ObjectBuilders.DLL" );
            VRage.Plugins.MyPlugins.RegisterGameObjectBuildersAssemblyFile( path );

            Log.Debug( "Initializing NoGrief plugin at path {0}\\", Path.GetDirectoryName( Assembly.GetExecutingAssembly( ).Location ) );
            DoInit( Path.GetDirectoryName( Assembly.GetExecutingAssembly( ).Location ) + "\\" );
        }

        public void InitWithPath( String modPath )
        {
            //register object builder assembly
            string path = System.IO.Path.Combine( AppDomain.CurrentDomain.BaseDirectory, "SpaceEngineers.ObjectBuilders.DLL" );
            VRage.Plugins.MyPlugins.RegisterGameObjectBuildersAssemblyFile( path );

            Log.Debug( "Initializing NoGrief plugin at path {0}\\", Path.GetDirectoryName( modPath ) );
            DoInit( Path.GetDirectoryName( modPath ) + "\\" );
        }

        public void Shutdown( )
        {
            Log.Info( "Shutting down plugin: {0} - {1}", Name, Version );

            foreach ( Thread thread in _processThreads )
                thread.Abort( );

            _processThread.Abort( );
        }

        public void Update( )
        {

        }

        #endregion

        #region IChatEventHandler Members

        public void OnMessageReceived( )
        {

        }


        public void HandleChatMessage( ulong steamId, string message )
        {
            // Parse chat message
            ulong remoteUserId = steamId;
            List<string> commandParts = CommandParser.GetCommandParts( message );
            
            // See if we have any valid handlers
            bool handled = false;
            foreach ( ChatHandlerBase chatHandler in _chatHandlers )
            {
                int commandCount = 0;
                if ( remoteUserId == 0 && !chatHandler.AllowedInConsole( ) )
                    continue;

                if ( chatHandler.CanHandle( remoteUserId, commandParts.ToArray( ), ref commandCount ) )
                {
                    try
                    {
                        chatHandler.HandleCommand( remoteUserId, commandParts.Skip( commandCount ).ToArray( ) );
                    }
                    catch ( Exception ex )
                    {
                        Log.Info( string.Format( "ChatHandler Error: {0}", ex ) );
                    }

                    handled = true;
                }
            }

            if ( !handled )
            {
                DisplayAvailableCommands( remoteUserId, message );
            }
        }

        /// <summary>
        /// This function displays available help for all the functionality of this plugin
        /// </summary>
        /// <param name="remoteUserId"></param>
        /// <param name="commandParts"></param>
        private void HandleHelpCommand( ulong remoteUserId, IReadOnlyCollection<string> commandParts )
        {
            if ( commandParts.Count == 1 )
            {
                List<string> commands = new List<string>( );
                foreach ( ChatHandlerBase handler in _chatHandlers )
                {
                    // We should replace this to just have the handler return a string[] of base commands
                    if ( handler.GetMultipleCommandText( ).Length < 1 )
                    {
                        string commandBase = handler.GetCommandText( ).Split( new[ ] { " " }, StringSplitOptions.RemoveEmptyEntries ).First( );
                        if ( !commands.Contains( commandBase ) && (!handler.IsClientOnly( )) && (!handler.IsAdminCommand( ) || (handler.IsAdminCommand( ) && (PlayerManager.Instance.IsUserAdmin( remoteUserId ) || remoteUserId == 0))) )
                        {
                            commands.Add( commandBase );
                        }
                    }
                    else
                    {
                        foreach ( string cmd in handler.GetMultipleCommandText( ) )
                        {
                            string commandBase = cmd.Split( new[ ] { " " }, StringSplitOptions.RemoveEmptyEntries ).First( );
                            if ( !commands.Contains( commandBase ) && (!handler.IsClientOnly( )) && (!handler.IsAdminCommand( ) || (handler.IsAdminCommand( ) && (PlayerManager.Instance.IsUserAdmin( remoteUserId ) || remoteUserId == 0))) )
                            {
                                commands.Add( commandBase );
                            }
                        }
                    }
                }

                string commandList = string.Join( ", ", commands );
                string info = string.Format( "NoGrief Plugin v{0}.  Available Commands: {1}", Version, commandList );
                Communication.SendPrivateInformation( remoteUserId, info );
            }
            else
            {
                string helpTarget = string.Join( " ", commandParts.Skip( 1 ).ToArray( ) );
                bool found = false;
                foreach ( ChatHandlerBase handler in _chatHandlers )
                {
                    // Again, we should get handler to just return string[] of command Text
                    if ( handler.GetMultipleCommandText( ).Length < 1 )
                    {
                        if ( String.Equals( handler.GetCommandText( ), helpTarget, StringComparison.CurrentCultureIgnoreCase ) )
                        {
                            Communication.SendPrivateInformation( remoteUserId, handler.GetHelp( ) );
                            found = true;
                        }
                    }
                    else
                    {
                        foreach ( string cmd in handler.GetMultipleCommandText( ) )
                        {
                            if ( String.Equals( cmd, helpTarget, StringComparison.CurrentCultureIgnoreCase ) )
                            {
                                Communication.SendPrivateInformation( remoteUserId, handler.GetHelp( ) );
                                found = true;
                            }
                        }
                    }
                }

                if ( !found )
                {
                    List<string> helpTopics = new List<string>( );

                    foreach ( ChatHandlerBase handler in _chatHandlers )
                    {
                        // Again, cleanup to one function
                        string[ ] multipleCommandText = handler.GetMultipleCommandText( );
                        if ( multipleCommandText.Length == 0 )
                        {
                            if ( handler.GetCommandText( ).ToLower( ).StartsWith( helpTarget.ToLower( ) ) && ((!handler.IsAdminCommand( )) || (handler.IsAdminCommand( ) && (PlayerManager.Instance.IsUserAdmin( remoteUserId ) || remoteUserId == 0))) )
                            {
                                helpTopics.Add( handler.GetCommandText( ).ToLower( ).Replace( helpTarget.ToLower( ), string.Empty ) );
                            }
                        }
                        else
                        {
                            foreach ( string cmd in multipleCommandText )
                            {
                                if ( cmd.ToLower( ).StartsWith( helpTarget.ToLower( ) ) && ((!handler.IsAdminCommand( )) || (handler.IsAdminCommand( ) && (PlayerManager.Instance.IsUserAdmin( remoteUserId ) || remoteUserId == 0))) )
                                {
                                    helpTopics.Add( cmd.ToLower( ).Replace( helpTarget.ToLower( ), string.Empty ) );
                                }
                            }
                        }
                    }

                    if ( helpTopics.Any( ) )
                    {
                        Communication.SendPrivateInformation( remoteUserId, string.Format( "Help topics for command '{0}': {1}", helpTarget.ToLower( ), string.Join( ",", helpTopics.ToArray( ) ) ) );
                        found = true;
                    }
                }

                if ( !found )
                    Communication.SendPrivateInformation( remoteUserId, "Unknown command" );
            }
        }

        /// <summary>
        /// This function displays available help for all the functionality of this plugin in a dialog window
        /// </summary>
        /// <param name="remoteUserId"></param>
        /// <param name="commandParts"></param>
        private void HandleHelpDialog( ulong remoteUserId, IReadOnlyCollection<string> commandParts )
        {
            if ( commandParts.Count == 2 )
            {
                List<string> commands = new List<string>( );
                foreach ( ChatHandlerBase handler in _chatHandlers )
                {
                    // We should replace this to just have the handler return a string[] of base commands
                    if ( handler.GetMultipleCommandText( ).Length < 1 )
                    {
                        string commandBase = handler.GetCommandText( ).Split( new[ ] { " " }, StringSplitOptions.RemoveEmptyEntries ).First( );
                        if ( !commands.Contains( commandBase ) && (!handler.IsClientOnly( )) && (!handler.IsAdminCommand( ) || (handler.IsAdminCommand( ) && (PlayerManager.Instance.IsUserAdmin( remoteUserId ) || remoteUserId == 0))) )
                        {
                            commands.Add( commandBase );
                        }
                    }
                    else
                    {
                        foreach ( string cmd in handler.GetMultipleCommandText( ) )
                        {
                            string commandBase = cmd.Split( new[ ] { " " }, StringSplitOptions.RemoveEmptyEntries ).First( );
                            if ( !commands.Contains( commandBase ) && (!handler.IsClientOnly( )) && (!handler.IsAdminCommand( ) || (handler.IsAdminCommand( ) && (PlayerManager.Instance.IsUserAdmin( remoteUserId ) || remoteUserId == 0))) )
                            {
                                commands.Add( commandBase );
                            }
                        }
                    }
                }

                string commandList = string.Join( ", ", commands );
                commandList = commandList.Replace( ", ", "|" );
                //take our list of commands, put line breaks between all the entries and stuff it into a dialog winow

                Communication.DisplayDialog( remoteUserId, "Help", "Available commands", commandList + "||Type '/help dialog <command>' for more info.", "close" );
            }
            else
            {
                string helpTarget = string.Join( " ", commandParts.Skip( 2 ).ToArray( ) );
                bool found = false;
                foreach ( ChatHandlerBase handler in _chatHandlers )
                {
                    // Again, we should get handler to just return string[] of command Text
                    if ( handler.GetMultipleCommandText( ).Length < 1 )
                    {
                        if ( String.Equals( handler.GetCommandText( ), helpTarget, StringComparison.CurrentCultureIgnoreCase ) )
                        {
                            Communication.DisplayDialog( remoteUserId, handler.GetHelpDialog( ) );
                            found = true;
                        }
                    }
                    else
                    {
                        foreach ( string cmd in handler.GetMultipleCommandText( ) )
                        {
                            if ( String.Equals( cmd, helpTarget, StringComparison.CurrentCultureIgnoreCase ) )
                            {
                                Communication.DisplayDialog( remoteUserId, handler.GetHelpDialog( ) );
                                found = true;
                            }
                        }
                    }
                }

                if ( !found )
                {
                    List<string> helpTopics = new List<string>( );

                    foreach ( ChatHandlerBase handler in _chatHandlers )
                    {
                        // Again, cleanup to one function
                        string[ ] multipleCommandText = handler.GetMultipleCommandText( );
                        if ( multipleCommandText.Length == 0 )
                        {
                            if ( handler.GetCommandText( ).ToLower( ).StartsWith( helpTarget.ToLower( ) ) && ((!handler.IsAdminCommand( )) || (handler.IsAdminCommand( ) && (PlayerManager.Instance.IsUserAdmin( remoteUserId ) || remoteUserId == 0))) )
                            {
                                helpTopics.Add( handler.GetCommandText( ).ToLower( ).Replace( helpTarget.ToLower( ), string.Empty ) );
                            }
                        }
                        else
                        {
                            foreach ( string cmd in multipleCommandText )
                            {
                                if ( cmd.ToLower( ).StartsWith( helpTarget.ToLower( ) ) && ((!handler.IsAdminCommand( )) || (handler.IsAdminCommand( ) && (PlayerManager.Instance.IsUserAdmin( remoteUserId ) || remoteUserId == 0))) )
                                {
                                    helpTopics.Add( cmd.ToLower( ).Replace( helpTarget.ToLower( ), string.Empty ) );
                                }
                            }
                        }
                    }

                    if ( helpTopics.Any( ) )
                    {
                        Communication.SendPrivateInformation( remoteUserId, string.Format( "Help topics for command '{0}': {1}", helpTarget.ToLower( ), string.Join( ",", helpTopics.ToArray( ) ) ) );
                        found = true;
                    }
                }

                if ( !found )
                    Communication.SendPrivateInformation( remoteUserId, "Unknown command" );
            }
        }

        /// <summary>
        /// Displays the available commands for the command entered
        /// </summary>
        /// <param name="remoteUserId"></param>
        /// <param name="recvMessage"></param>
        private void DisplayAvailableCommands( ulong remoteUserId, string recvMessage )
        {
            string message = recvMessage.ToLower( ).Trim( );
            List<string> availableCommands = new List<string>( );
            foreach ( ChatHandlerBase chatHandler in _chatHandlers )
            {
                // Cleanup to one function
                if ( chatHandler.GetMultipleCommandText( ).Length < 1 )
                {
                    string command = chatHandler.GetCommandText( );
                    if ( command.StartsWith( message ) )
                    {
                        string[ ] cmdPart = command.Replace( message, string.Empty ).Trim( ).Split( new[ ] { ' ' } );

                        if ( !availableCommands.Contains( cmdPart[0] ) )
                            availableCommands.Add( cmdPart[0] );
                    }
                }
                else
                {
                    foreach ( string command in chatHandler.GetMultipleCommandText( ) )
                    {
                        if ( command.StartsWith( message ) )
                        {
                            string[ ] cmdPart = command.Replace( message, string.Empty ).Trim( ).Split( new[ ] { ' ' } );

                            if ( !availableCommands.Contains( cmdPart[0] ) )
                                availableCommands.Add( cmdPart[0] );
                        }
                    }
                }
            }

            if ( availableCommands.Any( ) )
            {
                Communication.SendPrivateInformation( remoteUserId, string.Format( "Available subcommands for '{0}' command: {1}", message, string.Join( ", ", availableCommands.ToArray( ) ) ) );
            }
        }

        public void OnChatSent( ChatManager.ChatEvent obj )
        {

        }

        #endregion
        
        #region IPlayerEventHandler Members

        public void OnPlayerJoined( ulong remoteUserId )
        {
            foreach ( ProcessHandlerBase handler in _processHandlers )
                handler.OnPlayerJoined( remoteUserId );
        }

        public void OnPlayerLeft( ulong remoteUserId )
        {
            foreach ( ProcessHandlerBase handler in _processHandlers )
                handler.OnPlayerLeft( remoteUserId );
        }

        #endregion

        public void OnSectorSaved( object state )
        {
            foreach ( ProcessHandlerBase handler in _processHandlers )
                handler.OnSectorSaved( );
        }

        #region IPlugin Members

        public Guid Id
        {
            get
            {
                GuidAttribute guidAttr = (GuidAttribute)typeof( PluginExample ).Assembly.GetCustomAttributes( typeof( GuidAttribute ), true )[0];
                return new Guid( guidAttr.Value );
            }
        }

        public string Name
        {
            get
            {
                return "Plugin";
            }
        }

        public Version Version
        {
            get
            {
                return typeof( PluginExample ).Assembly.GetName( ).Version;
            }
        }

        #endregion
    }
}

