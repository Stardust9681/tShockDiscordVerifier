## Plugin Config
| Table of Contents       |
| ----------------------- |
| [Auth Length]           |
| [Ban Discord ID]        |
| [Ban tShock -> Discord] |
| [Verified Group]        |
| [Unverified Group]      |
| [Discord Invites]       |
| [Examples]              |

### Auth Length
> Label: `"Auth Length"` (`int32` default: `12`)

Allows you to adjust the length of the authentication strings generated.

**NOTE**: Doesn't yet reflect the actual length of the string, due to base64 conversion.

### Ban Discord ID
> Label: `"Ban Related Accounts"` (`boolean` default: `false`)

This plugin allows a user to verify multiple tShock accounts on the same Discord ID. Enabling this setting will ban accounts that share that ID, alongside the same flags and duration used for the ban.

### Ban tShock -> Discord
> Label: `"Discord Ban on TShock Ban` (`boolean` default: `false`)

Enabling this setting, provided that you've given the Discord bot permission through the developer portal, will ban users from the associated Discord server when their verified/linked account is banned from the tShock server.

### Verified Group
> Label: `"Verified Group"` (`string` default: `""`)

Sets the name of the group to act as your "verified" group. Required to allow proper verification.

### Unverified Group
> Label: `"Unverified Group"` (`string` default: `""`)

Sets the name of the group to act as your "unverified" group. Required to allow proper verification.
tShock doesn't have any form of role hierarchy, there's no flag that can easily be set, nor role that can be added or removed at will, to allow additional permissions from this. Hence, we need an unverified group to return users to.

### Discord Invites
> Label: `"Allow Discord Invite"` (`boolean` default: `true`)

Enabling this will allow the `/invite` command to be run ingame, which reports the largest (most-used) invite for your Discord server

### Examples

#### Figure 1
Sets auth length to 12, allows related bans, but not Discord ban from tShock. The verified group has name, `"verified"` and unverified, `"default"`. The `/invite` command can return a Discord invite if one exists.
```
"Settings" : {
    "Auth Length" : 12,
    "Ban Related Accounts" : true,
    "Discord Ban On TShock Ban" : false,
    "Verified Group" : "verified",
    "Unverified Group" : "default",
    "Allow Discord Invite" : true
}
```