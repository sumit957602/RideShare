using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using NUnit.Framework;
using RideShare_Connect.DTOs;

namespace RideShare_Connect_Backend.Tests;

public class AddMoneyDtoValidationTests
{
    [Test]
    public void TransactionStatus_InvalidValue_ReturnsValidationError()
    {
        var dto = new AddMoneyDto
        {
            UserId = 1,
            Amount = 10m,
            PaymentMethod = "UPI",
            TransactionStatus = "Unknown"
        };

        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(dto, context, results, true);

        Assert.That(isValid, Is.False);
        Assert.That(results.Any(r => r.ErrorMessage == "transactionStatus should be Successful, Failed or Pending"), Is.True);
    }
}
