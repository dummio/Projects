using Newtonsoft.Json;

//@authors: Kevin Xue & Griffin Zody
// Fall 2021, CS 3500

namespace TankWars
{
    /// <summary>
    /// Simple JSON class that represents a command sent to the server from the client.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class Command
    {
        // The client's key movement input
        [JsonProperty(PropertyName = "moving")]
        public string movement { set; get; }

        // The client's mouse click input
        [JsonProperty(PropertyName = "fire")]
        public string firing { set; get; }

        // The client's mouse movement input
        [JsonProperty(PropertyName = "tdir")]
        public Vector2D turretDir { set; get; }

        /// <summary>
        /// Creates a new server command.
        /// </summary>
        public Command()
        {
            movement = "none";
            firing = "none";
            turretDir = new Vector2D(0, 0);
        }
    }
}
