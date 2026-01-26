---
> This is an alpha release. **DON'T PANIC**. It is mostly harmless, but can be populated by bugs. If you forgot your towel (or fear instability), please do not press the download button.
---

## Tabs {.tabset}

### The Modfather
#### What does it do?

This MOD facilitates the synchronization of Client-Mods (and, if you feel adventurous, other highly improbable things like configurations) between your SPT-Server and the SPT-Clients connecting to it. Both the Server and the Clients possess a configuration file, allowing you to designate exactly which files and directories are permitted to hitch a ride across the network.

**Just to make it more clear: This mod synchronizes mods from your SPT Server to the Client(s), but does NOT download updates from the SPT Forge.**

#### Requirements

Practically none. If you can run SPT, you can run this.

#### If you use the FIKA headless client:

The Modfather also supports synchronization with one or more FIKA Headless Clients. However, unlike a standard Client (which is generally happy to receive whatever), this utilizes a Whitelist. Only files or directories explicitly listed in the Whitelist will be synchronized.

#### Configuration

**Step 1:** To generate the configuration files, you must **launch the Server or EFT at least once**. (This causes the files to materialize into existence).

**Step 2:** Edit the files.

The Server configuration can be found here:
```
/yourSPTServerFolder/SPT-4.0.11/SPT/user/mods/com.swiftxp.spt.themodfather/config/config.json
```

The Client and Headless-Client configuration can be found here:
```
/yourSPTClientFolder/TheModfather_Data/config.json
```

**IMPORTANT: How to write paths**

Path specifications must always be **relative** to your SPT folder.

* **Correct:** `BepInEx/plugins`
* **Wrong:** `C:/SPT4-OMG/BepInEx/plugins` (Do not use absolute paths or drive letters, it confuses the navigation computer).

#### Planned features/changes

- A lot... but my time is as limited as a decent cup of tea in space. If you have wishes, please leave them in the comments.

### SPT 4.x Installation  

#### Installation

**1. Client Installation:**
Copy the content of the downloaded zip (the **BepInEx** folder and the **.exe** file) into your main game directory.

* The file `SwiftXP.SPT.TheModfather.Updater.exe` must be in the same folder as your `EscapeFromTarkov.exe`.
* Resulting path check:
    ```
    - ...\yourSPTclient\BepInEx\plugins\com.swiftxp.spt.themodfather\SwiftXP.SPT.TheModfather.Client.dll
    - ...\yourSPTclient\SwiftXP.SPT.TheModfather.Updater.exe
    ```

**2. Server Installation:**
Copy the content of the downloaded zip into your server directory.

* Resulting path check:
    ```
    - ...\yourSPTserver\BepInEx\plugins\com.swiftxp.spt.themodfather\SwiftXP.SPT.TheModfather.Client.dll
    - ...\yourSPTserver\SwiftXP.SPT.TheModfather.Updater.exe
    - ...\yourSPTserver\SPT\user\mods\com.swiftxp.spt.themodfather\SwiftXP.SPT.TheModfather.Server.dll
    ```

### FAQ  
*No FAQ available yet... the Deep Thought computer is still calculating the questions.*

{.endtabset}

---

#### Support & Feature Requests

I maintain this in my spare time, between bouts of existential dread and tea.  
Please be patient.

**Shout-out to the SPT team and all modders.  
You’re brilliant, bewildering, and beloved.**