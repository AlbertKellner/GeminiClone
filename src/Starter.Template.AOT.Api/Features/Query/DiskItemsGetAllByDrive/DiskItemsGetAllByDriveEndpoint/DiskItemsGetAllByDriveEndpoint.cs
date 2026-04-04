using Microsoft.AspNetCore.Mvc;

namespace Starter.Template.AOT.Api.Features.Query.DiskItemsGetAllByDrive;

[ApiController]
[Route("drives")]
public class DiskItemsGetAllByDriveEndpoint(DiskItemsGetAllByDriveUseCase useCase, ILogger<DiskItemsGetAllByDriveEndpoint> logger) : ControllerBase
{
    [HttpGet("{driveId}/items")]
    public async Task<IActionResult> GetAllItems([FromRoute] string driveId)
    {
        logger.LogInformation("[DiskItemsGetAllByDriveEndpoint][GetAllItems] Receber requisição para varrer drive. DriveId={DriveId}", driveId);

        var output = await useCase.ExecuteAsync(driveId);

        if (output is null)
        {
            logger.LogInformation("[DiskItemsGetAllByDriveEndpoint][GetAllItems] Drive não encontrado. DriveId={DriveId}", driveId);

            return NotFound();
        }

        logger.LogInformation("[DiskItemsGetAllByDriveEndpoint][GetAllItems] Retornar estrutura do drive. DriveId={DriveId}", driveId);

        return Ok(output);
    }
}
