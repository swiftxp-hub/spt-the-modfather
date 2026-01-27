---
> This is an alpha release. **DON'T PANIC**. It is mostly harmless, but can be populated by bugs. If you forgot your towel (or fear instability), please do not press the download button.
---

## Tabs {.tabset}

### The Modfather
#### What does it do?

This MOD facilitates the synchronization of client-Mods (and, if you feel adventurous, other highly improbable things like configurations) between your SPT-server and the SPT-clients connecting to it. Both the server and the clients possess a configuration file, allowing you to designate exactly which files and directories are permitted to hitch a ride across the network.

**Just to make it more clear: This mod synchronizes mods from your SPT server to the client(s), but does NOT download updates from the SPT Forge.**

#### Requirements

Practically none. If you can run SPT, you can run this.

#### If you use the FIKA headless client:

The Modfather also supports synchronization with one or more FIKA headless clients. However, unlike a standard client (which is generally happy to receive whatever), this utilizes a Whitelist. Only files or directories explicitly listed in the Whitelist will be synchronized.

#### Configuration

**Step 1:** To generate the configuration files, you must **launch the server or EFT at least once**. (This causes the files to materialize into existence).

**Step 2:** Edit the files.

---

The server configuration can be found here:
```
/yourSPTserverFolder/SPT-4.0.11/SPT/user/mods/com.swiftxp.spt.themodfather/config/config.json
```

---

The client and headless-client configuration can be found here:
```
/yourSPTclientFolder/TheModfather_Data/config.json
```

---

**IMPORTANT: How to write paths**

Path specifications must always be **relative** to your SPT folder.

* **Correct:** `BepInEx/plugins`
* **Wrong:** `C:/SPT4-OMG/BepInEx/plugins`  
  (Do not use absolute paths or drive letters, it confuses the navigation computer).

**For more information on the server- and client-configuration, please see the corresponding sections.**

#### Planned features/changes

- A lot... but my time is as limited as a decent cup of tea in space. If you have wishes, please leave them in the comments.

### SPT 4.x Installation  

#### Installation

**1. Client Installation:**
Copy the content of the downloaded zip (the **BepInEx** folder and the **.exe** file) into your main game directory.

---

* The file `SwiftXP.SPT.TheModfather.Updater.exe` must be in the same folder as your `EscapeFromTarkov.exe`.
* Resulting path check:
    ```
    - ...\yourSPTclient\BepInEx\plugins\com.swiftxp.spt.themodfather\SwiftXP.SPT.TheModfather.client.dll
    - ...\yourSPTclient\SwiftXP.SPT.TheModfather.Updater.exe
    ```

---
  
**2. Server Installation:**
Copy the content of the downloaded zip into your server directory.

* Resulting path check:
    ```
    - ...\yourSPTserver\BepInEx\plugins\com.swiftxp.spt.themodfather\SwiftXP.SPT.TheModfather.client.dll
    - ...\yourSPTserver\SwiftXP.SPT.TheModfather.Updater.exe
    - ...\yourSPTserver\SPT\user\mods\com.swiftxp.spt.themodfather\SwiftXP.SPT.TheModfather.server.dll
    ```

---

### Server-Configuration

#### Server-Configuration

To generate the configuration file, you must **launch the server at least once** with the mod installed. (This causes the file to materialize into existence).

---

The server configuration can be found here:
```
/yourSPTserverFolder/SPT-4.0.11/SPT/user/mods/com.swiftxp.spt.themodfather/config/config.json
```

---

The default configuration for the server is (v0.2.2):

```json
{
  "ConfigVersion": "0.2.2",
  "SyncedPaths": [
    "SwiftXP.SPT.TheModfather.Updater.exe",
    "BepInEx/patchers",
    "BepInEx/plugins"
  ],
  "ExcludedPaths": [
    "BepInEx/patchers/spt-prepatch.dll",
    "BepInEx/plugins/spt"
  ]
}
```

---

**Parameter Explanation:**

* **ConfigVersion**:  
  **Please do not touch this.**  
  It ensures the mod knows which version of reality it is currently operating in.

* **SyncedPaths**:  
  These are the directories or specific files that will be **sent** to the connecting clients.
  * If you define a folder (e.g., `BepInEx/plugins`), **all** contents within that folder will be synchronized recursively.
  * Remember: Paths are relative to the SPT root folder.

* **ExcludedPaths**:  
  These are specific exceptions to the `SyncedPaths`. Files or folders listed here will **NOT** be sent, even if they reside inside a synced folder.
  * This is crucial for preventing the overwriting of client-specific files (like `spt-prepatch.dll` or the `spt` folder itself).

