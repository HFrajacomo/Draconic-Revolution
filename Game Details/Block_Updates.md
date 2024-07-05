# Block Updates

The huge universe of manual and automatic changes to a World is to be explained right here. Buckle up!

## Update Messages

If you didn't know yet, a *Block Update (BU)* is an event a block can receive that gives it some information about what to do next. Also, *Block Update Detection (BUD)* is a common term used to name the capture of BU events.

A Water block will be able to flow if there are no blocks around, and the newly made block must be able to flow again and again. A Torch is meant to break if the block that is supporting it is destroyed. Also, a Water block will instantly try to move once a chunk is loaded. Right now, there are 3 types of Block Update Messages currently:

 - **Change**: Sent to a block when one of it's neighbors is converted into a non-air block or has it's state changed
 - **Break**: Sent to a block when one of it's neighbors is broken into an air block.
 - **Load**: Sent to Load Specific blocks whenever the chunk it's in is loaded.

## The Recursion Problem

Okay, so now that every block has it's BU behaviour, all we have to do, is trigger lots of them and see the World change instantly, right? **Wrong!** Seriously, do you really think that updating thousands of Water blocks every frame is smart? That's the Recursion Problem. If we were to design the Block Update System as a big recursive code-show, we would be f\*cked whenever someone attempted to run a few thousands of those. The game would freeze, and, in the best-case scenario, would lose a bunch of BUD information after sitting still for a couple of seconds. In the worst-case scenario, it would crash due to *StackOverflow*.


## The Determinism Problem

Let's say we try to mitigate the *Recursion Problem* by adding a limit of BUs that the Scheduler can process per frame (let's say 30). Even if a million BUD happens in the same frame, only 30 will be processed and the other ones will be passed to the next frame and so on. This would save some FPS, but would lead to inconsistencies in the game. 

For example: if multiple water blocks are updated at once, only a few water chunks would move, while others would wait to flow. This could lead to Block Updates happening considerably too late and with inconsistent timing. So if water normally takes 10 frames to flow from one block to another, with this scenario, depending on the amount of BUs received, every specific Water block could have a completely different flow time, and that is unaceptable too. 

## The BUD-Scheduler

This class is made out of a dictionary that links In-Game time to a list of BUD requests. In that way, a block can literally *schedule* a BUD to happen in a later date, depending on Game Time. A few examples of pros of the Scheduler system are:

 1. BUs can be scheduled to happen at a later time. For example, Water can flow one block of distance every *X* frames.
 2. Runs all game logic in a deterministic way with exact timings
 3. 

It's important to note that our system is FPS-safe instead of BUD-safe. What does that mean though?

## FPS-Safe vs BUD-Safe System

Imagine that a million BUDs are scheduled to Frame #1. In a BUD-Safe system, the game would iterate through all the million BUDs and solve them individually inside the Frame #1's time period. Of course, processing a huge amount of BUD in such a short time is impossible, thus, the game will get lag spikes. It will slow down user experience to give the game time to solve all the BUDs. In a FPS-Safe system, the BUDScheduler has a limited amount of BUD it can process every frame. If this cap is surpassed, then all the remaining BUDs are passed on to be processed first thing in the next frame. It saves our FPS, but slows down the game logic.

### BUD-Safe
**Pros**

 - Will run all game logic in a predictable in-game time frame
 - Won't bleed BUDs into new frames

**Cons**

 - Will lower the FPS or straight up freeze the game while under heavy BUD activity

### FPS-Safe
**Pros**

 - Will maintain FPS static under all BUD circunstances
 - Will add the possibility of having it's maximum cap increased by server owners

**Cons**

 - Will disrupt the prediction of in-game time in regards to game logic during heavy BUD activity

**In short, the approach we've taken in this game, is the BUD-Safe System! (But we will add the option of toggling FPS-Safe Scheduling for Server owners in the future)**

# Congratulations

Now you know a little bit more about Block Event Management and what was used in order to make your world interact with itself!