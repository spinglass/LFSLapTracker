using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LFSLapTracker
{
    class CommandLine
    {
        public CommandLine(string[] args)
        {
            m_Args = new Dictionary<string, string>();
            foreach (string arg in args)
            {
                string[] split = arg.Split(new char[] { '=' }, 2);
                string key = split[0].ToLower();
                string value = (split.Length > 1) ? split[1] : "";
                m_Args[key] = value;
            }
        }

        public bool Contains(string key)
        {
            return m_Args.ContainsKey(key.ToLower());
        }

        public string GetValue(string key)
        {
            string value;
            m_Args.TryGetValue(key.ToLower(), out value);
            return value;
        }

        public string GetValue(string key, string defaultValue)
        {
            if (Contains(key))
            {
                return GetValue(key);
            }
            return defaultValue;
        }

        public int GetValue(string key, int defaultValue)
        {
            if (Contains(key))
            {
                int value;
                if (int.TryParse(GetValue(key), out value))
                {
                    return value;
                }
            }
            return defaultValue;
        }

        Dictionary<string, string> m_Args;
    }
}
