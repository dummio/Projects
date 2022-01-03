using Newtonsoft.Json;

//@authors: Kevin Xue & Griffin Zody
// Fall 2021, CS 3500
namespace TankWars
{
    /// <summary>
    /// Simple JSON class that represents a projectile in the game.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class Projectile
    {
        // The unique ID assigned to this projectile object
        [JsonProperty(PropertyName = "proj")]
        public int ID { set; get; }

        // This projectile's location in the game world
        [JsonProperty(PropertyName = "loc")]
        public Vector2D location { set; get; }

        // This projectile's angle in the game world
        [JsonProperty(PropertyName = "dir")]
        public Vector2D orientation { set; get; }

        // Whether this projectile has hit something
        [JsonProperty(PropertyName = "died")]
        public bool died { set; get; }

        // The tank ID that fired this projectile
        [JsonProperty(PropertyName = "owner")]
        public int tankID { set; get; }

        /// <summary>
        /// Creates a new projectile.
        /// </summary>
        public Projectile()
        {
            died = false;
        }
    }
}
