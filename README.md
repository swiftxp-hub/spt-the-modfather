## Tabs {.tabset}

### The Modfather
#### What does this MOD do?

This MOD facilitates the synchronization of client-Mods (and, if you feel adventurous, other highly improbable things like configurations) between your SPT-server and the SPT-clients connecting to it. Both the server and the clients possess a configuration file, allowing you to designate exactly which files and directories are permitted to hitch a ride across the network.

**Just to clarify: This mod synchronizes mods from your SPT server to the client(s), but does NOT download updates from the SPT Forge.**

#### Explanations of the "Actions" in the update screen

- **Add** - This file does not exist on your client and will be downloaded from the server.
- **Update** - This file exists on your client, but an update is available on the server that will be downloaded.
- **Delete** - This file was deleted on the server and will be deleted from your client.
- **Adopt** - This file exists on both your client and the server and does not need to be updated, but your client will add it to its 'memory'.
- **Untrack** - This file was tracked by the sync process, but you added it to the exclude patterns, so it will be ignored in the current and future syncs.
- **Blacklist** - This file exists on your client but was blacklisted by the server admin. It will be deleted.

#### Requirements

Practically none. If you can run SPT, you can run this.

#### If you use the Fika headless client:

The Modfather also supports synchronization with one or more Fika headless clients.

#### Server-Configuration

**Step 1:** To generate the configuration files, you must **launch the server at least once**. (This causes the files to materialize into existence).

**Step 2:** Edit the files.

---

The server configuration can be found here:
```
/yourSPTserverFolder/SPT-4.0.12/SPT/user/mods/com.swiftxp.spt.themodfather/config/serverConfig.json
```

---

**IMPORTANT: How to write paths**

Path specifications must always be **relative** to your SPT folder.

* **Correct:** `BepInEx/plugins/**/*`
* **Wrong:** `C:/SPT4-Server/BepInEx/plugins/**/*`  
  (Do not use absolute paths or drive letters; it confuses the navigation computer).

Please use globbing patterns to specify paths. For example, `BepInEx/plugins/**/*.dll` will synchronize all `.dll` files in the plugins folder and all its subdirectories.

**For more information on the server- and client-configuration, please see the corresponding sections.**

#### Planned features/changes

- Enable the ability to configure sync exceptions on the client-side via the BepInEx configurator.
- A lot more... but my time is as limited as a decent cup of tea in space. If you have wishes, please leave them in the comments.

### SPT 4.x Installation  

#### Installation

**1. Client Installation:**
Copy the content of the downloaded zip (the **BepInEx** folder and the **.exe** file) into your main game directory.

---

* The file `SwiftXP.SPT.TheModfather.Updater.exe` must be in the same folder as your `EscapeFromTarkov.exe`.
* Resulting path check:
    ```
    - ...\yourSPTclient\BepInEx\plugins\com.swiftxp.spt.themodfather\Microsoft.Extensions.FileSystemGlobbing.dll
    - ...\yourSPTclient\BepInEx\plugins\com.swiftxp.spt.themodfather\SwiftXP.SPT.TheModfather.Client.dll
    - ...\yourSPTclient\BepInEx\plugins\com.swiftxp.spt.themodfather\SwiftXP.SPT.TheModfather.Client.LICENSES.txt
    - ...\yourSPTclient\SwiftXP.SPT.TheModfather.Updater.exe
    - ...\yourSPTclient\SwiftXP.SPT.TheModfather.Updater.LICENSES.txt
    ```

---
  
**2. Server Installation:**
Copy the content of the downloaded zip into your server directory.

