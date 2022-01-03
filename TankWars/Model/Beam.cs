using Newtonsoft.Json;

//@authors: Kevin Xue & Griffin Zody
// Fall 2021, CS 3500
namespace TankWars
{
    /// <summary>
    /// Simple JSON class that represents a beam in the game.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class Beam
    {
        // The unique ID assigned to this beam object
        [JsonProperty(PropertyName = "beam")]
        public int ID { set; get; }

        // This beam's location in the game world
        [JsonProperty(PropertyName = "org")]
        public Vector2D location { set; get; }

        // This beam's angle in the game world
        [JsonProperty(PropertyName = "dir")]
        public Vector2D orientation { set; get; }

        // The ID of the tank that fired the beam
        [JsonProperty(PropertyName = "owner")]
        public int tankID { set; get; }

        /// <summary>
        /// Creates a new beam.
        /// </summary>
        public Beam()
        {

        }
    }
}