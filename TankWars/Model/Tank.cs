using Newtonsoft.Json;

//@authors: Kevin Xue & Griffin Zody
// Fall 2021, CS 3500
namespace TankWars
{
    /// <summary>
    /// Simple JSON class that represents a tank in the game.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class Tank
    {
        // The unique ID assigned to this tank object
        [JsonProperty(PropertyName = "tank")]
        public int ID { set; get; }

        // The location of this tank in the game world
        [JsonProperty(PropertyName = "loc")]
        public Vector2D location { set; get; }

        // The angle of the tank in the game world
        [JsonProperty(PropertyName = "bdir")]
        public Vector2D orientation { set; get; }

        // The angle of this tank's turret in the game world
        [JsonProperty(PropertyName = "tdir")]
        public Vector2D aiming { set; get; }

        // The name of this tank's associated player
        [JsonProperty(PropertyName = "name")]
        public string name { set; get; }

        // The health of this tank
        [JsonProperty(PropertyName = "hp")]
        public int hitPoints { set; get; }

        // The number of tanks destroyed by this tank
        [JsonProperty(PropertyName = "score")]
        public int score { set; get; }

        // Whether this tank has been destroyed
        [JsonProperty(PropertyName = "died")]
        public bool died { set; get; }

        // Whether this tank's client has disconnected from the server
        [JsonProperty(PropertyName = "dc")]
        public bool disconnected { set; get; }

        // Whether this tank's client has joined the server
        [JsonProperty(PropertyName = "join")]
        public bool joined { set; get; }

        /// <summary>
        /// Creates a new tank.
        /// </summary>
        public Tank()
        {
            aiming = new Vector2D(0, -1);
            score = 0;
            died = false;
            disconnected = false;
            joined = false;
        }
    }
}
