using System;
using System.Collections.Generic;

namespace BuyOldBike_DAL.Entities;

public partial class Address
{
    public Guid AddressId { get; set; }

    public Guid? UserId { get; set; }

    public string? FullName { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Province { get; set; }

    public string? City { get; set; }

    public string? District { get; set; }

    public string? Ward { get; set; }

    public string? Detail { get; set; }

    public virtual User? User { get; set; }
}
