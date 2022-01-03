using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Threading;

//@authors: Kevin Xue & Griffin Zody
// Fall 2021, CS 3500
namespace TankWars
{
    /// <summary>
    /// Represents and runs a server for the TankWars game. Takes settings from an XML file in the Resources folder.
    /// </summary>
    public class Server
    {
        // The default port for TankWars in CS 3500
        private const int port = 11000;

        // Stores the list of clients and their networking data
        private Dictionary<int, SocketState> clients;

        // The model object that stores the underlying game logic
        private World theWorld;

        // Mandatory modifiable XML settings
        private int MSPerFrame;
        private int FramesPerShot;
        private int RespawnRate;

        // Extra modifiable XML settings that have defaults if they are not included in the settings file
        private int TankSpeed = 3;
        private int ProjSpeed = 25;
        private int TankWallOffset = 55;
        private int ProjWallOffset = 25;
        private int MaxPowerUps = 2;
        private int PowerUpDelay = 1650;
        private int MaxHitPoints = 3;
        private int TankSize = 60;
        private int WallSize = 50;

        // Tracks whether the clients have been sent the final data they need before disconnecting a tank
        // The int represents whether the frame has not been handled, has been set up, or whether it has been handled
        private Dictionary<int, int> handledFinalDCFrame;
        private const int DCFrameUnhandled = 0;
        private const int DCFrameSent = 1;
        private const int DCFrameHandled = 2;

        // Tracks each clients' associated command using its player ID
        private Dictionary<int, Command> ClientCommands;

        // Tracks each clients' cooldown on "main" firing 
        private Dictionary<int, int> ProjectileCooldown;

        // Tracks each clients' respawn timer
        private Dictionary<int, int> PlayerDeathTimer;

        // Tracks whether each client can fire a beam (has collected a powerup)
        private Dictionary<int, int> TankPowerups;

        // Tracks whether each tank can fire a beam
        private Dictionary<int, int> BeamCoolDown;

        // Stores dead projectiles temporarily for removal
        private Stack<Projectile> DeadProjectiles;

        // Stores dead powerups temporarily for removal
        private Stack<Powerup> DeadPowerups;

        // Stores disconnected clients temporarily for removal
        private List<int> DisconnectedClients;

        // Stores the beams that have been fired
        private List<Beam> beams;

        // Used to give different game objects unique IDs based on the number that have previously existed
        private int ProjectileCount;
        private int BeamCount;

        // Tracks the number of frames that have passed since the max number of powerups has been reached
        private int PowerUpFrames;

        // Used to randomly decide when the next powerup will spawn
        private Random DelayRandomizer;

        /// <summary>
        /// Creates a new server object, reads the settings in the settings XML file into the server object, and starts
        /// the server world and networking update loops. Performs the world update loop in a separate thread.
        /// </summary>
        static void Main(string[] args)
        {
            Server theServer = new Server();

            theServer.GetSettings("..\\..\\..\\..\\Resources\\settings.xml");

            theServer.StartServer();

            // Start the update world loop on a new thread
            Thread UpdateWorldThread = new Thread(theServer.UpdateClients);

            UpdateWorldThread.Start();

            // Holds the console open
            Console.Read();
        }

        /// <summary>
        /// Creates a server object for storing information about the game.
        /// </summary>
        public Server()
        {
            clients = new Dictionary<int, SocketState>();
            handledFinalDCFrame = new Dictionary<int, int>();
            ClientCommands = new Dictionary<int, Command>();
            ProjectileCooldown = new Dictionary<int, int>();
            PlayerDeathTimer = new Dictionary<int, int>();
            TankPowerups = new Dictionary<int, int>();
            BeamCoolDown = new Dictionary<int, int>();

            DeadProjectiles = new Stack<Projectile>();
            DeadPowerups = new Stack<Powerup>();
            DisconnectedClients = new List<int>();
            beams = new List<Beam>();

            theWorld = new World();

            ProjectileCount = 0;
            PowerUpFrames = 0;
            BeamCount = 0;

            DelayRandomizer = new Random();
        }

