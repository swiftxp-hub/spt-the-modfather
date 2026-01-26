using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.External;
using SPTarkov.Server.Core.Models.Utils;
using SwiftXP.SPT.TheModfather.Http.Interfaces;

namespace SwiftXP.SPT.TheModfather.Server;

[Injectable(InjectionType = InjectionType.Singleton, TypePriority = OnLoadOrder.PreSptModLoader + 1)]

#pragma warning disable CS9113 // Parameter is unread.
public class TheModfatherMod(ISptLogger<TheModfatherMod> logger,
    IModHttpListener httpListener)
#pragma warning restore CS9113 // Parameter is unread.
    : IPreSptLoadModAsync
{
#pragma warning disable CS1998 // This async method lacks 'await' operators.
    public async Task PreSptLoadAsync()
#pragma warning restore CS1998 // This async method lacks 'await' operators.
    {
        
    }
}