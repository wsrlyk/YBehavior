using System;
using System.Collections.Generic;
using System.Windows.Input;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace YBehavior.Editor.Core.New
{
    public enum Command
    {
        None,
        Delete,
        Duplicate,
        Copy,
        Paste,
        Undo,
        Redo,
        Open,
        Save,
        Search,
        BreakPoint,
        LogPoint,
        Disable,
        Condition,
        Fold,
        Default,
        Center,
        Clear,
        DebugContinue,
        DebugStepOver,
        DebugStepIn,
    }
    [JsonObject(MemberSerialization.OptIn)]
    public class KeyBindings
    {
        [JsonProperty(PropertyName = "UserConfig")]
        UserConfig m_UserConfig = new UserConfig();

        ModifierKeys m_MultiKey = ModifierKeys.Shift;
        public ModifierKeys MultiKey { get { return m_MultiKey; } }

        public struct KeyBinding
        {
            public Key key;
            public ModifierKeys modifier;

            public KeyBinding(Key k, ModifierKeys m)
            {
                key = k;
                modifier = m;
            }
        }
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

        public bool IsMulti(ModifierKeys modifier)
        {
            return (modifier & m_MultiKey) != 0;
        }

        public KeyBinding GetKeyBinding(Command cmd)
        {
            m_KeyBindings.TryGetValue(cmd, out var kb);
            return kb;
        }
    }
}
