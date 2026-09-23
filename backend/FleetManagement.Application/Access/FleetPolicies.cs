namespace FleetManagement.Application.Access;

public static class FleetPolicies
{
    public const string ManageUsers = nameof(ManageUsers);
    public const string ManageFleet = nameof(ManageFleet);
    public const string ManageOperations = nameof(ManageOperations);
    public const string ManageMaintenance = nameof(ManageMaintenance);
    public const string DriverWorkflow = nameof(DriverWorkflow);
    public const string ReadReports = nameof(ReadReports);

    public static IReadOnlyDictionary<string, string> Roles { get; } = new Dictionary<string, string>
    {
        [ManageUsers] = FleetRoles.FleetAdministrator,
        [ManageFleet] = FleetRoles.FleetAdministrator,
        [ManageOperations] = FleetRoles.OperationsCoordinator,
        [ManageMaintenance] = FleetRoles.Mechanic,
        [DriverWorkflow] = FleetRoles.Driver,
        [ReadReports] = FleetRoles.FleetOwner
    };
}