**Important Note:** Ensure you maintain valid JSON syntax (commas at the end of lines, quotes around strings). If the JSON is invalid, the universe might implode (mild exaggeration, but the mod won't load).

#### Examples

---

**Scenario 1: The "Hive Mind" (syncing configuration files)**

You want all your connected clients to share the exact same mod settings as the server. By default, configs are local, but you can force synchronization.

Simply add `"BepInEx/config"` to your `SyncedPaths`.

```json
{
  "ConfigVersion": "0.2.2",
  "SyncedPaths": [
    "SwiftXP.SPT.TheModfather.Updater.exe",
    "BepInEx/patchers",
    "BepInEx/plugins",
    "BepInEx/config"
  ],
  "ExcludedPaths": [
    "BepInEx/patchers/spt-prepatch.dll",
    "BepInEx/plugins/spt"
  ]
}
```

You can also sync only specific config files (e.g. the settings for Acid's Bot Placement System) instead of all configurations.

```json
{
  "ConfigVersion": "0.2.2",
  "SyncedPaths": [
    "SwiftXP.SPT.TheModfather.Updater.exe",
    "BepInEx/patchers",
    "BepInEx/plugins",
    "BepInEx/config/com.acidphantasm.botplacementsystem.cfg"
  ],
  "ExcludedPaths": [
    "BepInEx/patchers/spt-prepatch.dll",
    "BepInEx/plugins/spt"
  ]
}
```

---

**Scenario 2: The "Pick and Choose" (syncing specific files only)**

Perhaps you don't want to sync the entire plugins folder. You can target specific mods.  
  
In this example, we only sync one specific mod folder:

```json
{
  "ConfigVersion": "0.2.2",
  "SyncedPaths": [
    "BepInEx/plugins/SamSWAT-TimeWeatherChanger"
  ],
  "ExcludedPaths": []
}
```

---

**Scenario 3: The "Exception to the Rule" (excluding specific configs)**

You want to sync all configs (`BepInEx/config`), EXCEPT one specific file that contains vogon poetry that clients must not possess.

```json
{
  "ConfigVersion": "0.2.2",
  "SyncedPaths": [
    "SwiftXP.SPT.TheModfather.Updater.exe",
    "BepInEx/plugins",
    "BepInEx/config"
  ],
  "ExcludedPaths": [
    "BepInEx/patchers/spt-prepatch.dll",
    "BepInEx/plugins/spt",
    "BepInEx/config/vogon.poetry.cfg"
  ]
}
```

### Client-Configuration

#### Client-Configuration

To generate the configuration file, you must **launch the Game (client) at least once** with the mod installed. (This causes the file to pop into existence from the void).

---

The client configuration can be found here:
```
/yourSPTclientFolder/TheModfather_Data/config.json
```

---

The default configuration for the client is (v0.2.2):

```json
{
  "ConfigVersion": "0.2.2",
  "ExcludedPaths": [
    "BepInEx/patchers/spt-prepatch.dll",
    "BepInEx/plugins/spt"
  ],
  "HeadlessWhitelist": [
    "SwiftXP.SPT.TheModfather.Updater.exe",
    "BepInEx/plugins/com.swiftxp.spt.themodfather",
    "BepInEx/plugins/acidphantasm-botplacementsystem",
    "BepInEx/plugins/DrakiaXYZ-Waypoints",
    "BepInEx/plugins/SAIN",
    "BepInEx/plugins/DrakiaXYZ-BigBrain.dll",
    "BepInEx/plugins/Tyfon.UIFixes.dll",
    "BepInEx/plugins/Tyfon.UIFixes.Net.dll"
  ]
}
```

---

**The Concept (Read this, it helps)**
Before diving into the parameters, understand the hierarchy:

The mod operates on a **Benevolent Dictator** system.

  - **The Server (The Dictator):** Dictates which files and folders must exist. If the server adds or updates a mod or changes a file, your client will mirror it.

  - **The Client (The Rebel):** This configuration file allows you to define exceptions. It is your shield.

By using `ExcludedPaths`, you tell the synchronization process: "I don't care what the server says, do not touch this file/folder on my machine."

**Parameter Explanation:**

* **ConfigVersion**:  
  **Please do not touch this.**  
  It ensures the mod knows which version of reality it is currently operating in.

* **ExcludedPaths**:  
  Files or folders listed here will NOT be touched, even if the server tries to update, add, or delete them.  

  * Use Case: This allows you to keep client-side-only mods that the server doesn't have, without the sync process deleting them as "alien debris".

* **HeadlessWhitelist**:
  
  * **No:** Ignore this section completely. It does nothing for human players.

  * **Yes:** If you are running a FIKA headless client, this list defines the only things you are allowed to download. Please see the specific "FIKA Headless" section for details.

**Important Note:** Ensure you maintain valid JSON syntax (commas at the end of lines, quotes around strings). If the JSON is invalid, the universe might implode (mild exaggeration, but the mod won't load).

#### Examples

---

**Scenario 1: "My Eyes, My Choice" (Protecting graphics settings)**

The server pushes the Amands Graphics mod to everyone. Great! But the server admin has terrible taste in lighting settings. You want the mod, but you want to keep your own configuration file.  

You exclude the specific config file so the server can't overwrite your beautiful settings.  

```json
{
  "ConfigVersion": "0.2.2",
  "ExcludedPaths": [
    "BepInEx/patchers/spt-prepatch.dll",
    "BepInEx/plugins/spt",
    "BepInEx/config/com.amands.graphics.cfg"
  ],
  "HeadlessWhitelist": [...]
}
```

---

**Scenario 2: "The Local Rebel" (Keeping client-only mods)**

You love the Audio Accessibility Indicators mod, but the server doesn't have it installed. You want to ensure the sync process doesn't delete it.

```json
{
  "ConfigVersion": "0.2.2",
  "ExcludedPaths": [
    "BepInEx/patchers/spt-prepatch.dll",
    "BepInEx/plugins/spt",
    "BepInEx/plugins/acidphantasm-accessibilityindicators",
    "BepInEx/config/com.acidphantasm.accessibilityindicators.cfg"
  ],
  "HeadlessWhitelist": [...]
}
```


### FIKA Headless

#### FIKA Headless

Coming soon...

### FAQ  
*No FAQ available yet... the Deep Thought computer is still calculating the questions.*

{.endtabset}

---

#### Support & Feature Requests

I maintain this in my spare time, between bouts of existential dread and tea.  
Please be patient.

**Shout-out to the SPT team and all modders.  
You’re brilliant, bewildering, and beloved.**