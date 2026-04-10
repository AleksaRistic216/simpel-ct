using System.Numerics;
using SimpelTransport.CodeTest.Application.Dto;

namespace SimpelTransport.CodeTest.Application.Validators;

public class ShipmentValidator
{
    public ValidationResultDto ValidateAndNormalize(ShipmentRequestDto request)
    {
        // TODO: Implement the logic to normalize the DeclaredType and validate against the business rules.
        // Currently, it just returns exactly what the customer sent (WHICH IS WRONG)

        // NOTE: I didn't find any constraint about height???

        if (IsInvalidShipment(request.WeightKg, request.Dimensions))
            return new ValidationResultDto(false, string.Empty, "Too big for us");

        ValidateDeclarationType(ref request);

        return new ValidationResultDto(
            true,
            request.DeclaredType,
            "Dont see any requirements for message"
        );
    }

    void ValidateDeclarationType(ref ShipmentRequestDto request)
    {
        if (DoesFitInPallet(request.WeightKg, request.Dimensions))
            request = request with { DeclaredType = "Pallet" };
        if (DoesFitInHalfPallet(request.WeightKg, request.Dimensions))
            request = request with { DeclaredType = "HalfPallet" };
        else
            return;
        if (DoesFitInPackage(request.WeightKg, request.Dimensions))
            request = request with { DeclaredType = "Package" };
        else
            return;
    }

    bool IsInvalidShipment(double weightKg, Vector3 dimensions)
    {
        if (weightKg <= 0 || dimensions.X <= 0 || dimensions.Y <= 0 || dimensions.Z <= 0)
            return true;
        if (weightKg > 40_000 || dimensions.X > 600 || dimensions.Y > 600 || dimensions.Z > 600)
            return true;
        return false;
    }

    bool DoesFitInPackage(double weightKg, Vector3 dimensions)
    {
        if (weightKg > 35)
            return false;
        if (dimensions.X > 150 || dimensions.Y > 150) // FOR HEIGHT CONSTRAINT SEE NOTE ABOVE
            return false;
        return true;
    }

    bool DoesFitInHalfPallet(double weightKg, Vector3 dimensions)
    {
        if (weightKg > 400)
            return false;

        // FOR HEIGHT CONSTRAINT SEE NOTE ABOVE

        // Dont allow if any side greater than 80
        if (dimensions.X > 80 || dimensions.Y > 80) // FOR HEIGHT CONSTRAINT SEE NOTE ABOVE
            return false;

        // Allow max 60 x 80
        var cSidesOver60 = (dimensions.X > 60 ? 1 : 0) + (dimensions.Y > 60 ? 1 : 0);
        if (cSidesOver60 > 1) // if both sides over 60, doesn't fit
            return false;

        return true;
    }

    bool DoesFitInPallet(double weightKg, Vector3 dimensions)
    {
        if (weightKg > 1000)
            return false;

        // NOTE: That max dimension in table for for Pallet (~120 cm x 80 cm) is confusing
        // FOR HEIGHT CONSTRAINT SEE NOTE ABOVE

        // Dont allow if any side greater than 600
        if (dimensions.X > 600 || dimensions.Y > 600) // FOR HEIGHT CONSTRAINT SEE NOTE ABOVE
            return false;

        // Allow max 300 x 600
        var cSidesOver300 = (dimensions.X > 300 ? 1 : 0) + (dimensions.Y > 300 ? 1 : 0);
        if (cSidesOver300 > 1) // if both sides over 300, doesn't fit
            return false;

        return true;
    }
}
