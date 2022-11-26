<h1 align="center">PasteMystBot</h1>
<p align="center"><img src="icon.png" width="128"></p>
<p align="center"><i>A Discord bot handling PasteMyst integration.</i></p>
<p align="center">
<a href="https://github.com/BrackeysBot/PasteMystBot/releases"><img src="https://img.shields.io/github/v/release/BrackeysBot/PasteMystBot?include_prereleases"></a>
<a href="https://github.com/BrackeysBot/PasteMystBot/actions?query=workflow%3A%22.NET%22"><img src="https://img.shields.io/github/workflow/status/BrackeysBot/PasteMystBot/.NET" alt="GitHub Workflow Status" title="GitHub Workflow Status"></a>
<a href="https://github.com/BrackeysBot/PasteMystBot/issues"><img src="https://img.shields.io/github/issues/BrackeysBot/PasteMystBot" alt="GitHub Issues" title="GitHub Issues"></a>
<a href="https://github.com/BrackeysBot/PasteMystBot/blob/main/LICENSE.md"><img src="https://img.shields.io/github/license/BrackeysBot/PasteMystBot" alt="MIT License" title="MIT License"></a>
</p>

## About
PasteMystBot is a Discord bot which will automatically detect codeblocks and file attachments, and upload them to https://paste.myst.rs/

## Installing and configuring PasteMystBot 
PasteMystBot runs in a Docker container, and there is a [docker-compose.yaml](docker-compose.yaml) file which simplifies this process.

### Clone the repository
To start off, clone the repository into your desired directory:
```bash
git clone https://github.com/BrackeysBot/PasteMystBot.git
```
Step into the PasteMystBot directory using `cd PasteMystBot`, and continue with the steps below.

### Setting things up
The bot's token is passed to the container using the `DISCORD_TOKEN` environment variable. Create a file named `.env`, and add the following line:
```
DISCORD_TOKEN=your_token_here
```

Two directories are required to exist for Docker compose to mount as container volumes, `data` and `logs`:
```bash
mkdir data
mkdir logs
```
Copy the example `config.example.json` to `data/config.json`, and assign the necessary config keys. Below is breakdown of the config.json layout:
```json
{
  "GUILD_ID": {
    "countThreshold": /* The codeblock count threshold for pasting a codeblock to PasteMyst */,
    "lineThreshold": /* The line threshold for pasting a codeblock to PasteMyst */,
    "pasteAttachments": /* Whether to post text attachments to PasteMyst. Defaults to true */,
    "ignoredChannels": /* An array of channel IDs that are exempt from auto-pasting */,
    "autoPasteIfText": /* Whether or not to auto-paste messages if they contain text other than codeblocks. Defaults to false */
  }
}
```
The `logs` directory is used to store logs in a format similar to that of a Minecraft server. `latest.log` will contain the log for the current day and current execution. All past logs are archived.

The `data` directory is used to store persistent state of the bot, such as config values and the infraction database.

### Launch PasteMystBot
To launch PasteMystBot, simply run the following commands:
```bash
sudo docker-compose build
sudo docker-compose up --detach
```

## Updating PasteMystBot
To update PasteMystBot, simply pull the latest changes from the repo and restart the container:
```bash
git pull
sudo docker-compose stop
sudo docker-compose build
sudo docker-compose up --detach
```

## Using PasteMystBot
For further usage breakdown and explanation of commands, see [USAGE.md](USAGE.md).

## License
This bot is under the [MIT License](LICENSE.md).

## Disclaimer
This bot is tailored for use within the [Brackeys Discord server](https://discord.gg/brackeys). While this bot is open source and you are free to use it in your own servers, you accept responsibility for any mishaps which may arise from the use of this software. Use at your own risk.
