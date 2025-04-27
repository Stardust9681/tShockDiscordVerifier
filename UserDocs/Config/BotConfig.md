## Discord Bot Configuration
Controls interactions with the bot, including most of the verification process.

| Table of Contents           |
|-----------------------------|
| [Bot Token]                 |
| [Ban Discord -> tShock]     |
| [Verification Requirements] |
| [Examples]                  |

### Bot Token
> Label: `"Discord Bot Token"` (`string` default `""`)

You will need to enter you Discord bot token for this. The token is generated once when you create the bot through Discord's "[Developer Portal](<https://discord.com/developers/applications>)," and may be reset at any time (note you will have to enter the new token here if you do this).
See [Creating A Discord Bot] for more details

### Ban Discord -> tShock
> Label: `"tShock Ban On Discord Ban"` (`boolean` default `true`)

If enabled, when a user is banned on Discord, the plugin will attempt to also ban an associated account through tShock if one exists.

### Verification Requirements
> Label: `"Verification Requirements"` (`Array` default `[]`)

Allows you to set verification requirements for users, including:
- Has (read) access to channel
- Does NOT have (read) access to channel
- Has role
- Does NOT have role

See Figure 2 below for an example of this in use.
Prefix types with, `Not` to invert condition (eg, `HasRole`->`NotHasRole`)



### Examples
Note that no valid bot tokens are provided, and these are only examples of how one might expect the config to appear.

#### Figure 1

Bot with Token, `AAaaAAaa`, will not ban from tShock server if banned from Discord, no additional verification requirements
```
"Settings" : {
    "Bot Token" : "AAaaAAaa",
    "tShock Ban On Discord Ban" : false,
    "Verification Requirements" : []
}
```

#### Figure 2
Bot with Token, `BBbbBBbb`, will ban from tShock server if banned from Discord, must be in channel with ID, `CCccCCcc`, and have role with ID, `DDddDDdd`
```
"Settings" : {
    "Bot Token" : "BBbbBBbb",
    "tShock Ban On Discord Ban" : true,
    "Verification Requirements" : [
        {
            "Requirement Type" : "InChannel",
            "ID" : "CCccCCcc"
        },
        {
            "Requirement Type" : "HasRole",
            "ID" : "DDddDDdd"
        }
    ]
```