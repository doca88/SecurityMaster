using System.Text.Json.Serialization;

public class Allocation
{
    public int AllocationId { get; set; }
    public decimal Quantity { get; set; }
    public int ManagerId { get; set; }
    public int StrategyId { get; set; }
    public int OrderId { get; set; }

    public required Manager Manager { get; set; }
    public required Strategy Strategy { get; set; }

    [JsonIgnore]
    public Order Order { get; set; } = null!;
}