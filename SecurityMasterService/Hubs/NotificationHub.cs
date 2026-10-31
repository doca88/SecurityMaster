using Microsoft.AspNetCore.SignalR;

namespace SecurityMasterService.Hubs;

/// <summary>
/// Jedan kanal za sva obaveštenja klijentima (lookup liste i nalozi).
/// Prazan je jer klijenti ne šalju ništa serveru, samo slušaju.
/// </summary>
public class NotificationHub : Hub
{
    // lookup liste: bez podataka, klijent ponovo učita listu
    public const string ManagersChanged = "ManagersChanged";
    public const string StrategiesChanged = "StrategiesChanged";
    public const string SecuritiesChanged = "SecuritiesChanged";

    // nalog: šalje se orderId, pa klijent osveži samo taj nalog
    public const string OrderChanged = "OrderChanged";

    public const string AllocationChanged = "AllocationChanged";
}