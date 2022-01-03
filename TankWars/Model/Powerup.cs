using Newtonsoft.Json;

//@authors: Kevin Xue & Griffin Zody
// Fall 2021, CS 3500
namespace TankWars
{
    /// <summary>
    /// Simple JSON class that represents a powerup in the game.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class Powerup
    {
        // The unique ID assigned to this powerup object
        [JsonProperty(PropertyName = "power")]
        public int ID { set; get; }

        // This powerup's location in the game world
        [JsonProperty(PropertyName = "loc")]
        public Vector2D location { set; get; }

        // Whether this powerup has been collected or not
        [JsonProperty(PropertyName = "died")]
        public bool died { set; get; }

        /// <summary>
        /// Creates a new powerup.
        /// </summary>
        public Powerup()
        {
            died = false;
        }
    }
}
