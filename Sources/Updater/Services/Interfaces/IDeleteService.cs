namespace SwiftXP.SPT.TheModfather.Updater.Services.Interfaces;

public interface IDeleteService
{
    void ProcessDeleteInstructions(string basePath, string payloadPath, string deleteInstructionSuffix);
}
