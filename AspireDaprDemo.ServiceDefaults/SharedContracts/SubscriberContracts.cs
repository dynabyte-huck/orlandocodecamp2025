using System.ComponentModel.DataAnnotations;

namespace AspireDaprDemo.ServiceDefaults.SharedContracts;

public record Subscriber(
    [property: Required(ErrorMessage = "SubscriberId is required.")]
    [property: MinLength(1, ErrorMessage = "SubscriberId cannot be empty.")]
    string SubscriberId,

    [property: Required(ErrorMessage = "Email is required.")]
    [property: EmailAddress(ErrorMessage = "Invalid email format.")]
    string Email,

    [property: Required(ErrorMessage = "FirstName is required.")]
    [property: MinLength(1, ErrorMessage = "FirstName cannot be empty.")]
    string FirstName,

    [property: Required(ErrorMessage = "LastName is required.")]
    [property: MinLength(1, ErrorMessage = "LastName cannot be empty.")]
    string LastName
);