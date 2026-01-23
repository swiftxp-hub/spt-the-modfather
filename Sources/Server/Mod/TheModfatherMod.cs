using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.External;
using SPTarkov.Server.Core.Models.Utils;

namespace SwiftXP.SPT.TheModfather;

[Injectable(InjectionType = InjectionType.Singleton, TypePriority = OnLoadOrder.PreSptModLoader + 1)]
public class TheModfatherMod(ISptLogger<TheModfatherMod> logger,
    IModHttpListener httpListener)

    : IPreSptLoadModAsync
{
    public async Task PreSptLoadAsync()
    {
        
    }
}