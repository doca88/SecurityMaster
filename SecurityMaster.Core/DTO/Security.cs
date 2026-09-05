public class Security
{
        public long Sid { get; set; }
        public required string Description { get; set; }
}

public class CreateSecurityRequest
{
    public long Sid { get; set; }
    public required string Description { get; set; }
}