Authors: Kevin Xue & Griffin Zody
Fall 2021, CS 3500

DISCLAIMER: 
	Beams do not fully work with the provided view, however the view created in PS8 is fully functional with this server 
	implementation! Beams are not fully functional because sometimes the beam commands are not registered (when using provided
	view).

Basic Description:
	This program represents our unique take on the client and server for TankWars. It has fully functional GUI, user input (both
	mouse and keyboard), and networking (for both the server and client side). The server and the client can be launched separately
	by either launching the server or the view class. Utilizes multi-threading to draw the game. 

Major Design Decisions in Each Important Class:
	Game Controller:
		-To handle movement, we chose to store the currently pressed keys in a list and set the current movement command
		to the most recently pressed key at the back of the list.
		-For the client commands to the server, we chose to make a single command a variable within the class
		because we did not want to keep creating new commands when sending different instructions to the server. Instead,
		we modify our one existing command and re-send it every frame.
		-We chose to use events to inform the view of networking errors because the view needs to display an error message
		and the controller/model need to clear any deprecated information from a failed or previous connection.
		-We chose to also use events for beams and tank explosions because they are only sent on a single frame from 
		the server (beam objects only exist on one frame, and tanks are only set to be dead for one frame) and so they
		do not belong in the model as more permanent objects, but rather should function more like single-fire events.
		-To handle the first two parts of the handshake, two boolean variables track whether the player's ID and the world
		size have been sent. Once both have been sent, a "worldLoaded" event is fired to inform the view that it can begin
		drawing without fear of missing any critical information (basically to avoid any null pointers).

	World:
		-To handle the event of tank death, tanks are removed from the model temporarily when their HP hits 0, rather than
		when "died" is set to true. This is because it makes more sense to re-add the tanks to the model when their HP is
		reset (aka: they have respawned) than to wait some set amount of time. 
		-The model is passed the player's unique ID when the handshake occurs so that the model can keep track of which
		tank belongs to this client's player. This is used to maintain the camera focus when the player dies.
		-The world has an reset function that clears the dictionaries and resets any other member variables for when
		an error state occurs and any deprecated or bad data needs to be removed.
		-The addition and removal of objects to the model is handled in a single function, so that the controller does
		not need to worry about when to call remove or add, that work is done by the model.

	Form:
		-The most notable decision in the form is the decision to hide the drawing panel when an error state is hit. This
		means that the drawing panel will not be visible after an error message is shown, and will only be re-enabled
		when a successful handshake occurs.

	Drawing Panel:
		-To handle the space for images, we chose to pre-load them into dictionaries or standalong Image objects in the
		constructor because the game is highly likely to use all of these images, and pre-loading them saves a lot of 
		overhead.
		-We use dictionaries to keep track of the frames of each beam and tank explosion animation, along with the
		associated objects themselves as the keys. It made sense to us to associate each animation with the object
		it originated from.
		-We use stacks to keep track of finished animations that need to be removed, because they are a constant time
		removal data structure (assuming they are implemented somewhat optimally), and we needed to be able to remove
		animations from the dictionaries outside of the area where we enumerate through and draw each animation.
		-We choose which player color of tank, turret, and projectile to draw based on the tank ID of each object, 
		modulo 8. Each of the possible outcomes of modulo 8 (0-7) are given a constant int that represents the associated
		color.
		-The beam animation works by drawing a straight line to the edge of the world, and progressively making the size
		of the animation's rectangle smaller with each frame. The explosion animation works by increasing the x and y of
		each of the 8 pieces of the animation in one of the cardinal directions until it has ended.
		-The walls are drawn by calculating the number of walls between two endpoints (it is always a multiple of 50) and
		then determining which endpoint is closer to the origin and drawing walls until the second endpoint is reached.

	Server:
		-Chose to represent each unqiue client's ID with the socket state's provided ID.
		-Gave each player a beam cooldown so that players cannot fire beams too quickly when multiple are used.
		-Chose to allow player to retain beam powerups on death
		-Created dictionaries containing client commands, projectile cooldowns, death timers, powerups, and beam cooldowns to
		 represent each unqiue client's individual status within the game.
		-The server detects disconnections by seeing if any of the send operations fail.
		-Chose to use a frame counter to disconnect clients safely and properly. When a disconnection is detected, set the frame
		 counter to 1. This means that the client is flagged for removal (on the server's dictionary containing clients). We then 
		 send that the disconnected player's tank has died and disconnected. Finally, on the next frame, we set the frame 
		 counter to 2 and remove all data pertaining to the disconnected client.
		-We chose to allow users to change all of the game settings through the provided XML file.
		-We handled collisions using rectangular collisions except for beams (which uses circular collisions).
		-The server ignores malformed client data, and all but the first command sent on a frame (unless another command contains a
		 fire beam operation).
		

Changelog:

	11/5/21: 
		-Set up skeleton for the project
		-Resolved vector 2d build errors
		-Added JSON objects
		-Started model design

	11/19/21
		-Set up the network protocol (handshake)
		-Started receiving from server

	11/20/21
		-Added basic view logic
		-Bug fixed a server start connection error
		-Removed extra network controller DLLs and changed our design to use a copy of our PS7 
		-Updated connections between the controller and the view

	11/22/21
		-Added tanks and powerups to the view (drawers)
		-Resolved a few controller bugs
		-Resolved some issues in the view
		-Added more fields to the model for information the view would need

	11/23/21
		-Added object removal to the model
		-Centered view properly on player tank
		-Added movement commands, fixed a bug with player view tracking on death
		-Fixed wall drawing logic where one wall would be missing when overlapping
		-Added non-functional version of mouse tracking

	11/24/21
		-Fixed the background not being drawn and updated properly
		-Fixed mouse tracking to accurately follow mouse

	11/25/21
		-Removed sends to server that were inbetween frames
		-Resolved laggy movement

	11/26/21
		-Added tank gui elements to view
		-Added working version of beams, but beam collection still has issues
		-Added projectile firing
		-Added death and disconnect logic for tanks
	
	11/27/21
		-Removed more extra send to servers
		-Found death bug on camera where the view centered on the most recently killed tank during respawn

	11/28/21
		-Fixed death bug
		-Fixed a small issue in PS7
		-Added basic explosions
		-Fixed bug where projectiles would not be removed if fired from inside another tank's turret 
		-Fixed beam removal logic and added locks to animations

	11/29/21
		-Optimized the amount of locking occurring for the view animations
		-Cleaned up and refactored code
		-Added comments

	12/4/21
		-Added skeleton for server class.
		-Setup basic handshake between user and server.
		-Got the first frame to be drawn.
		-Decided that the server should be a single class instead of a view and controller

	12/5/21
		-Added tank movement processes
		-Added basic XML reading
		-Added firing functionality

	12/6/21
		-Added collisions for tank and walls
		-Added collisions for projectiles and walls
		-Added collisions for projectiles and tanks


	12/7/21
		-Added respawn logic
		-Debugged handshake between server and client
		-Debugged random starting location logic
		-Added wraparound
		-Added powerup spawning logic
		-Added malfunctional disconnection logic

	12/9/21
		-Added (failing) beam logic
		-Added tank scoring logic
		-Added beam and tank collision
		-Fixed disapearing projectiles 
		
	12/10/21
		-Finished disconnect logic
		-Updated XML setting reader to include all settings
		-Fixed an issue where the AI were moving the player (if the player did not input a movement)
		-Added socket closing (to disconnect)
		-Commented server class and updated README