        /// <summary>
        /// Updates the world every frames based on the frame delay passed in the settings, and sends the updated world data
        /// to each connected client. Also handles client disconnections and clears any existing beams.
        /// </summary>
        public void UpdateClients()
        {
            Stopwatch timer = new Stopwatch();

            timer.Start();

            while (true)
            {
                while (timer.ElapsedMilliseconds < MSPerFrame)
                {
                    //do nothing
                }
                timer.Restart();

                UpdateWorld();

                lock (clients)
                {
                    // For each client connected to the server, attempt to send the updated world
                    foreach (int clientID in clients.Keys)
                    {
                        // If sending the world fails and we have not flagged the client for disconnection, do so
                        if (!SendWorld(clients[clientID]) && handledFinalDCFrame[clientID] == DCFrameUnhandled)
                        {
                            DisconnectedClients.Add(clientID);
                            theWorld.GetTank(clientID).disconnected = true;
                        }
                    }

                    // Loop through the clients flagged for disconnection, and handle their final disconnection frames
                    for (int i = 0; i < DisconnectedClients.Count; i++)
                    {
                        int playerId = DisconnectedClients[i];

                        // If the DC frame has been sent, go ahead and remove the client and tank
                        if (handledFinalDCFrame[playerId] == DCFrameSent)
                        {
                            clients[playerId].TheSocket.Close();
                            clients.Remove(playerId);
                            theWorld.RemoveTank(theWorld.GetTank(playerId));
                            handledFinalDCFrame[playerId] = DCFrameHandled;
                        }
                        // Otherwise, the DC frame has not yet been sent but will be after the next loop
                        else
                        {
                            handledFinalDCFrame[playerId] = DCFrameSent;
                        }
                    }

                    // For each client currently in the process of disconnecting, check whether the DC has been fully handled
                    // and can be unflagged for disconnection
                    foreach (int clientID in handledFinalDCFrame.Keys)
                    {
                        if (handledFinalDCFrame[clientID] == DCFrameHandled)
                        {
                            DisconnectedClients.Remove(clientID);
                        }
                    }

                    // Remove any beams in the server's storage that exist after the world has been sent
                    lock (beams)
                    {
                        beams.Clear();
                    }
                }
            }
        }

