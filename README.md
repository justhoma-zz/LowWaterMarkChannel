### Usage 

The NextSequencerProvider object accepts the following parameters

- **capacity** the maxiumum number of values
- **loadChannelAction** the action to load items into the channel

If you execute the app you will see that every second 5 concurrent tasks will be spun up to read an item from the channel.

### Without Low Water Mark 
If you execute without a low water mark the code will pause every 20 items while the code waits for the *loadChannelAction* task to complete.

### With Low Water Mark 
If you execute with a low water mark the pause will no longer occur.


