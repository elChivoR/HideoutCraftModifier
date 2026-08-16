using HideoutCraftModifier.Services;
using Microsoft.AspNetCore.Mvc;

namespace HideoutCraftModifier.Web.Controllers;

[Route("hcm/api/icon")]
[ApiController]
public class IconController(IconCacheService iconCacheService) : ControllerBase
{
    [HttpGet("{templateId}")]
    public async Task<IActionResult> GetIcon(string templateId)
    {
        if (string.IsNullOrWhiteSpace(templateId) || templateId.Length != 24)
            return BadRequest();

        var bytes = await iconCacheService.GetIconAsync(templateId);
        if (bytes is null)
            return NotFound();

        return File(bytes, "image/webp");
    }
}
