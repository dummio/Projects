using System;
using System.Text;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Drawing;

//@authors: Kevin Xue & Griffin Zody
// Fall 2021, CS 3500
namespace TankWars
{
    /// <summary>
    /// Class that represents the game's controller. Handles user input, networking, and server updates. 
    /// </summary>
    public class GameController
    {
        // Stores data related to this client's connection to the server
        private SocketState state;

        // Stores the unique ID assigned to the player by the server
        private int playerID;

        // Stores the world size of the game recieved from the server
        private int worldSize;

        // Stores the player name entered by the player
        private string playerName;

        // Tracks whether the unique player ID has been successfully received from the server yet
        private bool receivedPlayerID;

        // Tracks whether the world size has been successfully received from the server yet
        private bool receivedWorldSize;

        // Copy of the game's model
        private World world;

        // Delegate used for events that involve receiving data from the server
        public delegate void ServerUpdateHandler();

        // Delegate used for events that involve networking errors
        public delegate void ServerErrorHandler(string errorMessage);

        // Delegate used for beam events
        public delegate void ServerBeamHandler(Beam b);

        // Delegate used for explosion events
        public delegate void ServerExplosionHandler(Tank t);

        // Fired when data is received from the server
        public event ServerUpdateHandler UpdateArrived;

        // Fired when the world data has been received and the model is ready
        public event ServerUpdateHandler WorldLoaded;

        // Fired when a beam occurs
        public event ServerBeamHandler BeamFired;

        // Fired when a tank dies and an explosion occurs
        public event ServerExplosionHandler ExplosionOccurred;

        // Fired when an error happens during the connection process
        public event ServerErrorHandler NetworkingErrorOccurred;

        // Stores the messages received from the server
        StringBuilder serverMessage;

        // Modified in order to send different commands to the server from the client
        Command cmd;

        // Stores the user's movement inputs
        List<string> moveList = new List<string>();

        // Constant the represents the port used to play TankWars in CS 3500
        const int serverPort = 11000;

        /// <summary>
        /// Creates a new controller object.
        /// </summary>
        public GameController()
        {
            receivedPlayerID = false;
            receivedWorldSize = false;

            serverMessage = new StringBuilder("");
            cmd = new Command();
            world = new World();
        }

        /// <summary>
        /// Initiates the handshake with the game server. Takes in the server IP and player name
        /// entered into the client.
        /// </summary>
        /// <param name="server">the IP address of the game server</param>
        /// <param name="playerName">the name the player wants to use</param>
        public void NetworkProtocol(string server, string playerName)
        {
            this.playerName = playerName;

            // Attempts to establish a connection to the server on port 11000
            Networking.ConnectToServer(OnConnect, server, serverPort);
        }

        /// <summary>
        /// Callback that occurs after the networking library has attempted to connect to the server.
        /// </summary>
        /// <param name="st">object that stores data related to the server-client connection</param>
        private void OnConnect(SocketState st)
        {
            // If an error occurred during networking, fire an error occurred event and reset the controller
            if (st.ErrorOccurred)
            {
                NetworkingErrorOccurred(st.ErrorMessage);
                ResetOnError();
                return;
            }
                
            state = st;

            // Send the desired player name to the server
            Networking.Send(st.TheSocket, playerName + "\n");

            // Set the next step to be to receiving info from the server
            st.OnNetworkAction = ReceiveMessage;

            Networking.GetData(st);

        }

        /// <summary>
        /// Callback that occurs after the networking library tries to receive data from the server.
        /// </summary>
        /// <param name="st">object that stores data related to the server-client connection</param>
        private void ReceiveMessage(SocketState st)
        {
            // If an error occurred during networking, fire an error occurred event and reset the controller
            if (st.ErrorOccurred)
            {
                NetworkingErrorOccurred(st.ErrorMessage);
                ResetOnError();
                return;
            }

            // Handle any data received from the server
            ProcessServerUpdate(st);

            // Continue the event loop
            Networking.GetData(st);
        }

        /// <summary>
        /// Handles any data received from the server. This is where the JSON from the server is parsed
        /// and sent to the world in order to update the model.
        /// </summary>
        /// <param name="st">object that stores data related to the server-client connection</param>
        private void ProcessServerUpdate(SocketState st)
        {
            // Fetch each character in socket state's server message storage
            foreach (char c in st.GetData())
            {
                // If the message has not ended, store the existing data in the server message sb (stringbuilder)
                if (c != '\n')
                {
                    serverMessage.Append(c);
                }
                // Otherwise, attempt to process what is currently in the sb
                else
                {
                    // If the player ID has not yet been received, try to parse the message as a player ID
                    if (!receivedPlayerID)
                    {
                        Int32.TryParse(serverMessage.ToString(), out playerID);
                        world.SetID(playerID);
                        receivedPlayerID = true;
                    }
                    // If the player ID has not yet been received, try to parse the message as world size
                    else if (!receivedWorldSize)
                    {
                        Int32.TryParse(serverMessage.ToString(), out worldSize);
                        receivedWorldSize = true;
                        world.SetWorldSize(worldSize);

                        // Fire a world loaded event so that the drawing panel knows it won't be missing any data
                        if (WorldLoaded != null)
                            WorldLoaded();
                    }
                    // Otherwise, the client can start sending commands and parsing objects
                    else
                    {
                        if(receivedPlayerID && receivedWorldSize)
                        {
                            SendCommandToServer();
                        }
                        ProcessData(serverMessage.ToString());
                    }

                    serverMessage.Clear();
                }
            }

            // Remove the data received from the server from the socket state after it has been processed
            st.RemoveData(0, state.GetData().Length);
        }

