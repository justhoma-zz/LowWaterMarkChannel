## Description
The **NextSequenceProvider** utilizes a bounded Channel to store a list of ints and exposes a *GetAsync* method for retrieving these in a thread-safe manner.
It supports the concept of a low water mark, meaning that when this is hit the channel will be refilled, potentially without blocking.

It accepts the following parameters

- **capacity** the maxiumum number of values
- **lowWaterMark** the low water mark
- **loadChannelAction** the action to load items into the channel

### Usage 

The test app will spin up 5 concurrent tasks every second to read an item from the channel.

### Without Low Water Mark 
If you execute without a low water mark the code will pause every 20 items while the code waits for the *loadChannelAction* task to complete.

### With Low Water Mark 
If you execute with a low water mark of 15 the pause will no longer occur.

### Notes
This does not guarantee that there will never be any blocking. 
For example blocking will occur if:
- The items are being read faster than the time it takes to load new ones.


