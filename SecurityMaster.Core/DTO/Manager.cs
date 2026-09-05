
public class Manager
{
        public int Id { get; set; }
        public required string Display { get; set; }
}

public class CreateManagerRequest
{
    public required string Display { get; set; }
}