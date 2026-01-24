---
> This is an alpha release. **DON'T PANIC**. It is mostly harmless, but can be populated by bugs. If you forgot your towel (or fear instability), please do not press the download button.
---

## Tabs {.tabset}

### The Modfather
#### What does it do?

This MOD facilitates the synchronization of Client-Mods (and, if you feel adventurous, other highly improbable things like configurations) between your SPT-Server and the SPT-Clients connecting to it. Both the Server and the Clients possess a configuration file, allowing you to designate exactly which files and directories are permitted to hitch a ride across the network.

#### Requirements

Practically none. If you can run SPT, you can run this.

#### If you use the FIKA headless client:

The Modfather also supports synchronization with one or more FIKA Headless Clients. However, unlike a standard Client (which is generally happy to receive whatever), this utilizes a Whitelist. Only files or directories explicitly listed in the Whitelist will be synchronized.

#### Configuration

To cause the configuration files to materialize into existence, you must launch the Server or EFT at least once.

The Server configuration can be adjusted here:

- /yourSPTServerFolder/SPT-4.0.11/SPT/user/mods/com.swiftxp.spt.themodfather/config/config.json

The Client and Headless-Client configuration can be adjusted here:

- /yourSPTClientFolder/TheModfather_Data/config.json

Path specifications must always be relative to your SPT folder. E.g., "BepInEx/plugins". Absolute coordinates like "C:/SPT4-OMG/BepInEx/plugins" will not work (and may confuse the navigation computer).

#### Planned features/changes

- A lot... but my time is as limited as a decent cup of tea in space. If you have wishes, please leave them in the comments.

### SPT 4.x Installation  

#### Installation

Extract the **BepInEx** folder and the **SwiftXP.SPT.TheModfather.Updater.exe** file into the client. Extract the **SPT** folder into the server.

Client:
```
- C:\yourSPTclient\BepInEx\plugins\com.swiftxp.spt.themodfather\SwiftXP.SPT.TheModfather.Client.dll
- C:\yourSPTclient\SwiftXP.SPT.TheModfather.Updater.exe
```

Server:
```
- C:\yourSPTserver\SPT\user\mods\com.swiftxp.spt.themodfather\SwiftXP.SPT.TheModfather.Server.dll
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