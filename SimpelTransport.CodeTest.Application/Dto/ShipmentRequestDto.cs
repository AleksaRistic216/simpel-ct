using System.Numerics;

namespace SimpelTransport.CodeTest.Application.Dto;

// DeclaredType: "Package", "HalfPallet", "Pallet", or "Unspecified"
public record ShipmentRequestDto(
    string RequestId,
    string DeclaredType,
    double WeightKg,
    double LengthCm,
    double WidthCm,
    double HeightCm
)
{
    public Vector3 Dimensions => new Vector3((float)LengthCm, (float)WidthCm, (float)HeightCm);
}
