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
        public LoginMessage() : base(2, 2) { }//TODO: add field for time of request

        protected override bool ValidateDataValues(ref Type[] types, ref NetworkedDataObject[] input_data)
        {
            return types[0] == typeof(network_objects.datatypes.String) && types[1] == typeof(network_objects.datatypes.String);
        }

        public LoginMessage(network_objects.datatypes.String username, network_objects.datatypes.String password_hash) : this()
        {
            data = new NetworkedDataObject[] { username, password_hash };
        }

        public string Username => ((network_objects.datatypes.String)data[0]).Value;
        public string PasswordHash => ((network_objects.datatypes.String)data[1]).Value;//TODO: SECURITY: this message should be encrypted!!!

        public override string GetDisplayString()
        {
            return $"LoginMessage:\n    Username: {data[0]}";
        }
    }
}