        private void GetSettings(string filepath)
        {
            // Check if the file path is valid
            if (!File.Exists(filepath))
            {
                throw new Exception("File path is not valid or file is missing.");
            }

            // Makes sure the file is valid XML beforehand
            try
            {
                XDocument.Load(filepath);
            }
            catch (Exception)
            {
                throw new Exception("XML is most likely formatted incorrectly.");
            }

            // Loop through the settings file and extract the data
            using (XmlReader reader = XmlReader.Create(filepath))
            {
                if (reader.ReadToFollowing("GameSettings"))
                {
                    if (reader.ReadToFollowing("UniverseSize"))
                    {
                        reader.Read();
                        theWorld.SetWorldSize(Int32.Parse(reader.Value));
                    }

                    if (reader.ReadToFollowing("MSPerFrame"))
                    {
                        reader.Read();
                        MSPerFrame = Int32.Parse(reader.Value);
                    }

                    if (reader.ReadToFollowing("FramesPerShot"))
                    {
                        reader.Read();
                        FramesPerShot = Int32.Parse(reader.Value);
                    }

                    if (reader.ReadToFollowing("RespawnRate"))
                    {
                        reader.Read();
                        RespawnRate = Int32.Parse(reader.Value);
                    }

                    if (reader.ReadToFollowing("MaxHitPoints"))
                    {
                        reader.Read();
                        MaxHitPoints = Int32.Parse(reader.Value);
                    }

                    if (reader.ReadToFollowing("ProjectileSpeed"))
                    {
                        reader.Read();
                        ProjSpeed = Int32.Parse(reader.Value);
                    }

                    if (reader.ReadToFollowing("EngineStrength"))
                    {
                        reader.Read();
                        TankSpeed = Int32.Parse(reader.Value);
                    }

                    if (reader.ReadToFollowing("TankSize"))
                    {
                        reader.Read();
                        TankSize = Int32.Parse(reader.Value);
                    }

                    if (reader.ReadToFollowing("WallSize"))
                    {
                        reader.Read();
                        WallSize = Int32.Parse(reader.Value);
                    }

                    if (reader.ReadToFollowing("MaxPowerUps"))
                    {
                        reader.Read();
                        MaxPowerUps = Int32.Parse(reader.Value);
                    }

                    if (reader.ReadToFollowing("PowerUpDelay"))
                    {
                        reader.Read();
                        PowerUpDelay = Int32.Parse(reader.Value);
                    }

                    // Calculate the offset for wall-tank collision based on the size of the walls and tanks
                    TankWallOffset = TankSize / 2 + WallSize / 2;

                    // Calculate the offset for wall-projectile collision based on the size of the walls
                    ProjWallOffset = WallSize / 2 + 10;

                    // Used to give each wall a unique ID
                    int numWallsFound = 0;
                    while (reader.ReadToFollowing("Wall"))
                    {
                        Wall currWall = new Wall();
                        int currX;
                        int currY;

                        // Tracks whether the p1 endpoint was found first, or the p2 endpoint was found first
                        bool p1First = false;

                        // Has the  reader advance until an endpoint is reached
                        reader.Read();
                        while (!reader.IsStartElement())
                            reader.Read();

                        // If p1 is found first, handle it appropriately
                        if (reader.Name == "p1")
                        {
                            p1First = true;

                            reader.ReadToFollowing("x");
                            reader.Read();

                            currX = Int32.Parse(reader.Value);

                            reader.ReadToFollowing("y");
                            reader.Read();

                            currY = Int32.Parse(reader.Value);

                            currWall.endpoint1 = new Vector2D(currX, currY);
                        }
                        // If p2 is found first, handle it appropriately
                        else
                        {
                            reader.ReadToFollowing("x");
                            reader.Read();

                            currX = Int32.Parse(reader.Value);

                            reader.ReadToFollowing("y");
                            reader.Read();

                            currY = Int32.Parse(reader.Value);

                            currWall.endpoint2 = new Vector2D(currX, currY);
                        }

                        // Parse whichever endpoint came after the other
                        if (p1First)
                            reader.ReadToFollowing("p2");
                        else
                            reader.ReadToFollowing("p1");

                        reader.ReadToFollowing("x");
                        reader.Read();

                        currX = Int32.Parse(reader.Value);

                        reader.ReadToFollowing("y");
                        reader.Read();

                        currY = Int32.Parse(reader.Value);

                        if (p1First)
                            currWall.endpoint2 = new Vector2D(currX, currY);
                        else
                            currWall.endpoint1 = new Vector2D(currX, currY);

                        currWall.ID = numWallsFound;
                        theWorld.AddAndRemoveDeadObject(currWall);
                        numWallsFound++;
                    }
                }
            }
        }

        /// <summary>
        /// Sends all of the wall objects in the model to a client.
        /// </summary>
        /// <param name="client">the TankWars client to send the walls to</param>
        private void SendWalls(SocketState client)
        {
            foreach (Wall w in theWorld.GetWalls())
            {
                Networking.Send(client.TheSocket, JsonConvert.SerializeObject(w) + "\n");
            }
        }

        /// <summary>
        /// Attempts to send the model data to a given client. Returns false if the send failed due to a networking issue.
        /// </summary>
        /// <param name="client">the TankWars client to send the walls to</param>
        public bool SendWorld(SocketState client)
        {
            // Sends all the beams to the client
            lock (beams)
            {
                for (int i = 0; i < beams.Count; i++)
                {
                    if (!Networking.Send(client.TheSocket, JsonConvert.SerializeObject(beams[i]) + "\n"))
                        return false;
                }
            }

            lock (theWorld)
            {
                // Sends all the projectiles to the client
                foreach (Projectile proj in theWorld.GetProjectiles())
                {
                    if (!Networking.Send(client.TheSocket, JsonConvert.SerializeObject(proj) + "\n"))
                        return false;
                }

                // Sends all the tanks to the client
                foreach (Tank tank in theWorld.GetTanks())
                {
                    if (!Networking.Send(client.TheSocket, JsonConvert.SerializeObject(tank) + "\n"))
                        return false;
                }

                // Sends all the powerups to the client
                foreach (Powerup powerup in theWorld.GetPowerups())
                {
                    if (!Networking.Send(client.TheSocket, JsonConvert.SerializeObject(powerup) + "\n"))
                        return false;

                }
            }
            return true;
        }

