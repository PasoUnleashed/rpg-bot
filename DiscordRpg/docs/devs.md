# Interactions

Interaction defention is done through a combo of specially formatted files and javascripts
the general idea is that content creators describe an npc/item behaviour through simple scripts and text files

## The IDF

The interaction definition format is used to define an interaction tree, an interaction tree can have the following node types
* Conversation Node
	* a node that once is reached in the interaction tree will prompt the player to choose a dialogue option, each leading to another interaction node.
* Script Node
	* a script node will run a script to evaluate what is the action to be done and the text to be sent to the player, script nodes can store player-specific states and general states
* Reward Node
	* a reward node grants the player an item
* Text Node
	* a text node sends a text to the player upon being reached in the interaction list
* End Node
	* an end node ends the interaction

Node types to come:

* Conditional Conversation
	* A conversation that has conditional dialouge options, such as dialogue options that check the player's state or the guild's state or the player's inventory.
* Spawn NPC Node
	* A node that once reached will spawn an npc in the same guild as the interacting npc.
	
### IDF and Script Formatting

Text defined in an idf or script file that is to be displayed to the player can have the following formatters

|Tag| Description|
|-|-|
|<player\>| will be replaced by the interacting player's name|
|<npc\> | will be replaced by the npc's name|

### IDF syntax

Each tree to be defined needs a type and a unique id and arguments matching the arguments required by the node type
<br>

``` 
<node type>:<id>: <argument 1:argument 2:...:argument N> 
```
full tree example:

`001.idf`
```
C:0: *You approach <npc>*: Greetings, who are you?:1: Why are you here?:2:Goodbye!:E:Enter "The Script":5
T:1: Hi! I am <npc>:0
C:2: I am going around this realm gathering apples: Can I have one?:3:Why the fuck?:6
C:3:Yes Of course! *he reaches out to give you the apple*:Take apple from <npc>:4
R:4: "And remember, an apple a day keeps the goblins at bay!" he says as he walks away:0:1:-1
S:5:0
T:6:*awkward silence*:-1
```


| Node type| letter | arguments|
|-|-|-|
|Conversation| C | Entry text then each conversation option followed by the next node id|
|Script| S| the script id|
|Reward| R| entry text followed by the rewarded item id followed by the quantity followed by the next node id|
|Text Node | T|the text to be sent,next node |
|End Node| E| none|

you do not need to define an end node for each interaction, any "next node id" can be passed the argument `E` to signify an end node follows
 
### IDF Scripts

While an interaction instance is at a Script Node most server and game events are directed to an instance of a javascript interpreter that runs the script specified by the node,
scripts can save a "state" object at the end of each execution, this state object is shared by all instances of this script, although changes done to it are not reflected in any already running scripts and not until the modifying script is done executing

scripts need to have implementations of the the following methods:

| Name | Args| Description| Returns|
|-|-|-|
|onInteractMessage| `text` (the body of the message)| a message was received from the player interacting with the script currently| {message,next}| 
|onOthersMessage| `text` <br> `id` (id of sender) | a message was received by another player | {message,next}|
|otherPlayerInteract| `id` | another player has attempted interacting with this npc| {message,next}|
|onInteractionTimeout|n/a| the player has left or not responded in a while | {message}|
|onCreate|  | called when the node is first reached | {message} |
|getEntry| | get the entry text for the node | 
### script environment

The script env provides access to the following functions and objects
<br>
Methods: 
<br>

| Name| Args| Returns|
|-|-|-|
|getPlayerName| id| the current name of the player in his main guild|

Objects:

|Name| Properties|Description|
|-|-|-|
|NPC| `id`,`name`,`state` - a state unique to this npc instance |the npc instance currently invoking the script|
|State|n/a| A state available only throughout a single interaction |
|Guild| `id`,`name`,`players`|the current guild this npc script is running on|
|Player|`id`,`name`,`inventory`,`effects`,`location` |the player currently interacting with the script|

# Effects

Effects are instant or lasting modifiers applied to players, effects subscribe to all a player's events and act as middleware, modifying any subsequent effects applied to the player.

* Effects can be applied instantly, such as damage or healing
* Effects can be applied for a duration, such as a gibberish debuff
* Effects can be applied conditionally, such as possessing/equipping an item

## Effect Types

* Damage - Deals a set amount of damage to a player or npc
* Healing - heals a target for preset health
* Entry Sound - play an mp3 when the player joins a voice channel
* Script - a script that is subscribed to many of the guild events for heavily customizable effects

## EDF
The edf is the effect definition format. it's simmilar to the IDF (interaction defention format).
an edf file can define multiple effect types in a single effect.

to define an effect type a specifier is used

|Type | Specifier | Arguments |
|-|-|-|
|Damage| D | target (S for self, T for targets ), amount|
|Healing| H |target (S for self, T for targets ), amount|
|Entry Sound| E|filename|
|Script | S | scriptname|

example effect `001.edf`
```
0:D:T:20
1:E:rawr.mp3
```

### EDF Scripts

Edf script will be notified with the following events

| Name | Args| Description| Returns|
|-|-|-|
|onEffectApplied| n/a| the effect has been applied | {message,isExpired} |
|onEffectRemoved| | An attempt has been made to remove the effect| {isAllowed}| 
|onGuildMessage| `text` <br> `id` (id of sender) | a message was sent to the guild ||
|onaffectedMessage| `id` | the affected player has sent a message| |
|onEffectLoaded|n/a| the effect has been loaded after a server restart ||
|onEffectUnloading|  | the server is shutting down |  |
|onaffectedReaction|  | the affected player has reacted to a message||
|onReactionToaffected|| a player has reacted to the affected player's message||
|onaffectedJoinVoiceChannel||the affected player has joined a voice channel||
|onaffectedVoiceChannelJoined||the channel the affected player is in has been joined||
|onaffectedChangeLocation||The effected has moved to another text channled||

# Items

Items are stored in the item list csv file. what is stored about them in that file is

* name
* id
* icon (discord emote) *not implemented
* cooldown
* minimum targets
* maximum targets

When a player receives an instance of an item an entry of it is created in his inventory
containing the following data

* instance id
* last used
* state

# Script environment api
A set of functions available inside all script environments


# TO BE IMPLEMENTED :>
Not enough use casses to compile functional requirements.