        /// <summary>
        /// Takes a message from the server and attempts to parse it as a JSON object. Updates the model with
        /// the received information.
        /// </summary>
        /// <param name="objectString">JSON from the server</param>
        private void ProcessData(string objectString)
        {
            JObject gameObj = JObject.Parse(objectString.ToString());

            Object rebuiltObj = null;

            // Depending on what type of object the JSON contains, deserialize it into the correct type
            if (gameObj.ContainsKey("tank"))
            {
                rebuiltObj = JsonConvert.DeserializeObject<Tank>(objectString.ToString());
            }
            else if (gameObj.ContainsKey("beam"))
            {
                rebuiltObj = JsonConvert.DeserializeObject<Beam>(objectString.ToString());
            }
            else if (gameObj.ContainsKey("power"))
            {
                rebuiltObj = JsonConvert.DeserializeObject<Powerup>(objectString.ToString());
            }
            else if (gameObj.ContainsKey("proj"))
            {
                rebuiltObj = JsonConvert.DeserializeObject<Projectile>(objectString.ToString());
            }
            else if (gameObj.ContainsKey("wall"))
            {
                rebuiltObj = JsonConvert.DeserializeObject<Wall>(objectString.ToString());
            }

            // Update the model and fire possible events
            lock (world)
            {
                // Beams do not need to be stored in the model since they are only sent on one frame, 
                // instead just fire an event so the view knows to draw the beam
                if (rebuiltObj is Beam b)
                {
                    BeamFired(b);
                }
                else if (!(rebuiltObj is null))
                {
                    // If the JSON is death frame for a tank, let the view know to draw the tank exploding
                    if (rebuiltObj is Tank t && t.died)
                    {
                        ExplosionOccurred(t);
                    }

                    // Update the model
                    world.AddAndRemoveDeadObject(rebuiltObj);
                }
                    
            }

            // Inform the view a server update has occurred 
            if (UpdateArrived != null)
                UpdateArrived();
        }

        /// <summary>
        /// Modifies the movement command based on user inputs passed by the view.
        /// </summary>
        /// <param name="key">the movement key pressed by the user</param>
        public void HandleMovement(string key)
        {
            lock(moveList)
            {
                // Check to see if the key is already pressed down
                if (!moveList.Contains(key))
                    moveList.Add(key);

                // Set the movement command to the most recent key press (at the back of the list)
                cmd.movement = moveList[moveList.Count - 1];
            }
        }

        /// <summary>
        /// Modifies the movement command based on cancelled user inputs passed by the view.
        /// </summary>
        /// <param name="key">the movement key lifted by the user</param>
        public void HandleMovementCancel(string key)
        {
            lock(moveList)
            {
                moveList.Remove(key);

                // If there are still keys held down, change the movement command to the most recent one
                if (moveList.Count > 0)
                {
                    cmd.movement = moveList[moveList.Count - 1];
                }
                else
                {
                    cmd.movement = "none";
                }
            }
        }

        /// <summary>
        /// Modifies the firing command based on whether the used left or right clicked.
        /// </summary>
        /// <param name="key">the mouse key pressed by the user</param>
        public void HandleMouseClick(string key)
        {
            if (key == "left")
                cmd.firing = "main";
            else if (key == "right")
                cmd.firing = "alt";
        }

        /// <summary>
        /// Sets the firing command to none when a mouse click has been lifted.
        /// </summary>
        public void HandleMouseClickCancel()
        {
            cmd.firing = "none";
        }

        /// <summary>
        /// Modifies the direction of the player's tank's turret based on the mouse's position.
        /// </summary>
        /// <param name="p1">the location of the mouse</param>
        /// <param name="p2">the middle of the view</param>
        public void HandleMouseChange(Point p1, Point p2)
        {
            if (receivedPlayerID && receivedWorldSize)
            {
                Vector2D turretDir = new Vector2D(p1.X - p2.X, p1.Y - p2.Y);
                turretDir.Normalize();
                cmd.turretDir = turretDir;
            }
        }

        /// <summary>
        /// Sends the current command to the server as JSON.
        /// </summary>
        private void SendCommandToServer()
        {
            Networking.Send(state.TheSocket, JsonConvert.SerializeObject(cmd) + "\n");
        }

        /// <summary>
        /// Used to reset the necessary properties of the controller and model when a networking error occurs.
        /// </summary>
        private void ResetOnError()
        {
            receivedPlayerID = false;
            receivedWorldSize = false;

            serverMessage.Clear();

            // Resets the model
            world.ClearObjects();
        }

        /// <summary>
        /// Returns a reference to the model object used and updated by this controller
        /// </summary>
        public World GetWorld()
        {
            return world;
        }
    }

    
}
