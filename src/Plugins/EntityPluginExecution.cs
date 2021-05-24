namespace DotNetDevOps.Extensions.EAVFramework.Plugins
{
    public enum EntityPluginExecution
    {
        PreValidate,
        PreOperation,
        PostOperation
    }
    public enum EntityPluginOperation
    {
        Create,
        Update,
        Retrieve,
        RetrieveAll
    }
}
