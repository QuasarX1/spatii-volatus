using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

using spaceflight_simulator.network_objects.datatypes;

namespace spaceflight_simulator.messages
{
    public sealed class LoginMessage : network_objects.Message
    {
        public LoginMessage() : base(2, 3) { }

        protected override bool ValidateDataValues(ref Type[] types, ref NetworkedDataObject[] input_data)
        {
            return types[0] == typeof(network_objects.datatypes.String) && types[1] == typeof(network_objects.datatypes.Vector3) && types[2] == typeof(network_objects.datatypes.Vector3);
        }

        public LoginMessage(network_objects.datatypes.String name, network_objects.datatypes.Vector3 position, network_objects.datatypes.Vector3 velocity) : this()
        {
            data = new NetworkedDataObject[] { name, position, velocity };
        }

        public string Name => ((network_objects.datatypes.String)data[0]).Value;
        public System.Numerics.Vector3 Position => ((network_objects.datatypes.Vector3)data[1]).Value;
        public System.Numerics.Vector3 Velocity => ((network_objects.datatypes.Vector3)data[1]).Value;

        public override string GetDisplayString()
        {
            return $"TestMessage:\n    Name: {data[0]}\n    Position: {data[1]}\n    Velocity: {data[2]}";
        }
    }
}