* Resulting path check:
    ```
    - ...\yourSPTserver\BepInEx\plugins\com.swiftxp.spt.themodfather\Microsoft.Extensions.FileSystemGlobbing.dll
    - ...\yourSPTserver\BepInEx\plugins\com.swiftxp.spt.themodfather\SwiftXP.SPT.TheModfather.Client.dll
    - ...\yourSPTserver\BepInEx\plugins\com.swiftxp.spt.themodfather\SwiftXP.SPT.TheModfather.Client.LICENSES.txt
    - ...\yourSPTserver\SwiftXP.SPT.TheModfather.Updater.exe
    - ...\yourSPTserver\SwiftXP.SPT.TheModfather.Updater.LICENSES.txt
    - ...\yourSPTserver\SPT\user\mods\com.swiftxp.spt.themodfather\SwiftXP.SPT.TheModfather.Server.dll
    - ...\yourSPTserver\SPT\user\mods\com.swiftxp.spt.themodfather\SwiftXP.SPT.TheModfather.Server.LICENSES.txt
    ```

---

### Server-Configuration

#### Server-Configuration

To generate the configuration file, you must **launch the server at least once** with the mod installed. (This causes the file to materialize into existence). Alternatively, you can create it manually.

---

The server configuration can be found here:
```
/yourSPTserverFolder/SPT-4.0.11/SPT/user/mods/com.swiftxp.spt.themodfather/config/serverConfig.json
```

---

The default configuration for the server is (v2.0.0):

```json
{
  "ConfigVersion": "2.0.0",
  "IncludePatterns": [
    "SwiftXP.SPT.TheModfather.Updater.exe",
    "BepInEx/patchers/**/*",
    "BepInEx/plugins/**/*"
  ],
  "ExcludePatterns": [
    "**/*.log",
    "BepInEx/patchers/spt-prepatch.dll",
    "BepInEx/plugins/Fika/Fika.Headless.dll",
    "BepInEx/plugins/SAIN/**/*.json",
    "BepInEx/plugins/spt/**/*"
  ],
  "FileHashBlacklist": []
}
```

---

**Parameter Explanation:**

* **ConfigVersion**:  
  **Please do not touch this.** It ensures the mod knows which version of reality it is currently operating in.

* **IncludePatterns**:  
  These are the directories or specific files that will be **sent** to the connecting clients. Globbing patterns are supported (and necessary).
  * If you define a folder (e.g., `BepInEx/plugins/**/*`), **all** contents within that folder will be synchronized recursively.
  * Remember: Paths are relative to the SPT root folder.

* **ExcludePatterns**:  
  These are specific exceptions to the `IncludePatterns`. Files or folders listed here will **NOT** be sent, even if they reside inside a synced folder. Globbing patterns are supported (and necessary).
  * This is crucial for preventing the overwriting of client-specific files (like `spt-prepatch.dll` or the `spt` folder itself).

* **FileHashBlacklist**:  
  This allows files to be blacklisted based on their XxHash128. The client will search the `BepInEx` directory for a file matching this hash and prompt the user to delete it during the synchronization process. Users cannot refuse this specific deletion. However, they can choose to skip the synchronization entirely, which avoids the deletion - but they will consequently lose the ability to synchronize updates for other plugins. Why hashes? So users can't simply bypass the blacklist by renaming the file.

