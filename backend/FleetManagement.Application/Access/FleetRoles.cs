namespace FleetManagement.Application.Access;

public static class FleetRoles
{
    public const string FleetAdministrator = nameof(FleetAdministrator);
    public const string OperationsCoordinator = nameof(OperationsCoordinator);
    public const string Mechanic = nameof(Mechanic);
    public const string Driver = nameof(Driver);
    public const string FleetOwner = nameof(FleetOwner);
    public static IReadOnlyList<string> All { get; } =
        Array.AsReadOnly(new[] { FleetAdministrator, OperationsCoordinator, Mechanic, Driver, FleetOwner });
}
