# Ethnessa API
What is this? As of right now it is a fork of TShock which features a complete database provider switch. We use MongoDB for local server and global server storage. Along with this, we have re-done a few of the administration systems.

> [!CAUTION]
> EthnessaAPI is not in any way finished, and is expected to have bugs. Please keep this in mind. 

> [!WARNING]
> Vanilla TShock plugins WILL NOT WORK with this fork! However -- they are not hard to port over!
 
> [!IMPORTANT]
> A MongoDB connection is REQUIRED for this to run! Configure in `data/config.json`

## Why?
We are a group of developers that are working on creating a new Terraria server network, and we will be using this fork of TShock.

## For personal use: How do I get MongoDB setup?
Getting MongoDB installed and configured to run with your server is easy! 

1. Download & Install [MongoDB](https://www.mongodb.com/try/download/community)
2. If you are running it locally, you don't have to do anything else! Otherwise update `MongoConnectionString` in `data/config.json`.
3. (Recommended, but optional) Setup authentication on your MongoDB!

## Notable changes:

### Refactoring
 Some pretty major refactoring has taken place in Ethnessa. Our aim is to make the codebase more consistent, and easy to maintain. As of right now, a lot of the code still uses TShock's original code, but throughout the development cycle of this project we aim to be pretty distinctive from TShock. Here are some important things to note as far as refactoring: 
- TSPlayer has been renamed **ServerPlayer**
  - ServerPlayer has been renamed to **ServerConsolePlayer**
- TShock main class is now **ServerBase**
- TShockAPI namespace is now **EthnessaAPI**

---

### Database Provider Swap
 As stated earlier, we have switched from using SQLite and MySQL to MongoDB for concurrent data storage. This means there are some changes to note:
  - Accessing DB storage can be done from `ServerBase.GlobalDatabase` or `ServerBase.LocalDatabase`. These are both IMongoDatabase objects.
  - All database managers previously were objects, now they are interfaced through a static class.

---

### UserAccounts, groups, and permissions re-done
Ethnessa has remodeled the permissions systems and user accounts to further align with more familiar permission systems, such as PermissionsEx from Minecraft.   
- UserAccounts can now have **multiple groups!**
  - They can also have account-independent permissions.
        - The server console now has an account within the database, with no group, but with the `*` permission flag in it's **User Permissions**. This means it still has access to all permission.
   - Groups are now weighted, for prefix assignment
     - Prefixes can also be chosen by the user in-game     
- **Default groups re-worked:**
   -  Only three default groups are created
   -  The only enforced groups are a default, and guest group. Both of which can be renamed.
   -  SuperAdmin and all staff groups have been replaced with `admin`, which is unenforced (can be deleted) and is not as restrictive as the super-admin group in vanilla TShock. **NOTE:** `admin` has the `*` permissions flag, meaning it has access to all permissions.

---

### Quality of Life improvements
- **Introducing.. tags!** Tags are prefixes that can be appended to user's chat messages alongside the user's group prefix. As many can be toggled on or off. Why would you want this? Let's say you want staff members to have a "[Staff]" tag while also retaining their other group tags, or maybe a "[Donor]" tag.
- Ban & mute system has been redone to be more understandable and easy to manage and use.
- **Chat filtering!** You can now filter the chat for messages you don't want players saying. Blacklisted words will be censored as '***'
- **Nicknames!** Players can now change their name on the server to their preferred nickname, differing from their account name / character name.

---
 
### Other notes
- Most commands have been ripped out but we are slowly working on reimplementing most useful and essential commands.
