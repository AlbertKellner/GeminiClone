using Microsoft.AspNetCore.Mvc;

namespace Starter.Template.AOT.Api.Features.Query.DiskItemGetByFolder;

[ApiController]
[Route("drives")]
public class DiskItemGetByFolderEndpoint(DiskItemGetByFolderUseCase useCase, ILogger<DiskItemGetByFolderEndpoint> logger) : ControllerBase
{
    [HttpGet("{driveId}/folder")]
    public async Task<IActionResult> GetByFolder([FromRoute] string driveId, [FromQuery] string path = "")
    {
        logger.LogInformation("[DiskItemGetByFolderEndpoint][GetByFolder] Receber requisição para pasta. DriveId={DriveId}, Path={Path}", driveId, path);

        var input = new DiskItemGetByFolderInput
        {
            DriveId = driveId,
            FolderPath = path
        };

        var output = await useCase.ExecuteAsync(input);

        if (output is null)
        {
            logger.LogInformation("[DiskItemGetByFolderEndpoint][GetByFolder] Pasta não encontrada. DriveId={DriveId}, Path={Path}", driveId, path);

            return NotFound();
        }

        logger.LogInformation("[DiskItemGetByFolderEndpoint][GetByFolder] Retornar itens da pasta. DriveId={DriveId}, Path={Path}", driveId, path);

        return Ok(output);
    }
}
