using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

//@authors: Kevin Xue & Griffin Zody
// Fall 2021, CS 3500
namespace TankWars
{
    /// <summary>
    /// The canvas for drawing the game. Contains the images and logic used for the game's visuals.
    /// </summary>
    public class DrawingPanel : Panel
    {
        // Copy of the game's model
        private World theWorld;

        // Stores the pre-loaded tank images
        private Dictionary<int, Image> tankImages;

        // Stores the pre-loaded turret images 
        private Dictionary<int, Image> turretImages;

        // Stores the pre-loaded shot/projectile images 
        private Dictionary<int, Image> shotImages;

        // Image used for the walls
        private Image wallImage;

        // Image used for the background
        private Image backgroundImage;

        // Image used for parts of the explosions
        private Image explosionPieceImage;

        // Tracks the frames of beams as they are fired and expire
        Dictionary<Beam, int> beamFrames;

        // Tracks the frames of the explosions of recently killed tanks
        Dictionary<Tank, int> tankExplosionFrames;

        // Used to when beams have finished their animations and need to be removed
        Stack<Beam> finishedBeams;

        // Used to when explosions have finished their animations and need to be removed
        Stack<Tank> finishedExplosions;

        // Represent the integer keys associated with each color of tank (used with the image dictionaries)
        const int Blue = 0;
        const int Grey = 1;
        const int Green = 2;
        const int LightGreen = 3;
        const int Orange = 4;
        const int Purple = 5;
        const int Red = 6;
        const int Yellow = 7;

        // Represent the length of the animations of beams and explosions
        const int maxBeamFrames = 10;
        const int maxExplosionFrames = 15;

        // True if the world data has been received and properly loaded (the world is ready to be drawn)
        private bool isWorldLoaded;

        /// <summary>
        /// Creates a new drawing panel using the data from the passed world (model).
        /// </summary>
        /// <param name="w">the model whose data is to be drawn</param>
        public DrawingPanel(World w)
        {
            DoubleBuffered = true;
            theWorld = w;

            // Preloads the images of tanks
            tankImages = new Dictionary<int, Image>();
            tankImages.Add(Blue, Image.FromFile("..\\..\\..\\Resources\\Image\\BlueTank.png"));
            tankImages.Add(Grey, Image.FromFile("..\\..\\..\\Resources\\Image\\DarkTank.png"));
            tankImages.Add(Green, Image.FromFile("..\\..\\..\\Resources\\Image\\GreenTank.png"));
            tankImages.Add(LightGreen, Image.FromFile("..\\..\\..\\Resources\\Image\\LightGreenTank.png"));
            tankImages.Add(Orange, Image.FromFile("..\\..\\..\\Resources\\Image\\OrangeTank.png"));
            tankImages.Add(Purple, Image.FromFile("..\\..\\..\\Resources\\Image\\PurpleTank.png"));
            tankImages.Add(Red, Image.FromFile("..\\..\\..\\Resources\\Image\\RedTank.png"));
            tankImages.Add(Yellow, Image.FromFile("..\\..\\..\\Resources\\Image\\YellowTank.png"));

            // Preloads the images of the tank turrets
            turretImages = new Dictionary<int, Image>();
            turretImages.Add(Blue, Image.FromFile("..\\..\\..\\Resources\\Image\\BlueTurret.png"));
            turretImages.Add(Grey, Image.FromFile("..\\..\\..\\Resources\\Image\\DarkTurret.png"));
            turretImages.Add(Green, Image.FromFile("..\\..\\..\\Resources\\Image\\GreenTurret.png"));
            turretImages.Add(LightGreen, Image.FromFile("..\\..\\..\\Resources\\Image\\LightGreenTurret.png"));
            turretImages.Add(Orange, Image.FromFile("..\\..\\..\\Resources\\Image\\OrangeTurret.png"));
            turretImages.Add(Purple, Image.FromFile("..\\..\\..\\Resources\\Image\\PurpleTurret.png"));
            turretImages.Add(Red, Image.FromFile("..\\..\\..\\Resources\\Image\\RedTurret.png"));
            turretImages.Add(Yellow, Image.FromFile("..\\..\\..\\Resources\\Image\\YellowTurret.png"));

            // Preloads the projectile images of the tanks
            shotImages = new Dictionary<int, Image>();
            shotImages.Add(Blue, Image.FromFile("..\\..\\..\\Resources\\Image\\shot-blue.png"));
            shotImages.Add(Grey, Image.FromFile("..\\..\\..\\Resources\\Image\\shot-grey.png"));
            shotImages.Add(Green, Image.FromFile("..\\..\\..\\Resources\\Image\\shot-green.png"));
            shotImages.Add(LightGreen, Image.FromFile("..\\..\\..\\Resources\\Image\\shot-brown.png"));
            shotImages.Add(Orange, Image.FromFile("..\\..\\..\\Resources\\Image\\shot-white.png"));
            shotImages.Add(Purple, Image.FromFile("..\\..\\..\\Resources\\Image\\shot-violet.png"));
            shotImages.Add(Red, Image.FromFile("..\\..\\..\\Resources\\Image\\shot-red.png"));
            shotImages.Add(Yellow, Image.FromFile("..\\..\\..\\Resources\\Image\\shot-yellow.png"));

            // Preloads other needed images
            backgroundImage = Image.FromFile("..\\..\\..\\Resources\\Image\\Background.png");
            wallImage = Image.FromFile("..\\..\\..\\Resources\\Image\\WallSprite.png");
            explosionPieceImage = Image.FromFile("..\\..\\..\\Resources\\Image\\shot-white.png");

            // Set up the storage to keep track of animation frames
            beamFrames = new Dictionary<Beam, int>();
            tankExplosionFrames = new Dictionary<Tank, int>();

            // Set up the stacks that keep track of finished animations
            finishedBeams = new Stack<Beam>();
            finishedExplosions = new Stack<Tank>();

            isWorldLoaded = false;
        }

