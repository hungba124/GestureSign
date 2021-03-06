using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using GestureSign.Common.Applications;
using GestureSign.Common.Localization;
using GestureSign.Common.Plugins;

namespace GestureSign.ControlPanel.ViewModel
{
    public class CommandInfo : INotifyPropertyChanged, IEquatable<IAction>
    {

        public CommandInfo(IAction action, ICommand command, string commandName, string description, bool isEnabled)
        {
            Action = action;
            Command = command;
            IsEnabled = isEnabled;
            CommandName = commandName;
            Description = description;
        }
        private bool _isEnabled;
        private IAction _action;

        public bool IsEnabled
        {
            get
            {
                return _isEnabled;
            }

            set { SetProperty(ref _isEnabled, value); }
        }

        public string CommandName { get; set; }

        public string Description { get; set; }

        public IAction Action
        {
            get { return _action; }
            set { SetProperty(ref _action, value); }
        }

        public ICommand Command { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        protected void SetProperty<T>(ref T storage, T value, [CallerMemberName] String propertyName = null)
        {
            if (Equals(storage, value)) return;

            storage = value;
            this.OnPropertyChanged(propertyName);
        }

        public bool Equals(IAction action)
        {
            return action != null && CommandName == action.Name;
        }

        public static CommandInfo FromCommand(ICommand command, IAction action)
        {
            string description;
            string pluginName = string.Empty;
            // Ensure this action has a plugin
            if (string.IsNullOrEmpty(command.PluginClass) || string.IsNullOrEmpty(command.PluginFilename))
            {
                description = LocalizationProvider.Instance.GetTextValue("Action.Messages.DoubleClickToEditCommand");
            }
            else if (PluginManager.Instance.PluginExists(command.PluginClass, command.PluginFilename))
            {
                try
                {
                    // Get plugin for this action
                    IPluginInfo pluginInfo =
                        PluginManager.Instance.FindPluginByClassAndFilename(command.PluginClass,
                            command.PluginFilename);

                    // Feed settings to plugin
                    if (!pluginInfo.Plugin.Deserialize(command.CommandSettings))
                        command.CommandSettings = pluginInfo.Plugin.Serialize();

                    pluginName = pluginInfo.Plugin.Name;
                    description = pluginInfo.Plugin.Description;
                }
                catch
                {
                    pluginName = string.Empty;
                    description = LocalizationProvider.Instance.GetTextValue("Action.Messages.DoubleClickToEditCommand");
                }
            }
            else
            {
                description = string.Format(LocalizationProvider.Instance.GetTextValue("Action.Messages.NoAssociationAction"), command.PluginClass, command.PluginFilename);
            }

            return new CommandInfo(action, command, !string.IsNullOrEmpty(command.Name) ? command.Name : pluginName, description, command.IsEnabled);
        }
    }
}