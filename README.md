### Usage 

The NextSequencerProvider object accepts the following parameters

- **capacity** the maxiumum number of values
- **loadChannelAction** the action to load items into the channel

If you execute the app you will see that every second an attempt will be made to read an item from the channel.

### Without Low Water Mark 
If you execute without a low water mark the code will pause every 10 items.

### With Low Water Mark 
If you execute with a low water mark the pause will no longer occur.