        /// <summary>
        /// Used to let the drawing panel know when the world is ready to be drawn.
        /// </summary>
        public void WorldLoaded()
        {
            isWorldLoaded = true;
        }

        /// <summary>
        /// Adds a beam to the list of beams to animate.
        /// </summary>
        /// <param name="b">the beam to animate</param>
        public void AddBeamAnimation(Beam b)
        {
            lock (beamFrames)
            {
                // Starts the animation at 0 frames
                beamFrames.Add(b, 0);
            }
        }

        /// <summary>
        /// Adds a tank to the list of tank explosions to animate.
        /// </summary>
        /// <param name="t">the tank with an explosion to animate</param>
        public void AddExplosionAnimation(Tank t)
        {
            lock (tankExplosionFrames)
            {
                // Starts the animation at 0 frames
                tankExplosionFrames.Add(t, 0);
            }
        }

        // A delegate for DrawObjectWithTransform
        // Methods matching this delegate can draw whatever they want using e  
        public delegate void ObjectDrawer(object o, PaintEventArgs e);

        /// <summary>
        /// This method performs a translation and rotation to drawn an object in the world.
        /// </summary>
        /// <param name="e">PaintEventArgs to access the graphics (for drawing)</param>
        /// <param name="o">The object to draw</param>
        /// <param name="worldX">The X coordinate of the object in world space</param>
        /// <param name="worldY">The Y coordinate of the object in world space</param>
        /// <param name="angle">The orientation of the objec, measured in degrees clockwise from "up"</param>
        /// <param name="drawer">The drawer delegate. After the transformation is applied, the delegate is invoked to draw whatever it wants</param>
        private void DrawObjectWithTransform(PaintEventArgs e, object o, double worldX, double worldY, double angle, ObjectDrawer drawer)
        {
            // "push" the current transform
            System.Drawing.Drawing2D.Matrix oldMatrix = e.Graphics.Transform.Clone();

            e.Graphics.TranslateTransform((int)worldX, (int)worldY);
            e.Graphics.RotateTransform((float)angle);
            drawer(o, e);

            // "pop" the transform
            e.Graphics.Transform = oldMatrix;
        }

