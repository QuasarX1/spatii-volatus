using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace game_management_server
{
    public class TestServerMessage : network_objects.Message
    {
        public TestServerMessage() : base(1, 3) { }

        protected override bool ValidateDataValues(ref Type[] types, ref network_objects.NetworkedDataObject[] input_data)
        {
            return types[0] == typeof(network_objects.String) && types[1] == typeof(network_objects.Vector3) && types[2] == typeof(network_objects.Vector3);
        }

        public TestServerMessage(string name, Vector3 position, Vector3 velocity) : this()
        {
            data = new network_objects.NetworkedDataObject[] { name, position, velocity };
        }

        public string Name => ((network_objects.String)data[0]).Value;
        public Vector3 Position => ((network_objects.Vector3)data[1]).Value;
        public Vector3 Velocity => ((network_objects.Vector3)data[1]).Value;

        public override string GetDisplayString()
        {
            return $"TestMessage:\n    Name: {data[0]}\n    Position: {data[1]}\n    Velocity: {data[2]}";
        }
    }
}
