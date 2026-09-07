
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace SecurityMaster.Core.Filtering;

public class AllowedFields<T>
{
    public HashSet<string> Fields { get; }
    public AllowedFields(HashSet<string> fields) => Fields = fields;
}