        /// <summary>
        /// Starts the server's networking loop.
        /// </summary>
        public void StartServer()
        {
            Networking.StartServer(ClientConnected, port);
        }

        /// <summary>
        /// Runs when a client attempts to connect to the server. If successful, starts the handshake.
        /// </summary>
        /// <param name="state">networking data of the associated client</param>
        private void ClientConnected(SocketState state)
        {
            if (state.ErrorOccurred)
                return;

            state.OnNetworkAction = RecievedName;

            Networking.GetData(state);
        }

        /// <summary>
        /// The first part of the handshake, where the client sends the player's name and the server sends back the
        /// player's unique ID, the worldsize, and the walls.
        /// </summary>
        /// <param name="state">networking data of the associated client</param>
        private void RecievedName(SocketState state)
        {
            if (state.ErrorOccurred)
                return;

            string clientData = state.GetData();
            state.RemoveData(0, clientData.Length);

            // Assign the player ID based on the ID generated by the socket state class
            int playerID = (int)state.ID;

            // Send all start up info
            Networking.Send(state.TheSocket, playerID + "\n");
            Networking.Send(state.TheSocket, theWorld.GetWorldSize() + "\n");
            SendWalls(state);

            // Save the client's player data
            lock (clients)
            {
                clients.Add(playerID, state);
            }

            // Create a new tank for the client and add it to the world
            Tank player = new Tank();
            player.hitPoints = MaxHitPoints;
            player.ID = playerID;
            // Randomly generates the starting location of the tank
            player.location = GetRespawnLoc();
            player.orientation = new Vector2D(0, 0);
            player.name = clientData;
            lock (theWorld)
            {
                theWorld.AddAndRemoveDeadObject(player);
                string playerData = JsonConvert.SerializeObject(player);
                Networking.Send(state.TheSocket, playerData + "\n");
            }

            // Flag the client as having its dc frame unhandled
            lock (handledFinalDCFrame)
            {
                handledFinalDCFrame.Add(playerID, DCFrameUnhandled);
            }

            // Flag the client as not having fired a shot yet
            lock (ProjectileCooldown)
            {
                ProjectileCooldown.Add(playerID, FramesPerShot);
            }

            // Flag the client as not having collected a powerup yet
            lock (TankPowerups)
            {
                TankPowerups.Add(playerID, 0);
            }

            // Adds the beam cooldown for each tank
            lock(BeamCoolDown)
            {
                BeamCoolDown.Add(playerID, 0);
            }

            // Begin the receive command event loop
            state.OnNetworkAction = RecievedCommand;
            Networking.GetData(state);
        }

        /// <summary>
        /// Takes commands received from the client and processes them. Continues the event loop indefinitely until a client
        /// has disconnected.
        /// </summary>
        /// <param name="state">networking data of the associated client</param>
        private void RecievedCommand(SocketState state)
        {
            if (state.ErrorOccurred)
                return;

            // Parse commands recieved from the client
            string receivedCommands = state.GetData();

            // If multiple commands have been sent, split them based on newlines
            string[] passedCMDS = receivedCommands.Split("\n");

            Command cmd = null;
            Command currCmd;
            // Loop through the received commands and set the current command associated with the client to either the
            // first command sent in the frame or the frame that most recently used "alt" firing
            for (int i = 0; i < passedCMDS.Length; i++)
            {
                if (passedCMDS[i].Length > 0)
                {
                    try
                    {
                        currCmd = JsonConvert.DeserializeObject<Command>(passedCMDS[i]);
                    }
                    catch (Exception)
                    {
                        continue; // Ignore malformed client data
                    }
                    if (cmd is null || currCmd.firing == "alt")
                    {
                        cmd = currCmd;
                    }
                }
            }

            // If a command was successfully processed, set it as the client's associated command
            if (cmd != null)
            {
                int playerID = (int)state.ID;
                lock (ClientCommands)
                {
                    if (ClientCommands.ContainsKey(playerID))
                        ClientCommands[playerID] = cmd;
                    else
                        ClientCommands.Add(playerID, cmd);
                }
            }

            state.RemoveData(0, receivedCommands.Length);

            // Continue the networking loop
            Networking.GetData(state);
        }

