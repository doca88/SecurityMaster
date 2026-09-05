public class Strategy
{
        public int Id { get; set; }
        public required string Display { get; set; }
}

public class CreateStrategyRequest
{
    public required string Display { get; set; }
}