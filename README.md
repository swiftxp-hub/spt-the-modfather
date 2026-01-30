## Tabs {.tabset}

### The Modfather
#### What does it do?

This MOD facilitates the synchronization of client-Mods (and, if you feel adventurous, other highly improbable things like configurations) between your SPT-server and the SPT-clients connecting to it. Both the server and the clients possess a configuration file, allowing you to designate exactly which files and directories are permitted to hitch a ride across the network.

**Just to make it more clear: This mod synchronizes mods from your SPT server to the client(s), but does NOT download updates from the SPT Forge.**

#### Requirements

Practically none. If you can run SPT, you can run this.

#### If you use the Fika headless client:

The Modfather also supports synchronization with one or more Fika headless clients. However, unlike a standard client (which is generally happy to receive whatever), this utilizes a Whitelist. Only files or directories explicitly listed in the Whitelist will be synchronized.

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

* **Correct:** `BepInEx/plugins/**/*`
* **Wrong:** `C:/SPT4-OMG/BepInEx/plugins/**/*`  
  (Do not use absolute paths or drive letters, it confuses the navigation computer).

Please use globbing patterns to specify paths. For example, `BepInEx/plugins/**/*.dll` will synchronize all `.dll` files in the plugins folder and all its subdirectories.

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
    - ...\yourSPTclient\BepInEx\plugins\com.swiftxp.spt.themodfather\SwiftXP.SPT.TheModfather.Client.dll
    - ...\yourSPTclient\BepInEx\plugins\com.swiftxp.spt.themodfather\Microsoft.Extensions.FileSystemGlobbing.dll
    - ...\yourSPTclient\BepInEx\plugins\com.swiftxp.spt.themodfather\SwiftXP.SPT.TheModfather.Client.LICENSES.txt
    - ...\yourSPTclient\SwiftXP.SPT.TheModfather.Updater.exe
    - ...\yourSPTclient\SwiftXP.SPT.TheModfather.Updater.LICENSES.txt
    ```

---
  
**2. Server Installation:**
Copy the content of the downloaded zip into your server directory.

* Resulting path check:
    ```
    - ...\yourSPTserver\BepInEx\plugins\com.swiftxp.spt.themodfather\SwiftXP.SPT.TheModfather.Client.dll
    - ...\yourSPTserver\BepInEx\plugins\com.swiftxp.spt.themodfather\Microsoft.Extensions.FileSystemGlobbing.dll
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
/yourSPTserverFolder/SPT-4.0.11/SPT/user/mods/com.swiftxp.spt.themodfather/config/config.json
```

---

The default configuration for the server is (v1.0.1):

```json
{
  "ConfigVersion": "1.0.1",
  "SyncedPaths": [
    "SwiftXP.SPT.TheModfather.Updater.exe",
    "BepInEx/patchers/**/*",
    "BepInEx/plugins/**/*"
  ],
  "ExcludedPaths": [
    "BepInEx/patchers/spt-prepatch.dll",
    "BepInEx/plugins/spt/**/*",
    "**/*.log",
    "BepInEx/plugins/SAIN/BotTypes.json",
    "BepInEx/plugins/SAIN/Default Bot Config Values/**/*",
    "BepInEx/plugins/SAIN/Presets/**/*"
  ]
}
```

---

**Parameter Explanation:**

* **ConfigVersion**:  
  **Please do not touch this.**  
  It ensures the mod knows which version of reality it is currently operating in.

* **SyncedPaths**:  
  These are the directories or specific files that will be **sent** to the connecting clients. Globbing patterns are supported (and necessary).
  * If you define a folder (e.g., `BepInEx/plugins/**/*`), **all** contents within that folder will be synchronized recursively.
  * Remember: Paths are relative to the SPT root folder.

* **ExcludedPaths**:  
  These are specific exceptions to the `SyncedPaths`. Files or folders listed here will **NOT** be sent, even if they reside inside a synced folder. Globbing patterns are supported (and necessary).
  * This is crucial for preventing the overwriting of client-specific files (like `spt-prepatch.dll` or the `spt` folder itself).

**Important Note:** Ensure you maintain valid JSON syntax (commas at the end of lines, quotes around strings). If the JSON is invalid, the universe might implode (mild exaggeration, but the mod won't load).

#### Examples

---

**Scenario 1: The "Hive Mind" (syncing configuration files)**

You want all your connected clients to share the exact same mod settings as the server. By default, configs are local, but you can force synchronization.

Simply add `"BepInEx/config"` to your `SyncedPaths`.

```json
{
  "ConfigVersion": "1.0.1",
  "SyncedPaths": [
    "SwiftXP.SPT.TheModfather.Updater.exe",
    "BepInEx/patchers/**/*",
    "BepInEx/plugins/**/*",
    "BepInEx/config/**/*"
  ],
  "ExcludedPaths": [
    "BepInEx/patchers/spt-prepatch.dll",
    "BepInEx/plugins/spt/**/*",
    "**/*.log",
    "BepInEx/plugins/SAIN/BotTypes.json",
    "BepInEx/plugins/SAIN/Default Bot Config Values/**/*",
    "BepInEx/plugins/SAIN/Presets/**/*"
  ]
}
```

You can also sync only specific config files (e.g. the settings for Acid's Bot Placement System) instead of all configurations.

```json
{
  "ConfigVersion": "1.0.1",
  "SyncedPaths": [
    "SwiftXP.SPT.TheModfather.Updater.exe",
    "BepInEx/patchers/**/*",
    "BepInEx/plugins/**/*",
    "BepInEx/config/com.acidphantasm.botplacementsystem.cfg"
  ],
  "ExcludedPaths": [
    "BepInEx/patchers/spt-prepatch.dll",
    "BepInEx/plugins/spt/**/*",
    "**/*.log",
    "BepInEx/plugins/SAIN/BotTypes.json",
    "BepInEx/plugins/SAIN/Default Bot Config Values/**/*",
    "BepInEx/plugins/SAIN/Presets/**/*"
  ]
}
```

---

**Scenario 2: The "Pick and Choose" (syncing specific files only)**

Perhaps you don't want to sync the entire plugins folder. You can target specific mods.  
  
In this example, we only sync one specific mod folder:

```json
{
  "ConfigVersion": "1.0.1",
  "SyncedPaths": [
    "BepInEx/plugins/SamSWAT-TimeWeatherChanger/**/*"
  ],
  "ExcludedPaths": []
}
```

---

**Scenario 3: The "Exception to the Rule" (excluding specific configs)**

You want to sync all configs (`BepInEx/config`), EXCEPT one specific file that contains vogon poetry that clients must not possess.

```json
{
  "ConfigVersion": "1.0.1",
  "SyncedPaths": [
    "SwiftXP.SPT.TheModfather.Updater.exe",
    "BepInEx/plugins/**/*",
    "BepInEx/config/**/*"
  ],
  "ExcludedPaths": [
    "BepInEx/patchers/spt-prepatch.dll",
    "BepInEx/plugins/spt/**/*",
    "**/*.log",
    "BepInEx/plugins/SAIN/BotTypes.json",
    "BepInEx/plugins/SAIN/Default Bot Config Values/**/*",
    "BepInEx/plugins/SAIN/Presets/**/*",
    "BepInEx/config/vogon.poetry.cfg"
  ]
}
```

### Client-Configuration

#### Client-Configuration

To generate the configuration file, you must **launch the Game (client) at least once** with the mod installed. (This causes the file to pop into existence from the void). Alternatively, you can create it manually.

---

The client configuration can be found here:
```
/yourSPTclientFolder/TheModfather_Data/config.json
```

---

The default configuration for the client is (v1.0.1):

```json
{
  "ConfigVersion": "1.0.1",
  "ExcludedPaths": [
    "BepInEx/patchers/spt-prepatch.dll",
    "BepInEx/plugins/spt/**/*",
    "**/*.log",
    "BepInEx/plugins/SAIN/BotTypes.json",
    "BepInEx/plugins/SAIN/Default Bot Config Values/**/*",
    "BepInEx/plugins/SAIN/Presets/**/*"
  ],
  "HeadlessWhitelist": [
    "SwiftXP.SPT.TheModfather.Updater.exe",
    "BepInEx/plugins/com.swiftxp.spt.themodfather/**/*",
    "BepInEx/plugins/acidphantasm-botplacementsystem/**/*",
    "BepInEx/plugins/DrakiaXYZ-Waypoints/**/*",
    "BepInEx/plugins/SAIN/**/*",
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
  Files or folders listed here will NOT be touched, even if the server tries to update, add, or delete them. Globbing patterns are supported (and necessary).

  * Use Case: This allows you to keep client-side-only mods that the server doesn't have, without the sync process deleting them as "alien debris".

* **HeadlessWhitelist**:
  
  * **No:** Ignore this section completely. It does nothing for human players.

  * **Yes:** If you are running a Fika headless client, this list defines the only things you are allowed to download. Please see the specific "Fika Headless" section for details.

**Important Note:** Ensure you maintain valid JSON syntax (commas at the end of lines, quotes around strings). If the JSON is invalid, the universe might implode (mild exaggeration, but the mod won't load).

#### Examples

---

**Scenario 1: "My Eyes, My Choice" (Protecting graphics settings)**

The server pushes the Amands Graphics mod to everyone. Great! But the server admin has terrible taste in lighting settings. You want the mod, but you want to keep your own configuration file.  

You exclude the specific config file so the server can't overwrite your beautiful settings.  

```json
{
  "ConfigVersion": "1.0.1",
  "ExcludedPaths": [
    "BepInEx/patchers/spt-prepatch.dll",
    "BepInEx/plugins/spt/**/*",
    "**/*.log",
    "BepInEx/plugins/SAIN/BotTypes.json",
    "BepInEx/plugins/SAIN/Default Bot Config Values/**/*",
    "BepInEx/plugins/SAIN/Presets/**/*",
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
  "ConfigVersion": "1.0.1",
  "ExcludedPaths": [
    "BepInEx/patchers/spt-prepatch.dll",
    "BepInEx/plugins/spt/**/*",
    "**/*.log",
    "BepInEx/plugins/SAIN/BotTypes.json",
    "BepInEx/plugins/SAIN/Default Bot Config Values/**/*",
    "BepInEx/plugins/SAIN/Presets/**/*",
    "BepInEx/plugins/acidphantasm-accessibilityindicators/**/*",
    "BepInEx/config/com.acidphantasm.accessibilityindicators.cfg"
  ],
  "HeadlessWhitelist": [...]
}
```

### Fika Headless

#### Fika Headless

To generate the configuration file, you must **launch the Fika headless client at least once** with the mod installed. (This causes the file to pop into existence from the void). Alternatively, you can create it manually.

---

The Fika headless client configuration can be found here:
```
/yourSPTclientFolder/TheModfather_Data/config.json
```

---

The default configuration is (v1.0.1):

```json
{
  "ConfigVersion": "1.0.1",
  "ExcludedPaths": [
    "BepInEx/patchers/spt-prepatch.dll",
    "BepInEx/plugins/spt/**/*",
    "**/*.log",
    "BepInEx/plugins/SAIN/BotTypes.json",
    "BepInEx/plugins/SAIN/Default Bot Config Values/**/*",
    "BepInEx/plugins/SAIN/Presets/**/*"
  ],
  "HeadlessWhitelist": [
    "SwiftXP.SPT.TheModfather.Updater.exe",
    "BepInEx/plugins/com.swiftxp.spt.themodfather/**/*",
    "BepInEx/plugins/acidphantasm-botplacementsystem/**/*",
    "BepInEx/plugins/DrakiaXYZ-Waypoints/**/*",
    "BepInEx/plugins/SAIN/**/*",
    "BepInEx/plugins/DrakiaXYZ-BigBrain.dll",
    "BepInEx/plugins/Tyfon.UIFixes.dll",
    "BepInEx/plugins/Tyfon.UIFixes.Net.dll"
  ]
}
```

---

**Parameter Explanation:**

* **ConfigVersion**:  
  **Please do not touch this.**  
  It ensures the mod knows which version of reality it is currently operating in.

* **ExcludedPaths**:  
  Files or folders listed here will NOT be touched, even if the server tries to update, add, or delete them.  

* **HeadlessWhitelist**:

  **The mod automatically detects if it is running on a Fika headless client. In this mode, ONLY the files and folders listed in this whitelist will be synchronized.**  
  
  Many client-side mods are not required on a headless client. It is generally recommended to avoid installing unnecessary client mods in a headless environment.  

  The default whitelist already contains mods that are essential for the Fika headless client to function correctly (e.g., SAIN, to ensure AI improvements are properly calculated).

  Globbing patterns are supported (and necessary).

**Important Note:** Ensure you maintain valid JSON syntax (commas at the end of lines, quotes around strings). If the JSON is invalid, the universe might implode (mild exaggeration, but the mod won't load).

### FAQ

#### Frequently asked questions *(or questions I made up to feel important)*

*(Last updated Jan 30, 2026 — The Modfather v1.0.1)*

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
  - Yes. Please consult the **"Fika Headless"** section of this document for the specific incantations (configurations) required.

- **Can this mod update the Fika headless client?**
  - **Yes.** The mod is clever enough to distinguish between a headless client and a regular human player. This means you can place Fika Headless specific files on your server (e.g., `Fika.Headless.dll`), and *The Modfather* will intelligently sync these updates **only** to your headless client(s), sparing the regular players from receiving files they neither need nor understand.

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