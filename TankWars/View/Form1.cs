using System;
using System.Drawing;
using System.Windows.Forms;

//@authors: Kevin Xue & Griffin Zody
// Fall 2021, CS 3500
namespace TankWars
{
    /// <summary>
    /// Class that represents the view of the game's client, include the server connection interface.
    /// </summary>
    public partial class Form1 : Form
    {
        // The controller handles updates from the server
        private GameController theController;

        // The model that handles the code representation of the game world
        private World theWorld;

        // The canvas for the actual game
        DrawingPanel drawingPanel;

        // Used to connect once a server and player name are entered
        Button startButton;

        // Label and textbox for the playername entry 
        Label nameLabel;
        TextBox nameText;

        // Label and textbox for the server IP entry
        TextBox serverText;
        Label serverLabel;

        // Constant sizes used for the client
        private const int viewSize = 900;
        private const int menuSize = 40;

        /// <summary>
        /// Creates a new form with the controller passed from program.cs (main)
        /// </summary>
        /// <param name="ctl">this client's controller</param>
        public Form1(GameController ctl)
        {
            InitializeComponent();
            theController = ctl;

            // Fetches the model used by the passed controller
            theWorld = theController.GetWorld();

            // When a server update has arrived, process a game frame
            theController.UpdateArrived += OnFrame;

            // When the world has loaded, inform the drawing panel it is ready
            theController.WorldLoaded += WorldLoaded;

            // When a beam is fired, add the beam to the drawing panel's list of animations to draw
            theController.BeamFired += AddBeam;

            // When a tank dies, add the tank to the drawing panel's list of animations to draw
            theController.ExplosionOccurred += AddExplosion;

            // When a networking error occurs, handle it gracefully
            theController.NetworkingErrorOccurred += ShowErrorMessage;

            // The title of this window
            this.Text = "TankWars";

            // Set the window size
            ClientSize = new Size(viewSize, viewSize + menuSize);

            // Place and add the start button
            startButton = new Button();
            startButton.Location = new Point(300, 5);
            startButton.Size = new Size(70, 20);
            startButton.Text = "Start";
            startButton.Click += StartClick;
            this.Controls.Add(startButton);

            // Place and add the name label
            nameLabel = new Label();
            nameLabel.Text = "Name:";
            nameLabel.Location = new Point(5, 10);
            nameLabel.Size = new Size(40, 15);
            this.Controls.Add(nameLabel);

            // Place and add the server label
            serverLabel = new Label();
            serverLabel.Text = "Server:";
            serverLabel.Location = new Point(150, 10);
            serverLabel.Size = new Size(40, 15);
            this.Controls.Add(serverLabel);

            // Place and add the name textbox
            nameText = new TextBox();
            nameText.Text = "player";
            nameText.Location = new Point(50, 5);
            nameText.Size = new Size(70, 15);
            this.Controls.Add(nameText);

            // Place and add the server textbox
            serverText = new TextBox();
            serverText.Text = "localhost";
            serverText.Location = new Point(195, 5);
            serverText.Size = new Size(70, 15);
            this.Controls.Add(serverText);

            // Place and add the drawing panel
            drawingPanel = new DrawingPanel(theWorld);
            drawingPanel.Location = new Point(0, menuSize);
            drawingPanel.Size = new Size(viewSize, viewSize);
            this.Controls.Add(drawingPanel);

            // Set up key and mouse handlers
            this.KeyDown += HandleKeyDown;
            this.KeyUp += HandleKeyUp;
            drawingPanel.MouseDown += HandleMouseDown;
            drawingPanel.MouseUp += HandleMouseUp;
            drawingPanel.MouseMove += HandleMouseMove;
        }

        /// <summary>
        /// When the world is loaded, inform the drawing panel and set the drawing panel to be visible.
        /// </summary>
        private void WorldLoaded()
        {
            drawingPanel.WorldLoaded();
            this.Invoke(new MethodInvoker(
                () =>
                {
                    drawingPanel.Visible = true;
                }));
        }

        /// <summary>
        /// When a beam is fired, add a beam animation to be drawn to the drawing panel.
        /// </summary>
        /// <param name="b">the beam to be animated</param>
        private void AddBeam(Beam b)
        {
            drawingPanel.AddBeamAnimation(b);
        }

        /// <summary>
        /// When a tank dies, add an explosion animation to be drawn to the drawing panel.
        /// </summary>
        /// <param name="t">the tank's explosion to be animated</param>
        private void AddExplosion(Tank t)
        {
            drawingPanel.AddExplosionAnimation(t);
        }

        /// <summary>
        /// Handles networking errors gracefully, displaying an error message and allowing the user
        /// to try to reconnect without having to re-open the client.
        /// </summary>
        /// <param name="errorMessage">the error from the networking library</param>
        private void ShowErrorMessage(string errorMessage)
        {
            string fullErrorMessage = errorMessage + "\nPlease try reconnecting.";
            string caption = "Networking Error Encountered";
            MessageBox.Show(fullErrorMessage, caption);

            // Resets the form to attempt to allow the user to attempt to reconnect
            this.Invoke(new MethodInvoker(
                () =>
                {
                    startButton.Enabled = true;
                    nameText.Enabled = true;
                    serverText.Enabled = true;
                    KeyPreview = false;
                    drawingPanel.Visible = false;
                }));
        }

        /// <summary>
        /// Attempts to connect to the server with the entered IP and player name.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StartClick(object sender, EventArgs e)
        {
            // Disable the form controls
            startButton.Enabled = false;
            nameText.Enabled = false;
            serverText.Enabled = false;

            // Enable the global form to capture key presses
            KeyPreview = true;

            // "connect" to the "server"
            theController.NetworkProtocol(serverText.Text, nameText.Text);
        }

        /// <summary>
        /// Handler for the controller's UpdateArrived event
        /// </summary>
        private void OnFrame()
        {
            // Invalidate this form and all its children
            // This will cause the form to redraw as soon as it can
            this.Invoke(new MethodInvoker(() => this.Invalidate(true)));
        }

        /// <summary>
        /// Key down handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
                Application.Exit();
            if (e.KeyCode == Keys.W)
                theController.HandleMovement("up");
            else if (e.KeyCode == Keys.A)
                theController.HandleMovement("left");
            else if (e.KeyCode == Keys.S)
                theController.HandleMovement("down");
            else if (e.KeyCode == Keys.D)
                theController.HandleMovement("right");

            // Prevent other key handlers from running
            e.SuppressKeyPress = true;
            e.Handled = true;
        }


        /// <summary>
        /// Key up handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleKeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.W)
                theController.HandleMovementCancel("up");
            else if (e.KeyCode == Keys.A)
                theController.HandleMovementCancel("left");
            else if (e.KeyCode == Keys.S)
                theController.HandleMovementCancel("down");
            else if (e.KeyCode == Keys.D)
                theController.HandleMovementCancel("right");
        }

        /// <summary>
        /// Mouse down handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                theController.HandleMouseClick("left");
            else if (e.Button == MouseButtons.Right)
                theController.HandleMouseClick("right");
        }

        /// <summary>
        /// Mouse up handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleMouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Right)
                theController.HandleMouseClickCancel();
        }

        /// <summary>
        /// Mouse move handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleMouseMove(object sender, MouseEventArgs e)
        {
            theController.HandleMouseChange(e.Location, new Point(viewSize / 2, viewSize / 2));
        }

    }
}
