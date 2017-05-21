using System;
using System.Collections.Generic;
using System.Linq;

namespace SimShift.Utils
{
    public class IniValueObject
    {
        public IniValueObject(IEnumerable<string> nestedGroup, string key, string rawValue)
        {
            this.NestedGroup = nestedGroup;
            this.Key = key;
            this.RawValue = rawValue;

            var value = rawValue;

            // Does this rawValue contain multiple values?
            if (value.StartsWith("(") && value.EndsWith(")") && value.Length > 2)
            {
                value = value.Substring(1, value.Length - 2);
            }

            if (value.StartsWith("\"") && value.EndsWith("\"") && value.Length > 2)
            {
                value = value.Substring(1, value.Length - 2);
            }

            if (value.Contains(","))
            {
                this.IsTuple = true;

                var values = value.Split(new[] { ',' });
                this.ValueArray = new string[values.Length];

                for (var i = 0; i < values.Length; i++)
                {
                    var val = values[i];
                    if (val.StartsWith("\"") && val.EndsWith("\"") && val.Length > 2)
                    {
                        val = val.Substring(1, val.Length - 2);
                    }

                    this.ValueArray[i] = val.Trim();
                }
            }
            else
            {
                this.IsTuple = false;
                this.Value = value;
            }
        }

        public string Group => this.NestedGroup.ElementAt(this.NestedGroup.Count() - 1);

        public bool IsTuple { get; private set; }

        public string Key { get; private set; }

        public IEnumerable<string> NestedGroup { get; private set; }

        public string NestedGroupName => string.Join(".", this.NestedGroup);

        public string RawValue { get; private set; }

        protected string Value { get; private set; }

        protected string[] ValueArray { get; private set; }

        public bool BelongsTo(string group)
        {
            return this.NestedGroup.Contains(group);
        }

        public double ReadAsDouble(int index)
        {
            if (!this.IsTuple)
            {
                throw new Exception("This is not a tuple value");
            }

            return double.Parse(this.ValueArray[index]);
        }

        public double ReadAsDouble()
        {
            return this.IsTuple ? this.ReadAsDouble(0) : double.Parse(this.Value);
        }

        public float ReadAsFloat(int index)
        {
            if (!this.IsTuple)
            {
                throw new Exception("This is not a tuple value");
            }

            return float.Parse(this.ValueArray[index]);
        }

        public float ReadAsFloat()
        {
            return this.IsTuple ? this.ReadAsFloat(0) : float.Parse(this.Value);
        }

        public int ReadAsInteger(int index)
        {
            if (!this.IsTuple)
            {
                throw new Exception("This is not a tuple value");
            }

            return int.Parse(this.ValueArray[index]);
        }

        public int ReadAsInteger()
        {
            return this.IsTuple ? this.ReadAsInteger(0) : int.Parse(this.Value);
        }

        public string ReadAsString(int index)
        {
            if (!this.IsTuple)
            {
                throw new Exception("This is not a tuple value");
            }

            return this.ValueArray[index];
        }

        public string ReadAsString()
        {
            return this.IsTuple ? this.ReadAsString(0) : this.Value;
        }

        public IEnumerable<string> ReadAsStringArray()
        {
            return this.IsTuple ? this.ValueArray : new[] { this.Value };
        }
    }
}