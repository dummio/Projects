using Newtonsoft.Json;

//@authors: Kevin Xue & Griffin Zody
// Fall 2021, CS 3500
namespace TankWars
{
    /// <summary>
    /// Simple JSON class that represents a wall in the game.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class Wall
    {
        // The unique ID assigned to this wall object
        [JsonProperty(PropertyName = "wall")]
        public int ID { set; get; }

        // The first endpoint of this wall object (coordinates)
        [JsonProperty(PropertyName = "p1")]
        public Vector2D endpoint1 { set; get; }

        // The second endpoint of this wall object (coordinates)
        [JsonProperty(PropertyName = "p2")]
        public Vector2D endpoint2 { set; get; }

        /// <summary>
        /// Creates a new wall.
        /// </summary>
        public Wall()
        {

        }
    }
}