**Important Note:** Ensure you maintain valid JSON syntax (commas at the end of lines, quotes around strings). If the JSON is invalid, the universe might implode (mild exaggeration, but the mod won't load).

#### Examples

---

**Scenario 1: The "Hive Mind" (syncing configuration files)**

You want all your connected clients to share the exact same mod settings as the server. By default, configs are local, but you can force synchronization.

Simply add `"BepInEx/config"` to your `IncludePatterns`.

```json
{
  "ConfigVersion": "2.0.0",
  "IncludePatterns": [
    "SwiftXP.SPT.TheModfather.Updater.exe",
    "BepInEx/patchers/**/*",
    "BepInEx/plugins/**/*",
    "BepInEx/config/**/*"
  ],
  "ExcludePatterns": [
    "**/*.log",
    "BepInEx/patchers/spt-prepatch.dll",
    "BepInEx/plugins/Fika/Fika.Headless.dll",
    "BepInEx/plugins/SAIN/**/*.json",
    "BepInEx/plugins/spt/**/*"
  ],
  "FileHashBlacklist": []
}
```

You can also sync only specific config files (e.g. the settings for Acid's Bot Placement System) instead of all configurations.

```json
{
  "ConfigVersion": "2.0.0",
  "IncludePatterns": [
    "SwiftXP.SPT.TheModfather.Updater.exe",
    "BepInEx/patchers/**/*",
    "BepInEx/plugins/**/*",
    "BepInEx/config/com.acidphantasm.botplacementsystem.cfg"
  ],
  "ExcludePatterns": [
    "**/*.log",
    "BepInEx/patchers/spt-prepatch.dll",
    "BepInEx/plugins/Fika/Fika.Headless.dll",
    "BepInEx/plugins/SAIN/**/*.json",
    "BepInEx/plugins/spt/**/*"
  ],
  "FileHashBlacklist": []
}
```

---

**Scenario 2: The "Pick and Choose" (syncing specific files only)**

Perhaps you don't want to sync the entire plugins folder. You can target specific mods.  
  
In this example, we only sync one specific mod folder:

```json
{
  "ConfigVersion": "2.0.0",
  "IncludePatterns": [
    "BepInEx/plugins/SamSWAT-TimeWeatherChanger/**/*"
  ],
  "ExcludePatterns": [],
  "FileHashBlacklist": []
}
```

---

**Scenario 3: The "Exception to the Rule" (excluding specific configs)**

You want to sync all configs (`BepInEx/config`), EXCEPT one specific file that contains Vogon poetry that clients must not possess.

```json
{
  "ConfigVersion": "2.0.0",
  "IncludePatterns": [
    "SwiftXP.SPT.TheModfather.Updater.exe",
    "BepInEx/patchers/**/*",
    "BepInEx/plugins/**/*",
    "BepInEx/config/**/*"
  ],
  "ExcludePatterns": [
    "**/*.log",
    "BepInEx/patchers/spt-prepatch.dll",
    "BepInEx/plugins/Fika/Fika.Headless.dll",
    "BepInEx/plugins/SAIN/**/*.json",
    "BepInEx/plugins/spt/**/*",
    "BepInEx/config/vogon.poetry.cfg"
  ]
}
```

---

**Scenario 4: The "Surprising the bad boys" (blacklisting mods)**

You want to ban users from using the "ACCURATE CIRCULAR RADAR v1.2.1" mod on your server.

```json
{
  "ConfigVersion": "2.0.0",
  "IncludePatterns": [
    "SwiftXP.SPT.TheModfather.Updater.exe",
    "BepInEx/patchers/**/*",
    "BepInEx/plugins/**/*"
  ],
  "ExcludePatterns": [
    "**/*.log",
    "BepInEx/patchers/spt-prepatch.dll",
    "BepInEx/plugins/Fika/Fika.Headless.dll",
    "BepInEx/plugins/SAIN/**/*.json",
    "BepInEx/plugins/spt/**/*"
  ],
  "FileHashBlacklist": [
    "19c176c57c531abc6232c206e8534edb"
  ]
}
```

### Client-Configuration

#### Client-Configuration

The client configuration is generated and maintained automatically based on your selections in the update window. If you uncheck a file in the update window, it will automatically be ignored the next time you start the game. It is important that you click "Accept Offer" to ensure the exception is actually written to the configuration - even if you have deselected all files.

Fundamentally, the client retains sovereignty over what is synchronized and deleted. You can choose whether to perform each action (except for files blacklisted by the server admin). If you have installed mods that the server is unaware of, they will not be deleted. Mods you install later are also left untouched by Modfather.

---

In case you need to edit the client configuration by hand - it can be found here:
```
/yourSPTclientFolder/TheModfather_Data/clientConfig.json
```

---

The default configuration for the client is (v2.0.0):

```json
{
  "ConfigVersion": "2.0.0",
  "ExcludePatterns": []
}
```

### Fika Headless

#### Fika Headless

To generate the configuration file, you must **launch the Fika headless client at least once** with the mod installed. (This causes the file to pop into existence from the void). Alternatively, you can create it manually.

---

The Fika headless client configuration can be found here:
```
/yourSPTclientFolder/TheModfather_Data/clientConfig.json
```

---

The default configuration is (v2.0.0):

```json
{
  "ConfigVersion": "2.0.0",
  "ExcludePatterns": []
}
```

---

**Parameter Explanation:**

* **ConfigVersion**:  
  **Please do not touch this.** It ensures the mod knows which version of reality it is currently operating in.

* **ExcludePatterns**:  
  Files or folders listed here will NOT be touched, even if the server tries to update, add, or delete them. Globbing patterns are supported (and necessary).

**Important Note:** Ensure you maintain valid JSON syntax (commas at the end of lines, quotes around strings). If the JSON is invalid, the universe might implode (mild exaggeration, but the mod won't load).

### FAQ

#### Frequently asked questions *(or questions I made up to feel important)*

*(Last updated Feb 12, 2026 - The Modfather v2.0.0)*

- **Is there a chance it will brick my SPT profile or installation?**
  - To be honest with you: **Don't Panic**, but every mod has the inherent potential to turn your installation into a sophisticated paperweight. Rest assured, nobody does this on purpose. I try my very best not to break things. However, entropy is a fundamental law of the universe.

  - **Golden Rule**: Always backup your stuff. Remember, there are two types of people in the galaxy: Those who have backups, and those who have never lost data... yet.

- **Which files can be synchronized?**
  - There is practically no limitation on the files and folders you can synchronize, provided they exist within the SPT folder. You can synchronize almost anything your heart desires.

  - **However**, there are security protocols in place: The mod strictly prohibits synchronizing files outside your SPT folder. We wouldn't want to accidentally synchronize your tax returns or the launch codes for a planetary demolition bot, would we?

- **Does this mean the mod can synchronize itself, enabling self-update capability?**
  - Precisely. In the default configuration, the mod is fully aware of its own existence and capable of self-updating. Clients must install the mod manually at least once (to establish the link). After that, updates placed on the server will propagate to the clients.

  - *Note:* Self-updating is not strictly guaranteed. Bugs, cosmic radiation, or highly improbable circumstances may occasionally prevent this.

- **Hold on! That implies I could also update the entire SPT installation to a newer version. Right?**
  - Technically, yes. You could. But I will not provide example configurations for this.

  - This is a procedure so fraught with peril that it could brick the client's installation. If you insist on attempting this and the universe subsequently implodes (or your game crashes), please do not ask for support. You will be politely ignored.

- **Does this mod work with Fika?**
  - Yes, absolutely. They get along splendidly.

- **Does this mod work with Fika Headless?**
  - Yes.

- **Can this mod update the Fika headless client?**
  - Yes, but you need to remove the ExcludePattern for "Fika.Headless.dll" in the configuration on the headless client.

- **Is the mod compatible with the `fika-headless-docker` image from `zhliau`?**
  - Yes, it is.
  - **Crucial Note:** Remember to set the environment variable `USE_MODSYNC` to `true`. Otherwise, the container will interpret the sync as a critical event and shut down in protest every time an update is processed.

- **Does synchronization work with very large files?**
  - Yes and no. The mod sets a strict connection timeout of 15 minutes per file.

  - Time is an illusion, but timeouts are very real. If a client can download the file within that window, they are fine. If the file is too colossal or the client's connection is slower than a depressed robot, the synchronization will fail.

 - *More to come...*

{.endtabset}

---

#### Support & Feature Requests

I maintain this in my spare time, between bouts of existential dread and tea.  
Please be patient.

**Shout-out to the SPT team and all modders.  
You’re brilliant, bewildering, and beloved.**