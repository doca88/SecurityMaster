public class Order
{
    public int OrderId { get; set; }
    public decimal Quantity { get; set; }
    public DateTime TradeDate { get; set; }
    public int ManagerId { get; set; }
    public int StrategyId { get; set; }
    public long Sid { get; set; }

    public required Manager Manager { get; set; }
    public required Strategy Strategy { get; set; }
    public required Security Security { get; set; }
    public ICollection<Allocation> Allocations { get; set; } = new List<Allocation>();
}

public class CreateOrderRequest
{
    public decimal Quantity { get; set; }
    public DateTime TradeDate { get; set; }
    public int ManagerId { get; set; }
    public int StrategyId { get; set; }
    public long Sid { get; set; }
     public List<AllocationRequest> Allocations { get; set; } = new List<AllocationRequest>();
}

public class AllocationRequest
{
    public int AllocationId { get; set; }   // 0 = nova alokacija
    public int ManagerId { get; set; }
    public int StrategyId { get; set; }
    public decimal Quantity { get; set; }
}