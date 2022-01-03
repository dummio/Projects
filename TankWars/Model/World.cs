using System.Collections.Generic;

//@authors: Kevin Xue & Griffin Zody
// Fall 2021, CS 3500
namespace TankWars
{
    /// <summary>
    /// Class that represents the game's model. Stores data about what objects currently exist in the game.
    /// </summary>
    public class World
    {
        // Stores the living tanks
        private Dictionary<int, Tank> tanks;

        // Stores the powerups
        private Dictionary<int, Powerup> powerups;

        // Stores the projectiles
        private Dictionary<int, Projectile> projectiles;

        // Stores the walls
        private Dictionary<int, Wall> walls;

        // The unique ID assigned to the player by the server
        private int playerID;

        // The world size received from the server
        private int worldSize;

        // Where this client's player's tank is (coordinates)
        private double prevPlayerLocX;
        private double prevPlayerLocY;

        /// <summary>
        /// Creates a new world object.
        /// </summary>
        public World()
        {
            tanks = new Dictionary<int, Tank>();
            powerups = new Dictionary<int, Powerup>();
            projectiles = new Dictionary<int, Projectile>();
            walls = new Dictionary<int, Wall>();

            prevPlayerLocX = 0;
            prevPlayerLocY = 0;
        }

        /// <summary>
        /// Returns the player's tank's current X coordinate if their tank is alive, or their most
        /// recent location prior to death.
        /// </summary>
        public float GetPlayerLocationX()
        {
            if(tanks.ContainsKey(playerID))
                return (float) tanks[playerID].location.GetX();
            else
            {
                return (float) prevPlayerLocX;
            }
        }

        /// <summary>
        /// Returns the player's tank's current Y coordinate if their tank is alive, or their most
        /// recent location prior to death.
        /// </summary>
        public float GetPlayerLocationY()
        {
            if (tanks.ContainsKey(playerID))
                return (float)tanks[playerID].location.GetY();
            else
            {
                return (float)prevPlayerLocY;
            }
        }

        /// <summary>
        /// Takes an object received from the server, sent by the controller, and updates the model
        /// based on the type of object it is.
        /// </summary>
        /// <param name="o">the JSON object received from the controller</param>
        public void AddAndRemoveDeadObject(object o)
        {
            if (o is Tank tank)
            {
                // Tank has hp and is not in world
                if (tank.hitPoints != 0 && !tanks.ContainsKey(tank.ID))
                {
                    tanks.Add(tank.ID, tank);
                }
                // Tank does not have hp and is still in world
                else if (tank.hitPoints == 0 && tanks.ContainsKey(tank.ID))
                {
                    tanks.Remove(tank.ID);
                }
                // Tank has hp and is in world
                else if (tank.hitPoints != 0 && tanks.ContainsKey(tank.ID))
                {
                     tanks[tank.ID] = tank;
                }

                // If the player tank has died, update the prev location with their location
                if (tank.ID == playerID && tank.died)
                {
                    prevPlayerLocX = tank.location.GetX();
                    prevPlayerLocY = tank.location.GetY();
                }

                // If a tank has disconnected, remove it from the model
                if (tank.disconnected)
                {
                    tanks.Remove(tank.ID);
                }
            }

            if (o is Powerup pow)
            {
                if (!powerups.ContainsKey(pow.ID) && !pow.died)
                {
                    powerups.Add(pow.ID, pow);
                }
                else
                {
                    if (pow.died)
                    {
                        powerups.Remove(pow.ID);
                    }
                    else
                        powerups[pow.ID] = pow;
                }
            }

            if (o is Projectile p)
            {
                if (!projectiles.ContainsKey(p.ID) && !p.died)
                {
                    projectiles.Add(p.ID, p);
                }
                else
                {
                    if (p.died)
                        projectiles.Remove(p.ID);
                    else
                        projectiles[p.ID] = p;
                }
            }

            if (o is Wall w)
            {
                if (!walls.ContainsKey(w.ID))
                {
                    walls.Add(w.ID, (Wall)o);
                }
                else
                {
                    walls[w.ID] = (Wall)o;
                }
            }
        }

        /// <summary>
        /// Returns a list of living tanks.
        /// </summary>
        public IEnumerable<Tank> GetTanks()
        {
            return tanks.Values;
        }

        /// <summary>
        /// Returns a list of living powerups.
        /// </summary>
        public IEnumerable<Powerup> GetPowerups()
        {
            return powerups.Values;
        }

        /// <summary>
        /// Returns a list of living projectiles.
        /// </summary>
        public IEnumerable<Projectile> GetProjectiles()
        {
            return projectiles.Values;
        }

        /// <summary>
        /// Returns a list of walls.
        /// </summary>
        public IEnumerable<Wall> GetWalls()
        {
            return walls.Values;
        }

        /// <summary>
        /// Sets the size of this world as received from the server.
        /// </summary>
        public void SetWorldSize(int size)
        {
            worldSize = size;
        }

        /// <summary>
        /// Returns the size of this world received from the server.
        /// </summary>
        public int GetWorldSize()
        {
            return worldSize;
        }

        /// <summary>
        /// Sets the player ID of this world as received from the server.
        /// </summary>
        public void SetID(int i)
        {
            playerID = i;
        }

        /// <summary>
        /// Returns the player ID of this world received from the server.
        /// </summary>
        public int GetID()
        {
            return playerID;
        }

        /// <summary>
        /// Resets the objects in this world.
        /// </summary>
        public void ClearObjects()
        {
            tanks.Clear();
            powerups.Clear();
            projectiles.Clear();
            walls.Clear();

            prevPlayerLocX = 0;
            prevPlayerLocY = 0;
        }

        /// <summary>
        /// Server side model methods
        /// </summary>
        /// <returns></returns>
        /// 

        public Tank GetTank(int id) 
        {
            return tanks[id];
        }

        public void RemoveProjectile(Projectile p)
        {
            projectiles.Remove(p.ID);
        }
 
        public IEnumerable<Powerup> GetPowerUps()
        {
            return powerups.Values;
        }

        public void RemovePower(Powerup p)
        {
            powerups.Remove(p.ID);
        }

        public void RemoveTank(Tank t)
        {
            tanks.Remove(t.ID);
        }
    }
}
