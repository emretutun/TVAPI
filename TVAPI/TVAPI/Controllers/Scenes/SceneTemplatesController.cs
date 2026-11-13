using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using TVAPI.Application.Scenes;

namespace TVAPI.Controllers.Scenes
{
    // SAHNE ŞABLONU UÇLARI (kaynak: kanal veritabanı; tbl_gui, tbl_gui_elements, tbl_gui_director)
    //   GET /api/v1/scenes/templates?directoryId=&name=  klasöre ve/veya ada göre şablonlar
    //   GET /api/v1/scenes/templates/{id}                tek şablon
    //
    // Her şablon, doldurulabilir alanları (elements) ve animasyonlarıyla (directors) birlikte döner.
    [ApiController]
    [Route("api/v1/scenes/templates")]
    public class SceneTemplatesController : ControllerBase
    {
        private const int MaxDirectories = 50;
        private const int MaxNameLength = 100; // tbl_gui.gui_name sütununun uzunluğu

        private readonly ISceneTemplateService _sceneTemplateService;

        public SceneTemplatesController(ISceneTemplateService sceneTemplateService)
        {
            _sceneTemplateService = sceneTemplateService;
        }

        /// <summary>
        /// Şablonları ada göre sıralı döner. En az bir süzgeç zorunludur.
        /// <c>directoryId</c> birden çok kez yazılabilir: <c>?directoryId=157&amp;directoryId=175</c>.
        /// <c>name</c> tam eşleşme arar.
        /// </summary>
        [HttpGet]
        [ProducesResponseType<IReadOnlyList<SceneTemplateDto>>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IReadOnlyList<SceneTemplateDto>>> Search(
            [FromQuery, MaxLength(MaxDirectories)] int[] directoryId,
            [FromQuery, MaxLength(MaxNameLength)] string? name,
            CancellationToken cancellationToken)
        {
            if (directoryId.Length == 0 && string.IsNullOrWhiteSpace(name))
            {
                ModelState.AddModelError(nameof(directoryId), "En az bir süzgeç verilmelidir: directoryId ya da name.");

                return ValidationProblem(ModelState);
            }

            return Ok(await _sceneTemplateService.SearchAsync(directoryId, name, cancellationToken));
        }

        /// <summary>Tek şablonu döner; yoksa ya da silinmişse 404.</summary>
        [HttpGet("{templateId:int:min(1)}")]
        [ProducesResponseType<SceneTemplateDto>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SceneTemplateDto>> GetById(int templateId, CancellationToken cancellationToken)
        {
            var template = await _sceneTemplateService.GetByIdAsync(templateId, cancellationToken);

            return template is null ? NotFound() : Ok(template);
        }
    }
}
