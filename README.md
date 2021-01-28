### Description

How to use a Channel with a low-water mark.

### Usage 

The NextSequencerProvider object accepts the followin paramaters

- **fetchMax** The maxiumum number of values to queue up
- **fetchMin** The low water mark

 
If you execute the console app you will see that every second an attempt will be made to read an item off of the channel.