        /// <summary>
        /// Goes through each client's current command and the objects in the model and updates the model based on
        /// current information.
        /// </summary>
        public void UpdateWorld()
        {
            lock (clients)
            {
                // Loop through the clients and check if their tank has either disconnected or died
                foreach (int playerID in clients.Keys)
                {
                    lock (theWorld)
                    {
                        // If the player has disconnected, set the tank to be dead, and remove from any tracking dictionaries
                        if (theWorld.GetTank(playerID).disconnected)
                        {
                            theWorld.GetTank(playerID).hitPoints = 0;
                            theWorld.GetTank(playerID).died = true;

                            ClientCommands.Remove(playerID);
                            ProjectileCooldown.Remove(playerID);
                            PlayerDeathTimer.Remove(playerID);
                            TankPowerups.Remove(playerID);
                        }
                        // Otherwise, check if the tank has died
                        else
                        {
                            lock (PlayerDeathTimer)
                            {
                                // If the tank has died, increase the number of frames it has been dead for
                                if (theWorld.GetTank(playerID).hitPoints == 0)
                                {
                                    if (PlayerDeathTimer.ContainsKey(playerID))
                                    {
                                        PlayerDeathTimer[playerID]++;
                                    }
                                    else
                                    {
                                        PlayerDeathTimer.Add(playerID, 0);
                                        theWorld.GetTank(playerID).died = false;
                                    }

                                    // If the number of dead frames has exceeded the respawn rate, respawn the tank with full health
                                    if (PlayerDeathTimer[playerID] >= RespawnRate)
                                    {
                                        theWorld.GetTank(playerID).location = GetRespawnLoc();
                                        theWorld.GetTank(playerID).hitPoints = MaxHitPoints;
                                        PlayerDeathTimer.Remove(playerID);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            lock (ClientCommands)
            {
                // Loop through each client's most recent command and update the world accordingly
                foreach (int playerID in ClientCommands.Keys)
                {
                    Vector2D velocity = new Vector2D(0, 0);
                    Tank currTank = theWorld.GetTank(playerID);

                    // If the tank is not dead, fire any projectiles / beams and update its location
                    if (currTank.hitPoints != 0)
                    {
                        Command playerCmd = ClientCommands[playerID];

                        // If the player has collected a powerup and has requested an alt fire, generate a beam
                        if (playerCmd.firing == "alt" && TankPowerups[playerID] > 0 && BeamCoolDown[playerID] >= 25)
                        {
                            BeamCoolDown[playerID] = 0;
                            Beam b = new Beam();
                            b.ID = BeamCount;
                            b.tankID = playerID;
                            b.location = currTank.location;
                            b.orientation = currTank.aiming;
                            lock (beams)
                            {
                                beams.Add(b);
                            }

                            // Lock and test
                            lock (TankPowerups)
                            {
                                TankPowerups[playerID]--;
                            }

                            // Check if the beam has hit any tanks, and handle any of those collisions
                            BeamCollision(b);
                        }

                        // If the player has requested a regular shot and their cooldown has expired, generate a projectile
                        if (playerCmd.firing == "main" && ProjectileCooldown[playerID] >= FramesPerShot)
                        {
                            // Reset projectile cooldown 
                            ProjectileCooldown[playerID] = 0;

                            Projectile p = new Projectile();
                            p.ID = ProjectileCount;
                            p.tankID = playerID;
                            p.location = currTank.location;
                            p.orientation = playerCmd.turretDir;

                            ProjectileCount++;
                            lock (theWorld)
                            {
                                theWorld.AddAndRemoveDeadObject(p);
                            }
                        }

                        if (playerCmd.movement == "right")
                        {
                            velocity = new Vector2D(1, 0);
                        }
                        else if (playerCmd.movement == "left")
                        {
                            velocity = new Vector2D(-1, 0);
                        }
                        else if (playerCmd.movement == "up")
                        {
                            velocity = new Vector2D(0, -1);
                        }
                        else if (playerCmd.movement == "down")
                        {
                            velocity = new Vector2D(0, 1);
                        }

                        if (playerCmd.movement != "none")
                        {
                            currTank.orientation = velocity;
                        }

                        currTank.aiming = playerCmd.turretDir;

                        // If the tank has not hit a will with its updated location, allow the update to go through
                        if (!ObjectWallCollision(currTank.location + velocity * TankSpeed, TankWallOffset))
                        {
                            currTank.location += velocity * TankSpeed;
                        }

                        // If the tank is out of bounds, move it to the opposite side of the world
                        if (TankOutOfBounds(currTank))
                        {
                            if (currTank.location.GetX() > theWorld.GetWorldSize() / 2)
                            {
                                currTank.location = new Vector2D(-theWorld.GetWorldSize() / 2 + 1, currTank.location.GetY());
                            }

                            if (currTank.location.GetX() < -theWorld.GetWorldSize() / 2)
                            {
                                currTank.location = new Vector2D(theWorld.GetWorldSize() / 2 - 1, currTank.location.GetY());
                            }

                            if (currTank.location.GetY() > theWorld.GetWorldSize() / 2)
                            {
                                currTank.location = new Vector2D(currTank.location.GetX(), -theWorld.GetWorldSize() / 2 + 1);
                            }

                            if (currTank.location.GetY() < -theWorld.GetWorldSize() / 2)
                            {
                                currTank.location = new Vector2D(currTank.location.GetX(), theWorld.GetWorldSize() / 2 - 1);
                            }
                        }

                        // Check if the tank has collected a powerup and handle that
                        TankPowerCollision(currTank);
                    }
                }
            }

            lock (theWorld)
            {
                // Remove any projectiles from the world that have been flagged as dead
                for (int i = 0; i < DeadProjectiles.Count; i++)
                {
                    theWorld.RemoveProjectile(DeadProjectiles.Pop());
                }

                // Remove any powerups from the world that have been flagged as dead
                for (int i = 0; i < DeadPowerups.Count; i++)
                {
                    theWorld.RemovePower(DeadPowerups.Pop());
                }
            }

            lock (theWorld)
            {
                // Loop through the projectiles, handle any collisions, and update location
                foreach (Projectile proj in theWorld.GetProjectiles())
                {
                    // If the projectile has gone out of bounds, hit a tank, or hit a wall, flag it as dead
                    if (ProjectileOutOfBounds(proj) || 
                        ProjectileTankCollision(proj) || 
                        ObjectWallCollision(proj.location, ProjWallOffset))
                    {
                        proj.died = true;
                        DeadProjectiles.Push(proj);
                    }

                    else
                    {
                        proj.location += proj.orientation * ProjSpeed;
                    }
                }

                // If there are less powerups in the world than the max, spawn a new one based on a random delay
                if (theWorld.GetPowerups().Count() < MaxPowerUps)
                {
                    int delay = DelayRandomizer.Next(0, PowerUpDelay);
                    if (SpawnPowerup(PowerUpFrames, delay))
                    {
                        PowerUpFrames = 0;
                    }
                    PowerUpFrames++;
                }

                // Flag any dead powerups for removal
                foreach (Powerup p in theWorld.GetPowerups())
                {
                    if (p.died)
                        DeadPowerups.Push(p);
                }
            }

            lock (ProjectileCooldown)
            {
                // Increase the projectile cooldown of all players
                foreach (int playerID in ProjectileCooldown.Keys.ToList())
                {
                    ProjectileCooldown[playerID]++;
                }
            }

            lock(BeamCoolDown)
            {
                // Increase the projectile cooldown of all players
                foreach (int playerID in BeamCoolDown.Keys.ToList())
                {
                    BeamCoolDown[playerID]++;
                }
            }
        }

        /// <summary>
        /// Spawns a powerup if the number of frames is greater than the delay. Returns if a powerup was spawned.
        /// </summary>
        /// <param name="frames">number of frames since the max number of powerups was reached</param>
        /// <param name="delay">randomly generated delay lower than the max delay</param>
        private bool SpawnPowerup(int frames, int delay)
        {
            if (frames >= delay)
            {
                Powerup p = new Powerup();
                p.location = GetRespawnLoc();
                p.ID = PowerUpFrames;
                theWorld.AddAndRemoveDeadObject(p);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Checks if a given projectile is outside the world bounds. Returns true if it is outside.
        /// </summary>
        /// <param name="p">projectile to check location of</param>
        private bool ProjectileOutOfBounds(Projectile p)
        {
            return (Math.Abs(p.location.GetX()) > theWorld.GetWorldSize() / 2 || Math.Abs(p.location.GetY()) > theWorld.GetWorldSize() / 2);
        }

        /// <summary>
        /// Checks if a given tank is outside the world bounds. Returns true if it is outside.
        /// </summary>
        /// <param name="t">tank to check location of</param>
        private bool TankOutOfBounds(Tank t)
        {
            return (Math.Abs(t.location.GetX()) > theWorld.GetWorldSize() / 2 || Math.Abs(t.location.GetY()) > theWorld.GetWorldSize() / 2);
        }

        /// <summary>
        /// Loops through the tanks in the world, and checks if the projectile has hit any of them. Returns true if so.
        /// </summary>
        /// <param name="p">the projectile to check</param>
        private bool ProjectileTankCollision(Projectile p)
        {
            foreach (Tank t in theWorld.GetTanks())
            {
                // If the current tank is the one that fired this projectile, ignore collisions
                if (t.ID == p.tankID)
                    continue;

                // If the tank collided with is alive and within 30 units, a collision has occurred
                if (t.hitPoints > 0 && p.location.GetX() <= t.location.GetX() + 30 && p.location.GetX() >= t.location.GetX() - 30 && (p.location.GetY() <= t.location.GetY() + 30 && p.location.GetY() >= t.location.GetY() - 30))
                {
                    // Decrement the hit tank's hp
                    t.hitPoints--;

                    // If the tank died, flag it as dead and adjust the projectile owner's score
                    if (t.hitPoints == 0)
                    {
                        theWorld.GetTank(p.tankID).score++;
                        t.died = true;
                    }

                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Loops through the walls in the world, and checks if the passed location is inside any of them. Returns true if so.
        /// </summary>
        /// <param name="newLocation">the location to check for being inside a wall</param>
        /// <param name="offset">the allowed distance outside of a wall</param>
        /// <returns></returns>
        private bool ObjectWallCollision(Vector2D newLocation, int offset)
        {
            foreach (Wall w in theWorld.GetWalls())
            {
                // Check for collisions for a horizontal wall with endpoint 1 being farther left
                if (w.endpoint1.GetX() < w.endpoint2.GetX())
                {
                    if ((w.endpoint1.GetX() - offset <= newLocation.GetX() && w.endpoint2.GetX() + offset >= newLocation.GetX()) && (w.endpoint1.GetY() - offset <= newLocation.GetY() && w.endpoint1.GetY() + offset >= newLocation.GetY()))
                    {
                        return true;
                    }
                }
                // Check for collisions for a horizontal wall with endpoint 2 being farther left
                else if (w.endpoint1.GetX() > w.endpoint2.GetX())
                {
                    if ((w.endpoint2.GetX() - offset <= newLocation.GetX() && w.endpoint1.GetX() + offset >= newLocation.GetX()) && (w.endpoint1.GetY() - offset <= newLocation.GetY() && w.endpoint1.GetY() + offset >= newLocation.GetY()))
                    {
                        return true;
                    }
                }
                // Check for collisions for a vertical wall with endpoint 1 being farther up
                else if (w.endpoint1.GetY() < w.endpoint2.GetY())
                {
                    if ((w.endpoint1.GetX() - offset <= newLocation.GetX() && w.endpoint1.GetX() + offset >= newLocation.GetX()) && (w.endpoint1.GetY() - offset <= newLocation.GetY() && w.endpoint2.GetY() + offset >= newLocation.GetY()))
                    {
                        return true;
                    }
                }
                // Check for collisions for a vertical wall with endpoint 2 being farther up
                else
                {
                    if ((w.endpoint1.GetX() - offset <= newLocation.GetX() && w.endpoint2.GetX() + offset >= newLocation.GetX()) && (w.endpoint2.GetY() - offset <= newLocation.GetY() && w.endpoint1.GetY() + offset >= newLocation.GetY()))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Loop through the powerups in the world and check if the tank is inside one. Returns true if so.
        /// </summary>
        /// <param name="t">the tank to check for collisions</param>
        private void TankPowerCollision(Tank t)
        {
            foreach (Powerup pow in theWorld.GetPowerups())
            {
                if (pow.location.GetX() <= t.location.GetX() + 30 && pow.location.GetX() >= t.location.GetX() - 30 && pow.location.GetY() <= t.location.GetY() + 30 && pow.location.GetY() >= t.location.GetY() - 30)
                {
                    // If the tank has collided with a powerup, flag it as having collected one
                    lock (TankPowerups)
                    {
                        TankPowerups[t.ID]++;
                    }

                    pow.died = true;
                }
            }
        }

        /// <summary>
        /// Determines if a ray interescts a circle, used for checking beam collisons.
        /// </summary>
        /// <param name="rayOrig">The origin of the ray</param>
        /// <param name="rayDir">The direction of the ray</param>
        /// <param name="center">The center of the circle</param>
        /// <param name="r">The radius of the circle</param>
        private bool Intersects(Vector2D rayOrig, Vector2D rayDir, Vector2D center, double r)
        {
            // ray-circle intersection test
            // P: hit point
            // ray: P = O + tV
            // circle: (P-C)dot(P-C)-r^2 = 0
            // substituting to solve for t gives a quadratic equation:
            // a = VdotV
            // b = 2(O-C)dotV
            // c = (O-C)dot(O-C)-r^2
            // if the discriminant is negative, miss (no solution for P)
            // otherwise, if both roots are positive, hit

            double a = rayDir.Dot(rayDir);
            double b = ((rayOrig - center) * 2.0).Dot(rayDir);
            double c = (rayOrig - center).Dot(rayOrig - center) - r * r;

            // discriminant
            double disc = b * b - 4.0 * a * c;

            if (disc < 0.0)
                return false;

            // find the signs of the roots
            // technically we should also divide by 2a
            // but all we care about is the sign, not the magnitude
            double root1 = -b + Math.Sqrt(disc);
            double root2 = -b - Math.Sqrt(disc);

            return (root1 > 0.0 && root2 > 0.0);
        }

        /// <summary>
        /// Loops through the tanks in the world and checks if the beam has hit any of them.
        /// </summary>
        /// <param name="b">the beam to check for collisions</param>
        private void BeamCollision(Beam b)
        {
            lock (theWorld)
            {
                foreach (Tank t in theWorld.GetTanks())
                {
                    // If the current tank is the one that fired this beam, ignore collisions
                    if (t.ID == b.tankID)
                        continue;
                    // If a tank is hit, immediately kill it and update the beam firer's score
                    if (t.hitPoints > 0 && Intersects(b.location, b.orientation, t.location, TankSize / 2))
                    {
                        t.hitPoints = 0;
                        t.died = true;
                        theWorld.GetTank(b.tankID).score++;
                    }
                }
            }
        }

        /// <summary>
        /// Generates random locations in the world until one is generated that does not collide with any walls, and then
        /// returns that location.
        /// </summary>
        private Vector2D GetRespawnLoc()
        {
            int worldSize = theWorld.GetWorldSize() / 2;
            Random Rand = new Random();
            int x = Rand.Next(-worldSize, worldSize);
            int y = Rand.Next(-worldSize, worldSize);

            Vector2D newLoc = new Vector2D(x, y);

            while (ObjectWallCollision(newLoc, TankWallOffset))
            {
                x = Rand.Next(-worldSize, worldSize);
                y = Rand.Next(-worldSize, worldSize);
                newLoc = new Vector2D(x, y);
            }
            return newLoc;
        }
    }
}