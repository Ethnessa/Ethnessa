# Ethnessa API
What is this? As of right now it is a fork of TShock which features a complete database provider switch. We use MongoDB for local server and global server storage. This is the only major change at the moment, apart from minor changes here and there.

> [!WARNING]
> Vanilla TShock plugins WILL NOT WORK with this fork!
 
> [!IMPORTANT]
> A MongoDB connection is REQUIRED for this to run! Configure in `data/config.json`

## How do I get MongoDB setup?
Getting MongoDB installed and configured to run with your server is easy! 

1. Download & Install [MongoDB](https://www.mongodb.com/try/download/community)
2. If you are running it locally, you don't have to do anything else! Otherwise update `MongoConnectionString` in `data/config.json`.
3. (Recommended, but optional) Setup authentication on your MongoDB!

## Notable changes:
- TSPlayer has been renamed **ServerPlayer**
- TShock main class is now **ServerBase**
- Accessing DB storage can be done from `ServerBase.GlobalDatabase` or `ServerBase.LocalDatabase`. These are both IMongoDatabase objects.
- ServerPlayer has been renamed to **ServerConsolePlayer**
- UserAccounts can now have **multiple groups, account-independent permissions, weighted and user-preferred prefixes**
- **Default groups re-worked:** only three groups are created and are not enforced, except for "default" and "guest" (which can be configured if you'd like to change the names). One un-enforced group is created called "admin", this group has the `*` flag, which means it has all permissions. This group can be removed. We have removed the enforcement of a super-admin group in place of forcing the console to have a user account with all permissions added by default. This means you can also change the permissions the server console itself has, which isn't really recommended, but you can. 
- Ban system has been redone
- Most commands have been ripped out

## Why?
We are a group of developers that are working on creating a new Terraria server network, and we will be using this fork of TShock.