        /// <summary>
        /// Acts as a drawing delegate for DrawObjectWithTransform
        /// After performing the necessary transformation (translate/rotate)
        /// DrawObjectWithTransform will invoke this method to draw a tank
        /// </summary>
        /// <param name="o">The object to draw</param>
        /// <param name="e">The PaintEventArgs to access the graphics</param>
        private void TankDrawer(object o, PaintEventArgs e)
        {
            Tank t = o as Tank;

            int width = 60;
            int height = 60;
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            Rectangle r = new Rectangle(-(width / 2), -(height / 2), width, height);

            // Picks which tank image to use based on the tank ID
            e.Graphics.DrawImage(tankImages[t.ID % 8], r);
        }

        /// <summary>
        /// Acts as a drawing delegate for DrawObjectWithTransform
        /// After performing the necessary transformation (translate/rotate)
        /// DrawObjectWithTransform will invoke this method to draw a tank's turret
        /// </summary>
        /// <param name="o">The object to draw</param>
        /// <param name="e">The PaintEventArgs to access the graphics</param>
        private void TurretDrawer(object o, PaintEventArgs e)
        {
            Tank t = o as Tank;

            int width = 50;
            int height = 50;
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            Rectangle r = new Rectangle(-(width / 2), -(height / 2), width, height);

            // Picks which turret image to use based on the tank ID
            e.Graphics.DrawImage(turretImages[t.ID % 8], r);
        }

