# Channels and Rooms

# Players

## Properties

* Health
* Max Health
* Main Guild
* Entry Sound Item
* Gold
* Inventory
* Active Effects



# Chapters and the story


# Database

Player and inventory information is stored contemporarly in memory and an sqlite3 database. the database is meant to be a means to load information and not for live storage. and thus shall be updated between many frames.

## Schema

Tables:

* Players
* Guilds
* Rooms
* Item Instances
* Effect Instances
* NPC Instances


### Players

Columns: 

| Name | Type | Description |
|-|-|-|
|id| ulong | the discord id of the user|
|health| int| the current health of the player|
|location| ulong | the channel id |
|mainguild|ulong | the guild id|

### Guilds

Columns:

| Name | Type | Description |
|-|-|-|
|id| ulong | The guild id|
|state| text| a script global state|

### Rooms

Columns:

| Name | Type | Description |
|-|-|-|
|id|ulong|The channel id|

### Item Instances 

Columns:

| Name | Type | Description | 
|-|-|-|
|id | ulong | the instance id|
|itemid| int | the base item id|
|state | text| the state of the item |
|ownerid|ulong | the discord id of the owning player|

### Effect Instances

Columns:

|Name|Type|Description|
|-|-|-|
|id|ulong|the id of the effect instance|
|effectid| int| the id of the effect|
|effecttypei| int | the index of the item effect |
|state| text | the effect state|
|applied | date(text) | the time the effect was applied on |

### NPC Instances

Columns:

|Name| Type| Description|
|-|-|-|
|id| ulong| the npc instance id|
|npcid| int| the npc id|
|location | ulong | the location id| 
|health | int | the health of the npc|
|state| text| the npc's current script-wide state|

