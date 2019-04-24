using ExamplePlugin.Settings;
using ExamplePlugin.UtilityClasses;

namespace ExamplePlugin
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.IO;
    using System.Xml.Serialization;
    using ExamplePlugin.Settings;
    using ExamplePlugin.UtilityClasses;

    [Serializable]
    public class PluginSettings
    {
        #region Private Fields

        private int _example;
        private string _serverChatName;
        private MTObservableCollection<SettingsDialogItem> _exampleMTO;
        
        private static PluginSettings _instance;
        private static bool _loading = false;

        #endregion

        #region Static Properties

        public static PluginSettings Instance
        {
            get
            {
                return _instance ?? (_instance = new PluginSettings( ));
            }
        }
        #endregion

        #region Properties

        public string ServerChatName
        {
            get
            {
                return _serverChatName;
            }
            set
            {
                _serverChatName = value;
                Save( );
            }
        }

        public int Example
        {
            get
            {
                return _example;
            }
            set
            {
                _example = value;
                Save( );
            }
        }

        public MTObservableCollection<SettingsDialogItem> ExampleMTO
        {
            get
            {
                return _exampleMTO;
            }
            set
            {
                _exampleMTO = value;
                Save( );
            }
        }
        #endregion



        #region Constructor
        public PluginSettings( )
        {
            _exampleMTO = new MTObservableCollection<SettingsDialogItem>( );
            _exampleMTO.CollectionChanged += ItemsCollectionChanged;
            _example = 100;
            _serverChatName = "Example";
        }


        #endregion

        #region Loading and Saving

        /// <summary>
        /// Loads our settings
        /// </summary>
        public void Load( )
        {
            _loading = true;

            try
            {
                lock ( this )
                {
                    String fileName = PluginExample.PluginPath + "NoGrief-Settings.xml";
                    if ( File.Exists( fileName ) )
                    {
                        using ( StreamReader reader = new StreamReader( fileName ) )
                        {
                            XmlSerializer x = new XmlSerializer( typeof( PluginSettings ) );
                            PluginSettings settings = (PluginSettings)x.Deserialize( reader );
                            reader.Close( );

                            _instance = settings;
                        }
                    }
                }
            }
            catch ( Exception ex )
            {
                PluginExample.Log.Error( ex );
            }
            finally
            {
                _loading = false;
            }
        }

        /// <summary>
        /// Saves our settings
        /// </summary>
        public void Save( )
        {
            if ( _loading )
                return;

            try
            {
                lock ( this )
                {
                    String fileName = PluginExample.PluginPath + "NoGrief-Settings.xml";
                    using ( StreamWriter writer = new StreamWriter( fileName ) )
                    {
                        XmlSerializer x = new XmlSerializer( typeof( PluginSettings ) );
                        x.Serialize( writer, _instance );
                        writer.Close( );
                    }
                }
            }
            catch ( Exception ex )
            {
                PluginExample.Log.Error( ex );
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Triggered when items changes.  
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ItemsCollectionChanged( object sender, NotifyCollectionChangedEventArgs e )
        {
            Save( );
        }

        private void OnPropertyChanged( object sender, PropertyChangedEventArgs e )
        {
            Console.WriteLine( "PropertyChanged()" );
            Save( );
        }

        #endregion
    }
}