        /// <summary>
        /// Acts as a drawing delegate for DrawObjectWithTransform
        /// After performing the necessary transformation (translate/rotate)
        /// DrawObjectWithTransform will invoke this method to draw a tank's HP
        /// </summary>
        /// <param name="o">The object to draw</param>
        /// <param name="e">The PaintEventArgs to access the graphics</param>
        private void TankHPDrawer(object o, PaintEventArgs e)
        {
            Tank t = o as Tank;

            int tankWidth = 60;
            int tankHeight = 60;
            int hpWidth = 50;
            int hpHeight = 5;
            int tankOffset = -15;

            // Changes the color and size of the HP bar based on the health of the tank
            System.Drawing.SolidBrush hpBrush;
            if (t.hitPoints == 3)
            {
                hpBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Green);
            }
            else if (t.hitPoints == 2)
            {
                hpBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Yellow);
                hpWidth = 35;
            }
            else if (t.hitPoints == 1)
            {
                hpBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Red);
                hpWidth = 20;
            }
            else
            {
                hpBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Empty);
            }

            using (hpBrush)
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                Rectangle r = new Rectangle(-(tankWidth / 2) + (tankWidth - hpWidth) / 2, -(tankHeight / 2) + tankOffset, hpWidth, hpHeight);
                e.Graphics.FillRectangle(hpBrush, r);
            }
        }

        /// <summary>
        /// Acts as a drawing delegate for DrawObjectWithTransform
        /// After performing the necessary transformation (translate/rotate)
        /// DrawObjectWithTransform will invoke this method to draw a tank's name
        /// and score
        /// </summary>
        /// <param name="o">The object to draw</param>
        /// <param name="e">The PaintEventArgs to access the graphics</param>
        private void TankNameAndScoreDrawer(object o, PaintEventArgs e)
        {
            Tank t = o as Tank;

            int tankWidth = 60;
            int nameHeightOffset = 35;
            int nameWidth = 100;
            int nameHeight = 60;

            string nameScore = t.name + ": " + t.score;

            Font drawFont = new Font("Arial", 12);

            // Align the string in the center of its parent object
            StringFormat drawFormat = new StringFormat();
            drawFormat.Alignment = StringAlignment.Center;
            drawFormat.LineAlignment = StringAlignment.Near;

            using (SolidBrush drawBrush = new System.Drawing.SolidBrush(Color.White))
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                Rectangle r = new Rectangle(-(tankWidth / 2) + (tankWidth - nameWidth) / 2, nameHeightOffset, nameWidth, nameHeight);

                e.Graphics.DrawString(nameScore, drawFont, drawBrush, r, drawFormat);
            }
        }

        /// <summary>
        /// Acts as a drawing delegate for DrawObjectWithTransform
        /// After performing the necessary transformation (translate/rotate)
        /// DrawObjectWithTransform will invoke this method to draw one piece of
        /// a tank's explosion
        /// </summary>
        /// <param name="o">The object to draw</param>
        /// <param name="e">The PaintEventArgs to access the graphics</param>
        private void ExplosionPieceDrawer(object o, PaintEventArgs e)
        {
            int width = 15;
            int height = 15;
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            Rectangle r = new Rectangle(-(width / 2), -(height / 2), width, height);

            e.Graphics.DrawImage(explosionPieceImage, r);
        }

        /// <summary>
        /// Acts as a drawing delegate for DrawObjectWithTransform
        /// After performing the necessary transformation (translate/rotate)
        /// DrawObjectWithTransform will invoke this method to draw a powerup
        /// </summary>
        /// <param name="o">The object to draw</param>
        /// <param name="e">The PaintEventArgs to access the graphics</param>
        private void PowerupDrawer(object o, PaintEventArgs e)
        {
            int width = 10;
            int height = 10;
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (System.Drawing.SolidBrush brush = new System.Drawing.SolidBrush(System.Drawing.Color.WhiteSmoke))
            {
                Rectangle r1 = new Rectangle(-(width / 2), -(height / 2), width, height);
                e.Graphics.FillEllipse(brush, r1);

                brush.Color = Color.LightSkyBlue;
                Rectangle r2 = new Rectangle(r1.X / 2, r1.Y / 2, width / 2, height / 2);
                e.Graphics.FillEllipse(brush, r2);
            }
        }

        /// <summary>
        /// Acts as a drawing delegate for DrawObjectWithTransform
        /// After performing the necessary transformation (translate/rotate)
        /// DrawObjectWithTransform will invoke this method to draw a projectile
        /// </summary>
        /// <param name="o">The object to draw</param>
        /// <param name="e">The PaintEventArgs to access the graphics</param>
        private void ProjectileDrawer(object o, PaintEventArgs e)
        {
            Projectile p = o as Projectile;

            int width = 30;
            int height = 30;
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(-(width / 2), -(height / 2), width, height);

            // Draws a projectile based on the ID of the tank that shot the projectile
            e.Graphics.DrawImage(shotImages[p.tankID % 8], r);
        }

        /// <summary>
        /// Acts as a drawing delegate for DrawObjectWithTransform
        /// After performing the necessary transformation (translate/rotate)
        /// DrawObjectWithTransform will invoke this method to draw a beam
        /// </summary>
        /// <param name="o">The object to draw</param>
        /// <param name="e">The PaintEventArgs to access the graphics</param>
        private void BeamDrawer(object o, PaintEventArgs e)
        {
            Beam b = o as Beam;

            Pen p;
            lock (beamFrames)
            {
                p = new Pen(Color.White, 20f - beamFrames[b] * 2);
            }

            // The size of the beam changes decreases as its frames increase
            using (p)
            {
                e.Graphics.DrawLine(p, new Point(0, 0), new Point(0, -theWorld.GetWorldSize()));
            }
        }

        /// <summary>
        /// Acts as a drawing delegate for DrawObjectWithTransform
        /// After performing the necessary transformation (translate/rotate)
        /// DrawObjectWithTransform will invoke this method to draw a wall
        /// </summary>
        /// <param name="o">The object to draw</param>
        /// <param name="e">The PaintEventArgs to access the graphics</param>
        private void WallDrawer(object o, PaintEventArgs e)
        {
            Wall p = o as Wall;

            int width = 50;
            int height = 50;
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(-(width / 2), -(height / 2), width, height);

            e.Graphics.DrawImage(wallImage, r);
        }

        /// <summary>
        /// Acts as a drawing delegate for DrawObjectWithTransform
        /// After performing the necessary transformation (translate/rotate)
        /// DrawObjectWithTransform will invoke this method to draw the background
        /// </summary>
        /// <param name="o">The object to draw</param>
        /// <param name="e">The PaintEventArgs to access the graphics</param>
        private void BackgroundDrawer(object o, PaintEventArgs e)
        {
            int width = theWorld.GetWorldSize();
            int height = theWorld.GetWorldSize();
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            Rectangle r = new Rectangle(-(width / 2), -(height / 2), width, height);
            e.Graphics.DrawImage(backgroundImage, r);
        }

        /// <summary>
        /// Redraws the drawing panel when this component is invoked.
        /// </summary>
        /// <param name="e">The PaintEventArgs to access the graphics</param>
        protected override void OnPaint(PaintEventArgs e)
        {
            // The size of this component
            int viewSize = Size.Width;

            // Draw the background and move the camera if the world is ready to be drawn
            if (isWorldLoaded)
            {
                e.Graphics.TranslateTransform(-theWorld.GetPlayerLocationX() + (viewSize / 2), -theWorld.GetPlayerLocationY() + (viewSize / 2));
                BackgroundDrawer(null, e);
            }

            lock (theWorld)
            {
                //Draw the tanks and their GUI 
                foreach (Tank tank in theWorld.GetTanks())
                {
                    DrawObjectWithTransform(e, tank, tank.location.GetX(), tank.location.GetY(), tank.orientation.ToAngle(), TankDrawer);
                    DrawObjectWithTransform(e, tank, tank.location.GetX(), tank.location.GetY(), tank.aiming.ToAngle(), TurretDrawer);
                    DrawObjectWithTransform(e, tank, tank.location.GetX(), tank.location.GetY(), 0, TankHPDrawer);
                    DrawObjectWithTransform(e, tank, tank.location.GetX(), tank.location.GetY(), 0, TankNameAndScoreDrawer);
                }

                // Draw the powerups
                foreach (Powerup pow in theWorld.GetPowerups())
                {
                    DrawObjectWithTransform(e, pow, pow.location.GetX(), pow.location.GetY(), 0, PowerupDrawer);
                }

                // Draw the walls
                foreach (Wall w in theWorld.GetWalls())
                {
                    // Wall is vertical
                    if (w.endpoint1.GetX() == w.endpoint2.GetX())
                    {
                        double numWalls = Math.Abs(w.endpoint2.GetY() - w.endpoint1.GetY()) / 50;

                        for (int i = 0; i <= numWalls; i++)
                        {
                            // Draw from left to right starting with the leftmost endpoint
                            if (w.endpoint1.GetY() < w.endpoint2.GetY())
                            {
                                DrawObjectWithTransform(e, w, w.endpoint1.GetX(), w.endpoint1.GetY() + i * 50, 0, WallDrawer);
                            }
                            else
                            {
                                DrawObjectWithTransform(e, w, w.endpoint2.GetX(), w.endpoint2.GetY() + i * 50, 0, WallDrawer);
                            }
                        }
                    }
                    // Wall is horizontal
                    else
                    {
                        double numWalls = Math.Abs(w.endpoint2.GetX() - w.endpoint1.GetX()) / 50;

                        for (int i = 0; i <= numWalls; i++)
                        {
                            // Draw from top to bottom starting with the topmost endpoint
                            if (w.endpoint1.GetX() < w.endpoint2.GetX())
                            {
                                DrawObjectWithTransform(e, w, w.endpoint1.GetX() + i * 50, w.endpoint1.GetY(), 0, WallDrawer);
                            }
                            else
                            {
                                DrawObjectWithTransform(e, w, w.endpoint2.GetX() + i * 50, w.endpoint2.GetY(), 0, WallDrawer);
                            }
                        }
                    }
                }

                // Draw the projectiles
                foreach (Projectile proj in theWorld.GetProjectiles())
                {
                    DrawObjectWithTransform(e, proj, proj.location.GetX(), proj.location.GetY(), proj.orientation.ToAngle(), ProjectileDrawer);
                }
            }

            // Draw beam animations
            lock (beamFrames)
            {
                foreach (Beam b in beamFrames.Keys.ToList<Beam>())
                {
                    if (beamFrames.ContainsKey(b))
                    {
                        // Increase the frame count of the current beam
                        beamFrames[b]++;

                        // If the beam's animation has finished, store it to be removed later
                        if (beamFrames[b] > maxBeamFrames)
                        {
                            finishedBeams.Push(b);
                        }

                        DrawObjectWithTransform(e, b, b.location.GetX(), b.location.GetY(), b.orientation.ToAngle(), BeamDrawer);
                    }
                }
            }

            // Remove beams that have finished their animations
            for (int i = 0; i < finishedBeams.Count; i++)
            {
                lock (beamFrames)
                {
                    beamFrames.Remove(finishedBeams.Pop());
                }
            }

            // Draw tank explosion animations
            lock (tankExplosionFrames)
            {
                foreach (Tank t in tankExplosionFrames.Keys.ToList<Tank>())
                {
                    if (tankExplosionFrames.ContainsKey(t))
                    {
                        // Increase the frame count of the current explosion
                        tankExplosionFrames[t]++;

                        // If the explosion's animation has finished, store it to be removed later
                        if (tankExplosionFrames[t] > maxExplosionFrames)
                        {
                            finishedExplosions.Push(t);
                        }

                        // Draw the explosion using 8 explosion pieces, spreading out in the cardinal directions
                        DrawObjectWithTransform(e, t, t.location.GetX(), t.location.GetY() - tankExplosionFrames[t] * 5, 0, ExplosionPieceDrawer);
                        DrawObjectWithTransform(e, t, t.location.GetX() + tankExplosionFrames[t] * 4, t.location.GetY() - tankExplosionFrames[t] * 4, 45, ExplosionPieceDrawer);
                        DrawObjectWithTransform(e, t, t.location.GetX() + tankExplosionFrames[t] * 5, t.location.GetY(), 90, ExplosionPieceDrawer);
                        DrawObjectWithTransform(e, t, t.location.GetX() + tankExplosionFrames[t] * 4, t.location.GetY() + tankExplosionFrames[t] * 4, 135, ExplosionPieceDrawer);
                        DrawObjectWithTransform(e, t, t.location.GetX(), t.location.GetY() + tankExplosionFrames[t] * 5, 180, ExplosionPieceDrawer);
                        DrawObjectWithTransform(e, t, t.location.GetX() - tankExplosionFrames[t] * 4, t.location.GetY() + tankExplosionFrames[t] * 4, 225, ExplosionPieceDrawer);
                        DrawObjectWithTransform(e, t, t.location.GetX() - tankExplosionFrames[t] * 5, t.location.GetY(), 270, ExplosionPieceDrawer);
                        DrawObjectWithTransform(e, t, t.location.GetX() - tankExplosionFrames[t] * 4, t.location.GetY() - tankExplosionFrames[t] * 4, 315, ExplosionPieceDrawer);
                    }
                }
            }

            // Remove explosions that have finished their animations
            for (int i = 0; i < finishedExplosions.Count; i++)
            {
                lock (tankExplosionFrames)
                {
                    tankExplosionFrames.Remove(finishedExplosions.Pop());
                }
            }

            // Do anything that Panel (from which we inherit) needs to do
            base.OnPaint(e);
        }

    }
}
