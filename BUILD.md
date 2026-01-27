# default debug
dotnet build SwiftXP.SPT.TheModfather.slnx /p:Version=0.2.1

# default release
dotnet build -c Release SwiftXP.SPT.TheModfather.slnx /p:Version=0.2.1

# publish winforms updater
dotnet publish ./Sources/Updater/SwiftXP.SPT.TheModfather.Updater.csproj -c Release -r win-x64 /p:Version=0.2.1