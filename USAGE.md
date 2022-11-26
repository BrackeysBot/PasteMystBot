# Context Menus & Reactions

PasteMystBot makes use of modern Discord API features such as slash commands and context menus. Below is a list of the available
context menus.

### ✉️ Message > Apps > Paste Message

**Staff action.** Allows staff members (and potentially Guru) to paste a message to PasteMyst. This action will delete the original message.

# Slash Commands

Below is an outline of every slash command currently implemented in PasteMystBot, along with their descriptions and parameters.

### `/paste`

Prevent a user's message reports from being acknowledged.

| Parameter      | Required | Type               | Description                                              |
|:---------------|:---------|:-------------------|:---------------------------------------------------------|
| message        | ✅ Yes    | Message ID or link | The user whose reports to block.                         |
| deleteOriginal | ❌ No     | Message ID or link | Whether to delete the original message. Default is true. |
