using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SimShift.Utils
{
    public class IniReader : IDisposable
    {
        protected readonly IList<string> _group = new List<string>();

        protected readonly IList<Action<IniValueObject>> _handlers = new List<Action<IniValueObject>>();

        public IniReader(string dataSource)
            : this(dataSource, true)
        { }

        public IniReader(string dataSource, bool isFileData)
        {
            this.Filedata = string.Empty;

            if (isFileData)
            {
                if (File.Exists(dataSource) == false)
                {
                    throw new IOException("Could not find file " + dataSource);
                }

                this.Filename = dataSource;
                this.Filedata = File.ReadAllText(dataSource);
            }
            else
            {
                this.Filedata = dataSource;
            }
        }

        public string Filedata { get; private set; }

        public string Filename { get; private set; }

        public void AddHandler(Action<IniValueObject> handler)
        {
            if (this._handlers.Contains(handler) == false)
            {
                this._handlers.Add(handler);
            }
        }

        public void ApplyGroup(string group, bool nest)
        {
            if (nest == false)
            {
                this._group.Clear();
            }

            this._group.Add(group);
        }

        public void Dispose()
        {
            this.Filedata = null;
            this._group.Clear();
            this._handlers.Clear();
        }

        public void Parse()
        {
            if (this.Filedata == string.Empty)
            {
                throw new Exception("No data assigned to this reader");
            }

            var filelines = this.Filedata.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Select(x => x.Contains("//") ? x.Remove(x.IndexOf("//")).Trim() : x.Trim()).Where(x => x.Length != 0).ToList();

            this.ApplyGroup("Main", false);

            for (var i = 0; i < filelines.Count; i++)
            {
                var line = filelines[i];
                var nextLine = (i + 1 == filelines.Count) ? string.Empty : filelines[i + 1];

                if (line == "{")
                {
                    continue;
                }

                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    if (line.Length < 3)
                    {
                        this.ApplyGroup(string.Empty, false);
                    }

                    this.ApplyGroup(line.Substring(1, line.Length - 2), false);
                    continue;
                }

                if (line == "}")
                {
                    this.LeaveGroup(true);
                    continue;
                }

                if (nextLine == "{")
                {
                    // This is a header.
                    this.ApplyGroup(line, true);
                    continue;
                }

                // Parse this value.
                var key = string.Empty;
                var value = string.Empty;

                if (line.Contains("="))
                {
                    var data = line.Split(new[] { '=' }, 2);
                    key = data[0].Trim();
                    value = data[1].Trim();
                }
                else
                {
                    value = line;
                }

                var obj = new IniValueObject(this._group, key, value);

                foreach (var handler in this._handlers)
                {
                    handler(obj);
                }
            }
        }

        public void RemoveHandler(Action<IniValueObject> handler)
        {
            if (this._handlers.Contains(handler))
            {
                this._handlers.Remove(handler);
            }
        }

        private void LeaveGroup(bool nest)
        {
            if (nest == false || this._group.Count <= 1)
            {
                this.ApplyGroup("Main", false);
            }
            else
            {
                this._group.RemoveAt(this._group.Count - 1); // remove last element
            }
        }
    }
}