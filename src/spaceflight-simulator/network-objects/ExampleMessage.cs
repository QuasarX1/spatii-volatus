using System;
using System.Collections.Generic;
using System.Text;

namespace network_objects
{
    public sealed class ExampleMessage : Message
    {
        public ExampleMessage() : base(0, 1) { }

        protected override bool ValidateDataValues(ref Type[] types, ref NetworkedDataObject[] input_data)
        {
            return types[0] == typeof(Integer32);
        }

        public ExampleMessage(int value) : this()
        {
            data = new NetworkedDataObject[] { new Integer32(value) };
        }

        public override string GetDisplayString()
        {
            return data[0].ToString();
        }
    }
}
