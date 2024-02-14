# TShock Modified
What is this? As of right now it is a fork of TShock which features a complete database provider switch. We use MongoDB for local server and global server storage. This is the only major change at the moment, apart from minor changes here and there.

> [!WARNING]
> Vanilla TShock plugins WILL NOT WORK with this fork!
 
> [!IMPORTANT]
> A MongoDB connection is REQUIRED for this to run! Configure in `data/config.json`

## Notable changes:
- TSPlayer has been renamed **ServerPlayer**
- TShock main class is now **ServerBase**
- Accessing DB storage can be done from `ServerBase.GlobalDatabase` or `ServerBase.LocalDatabase`. These are both IMongoDatabase objects.
- ServerPlayer has been renamed to **ServerConsolePlayer**
- Ban system has been redone
- Most commands have been ripped out

## Why?
We are a group of developers that are working on creating a new Terraria server network, and we will be using this fork of TShock.
