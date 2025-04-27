## Creating A Discord Bot
This plugin utilises and logs in as an authorised Discord bot.
You, the person who wants to use this plugin, will have to create one through Discord's "[Developer Portal](<https://discord.com/developers/applications>)"

Once you have navigated to the portal, clicking on the button in the top right ("New Application") will create a new bot. You get to choose what name and image you assign it, it doesn't matter for the plugin.

Once you have done that, you should be in the "Bot" tab (on the left hand side). Scrolling down should reveal an entry for a "Token."\
**IMPORTANT, DO NOT SHARE THIS TOKEN WITH ANYONE**.\
It's recommended you keep this token safe somewhere. This token is what allows people and bots alike, to sign in to that account. This plugin requires that token, however, to operate your bot, to allow people to verify. So, go ahead and copy that, and (once this plugin has been loaded for the first time) paste it into `discord_config.json` (read more about config at [UserDocs/Config/README.md]).

After *creating* the bot, you'll have to assign it some permissions through the developer portal. Thankfully, we don't need very many, you'll just want to go through the portal.\
Navigate to the "Bot" tab if you aren't already there, and enable `SERVER MEMBERS INTENT` (also reads: `GUILD_MEMBERS`). This will allow us to know that users are actually in the server, as well as what roles or channels they have.\
Save that, then flip to the tab that says, "OAuth2," where you should find a table of very convenient checkboxes. Tick `applications.commands`, and `bot`, and open the provided URL to add the bot to your server. If you want to allow this bot to ban members (toggleable through its config), you'll want to also enable the `Ban Members` permission before adding this bot to your server.

As a final precaution, you don't want users adding your bot without your knowing. Page over to the "Installation" tab, and change the install link to `None`. Save, then return to "Bot" tab, and *disable* public bot. Save.

And there you have it, you *should* (in theory) have a functioning bot!