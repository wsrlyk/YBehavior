using System;
using System.Collections.Generic;
using System.Windows.Input;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace YBehavior.Editor.Core.New
{
    /// <summary>
    /// Commands for key bindings
    /// </summary>
    public enum Command
    {
        /// <summary>
        /// Invalid
        /// </summary>
        None,
        /// <summary>
        /// Delete a node/line
        /// </summary>
        Delete,
        /// <summary>
        /// Duplicate a node/nodes
        /// </summary>
        Duplicate,
        /// <summary>
        /// Copy a node/nodes
        /// </summary>
        Copy,
        /// <summary>
        /// Paster a node/nodes
        /// </summary>
        Paste,
        /// <summary>
        /// Undo an operation
        /// </summary>
        Undo,
        /// <summary>
        /// Redo an operation
        /// </summary>
        Redo,
        /// <summary>
        /// Open a tree/fsm
        /// </summary>
        Open,
        /// <summary>
        /// Save a tree/fsm
        /// </summary>
        Save,
        /// <summary>
        /// Save as a tree/fsm
        /// </summary>
        SaveAs,
        /// <summary>
        /// Open/close search window
        /// </summary>
        Search,
        /// <summary>
        /// Toggle the break point of a node
        /// </summary>
        BreakPoint,
        /// <summary>
        /// Toggle the log point of a node
        /// </summary>
        LogPoint,
        /// <summary>
        /// Disable/enable a node
        /// </summary>
        Disable,
        /// <summary>
        /// Open/close the condition pin of a node
        /// </summary>
        Condition,
        /// <summary>
        /// Fold/unfold a node
        /// </summary>
        Fold,
        /// <summary>
        /// Make a fsm node the default one
        /// </summary>
        Default,
        /// <summary>
        /// Make the nodes at the center of the canvas
        /// </summary>
        Center,
        /// <summary>
        /// Clear the command lines
        /// </summary>
        Clear,
        /// <summary>
        /// Continue when debugging
        /// </summary>
        DebugContinue,
        /// <summary>
        /// Step over the children when debugging
        /// </summary>
        DebugStepOver,
        /// <summary>
        /// Step into the children when debugging
        /// </summary>
        DebugStepIn,
    }
    /// <summary>
    /// Key bindings management
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class KeyBindings
    {
        [JsonProperty(PropertyName = "UserConfig")]
        UserConfig m_UserConfig = new UserConfig();

        ModifierKeys m_MultiKey = ModifierKeys.Shift;
        /// <summary>
        /// Multiple operation key
        /// </summary>
        public ModifierKeys MultiKey { get { return m_MultiKey; } }

        /// <summary>
        /// A key structure
        /// </summary>
        public struct KeyBinding
        {
            /// <summary>
            /// The key
            /// </summary>
            public Key key;
            /// <summary>
            /// The modifier
            /// </summary>
            public ModifierKeys modifier;

            public KeyBinding(Key k, ModifierKeys m)
            {
                key = k;
                modifier = m;
            }
        }
        /// <summary>
        /// A default key binding configuration.
        /// Can be overwrite by user configuration.
        /// </summary>
        Dictionary<Command, KeyBinding> m_KeyBindings = new Dictionary<Command, KeyBinding>()
        {
            {Command.Delete, new KeyBinding(Key.Delete, ModifierKeys.None)},
            {Command.Duplicate, new KeyBinding(Key.D, ModifierKeys.Control)},
            {Command.Copy, new KeyBinding(Key.C, ModifierKeys.Control)},
            {Command.Paste, new KeyBinding(Key.V, ModifierKeys.Control)},
            {Command.Undo, new KeyBinding(Key.Z, ModifierKeys.Control)},
            {Command.Redo, new KeyBinding(Key.Y, ModifierKeys.Control)},
            {Command.Open, new KeyBinding(Key.O, ModifierKeys.Control)},
            {Command.Save, new KeyBinding(Key.S, ModifierKeys.Control)},
            {Command.Search, new KeyBinding(Key.F, ModifierKeys.Control)},
            {Command.BreakPoint, new KeyBinding(Key.F9, ModifierKeys.None)},
            {Command.LogPoint, new KeyBinding(Key.F8, ModifierKeys.None)},
            {Command.Disable, new KeyBinding(Key.F12, ModifierKeys.None)},
            {Command.Condition, new KeyBinding(Key.F6, ModifierKeys.None)},
            {Command.Fold, new KeyBinding(Key.F7, ModifierKeys.None)},
            {Command.Default, new KeyBinding(Key.F3, ModifierKeys.None)},
            {Command.Center, new KeyBinding(Key.F1, ModifierKeys.None)},
            {Command.Clear, new KeyBinding(Key.F2, ModifierKeys.None)},
            {Command.DebugContinue, new KeyBinding(Key.F5, ModifierKeys.None)},
            {Command.DebugStepOver, new KeyBinding(Key.F10, ModifierKeys.None)},
            {Command.DebugStepIn, new KeyBinding(Key.F11, ModifierKeys.None)},
        };
        /// <summary>
        /// A key->command map
        /// </summary>
        Dictionary<Key, Dictionary<ModifierKeys, Command>> m_Maps = new Dictionary<Key, Dictionary<ModifierKeys, Command>>();

        [JsonObject]
        public class UserConfig
        {
            [JsonProperty(PropertyName = "KeyBindings")]
            public Dictionary<string, string> Bindings = new Dictionary<string, string>();
            [JsonConverter(typeof(StringEnumConverter))]
            public ModifierKeys MultiKey = ModifierKeys.Shift;
        }

        bool m_bDirty;
        /// <summary>
        /// The setting is changed
        /// </summary>
        public bool IsDirty { get { return m_bDirty; } }
        public void Init()
        {
            var hash = _ReadUserConfig();
            _CreateMap();
            ///> write back the user config. 
            ///  if user delete some default bindings in the config file,
            ///  this will recover them
            var newhash = _WriteUserConfig();
            m_bDirty = hash != newhash;
        }

        uint _ReadUserConfig()
        {
            StringBuilder hashsb = new StringBuilder();
            if (m_UserConfig.MultiKey != ModifierKeys.None)
            {
                m_MultiKey = m_UserConfig.MultiKey;
                hashsb.Append(m_MultiKey);
            }


            foreach (var p in m_UserConfig.Bindings)
            {
                if (!Enum.TryParse(p.Key, true, out Command c))
                {
                    LogMgr.Instance.Error("Invalid Command: " + p.Key);
                    continue;
                }
                hashsb.Append(p.Key).Append(p.Value);

                var keys = p.Value.Split('+');
                if (keys == null || keys.Length == 0)
                {
                    LogMgr.Instance.Error("Invalid Keys: " + p.Value);
                    continue;
                }

                Key key = Key.None;
                ModifierKeys modifier = ModifierKeys.None;
                foreach (var s in keys)
                {
                    if (Enum.TryParse(s, true, out ModifierKeys mod))
                    {
                        modifier |= mod;
                    }
                    else if (key == Key.None)
                    {
                        if (Enum.TryParse(s, true, out Key k))
                        {
                            key = k;
                        }
                        else
                        {
                            LogMgr.Instance.Error("Invalid Key " + s + " for Command " + p.Key);
                        }
                    }
                    else
                    {
                        LogMgr.Instance.Error("Invalid/Duplicated Keys for Command: " + p.Key);
                    }

                }

                if (key == Key.None)
                {
                    LogMgr.Instance.Error(string.Format("Command %s is not assigned by a key", p.Key));
                }
                else
                {
                    m_KeyBindings[c] = new KeyBinding(key, modifier);
                }
            }

            return Utility.Hash(hashsb.ToString());
        }
        uint _WriteUserConfig()
        {
            m_UserConfig.MultiKey = m_MultiKey;
            m_UserConfig.Bindings.Clear();

            StringBuilder sb = new StringBuilder();
            StringBuilder hashsb = new StringBuilder();
            hashsb.Append(m_MultiKey);
            foreach (var p in m_KeyBindings)
            {
                string k = p.Key.ToString();
                string v = null;
                if (p.Value.modifier == ModifierKeys.None)
                    v = p.Value.key.ToString();
                else
                {
                    sb.Length = 0;
                    ModifierKeys mod = p.Value.modifier;
                    if ((mod & ModifierKeys.Control) != 0)
                    {
                        sb.Append(ModifierKeys.Control).Append('+');
                    }
                    if ((mod & ModifierKeys.Alt) != 0)
                    {
                        sb.Append(ModifierKeys.Alt).Append('+');
                    }
                    if ((mod & ModifierKeys.Shift) != 0)
                    {
                        sb.Append(ModifierKeys.Shift).Append('+');
                    }
                    if ((mod & ModifierKeys.Windows) != 0)
                    {
                        sb.Append(ModifierKeys.Windows).Append('+');
                    }
                    sb.Append(p.Value.key);
                    v = sb.ToString();
                }

                m_UserConfig.Bindings[k] = v;
                hashsb.Append(k).Append(v);
            }

            return Utility.Hash(hashsb.ToString());
        }
        void _CreateMap()
        {
            foreach (var p in m_KeyBindings)
            {
                if (!m_Maps.TryGetValue(p.Value.key, out var dic))
                {
                    dic = new Dictionary<ModifierKeys, Command>();
                    m_Maps[p.Value.key] = dic;
                }

                if (dic.TryGetValue(p.Value.modifier, out var c))
                {
                    LogMgr.Instance.Error(string.Format("key bindings conflict for %s and %s", c.ToString(), p.Key.ToString()));
                }
                else
                {
                    dic[p.Value.modifier] = p.Key;
                }
            }
        }

        /// <summary>
        /// Find a command with the key and modifier
        /// </summary>
        /// <param name="key"></param>
        /// <param name="modifier"></param>
        /// <returns></returns>
        public Command GetCommand(Key key, ModifierKeys modifier)
        {
            if (!m_Maps.TryGetValue(key, out var dic))
            {
                return Command.None;
            }

            foreach (var p in dic)
            {
                if (p.Key == ModifierKeys.None || (p.Key & modifier) != 0)
                {
                    return p.Value;
                }
            }

            return Command.None;
        }

        /// <summary>
        /// Is multiple operation key
        /// </summary>
        /// <param name="modifier"></param>
        /// <returns></returns>
        public bool IsMulti(ModifierKeys modifier)
        {
            return (modifier & m_MultiKey) != 0;
        }

        /// <summary>
        /// Get the key bindings of a command
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public KeyBinding GetKeyBinding(Command cmd)
        {
            m_KeyBindings.TryGetValue(cmd, out var kb);
            return kb;
        }
    }
}
